using EquipmentRental.BuildingBlocks.Eventing;
using EquipmentRental.Modules.Rentals.Application.Availability;
using EquipmentRental.Modules.Rentals.Domain.RentalOrders;
using EquipmentRental.Modules.Rentals.ProcessManagers.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EquipmentRental.Modules.Rentals.ProcessManagers;

public sealed class RentalConfirmationProcessProcessor(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider)
{
    private static readonly TimeSpan ClaimDuration = TimeSpan.FromMinutes(1);
    private const int MaximumAttempts = 5;

    public async Task<int> ProcessBatchAsync(
        int batchSize,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);

        var transitions = 0;
        for (var index = 0; index < batchSize; index++)
        {
            if (!await ProcessNextAsync(cancellationToken))
            {
                break;
            }

            transitions++;
        }

        return transitions;
    }

    private async Task<bool> ProcessNextAsync(
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var processDbContext = scope.ServiceProvider
            .GetRequiredService<RentalConfirmationProcessDbContext>();
        var process = await TryClaimNextAsync(
            processDbContext,
            cancellationToken);

        if (process is null)
        {
            return false;
        }

        try
        {
            var now = timeProvider.GetUtcNow();

            if (process.Status == RentalConfirmationProcessStatus.Running
                && now >= process.DeadlineUtc)
            {
                process.BeginCompensation(
                    "rentals.confirmation.timeout",
                    now);
                process.RecordProgress(now);
            }
            else if (process.Status ==
                     RentalConfirmationProcessStatus.Running)
            {
                await AdvanceConfirmationAsync(
                    scope.ServiceProvider,
                    process,
                    now,
                    cancellationToken);
            }
            else
            {
                await AdvanceCompensationAsync(
                    scope.ServiceProvider,
                    process,
                    now,
                    cancellationToken);
            }

            await processDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (
            exception is not OperationCanceledException
            || !cancellationToken.IsCancellationRequested)
        {
            process.RecordAttemptFailure(
                exception.Message,
                timeProvider.GetUtcNow(),
                MaximumAttempts);
            await processDbContext.SaveChangesAsync(cancellationToken);

            if (process.Status ==
                RentalConfirmationProcessStatus.RequiresIntervention)
            {
                OperationalTelemetry.ProcessRequiresIntervention.Add(1);
            }
        }

        OperationalTelemetry.ProcessTransitions.Add(
            1,
            new KeyValuePair<string, object?>(
                "process.status",
                process.Status.ToString()));
        return true;
    }

    private static async Task AdvanceConfirmationAsync(
        IServiceProvider services,
        RentalConfirmationProcess process,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var step = process.NextPendingStep();
        if (step is null)
        {
            await ConfirmRentalAsync(
                services,
                process,
                now,
                cancellationToken);
            return;
        }

        var gateway = services.GetRequiredService<IEquipmentAvailabilityGateway>();
        var outcome = await gateway.CommitAsync(
            new EquipmentAvailabilityRequest(
                RentalLineId.From(step.RentalLineId),
                EquipmentCategoryId.From(step.EquipmentCategoryId),
                FulfilmentLocationId.From(step.FulfilmentLocationId),
                step.Quantity,
                RentalPeriod.From(step.StartDate, step.EndDateExclusive)),
            cancellationToken);

        if (!outcome.Accepted)
        {
            var rejectionCode = outcome.RejectionCode
                ?? "fleet_availability.rejected";
            step.MarkRejected(rejectionCode);
            process.BeginCompensation(rejectionCode, now);
            process.RecordProgress(now);
            return;
        }

        var commitmentId = outcome.CommitmentId
            ?? throw new InvalidOperationException(
                "An accepted commitment requires an id.");
        var repository = services.GetRequiredService<IRentalOrderRepository>();
        var order = await GetOrderAsync(
            repository,
            process.RentalOrderId,
            cancellationToken);
        order.RecordAvailabilityCommitment(
            RentalLineId.From(step.RentalLineId),
            commitmentId,
            RentalPeriod.From(step.StartDate, step.EndDateExclusive),
            now);
        await repository.SaveChangesAsync(cancellationToken);

        step.MarkCommitted(commitmentId.Value);
        process.RecordProgress(now);
    }

    private static async Task ConfirmRentalAsync(
        IServiceProvider services,
        RentalConfirmationProcess process,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (!process.AllStepsCommitted())
        {
            throw new InvalidOperationException(
                "Confirmation cannot complete with unfinished steps.");
        }

        var repository = services.GetRequiredService<IRentalOrderRepository>();
        var order = await GetOrderAsync(
            repository,
            process.RentalOrderId,
            cancellationToken);

        if (order.Status == RentalOrderStatus.Quoted)
        {
            order.Confirm(now);
            await repository.SaveChangesAsync(cancellationToken);
        }
        else if (order.Status != RentalOrderStatus.Confirmed)
        {
            throw new InvalidOperationException(
                "The rental order is not confirmable.");
        }

        process.Complete(now);
    }

    private static async Task AdvanceCompensationAsync(
        IServiceProvider services,
        RentalConfirmationProcess process,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var step = process.NextCommittedStep();
        if (step is null)
        {
            process.FinishCompensation(now);
            return;
        }

        var expectedCommitmentId = step.CommitmentId
            ?? throw new InvalidOperationException(
                "A committed step requires a commitment id.");
        var gateway = services.GetRequiredService<IEquipmentAvailabilityGateway>();
        var outcome = await gateway.ReleaseAsync(
            new EquipmentAvailabilityReleaseRequest(
                RentalLineId.From(step.RentalLineId),
                EquipmentCategoryId.From(step.EquipmentCategoryId),
                FulfilmentLocationId.From(step.FulfilmentLocationId)),
            cancellationToken);

        if (!outcome.Released || outcome.CommitmentId?.Value != expectedCommitmentId)
        {
            throw new InvalidOperationException(
                outcome.RejectionCode
                ?? "Availability compensation failed.");
        }

        var repository = services.GetRequiredService<IRentalOrderRepository>();
        var order = await GetOrderAsync(
            repository,
            process.RentalOrderId,
            cancellationToken);
        order.ReleaseAvailabilityCommitment(
            RentalLineId.From(step.RentalLineId),
            AvailabilityCommitmentId.From(expectedCommitmentId),
            now);
        await repository.SaveChangesAsync(cancellationToken);

        step.MarkReleased();
        process.RecordProgress(now);
    }

    private async Task<RentalConfirmationProcess?> TryClaimNextAsync(
        RentalConfirmationProcessDbContext dbContext,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var now = timeProvider.GetUtcNow();
            var process = await dbContext.Processes
                .Include(candidate => candidate.Steps)
                .Where(candidate =>
                    candidate.Status == RentalConfirmationProcessStatus.Running
                    || candidate.Status ==
                    RentalConfirmationProcessStatus.Compensating)
                .Where(candidate =>
                    candidate.ClaimedUntilUtc == null
                    || candidate.ClaimedUntilUtc < now)
                .OrderBy(candidate => candidate.StartedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (process is null)
            {
                return null;
            }

            process.Claim(Guid.NewGuid(), now.Add(ClaimDuration));

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                return process;
            }
            catch (DbUpdateConcurrencyException)
            {
                dbContext.ChangeTracker.Clear();
            }
        }

        return null;
    }

    private static async Task<RentalOrder> GetOrderAsync(
        IRentalOrderRepository repository,
        Guid rentalOrderId,
        CancellationToken cancellationToken) =>
        await repository.GetAsync(
            RentalOrderId.From(rentalOrderId),
            cancellationToken)
        ?? throw new InvalidOperationException(
            $"Rental order '{rentalOrderId}' no longer exists.");
}

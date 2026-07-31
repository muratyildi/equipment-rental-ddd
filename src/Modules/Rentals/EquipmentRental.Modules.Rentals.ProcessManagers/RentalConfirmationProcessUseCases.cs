using EquipmentRental.Modules.Rentals.Application.Abstractions;
using EquipmentRental.Modules.Rentals.Domain.RentalOrders;
using EquipmentRental.Modules.Rentals.ProcessManagers.Persistence;

using Microsoft.EntityFrameworkCore;

using Npgsql;

namespace EquipmentRental.Modules.Rentals.ProcessManagers;

public sealed record StartRentalConfirmation(Guid RentalOrderId);

public sealed record GetRentalConfirmation(Guid RentalOrderId);

public sealed record RentalConfirmationStepSnapshot(
    Guid RentalLineId,
    string Status,
    Guid? CommitmentId,
    string? RejectionCode);

public sealed record RentalConfirmationProcessSnapshot(
    Guid Id,
    Guid RentalOrderId,
    string Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset DeadlineUtc,
    string? FailureCode,
    string? LastError,
    int Attempts,
    IReadOnlyCollection<RentalConfirmationStepSnapshot> Steps)
{
    internal static RentalConfirmationProcessSnapshot From(
        RentalConfirmationProcess process) =>
        new(
            process.Id,
            process.RentalOrderId,
            process.Status.ToString(),
            process.StartedAtUtc,
            process.UpdatedAtUtc,
            process.DeadlineUtc,
            process.FailureCode,
            process.LastError,
            process.Attempts,
            process.Steps
                .OrderBy(step => step.RentalLineId)
                .Select(step => new RentalConfirmationStepSnapshot(
                    step.RentalLineId,
                    step.Status.ToString(),
                    step.CommitmentId,
                    step.RejectionCode))
                .ToArray());
}

public sealed class StartRentalConfirmationHandler(
    IRentalOrderRepository rentalOrderRepository,
    RentalConfirmationProcessDbContext processDbContext,
    TimeProvider timeProvider)
{
    private static readonly TimeSpan MaximumDuration = TimeSpan.FromMinutes(5);

    public async Task<RentalConfirmationProcessSnapshot> Handle(
        StartRentalConfirmation command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var existing = await processDbContext.Processes
            .Include(process => process.Steps)
            .SingleOrDefaultAsync(
                process => process.RentalOrderId == command.RentalOrderId,
                cancellationToken);

        if (existing is not null)
        {
            return RentalConfirmationProcessSnapshot.From(existing);
        }

        var orderId = RentalOrderId.From(command.RentalOrderId);
        var order = await rentalOrderRepository.GetAsync(
            orderId,
            cancellationToken)
            ?? throw new RentalOrderNotFoundException(orderId);
        var now = timeProvider.GetUtcNow();
        var requirements = order.Lines
            .OrderBy(line => line.Id.Value)
            .Select(line => order.PrepareAvailabilityRequest(line.Id, now))
            .ToArray();
        var quoteDeadline = order.QuoteExpiresAtUtc
            ?? throw new InvalidOperationException(
                "A quoted rental order must have an expiry.");
        var deadline = now.Add(MaximumDuration) < quoteDeadline
            ? now.Add(MaximumDuration)
            : quoteDeadline;
        var process = RentalConfirmationProcess.Start(
            order.Id.Value,
            now,
            deadline,
            requirements);

        processDbContext.Processes.Add(process);
        try
        {
            await processDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
            })
        {
            processDbContext.ChangeTracker.Clear();
            var winner = await processDbContext.Processes
                .AsNoTracking()
                .Include(candidate => candidate.Steps)
                .SingleAsync(
                    candidate =>
                        candidate.RentalOrderId == command.RentalOrderId,
                    cancellationToken);
            return RentalConfirmationProcessSnapshot.From(winner);
        }

        return RentalConfirmationProcessSnapshot.From(process);
    }
}

public sealed class GetRentalConfirmationHandler(
    RentalConfirmationProcessDbContext dbContext)
{
    public async Task<RentalConfirmationProcessSnapshot?> Handle(
        GetRentalConfirmation query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var process = await dbContext.Processes
            .AsNoTracking()
            .Include(candidate => candidate.Steps)
            .SingleOrDefaultAsync(
                candidate => candidate.RentalOrderId == query.RentalOrderId,
                cancellationToken);

        return process is null
            ? null
            : RentalConfirmationProcessSnapshot.From(process);
    }
}

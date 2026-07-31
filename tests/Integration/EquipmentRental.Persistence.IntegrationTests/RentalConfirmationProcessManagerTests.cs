using EquipmentRental.Modules.FleetAvailability.Application.Availability;
using EquipmentRental.Modules.FleetAvailability.Contracts.Availability;
using EquipmentRental.Modules.FleetAvailability.Domain.Schedules;
using EquipmentRental.Modules.FleetAvailability.Infrastructure;
using EquipmentRental.Modules.Rentals.Domain.RentalOrders;
using EquipmentRental.Modules.Rentals.Infrastructure;
using EquipmentRental.Modules.Rentals.ProcessManagers;
using EquipmentRental.Modules.Rentals.ProcessManagers.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using FleetCategoryId =
    EquipmentRental.Modules.FleetAvailability.Domain.Schedules.EquipmentCategoryId;
using FleetLocationId =
    EquipmentRental.Modules.FleetAvailability.Domain.Schedules.LocationId;
using RentalCategoryId =
    EquipmentRental.Modules.Rentals.Domain.RentalOrders.EquipmentCategoryId;

namespace EquipmentRental.Persistence.IntegrationTests;

[Collection(PostgreSqlTestGroup.Name)]
public sealed class RentalConfirmationProcessManagerTests(
    PostgreSqlFixture fixture)
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AllLinesAvailable_ProcessConfirmsRental()
    {
        await FinishExistingProcessesAsync();
        var scenario = await SeedScenarioAsync(blockSecondCategory: false);
        var timeProvider = new MutableTimeProvider(Now);
        await using var services = CreateServices(timeProvider);
        var first = await StartAsync(services, scenario.RentalOrderId);
        var retry = await StartAsync(services, scenario.RentalOrderId);

        var transitions = await services
            .GetRequiredService<RentalConfirmationProcessProcessor>()
            .ProcessBatchAsync(20, CancellationToken.None);

        Assert.Equal(first.Id, retry.Id);
        Assert.Equal(3, transitions);

        await using var processContext =
            fixture.CreateRentalConfirmationProcessContext();
        var process = await processContext.Processes
            .Include(candidate => candidate.Steps)
            .SingleAsync(candidate =>
                candidate.RentalOrderId == scenario.RentalOrderId);
        Assert.Equal(
            RentalConfirmationProcessStatus.Completed,
            process.Status);
        Assert.All(
            process.Steps,
            step => Assert.Equal(
                RentalConfirmationStepStatus.Committed,
                step.Status));

        await using var rentalsContext = fixture.CreateRentalsContext();
        var order = await rentalsContext.RentalOrders
            .Include(candidate => candidate.Lines)
            .SingleAsync(candidate =>
                candidate.Id == RentalOrderId.From(scenario.RentalOrderId));
        Assert.Equal(RentalOrderStatus.Confirmed, order.Status);
        Assert.All(
            order.Lines,
            line => Assert.NotNull(line.AvailabilityCommitmentId));
    }

    [Fact]
    public async Task ConcurrentStartCommands_CreateOneProcess()
    {
        await FinishExistingProcessesAsync();
        var scenario = await SeedScenarioAsync(blockSecondCategory: false);
        await using var services =
            CreateServices(new MutableTimeProvider(Now));

        var results = await Task.WhenAll(
            StartAsync(services, scenario.RentalOrderId),
            StartAsync(services, scenario.RentalOrderId));

        Assert.Equal(results[0].Id, results[1].Id);
        await using var context =
            fixture.CreateRentalConfirmationProcessContext();
        Assert.Equal(1, await context.Processes.CountAsync(
            process => process.RentalOrderId == scenario.RentalOrderId));
    }

    [Fact]
    public async Task LaterLineRejected_ProcessCompensatesEarlierCommitment()
    {
        await FinishExistingProcessesAsync();
        var scenario = await SeedScenarioAsync(blockSecondCategory: true);
        var timeProvider = new MutableTimeProvider(Now);
        await using var services = CreateServices(timeProvider);
        await StartAsync(services, scenario.RentalOrderId);

        var transitions = await services
            .GetRequiredService<RentalConfirmationProcessProcessor>()
            .ProcessBatchAsync(20, CancellationToken.None);

        Assert.Equal(4, transitions);

        await using var processContext =
            fixture.CreateRentalConfirmationProcessContext();
        var process = await processContext.Processes
            .Include(candidate => candidate.Steps)
            .SingleAsync(candidate =>
                candidate.RentalOrderId == scenario.RentalOrderId);
        Assert.Equal(RentalConfirmationProcessStatus.Failed, process.Status);
        Assert.Equal(
            "fleet_availability.insufficient_capacity",
            process.FailureCode);
        Assert.Contains(
            process.Steps,
            step => step.Status == RentalConfirmationStepStatus.Released);
        Assert.Contains(
            process.Steps,
            step => step.Status == RentalConfirmationStepStatus.Rejected);

        await using var rentalsContext = fixture.CreateRentalsContext();
        var order = await rentalsContext.RentalOrders
            .Include(candidate => candidate.Lines)
            .SingleAsync(candidate =>
                candidate.Id == RentalOrderId.From(scenario.RentalOrderId));
        Assert.Equal(RentalOrderStatus.Quoted, order.Status);
        Assert.All(
            order.Lines,
            line => Assert.Null(line.AvailabilityCommitmentId));

        await using var fleetContext = fixture.CreateFleetContext();
        var firstSchedule = await fleetContext.AvailabilitySchedules
            .Include(schedule => schedule.Commitments)
            .SingleAsync(schedule =>
                schedule.EquipmentCategoryId ==
                FleetCategoryId.From(scenario.AvailableCategoryId)
                && schedule.LocationId ==
                FleetLocationId.From(scenario.LocationId));
        var processCommitment = Assert.Single(firstSchedule.Commitments);
        Assert.NotNull(processCommitment.ReleasedAtUtc);
    }

    [Fact]
    public async Task DeadlineExceeded_ProcessFailsWithoutHoldingCapacity()
    {
        await FinishExistingProcessesAsync();
        var scenario = await SeedScenarioAsync(blockSecondCategory: false);
        var timeProvider = new MutableTimeProvider(Now);
        await using var services = CreateServices(timeProvider);
        await StartAsync(services, scenario.RentalOrderId);
        timeProvider.Advance(TimeSpan.FromMinutes(6));

        var transitions = await services
            .GetRequiredService<RentalConfirmationProcessProcessor>()
            .ProcessBatchAsync(20, CancellationToken.None);

        Assert.Equal(2, transitions);

        await using var processContext =
            fixture.CreateRentalConfirmationProcessContext();
        var process = await processContext.Processes
            .Include(candidate => candidate.Steps)
            .SingleAsync(candidate =>
                candidate.RentalOrderId == scenario.RentalOrderId);
        Assert.Equal(RentalConfirmationProcessStatus.Failed, process.Status);
        Assert.Equal("rentals.confirmation.timeout", process.FailureCode);
        Assert.All(
            process.Steps,
            step => Assert.Equal(
                RentalConfirmationStepStatus.Pending,
                step.Status));
    }

    [Fact]
    public async Task RepeatedTechnicalFailure_RequiresOperatorIntervention()
    {
        await FinishExistingProcessesAsync();
        var scenario = await SeedScenarioAsync(blockSecondCategory: false);
        await using var services =
            CreateServices(new MutableTimeProvider(Now));
        await StartAsync(services, scenario.RentalOrderId);

        await using (var context =
                     fixture.CreateRentalConfirmationProcessContext())
        {
            var process = await context.Processes.SingleAsync(candidate =>
                candidate.RentalOrderId == scenario.RentalOrderId);

            for (var attempt = 0; attempt < 5; attempt++)
            {
                process.RecordAttemptFailure(
                    "Permanent downstream failure.",
                    Now.AddMinutes(attempt),
                    maximumAttempts: 5);
            }

            await context.SaveChangesAsync(CancellationToken.None);
        }

        await using var verificationContext =
            fixture.CreateRentalConfirmationProcessContext();
        var stored = await verificationContext.Processes.SingleAsync(
            process => process.RentalOrderId == scenario.RentalOrderId);
        Assert.Equal(
            RentalConfirmationProcessStatus.RequiresIntervention,
            stored.Status);
        Assert.Equal(5, stored.Attempts);
    }

    private ServiceProvider CreateServices(TimeProvider timeProvider)
    {
        var services = new ServiceCollection();
        services.AddSingleton(timeProvider);
        services.AddFleetAvailabilityInfrastructure(fixture.ConnectionString);
        services.AddScoped<
            IAvailabilityCommitmentService,
            AvailabilityCommitmentService>();
        services.AddRentalsInfrastructure(fixture.ConnectionString);
        services.AddRentalsProcessManagers(fixture.ConnectionString);
        return services.BuildServiceProvider();
    }

    private async Task FinishExistingProcessesAsync()
    {
        await using var context =
            fixture.CreateRentalConfirmationProcessContext();
        await context.Database.ExecuteSqlRawAsync(
            """
            UPDATE rentals_process_manager.rental_confirmation_processes
            SET status = 'Failed',
                claimed_by = NULL,
                claimed_until_utc = NULL
            WHERE status IN ('Running', 'Compensating');
            """);
    }

    private static async Task<RentalConfirmationProcessSnapshot> StartAsync(
        ServiceProvider services,
        Guid rentalOrderId)
    {
        using var scope = services.CreateScope();
        return await scope.ServiceProvider
            .GetRequiredService<StartRentalConfirmationHandler>()
            .Handle(
                new StartRentalConfirmation(rentalOrderId),
                CancellationToken.None);
    }

    private async Task<Scenario> SeedScenarioAsync(bool blockSecondCategory)
    {
        var firstCategoryId = Guid.NewGuid();
        var secondCategoryId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var rentalOrderId = RentalOrderId.New();
        var period = RentalPeriod.From(
            new DateOnly(2026, 8, 10),
            new DateOnly(2026, 8, 13));
        var order = RentalOrder.Draft(
            rentalOrderId,
            CustomerId.From(Guid.NewGuid()),
            FulfilmentLocationId.From(locationId),
            period,
            Now);
        var firstLineId = order.AddLine(
            RentalCategoryId.From(firstCategoryId),
            1,
            Money.Of(100, "TRY"),
            Now);
        var secondLineId = order.AddLine(
            RentalCategoryId.From(secondCategoryId),
            1,
            Money.Of(100, "TRY"),
            Now);
        order.Quote(Now.AddHours(1), Now);

        await using (var rentalsContext = fixture.CreateRentalsContext())
        {
            rentalsContext.RentalOrders.Add(order);
            await rentalsContext.SaveChangesAsync(CancellationToken.None);
        }

        var firstSchedule = AvailabilitySchedule.Define(
            AvailabilityScheduleId.New(),
            FleetCategoryId.From(firstCategoryId),
            FleetLocationId.From(locationId),
            1,
            Now);
        var secondSchedule = AvailabilitySchedule.Define(
            AvailabilityScheduleId.New(),
            FleetCategoryId.From(secondCategoryId),
            FleetLocationId.From(locationId),
            1,
            Now);

        var firstLineComesFirst =
            firstLineId.Value.CompareTo(secondLineId.Value) < 0;
        var availableCategoryId = firstLineComesFirst
            ? firstCategoryId
            : secondCategoryId;
        var blockedSchedule = firstLineComesFirst
            ? secondSchedule
            : firstSchedule;

        if (blockSecondCategory)
        {
            blockedSchedule.Commit(
                ExternalDemandId.From(Guid.NewGuid()),
                AvailabilityPeriod.From(
                    period.Start,
                    period.EndExclusive),
                1,
                Now);
        }

        await using (var fleetContext = fixture.CreateFleetContext())
        {
            fleetContext.AvailabilitySchedules.AddRange(
                firstSchedule,
                secondSchedule);
            await fleetContext.SaveChangesAsync(CancellationToken.None);
        }

        return new Scenario(
            rentalOrderId.Value,
            availableCategoryId,
            locationId);
    }

    private sealed record Scenario(
        Guid RentalOrderId,
        Guid AvailableCategoryId,
        Guid LocationId);

    private sealed class MutableTimeProvider(DateTimeOffset utcNow)
        : TimeProvider
    {
        private DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan duration) => _utcNow += duration;
    }
}

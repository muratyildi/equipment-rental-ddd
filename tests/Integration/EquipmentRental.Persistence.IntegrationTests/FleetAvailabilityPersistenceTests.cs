using EquipmentRental.Modules.FleetAvailability.Domain.Schedules;
using EquipmentRental.Modules.FleetAvailability.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace EquipmentRental.Persistence.IntegrationTests;

[Collection(PostgreSqlTestGroup.Name)]
public sealed class FleetAvailabilityPersistenceTests(PostgreSqlFixture fixture)
{
    private static readonly DateTimeOffset OccurredAt =
        new(2026, 7, 31, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Repository_RoundTripsScheduleAndCommitments()
    {
        var categoryId = EquipmentCategoryId.From(Guid.NewGuid());
        var locationId = LocationId.From(Guid.NewGuid());
        var period = AvailabilityPeriod.From(
            new DateOnly(2026, 8, 10),
            new DateOnly(2026, 8, 13));
        var schedule = AvailabilitySchedule.Define(
            AvailabilityScheduleId.New(),
            categoryId,
            locationId,
            4,
            OccurredAt);
        var decision = schedule.Commit(
            ExternalDemandId.From(Guid.NewGuid()),
            period,
            2,
            OccurredAt.AddMinutes(1));

        await using (var writeContext = fixture.CreateFleetContext())
        {
            var repository = new PostgresAvailabilityScheduleRepository(writeContext);
            await repository.AddAsync(
                schedule,
                CancellationToken.None);
            await repository.SaveChangesAsync(CancellationToken.None);
        }

        await using var readContext = fixture.CreateFleetContext();
        var readRepository = new PostgresAvailabilityScheduleRepository(readContext);
        var restored = await readRepository.GetAsync(
            categoryId,
            locationId,
            CancellationToken.None);

        Assert.NotNull(restored);
        Assert.True(decision.Accepted);
        Assert.Single(restored.Commitments);
        Assert.Equal(2, restored.CalculateAvailableQuantity(period));
        Assert.Empty(restored.DomainEvents);
    }

    [Fact]
    public async Task ConcurrentCommitments_CannotOverbookTheSameAggregate()
    {
        var categoryId = EquipmentCategoryId.From(Guid.NewGuid());
        var locationId = LocationId.From(Guid.NewGuid());
        var period = AvailabilityPeriod.From(
            new DateOnly(2026, 8, 10),
            new DateOnly(2026, 8, 13));
        var schedule = AvailabilitySchedule.Define(
            AvailabilityScheduleId.New(),
            categoryId,
            locationId,
            2,
            OccurredAt);

        await using (var seedContext = fixture.CreateFleetContext())
        {
            var repository = new PostgresAvailabilityScheduleRepository(seedContext);
            await repository.AddAsync(
                schedule,
                CancellationToken.None);
            await repository.SaveChangesAsync(CancellationToken.None);
        }

        await using var firstContext = fixture.CreateFleetContext();
        await using var secondContext = fixture.CreateFleetContext();
        var firstRepository =
            new PostgresAvailabilityScheduleRepository(firstContext);
        var secondRepository =
            new PostgresAvailabilityScheduleRepository(secondContext);

        var firstCopy = await firstRepository.GetAsync(
            categoryId,
            locationId,
            CancellationToken.None);
        var staleCopy = await secondRepository.GetAsync(
            categoryId,
            locationId,
            CancellationToken.None);

        var firstDecision = firstCopy!.Commit(
            ExternalDemandId.From(Guid.NewGuid()),
            period,
            2,
            OccurredAt.AddMinutes(1));
        var staleDecision = staleCopy!.Commit(
            ExternalDemandId.From(Guid.NewGuid()),
            period,
            2,
            OccurredAt.AddMinutes(1));

        Assert.True(firstDecision.Accepted);
        Assert.True(staleDecision.Accepted);

        await firstRepository.SaveChangesAsync(CancellationToken.None);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => secondRepository.SaveChangesAsync(
                CancellationToken.None));
    }
}

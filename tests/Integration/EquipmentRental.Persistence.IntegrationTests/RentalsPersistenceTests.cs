using EquipmentRental.Modules.Rentals.Domain.RentalOrders;
using EquipmentRental.Modules.Rentals.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace EquipmentRental.Persistence.IntegrationTests;

[Collection(PostgreSqlTestGroup.Name)]
public sealed class RentalsPersistenceTests(PostgreSqlFixture fixture)
{
    private static readonly DateTimeOffset OccurredAt =
        new(2026, 7, 31, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Repository_RoundTripsAggregateWithoutLeakingPersistenceIntoDomain()
    {
        var order = CreateQuotedOrder(out var firstLineId, out _);
        order.RecordAvailabilityCommitment(
            firstLineId,
            AvailabilityCommitmentId.From(Guid.NewGuid()),
            order.Period,
            OccurredAt.AddMinutes(2));

        await using (var writeContext = fixture.CreateRentalsContext())
        {
            var repository = new PostgresRentalOrderRepository(writeContext);
            await repository.AddAsync(order, CancellationToken.None);
            await repository.SaveChangesAsync(CancellationToken.None);
        }

        await using var readContext = fixture.CreateRentalsContext();
        var readRepository = new PostgresRentalOrderRepository(readContext);

        var restored = await readRepository.GetAsync(
            order.Id,
            CancellationToken.None);

        Assert.NotNull(restored);
        Assert.Equal(RentalOrderStatus.Quoted, restored.Status);
        Assert.Equal(2, restored.Lines.Count);
        Assert.Equal(9000m, restored.EstimatedTotal!.Amount);
        Assert.Equal("TRY", restored.EstimatedTotal.Currency);
        Assert.NotNull(restored.Lines.Single(line => line.Id == firstLineId)
            .AvailabilityCommitmentId);
        Assert.Empty(restored.DomainEvents);
    }

    [Fact]
    public async Task SavingTwoStaleCopies_ThrowsOptimisticConcurrencyException()
    {
        var order = CreateQuotedOrder(out var firstLineId, out var secondLineId);

        await using (var seedContext = fixture.CreateRentalsContext())
        {
            var repository = new PostgresRentalOrderRepository(seedContext);
            await repository.AddAsync(order, CancellationToken.None);
            await repository.SaveChangesAsync(CancellationToken.None);
        }

        await using var firstContext = fixture.CreateRentalsContext();
        await using var secondContext = fixture.CreateRentalsContext();
        var firstRepository = new PostgresRentalOrderRepository(firstContext);
        var secondRepository = new PostgresRentalOrderRepository(secondContext);

        var firstCopy = await firstRepository.GetAsync(
            order.Id,
            CancellationToken.None);
        var staleCopy = await secondRepository.GetAsync(
            order.Id,
            CancellationToken.None);

        firstCopy!.RecordAvailabilityCommitment(
            firstLineId,
            AvailabilityCommitmentId.From(Guid.NewGuid()),
            firstCopy.Period,
            OccurredAt.AddMinutes(2));
        staleCopy!.RecordAvailabilityCommitment(
            secondLineId,
            AvailabilityCommitmentId.From(Guid.NewGuid()),
            staleCopy.Period,
            OccurredAt.AddMinutes(2));

        await firstRepository.SaveChangesAsync(CancellationToken.None);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => secondRepository.SaveChangesAsync(
                CancellationToken.None));
    }

    private static RentalOrder CreateQuotedOrder(
        out RentalLineId firstLineId,
        out RentalLineId secondLineId)
    {
        var order = RentalOrder.Draft(
            RentalOrderId.New(),
            CustomerId.From(Guid.NewGuid()),
            FulfilmentLocationId.From(Guid.NewGuid()),
            RentalPeriod.From(
                new DateOnly(2026, 8, 10),
                new DateOnly(2026, 8, 13)),
            OccurredAt);

        firstLineId = order.AddLine(
            EquipmentCategoryId.From(Guid.NewGuid()),
            2,
            Money.Of(1000, "TRY"),
            OccurredAt);
        secondLineId = order.AddLine(
            EquipmentCategoryId.From(Guid.NewGuid()),
            1,
            Money.Of(1000, "TRY"),
            OccurredAt);
        order.Quote(OccurredAt.AddHours(1), OccurredAt.AddMinutes(1));

        return order;
    }
}

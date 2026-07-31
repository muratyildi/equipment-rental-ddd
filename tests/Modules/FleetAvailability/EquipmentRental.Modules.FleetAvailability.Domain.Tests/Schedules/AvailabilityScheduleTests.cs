using EquipmentRental.Modules.FleetAvailability.Domain.Abstractions;
using EquipmentRental.Modules.FleetAvailability.Domain.Schedules;

namespace EquipmentRental.Modules.FleetAvailability.Domain.Tests.Schedules;

public sealed class AvailabilityScheduleTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);

    private static readonly AvailabilityPeriod Period =
        AvailabilityPeriod.From(
            new DateOnly(2026, 8, 10),
            new DateOnly(2026, 8, 13));

    [Fact]
    public void Define_WithNonPositiveCapacity_IsRejected()
    {
        var exception = Assert.Throws<DomainRuleViolationException>(
            () => CreateSchedule(totalCapacity: 0));

        Assert.Equal("fleet_availability.capacity.invalid", exception.Code);
    }

    [Fact]
    public void Commit_WithinCapacity_AcceptsAndRaisesDomainEvent()
    {
        var schedule = CreateSchedule(totalCapacity: 3);

        var decision = schedule.Commit(
            ExternalDemandId.From(Guid.NewGuid()),
            Period,
            quantity: 2,
            Now);

        Assert.True(decision.Accepted);
        Assert.NotNull(decision.CommitmentId);
        Assert.Equal(1, decision.AvailableQuantity);
        Assert.False(decision.WasAlreadyCommitted);
        var committed = Assert.Single(
            schedule.DomainEvents.OfType<EquipmentAvailabilityCommitted>());
        Assert.NotEqual(Guid.Empty, committed.EventId);
    }

    [Fact]
    public void Commit_WhenOverlappingDemandExceedsCapacity_ReturnsBusinessRejection()
    {
        var schedule = CreateSchedule(totalCapacity: 3);
        schedule.Commit(
            ExternalDemandId.From(Guid.NewGuid()),
            Period,
            quantity: 2,
            Now);

        var decision = schedule.Commit(
            ExternalDemandId.From(Guid.NewGuid()),
            Period,
            quantity: 2,
            Now.AddMinutes(1));

        Assert.False(decision.Accepted);
        Assert.Equal(1, decision.AvailableQuantity);
        Assert.Equal(
            "fleet_availability.insufficient_capacity",
            decision.RejectionCode);
        Assert.Single(schedule.Commitments);
    }

    [Fact]
    public void Commit_ForAdjacentPeriod_CanReuseFullCapacity()
    {
        var schedule = CreateSchedule(totalCapacity: 2);
        schedule.Commit(
            ExternalDemandId.From(Guid.NewGuid()),
            Period,
            quantity: 2,
            Now);
        var adjacent = AvailabilityPeriod.From(
            Period.EndExclusive,
            Period.EndExclusive.AddDays(2));

        var decision = schedule.Commit(
            ExternalDemandId.From(Guid.NewGuid()),
            adjacent,
            quantity: 2,
            Now.AddMinutes(1));

        Assert.True(decision.Accepted);
        Assert.Equal(0, decision.AvailableQuantity);
        Assert.Equal(2, schedule.Commitments.Count);
    }

    [Fact]
    public void Commit_WhenSameDemandIsRetried_ReturnsOriginalCommitment()
    {
        var schedule = CreateSchedule(totalCapacity: 2);
        var demandId = ExternalDemandId.From(Guid.NewGuid());
        var first = schedule.Commit(demandId, Period, quantity: 1, Now);
        var eventCount = schedule.DomainEvents.Count;

        var retry = schedule.Commit(
            demandId,
            Period,
            quantity: 1,
            Now.AddMinutes(1));

        Assert.True(retry.Accepted);
        Assert.True(retry.WasAlreadyCommitted);
        Assert.Equal(first.CommitmentId, retry.CommitmentId);
        Assert.Equal(eventCount, schedule.DomainEvents.Count);
        Assert.Single(schedule.Commitments);
    }

    [Fact]
    public void Commit_WhenSameDemandHasDifferentTerms_IsRejectedAsConflict()
    {
        var schedule = CreateSchedule(totalCapacity: 2);
        var demandId = ExternalDemandId.From(Guid.NewGuid());
        schedule.Commit(demandId, Period, quantity: 1, Now);

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => schedule.Commit(
                demandId,
                Period,
                quantity: 2,
                Now.AddMinutes(1)));

        Assert.Equal(
            "fleet_availability.commitment.demand_conflict",
            exception.Code);
    }

    [Fact]
    public void CalculateAvailableQuantity_UsesPeakOverlapRatherThanSumOfAllCommitments()
    {
        var schedule = CreateSchedule(totalCapacity: 5);
        schedule.Commit(
            ExternalDemandId.From(Guid.NewGuid()),
            AvailabilityPeriod.From(
                new DateOnly(2026, 8, 10),
                new DateOnly(2026, 8, 12)),
            quantity: 2,
            Now);
        schedule.Commit(
            ExternalDemandId.From(Guid.NewGuid()),
            AvailabilityPeriod.From(
                new DateOnly(2026, 8, 12),
                new DateOnly(2026, 8, 14)),
            quantity: 2,
            Now);

        var available = schedule.CalculateAvailableQuantity(
            AvailabilityPeriod.From(
                new DateOnly(2026, 8, 11),
                new DateOnly(2026, 8, 13)));

        Assert.Equal(3, available);
    }

    [Fact]
    public void Release_ReturnsCapacityAndRaisesDomainEvent()
    {
        var schedule = CreateSchedule(totalCapacity: 2);
        var demandId = ExternalDemandId.From(Guid.NewGuid());
        schedule.Commit(demandId, Period, quantity: 2, Now);

        var decision = schedule.Release(demandId, Now.AddMinutes(1));

        Assert.True(decision.Released);
        Assert.False(decision.WasAlreadyReleased);
        Assert.Equal(2, schedule.CalculateAvailableQuantity(Period));
        Assert.Single(
            schedule.DomainEvents.OfType<EquipmentAvailabilityReleased>());
    }

    [Fact]
    public void Release_WhenRetried_IsIdempotent()
    {
        var schedule = CreateSchedule(totalCapacity: 2);
        var demandId = ExternalDemandId.From(Guid.NewGuid());
        schedule.Commit(demandId, Period, quantity: 1, Now);
        var first = schedule.Release(demandId, Now.AddMinutes(1));
        var eventCount = schedule.DomainEvents.Count;

        var retry = schedule.Release(demandId, Now.AddMinutes(2));

        Assert.True(retry.Released);
        Assert.True(retry.WasAlreadyReleased);
        Assert.Equal(first.CommitmentId, retry.CommitmentId);
        Assert.Equal(eventCount, schedule.DomainEvents.Count);
    }

    private static AvailabilitySchedule CreateSchedule(int totalCapacity) =>
        AvailabilitySchedule.Define(
            AvailabilityScheduleId.New(),
            EquipmentCategoryId.From(Guid.NewGuid()),
            LocationId.From(Guid.NewGuid()),
            totalCapacity,
            Now);
}

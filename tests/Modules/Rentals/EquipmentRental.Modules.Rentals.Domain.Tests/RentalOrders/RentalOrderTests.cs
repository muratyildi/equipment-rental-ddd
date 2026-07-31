using EquipmentRental.Modules.Rentals.Domain.Abstractions;
using EquipmentRental.Modules.Rentals.Domain.RentalOrders;

namespace EquipmentRental.Modules.Rentals.Domain.Tests.RentalOrders;

public sealed class RentalOrderTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);

    private static readonly RentalPeriod Period =
        RentalPeriod.From(new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 13));

    [Fact]
    public void Draft_RaisesEventContainingBusinessIdentityAndPeriod()
    {
        var customerId = CustomerId.From(Guid.NewGuid());

        var order = RentalOrder.Draft(
            RentalOrderId.New(),
            customerId,
            FulfilmentLocationId.From(Guid.NewGuid()),
            Period,
            Now);

        var drafted = Assert.Single(order.DomainEvents.OfType<RentalOrderDrafted>());
        Assert.NotEqual(Guid.Empty, drafted.EventId);
        Assert.Equal(customerId, drafted.CustomerId);
        Assert.Equal(Period, drafted.Period);
        Assert.Equal(RentalOrderStatus.Draft, order.Status);
    }

    [Fact]
    public void Quote_WhenOrderHasNoLines_IsRejected()
    {
        var order = DraftOrder();

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => order.Quote(Now.AddDays(1), Now));

        Assert.Equal("rentals.order.empty", exception.Code);
    }

    [Fact]
    public void Quote_CalculatesEstimateFromPeriodQuantityAndDailyRate()
    {
        var order = DraftOrder();
        order.AddLine(
            EquipmentCategoryId.From(Guid.NewGuid()),
            quantity: 2,
            Money.Of(100, "TRY"),
            Now);
        order.AddLine(
            EquipmentCategoryId.From(Guid.NewGuid()),
            quantity: 1,
            Money.Of(50, "TRY"),
            Now);

        order.Quote(Now.AddDays(1), Now);

        Assert.Equal(RentalOrderStatus.Quoted, order.Status);
        Assert.Equal(Money.Of(750, "TRY"), order.EstimatedTotal);
    }

    [Fact]
    public void AddLine_AfterQuote_IsRejectedToProtectAcceptedTerms()
    {
        var order = DraftOrderWithSingleLine();
        order.Quote(Now.AddDays(1), Now);

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => order.AddLine(
                EquipmentCategoryId.From(Guid.NewGuid()),
                1,
                Money.Of(25, "TRY"),
                Now));

        Assert.Equal("rentals.order.status_transition_invalid", exception.Code);
    }

    [Fact]
    public void Confirm_WithoutCommitmentForEveryLine_IsRejected()
    {
        var order = DraftOrderWithSingleLine();
        order.Quote(Now.AddDays(1), Now);

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => order.Confirm(Now.AddMinutes(5)));

        Assert.Equal("rentals.order.availability_incomplete", exception.Code);
    }

    [Fact]
    public void RecordAvailabilityCommitment_ForDifferentPeriod_IsRejected()
    {
        var order = DraftOrderWithSingleLine();
        order.Quote(Now.AddDays(1), Now);
        var line = Assert.Single(order.Lines);
        var differentPeriod = RentalPeriod.From(
            Period.Start,
            Period.EndExclusive.AddDays(1));

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => order.RecordAvailabilityCommitment(
                line.Id,
                AvailabilityCommitmentId.From(Guid.NewGuid()),
                differentPeriod,
                Now.AddMinutes(5)));

        Assert.Equal("rentals.order.commitment_period_mismatch", exception.Code);
    }

    [Fact]
    public void Confirm_WithMatchingCommitments_TransitionsAndRaisesDomainEvent()
    {
        var order = DraftOrderWithSingleLine();
        order.Quote(Now.AddDays(1), Now);
        var line = Assert.Single(order.Lines);
        order.RecordAvailabilityCommitment(
            line.Id,
            AvailabilityCommitmentId.From(Guid.NewGuid()),
            Period,
            Now.AddMinutes(5));

        order.Confirm(Now.AddMinutes(10));

        Assert.Equal(RentalOrderStatus.Confirmed, order.Status);
        var confirmed = Assert.Single(
            order.DomainEvents.OfType<RentalOrderConfirmed>());
        Assert.NotEqual(Guid.Empty, confirmed.EventId);
        Assert.Equal(order.Id, confirmed.RentalOrderId);
        Assert.Equal(Money.Of(300, "TRY"), confirmed.EstimatedTotal);
    }

    [Fact]
    public void Confirm_WhenQuoteExpired_IsRejected()
    {
        var order = DraftOrderWithSingleLine();
        order.Quote(Now.AddMinutes(30), Now);
        var line = Assert.Single(order.Lines);
        order.RecordAvailabilityCommitment(
            line.Id,
            AvailabilityCommitmentId.From(Guid.NewGuid()),
            Period,
            Now.AddMinutes(5));

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => order.Confirm(Now.AddMinutes(30)));

        Assert.Equal("rentals.order.quote_expired", exception.Code);
    }

    [Fact]
    public void ReleaseCommitment_ClearsLineForCompensation()
    {
        var order = DraftOrderWithSingleLine();
        order.Quote(Now.AddDays(1), Now);
        var line = Assert.Single(order.Lines);
        var commitmentId = AvailabilityCommitmentId.From(Guid.NewGuid());
        order.RecordAvailabilityCommitment(
            line.Id,
            commitmentId,
            Period,
            Now.AddMinutes(1));

        order.ReleaseAvailabilityCommitment(
            line.Id,
            commitmentId,
            Now.AddMinutes(2));

        Assert.Null(line.AvailabilityCommitmentId);
        Assert.Single(
            order.DomainEvents.OfType<AvailabilityReleasedForRentalLine>());
    }

    private static RentalOrder DraftOrder() =>
        RentalOrder.Draft(
            RentalOrderId.New(),
            CustomerId.From(Guid.NewGuid()),
            FulfilmentLocationId.From(Guid.NewGuid()),
            Period,
            Now);

    private static RentalOrder DraftOrderWithSingleLine()
    {
        var order = DraftOrder();
        order.AddLine(
            EquipmentCategoryId.From(Guid.NewGuid()),
            quantity: 1,
            Money.Of(100, "TRY"),
            Now);

        return order;
    }
}

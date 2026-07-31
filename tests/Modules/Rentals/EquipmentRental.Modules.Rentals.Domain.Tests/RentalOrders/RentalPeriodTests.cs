using EquipmentRental.Modules.Rentals.Domain.RentalOrders;

namespace EquipmentRental.Modules.Rentals.Domain.Tests.RentalOrders;

public sealed class RentalPeriodTests
{
    [Fact]
    public void From_UsesHalfOpenDateRange()
    {
        var period = RentalPeriod.From(
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 4));

        Assert.Equal(3, period.BillableDays);
    }

    [Fact]
    public void Overlaps_WhenOnePeriodStartsAtOtherEnd_ReturnsFalse()
    {
        var first = RentalPeriod.From(
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 4));
        var second = RentalPeriod.From(
            new DateOnly(2026, 8, 4),
            new DateOnly(2026, 8, 6));

        Assert.False(first.Overlaps(second));
    }
}

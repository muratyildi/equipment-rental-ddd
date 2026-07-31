using EquipmentRental.Modules.Rentals.Domain.Abstractions;
using EquipmentRental.Modules.Rentals.Domain.RentalOrders;

namespace EquipmentRental.Modules.Rentals.Domain.Tests.RentalOrders;

public sealed class MoneyTests
{
    [Fact]
    public void Of_NormalizesCurrencyAndUsesBankersRounding()
    {
        var money = Money.Of(10.125m, " try ");

        Assert.Equal(10.12m, money.Amount);
        Assert.Equal("TRY", money.Currency);
    }

    [Fact]
    public void Add_WhenCurrenciesDiffer_RejectsOperation()
    {
        var tryAmount = Money.Of(100, "TRY");
        var usdAmount = Money.Of(100, "USD");

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => tryAmount.Add(usdAmount));

        Assert.Equal("rentals.money.currency_mismatch", exception.Code);
    }
}

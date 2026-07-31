using EquipmentRental.Modules.Rentals.Domain.Abstractions;

namespace EquipmentRental.Modules.Rentals.Domain.RentalOrders;

public sealed record Money
{
    private Money()
    {
        Currency = null!;
    }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; }

    public static Money Of(decimal amount, string currency)
    {
        if (amount < 0)
        {
            throw new DomainRuleViolationException(
                "rentals.money.negative",
                "Money cannot have a negative amount.");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new DomainRuleViolationException(
                "rentals.money.currency_required",
                "Money requires a currency.");
        }

        var normalizedCurrency = currency.Trim().ToUpperInvariant();
        if (normalizedCurrency.Length != 3)
        {
            throw new DomainRuleViolationException(
                "rentals.money.currency_invalid",
                "Currency must be a three-letter ISO 4217 code.");
        }

        return new Money(decimal.Round(amount, 2, MidpointRounding.ToEven), normalizedCurrency);
    }

    public Money Add(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (!string.Equals(Currency, other.Currency, StringComparison.Ordinal))
        {
            throw new DomainRuleViolationException(
                "rentals.money.currency_mismatch",
                $"Cannot add {Currency} and {other.Currency} amounts.");
        }

        return Of(Amount + other.Amount, Currency);
    }

    public Money Multiply(int multiplier)
    {
        if (multiplier < 0)
        {
            throw new DomainRuleViolationException(
                "rentals.money.multiplier_negative",
                "Money multiplier cannot be negative.");
        }

        return Of(Amount * multiplier, Currency);
    }
}

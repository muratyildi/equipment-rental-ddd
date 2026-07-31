namespace EquipmentRental.Modules.Rentals.Domain.Abstractions;

public sealed class DomainRuleViolationException(string code, string message)
    : Exception(message)
{
    public string Code { get; } = string.IsNullOrWhiteSpace(code)
        ? throw new ArgumentException("A domain rule code is required.", nameof(code))
        : code;
}

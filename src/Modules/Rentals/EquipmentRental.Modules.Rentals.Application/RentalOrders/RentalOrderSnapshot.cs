using EquipmentRental.Modules.Rentals.Domain.RentalOrders;

namespace EquipmentRental.Modules.Rentals.Application.RentalOrders;

public sealed record RentalLineSnapshot(
    Guid Id,
    Guid EquipmentCategoryId,
    int Quantity,
    decimal DailyRate,
    string Currency,
    Guid? AvailabilityCommitmentId);

public sealed record RentalOrderSnapshot(
    Guid Id,
    Guid CustomerId,
    Guid FulfilmentLocationId,
    DateOnly StartDate,
    DateOnly EndDateExclusive,
    string Status,
    decimal? EstimatedTotal,
    string? Currency,
    DateTimeOffset? QuoteExpiresAtUtc,
    IReadOnlyCollection<RentalLineSnapshot> Lines)
{
    public static RentalOrderSnapshot From(RentalOrder order) =>
        new(
            order.Id.Value,
            order.CustomerId.Value,
            order.FulfilmentLocationId.Value,
            order.Period.Start,
            order.Period.EndExclusive,
            order.Status.ToString(),
            order.EstimatedTotal?.Amount,
            order.EstimatedTotal?.Currency,
            order.QuoteExpiresAtUtc,
            order.Lines
                .Select(line => new RentalLineSnapshot(
                    line.Id.Value,
                    line.EquipmentCategoryId.Value,
                    line.Quantity,
                    line.DailyRate.Amount,
                    line.DailyRate.Currency,
                    line.AvailabilityCommitmentId?.Value))
                .ToArray());
}

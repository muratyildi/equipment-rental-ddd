using EquipmentRental.Modules.Rentals.Domain.RentalOrders;

namespace EquipmentRental.Modules.Rentals.Application.Abstractions;

public sealed class RentalOrderNotFoundException(RentalOrderId rentalOrderId)
    : Exception($"Rental order '{rentalOrderId}' was not found.")
{
    public RentalOrderId RentalOrderId { get; } = rentalOrderId;
}

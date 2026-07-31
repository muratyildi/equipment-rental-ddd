using EquipmentRental.Modules.Rentals.Domain.RentalOrders;

namespace EquipmentRental.Modules.Rentals.Application.Abstractions;

public sealed class RentalLineNotFoundException(
    RentalOrderId rentalOrderId,
    RentalLineId rentalLineId)
    : Exception(
        $"Rental line '{rentalLineId}' was not found in order '{rentalOrderId}'.")
{
    public RentalOrderId RentalOrderId { get; } = rentalOrderId;

    public RentalLineId RentalLineId { get; } = rentalLineId;
}

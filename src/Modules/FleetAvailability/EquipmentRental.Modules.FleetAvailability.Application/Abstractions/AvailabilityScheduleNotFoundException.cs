using EquipmentRental.Modules.FleetAvailability.Domain.Schedules;

namespace EquipmentRental.Modules.FleetAvailability.Application.Abstractions;

public sealed class AvailabilityScheduleNotFoundException(
    EquipmentCategoryId equipmentCategoryId,
    LocationId locationId)
    : Exception(
        $"No availability schedule exists for category '{equipmentCategoryId}' " +
        $"at location '{locationId}'.")
{
    public EquipmentCategoryId EquipmentCategoryId { get; } = equipmentCategoryId;

    public LocationId LocationId { get; } = locationId;
}

using EquipmentRental.Modules.FleetAvailability.Domain.Schedules;

namespace EquipmentRental.Modules.FleetAvailability.Application.Schedules;

public sealed record AvailabilityScheduleSnapshot(
    Guid Id,
    Guid EquipmentCategoryId,
    Guid LocationId,
    int TotalCapacity,
    int CommitmentCount)
{
    public static AvailabilityScheduleSnapshot From(AvailabilitySchedule schedule) =>
        new(
            schedule.Id.Value,
            schedule.EquipmentCategoryId.Value,
            schedule.LocationId.Value,
            schedule.TotalCapacity,
            schedule.Commitments.Count);
}

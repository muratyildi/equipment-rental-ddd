namespace EquipmentRental.Modules.FleetAvailability.ReadModel.Persistence;

public sealed class AvailabilityScheduleProjection
{
    private AvailabilityScheduleProjection()
    {
    }

    private AvailabilityScheduleProjection(
        Guid scheduleId,
        Guid equipmentCategoryId,
        Guid locationId,
        int totalCapacity,
        DateTimeOffset lastEventAtUtc)
    {
        ScheduleId = scheduleId;
        EquipmentCategoryId = equipmentCategoryId;
        LocationId = locationId;
        TotalCapacity = totalCapacity;
        LastEventAtUtc = lastEventAtUtc;
    }

    public Guid ScheduleId { get; private set; }

    public Guid EquipmentCategoryId { get; private set; }

    public Guid LocationId { get; private set; }

    public int TotalCapacity { get; private set; }

    public DateTimeOffset LastEventAtUtc { get; private set; }

    public static AvailabilityScheduleProjection Create(
        Guid scheduleId,
        Guid equipmentCategoryId,
        Guid locationId,
        int totalCapacity,
        DateTimeOffset lastEventAtUtc) =>
        new(
            scheduleId,
            equipmentCategoryId,
            locationId,
            totalCapacity,
            lastEventAtUtc);

    public bool HasSameDefinition(
        Guid equipmentCategoryId,
        Guid locationId,
        int totalCapacity) =>
        EquipmentCategoryId == equipmentCategoryId
        && LocationId == locationId
        && TotalCapacity == totalCapacity;
}

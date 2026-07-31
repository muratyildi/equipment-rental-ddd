namespace EquipmentRental.Modules.FleetAvailability.ReadModel.Persistence;

public sealed class AvailabilityDayProjection
{
    private AvailabilityDayProjection()
    {
    }

    public Guid ScheduleId { get; private set; }

    public DateOnly Date { get; private set; }

    public int CommittedQuantity { get; private set; }

    public DateTimeOffset LastEventAtUtc { get; private set; }
}

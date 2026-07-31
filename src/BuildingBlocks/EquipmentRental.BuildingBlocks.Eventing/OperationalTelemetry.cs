using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace EquipmentRental.BuildingBlocks.Eventing;

public static class OperationalTelemetry
{
    public const string ActivitySourceName = "EquipmentRental";
    public const string MeterName = "EquipmentRental.Operations";

    public static readonly ActivitySource ActivitySource =
        new(ActivitySourceName);

    private static readonly Meter Meter = new(MeterName);

    public static readonly Counter<long> OutboxPublished =
        Meter.CreateCounter<long>("equipment_rental.outbox.published");

    public static readonly Counter<long> OutboxFailed =
        Meter.CreateCounter<long>("equipment_rental.outbox.failed");

    public static readonly Counter<long> OutboxDeadLettered =
        Meter.CreateCounter<long>("equipment_rental.outbox.dead_lettered");

    public static readonly Counter<long> ProcessTransitions =
        Meter.CreateCounter<long>(
            "equipment_rental.process_manager.transitions");

    public static readonly Counter<long> ProcessRequiresIntervention =
        Meter.CreateCounter<long>(
            "equipment_rental.process_manager.requires_intervention");
}

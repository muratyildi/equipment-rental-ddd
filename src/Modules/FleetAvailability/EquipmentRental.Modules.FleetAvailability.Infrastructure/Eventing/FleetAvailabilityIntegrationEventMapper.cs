using System.Text.Json;

using EquipmentRental.BuildingBlocks.Eventing;
using EquipmentRental.Modules.FleetAvailability.Contracts.IntegrationEvents;
using EquipmentRental.Modules.FleetAvailability.Domain.Abstractions;
using EquipmentRental.Modules.FleetAvailability.Domain.Schedules;

namespace EquipmentRental.Modules.FleetAvailability.Infrastructure.Eventing;

internal static class FleetAvailabilityIntegrationEventMapper
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    public static OutboxMessage? Map(
        IDomainEvent domainEvent,
        DateTimeOffset createdAtUtc) =>
        domainEvent switch
        {
            AvailabilityCapacityDefined capacityDefined =>
                Map(capacityDefined, createdAtUtc),
            EquipmentAvailabilityCommitted committed =>
                Map(committed, createdAtUtc),
            EquipmentAvailabilityReleased released =>
                Map(released, createdAtUtc),
            _ => null,
        };

    private static OutboxMessage Map(
        AvailabilityCapacityDefined domainEvent,
        DateTimeOffset createdAtUtc)
    {
        var integrationEvent = new AvailabilityCapacityDefinedV1(
            domainEvent.EventId,
            domainEvent.ScheduleId.Value,
            domainEvent.EquipmentCategoryId.Value,
            domainEvent.LocationId.Value,
            domainEvent.TotalCapacity,
            domainEvent.OccurredAtUtc);

        return OutboxMessage.Create(
            domainEvent.EventId,
            AvailabilityCapacityDefinedV1.EventType,
            JsonSerializer.Serialize(integrationEvent, SerializerOptions),
            domainEvent.OccurredAtUtc,
            createdAtUtc);
    }

    private static OutboxMessage Map(
        EquipmentAvailabilityCommitted domainEvent,
        DateTimeOffset createdAtUtc)
    {
        var integrationEvent = new EquipmentAvailabilityCommittedV1(
            domainEvent.EventId,
            domainEvent.ScheduleId.Value,
            domainEvent.CommitmentId.Value,
            domainEvent.DemandId.Value,
            domainEvent.Period.Start,
            domainEvent.Period.EndExclusive,
            domainEvent.Quantity,
            domainEvent.OccurredAtUtc);

        return OutboxMessage.Create(
            domainEvent.EventId,
            EquipmentAvailabilityCommittedV1.EventType,
            JsonSerializer.Serialize(integrationEvent, SerializerOptions),
            domainEvent.OccurredAtUtc,
            createdAtUtc);
    }

    private static OutboxMessage Map(
        EquipmentAvailabilityReleased domainEvent,
        DateTimeOffset createdAtUtc)
    {
        var integrationEvent = new EquipmentAvailabilityReleasedV1(
            domainEvent.EventId,
            domainEvent.ScheduleId.Value,
            domainEvent.CommitmentId.Value,
            domainEvent.DemandId.Value,
            domainEvent.Period.Start,
            domainEvent.Period.EndExclusive,
            domainEvent.Quantity,
            domainEvent.OccurredAtUtc);

        return OutboxMessage.Create(
            domainEvent.EventId,
            EquipmentAvailabilityReleasedV1.EventType,
            JsonSerializer.Serialize(integrationEvent, SerializerOptions),
            domainEvent.OccurredAtUtc,
            createdAtUtc);
    }
}

namespace EquipmentRental.BuildingBlocks.Eventing;

public sealed record IntegrationEventEnvelope(
    Guid Id,
    string Type,
    string Payload,
    DateTimeOffset OccurredAtUtc);

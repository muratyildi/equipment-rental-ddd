namespace EquipmentRental.BuildingBlocks.Eventing;

public interface IIntegrationEventPublisher
{
    Task PublishAsync(
        IntegrationEventEnvelope message,
        CancellationToken cancellationToken);
}

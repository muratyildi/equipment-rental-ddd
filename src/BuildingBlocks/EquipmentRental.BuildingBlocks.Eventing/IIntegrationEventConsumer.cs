namespace EquipmentRental.BuildingBlocks.Eventing;

public interface IIntegrationEventConsumer
{
    string ConsumerName { get; }

    bool CanHandle(string integrationEventType);

    Task ConsumeAsync(
        IntegrationEventEnvelope message,
        CancellationToken cancellationToken);
}

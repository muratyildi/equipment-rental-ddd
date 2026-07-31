using EquipmentRental.BuildingBlocks.Eventing;

namespace EquipmentRental.Api.Eventing;

public sealed partial class InProcessIntegrationEventPublisher(
    IEnumerable<IIntegrationEventConsumer> consumers,
    ILogger<InProcessIntegrationEventPublisher> logger)
    : IIntegrationEventPublisher
{
    public async Task PublishAsync(
        IntegrationEventEnvelope message,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        var matchingConsumers = consumers
            .Where(consumer => consumer.CanHandle(message.Type))
            .ToArray();

        IntegrationEventPublished(
            logger,
            message.Id,
            message.Type,
            matchingConsumers.Length);

        foreach (var consumer in matchingConsumers)
        {
            await consumer.ConsumeAsync(message, cancellationToken);
        }
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Published integration event {IntegrationEventId} {IntegrationEventType} to {ConsumerCount} local consumers.")]
    private static partial void IntegrationEventPublished(
        ILogger logger,
        Guid integrationEventId,
        string integrationEventType,
        int consumerCount);
}

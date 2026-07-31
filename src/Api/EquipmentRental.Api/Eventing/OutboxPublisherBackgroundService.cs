using EquipmentRental.BuildingBlocks.Eventing;

namespace EquipmentRental.Api.Eventing;

public sealed partial class OutboxPublisherBackgroundService(
    IEnumerable<IOutboxProcessor> processors,
    TimeProvider timeProvider,
    ILogger<OutboxPublisherBackgroundService> logger)
    : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(1);
    private const int BatchSize = 20;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var processor in processors)
            {
                try
                {
                    var processed = await processor.ProcessBatchAsync(
                        BatchSize,
                        stoppingToken);

                    if (processed > 0)
                    {
                        OutboxBatchProcessed(
                            logger,
                            processed,
                            processor.ModuleName);
                    }
                }
                catch (OperationCanceledException) when (
                    stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception exception)
                {
                    OutboxProcessingFailed(logger, processor.ModuleName, exception);
                }
            }

            await Task.Delay(PollingInterval, timeProvider, stoppingToken);
        }
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Processed {OutboxMessageCount} {ModuleName} outbox messages.")]
    private static partial void OutboxBatchProcessed(
        ILogger logger,
        int outboxMessageCount,
        string moduleName);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to process the {ModuleName} outbox.")]
    private static partial void OutboxProcessingFailed(
        ILogger logger,
        string moduleName,
        Exception exception);
}

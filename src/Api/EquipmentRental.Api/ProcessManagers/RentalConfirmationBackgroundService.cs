using EquipmentRental.Modules.Rentals.ProcessManagers;

namespace EquipmentRental.Api.ProcessManagers;

public sealed partial class RentalConfirmationBackgroundService(
    RentalConfirmationProcessProcessor processor,
    TimeProvider timeProvider,
    ILogger<RentalConfirmationBackgroundService> logger)
    : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await processor.ProcessBatchAsync(20, stoppingToken);
            }
            catch (OperationCanceledException) when (
                stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                ProcessingFailed(logger, exception);
            }

            await Task.Delay(PollingInterval, timeProvider, stoppingToken);
        }
    }

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Rental confirmation process manager cycle failed.")]
    private static partial void ProcessingFailed(
        ILogger logger,
        Exception exception);
}

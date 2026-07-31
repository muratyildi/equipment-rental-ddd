namespace EquipmentRental.BuildingBlocks.Eventing;

public interface IOutboxProcessor
{
    string ModuleName { get; }

    Task<int> ProcessBatchAsync(
        int batchSize,
        CancellationToken cancellationToken);
}

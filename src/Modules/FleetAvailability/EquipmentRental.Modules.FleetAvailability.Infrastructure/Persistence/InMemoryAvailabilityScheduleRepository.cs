using System.Collections.Concurrent;

using EquipmentRental.Modules.FleetAvailability.Domain.Schedules;

namespace EquipmentRental.Modules.FleetAvailability.Infrastructure.Persistence;

public sealed class InMemoryAvailabilityScheduleRepository
    : IAvailabilityScheduleRepository
{
    private readonly ConcurrentDictionary<
        (EquipmentCategoryId CategoryId, LocationId LocationId),
        AvailabilitySchedule> _schedules = new();

    public Task AddAsync(
        AvailabilitySchedule schedule,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        cancellationToken.ThrowIfCancellationRequested();

        var key = (schedule.EquipmentCategoryId, schedule.LocationId);
        if (!_schedules.TryAdd(key, schedule))
        {
            throw new InvalidOperationException(
                $"Availability schedule '{schedule.Id}' has already been added.");
        }

        return Task.CompletedTask;
    }

    public Task<AvailabilitySchedule?> GetAsync(
        EquipmentCategoryId equipmentCategoryId,
        LocationId locationId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _schedules.TryGetValue(
            (equipmentCategoryId, locationId),
            out var schedule);

        return Task.FromResult(schedule);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}

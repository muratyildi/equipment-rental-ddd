using EquipmentRental.Modules.FleetAvailability.Application.Abstractions;
using EquipmentRental.Modules.FleetAvailability.Domain.Schedules;

namespace EquipmentRental.Modules.FleetAvailability.Application.Schedules;

public sealed record DefineAvailabilityCapacity(
    Guid EquipmentCategoryId,
    Guid LocationId,
    int TotalCapacity);

public sealed class DefineAvailabilityCapacityHandler(
    IAvailabilityScheduleRepository repository,
    TimeProvider timeProvider)
{
    public async Task<AvailabilityScheduleSnapshot> Handle(
        DefineAvailabilityCapacity command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var categoryId = Domain.Schedules.EquipmentCategoryId.From(
            command.EquipmentCategoryId);
        var locationId = Domain.Schedules.LocationId.From(command.LocationId);

        var existing = await repository.GetAsync(
            categoryId,
            locationId,
            cancellationToken);

        if (existing is not null)
        {
            throw new AvailabilityScheduleAlreadyExistsException(categoryId, locationId);
        }

        var schedule = AvailabilitySchedule.Define(
            AvailabilityScheduleId.New(),
            categoryId,
            locationId,
            command.TotalCapacity,
            timeProvider.GetUtcNow());

        await repository.AddAsync(schedule, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return AvailabilityScheduleSnapshot.From(schedule);
    }
}

namespace EquipmentRental.Modules.FleetAvailability.Domain.Schedules;

public interface IAvailabilityScheduleRepository
{
    Task AddAsync(
        AvailabilitySchedule schedule,
        CancellationToken cancellationToken);

    Task<AvailabilitySchedule?> GetAsync(
        EquipmentCategoryId equipmentCategoryId,
        LocationId locationId,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

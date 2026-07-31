using EquipmentRental.Modules.FleetAvailability.Domain.Schedules;

using Microsoft.EntityFrameworkCore;

namespace EquipmentRental.Modules.FleetAvailability.Infrastructure.Persistence;

public sealed class PostgresAvailabilityScheduleRepository(
    FleetAvailabilityDbContext dbContext)
    : IAvailabilityScheduleRepository
{
    public async Task AddAsync(
        AvailabilitySchedule schedule,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        await dbContext.AvailabilitySchedules.AddAsync(schedule, cancellationToken);
    }

    public Task<AvailabilitySchedule?> GetAsync(
        EquipmentCategoryId equipmentCategoryId,
        LocationId locationId,
        CancellationToken cancellationToken) =>
        dbContext.AvailabilitySchedules
            .Include(schedule => schedule.Commitments)
            .SingleOrDefaultAsync(
                schedule =>
                    schedule.EquipmentCategoryId == equipmentCategoryId &&
                    schedule.LocationId == locationId,
                cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}

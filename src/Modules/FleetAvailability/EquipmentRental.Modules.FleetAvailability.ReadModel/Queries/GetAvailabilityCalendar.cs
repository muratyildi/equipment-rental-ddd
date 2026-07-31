using EquipmentRental.Modules.FleetAvailability.ReadModel.Persistence;

using Microsoft.EntityFrameworkCore;

namespace EquipmentRental.Modules.FleetAvailability.ReadModel.Queries;

public sealed record GetAvailabilityCalendar(
    Guid EquipmentCategoryId,
    Guid LocationId,
    DateOnly StartDate,
    DateOnly EndDateExclusive);

public sealed record AvailabilityCalendarDay(
    DateOnly Date,
    int TotalCapacity,
    int CommittedQuantity,
    int AvailableQuantity);

public sealed record AvailabilityCalendarSnapshot(
    Guid ScheduleId,
    Guid EquipmentCategoryId,
    Guid LocationId,
    DateOnly StartDate,
    DateOnly EndDateExclusive,
    IReadOnlyCollection<AvailabilityCalendarDay> Days);

public sealed class GetAvailabilityCalendarHandler(
    AvailabilityCalendarDbContext dbContext)
{
    private const int MaximumCalendarDays = 366;

    public async Task<AvailabilityCalendarSnapshot?> Handle(
        GetAvailabilityCalendar query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var dayCount =
            query.EndDateExclusive.DayNumber - query.StartDate.DayNumber;

        if (dayCount <= 0 || dayCount > MaximumCalendarDays)
        {
            throw new ArgumentOutOfRangeException(
                nameof(query),
                $"Calendar range must contain 1-{MaximumCalendarDays} days.");
        }

        var schedule = await dbContext.Schedules
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.EquipmentCategoryId == query.EquipmentCategoryId
                    && candidate.LocationId == query.LocationId,
                cancellationToken);

        if (schedule is null)
        {
            return null;
        }

        var committedByDate = await dbContext.Days
            .AsNoTracking()
            .Where(day =>
                day.ScheduleId == schedule.ScheduleId
                && day.Date >= query.StartDate
                && day.Date < query.EndDateExclusive)
            .ToDictionaryAsync(
                day => day.Date,
                day => day.CommittedQuantity,
                cancellationToken);

        var days = Enumerable.Range(0, dayCount)
            .Select(offset =>
            {
                var date = query.StartDate.AddDays(offset);
                var committedQuantity = committedByDate.GetValueOrDefault(date);

                return new AvailabilityCalendarDay(
                    date,
                    schedule.TotalCapacity,
                    committedQuantity,
                    schedule.TotalCapacity - committedQuantity);
            })
            .ToArray();

        return new AvailabilityCalendarSnapshot(
            schedule.ScheduleId,
            schedule.EquipmentCategoryId,
            schedule.LocationId,
            query.StartDate,
            query.EndDateExclusive,
            days);
    }
}

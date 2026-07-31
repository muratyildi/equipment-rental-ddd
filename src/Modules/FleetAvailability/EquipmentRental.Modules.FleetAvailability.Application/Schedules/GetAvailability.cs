using EquipmentRental.Modules.FleetAvailability.Application.Abstractions;
using EquipmentRental.Modules.FleetAvailability.Domain.Schedules;

namespace EquipmentRental.Modules.FleetAvailability.Application.Schedules;

public sealed record GetAvailability(
    Guid EquipmentCategoryId,
    Guid LocationId,
    DateOnly StartDate,
    DateOnly EndDateExclusive);

public sealed record AvailabilitySnapshot(
    Guid EquipmentCategoryId,
    Guid LocationId,
    DateOnly StartDate,
    DateOnly EndDateExclusive,
    int TotalCapacity,
    int AvailableQuantity);

public sealed class GetAvailabilityHandler(IAvailabilityScheduleRepository repository)
{
    public async Task<AvailabilitySnapshot> Handle(
        GetAvailability query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var categoryId = Domain.Schedules.EquipmentCategoryId.From(
            query.EquipmentCategoryId);
        var locationId = Domain.Schedules.LocationId.From(query.LocationId);

        var schedule = await repository.GetAsync(
            categoryId,
            locationId,
            cancellationToken)
            ?? throw new AvailabilityScheduleNotFoundException(categoryId, locationId);

        var period = AvailabilityPeriod.From(
            query.StartDate,
            query.EndDateExclusive);

        return new AvailabilitySnapshot(
            categoryId.Value,
            locationId.Value,
            period.Start,
            period.EndExclusive,
            schedule.TotalCapacity,
            schedule.CalculateAvailableQuantity(period));
    }
}

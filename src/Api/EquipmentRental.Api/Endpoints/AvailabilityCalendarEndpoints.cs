using EquipmentRental.Api.Security;
using EquipmentRental.Modules.FleetAvailability.ReadModel.Queries;

namespace EquipmentRental.Api.Endpoints;

public static class AvailabilityCalendarEndpoints
{
    public static IEndpointRouteBuilder MapAvailabilityCalendarEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
                "/api/fleet-availability/calendar",
                GetCalendar)
            .WithTags("Fleet Availability")
            .WithName("GetAvailabilityCalendar")
            .RequireAuthorization(ApiSecurity.ReadPolicy);

        return endpoints;
    }

    private static async Task<IResult> GetCalendar(
        Guid equipmentCategoryId,
        Guid locationId,
        DateOnly startDate,
        DateOnly endDateExclusive,
        GetAvailabilityCalendarHandler handler,
        CancellationToken cancellationToken)
    {
        var dayCount = endDateExclusive.DayNumber - startDate.DayNumber;

        if (dayCount is <= 0 or > 366)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    [nameof(endDateExclusive)] =
                    [
                        "Calendar range must contain 1-366 days.",
                    ],
                });
        }

        var result = await handler.Handle(
            new GetAvailabilityCalendar(
                equipmentCategoryId,
                locationId,
                startDate,
                endDateExclusive),
            cancellationToken);

        return result is null
            ? Results.NotFound()
            : Results.Ok(result);
    }
}

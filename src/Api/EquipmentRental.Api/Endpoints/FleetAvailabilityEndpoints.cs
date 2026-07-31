using EquipmentRental.Api.Security;
using EquipmentRental.Modules.FleetAvailability.Application.Schedules;

namespace EquipmentRental.Api.Endpoints;

public static class FleetAvailabilityEndpoints
{
    public static IEndpointRouteBuilder MapFleetAvailabilityEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/fleet-availability")
            .WithTags("Fleet Availability");

        group.MapPost("/schedules", DefineCapacity)
            .WithName("DefineAvailabilityCapacity")
            .RequireAuthorization(ApiSecurity.WritePolicy);

        group.MapGet("/availability", GetAvailability)
            .WithName("GetEquipmentAvailability")
            .RequireAuthorization(ApiSecurity.ReadPolicy);

        return endpoints;
    }

    private static async Task<IResult> DefineCapacity(
        DefineAvailabilityCapacityRequest request,
        DefineAvailabilityCapacityHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new DefineAvailabilityCapacity(
                request.EquipmentCategoryId,
                request.LocationId,
                request.TotalCapacity),
            cancellationToken);

        return Results.Created(
            $"/api/fleet-availability/availability" +
            $"?equipmentCategoryId={result.EquipmentCategoryId}" +
            $"&locationId={result.LocationId}",
            result);
    }

    private static async Task<IResult> GetAvailability(
        Guid equipmentCategoryId,
        Guid locationId,
        DateOnly startDate,
        DateOnly endDateExclusive,
        GetAvailabilityHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new GetAvailability(
                equipmentCategoryId,
                locationId,
                startDate,
                endDateExclusive),
            cancellationToken);

        return Results.Ok(result);
    }
}

public sealed record DefineAvailabilityCapacityRequest(
    Guid EquipmentCategoryId,
    Guid LocationId,
    int TotalCapacity);

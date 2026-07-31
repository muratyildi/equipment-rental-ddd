using EquipmentRental.Api.Security;
using EquipmentRental.Modules.Rentals.Application.RentalOrders;
using EquipmentRental.Modules.Rentals.ProcessManagers;

namespace EquipmentRental.Api.Endpoints;

public static class RentalOrderEndpoints
{
    public static IEndpointRouteBuilder MapRentalOrderEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/rental-orders")
            .WithTags("Rental Orders");

        group.MapPost("/", CreateRentalOrder)
            .WithName("CreateRentalOrder")
            .RequireAuthorization(ApiSecurity.WritePolicy);

        group.MapGet("/{rentalOrderId:guid}", GetRentalOrder)
            .WithName("GetRentalOrder")
            .RequireAuthorization(ApiSecurity.ReadPolicy);

        group.MapPost("/{rentalOrderId:guid}/quote", QuoteRentalOrder)
            .WithName("QuoteRentalOrder")
            .RequireAuthorization(ApiSecurity.WritePolicy);

        group.MapPost(
                "/{rentalOrderId:guid}/lines/{rentalLineId:guid}/request-availability",
                RequestAvailability)
            .WithName("RequestRentalLineAvailability")
            .RequireAuthorization(ApiSecurity.WritePolicy);

        group.MapPost(
                "/{rentalOrderId:guid}/confirmation",
                StartRentalConfirmation)
            .WithName("StartRentalConfirmation")
            .RequireAuthorization(ApiSecurity.WritePolicy);

        group.MapGet(
                "/{rentalOrderId:guid}/confirmation",
                GetRentalConfirmation)
            .WithName("GetRentalConfirmation")
            .RequireAuthorization(ApiSecurity.ReadPolicy);

        return endpoints;
    }

    private static async Task<IResult> CreateRentalOrder(
        CreateRentalOrderRequest request,
        CreateRentalOrderHandler handler,
        CancellationToken cancellationToken)
    {
        var validationErrors =
            CreateRentalOrderRequestValidator.Validate(request);
        if (validationErrors.Count > 0)
        {
            return Results.ValidationProblem(validationErrors);
        }

        var command = new CreateRentalOrder(
            request.CustomerId,
            request.FulfilmentLocationId,
            request.StartDate,
            request.EndDateExclusive,
            request.Lines!
                .Select(line => new CreateRentalLine(
                    line!.EquipmentCategoryId,
                    line.Quantity,
                    line.DailyRate,
                    line.Currency))
                .ToArray());

        var result = await handler.Handle(command, cancellationToken);
        return Results.CreatedAtRoute(
            "GetRentalOrder",
            new { rentalOrderId = result.Id },
            result);
    }

    private static async Task<IResult> GetRentalOrder(
        Guid rentalOrderId,
        GetRentalOrderHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new GetRentalOrder(rentalOrderId),
            cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> QuoteRentalOrder(
        Guid rentalOrderId,
        QuoteRentalOrderRequest request,
        QuoteRentalOrderHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new QuoteRentalOrder(rentalOrderId, request.ExpiresAtUtc),
            cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> RequestAvailability(
        Guid rentalOrderId,
        Guid rentalLineId,
        RequestAvailabilityForRentalLineHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new RequestAvailabilityForRentalLine(rentalOrderId, rentalLineId),
            cancellationToken);

        return result.Accepted
            ? Results.Ok(result)
            : Results.Conflict(result);
    }

    private static async Task<IResult> StartRentalConfirmation(
        Guid rentalOrderId,
        StartRentalConfirmationHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new StartRentalConfirmation(rentalOrderId),
            cancellationToken);

        return Results.AcceptedAtRoute(
            "GetRentalConfirmation",
            new { rentalOrderId },
            result);
    }

    private static async Task<IResult> GetRentalConfirmation(
        Guid rentalOrderId,
        GetRentalConfirmationHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new GetRentalConfirmation(rentalOrderId),
            cancellationToken);

        return result is null
            ? Results.NotFound()
            : Results.Ok(result);
    }
}

public static class CreateRentalOrderRequestValidator
{
    public static Dictionary<string, string[]> Validate(
        CreateRentalOrderRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.Lines is null || request.Lines.Count == 0)
        {
            errors["lines"] =
                ["At least one rental line is required."];
        }
        else if (request.Lines.Any(line => line is null))
        {
            errors["lines"] =
                ["Rental lines cannot contain null items."];
        }

        return errors;
    }
}

public sealed record CreateRentalOrderRequest(
    Guid CustomerId,
    Guid FulfilmentLocationId,
    DateOnly StartDate,
    DateOnly EndDateExclusive,
    IReadOnlyCollection<CreateRentalLineRequest?>? Lines);

public sealed record CreateRentalLineRequest(
    Guid EquipmentCategoryId,
    int Quantity,
    decimal DailyRate,
    string Currency);

public sealed record QuoteRentalOrderRequest(DateTimeOffset ExpiresAtUtc);

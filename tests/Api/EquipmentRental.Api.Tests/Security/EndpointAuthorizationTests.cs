using EquipmentRental.Api.Endpoints;
using EquipmentRental.Api.Security;
using EquipmentRental.Modules.FleetAvailability.Application.Schedules;
using EquipmentRental.Modules.FleetAvailability.ReadModel.Queries;
using EquipmentRental.Modules.Rentals.Application.RentalOrders;
using EquipmentRental.Modules.Rentals.ProcessManagers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace EquipmentRental.Api.Tests.Security;

public sealed class EndpointAuthorizationTests
{
    [Theory]
    [InlineData("/api/rental-orders/", ApiSecurity.WritePolicy)]
    [InlineData(
        "/api/rental-orders/{rentalOrderId:guid}/confirmation",
        ApiSecurity.WritePolicy)]
    [InlineData(
        "/api/rental-orders/{rentalOrderId:guid}",
        ApiSecurity.ReadPolicy)]
    [InlineData(
        "/api/fleet-availability/availability",
        ApiSecurity.ReadPolicy)]
    [InlineData(
        "/api/fleet-availability/calendar",
        ApiSecurity.ReadPolicy)]
    public async Task BusinessEndpoint_RequiresExpectedPolicy(
        string route,
        string expectedPolicy)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddTransient<DefineAvailabilityCapacityHandler>();
        builder.Services.AddTransient<GetAvailabilityHandler>();
        builder.Services.AddTransient<GetAvailabilityCalendarHandler>();
        builder.Services.AddTransient<CreateRentalOrderHandler>();
        builder.Services.AddTransient<QuoteRentalOrderHandler>();
        builder.Services.AddTransient<RequestAvailabilityForRentalLineHandler>();
        builder.Services.AddTransient<GetRentalOrderHandler>();
        builder.Services.AddTransient<StartRentalConfirmationHandler>();
        builder.Services.AddTransient<GetRentalConfirmationHandler>();
        await using var app = builder.Build();
        app.MapRentalOrderEndpoints();
        app.MapFleetAvailabilityEndpoints();
        app.MapAvailabilityCalendarEndpoints();

        var endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .First(candidate =>
                candidate.RoutePattern.RawText == route);
        var policies = endpoint.Metadata
            .GetOrderedMetadata<IAuthorizeData>()
            .Select(data => data.Policy);

        Assert.Contains(expectedPolicy, policies);
    }
}

using System.Threading.RateLimiting;

using EquipmentRental.Api.Endpoints;
using EquipmentRental.Api.ErrorHandling;
using EquipmentRental.Api.Eventing;
using EquipmentRental.Api.Observability;
using EquipmentRental.Api.OpenApi;
using EquipmentRental.Api.ProcessManagers;
using EquipmentRental.Api.Security;
using EquipmentRental.BuildingBlocks.Eventing;
using EquipmentRental.Modules.FleetAvailability.Application.Availability;
using EquipmentRental.Modules.FleetAvailability.Application.Schedules;
using EquipmentRental.Modules.FleetAvailability.Contracts.Availability;
using EquipmentRental.Modules.FleetAvailability.Infrastructure;
using EquipmentRental.Modules.FleetAvailability.ReadModel;
using EquipmentRental.Modules.Notifications.Infrastructure;
using EquipmentRental.Modules.Rentals.Application.Availability;
using EquipmentRental.Modules.Rentals.Application.RentalOrders;
using EquipmentRental.Modules.Rentals.Infrastructure;
using EquipmentRental.Modules.Rentals.ProcessManagers;

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] =
            context.HttpContext.TraceIdentifier;
    };
});
builder.Services.AddApiDocumentation();
builder.Services.AddExceptionHandler<RentalOrderNotFoundExceptionHandler>();
builder.Services.AddExceptionHandler<RentalLineNotFoundExceptionHandler>();
builder.Services.AddExceptionHandler<AvailabilityScheduleNotFoundExceptionHandler>();
builder.Services.AddExceptionHandler<AvailabilityScheduleAlreadyExistsExceptionHandler>();
builder.Services.AddExceptionHandler<DomainRuleViolationExceptionHandler>();
builder.Services.AddExceptionHandler<FleetAvailabilityDomainRuleViolationExceptionHandler>();
builder.Services.AddExceptionHandler<OptimisticConcurrencyExceptionHandler>();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddApiSecurity(builder.Configuration);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter =
        PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            var partitionKey =
                context.User.Identity?.Name
                ?? context.Connection.RemoteIpAddress?.ToString()
                ?? "unknown";

            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true,
                });
        });
});
builder.Services.AddHealthChecks()
    .AddCheck<OperationalReadinessHealthCheck>(
        "operational_dependencies",
        tags: ["ready"]);

builder.Services.AddSingleton<
    IIntegrationEventPublisher,
    InProcessIntegrationEventPublisher>();
builder.Services.AddHostedService<OutboxPublisherBackgroundService>();
builder.Services.AddHostedService<RentalConfirmationBackgroundService>();
builder.Services.AddScoped<
    IAvailabilityCommitmentService,
    AvailabilityCommitmentService>();

var databaseConnectionString =
    builder.Configuration.GetConnectionString("Database")
    ?? throw new InvalidOperationException(
        "Connection string 'Database' must be configured.");

builder.Services.AddFleetAvailabilityInfrastructure(databaseConnectionString);
builder.Services.AddAvailabilityCalendarReadModel(databaseConnectionString);
builder.Services.AddRentalsInfrastructure(databaseConnectionString);
builder.Services.AddRentalsProcessManagers(databaseConnectionString);
builder.Services.AddNotificationsInfrastructure(databaseConnectionString);

builder.Services.AddTransient<DefineAvailabilityCapacityHandler>();
builder.Services.AddTransient<GetAvailabilityHandler>();
builder.Services.AddTransient<CreateRentalOrderHandler>();
builder.Services.AddTransient<QuoteRentalOrderHandler>();
builder.Services.AddTransient<RequestAvailabilityForRentalLineHandler>();
builder.Services.AddTransient<ConfirmRentalOrderHandler>();
builder.Services.AddTransient<GetRentalOrderHandler>();

var app = builder.Build();

if (app.Configuration.GetValue<bool>(ApiDocumentation.ConfigurationKey))
{
    app.UseApiDocumentation();
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

string[] activeModules = ["Rentals", "FleetAvailability", "Notifications"];

app.MapGet("/", () => Results.Ok(new
{
    service = "Equipment Rental Platform",
    modules = activeModules,
    status = "healthy",
}))
    .AllowAnonymous()
    .DisableRateLimiting();

app.MapHealthChecks(
        "/health/live",
        new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = HealthResponseWriter.WriteAsync,
        })
    .AllowAnonymous()
    .DisableRateLimiting();
app.MapHealthChecks(
        "/health/ready",
        new HealthCheckOptions
        {
            Predicate = registration =>
                registration.Tags.Contains("ready"),
            ResponseWriter = HealthResponseWriter.WriteAsync,
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] =
                    StatusCodes.Status503ServiceUnavailable,
            },
        })
    .AllowAnonymous()
    .DisableRateLimiting();

app.MapRentalOrderEndpoints();
app.MapFleetAvailabilityEndpoints();
app.MapAvailabilityCalendarEndpoints();

app.Run();

public partial class Program;

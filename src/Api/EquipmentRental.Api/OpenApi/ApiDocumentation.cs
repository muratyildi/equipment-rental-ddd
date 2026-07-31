using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;

using Swashbuckle.AspNetCore.SwaggerGen;

namespace EquipmentRental.Api.OpenApi;

internal static class ApiDocumentation
{
    internal const string ConfigurationKey = "ApiDocumentation:Enabled";

    private const string ApiKeyScheme = "ApiKey";
    private const string DocumentName = "v1";

    internal static IServiceCollection AddApiDocumentation(
        this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(
                DocumentName,
                new OpenApiInfo
                {
                    Title = "Equipment Rental API",
                    Version = DocumentName,
                    Description =
                        "A .NET 10 reference API demonstrating strategic " +
                        "and tactical Domain-Driven Design in a modular monolith.",
                    License = new OpenApiLicense
                    {
                        Name = "MIT",
                        Url = new Uri(
                            "https://github.com/muratyildi/" +
                            "equipment-rental-ddd/blob/main/LICENSE"),
                    },
                });

            options.AddSecurityDefinition(
                ApiKeyScheme,
                new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Header,
                    Name = "X-Api-Key",
                    Description =
                        "Enter a configured read or write API key. " +
                        "Local examples are documented in " +
                        "EquipmentRental.Api.http.",
                });
            options.OperationFilter<ApiKeySecurityOperationFilter>();
        });

        return services;
    }

    internal static WebApplication UseApiDocumentation(
        this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.DocumentTitle = "Equipment Rental API";
            options.SwaggerEndpoint(
                $"/swagger/{DocumentName}/swagger.json",
                "Equipment Rental API v1");
            options.DisplayRequestDuration();
            options.EnablePersistAuthorization();
        });

        return app;
    }

    private sealed class ApiKeySecurityOperationFilter : IOperationFilter
    {
        public void Apply(
            OpenApiOperation operation,
            OperationFilterContext context)
        {
            var endpointMetadata =
                context.ApiDescription.ActionDescriptor.EndpointMetadata;

            var allowsAnonymous = endpointMetadata
                .OfType<IAllowAnonymous>()
                .Any();
            var requiresAuthorization = endpointMetadata
                .OfType<IAuthorizeData>()
                .Any();

            if (allowsAnonymous || !requiresAuthorization)
            {
                return;
            }

            operation.Security =
            [
                new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(
                        ApiKeyScheme,
                        context.Document)] = [],
                },
            ];

            operation.Responses ??= [];
            operation.Responses.TryAdd(
                "401",
                new OpenApiResponse
                {
                    Description = "A valid X-Api-Key header is required.",
                });
            operation.Responses.TryAdd(
                "403",
                new OpenApiResponse
                {
                    Description =
                        "The API key does not include the required scope.",
                });
        }
    }
}

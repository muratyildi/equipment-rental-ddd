using EquipmentRental.Modules.FleetAvailability.Application.Abstractions;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentRental.Api.ErrorHandling;

public sealed class AvailabilityScheduleNotFoundExceptionHandler(
    IProblemDetailsService problemDetailsService)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not AvailabilityScheduleNotFoundException notFound)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Availability schedule not found.",
                Detail = notFound.Message,
                Extensions =
                {
                    ["equipmentCategoryId"] = notFound.EquipmentCategoryId.Value,
                    ["locationId"] = notFound.LocationId.Value,
                },
            },
        });
    }
}

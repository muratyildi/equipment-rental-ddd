using EquipmentRental.Modules.FleetAvailability.Application.Abstractions;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentRental.Api.ErrorHandling;

public sealed class AvailabilityScheduleAlreadyExistsExceptionHandler(
    IProblemDetailsService problemDetailsService)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not AvailabilityScheduleAlreadyExistsException alreadyExists)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Availability schedule already exists.",
                Detail = alreadyExists.Message,
                Extensions =
                {
                    ["equipmentCategoryId"] = alreadyExists.EquipmentCategoryId.Value,
                    ["locationId"] = alreadyExists.LocationId.Value,
                },
            },
        });
    }
}

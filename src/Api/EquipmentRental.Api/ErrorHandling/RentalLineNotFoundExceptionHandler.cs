using EquipmentRental.Modules.Rentals.Application.Abstractions;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentRental.Api.ErrorHandling;

public sealed class RentalLineNotFoundExceptionHandler(
    IProblemDetailsService problemDetailsService)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not RentalLineNotFoundException notFound)
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
                Title = "Rental line not found.",
                Detail = notFound.Message,
                Extensions =
                {
                    ["rentalOrderId"] = notFound.RentalOrderId.Value,
                    ["rentalLineId"] = notFound.RentalLineId.Value,
                },
            },
        });
    }
}

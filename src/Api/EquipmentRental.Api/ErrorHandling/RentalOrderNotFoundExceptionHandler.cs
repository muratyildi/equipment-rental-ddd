using EquipmentRental.Modules.Rentals.Application.Abstractions;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentRental.Api.ErrorHandling;

public sealed class RentalOrderNotFoundExceptionHandler(
    IProblemDetailsService problemDetailsService)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not RentalOrderNotFoundException notFoundException)
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
                Title = "Rental order not found.",
                Detail = notFoundException.Message,
                Type = "https://httpstatuses.com/404",
                Extensions =
                {
                    ["rentalOrderId"] = notFoundException.RentalOrderId.Value,
                },
            },
        });
    }
}

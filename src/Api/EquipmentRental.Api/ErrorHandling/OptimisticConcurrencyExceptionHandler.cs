using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EquipmentRental.Api.ErrorHandling;

public sealed class OptimisticConcurrencyExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DbUpdateConcurrencyException)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;

        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "The resource changed during this operation.",
                Detail = "Reload the latest state and retry the command.",
                Type = "https://httpstatuses.com/409",
                Extensions =
                {
                    ["code"] = "persistence.optimistic_concurrency_conflict",
                    ["traceId"] = httpContext.TraceIdentifier,
                },
            },
            cancellationToken);

        return true;
    }
}

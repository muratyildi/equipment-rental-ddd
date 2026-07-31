using EquipmentRental.Modules.FleetAvailability.Domain.Abstractions;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentRental.Api.ErrorHandling;

public sealed class FleetAvailabilityDomainRuleViolationExceptionHandler(
    IProblemDetailsService problemDetailsService)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DomainRuleViolationException domainException)
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
                Title = "A Fleet Availability business rule was violated.",
                Detail = domainException.Message,
                Type = $"https://httpstatuses.com/409#{domainException.Code}",
                Extensions =
                {
                    ["code"] = domainException.Code,
                },
            },
        });
    }
}

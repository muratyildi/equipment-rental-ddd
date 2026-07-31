using EquipmentRental.Api.Observability;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace EquipmentRental.Api.Tests.Observability;

public sealed class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task ValidCallerCorrelationId_IsReturnedAndUsedAsTraceIdentifier()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] =
            "rental-request-123";
        var middleware = new CorrelationIdMiddleware(
            _ => Task.CompletedTask,
            NullLogger<CorrelationIdMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal("rental-request-123", context.TraceIdentifier);
        Assert.Equal(
            "rental-request-123",
            context.Response.Headers[
                CorrelationIdMiddleware.HeaderName]);
    }

    [Fact]
    public async Task UnsafeCallerCorrelationId_IsReplaced()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] =
            "contains unsafe spaces";
        var middleware = new CorrelationIdMiddleware(
            _ => Task.CompletedTask,
            NullLogger<CorrelationIdMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.NotEqual(
            "contains unsafe spaces",
            context.TraceIdentifier);
        Assert.Matches("^[a-f0-9]{32}$", context.TraceIdentifier);
    }
}

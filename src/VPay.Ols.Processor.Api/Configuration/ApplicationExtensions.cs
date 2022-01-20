using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using VPay.AspNetCore.HealthChecks;

namespace VPay.Ols.Processor.Api.Configuration;

public static class ApplicationExtensions
{
    public static void ConfigureMiddleware(this IApplicationBuilder app)
    {
        app
            .UseHttpLogging()
            .UseProblemDetails()
            .ApplySwaggerMiddleware();
    }

    public static void ConfigureEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapControllers();
        app.MapHealthChecks("/health", new DefaultHealthCheckOptions());
    }
}

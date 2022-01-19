using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Swashbuckle.AspNetCore.SwaggerUI;
using VPay.AspNetCore.HealthChecks;

namespace VPay.Ols.Processor.Api;

public static class ApplicationExtensions
{
    public static void ConfigureMiddleware(this IApplicationBuilder app)
    {
        app
            .UseProblemDetails()
            .UseSwagger()
            .UseSwaggerUI(c =>
            {
                const string Title = "OLS Processor API";
                c.SwaggerEndpoint("/swagger/v1.0/swagger.json", $"{Title} - v1.0");
                c.EnableDeepLinking();
                c.SetupVPayDocumentStyles(Title);
            })
            .UseRouting();
    }

    public static void ConfigureEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapControllers();
        app.MapHealthChecks("/health", new DefaultHealthCheckOptions());
    }
}

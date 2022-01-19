using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace VPay.Ols.Processor.Api.MvcCustomizations;

public class ProblemDetailsOptionsCustomSetup : IConfigureOptions<ProblemDetailsOptions>
{
    public ProblemDetailsOptionsCustomSetup(IWebHostEnvironment environment) =>
        Environment = environment;

    private IWebHostEnvironment Environment { get; }

    public void Configure(ProblemDetailsOptions options)
    {
        options.ValidationProblemStatusCode = StatusCodes.Status400BadRequest;

        options.IncludeExceptionDetails = (_, __) => Environment.IsDevelopment() || Environment.IsEnvironment("LocalDevelopment");

        // This will map NotImplementedException to the 501 Not Implemented status code.
        options.MapToStatusCode<NotImplementedException>(StatusCodes.Status501NotImplemented);
    }
}

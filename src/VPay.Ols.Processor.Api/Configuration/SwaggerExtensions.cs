using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;
using VPay.AspNetCore.SwashBuckle;
using VPay.AspNetCore.SwashBuckle.HealthChecks;
using VPay.Ols.Processor.Api.MvcCustomizations;

namespace VPay.Ols.Processor.Api.Configuration;

/// <summary>
/// Extensions used to configure Swagger.
/// </summary>
public static class SwaggerExtensions
{
    public static IServiceCollection AddSwagger(this IServiceCollection services)
    {
        services
            .AddEndpointsApiExplorer()
            .AddSwaggerGen(c =>
            {
                c.DescribeAllParametersInCamelCase();

                // Names used here are used in URL for SwaggerUI
                c.SwaggerDoc("v1.0", new OpenApiInfo { Title = "OLS Processor API", Version = "v1.0" });

                c.OperationFilter<HttpHeadOperationFilter>();

                // Determine which set of documentation an API should belong to
                c.DocInclusionPredicate((docName, apiDesc) =>
                {
                    ApiVersionModel? actionApiVersionModel = apiDesc.ActionDescriptor?.GetApiVersion();

                    // if no version is specified or API is marked version neutral add to all swagger documents
                    if (actionApiVersionModel?.IsApiVersionNeutral != false)
                    {
                        return true;
                    }

                    if (actionApiVersionModel.DeclaredApiVersions.Count > 0)
                    {
                        return actionApiVersionModel.DeclaredApiVersions.Any(v => $"v{v}" == docName);
                    }

                    return actionApiVersionModel.ImplementedApiVersions.Any(v => $"v{v}" == docName);
                });

                c.AddHealthCheckDocument("/health");
            })
            .AddSwaggerGenNewtonsoftSupport();

        return services;
    }

    public static IApplicationBuilder ApplySwaggerMiddleware(this IApplicationBuilder app)
    {
        return app
            .UseSwagger()
            .UseSwaggerUI(c =>
            {
                const string Title = "OLS Processor API";
                c.SwaggerEndpoint("/swagger/v1.0/swagger.json", $"{Title} - v1.0");
                c.EnableDeepLinking();
                c.SetupVPayDocumentStyles(Title);
            });
    }
}

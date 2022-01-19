using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using VPay.AspNetCore.Mvc;
using VPay.AspNetCore.SwashBuckle;
using VPay.AspNetCore.SwashBuckle.HealthChecks;
using VPay.Extensions.Logging.GrayLog;
using VPay.Ols.Processor.Api.MvcCustomizations;

namespace VPay.Ols.Processor.Api;

public static class ApplicationBuilderExtensions
{
    public static void Configure(this ConfigurationManager configuration)
    {
        configuration
            .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.secure.json", optional: true, reloadOnChange: true);
    }

    public static void Configure(this ILoggingBuilder logging)
    {
        logging.AddVPayGrayLog();
    }

    public static void Configure(this IServiceCollection services)
    {
        services
            .AddHttpContextAccessor()
            .ConfigureOptions<ProblemDetailsOptionsCustomSetup>()
            .AddProblemDetails();

        services.AddControllers(opt =>
        {
            opt.Filters.Add<ProblemDetailsResultAttribute>();

            opt.Conventions.Add(new RouteTokenTransformerConvention(new SlugifyParameterTransformer()));
        })
        .ConfigureApiBehaviorOptions(options =>
            options.InvalidModelStateResponseFactory = context =>
            {
                context.HttpContext.Items.Add("LogInvalidResponse", true);
                return new BadRequestObjectResult(context.ModelState);
            })
        .AddNewtonsoftJson(options =>
        {
            options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
            options.SerializerSettings.NullValueHandling = NullValueHandling.Include;
            options.SerializerSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
        });

        services.AddHealthChecks();

        services.AddApiVersioning(o =>
        {
            o.ReportApiVersions = true;
            o.AssumeDefaultVersionWhenUnspecified = true;
            o.DefaultApiVersion = new ApiVersion(1, 0);
            o.ApiVersionReader = new HeaderApiVersionReader();
        });

        services
            .AddEndpointsApiExplorer()
            .AddSwaggerGen(c =>
            {
                c.DescribeAllParametersInCamelCase();

            //Names used here are used in URL for SwaggerUI
            c.SwaggerDoc("v1.0", new OpenApiInfo { Title = "OLS Processor API", Version = "v1.0" });

                c.OperationFilter<HttpHeadOperationFilter>();

            //Determine which set of documentation an API should belong to
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
    }
}

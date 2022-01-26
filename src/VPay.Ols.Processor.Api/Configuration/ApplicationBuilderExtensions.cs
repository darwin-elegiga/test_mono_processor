using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VPay.AspNetCore.Mvc;
using VPay.Extensions.Logging.GrayLog;
using VPay.Ols.Processor.Api.MvcCustomizations;
using VPay.Ols.Processor.Data.Sql.DependencyInjection;
using VPay.Ols.Processor.Data.Sql.Health;

namespace VPay.Ols.Processor.Api.Configuration;

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

    public static void Configure(this IServiceCollection services, IConfiguration configuration)
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
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                context.HttpContext.Items.Add("LogInvalidResponse", true);
                return new BadRequestObjectResult(context.ModelState);
            };
        })
        .AddNewtonsoftJson(options =>
        {
            options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
            options.SerializerSettings.NullValueHandling = NullValueHandling.Include;
            options.SerializerSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
        });

        services
            .AddHealthChecks()
            .AddCheck<SqlServerSystemHealthCheck>("ols-sqlserver");

        services.AddApiVersioning(o =>
        {
            o.ReportApiVersions = true;
            o.AssumeDefaultVersionWhenUnspecified = true;
            o.DefaultApiVersion = new ApiVersion(1, 0);
            o.ApiVersionReader = new HeaderApiVersionReader();
        });

        services.AddSwagger();

        services.AddSql(config => configuration.Bind("OlsDatabase", config));
    }
}

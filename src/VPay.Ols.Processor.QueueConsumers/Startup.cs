using MassTransit;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VPay.MassTransit.Abstractions;
using VPay.MassTransit.DependencyInjection;
using VPay.Ols.Processor.Commands.OlsFile;
using VPay.Ols.Processor.Data.Sql.DependencyInjection;
using VPay.Ols.Processor.QueueConsumers.Consumers;

namespace VPay.Ols.Processor.QueueConsumers;

public static class Startup
{
    public static void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
    {
        services.AddMediatR(typeof(AddOlsFile.Handler).Assembly);

        var rabbitConfig = new RabbitMqConfig();
        hostContext.Configuration.Bind("OlsProcessorQueue", rabbitConfig);

        services.UseMassTransit(rabbitConfig, opts =>
        {
            opts.AddConsumer<SampleMessageConsumer>();
            // todo: Add consumers here
        });

        services.AddMassTransitHostedService();

        services.AddSql(config => hostContext.Configuration.Bind("OlsDatabase", config));
    }
}

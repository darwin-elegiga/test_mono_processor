using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VPay.MassTransit.Abstractions;
using VPay.MassTransit.DependencyInjection;
using VPay.Ols.Processor.QueueConsumers.Consumers;

namespace VPay.Ols.Processor.QueueConsumers;

public static class Startup
{
    public static void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
    {
        var rabbitConfig = new RabbitMqConfig();
        hostContext.Configuration.Bind("OlsProcessorQueue", rabbitConfig);

        services.UseMassTransit(rabbitConfig, opts =>
        {
            opts.AddConsumer<SampleMessageConsumer>();
            // todo: Add consumers here
        });

        services.AddMassTransitHostedService();
    }
}

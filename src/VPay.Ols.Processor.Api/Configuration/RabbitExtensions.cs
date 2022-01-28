using MassTransit;
using MassTransit.RabbitMqTransport;
using VPay.MassTransit.Abstractions;

namespace VPay.Ols.Processor.Api.Configuration;

public static class RabbitExtensions
{
    public static void Host(this IRabbitMqBusFactoryConfigurator configurator, RabbitMqConfig config)
    {
        configurator.Host(config.Hostnames[0], (ushort)config.Port, config.VirtualHost, hst =>
        {
            hst.Heartbeat(config.Heartbeat);

            hst.Username(config.Username);
            hst.Password(config.Password);

            hst.UseCluster(clusterConfig =>
            {
                foreach (var node in config.Hostnames)
                {
                    clusterConfig.Node(node);
                }
            });
        });
    }
}

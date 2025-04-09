using MassTransit.ConsumeConfigurators;
using MassTransit.Definition;
using MassTransit.RabbitMqTransport;
using MassTransit;

namespace VPay.Ols.Processor.QueueConsumers.Consumers;

public class AuthorizationFileConsumerDefinition : ConsumerDefinition<AuthorizationFileConsumer>
{
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<AuthorizationFileConsumer> consumerConfigurator)
    {
        if (endpointConfigurator is IRabbitMqReceiveEndpointConfigurator rabbit)
        {
            rabbit.SetQuorumQueue(3);
        }

        base.ConfigureConsumer(endpointConfigurator, consumerConfigurator);
    }
}

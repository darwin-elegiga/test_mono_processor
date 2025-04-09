using MassTransit.ConsumeConfigurators;
using MassTransit.Definition;
using MassTransit.RabbitMqTransport;
using MassTransit;

namespace VPay.Ols.Processor.QueueConsumers.Consumers;

public class PostedTransactionFileProcessedConsumerDefinition : ConsumerDefinition<PostedTransactionFileProcessedConsumer>
{
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<PostedTransactionFileProcessedConsumer> consumerConfigurator)
    {
        if (endpointConfigurator is IRabbitMqReceiveEndpointConfigurator rabbit)
        {
            rabbit.SetQuorumQueue(3);
        }

        base.ConfigureConsumer(endpointConfigurator, consumerConfigurator);
    }
}

using MassTransit.ConsumeConfigurators;
using MassTransit.Definition;
using MassTransit.RabbitMqTransport;
using MassTransit;

namespace VPay.Ols.Processor.QueueConsumers.Consumers;

public class NonFinancialFileProcessedConsumerDefinition : ConsumerDefinition<NonFinancialFileProcessedConsumer>
{
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<NonFinancialFileProcessedConsumer> consumerConfigurator)
    {
        if (endpointConfigurator is IRabbitMqReceiveEndpointConfigurator rabbit)
        {
            rabbit.SetQuorumQueue(3);
        }

        base.ConfigureConsumer(endpointConfigurator, consumerConfigurator);
    }
}

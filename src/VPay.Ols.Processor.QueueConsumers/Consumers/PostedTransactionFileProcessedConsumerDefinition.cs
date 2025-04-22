using MassTransit.ConsumeConfigurators;
using MassTransit.Definition;
using MassTransit.RabbitMqTransport;
using MassTransit;
using VPay.MassTransit.RabbitMqTransport;

namespace VPay.Ols.Processor.QueueConsumers.Consumers;

public class PostedTransactionFileProcessedConsumerDefinition : QuorumConsumerDefinition<PostedTransactionFileProcessedConsumer>
{
}

using System;
using GreenPipes;
using MassTransit;
using MassTransit.ConsumeConfigurators;
using MassTransit.Definition;

namespace VPay.Ols.Processor.QueueConsumers.Consumers.NonFinancialFile;

public sealed class NonFinancialFileConsumerDefinition : ConsumerDefinition<NonFinancialFileConsumer>
{
    private readonly int _retryLimit;
    private readonly int _initialIntervalInSeconds;
    private readonly int _intervalIncrementInSeconds;

    public NonFinancialFileConsumerDefinition(NonFinancialFileConsumerSettings settings)
    {
        EndpointName = settings.QueueName;

        _retryLimit = settings.ConsumerRetrySettings.RetryLimit;
        _initialIntervalInSeconds = settings.ConsumerRetrySettings.InitialIntervalInSeconds;
        _intervalIncrementInSeconds = settings.ConsumerRetrySettings.IntervalIncrementInSeconds;
    }

    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<NonFinancialFileConsumer> consumerConfigurator)
    {
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Incremental(_retryLimit, TimeSpan.FromSeconds(_initialIntervalInSeconds), TimeSpan.FromSeconds(_intervalIncrementInSeconds));
            r.Handle<Exception>();
        });

        base.ConfigureConsumer(endpointConfigurator, consumerConfigurator);
    }
}

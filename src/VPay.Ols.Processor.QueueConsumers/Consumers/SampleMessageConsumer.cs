using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;
using VPay.Ols.Processor.Messages;

namespace VPay.Ols.Processor.QueueConsumers.Consumers;

public class SampleMessageConsumer : IConsumer<SampleMessage>
{
    private readonly ILogger<SampleMessageConsumer> _logger;

    public SampleMessageConsumer(ILogger<SampleMessageConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<SampleMessage> context)
    {
        _logger.LogInformation($"Receieved message {context.Message.TextContent}");

        return Task.CompletedTask;
    }
}

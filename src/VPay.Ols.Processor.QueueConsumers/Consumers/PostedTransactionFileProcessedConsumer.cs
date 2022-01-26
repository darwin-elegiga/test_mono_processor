using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Ols.Processor.Commands.OlsFile;
using VPay.Ols.Processor.Messages;

namespace VPay.Ols.Processor.QueueConsumers.Consumers;

public class PostedTransactionFileProcessedConsumer : IConsumer<PostedTransactionFileProcessed>
{
    private readonly ILogger<PostedTransactionFileProcessedConsumer> _logger;
    private readonly IMediator _mediator;

    public PostedTransactionFileProcessedConsumer(ILogger<PostedTransactionFileProcessedConsumer> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    public async Task Consume(ConsumeContext<PostedTransactionFileProcessed> context)
    {
        using (_logger.BeginScope(new Dictionary<string, object> { ["FileName"] = context.Message.Filename }))
        {
            try
            {
                var result = await _mediator.Send(new ProcessPostedTransactions.Command(context.Message.Filename)).ConfigureAwait(false);

                if (!result.Success)
                {
                    _logger.LogError("Unable to process the posted transactions file.", result.Error);
                }
                else
                {
                    _logger.LogInformation("Posted transactions file processed successfully.");
                }
            }
            catch(Exception ex)
            {                
                _logger.LogError(ex, "An error occurred processing the posted transactions file.");
            }
            finally
            {
                // Clean-up? Archive?
            }
        }
    }
}

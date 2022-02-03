using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading.Tasks;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Ols.Processor.Commands.OlsFile;
using VPay.Ols.Processor.Messages;
using VPay.Ols.Processor.Models.PostedTransactions;

namespace VPay.Ols.Processor.QueueConsumers.Consumers;

public class PostedTransactionFileProcessedConsumer : IConsumer<PostedTransactionFileProcessed>
{
    private readonly ILogger<PostedTransactionFileProcessedConsumer> _logger;
    private readonly IMediator _mediator;
    private readonly PostedTransactionsFileSettings _postedTransactionsSettings;
    private readonly IFileSystem _fileSystem;

    public PostedTransactionFileProcessedConsumer(ILogger<PostedTransactionFileProcessedConsumer> logger, IMediator mediator, IFileSystem fileSystem, PostedTransactionsFileSettings postedTransactionsSettings)
    {
        _logger = logger;
        _mediator = mediator;
        _postedTransactionsSettings = postedTransactionsSettings;
        _fileSystem = fileSystem;
    }

    public async Task Consume(ConsumeContext<PostedTransactionFileProcessed> context)
    {
        using (_logger.BeginScope(new Dictionary<string, object> { ["FileName"] = context.Message.Filename }))
        {
            var filePath = _fileSystem.Path.Combine(_postedTransactionsSettings.WorkingDirectory, context.Message.Filename);

            try
            {
                var result = await _mediator.Send(new ProcessPostedTransactions.Command(filePath)).ConfigureAwait(false);

                if (!result.Success)
                {
                    _logger.LogError("Unable to process the posted transactions file. {Error}", result.Error);
                }
                else
                {
                    _logger.LogInformation("Posted transactions file processed successfully.");
                }
            }
            catch (Exception ex)
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

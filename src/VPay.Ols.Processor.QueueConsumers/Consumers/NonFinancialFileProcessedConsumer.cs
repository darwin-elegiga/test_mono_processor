using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading.Tasks;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Ols.Processor.Commands.OlsFile;
using VPay.Ols.Processor.Messages;
using VPay.Ols.Processor.Models;
using VPay.Ols.Processor.Models.NonFinancial;

namespace VPay.Ols.Processor.QueueConsumers.Consumers;

public class NonFinancialFileProcessedConsumer : IConsumer<NonFinancialFileProcessed>
{
    private readonly NonFinancialFileSettings _settings;
    private readonly ILogger<NonFinancialFileProcessedConsumer> _logger;
    private readonly IMediator _mediator;
    private readonly IFileSystem _fs;

    public NonFinancialFileProcessedConsumer(
        ILogger<NonFinancialFileProcessedConsumer> logger,
        NonFinancialFileSettings settings,
        IMediator mediator,
        IFileSystem fs)
    {
        _logger = logger;
        _mediator = mediator;
        _fs = fs;
        _settings = settings;
    }

    public async Task Consume(ConsumeContext<NonFinancialFileProcessed> context)
    {
        var fileName = context.Message.FileName;
        using (_logger.BeginScope(new Dictionary<string, string>()
        {
            ["FileType"] = OlsFileType.NonFinancial.ToString(),
            ["FileName"] = fileName,
        }))
        {
            var filePath = _fs.Path.Combine(_settings.WorkingDirectory, fileName);

            try
            {
                var proccessResult = await _mediator.Send(new ProcessNonFinancial.Command(filePath)).ConfigureAwait(false);

                if (proccessResult.Success)
                {
                    _logger.LogInformation("Non-financial file {FileName} has processed successfully.", fileName);
                }
                else
                {
                    _logger.LogError("Non-financial file {FileName} processing has failed.", fileName);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Non-financial file {FileName} processing has failed due to an error.", fileName);
            }
        }
    }
}

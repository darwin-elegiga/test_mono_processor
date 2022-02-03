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
using VPay.Ols.Processor.Models.Authorization;

namespace VPay.Ols.Processor.QueueConsumers.Consumers;

public sealed class AuthorizationFileConsumer : IConsumer<AuthorizationMessage>
{
    private readonly ILogger<AuthorizationFileConsumer> _logger;
    private readonly IMediator _mediator;
    private readonly AuthorizationFileSettings _authorizationSettings;
    private readonly IFileSystem _fileSystem;

    public AuthorizationFileConsumer(ILogger<AuthorizationFileConsumer> logger, IMediator mediator, IFileSystem fileSystem, AuthorizationFileSettings authorizationSettings)
    {
        _logger = logger;
        _mediator = mediator;
        _authorizationSettings = authorizationSettings;
        _fileSystem = fileSystem;
    }

    public async Task Consume(ConsumeContext<AuthorizationMessage> context)
    {
        using (_logger.BeginScope(new Dictionary<string, object> { ["FileName"] = context.Message.FileName }))
        {
            string filePath = _fileSystem.Path.Combine(_authorizationSettings.WorkingDirectory, context.Message.FileName);

            try
            {
                var command = new ProcessAuthorizations.Command(filePath);
                Result result = await _mediator.Send(command).ConfigureAwait(false);

                if (result.Success)
                {
                    _logger.LogInformation("Authorization file processed successfully.");
                }
                else
                {
                    _logger.LogError("Unable to process the authorization file.", result.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred processing the authorization file.");
            }
        }
    }
}

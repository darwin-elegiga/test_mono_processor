using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VPay.FileTransfer.Messaging;
using VPay.FileTransfer.Messaging.Abstractions;

namespace VPay.Ols.Processor.QueueConsumers.FileTransfer;

internal class FileReadyForTransferNotificationHandler : MassTransitFileReadyForTransferNotificationHandlerBase
{
    public FileReadyForTransferNotificationHandler(
        IFileTransferServiceBus fileTransferServiceBus,
        ILogger<FileReadyForTransferNotificationHandler> logger)
        : base(fileTransferServiceBus, logger)
    {
    }

    protected override Task UpdateFileStatus(FileReadyForTransferNotification notification, CancellationToken cancellationToken)
    {
        // NOTE: Intentionally left blank for now...
        return Task.CompletedTask;
    }
}

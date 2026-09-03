using System;
using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Options;
using VPay.FileTransfer.Messaging;
using VPay.FileTransfer.Messaging.Abstractions;
using VPay.Ols.Processor.Models;

namespace VPay.Ols.Processor.Commands;

public static class SendToFileTransferService
{
    public record Command(IFileInfo FileInfo) : IRequest;

    public class Handler : IRequestHandler<Command>
    {
        private readonly IFileSystem _fileSystem;
        private readonly ISigningService _signingService;
        private readonly IHashingService<SHA256CryptoServiceProvider> _hashingService;
        private readonly IOptions<SigningServiceOptions> _signingServiceOptions;
        private readonly FileTransferServiceSettings _fileTransferServiceSettings;
        private readonly IFileReadyForTransferNotificationPublisher _fileReadyForTransferNotificationPublisher;

        public Handler(
            IFileSystem fileSystem,
            ISigningService signingService,
            IHashingService<SHA256CryptoServiceProvider> hashingService,
            IOptions<SigningServiceOptions> signingServiceOptions,
            FileTransferServiceSettings fileTransferServiceSettings,
            IFileReadyForTransferNotificationPublisher fileReadyForTransferNotificationPublisher)
        {
            _fileSystem = fileSystem;
            _signingService = signingService;
            _hashingService = hashingService;
            _signingServiceOptions = signingServiceOptions;
            _fileTransferServiceSettings = fileTransferServiceSettings;
            _fileReadyForTransferNotificationPublisher = fileReadyForTransferNotificationPublisher;
        }

        public async Task Handle(Command request, CancellationToken cancellationToken)
        {
            var localFileInfo = CopyFileLocally(request.FileInfo);

            try
            {
                // copy the file to the file transfer service directory
                var transferServiceFileInfo = CopyFileToTransferServiceLocation(localFileInfo);

                // get the path to the file relative to the base directory
                var fileLocation = _fileSystem.Path.GetRelativePath(_fileTransferServiceSettings.FileTransferServiceFolderPath, transferServiceFileInfo.FullName);

                // calculate the hash for the file and sign it
                await using var fileStream = localFileInfo.OpenRead();
                var hash = _hashingService.ComputeHash(fileStream);

                var signedHash = await _signingService.Sign(_signingServiceOptions.Value.PrivateKeyPath, hash, cancellationToken).ConfigureAwait(false);

                // fire the notification so we can perform additional actions for the file
                await _fileReadyForTransferNotificationPublisher.Publish(
                    new FileReadyForTransferNotification("OPTUM_OLS", "OPTUM", fileLocation, signedHash),
                    cancellationToken
                ).ConfigureAwait(false);
            }
            finally
            {
                localFileInfo.Delete();
            }
        }

        private IFileInfo CopyFileLocally(IFileInfo fileInfo)
        {
            var valueToReturn = fileInfo;
            var local = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), fileInfo.Name);
            if (!_fileSystem.File.Exists(local))
            {
                valueToReturn = fileInfo.CopyTo(local);
            }

            return valueToReturn;
        }

        private IFileInfo CopyFileToTransferServiceLocation(IFileInfo fileInfo)
        {
            var directory = CreateDatedDirectory(_fileTransferServiceSettings.FileTransferServiceFolderPath, _fileTransferServiceSettings.FileTransferServiceFolderSubPath);
            var actualPath = _fileSystem.Path.Combine(directory, fileInfo.Name);
            return fileInfo.CopyTo(actualPath, overwrite: true);
        }

        private string CreateDatedDirectory(string basePath, string? subPath = null)
        {
            var date = DateTime.Today;
            var path = _fileSystem.Path.Combine(basePath, subPath ?? string.Empty, date.Year.ToString(), date.Month.ToString("00"), date.Day.ToString("00"));
            if (!_fileSystem.Directory.Exists(path))
            {
                _fileSystem.Directory.CreateDirectory(path);
            }

            return path;
        }
    }
}

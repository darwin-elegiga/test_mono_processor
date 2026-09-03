using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Options;
using Moq;
using VPay.FileTransfer.Messaging;
using VPay.FileTransfer.Messaging.Abstractions;
using VPay.Ols.Processor.Commands;
using VPay.Ols.Processor.Models;
using Xunit;

namespace VPay.Ols.Processor.Tests.Commands;

/// <summary>
/// Tests for the <see cref="SendToFileTransferService"/>.
/// </summary>
public class SendToFileTransferServiceTests : IDisposable
{
    private const string FileTransferPath = "/SRVFS/optum/file.txt";
    private const string PrivateKeyPath = "key.txt";
    private readonly MockFileSystem _fileSystem;
    private readonly Mock<ISigningService> _signingServiceMock = new(MockBehavior.Strict);
    private readonly Mock<IHashingService<SHA256CryptoServiceProvider>> _hashingServiceMock = new(MockBehavior.Strict);
    private readonly Mock<IFileReadyForTransferNotificationPublisher> _notificationPublisherMock = new(MockBehavior.Strict);
    private readonly SendToFileTransferService.Handler _handler;
    private readonly FileTransferServiceSettings _transferSettings;

    public SendToFileTransferServiceTests()
    {
        var signingOptions = Options.Create(new SigningServiceOptions
        {
            PrivateKeyPath = PrivateKeyPath
        });

        _transferSettings = new FileTransferServiceSettings
        {
            FileTransferServiceFolderPath = "/SRVFS/filetransfer",
            FileTransferServiceFolderSubPath = "optum_ols"
        };

        _fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [PrivateKeyPath] = new("key file contents..."),
            [FileTransferPath] = new("file contents...")
        });

        _handler = new SendToFileTransferService.Handler(
            _fileSystem,
            _signingServiceMock.Object,
            _hashingServiceMock.Object,
            signingOptions,
            _transferSettings,
            _notificationPublisherMock.Object);
    }

    public void Dispose()
    {
        _signingServiceMock.VerifyAll();
        _hashingServiceMock.VerifyAll();
        _notificationPublisherMock.VerifyAll();
    }

    [Fact]
    public async Task Handle_WhenFileReadyForTransfer_ShouldSendTransferNotification()
    {
        const string HashedData = "Hashed Data...";
        _hashingServiceMock
            .Setup(x => x.ComputeHash(It.IsAny<Stream>()))
            .Returns(HashedData)
            .Verifiable();

        const string SignedHash = "signed contents...";
        _signingServiceMock
            .Setup(x => x.Sign("key.txt", HashedData, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SignedHash)
            .Verifiable();

        string filePath = "";
        _notificationPublisherMock
            .Setup(x => x.Publish(It.Is<FileReadyForTransferNotification>(q =>
                    q.Source == "OPTUM_OLS"
                    && q.Destination == "OPTUM"
                    && q.SignedFileHash == SignedHash),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback((FileReadyForTransferNotification notification, CancellationToken _) =>
            {
                filePath = notification.FileName;
            })
            .Verifiable();

        var command = new SendToFileTransferService.Command(new MockFileInfo(_fileSystem, FileTransferPath));
        await _handler.Handle(command, default);

        using (new AssertionScope())
        {
            var expectedPath = _fileSystem.Path.Combine(
                _transferSettings.FileTransferServiceFolderPath,
                _transferSettings.FileTransferServiceFolderSubPath,
                DateTime.Now.Year.ToString(),
                DateTime.Now.Month.ToString("00"),
                DateTime.Now.Day.ToString("00"),
                _fileSystem.Path.GetFileName(FileTransferPath));
            _fileSystem.FileExists(expectedPath).Should().BeTrue($"because the file should have been moved to {expectedPath}, but was {filePath}");
        }
    }
}

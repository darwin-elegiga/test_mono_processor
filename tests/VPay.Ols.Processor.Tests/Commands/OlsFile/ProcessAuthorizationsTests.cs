using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VPay.Extensions.Testing.Logging;
using VPay.Ols.Processor.Commands.OlsFile;
using VPay.Ols.Processor.Hashing;
using VPay.Ols.Processor.Models;
using VPay.Ols.Processor.Models.Authorization;
using VPay.Ols.Processor.Parsers;
using VPay.Ols.Processor.Queries;
using VPay.Ols.Processor.Writers;
using Xunit;

namespace VPay.Ols.Processor.Tests.Commands.OlsFile;

/// <summary>
/// Tests for <see cref="ProcessAuthorizations.Handler"/>
/// </summary>
public class ProcessAuthorizationsTests
{
    private readonly Mock<IFileSystem> _fileSystem;
    private readonly Mock<IAuthorizationParser> _parser;
    private readonly Mock<IHashingService<SHA256CryptoServiceProvider>> _hashingService;
    private readonly Mock<IMediator> _mediator;
    private readonly Mock<IAuthorizationFileWriter> _writer;
    private readonly LoggerMock<ProcessAuthorizations.Handler> _logger;
    private readonly AuthorizationFileSettings _settings;

    private readonly ProcessAuthorizations.Handler _handler;

    public ProcessAuthorizationsTests()
    {
        _fileSystem = new Mock<IFileSystem>();
        _parser = new Mock<IAuthorizationParser>();
        _hashingService = new Mock<IHashingService<SHA256CryptoServiceProvider>>();
        _mediator = new Mock<IMediator>(MockBehavior.Strict);
        _writer = new Mock<IAuthorizationFileWriter>();
        _logger = LoggerMock<ProcessAuthorizations.Handler>.CreateDefault(); ;
        _settings = new AuthorizationFileSettings
        {
            OutputDirectory = "test_output",
            WorkingDirectory = "test_working"
        };

        _handler = new ProcessAuthorizations.Handler(_fileSystem.Object, _parser.Object, _hashingService.Object, _mediator.Object, _writer.Object, _logger.Object, _settings);
    }

    [Fact]
    public async Task WithUnreadableFile_ReturnsFailure()
    {
        // arrange
        var command = new ProcessAuthorizations.Command("files/test-file.txt");

        _fileSystem.Setup(x => x.Path.GetFileName(It.Is<string>(s => s == command.FilePath))).Returns("test-file.txt");

        var ex = new Exception("Test file exception");
        _fileSystem.Setup(x => x.File.OpenText(It.IsAny<string>())).Throws(ex);

        // act
        var result = await _handler.Handle(command, default).ConfigureAwait(false);

        // assert
        result.Should().BeEquivalentTo(Result.Fail($"Unable to read authorization file. {ex.Message}"));
    }

    #region WithGoodFile_WritesOptumFile_ReturnsOk

    [Fact]
    public async Task WithGoodFile_WritesOptumFile_ReturnsOk()
    {
        // arrange
        var command = new ProcessAuthorizations.Command("files/test-file.txt");

        _fileSystem.Setup(x => x.Path.GetFileName(It.Is<string>(s => s == command.FilePath))).Returns("test-file.txt");

        var fileStream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes("TEST")));
        _fileSystem.Setup(x => x.File.OpenText(It.Is<string>(s => s == command.FilePath))).Returns(fileStream);
        _fileSystem.Setup(x => x.Path.Combine(_settings.OutputDirectory, It.IsAny<string>())).Returns("optum-file.txt");
        _fileSystem.Setup(x => x.Directory.CreateDirectory(It.IsAny<string>()));
        _fileSystem.Setup(x => x.File.WriteAllTextAsync(It.Is<string>(s => s == "optum-file.txt"), It.Is<string>(s => s == "TEST_OUTPUT_STRING"), default));

        AuthorizationFile originalFile = BuildOriginalFile();
        _parser.Setup(x => x.ParseFile(It.IsAny<StreamReader>())).Returns(originalFile);

        _hashingService.Setup(x => x.ComputeHash(It.IsAny<Stream>())).Returns("TEST_HASH");

        var queryCaptor = new ArgumentCaptor<GetClientForTransactions.Query>();
        var expectedQueryList = new List<TransactionIdLookup>
        {
            new TransactionIdLookup(1),
            new TransactionIdLookup(2),
            new TransactionIdLookup(3)
        };

        _mediator.Setup(x => x.Send(queryCaptor.Capture(), default)).ReturnsAsync(new List<TransactionClient> {
            new TransactionClient
            {
                TransactionId = 1,
                ClientCode = "ABC"
            },
            new TransactionClient
            {
                TransactionId = 2,
                ClientCode = "DEF"
            }
        });

        var writeFileCaptor = new ArgumentCaptor<AuthorizationFile>();
        _writer.Setup(x => x.WriteAuthorizationFile(writeFileCaptor.Capture())).Returns("TEST_OUTPUT_STRING");

        var addFileCommandCaptor = new ArgumentCaptor<AddOlsFile.Command>();
        _mediator.Setup(x => x.Send(addFileCommandCaptor.Capture(), default)).ReturnsAsync(Result.Ok());

        var expectedAddFileCommand = new AddOlsFile.Command("test-file.txt", "TEST_HASH", OlsFileType.Authorized);
        AuthorizationFile expectedOutput = BuildExpectedFile();

        // act
        Result result = await _handler.Handle(command, default).ConfigureAwait(false);

        // assert
        using(new AssertionScope())
        {
            result.Should().BeEquivalentTo(Result.Ok());

            addFileCommandCaptor.Value.Should().BeEquivalentTo(expectedAddFileCommand);
            queryCaptor.Value.TransactionIdList.Should().BeEquivalentTo(expectedQueryList);
            writeFileCaptor.Value.Header.Should().BeEquivalentTo(expectedOutput.Header);
            writeFileCaptor.Value.Details.Should().BeEquivalentTo(expectedOutput.Details, opt => opt.Excluding(m => m.SelectedMemberPath.EndsWith("FileName")));
            writeFileCaptor.Value.Trailer.Should().BeEquivalentTo(expectedOutput.Trailer);

            _logger.VerifyMessageWasLogged("Row 3 with SE External Id 3 did not match any known transaction.", LogLevel.Warning);
        }
    }

    private static AuthorizationFile BuildOriginalFile()
    {
        var header = new AuthorizationHeader
        {
            RecordName = "abcd",
            ProcessorName = "bcde",
            ReportName = "cdef",
            FileDate = new DateOnly(1234, 2, 3),
            RunBeginDate = new DateOnly(2345, 3, 4),
            RunEndDate = new DateOnly(3456, 4, 5)
        };

        var details = new List<AuthorizationDetail>
        {
            new AuthorizationDetail
            {
                LineNumber = 1,
                CardNumber = "1234567890",
                TransactionDateTime = new DateTime(2345, 3, 4, 5, 6, 7),
                TransactionCurrencyCode = 5678,
                AddressVerificationResponse = "abcd",
                AuthorizationResponse = "bcde",
                AuthorizationAmount = 67.89m,
                AuthorizationCode = "cdef",
                NetworkCode = "defg",
                MerchantNumber = "efgh",
                MerchantName = "fghi",
                MerchantCategoryCode = "ghij",
                MerchantCountryCode = "hijk",
                SEExternalId = 1,
                Bin = 546893
            },
            new AuthorizationDetail
            {
                LineNumber = 2,
                CardNumber = "2345678901",
                TransactionDateTime = new DateTime(2345, 3, 4, 5, 6, 7),
                TransactionCurrencyCode = 5678,
                AddressVerificationResponse = "abcd",
                AuthorizationResponse = "bcde",
                AuthorizationAmount = 67.89m,
                AuthorizationCode = "cdef",
                NetworkCode = "defg",
                MerchantNumber = "efgh",
                MerchantName = "fghi",
                MerchantCategoryCode = "ghij",
                MerchantCountryCode = "hijk",
                SEExternalId = 2,
                Bin = 532086
            },
            new AuthorizationDetail
            {
                LineNumber = 3,
                CardNumber = "3456789012",
                TransactionDateTime = new DateTime(2345, 3, 4, 5, 6, 7),
                TransactionCurrencyCode = 5678,
                AddressVerificationResponse = "abcd",
                AuthorizationResponse = "bcde",
                AuthorizationAmount = 67.89m,
                AuthorizationCode = "cdef",
                NetworkCode = "defg",
                MerchantNumber = "efgh",
                MerchantName = "fghi",
                MerchantCategoryCode = "ghij",
                MerchantCountryCode = "hijk",
                SEExternalId = 3,
                Bin = 528972
            },
            new AuthorizationDetail
            {
                LineNumber = 4,
                CardNumber = "4567890123",
                TransactionDateTime = new DateTime(2345, 3, 4, 5, 6, 7),
                TransactionCurrencyCode = 5678,
                AddressVerificationResponse = "abcd",
                AuthorizationResponse = "bcde",
                AuthorizationAmount = 67.89m,
                AuthorizationCode = "cdef",
                NetworkCode = "defg",
                MerchantNumber = "efgh",
                MerchantName = "fghi",
                MerchantCategoryCode = "ghij",
                MerchantCountryCode = "hijk",
                SEExternalId = 4,
                Bin = 123456
            }
        };

        var trailer = new AuthorizationTrailer
        {
            RecordName = "abcd",
            Count = 1234
        };

        return new AuthorizationFile(header, details, trailer);
    }

    private static AuthorizationFile BuildExpectedFile()
    {
        var header = new AuthorizationHeader
        {
            RecordName = "HEADER",
            ProcessorName = "VPAY, INC",
            ReportName = "AUTHORIZED",
            FileDate = new DateOnly(1234, 2, 3),
            RunBeginDate = new DateOnly(2345, 3, 4),
            RunEndDate = new DateOnly(3456, 4, 5)
        };

        var details = new List<AuthorizationDetail>
        {
            new AuthorizationDetail
            {
                LineNumber = 1,
                CardNumber = "123456XXXXXX7890",
                TransactionDateTime = new DateTime(2345, 3, 4, 5, 6, 7),
                TransactionCurrencyCode = 5678,
                AddressVerificationResponse = "abcd",
                AuthorizationResponse = "bcde",
                AuthorizationAmount = 67.89m,
                AuthorizationCode = "cdef",
                NetworkCode = "defg",
                MerchantNumber = "efgh",
                MerchantName = "fghi",
                MerchantCategoryCode = "ghij",
                MerchantCountryCode = "hijk",
                SEExternalId = 1,
                Bin = 546893,
                ClientCode = "ABC"
            },
            new AuthorizationDetail
            {
                LineNumber = 2,
                CardNumber = "234567XXXXXX8901",
                TransactionDateTime = new DateTime(2345, 3, 4, 5, 6, 7),
                TransactionCurrencyCode = 5678,
                AddressVerificationResponse = "abcd",
                AuthorizationResponse = "bcde",
                AuthorizationAmount = 67.89m,
                AuthorizationCode = "cdef",
                NetworkCode = "defg",
                MerchantNumber = "efgh",
                MerchantName = "fghi",
                MerchantCategoryCode = "ghij",
                MerchantCountryCode = "hijk",
                SEExternalId = 2,
                Bin = 532086,
                ClientCode = "DEF"
            },
            new AuthorizationDetail
            {
                LineNumber = 3,
                CardNumber = "345678XXXXXX9012",
                TransactionDateTime = new DateTime(2345, 3, 4, 5, 6, 7),
                TransactionCurrencyCode = 5678,
                AddressVerificationResponse = "abcd",
                AuthorizationResponse = "bcde",
                AuthorizationAmount = 67.89m,
                AuthorizationCode = "cdef",
                NetworkCode = "defg",
                MerchantNumber = "efgh",
                MerchantName = "fghi",
                MerchantCategoryCode = "ghij",
                MerchantCountryCode = "hijk",
                SEExternalId = 3,
                Bin = 528972
            }
        };

        var trailer = new AuthorizationTrailer
        {
            RecordName = "TRAILER",
            Count = 3
        };

        return new AuthorizationFile(header, details, trailer);
    }

    #endregion WithGoodFile_WritesOptumFile_ReturnsOk
}

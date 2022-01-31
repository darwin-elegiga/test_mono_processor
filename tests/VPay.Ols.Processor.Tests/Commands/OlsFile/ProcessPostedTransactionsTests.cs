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
using VPay.Ols.Processor.Commands.OlsFile;
using VPay.Ols.Processor.Hashing;
using VPay.Ols.Processor.Models;
using VPay.Ols.Processor.Models.PostedTransactions;
using VPay.Ols.Processor.Parsers;
using VPay.Ols.Processor.Queries;
using VPay.Ols.Processor.Writers;
using Xunit;

namespace VPay.Ols.Processor.Tests.Commands.OlsFile;

/// <summary>
/// Tests for <see cref="ProcessPostedTransactions.Handler"/>
/// </summary>
public class ProcessPostedTransactionsTests
{
    private readonly Mock<IFileSystem> _fileSystem;
    private readonly Mock<IPostedTransactionsParser> _parser;
    private readonly Mock<IHashingService<SHA256CryptoServiceProvider>> _hashingService;
    private readonly Mock<IMediator> _mediator;
    private readonly Mock<IPostedTransactionFileWriter> _writer;
    private readonly ILogger<ProcessPostedTransactions.Handler> _logger = new NullLogger<ProcessPostedTransactions.Handler>();
    private readonly PostedTransactionsFileSettings _settings;

    private readonly ProcessPostedTransactions.Handler _handler;

    public ProcessPostedTransactionsTests()
    {
        _fileSystem = new Mock<IFileSystem>();
        _parser = new Mock<IPostedTransactionsParser>();
        _hashingService = new Mock<IHashingService<SHA256CryptoServiceProvider>>();
        _mediator = new Mock<IMediator>(MockBehavior.Strict);
        _writer = new Mock<IPostedTransactionFileWriter>();
        _settings = new PostedTransactionsFileSettings
        {
            OutputDirectory = "test_output",
            WorkingDirectory = "test_working"
        };

        _handler = new ProcessPostedTransactions.Handler(_fileSystem.Object, _parser.Object, _hashingService.Object, _mediator.Object, _writer.Object, _logger, _settings);
    }

    [Fact]
    public async Task WithUnreadableFile_ReturnsFailure()
    {
        var command = new ProcessPostedTransactions.Command("files/test-file.txt");

        _fileSystem.Setup(x => x.Path.GetFileName(It.Is<string>(s => s == command.FilePath))).Returns("test-file.txt");

        var ex = new Exception("Test file exception");
        _fileSystem.Setup(x => x.File.OpenText(It.IsAny<string>())).Throws(ex);

        var result = await _handler.Handle(command, default).ConfigureAwait(false);

        result.Should().BeEquivalentTo(Result.Fail($"Unable to read posted transactions file. {ex.Message}"));
    }


    [Fact]
    public async Task WithGoodFile_WritesOptumFile_ReturnsOk()
    {
        var command = new ProcessPostedTransactions.Command("files/test-file.txt");

        _fileSystem.Setup(x => x.Path.GetFileName(It.Is<string>(s => s == command.FilePath))).Returns("test-file.txt");

        var fileStream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes("TEST")));
        _fileSystem.Setup(x => x.File.OpenText(It.Is<string>(s => s == command.FilePath))).Returns(fileStream);

        var originalFile = new PostedTransactionFile
        {
            Header = new PostedTransactionHeader
            {
                RecordName = "HEADER",
                ProcessorName = "STONEEAGLE",
                ReportName = "POSTED",
                FileDate = new DateTime(2022,1,24),
                RunBeginDate = new DateTime(2022,1,13),
                RunEndDate = new DateTime(2022,1,14),
                FileFormat = "2"
            },
            Details = new List<PostedTransactionDetail>
            {
                new PostedTransactionDetail
                {
                    LineNumber = 1,
                    CardNumber = "5555930000003147",
                    TransactionDate = "01122022",
                    TransactionCode = "2200-2S-0000",
                    TransactionAmount = 0.10m,
                    TransactionAmountSign = "+",
                    TransactionCurrencyCode = 840,
                    AuthorizationCode = "990011",
                    PostDate = "01122022 18:56:30",
                    NetworkCode = "SE",                    
                    SeExternalIdNumber = "1",
                    Bin = "546893"
                },
                new PostedTransactionDetail
                {
                    LineNumber= 2,
                    CardNumber = "5555930000003237",
                    TransactionDate = "01122022",
                    TransactionCode = "2200-2S-0000",
                    TransactionAmount = 0.10m,
                    TransactionAmountSign = "-",
                    TransactionCurrencyCode = 840,
                    AuthorizationCode = "990012",
                    PostDate = "01122022 18:56:30",
                    NetworkCode = "MS",
                    MerchantNumber = "BOGUSMD",
                    MerchantName = "BOGUSMD",
                    MerchantCategoryCode = "6010",
                    MerchantCountryCode = "US",
                    SeExternalIdNumber = "2",
                    Bin = "532086"
                },
                new PostedTransactionDetail
                {
                    LineNumber = 3,
                    CardNumber = "5555930000004152",
                    TransactionDate = "01122022",
                    TransactionCode = "2200-2S-0000",
                    TransactionAmount = 0.10m,
                    TransactionAmountSign = "+",
                    TransactionCurrencyCode = 840,
                    AuthorizationCode = "990013",
                    PostDate = "01122022 18:56:30",
                    NetworkCode = "SE",
                    SeExternalIdNumber = "",
                    Bin = "528972"
                },
                new PostedTransactionDetail
                {
                    LineNumber = 4,
                    CardNumber = "5555930000004332",
                    TransactionDate = "01122022",
                    TransactionCode = "2200-2S-0000",
                    TransactionAmount = 0.20m,
                    TransactionAmountSign = "-",
                    TransactionCurrencyCode = 840,
                    AuthorizationCode = "990019",
                    PostDate = "01122022 18:56:30",
                    NetworkCode = "SE",
                    SeExternalIdNumber = "",
                    Bin = "123456"
                }
            },
            Trailer = new PostedTransactionTrailer("TRAILER", 3)            
        };

        _parser.Setup(x => x.ParseFile(It.IsAny<StreamReader>())).Returns(originalFile);

        _hashingService.Setup(x => x.ComputeHash(It.IsAny<Stream>())).Returns("TEST_HASH");

        var queryCaptor = new ArgumentCaptor<GetClientForTransactions.Query>();
        var expectedQueryList = new List<TransactionIdLookup>
        {
            new TransactionIdLookup(1),
            new TransactionIdLookup(2)
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

        _fileSystem.Setup(x => x.Path.Combine(_settings.OutputDirectory, It.IsAny<string>())).Returns("optum-file.txt");
        _fileSystem.Setup(x => x.Directory.CreateDirectory(It.IsAny<string>()));

        var outputObjCaptor = new ArgumentCaptor<PostedTransactionFile>();
        var expectedOutput = new PostedTransactionFile
        {
            Header = new PostedTransactionHeader
            {
                RecordName = "HEADER",
                ProcessorName = "VPAY, INC",
                ReportName = "AUTHORIZED",
                FileDate = new DateTime(2022, 1, 24),
                RunBeginDate = new DateTime(2022, 1, 13),
                RunEndDate = new DateTime(2022, 1, 14),
                FileFormat = "2"
            },
            Details = new List<PostedTransactionDetail>
            {
                new PostedTransactionDetail
                {
                    LineNumber = 1,
                    CardNumber = "555593XXXXXX3147",
                    TransactionDate = "01122022",
                    TransactionCode = "2200-2S-0000",
                    TransactionAmount = 0.10m,
                    TransactionAmountSign = "+",
                    TransactionCurrencyCode = 840,
                    AuthorizationCode = "990011",
                    PostDate = "01122022 18:56:30",
                    NetworkCode = "SE",
                    SeExternalIdNumber = "1",
                    TPA = "ABC",
                    Bin = "546893"
                },
                new PostedTransactionDetail
                {
                    LineNumber= 2,
                    CardNumber = "555593XXXXXX3237",
                    TransactionDate = "01122022",
                    TransactionCode = "2200-2S-0000",
                    TransactionAmount = 0.10m,
                    TransactionAmountSign = "-",
                    TransactionCurrencyCode = 840,
                    AuthorizationCode = "990012",
                    PostDate = "01122022 18:56:30",
                    NetworkCode = "MS",
                    MerchantNumber = "BOGUSMD",
                    MerchantName = "BOGUSMD",
                    MerchantCategoryCode = "6010",
                    MerchantCountryCode = "US",
                    SeExternalIdNumber = "2",
                    TPA = "DEF",
                    Bin = "532086"
                },
                new PostedTransactionDetail
                {
                    LineNumber = 3,
                    CardNumber = "555593XXXXXX4152",
                    TransactionDate = "01122022",
                    TransactionCode = "2200-2S-0000",
                    TransactionAmount = 0.10m,
                    TransactionAmountSign = "+",
                    TransactionCurrencyCode = 840,
                    AuthorizationCode = "990013",
                    PostDate = "01122022 18:56:30",
                    NetworkCode = "SE",
                    SeExternalIdNumber = "",
                    TPA = "",
                    Bin = "528972"
                }
            },
            Trailer = new PostedTransactionTrailer("TRAILER", 3)
        };

        _writer.Setup(x => x.WritePostedTransactionFile(outputObjCaptor.Capture())).Returns("TEST_OUTPUT_STRING");

        _fileSystem.Setup(x => x.File.WriteAllTextAsync(It.Is<string>(s => s == "optum-file.txt"), It.Is<string>(s => s == "TEST_OUTPUT_STRING"), default));

        var fileObjCaptor = new ArgumentCaptor<AddOlsFile.Command>();
        var expectedFileObj = new AddOlsFile.Command("test-file.txt", "TEST_HASH", OlsFileType.Posted);

        _mediator.Setup(x => x.Send(fileObjCaptor.Capture(), default)).ReturnsAsync(Result.Ok());

        var result = await _handler.Handle(command, default).ConfigureAwait(false);

        using(new AssertionScope())
        {
            result.Should().BeEquivalentTo(Result.Ok());

            fileObjCaptor.Value.Should().BeEquivalentTo(expectedFileObj);
            queryCaptor.Value.TransactionIdList.Should().BeEquivalentTo(expectedQueryList);
            outputObjCaptor.Value.Should().BeEquivalentTo(expectedOutput, opt => opt.Excluding(m => m.SelectedMemberPath.EndsWith("FileName")));
        }
    }
}

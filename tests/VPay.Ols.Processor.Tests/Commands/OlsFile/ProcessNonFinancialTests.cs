using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VPay.Extensions.Testing.Logging;
using VPay.Ols.Processor.Commands;
using VPay.Ols.Processor.Commands.OlsFile;
using VPay.Ols.Processor.Hashing;
using VPay.Ols.Processor.Models;
using VPay.Ols.Processor.Models.Constants;
using VPay.Ols.Processor.Models.NonFinancial;
using VPay.Ols.Processor.Parsers;
using VPay.Ols.Processor.Queries;
using VPay.Ols.Processor.Writers;
using Xunit;

namespace VPay.Ols.Processor.Tests.Commands.OlsFile;

public class ProcessNonFinancialTests
{
    private readonly Mock<IFileSystem> _fileSystem;
    private readonly Mock<INonFinancialParser> _parser;
    private readonly Mock<IHashingService<SHA256CryptoServiceProvider>> _hashingService;
    private readonly Mock<IMediator> _mediator;
    private readonly Mock<INonFinancialFileWriter> _writer;
    private readonly LoggerMock<ProcessNonFinancial.Handler> _logger;
    private readonly NonFinancialFileSettings _settings;

    private readonly ProcessNonFinancial.Handler _handler;

    public ProcessNonFinancialTests()
    {
        _fileSystem = new Mock<IFileSystem>();
        _parser = new Mock<INonFinancialParser>();
        _hashingService = new Mock<IHashingService<SHA256CryptoServiceProvider>>();
        _mediator = new Mock<IMediator>(MockBehavior.Strict);
        _writer = new Mock<INonFinancialFileWriter>();
        _logger = LoggerMock<ProcessNonFinancial.Handler>.CreateDefault(); ;
        _settings = new NonFinancialFileSettings
        {
            OutputDirectory = "dir1",
            WorkingDirectory = "dir2"
        };

        _handler = new ProcessNonFinancial.Handler(_logger.Object, _mediator.Object, _fileSystem.Object, _writer.Object, _parser.Object, _hashingService.Object, _settings);
    }

    [Fact]
    public async Task WithUnreadableFile_ReturnsFailure()
    {
        var command = new ProcessNonFinancial.Command("test/non-financial.txt");

        _fileSystem.Setup(x => x.Path.GetFileName(It.Is<string>(s => s == command.FilePath))).Returns("non-financial.txt");

        var ex = new Exception("Test file exception");
        _fileSystem.Setup(x => x.File.OpenText(It.IsAny<string>())).Throws(ex);

        var result = await _handler.Handle(command, default).ConfigureAwait(false);

        result.Should().BeEquivalentTo(Result.Fail($"Unable to read non-financial file. {ex.Message}"));
    }
    /*
    [Fact]
    public async Task WithGoodFile_WritesOptumFile_ReturnsOk()
    {
        var command = new ProcessNonFinancial.Command("files/test-file.txt");

        _fileSystem.Setup(x => x.Path.GetFileName(It.Is<string>(s => s == command.FilePath))).Returns("test-file.txt");

        var fileStream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes("TEST")));
        _fileSystem.Setup(x => x.File.OpenText(It.Is<string>(s => s == command.FilePath))).Returns(fileStream);

        var originalFile = new NonFinancialFile
        {
            Header = new NonFinancialHeader
            {
                RecordName = "HEADER",
                ProcessorName = "STONEEAGLE",
                ReportName = "NON-FINANCIAL",
                FileDate = new DateOnly(2022, 1, 24),
                RunBeginDate = new DateOnly(2022, 1, 13),
                RunEndDate = new DateOnly(2022, 1, 14),
                FileFormat = "3"
            },
            Details = new List<NonFinancialDetail>
            {
                new NonFinancialDetail
                {
                    LineNumber = 1,
                    CardNumber = "1234567777778900",
                    CardOpenDate = "01/01/2022",
                    CardExpirationDate = "01/01/2025",
                    CardholderIdCode = "111",
                    CardholderIdValue = "222",
                    CardholderLastName = "last-name",
                    CardholderAddress2 = "123 test addr",
                    CardholderState = "TX",
                    CardholderSecondaryPhone = "1231231123",
                    CardholderDOB = "01/01/1900",
                    Status = "status",
                    BalanceSign = "+",
                    ProgramId = "333",
                    SubProgramId = "444",
                    PseudoDDANumber = "555",
                    CustomerId = "666",
                    LinkedAccounts = "777",
                    CreditLine = "888",
                    CashAdvanceOutstanding = "999",
                    DaysDelinquent = "01",
                    AmountDelinquent = "02",
                    LastReageDate = "02/02/2022",
                    LastStatementDate = "03/03/2021",
                    CurrentPaymentDueDate = "01/01/2022",
                    CardholderFirstName = "The Stone Eagle Group",
                    CardholderAddress1 = "111 W. Spring Valley Road #100",
                    CardholderCity = "Richardson",
                    CardholderZip = "750814016",
                    CardholderPrimaryPhone = "9725551212",
                    AvailableBalance = 0.10m,
                    CurrentBalance = 0.50m,
                    SeExternalIdNumber = "1",
                    Bin = "546893"
                },
                new NonFinancialDetail
                {
                    LineNumber= 2,
                    CardNumber = "1234567777778901",
                    CardholderFirstName = "The Stone Eagle Group",
                    CardholderAddress1 = "111 W. Spring Valley Road #100",
                    CardholderCity = "Richardson",
                    CardholderZip = "750814016",
                    CardholderPrimaryPhone = "9725551212",
                    AvailableBalance = 0.22m,
                    CurrentBalance = 0.33m,
                    SeExternalIdNumber = "2",
                    Bin = "532086"
                },
                new NonFinancialDetail
                {
                    LineNumber = 3,
                    CardNumber = "1234567777778902",
                    CardholderFirstName = "The Stone Eagle Group",
                    CardholderAddress1 = "111 W. Spring Valley Road #100",
                    CardholderCity = "Richardson",
                    CardholderZip = "750814016",
                    CardholderPrimaryPhone = "9725551212",
                    AvailableBalance = 0.44m,
                    CurrentBalance = 1.66m,
                    SeExternalIdNumber = "000",
                    Bin = "528972"
                },
                new NonFinancialDetail
                {
                    LineNumber = 4,
                    CardNumber = "1234567777778903",
                    CardholderFirstName = "The Stone Eagle Group",
                    CardholderAddress1 = "111 W. Spring Valley Road #100",
                    CardholderCity = "Richardson",
                    CardholderZip = "750814016",
                    CardholderPrimaryPhone = "9725551212",
                    AvailableBalance = 0.00m,
                    CurrentBalance = 0.00m,
                    SeExternalIdNumber = "",
                    Bin = "123456"
                }
            },
            Trailer = new NonFinancialTrailer("TRAILER", 3)
        };

        _parser.Setup(x => x.ParseFile(It.IsAny<StreamReader>())).Returns(originalFile);

        _hashingService.Setup(x => x.ComputeHash(It.IsAny<Stream>())).Returns("TEST_HASH");

        var queryCaptor = new ArgumentCaptor<GetClientForTransactions.Query>();
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

        var expectedQueryList = new List<TransactionIdLookup>
        {
            new TransactionIdLookup(0),
            new TransactionIdLookup(1),
            new TransactionIdLookup(2)
        };

        _fileSystem.Setup(x => x.Path.Combine(_settings.OutputDirectory, It.IsAny<string>())).Returns("optum-file.txt");
        _fileSystem.Setup(x => x.Directory.CreateDirectory(It.IsAny<string>()));

        var outputObjCaptor = new ArgumentCaptor<NonFinancialFile>();
        var expectedOutput = new NonFinancialFile
        {
            Header = new NonFinancialHeader
            {
                RecordName = "HEADER",
                ProcessorName = "VPAY, INC",
                ReportName = "NON-FINANCIAL",
                FileDate = new DateOnly(2022, 1, 24),
                RunBeginDate = new DateOnly(2022, 1, 13),
                RunEndDate = new DateOnly(2022, 1, 14),
                FileFormat = "3"
            },
            Details = new List<NonFinancialDetail>
            {
                new NonFinancialDetail
                {
                    LineNumber = 1,
                    CardNumber = "123456XXXXXX8900",
                    CardOpenDate = "01/01/2022",
                    CardExpirationDate = "01/01/2025",
                    CardholderIdCode = "111",
                    CardholderIdValue = "222",
                    CardholderLastName = "last-name",
                    CardholderAddress2 = "123 test addr",
                    CardholderState = "TX",
                    CardholderSecondaryPhone = "1231231123",
                    CardholderDOB = "01/01/1900",
                    Status = "status",
                    BalanceSign = "+",
                    ProgramId = "333",
                    SubProgramId = "444",
                    PseudoDDANumber = "555",
                    CustomerId = "666",
                    LinkedAccounts = "777",
                    CreditLine = "888",
                    CashAdvanceOutstanding = "999",
                    DaysDelinquent = "01",
                    AmountDelinquent = "02",
                    LastReageDate = "02/02/2022",
                    LastStatementDate = "03/03/2021",
                    CurrentPaymentDueDate = "01/01/2022",
                    CardholderFirstName = NonFinancialFileConstants.OptumDetailValues.CardholderFirstName,
                    CardholderAddress1 = NonFinancialFileConstants.OptumDetailValues.CardholderAddressLine1,
                    CardholderCity = NonFinancialFileConstants.OptumDetailValues.CardholderCity,
                    CardholderZip = NonFinancialFileConstants.OptumDetailValues.CardholderZip,
                    CardholderPrimaryPhone = NonFinancialFileConstants.OptumDetailValues.CardholderPrimaryPhone,
                    AvailableBalance = 0.10m,
                    CurrentBalance = 0.50m,
                    SeExternalIdNumber = "1",
                    TPA = "ABC",
                    Bin = "546893"
                },
                new NonFinancialDetail
                {
                    LineNumber= 2,
                    CardNumber = "123456XXXXXX8901",
                    CardholderFirstName = NonFinancialFileConstants.OptumDetailValues.CardholderFirstName,
                    CardholderAddress1 = NonFinancialFileConstants.OptumDetailValues.CardholderAddressLine1,
                    CardholderCity = NonFinancialFileConstants.OptumDetailValues.CardholderCity,
                    CardholderZip = NonFinancialFileConstants.OptumDetailValues.CardholderZip,
                    CardholderPrimaryPhone = NonFinancialFileConstants.OptumDetailValues.CardholderPrimaryPhone,
                    AvailableBalance = 0.22m,
                    CurrentBalance = 0.33m,
                    SeExternalIdNumber = "2",
                    TPA = "DEF",
                    Bin = "532086"
                },
                new NonFinancialDetail
                {
                    LineNumber = 3,
                    CardNumber = "123456XXXXXX8902",
                    CardholderFirstName = NonFinancialFileConstants.OptumDetailValues.CardholderFirstName,
                    CardholderAddress1 = NonFinancialFileConstants.OptumDetailValues.CardholderAddressLine1,
                    CardholderCity = NonFinancialFileConstants.OptumDetailValues.CardholderCity,
                    CardholderZip = NonFinancialFileConstants.OptumDetailValues.CardholderZip,
                    CardholderPrimaryPhone = NonFinancialFileConstants.OptumDetailValues.CardholderPrimaryPhone,
                    AvailableBalance = 0.44m,
                    CurrentBalance = 1.66m,
                    SeExternalIdNumber = "000",
                    TPA = "",
                    Bin = "528972"
                }
            },
            Trailer = new NonFinancialTrailer("TRAILER", 3)
        };

        _writer.Setup(x => x.WriteNonFinancialFile(outputObjCaptor.Capture())).Returns("TEST_OUTPUT_STRING");

        _fileSystem.Setup(x => x.File.WriteAllTextAsync(It.Is<string>(s => s == "optum-file.txt"), It.Is<string>(s => s == "TEST_OUTPUT_STRING"), default));
        _fileSystem.Setup(x => x.FileInfo.New(It.IsAny<string>())).Returns(Mock.Of<IFileInfo>());

        var fileObjCaptor = new ArgumentCaptor<AddOlsFile.Command>();
        var expectedFileObj = new AddOlsFile.Command("test-file.txt", "TEST_HASH", OlsFileType.NonFinancial);

        _mediator.Setup(x => x.Send(fileObjCaptor.Capture(), default)).ReturnsAsync(Result.Ok());

        _mediator
            .Setup(x => x.Send(It.IsAny<SendToFileTransferService.Command>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value)
            .Verifiable();

        var result = await _handler.Handle(command, default).ConfigureAwait(false);

        using (new AssertionScope())
        {
            result.Should().BeEquivalentTo(Result.Ok());

            _logger.VerifyMessageWasLogged($"Row 3 with SE External Id 000 did not match any known transaction.", LogLevel.Warning);

            fileObjCaptor.Value.Should().BeEquivalentTo(expectedFileObj);
            queryCaptor.Value.TransactionIdList.Should().BeEquivalentTo(expectedQueryList);
            outputObjCaptor.Value.Should().BeEquivalentTo(expectedOutput, opt => opt.Excluding(m => m.Path.EndsWith("FileName")));

            // make sure the file was sent to the transfer service
            _mediator.Verify(x => x.Send(It.IsAny<SendToFileTransferService.Command>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
    */
}

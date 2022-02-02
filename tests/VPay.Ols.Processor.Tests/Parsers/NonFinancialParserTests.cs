using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FluentAssertions;
using VPay.Ols.Processor.Models.NonFinancial;
using VPay.Ols.Processor.Parsers;
using Xunit;


namespace VPay.Ols.Processor.Tests.Parsers;

public class NonFinancialParserTests
{
    [Fact]
    public void WithValidFile_ReturnsNonFinancialFile()
    {
        var fileContent = @"HEADER|STONEEAGLE|NON-FINANCIAL|01242022|01132022|01142022|3
546893XXXXXX1234|12102021|01102022|||The Stone Eagle Group||111 W. Spring Valley Road #100||Richardson|TX|750814016|US|9725551212||PYNNN|0.10|+||||||||0.10|||||||1|546893
546893XXXXXX4321|12102021|01102022|||The Stone Eagle Group||111 W. Spring Valley Road #100||Richardson|TX|750814016|US|9725551212||PYNNN|0.10|-||||||||0.10|||||||2|546893
546893XXXXXX4444|12102021|01102022|||The Stone Eagle Group||111 W. Spring Valley Road #100||Richardson|TX|750814016|US|9725551212||PYNNN|0.10|+||||||||0.10||||||||546893
TRAILER|3
";

        var file = new NonFinancialFile
        {
            Header = new NonFinancialHeader
            {
                RecordName = "HEADER",
                ProcessorName = "STONEEAGLE",
                ReportName = "NON-FINANCIAL",
                FileDate = new DateTime(2022, 1, 24),
                RunBeginDate = new DateTime(2022, 1, 13),
                RunEndDate = new DateTime(2022, 1, 14),
                FileFormat = "3"
            },
            Details = new List<NonFinancialDetail>
            {
                new NonFinancialDetail
                {
                    LineNumber = 1,
                    CardNumber = "546893XXXXXX1234",
                    CardOpenDate = "12102021",
                    CardExpirationDate = "01102022",
                    CardholderFirstName = "The Stone Eagle Group",
                    CardholderAddress1 = "111 W. Spring Valley Road #100",
                    CardholderCity = "Richardson",
                    CardholderState = "TX",
                    CardholderZip = "750814016",
                    CardholderCountry = "US",
                    CardholderPrimaryPhone = "9725551212",
                    Status = "PYNNN",
                    CurrentBalance = 0.10m,
                    BalanceSign = "+",
                    AvailableBalance = 0.10m,
                    SeExternalIdNumber = "1",
                    Bin = "546893"
                },
                new NonFinancialDetail
                {
                    LineNumber= 2,
                    CardNumber = "546893XXXXXX4321",
                    CardOpenDate = "12102021",
                    CardExpirationDate = "01102022",
                    CardholderFirstName = "The Stone Eagle Group",
                    CardholderAddress1 = "111 W. Spring Valley Road #100",
                    CardholderCity = "Richardson",
                    CardholderState = "TX",
                    CardholderZip = "750814016",
                    CardholderCountry = "US",
                    CardholderPrimaryPhone = "9725551212",
                    Status = "PYNNN",
                    CurrentBalance = 0.10m,
                    BalanceSign = "-",
                    AvailableBalance = 0.10m,
                    SeExternalIdNumber = "2",
                    Bin = "546893",
                },
                new NonFinancialDetail
                {
                    LineNumber = 3,
                    CardNumber = "546893XXXXXX4444",
                    CardOpenDate = "12102021",
                    CardExpirationDate = "01102022",
                    CardholderFirstName = "The Stone Eagle Group",
                    CardholderAddress1 = "111 W. Spring Valley Road #100",
                    CardholderCity = "Richardson",
                    CardholderState = "TX",
                    CardholderZip = "750814016",
                    CardholderCountry = "US",
                    CardholderPrimaryPhone = "9725551212",
                    Status = "PYNNN",
                    CurrentBalance = 0.10m,
                    BalanceSign = "+",
                    AvailableBalance = 0.10m,
                    Bin = "546893"
                }
            },
            Trailer = new NonFinancialTrailer("TRAILER", 3)
        };

        var parser = new NonFinancialParser();

        NonFinancialFile fileResult;

        using var fileStream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(fileContent)));
        fileResult = parser.ParseFile(fileStream);

        fileResult.Should().BeEquivalentTo(file);
    }
}

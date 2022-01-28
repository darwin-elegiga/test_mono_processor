using System;
using System.Collections.Generic;
using FluentAssertions;
using VPay.Ols.Processor.Models.PostedTransactions;
using VPay.Ols.Processor.Writers;
using Xunit;

namespace VPay.Ols.Processor.Tests.Writers;

/// <summary>
/// Tests for <see cref="OptumPostedTransactionFileWriter"/>
/// </summary>
public class OptumPostedTransactionFileWriterTests
{
    [Fact]
    public void WithInput_WritesStringOutput()
    {
        var genFileName = "20220124111433_Optum_posted_se_debit.TXT";

        var input = new PostedTransactionFile
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
                    Bin = "546893",
                    FileName = genFileName
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
                    Bin = "532086",
                    FileName = genFileName
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
                    Bin = "528972",
                    FileName = genFileName
                }
            },
            Trailer = new PostedTransactionTrailer("TRAILER", 3)
        };

        var expectedOutput = @"HEADER|VPAY, INC|AUTHORIZED|01242022|01132022|01142022|2
555593XXXXXX3147|01122022|2200-2S-0000|0.10|+|840|990011|01122022 18:56:30|SE|||||||||1|546893|ABC|20220124111433_Optum_posted_se_debit.TXT
555593XXXXXX3237|01122022|2200-2S-0000|0.10|-|840|990012|01122022 18:56:30|MS|BOGUSMD|BOGUSMD|6010|US|||||2|532086|DEF|20220124111433_Optum_posted_se_debit.TXT
555593XXXXXX4152|01122022|2200-2S-0000|0.10|+|840|990013|01122022 18:56:30|SE||||||||||528972||20220124111433_Optum_posted_se_debit.TXT
TRAILER|3
";

        var writer = new OptumPostedTransactionFileWriter();

        var result = writer.WritePostedTransactionFile(input);

        result.Should().Be(expectedOutput);
    }
}

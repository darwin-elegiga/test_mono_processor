using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FluentAssertions;
using VPay.Ols.Processor.Models.PostedTransactions;
using VPay.Ols.Processor.Parsers;
using Xunit;

namespace VPay.Ols.Processor.Tests.Parsers;

/// <summary>
/// Tests for <see cref="PostedTransactionsParser"/>
/// </summary>
public class PostedTransactionsParserTests
{
    [Fact]
    public void WithValidFile_ReturnsPostedTransactionFile()
    {
        var fileContent = $@"HEADER|STONEEAGLE|POSTED|01242022|01132022|01142022|2
555593XXXXXX3147|01122022|2200-2S-0000|0.10|+|840|990011|01122022 18:56:30|SE|||||||||{(long)int.MaxValue + 1}|546893
555593XXXXXX3237|01122022|2200-2S-0000|0.10|-|840|990012|01122022 18:56:30|MS|BOGUSMD|BOGUSMD|6010|US|||||{(long)int.MaxValue + 2}|532086
555593XXXXXX4152|01122022|2200-2S-0000|0.10|+|840|990013|01122022 18:56:30|SE||||||||||528972
TRAILER|3
";

        var file = new PostedTransactionFile
        {
            Header = new PostedTransactionHeader
            {
                RecordName = "HEADER",
                ProcessorName = "STONEEAGLE",
                ReportName = "POSTED",
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
                    SeExternalIdNumber = ((long)int.MaxValue + 1).ToString(),
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
                    SeExternalIdNumber = ((long)int.MaxValue + 2).ToString(),
                    Bin = "532086",
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
                    Bin = "528972"
                }
            },
            Trailer = new PostedTransactionTrailer("TRAILER", 3)
        };

        var parser = new PostedTransactionsParser();

        PostedTransactionFile fileResult;

        using var fileStream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(fileContent)));
        fileResult = parser.ParseFile(fileStream);

        fileResult.Should().BeEquivalentTo(file);
    }
}

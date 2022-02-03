using System;
using System.Collections.Generic;
using FluentAssertions;
using VPay.Ols.Processor.Models.Authorization;
using Xunit;

namespace VPay.Ols.Processor.Writers.Tests;

public class AuthorizationFileWriterTests
{
    [Fact]
    public void WriteAuthorizationFile_ShouldCreateOutput()
    {
        // arrange
        string generatedFileName = "20220124111433_Optum_authorized_se_debit.TXT";

        var header = new AuthorizationHeader
        {
            RecordName = "HEADER",
            ProcessorName = "VPAY, INC",
            ReportName = "AUTHORIZED",
            FileDate = new DateOnly(2022, 1, 24),
            RunBeginDate = new DateOnly(2022, 1, 13),
            RunEndDate = new DateOnly(2022, 1, 14),
        };

        var details = new List<AuthorizationDetail>
            {
                new AuthorizationDetail
                {
                    LineNumber = 1,
                    CardNumber = "555593XXXXXX3147",
                    TransactionDateTime = new DateTime(2022, 1, 12, 12, 28, 20),
                    TransactionCurrencyCode = 840,
                    AddressVerificationResponse = "X",
                    AuthorizationResponse = "2100-00-00",
                    AuthorizationAmount = 0.10m,
                    AuthorizationCode = "990011",
                    NetworkCode = "SE",
                    SEExternalId = 1,
                    Bin = 546893,
                    ClientCode = "ABC",
                    FileName = generatedFileName
                },
                new AuthorizationDetail
                {
                    LineNumber= 2,
                    CardNumber = "555593XXXXXX3237",
                    TransactionDateTime = new DateTime(2022, 1, 12),
                    TransactionCurrencyCode = 840,
                    AddressVerificationResponse = "X",
                    AuthorizationResponse = "2100-00-00",
                    AuthorizationAmount = 0.10m,
                    AuthorizationCode = "990012",
                    NetworkCode = "MS",
                    MerchantNumber = "BOGUSMD",
                    MerchantName = "BOGUSMD",
                    MerchantCategoryCode = "6010",
                    MerchantCountryCode = "US",
                    SEExternalId = 2,
                    Bin = 532086,
                    ClientCode = "DEF",
                    FileName = generatedFileName
                },
                new AuthorizationDetail
                {
                    LineNumber = 3,
                    CardNumber = "555593XXXXXX4152",
                    TransactionDateTime = new DateTime(2022, 01, 22, 01, 12, 22),
                    AddressVerificationResponse = "X",
                    AuthorizationResponse = "2100-00-00",
                    AuthorizationAmount = 0.10m,
                    TransactionCurrencyCode = 840,
                    AuthorizationCode = "990013",
                    NetworkCode = "SE",
                    SEExternalId = 26,
                    Bin = 528972,
                    ClientCode = "GHI",
                    FileName = generatedFileName
                }
            };

        var trailer = new AuthorizationTrailer
        {
            RecordName = "TRAILER",
            Count = 3
        };

        var file = new AuthorizationFile(header, details, trailer);

        string expectedOutput = @"HEADER|VPAY, INC|AUTHORIZED|01242022|01132022|01142022
555593XXXXXX3147|01122022 12:28:20|840|X|2100-00-00|0.10|990011|SE|||||1|546893|ABC|20220124111433_Optum_authorized_se_debit.TXT
555593XXXXXX3237|01122022 00:00:00|840|X|2100-00-00|0.10|990012|MS|BOGUSMD|BOGUSMD|6010|US|2|532086|DEF|20220124111433_Optum_authorized_se_debit.TXT
555593XXXXXX4152|01222022 01:12:22|840|X|2100-00-00|0.10|990013|SE|||||26|528972|GHI|20220124111433_Optum_authorized_se_debit.TXT
TRAILER|3
";

        var writer = new AuthorizationFileWriter();

        // act
        string result = writer.WriteAuthorizationFile(file);

        // assert
        result.Should().Be(expectedOutput);
    }
}

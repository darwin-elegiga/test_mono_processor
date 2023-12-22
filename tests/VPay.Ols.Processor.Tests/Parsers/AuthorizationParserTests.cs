using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FluentAssertions;
using VPay.Ols.Processor.Models.Authorization;
using Xunit;

namespace VPay.Ols.Processor.Parsers.Tests;

public class AuthorizationParserTests
{
    [Fact]
    public void ParseFileTest()
    {
        // arrange
        string fileContent = $@"abcd|bcde|cdef|02031234|03042345|04053456
1234|03042345 05:06:07|5678|abcd|bcde|67.89|cdef|defg|efgh|fghi|ghij|hijk|{(long)int.MaxValue + 56}|8901
abcd|1234
";

        var header = new AuthorizationHeader
        {
            RecordName = "abcd",
            ProcessorName = "bcde",
            ReportName = "cdef",
            FileDate = new DateOnly(1234, 2, 3),
            RunBeginDate = new DateOnly(2345, 3, 4),
            RunEndDate = new DateOnly(3456, 4, 5)
        };

        var detail = new AuthorizationDetail
        {
            LineNumber = 1,
            CardNumber = "1234",
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
            SEExternalId = (long)int.MaxValue + 56,
            Bin = 8901
        };

        var details = new List<AuthorizationDetail> { detail };

        var trailer = new AuthorizationTrailer
        {
            RecordName = "abcd",
            Count = 1234
        };

        var file = new AuthorizationFile(header, details, trailer);

        var parser = new AuthorizationParser();

        using var fileStream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(fileContent)));

        // act
        AuthorizationFile result = parser.ParseFile(fileStream);

        // assert
        result.Header.Should().BeEquivalentTo(header);
        result.Details.Should().BeEquivalentTo(details);
        result.Trailer.Should().BeEquivalentTo(trailer);
    }
}

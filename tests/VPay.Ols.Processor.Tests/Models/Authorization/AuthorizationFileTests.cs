using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace VPay.Ols.Processor.Models.Authorization.Tests;

public class AuthorizationFileTests
{
    [Fact]
    public void Copy()
    {
        // arrange
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
            LineNumber = 28,
            CardNumber = "1234",
            TransactionDateTime = new DateTime(2345, 3, 4),
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
            SEExternalId = 7890,
            Bin = 8901,
            ClientCode = "ijkl",
            FileName = "jklm"
        };

        var details = new List<AuthorizationDetail> { detail };

        var trailer = new AuthorizationTrailer
        {
            RecordName = "abcd",
            Count = 1234
        };

        var file = new AuthorizationFile(header, details, trailer);

        // act
        AuthorizationFile copy = file.Copy();

        // assert
        copy.Header.Should().BeEquivalentTo(header);
        copy.Details.Should().BeEquivalentTo(details);
        copy.Trailer.Should().BeEquivalentTo(trailer);
    }
}

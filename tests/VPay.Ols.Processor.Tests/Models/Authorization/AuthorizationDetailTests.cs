using System;
using FluentAssertions;
using Xunit;

namespace VPay.Ols.Processor.Models.Authorization.Tests;

public class AuthorizationDetailTests
{
    [Fact]
    public void Copy()
    {
        // arrange
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

        // act
        AuthorizationDetail copy = detail.Copy();

        // assert
        copy.Should().BeEquivalentTo(detail);
    }
}

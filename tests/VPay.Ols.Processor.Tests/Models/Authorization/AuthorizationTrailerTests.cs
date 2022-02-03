using FluentAssertions;
using Xunit;

namespace VPay.Ols.Processor.Models.Authorization.Tests;

public class AuthorizationTrailerTests
{
    [Fact]
    public void Copy()
    {
        // arrange
        var trailer = new AuthorizationTrailer
        {
            RecordName = "abcd",
            Count = 1234
        };

        // act
        AuthorizationTrailer copy = trailer.Copy();

        // assert
        copy.Should().BeEquivalentTo(trailer);
    }
}

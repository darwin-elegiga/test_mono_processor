using System;
using FluentAssertions;
using Xunit;

namespace VPay.Ols.Processor.Models.Authorization.Tests;

public class AuthorizationHeaderTests
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

        // act
        AuthorizationHeader copy = header.Copy();

        // assert
        copy.Should().BeEquivalentTo(header);
    }
}

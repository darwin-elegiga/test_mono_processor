using System;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Data.SqlClient;
using Moq;
using VPay.Ols.Processor.Commands.OlsFile;
using VPay.Ols.Processor.Data.Connection;
using VPay.Ols.Processor.Models;
using Xunit;

namespace VPay.Ols.Processor.Tests.Commands.OlsFile;

/// <summary>
/// Tests for <see cref="AddOlsFile.Handler"/>
/// </summary>
public class AddOlsFileTests
{
    private readonly Mock<IDataConnection<SqlConnection>> _connection;
    private readonly AddOlsFile.Handler _handler;

    public AddOlsFileTests()
    {
        _connection = new Mock<IDataConnection<SqlConnection>>(MockBehavior.Strict);
        _handler = new AddOlsFile.Handler(_connection.Object);
    }

    [Fact]
    public async Task WithGoodRequest_ReturnsOk()
    {
        var command = new AddOlsFile.Command("Test-File.txt", "TESTHASH", Models.OlsFileType.NonFinancial);

        var sprocCaptor = new ArgumentCaptor<string>();
        var objCaptor = new ArgumentCaptor<object>();

        var expectedObj = new
        {
            command.FileName,
            command.FileHash,
            FileType = "NonFinancial"            
        };

        _connection.Setup(x => x.ExecuteAsync(sprocCaptor.Capture(), objCaptor.Capture(), default)).ReturnsAsync(1);

        var result = await _handler.Handle(command, default);

        using(new AssertionScope())
        {
            result.Should().BeEquivalentTo(Result.Ok());
            sprocCaptor.Value.Should().Be(AddOlsFile.Handler.Sproc);
            objCaptor.Value.Should().BeEquivalentTo(expectedObj);
        }
    }

    [Fact]
    public async Task WithException_ReturnsFailure()
    {
        var command = new AddOlsFile.Command("Test-File.txt", "TESTHASH", Models.OlsFileType.Authorized);

        var sprocCaptor = new ArgumentCaptor<string>();
        var objCaptor = new ArgumentCaptor<object>();

        var expectedObj = new
        {
            command.FileName,
            command.FileHash,
            FileType = "Authorized"
        };

        var ex = new Exception("Test exception message");

        _connection.Setup(x => x.ExecuteAsync(sprocCaptor.Capture(), objCaptor.Capture(), default)).ThrowsAsync(ex);

        var result = await _handler.Handle(command, default);

        using (new AssertionScope())
        {
            result.Should().BeEquivalentTo(Result.Fail(ex.Message));
            sprocCaptor.Value.Should().Be(AddOlsFile.Handler.Sproc);
            objCaptor.Value.Should().BeEquivalentTo(expectedObj);
        }
    }
}

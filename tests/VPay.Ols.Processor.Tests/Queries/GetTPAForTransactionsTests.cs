using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Data.SqlClient;
using Moq;
using VPay.Ols.Processor.Data.Connection;
using VPay.Ols.Processor.Models;
using VPay.Ols.Processor.Queries;
using Xunit;

namespace VPay.Ols.Processor.Tests.Queries;

/// <summary>
/// Tests for <see cref="GetClientForTransactions.Handler"/>
/// </summary>
public class GetTPAForTransactionsTests
{
    private readonly Mock<IDataConnection<SqlConnection>> _connection;
    private readonly GetClientForTransactions.Handler _handler;

    public GetTPAForTransactionsTests()
    {
        _connection = new Mock<IDataConnection<SqlConnection>>();
        _handler = new GetClientForTransactions.Handler(_connection.Object);
    }

    [Fact]
    public async Task WithRequest_CallsConnection_ReturnsResult()
    {
        var ids = new List<int> { 1, 2, 3 };
        var expectedQuery = ids.Select(i => new TransactionIdLookup(i)).ToList();

        var query = new GetClientForTransactions.Query(ids);

        var sprocCaptor = new ArgumentCaptor<string>();
        var queryCaptor = new ArgumentCaptor<GetClientForTransactions.Query>();

        var queryResult = ids.Select(i => new TransactionClient { TransactionId = i, ClientCode = $"TPA{i}" }).ToList();

        _connection.Setup(x => x.ListAsync<TransactionClient>(sprocCaptor.Capture(), queryCaptor.Capture(), default)).ReturnsAsync(queryResult);

        var result = await _handler.Handle(query, default).ConfigureAwait(false);

        using(new AssertionScope())
        {
            result.Should().BeEquivalentTo(queryResult);

            sprocCaptor.Value.Should().Be(GetClientForTransactions.Handler.Sproc);
            queryCaptor.Value.TransactionIdList.Should().BeEquivalentTo(expectedQuery);
        }
    }
}

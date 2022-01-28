using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Ols.Processor.Data.Connection;
using VPay.Ols.Processor.Data.Extensions;
using VPay.Ols.Processor.Models;

namespace VPay.Ols.Processor.Queries;

public static class GetClientForTransactions
{
    public class Query : IRequest<List<TransactionClient>>
    {
        internal List<TransactionIdLookup> TransactionIdList { get; }
        public DataTable TransactionIds => TransactionIdList.ToDataTable();

        public Query(List<int> transactionIds) => TransactionIdList = transactionIds.ConvertAll(i => new TransactionIdLookup(i));
    }

    public class Handler : IRequestHandler<Query, List<TransactionClient>>
    {
        internal static readonly string Sproc = "[dbo].[usp_ListClient_ByTransactionIds]";

        private readonly IDataConnection<SqlConnection> _connection;

        public Handler(IDataConnection<SqlConnection> connection) => _connection = connection;

        public Task<List<TransactionClient>> Handle(Query request, CancellationToken cancellationToken)
        {
            return _connection.ListAsync<TransactionClient>(Sproc, request, cancellationToken);
        }
    }
}

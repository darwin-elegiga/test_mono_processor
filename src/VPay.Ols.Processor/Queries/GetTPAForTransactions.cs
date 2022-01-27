using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Ols.Processor.Data.Connection;
using VPay.Ols.Processor.Data.Extensions;
using VPay.Ols.Processor.Models;

namespace VPay.Ols.Processor.Queries;

public static class GetTPAForTransactions
{
    public class Query : IRequest<List<TransactionTpa>>
    {
        internal List<TransactionIdLookup> TransactionIdList { get; }
        public DataTable TransactionIds => TransactionIdList.ToDataTable();

        public Query(List<int> transactionIds) => TransactionIdList = transactionIds.Select(i => new TransactionIdLookup(i)).ToList();
    }

    public class Handler : IRequestHandler<Query, List<TransactionTpa>>
    {
        internal static readonly string Sproc = "[dbo].[usp_ListTpa_ByTransactionIds]";

        private readonly IDataConnection<SqlConnection> _connection;

        public Handler(IDataConnection<SqlConnection> connection) => _connection = connection;

        public Task<List<TransactionTpa>> Handle(Query request, CancellationToken cancellationToken)
        {
            return _connection.ListAsync<TransactionTpa>(Sproc, request, cancellationToken);
        }
    }
}

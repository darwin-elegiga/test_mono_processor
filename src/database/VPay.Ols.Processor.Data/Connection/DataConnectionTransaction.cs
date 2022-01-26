using System;
using System.Data;
using System.Data.Common;

namespace VPay.Ols.Processor.Data.Connection;

public sealed class DataConnectionTransaction<T> : IDataConnectionTransaction where T : DbConnection
{
    private readonly DataConnection<T> _dataConnection;

    public IDbTransaction Transaction { get; }
    public Guid TransactionId { get; }

    internal DataConnectionTransaction(DataConnection<T> dataConnection, IDbTransaction transaction)
    {
        _dataConnection = dataConnection;
        Transaction = transaction;
        TransactionId = Guid.NewGuid();

        _dataConnection.UseTransaction(this);
    }

    public void Commit()
    {
        Transaction.Commit();
    }

    public void Rollback()
    {
        Transaction.Rollback();
        _dataConnection.UseTransaction(null);
    }

    public void Dispose()
    {
        Transaction.Dispose();
        _dataConnection.UseTransaction(null);
    }
}

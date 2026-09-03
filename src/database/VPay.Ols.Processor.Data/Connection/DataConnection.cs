using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Polly;
using VPay.Ols.Processor.Data.Paging;

namespace VPay.Ols.Processor.Data.Connection;

/// <summary>
/// This is the base class for creating a DataConnection that should be passed into each of the Database services.
/// </summary>
/// <typeparam name="TDbConnection"></typeparam>
public abstract class DataConnection<TDbConnection> : IDataConnection<TDbConnection>
    where TDbConnection : DbConnection
{
    /// <summary>
    /// Instantiates a new DataConnection class with the connectionString
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <exception cref="ArgumentException">If the <paramref name="connectionString"/> is an empty string or a string containing only whitespace.</exception>
    protected DataConnection(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Must not be empty or whitespace", nameof(connectionString));

        ConnectionString = connectionString;
    }

    public int? DefaultCommandTimeout { get; set; }

    public abstract IRetryPolicyRegistry RetryPolicy { get; }

    /// <summary>
    /// This is the passed in Connection string
    /// </summary>
    protected string ConnectionString { get; }

    /// <summary>
    /// This is the DbConnection
    /// </summary>
    protected TDbConnection? DbConnection { get; private set; }

    public virtual IDbTransaction? CurrentTransaction => DataConnectionTransaction?.Transaction;

    /// <summary>
    ///     Gets the current transaction.
    /// </summary>
    public virtual DataConnectionTransaction<TDbConnection>? DataConnectionTransaction { get; protected set; }

    /// <summary>
    /// This will create a connection object
    /// </summary>
    protected abstract TDbConnection GetConnectionObject();

    /// <inheritdoc />
    public async Task<TDbConnection> GetOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (DbConnection == null)
        {
            DbConnection = GetConnectionObject();
            await OpenAsync(cancellationToken).ConfigureAwait(false);
        }
        else if (DbConnection.State == ConnectionState.Closed)
        {
            await OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        return DbConnection;
    }

    public async Task<IDataConnectionTransaction> BeginTransaction()
    {
        if (CurrentTransaction != null)
            throw new InvalidOperationException("Transaction is already started");

        TDbConnection connection = await GetOpenConnectionAsync().ConfigureAwait(false);
        DbTransaction transaction = await connection.BeginTransactionAsync().ConfigureAwait(false);

        return new DataConnectionTransaction<TDbConnection>(this, transaction);
    }

    public async Task<IDataConnectionTransaction> BeginTransaction(IsolationLevel isolationLevel)
    {
        if (CurrentTransaction != null)
            throw new InvalidOperationException("Transaction is already started");

        TDbConnection connection = await GetOpenConnectionAsync().ConfigureAwait(false);
        DbTransaction transaction = await connection.BeginTransactionAsync(isolationLevel).ConfigureAwait(false);

        return new DataConnectionTransaction<TDbConnection>(this, transaction);
    }

    private async Task OpenAsync(CancellationToken cancellationToken)
    {
        if (DbConnection == null)
        {
            return;
        }

        IAsyncPolicy policy = RetryPolicy.GetOpenConnectionRetryPolicy();
        if (policy == null)
        {
            await DbConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await policy.ExecuteAsync(t => DbConnection.OpenAsync(t), cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
    {
        TDbConnection connection = await GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        return connection.State == ConnectionState.Open;
    }

    /// <summary>
    ///     Specifies an existing <see cref="DbTransaction" /> to be used for database operations.
    /// </summary>
    /// <param name="transaction"> The transaction to be used. </param>
    internal virtual IDataConnectionTransaction? UseTransaction(
        DataConnectionTransaction<TDbConnection>? transaction)
    {
        if (transaction == null)
        {
            DataConnectionTransaction = null;
        }
        else
        {
            if (CurrentTransaction != null)
                throw new InvalidOperationException("Transaction is already started");

            DataConnectionTransaction = transaction;
        }

        return DataConnectionTransaction;
    }

    #region Read Queries

    /// <summary>
    /// Execute a sproc to return a single-row asynchronously.
    /// </summary>
    /// <typeparam name="TModel">The type of results to return.</typeparam>
    public async Task<TModel?> GetSingleOrDefaultAsync<TModel>(string sql, object? parameters,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException(nameof(sql) + " cannot be empty", nameof(sql));

        TDbConnection databaseConnection = await GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        IAsyncPolicy? retryPolicy = RetryPolicy?.GetStandardRetryPolicy();

        return await databaseConnection.QuerySingleOrDefaultAsync<TModel>(sql, parameters, CurrentTransaction,
                DefaultCommandTimeout, CommandType.StoredProcedure, retryPolicy, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Execute a sproc to return multiple rows asynchronously in a list.
    /// </summary>
    /// <typeparam name="TModel">The type of results to return.</typeparam>
    public async Task<List<TModel>> ListAsync<TModel>(string sql, object? parameters,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException(nameof(sql) + " cannot be empty", nameof(sql));

        TDbConnection databaseConnection = await GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        IAsyncPolicy? retryPolicy = RetryPolicy?.GetStandardRetryPolicy();

        IEnumerable<TModel> response = await databaseConnection.QueryAsync<TModel>(sql, parameters,
                CurrentTransaction, DefaultCommandTimeout, CommandType.StoredProcedure, retryPolicy,
                cancellationToken)
            .ConfigureAwait(false);

        List<TModel> items = response.ToList();
        return items;
    }

    /// <summary>
    /// Execute a sproc to return multiple lists of rows asynchronously.
    /// </summary>
    /// <typeparam name="TModel1">The first type of results to return.</typeparam>
    /// <typeparam name="TModel2">The second type of results to return.</typeparam>
    /// <typeparam name="TModel3">The third type of results to return.</typeparam>
    public async Task<(List<TModel1>, List<TModel2>, List<TModel3>)> ListAsync<TModel1, TModel2, TModel3>(
        string sql, object? parameters, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException(nameof(sql) + " cannot be empty", nameof(sql));

        TDbConnection databaseConnection = await GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        IAsyncPolicy? retryPolicy = RetryPolicy?.GetStandardRetryPolicy();

        SqlMapper.GridReader response = await databaseConnection.QueryMultipleAsync(sql, parameters,
                CurrentTransaction, DefaultCommandTimeout, CommandType.StoredProcedure, retryPolicy,
                cancellationToken)
            .ConfigureAwait(false);

        List<TModel1> results1 = response.Read<TModel1>().ToList();
        List<TModel2> results2 = response.Read<TModel2>().ToList();
        List<TModel3> results3 = response.Read<TModel3>().ToList();

        return (results1, results2, results3);
    }

    /// <summary>
    /// Execute a sproc to return multiple lists of rows asynchronously.
    /// </summary>
    /// <typeparam name="TModel1">The first type of results to return.</typeparam>
    /// <typeparam name="TModel2">The second type of results to return.</typeparam>
    /// <typeparam name="TModel3">The third type of results to return.</typeparam>
    /// <typeparam name="TModel4">The fourth type of results to return.</typeparam>
    /// <typeparam name="TModel5">The fifth type of results to return.</typeparam>
    public async
        Task<(List<TModel1>, List<TModel2>, List<TModel3>, List<TModel4>, List<TModel5>)> ListAsync<TModel1, TModel2, TModel3, TModel4, TModel5>(string sql, object? parameters,
            CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException(nameof(sql) + " cannot be empty", nameof(sql));

        TDbConnection databaseConnection = await GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        IAsyncPolicy? retryPolicy = RetryPolicy?.GetStandardRetryPolicy();

        SqlMapper.GridReader response = await databaseConnection.QueryMultipleAsync(sql, parameters,
                CurrentTransaction, DefaultCommandTimeout, CommandType.StoredProcedure, retryPolicy,
                cancellationToken)
            .ConfigureAwait(false);

        List<TModel1> results1 = response.Read<TModel1>().ToList();
        List<TModel2> results2 = response.Read<TModel2>().ToList();
        List<TModel3> results3 = response.Read<TModel3>().ToList();
        List<TModel4> results4 = response.Read<TModel4>().ToList();
        List<TModel5> results5 = response.Read<TModel5>().ToList();

        return (results1, results2, results3, results4, results5);
    }

    /// <summary>
    /// Execute a sproc to return multiple lists of rows asynchronously.
    /// </summary>
    /// <typeparam name="TModel1">The first type of results to return.</typeparam>
    /// <typeparam name="TModel2">The second type of results to return.</typeparam>
    /// <typeparam name="TModel3">The third type of results to return.</typeparam>
    /// <typeparam name="TModel4">The fourth type of results to return.</typeparam>
    /// <typeparam name="TModel5">The fifth type of results to return.</typeparam>
    /// <typeparam name="TModel6">The sixth type of results to return.</typeparam>
    public async
        Task<(List<TModel1>, List<TModel2>, List<TModel3>, List<TModel4>, List<TModel5>, List<TModel6>)> ListAsync<TModel1, TModel2, TModel3, TModel4, TModel5, TModel6>(string sql, object? parameters,
            CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException(nameof(sql) + " cannot be empty", nameof(sql));

        TDbConnection databaseConnection = await GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        IAsyncPolicy? retryPolicy = RetryPolicy?.GetStandardRetryPolicy();

        SqlMapper.GridReader response = await databaseConnection.QueryMultipleAsync(sql, parameters,
                CurrentTransaction, DefaultCommandTimeout, CommandType.StoredProcedure, retryPolicy,
                cancellationToken)
            .ConfigureAwait(false);

        List<TModel1> results1 = response.Read<TModel1>().ToList();
        List<TModel2> results2 = response.Read<TModel2>().ToList();
        List<TModel3> results3 = response.Read<TModel3>().ToList();
        List<TModel4> results4 = response.Read<TModel4>().ToList();
        List<TModel5> results5 = response.Read<TModel5>().ToList();
        List<TModel6> results6 = response.Read<TModel6>().ToList();

        return (results1, results2, results3, results4, results5, results6);
    }

    /// <summary>
    /// Execute a sproc to return multiple rows asynchronously in a PagedResult.
    /// </summary>
    /// <typeparam name="TModel">The type of results to return.</typeparam>
    public async Task<PagedResult<TModel>> GetPagedResultAsync<TModel>(string sql, object? parameters,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException(nameof(sql) + " cannot be empty", nameof(sql));

        TDbConnection databaseConnection = await GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        IAsyncPolicy? retryPolicy = RetryPolicy?.GetStandardRetryPolicy();

        SqlMapper.GridReader response = await databaseConnection.QueryMultipleAsync(sql, parameters,
                CurrentTransaction, DefaultCommandTimeout, CommandType.StoredProcedure, retryPolicy,
                cancellationToken)
            .ConfigureAwait(false);
        List<TModel> items = response.Read<TModel>().ToList();
        PageModel pageInfo = response.ReadFirst<PageModel>();

        return new PagedResult<TModel>(items, pageInfo);
    }

    #endregion Read Queries

    /// <summary>
    /// Execute a stored procedure asynchronously without expected results
    /// </summary>
    /// <typeparam name="int">The number of rows affected</typeparam>
    public async Task<int> ExecuteAsync(string sql, object? parameters, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException(nameof(sql) + " cannot be empty", nameof(sql));

        TDbConnection databaseConnection = await GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        IAsyncPolicy? retryPolicy = RetryPolicy?.GetStandardRetryPolicy();

        return await databaseConnection.ExecuteAsync(sql, parameters, CurrentTransaction, DefaultCommandTimeout, CommandType.StoredProcedure, retryPolicy, cancellationToken).ConfigureAwait(false);
    }

    #region IDisposable

    private bool _disposed;

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    // Protected implementation of Dispose pattern.
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            CurrentTransaction?.Dispose();

            DbConnection?.Close();
            DbConnection?.Dispose();
        }

        _disposed = true;
    }

    #endregion IDisposable
}

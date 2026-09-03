using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using VPay.Ols.Processor.Data.Paging;

namespace VPay.Ols.Processor.Data.Connection;

/// <summary>
/// This will be used to pass in the connection for each call
/// </summary>
/// <typeparam name="T">The type of the connection.</typeparam>
public interface IDataConnection<T> : IDisposable where T : IDbConnection
{
    /// <summary>
    /// The time in seconds to wait for the command to execute.
    /// </summary>
    int? DefaultCommandTimeout { get; }

    /// <summary>
    /// This is the standard Polly retry policy that each method will use when running
    /// </summary>
    IRetryPolicyRegistry RetryPolicy { get; }

    /// <summary>
    /// This is the current transaction
    /// </summary>
    IDbTransaction? CurrentTransaction { get; }

    /// <summary>
    /// This will open a connection to the database asynchronously
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>This returns an open connection of type IDbConnection</returns>
    Task<T> GetOpenConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// This will test if a connection can be opened to the database
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>This returns true if a connection to the database can be opened</returns>
    Task<bool> CanConnectAsync(CancellationToken cancellationToken = default);

    Task<IDataConnectionTransaction> BeginTransaction();

    Task<IDataConnectionTransaction> BeginTransaction(IsolationLevel isolationLevel);

    #region Read Queries

    /// <summary>
    /// Execute a sproc to return a single-row asynchronously.
    /// </summary>
    /// <typeparam name="TModel">The type of result to return.</typeparam>
    Task<TModel?> GetSingleOrDefaultAsync<TModel>(string sql, object? parameters,
        CancellationToken cancellationToken);

    /// <summary>
    /// Execute a sproc to return multiple rows asynchronously in a list.
    /// </summary>
    /// <typeparam name="TModel">The type of results to return.</typeparam>
    Task<List<TModel>> ListAsync<TModel>(string sql, object? parameters, CancellationToken cancellationToken);

    /// <summary>
    /// Execute a sproc to return multiple lists of rows asynchronously.
    /// </summary>
    /// <typeparam name="TModel1">The first type of results to return.</typeparam>
    /// <typeparam name="TModel2">The second type of results to return.</typeparam>
    /// <typeparam name="TModel3">The third type of results to return.</typeparam>
    Task<(List<TModel1>, List<TModel2>, List<TModel3>)> ListAsync<TModel1, TModel2, TModel3>(string sql,
        object? parameters, CancellationToken cancellationToken);

    /// <summary>
    /// Execute a sproc to return multiple lists of rows asynchronously.
    /// </summary>
    /// <typeparam name="TModel1">The first type of results to return.</typeparam>
    /// <typeparam name="TModel2">The second type of results to return.</typeparam>
    /// <typeparam name="TModel3">The third type of results to return.</typeparam>
    /// <typeparam name="TModel4">The fourth type of results to return.</typeparam>
    /// <typeparam name="TModel5">The fifth type of results to return.</typeparam>
    Task<(List<TModel1>, List<TModel2>, List<TModel3>, List<TModel4>, List<TModel5>)> ListAsync<TModel1, TModel2, TModel3, TModel4, TModel5>(
        string sql, object? parameters, CancellationToken cancellationToken);

    /// <summary>
    /// Execute a sproc to return multiple lists of rows asynchronously.
    /// </summary>
    /// <typeparam name="TModel1">The first type of results to return.</typeparam>
    /// <typeparam name="TModel2">The second type of results to return.</typeparam>
    /// <typeparam name="TModel3">The third type of results to return.</typeparam>
    /// <typeparam name="TModel4">The fourth type of results to return.</typeparam>
    /// <typeparam name="TModel5">The fifth type of results to return.</typeparam>
    /// <typeparam name="TModel6">The sixth type of results to return.</typeparam>
    Task<(List<TModel1>, List<TModel2>, List<TModel3>, List<TModel4>, List<TModel5>, List<TModel6>)> ListAsync<TModel1, TModel2, TModel3, TModel4, TModel5, TModel6>(
        string sql, object? parameters, CancellationToken cancellationToken);

    /// <summary>
    /// Execute a sproc to return multiple rows asynchronously in a PagedResult.
    /// </summary>
    /// <typeparam name="TModel">The type of results to return.</typeparam>
    Task<PagedResult<TModel>> GetPagedResultAsync<TModel>(string sql, object? parameters,
        CancellationToken cancellationToken);

    #endregion Read Queries

    /// <summary>
    /// Execute a stored procedure asynchronously without expected results
    /// </summary>
    /// <typeparam name="int">The number of rows affected</typeparam>
    Task<int> ExecuteAsync(string sql, object? parameters, CancellationToken cancellationToken);
}

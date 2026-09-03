using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Polly;

namespace VPay.Ols.Processor.Data.Connection;

internal static class DapperHelpers
{
    internal static Task<T?> QuerySingleOrDefaultAsync<T>
    (
        this IDbConnection connection,
        string sql,
        object? parameters,
        IDbTransaction? transaction,
        int? commandTimeout,
        CommandType? commandType,
        IAsyncPolicy? policy,
        CancellationToken cancellationToken
    )
    {
        if (policy == null || transaction != null)
        {
            var command = new CommandDefinition(sql, parameters, transaction, commandTimeout, commandType, cancellationToken: cancellationToken);
            return connection.QuerySingleOrDefaultAsync<T>(command);
        }

        var context = new Context
        {
            ["sql"] = sql
        };

        return policy.ExecuteAsync((_, token) =>
        {
            var command = new CommandDefinition(sql, parameters, transaction, commandTimeout, commandType, cancellationToken: token);
            return connection.QuerySingleOrDefaultAsync<T>(command);
        }, context, cancellationToken);
    }

    internal static Task<IEnumerable<T>> QueryAsync<T>
    (
        this IDbConnection connection,
        string sql,
        object? parameters,
        IDbTransaction? transaction,
        int? commandTimeout,
        CommandType? commandType,
        IAsyncPolicy? policy,
        CancellationToken cancellationToken
    )
    {
        if (policy == null || transaction != null)
        {
            var command = new CommandDefinition(sql, parameters, transaction, commandTimeout, commandType, cancellationToken: cancellationToken);
            return connection.QueryAsync<T>(command);
        }

        var context = new Context
        {
            ["sql"] = sql
        };

        return policy.ExecuteAsync((_, token) =>
        {
            var command = new CommandDefinition(sql, parameters, transaction, commandTimeout, commandType, cancellationToken: token);
            return connection.QueryAsync<T>(command);
        }, context, cancellationToken);
    }

    internal static Task<SqlMapper.GridReader> QueryMultipleAsync
    (
        this IDbConnection connection,
        string sql,
        object? parameters,
        IDbTransaction? transaction,
        int? commandTimeout,
        CommandType? commandType,
        IAsyncPolicy? policy,
        CancellationToken cancellationToken
    )
    {
        if (policy == null || transaction != null)
        {
            var command = new CommandDefinition(sql, parameters, transaction, commandTimeout, commandType, cancellationToken: cancellationToken);
            return connection.QueryMultipleAsync(command);
        }

        var context = new Context
        {
            ["sql"] = sql
        };

        return policy.ExecuteAsync((_, token) =>
        {
            var command = new CommandDefinition(sql, parameters, transaction, commandTimeout, commandType, cancellationToken: token);
            return connection.QueryMultipleAsync(command);
        }, context, cancellationToken);
    }

    internal static Task<int> ExecuteAsync(
       this IDbConnection cnn,
       string sql,
       object? param,
       IDbTransaction? transaction,
       int? commandTimeout,
       CommandType? commandType,
       IAsyncPolicy? policy = null,
       CancellationToken cancellationToken = default)
    {
        if (policy == null || transaction != null)
        {
            var def = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, cancellationToken: cancellationToken);
            return cnn.ExecuteAsync(def);
        }

        var context = new Context
        {
            ["sql"] = sql
        };

        return policy.ExecuteAsync((_, t) =>
        {
            var def = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, cancellationToken: t);
            return cnn.ExecuteAsync(def);
        }, context, cancellationToken);
    }
}

using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Registry;
using Polly.Retry;
using Polly.Wrap;
using VPay.Ols.Processor.Data.Connection;

namespace VPay.Ols.Processor.Data.Sql;

public sealed class SqlRetryPolicyRegistry : IRetryPolicyRegistry
{
    #region Constants

    private const string StandardRetryPolicy = "StandardRetryPolicy";
    private const string StandardSaveQueryRetryPolicy = "StandardSaveQueryRetryPolicy";
    private const string OpenConnectionRetryPolicy = "OpenConnectionRetryPolicy";

    private const int TimeoutError = -2;
    private const int DeadlockError = 1205;
    private const int KillState = 596;
    private const int RoleError = 983;
    private const int AccessibilityError = 978;

    private const int ShortRetryDelayInMilliseconds = 200;
    private const int LongRetryDelayInSeconds = 5;
    private const int RetryCount = 2;

    #endregion Constants

    private readonly PolicyRegistry _registry;
    private readonly ILogger _logger;

    public SqlRetryPolicyRegistry(ILogger<SqlRetryPolicyRegistry> logger)
    {
        _logger = logger;
        _registry = new PolicyRegistry();
    }

    /// <summary>
    /// This will get the policy that will retry 2 times when the following sql errors occur:
    /// Timeout errors (Number = -2) or deadlock errors (Number = 1205) or
    /// Cannot continue the execution because the session is in the kill state (Number = 596)
    /// </summary>
    /// <returns></returns>
    public IAsyncPolicy GetStandardRetryPolicy()
    {
        if (_registry.ContainsKey(StandardRetryPolicy))
        {
            return _registry.Get<IAsyncPolicy>(StandardRetryPolicy);
        }

        AsyncRetryPolicy waitAndRetryPolicy = Policy
            .Handle<SqlException>(x => x.Number == TimeoutError || x.Number == DeadlockError)
            .WaitAndRetryAsync(RetryCount,
                retryAttempt => TimeSpan.FromMilliseconds(ShortRetryDelayInMilliseconds * retryAttempt),
                onRetry: (exception, calculatedWaitDuration, retryCount, context) =>
                {
                    object sql = context.ContainsKey("sql") ? context["sql"] : "";

                    using (_logger.BeginScope(new Dictionary<string, object> { ["sql"] = sql }))
                    {
                        _logger.LogInformation(exception, "DB Retry {RetryCount} occurred after {timeSpan}", retryCount, calculatedWaitDuration);
                    }
                });

        // This error happens during the fail over and takes longer so have it in a different policy
        AsyncRetryPolicy waitAndRetryPolicyForSessionKillState = Policy
            .Handle<SqlException>(x => x.Number == KillState)
            .WaitAndRetryAsync(RetryCount,
                retryAttempt => TimeSpan.FromSeconds(LongRetryDelayInSeconds + retryAttempt),
                onRetry: (exception, calculatedWaitDuration, retryCount, context) =>
                {
                    object sql = context.ContainsKey("sql") ? context["sql"] : "";
                    using (_logger.BeginScope(new Dictionary<string, object> { ["sql"] = sql }))
                    {
                        _logger.LogInformation(exception, "DB Retry {RetryCount} occurred after {timeSpan}", retryCount, calculatedWaitDuration);
                    }
                });

        var setup = Policy.WrapAsync(waitAndRetryPolicyForSessionKillState, waitAndRetryPolicy);

        //using setter will use TryAdd behind the scenes and prevent duplicate key error
        _registry[StandardRetryPolicy] = setup;

        return setup;
    }

    /// <summary>
    /// This will get the policy that will retry 2 times when the following sql errors occur:
    /// Deadlock errors (Number = 1205) or
    /// Cannot continue the execution because the session is in the kill state (Number = 596)
    /// </summary>
    /// <returns></returns>
    public IAsyncPolicy GetStandardSaveQueryRetryPolicy()
    {
        if (_registry.ContainsKey(StandardSaveQueryRetryPolicy))
        {
            return _registry.Get<IAsyncPolicy>(StandardSaveQueryRetryPolicy);
        }

        AsyncRetryPolicy waitAndRetryPolicy = Policy
            .Handle<SqlException>(x => x.Number == DeadlockError)
            .WaitAndRetryAsync(RetryCount,
                retryAttempt => TimeSpan.FromMilliseconds(ShortRetryDelayInMilliseconds * retryAttempt),
                onRetry: (exception, calculatedWaitDuration, retryCount, context) =>
                {
                    object sql = context.ContainsKey("sql") ? context["sql"] : "";
                    using (_logger.BeginScope(new Dictionary<string, object> { ["sql"] = sql }))
                    {
                        _logger.LogInformation(exception, "DB Retry {RetryCount} occurred after {timeSpan}", retryCount, calculatedWaitDuration);
                    }
                });

        // This error happens during the fail over and takes longer so have it in a different policy
        AsyncRetryPolicy waitAndRetryPolicyForError596 = Policy
            .Handle<SqlException>(x => x.Number == KillState)
            .WaitAndRetryAsync(RetryCount,
                retryAttempt => TimeSpan.FromSeconds(LongRetryDelayInSeconds + retryAttempt),
                onRetry: (exception, calculatedWaitDuration, retryCount, context) =>
                {
                    object sql = context.ContainsKey("sql") ? context["sql"] : "";
                    using (_logger.BeginScope(new Dictionary<string, object> { ["sql"] = sql }))
                    {
                        _logger.LogInformation(exception, "DB Retry {RetryCount} occurred after {timeSpan}", retryCount, calculatedWaitDuration);
                    }
                });

        AsyncPolicyWrap setup = Policy.WrapAsync(waitAndRetryPolicyForError596, waitAndRetryPolicy);

        //using setter will use TryAdd behind the scenes and prevent duplicate key error
        _registry[StandardSaveQueryRetryPolicy] = setup;

        return setup;
    }

    /// <summary>
    /// This will get the policy that will retry 2 times when the following sql errors occur:
    /// Unable to access availability database because the database replica is not in the PRIMARY or SECONDARY role (Number = 983)
    /// Target database is in an availability group and currently accessible for connections when the application intent is set to read only. (Number = 978)
    /// </summary>
    /// <returns></returns>
    public IAsyncPolicy GetOpenConnectionRetryPolicy()
    {
        if (_registry.ContainsKey(OpenConnectionRetryPolicy))
        {
            return _registry.Get<IAsyncPolicy>(OpenConnectionRetryPolicy);
        }

        AsyncRetryPolicy waitAndRetryPolicy = Policy
            .Handle<SqlException>(x => x.Number == RoleError || x.Number == AccessibilityError)
            .WaitAndRetryAsync(RetryCount,
                retryAttempt => TimeSpan.FromSeconds(LongRetryDelayInSeconds + retryAttempt),
                onRetry: (exception, calculatedWaitDuration, retryCount, context) =>
                {
                    _logger.LogInformation(exception, "Open Connection - DB Retry {RetryCount} occurred after {timeSpan}", retryCount, calculatedWaitDuration);
                });

        //using setter will use TryAdd behind the scenes and prevent duplicate key error
        _registry[OpenConnectionRetryPolicy] = waitAndRetryPolicy;

        return waitAndRetryPolicy;
    }
}

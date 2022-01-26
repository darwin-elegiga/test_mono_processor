using System;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using VPay.Ols.Processor.Data.Connection;

namespace VPay.Ols.Processor.Data.Sql.DependencyInjection;

/// <summary>
/// Extensions related to dependency injection for this assembly.
/// </summary>
public static class SqlConfigurationExtension
{
    /// <summary>
    /// Add the SQL services to the service collection.
    /// </summary>
    /// <param name="serviceCollection">The collection to add the services to</param>
    /// <param name="configureConnection">An action that takes the connection to configure.</param>
    /// <returns>The <see cref="IServiceCollection" /> into which any configuration options should be registered.</returns>
    public static IServiceCollection AddSql(this IServiceCollection serviceCollection, Action<ConnectionConfig>? configureConnection = null)
    {
        serviceCollection.TryAddScoped<IDataConnection<SqlConnection>, SqlDataConnection>();
        serviceCollection.TryAddSingleton<IRetryPolicyRegistry, SqlRetryPolicyRegistry>();        

        if (configureConnection != null)
        {
            serviceCollection
                .Configure(configureConnection)
                .AddTransient(cfg => cfg.GetRequiredService<IOptions<ConnectionConfig>>().Value);
        }

        return serviceCollection;
    }
}

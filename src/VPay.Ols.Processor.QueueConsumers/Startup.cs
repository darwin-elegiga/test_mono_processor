using System.IO.Abstractions;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using VPay.MassTransit.Abstractions;
using VPay.MassTransit.DependencyInjection;
using VPay.Ols.Processor.Commands.OlsFile;
using VPay.Ols.Processor.Data.Sql.DependencyInjection;
using VPay.Ols.Processor.Hashing;
using VPay.Ols.Processor.Models.Authorization;
using VPay.Ols.Processor.Models.NonFinancial;
using VPay.Ols.Processor.Models.PostedTransactions;
using VPay.Ols.Processor.Parsers;
using VPay.Ols.Processor.QueueConsumers.Consumers;
using VPay.Ols.Processor.QueueConsumers.Consumers.NonFinancialFile;
using VPay.Ols.Processor.Writers;

namespace VPay.Ols.Processor.QueueConsumers;

public static class Startup
{
    public static void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
    {
        services.AddMediatR(typeof(AddOlsFile.Handler).Assembly);
        services.AddMediatR(typeof(ProcessNonFinancialFile.Command).Assembly);

        var rabbitConfig = new RabbitMqConfig();
        hostContext.Configuration.Bind("OlsProcessorQueue", rabbitConfig);

        services.Configure<NonFinancialFileConsumerSettings>(hostContext.Configuration.GetSection("NonFinancialFileConsumerSettings"));
        services.AddTransient(cfg => cfg.GetService<IOptions<NonFinancialFileConsumerSettings>>()!.Value);

        services.UseMassTransit(rabbitConfig, opts =>
        {
            opts.AddConsumer<AuthorizationFileConsumer>();
            opts.AddConsumer<PostedTransactionFileProcessedConsumer>();
            opts.AddConsumer<NonFinancialFileProcessedConsumer>();
        });

        services.AddMassTransitHostedService();

        services.AddSql(config => hostContext.Configuration.Bind("OlsDatabase", config));

        services.AddSingleton<IFileSystem, FileSystem>();

        services.AddConfigurationSettings<PostedTransactionsFileSettings>(hostContext.Configuration);
        services.AddConfigurationSettings<NonFinancialFileSettings>(hostContext.Configuration);
        services.AddConfigurationSettings<AuthorizationFileSettings>(hostContext.Configuration);

        services.AddTransient(typeof(IHashingService<>), typeof(HashingService<>));
        services.AddTransient<IAuthorizationParser, AuthorizationParser>();
        services.AddTransient<IAuthorizationFileWriter, AuthorizationFileWriter>();
        services.AddTransient<IPostedTransactionsParser, PostedTransactionsParser>();
        services.AddTransient<IPostedTransactionFileWriter, OptumPostedTransactionFileWriter>();
        services.AddTransient<INonFinancialParser, NonFinancialParser>();
        services.AddTransient<INonFinancialFileWriter, OptumNonFinancialFileWriter>();
    }
    
    private static void AddConfigurationSettings<TFileSettings>(this IServiceCollection services, IConfiguration configuration)
        where TFileSettings : class
    {
        string settingsName = typeof(TFileSettings).Name;

        services.Configure<TFileSettings>(configuration.GetSection(settingsName));
        services.AddTransient(cfg => cfg.GetRequiredService<IOptions<TFileSettings>>().Value);
    }
}

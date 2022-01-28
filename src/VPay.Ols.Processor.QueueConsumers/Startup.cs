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
using VPay.Ols.Processor.Models.PostedTransactions;
using VPay.Ols.Processor.Parsers;
using VPay.Ols.Processor.QueueConsumers.Consumers;
using VPay.Ols.Processor.Writers;

namespace VPay.Ols.Processor.QueueConsumers;

public static class Startup
{
    public static void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
    {
        services.AddMediatR(typeof(AddOlsFile.Handler).Assembly);        

        var rabbitConfig = new RabbitMqConfig();
        hostContext.Configuration.Bind("OlsProcessorQueue", rabbitConfig);

        services.UseMassTransit(rabbitConfig, opts =>
        {
            opts.AddConsumer<PostedTransactionFileProcessedConsumer>();          
        });

        services.AddMassTransitHostedService();

        services.AddSql(config => hostContext.Configuration.Bind("OlsDatabase", config));

        services.AddSingleton<IFileSystem, FileSystem>();

        services.Configure<PostedTransactionsFileSettings>(hostContext.Configuration.GetSection("PostedTransactionsFileSettings"));
        services.AddTransient(cfg => cfg.GetService<IOptions<PostedTransactionsFileSettings>>().Value);

        services.AddTransient(typeof(IHashingService<>), typeof(HashingService<>));
        services.AddTransient<IPostedTransactionsParser, PostedTransactionsParser>();
        services.AddTransient<IPostedTransactionFileWriter, OptumPostedTransactionFileWriter>();
    }
}

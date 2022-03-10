using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using VPay.FileTransfer.Messaging;
using VPay.Ols.Processor.Models;

namespace VPay.Ols.Processor.QueueConsumers.FileTransfer;

public static class FileTransferExtensions
{
    public static IServiceCollection ConfigureFileTransferServices(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .Configure<FileTransferServiceSettings>(configuration.GetSection("FileTransferServiceSettings"))
            .AddTransient(cfg => cfg.GetRequiredService<IOptions<FileTransferServiceSettings>>().Value);

        // add the service used to sign the file hash before sending the file to the File Transfer Service
        services
            .AddHashingService()
            .AddSigningService(options => configuration.Bind("SigningServiceOptions", options));

        // register the handler that will consume the FileReadyForTransferNotification
        services.ConfigureFileReadyForTransferNotificationPublisher(cfg =>
        {
            cfg.AddConsumer<FileReadyForTransferNotificationHandler>();
        });


        // register the objects that will consume the post-transfer message
        services.ConfigureFileTransferServiceBus(
            configureRabbitMq =>
            {
                configuration.Bind("FileTransferServiceRabbitQueue", configureRabbitMq);
            },
            addConsumers =>
            {
                // NOTE: Nothing to do here at the moment.  We could track when the file gets transferred, but we don't have
                // anything in place to track that within this system at the moment
            });

        return services;
    }
}

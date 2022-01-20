using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VPay.Extensions.Logging.GrayLog;

namespace VPay.Ols.Processor.QueueConsumers;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(config =>
            {
                config
                    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
                    .AddJsonFile("appsettings.secure.json", optional: true, reloadOnChange: true);
            })
            .ConfigureLogging(logging =>
            {
                logging
                    .ClearProviders()
                    .AddVPayGrayLog();
            })
            .ConfigureServices(Startup.ConfigureServices);
}

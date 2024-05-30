using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using VideoProcessingService.MassTransit.Events;
using VideoProcessingService.MassTransit.JobConsumers;
using VideoProcessingService.Services;

namespace VideoProcessingService
{
    /// <summary>
    /// Program
    /// </summary>
    public class Program
    {
        static void Main(string[] args)
        {
            AppStartup(args).Run();
        }

        /// <summary>
        /// Configure App
        /// </summary>
        public static IHost AppStartup(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddUserSecrets<Program>()
                .AddEnvironmentVariables()
                .Build();

            var host = Host.CreateDefaultBuilder(args);

            host.ConfigureAppConfiguration(app =>
            {
                app.Sources.Clear();
                app.AddConfiguration(configuration);    
            });

            Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(configuration)
                    .CreateBootstrapLogger();

            host.UseSerilog();

            host.ConfigureServices((context, services) =>
            {
                services.AddSingleton<MinioService>();

                services.AddMassTransit(options =>
                {
                    options.AddConsumer<VideoProcessingJobConsumer>(config =>
                    {
                        config.UseConcurrencyLimit(1);
                    });

                    options.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("video-processing-service", false));

                    options.UsingRabbitMq((context, config) =>
                    {
                        var host = configuration.GetSection("RabbitMq:Host").Get<string>();
                        var virtualHost = configuration.GetSection("RabbitMq:VirtualHost").Get<string>();

                        config.Host(host, virtualHost, host =>
                        {
                            host.Username(configuration.GetSection("RabbitMq:Username").Get<string>());
                            host.Password(configuration.GetSection("RabbitMq:Password").Get<string>());
                        });

                        config.ConfigureEndpoints(context);
                    });
                });
            });

            return host.Build();
        }

    }
}
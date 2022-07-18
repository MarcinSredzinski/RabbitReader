using JwtAuth.Library.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using RabbitBase.Library.Contracts;
using RabbitBase.Library.RabbitMQ;
using RabbitReader.API;
using Serilog;
using ILogger = Serilog.ILogger;

namespace RabbitReader
{
    internal static class Startup
    {
        internal static void ConfigureServices(IServiceCollection services)
        {
            services
                .AddHttpClient()
                .AddScoped<ILogger>(x => Log.Logger)
                .AddScoped<IApiClient, AuthorizedApiClient>()
                //.AddSingleton<IApiClient, ApiClient>()
                .AddSingleton<IMessageReceivedHandler, ApiHandler>()
                .AddSingleton<IAuthorizationService, AuthorizationService>()
                .AddSingleton<IQueueReaderDeclaration, QueueReaderDeclaration>();
        }

        internal static void BuildConfiguration(IConfigurationBuilder configurationBuilder)
        {
            configurationBuilder
                .AddJsonFile("appsettings.json");
        }
        internal static ILoggingBuilder ConfigureLogger(this ILoggingBuilder loggingBuilder)
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string logPath = Path.Combine(basePath, "logs", "my_logNew.log");
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? string.Empty;

            Log.Logger = new LoggerConfiguration()
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
            .ConfigureForEnvironment(environment, logPath)
            .CreateLogger();
            loggingBuilder.AddSerilog(Log.Logger);
            return loggingBuilder;
        }

        private static LoggerConfiguration ConfigureForEnvironment(this LoggerConfiguration loggerSinkConfiguration, string environment, string logPath)
        => environment switch
        {
            "Development" => loggerSinkConfiguration.WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
                .WriteTo.Console()
                .MinimumLevel.Debug(),
            "Testing" => loggerSinkConfiguration.WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
                .WriteTo.Console()
                .MinimumLevel.Information(),
            "Production" => loggerSinkConfiguration.WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
                .MinimumLevel.Warning(),
            _ => loggerSinkConfiguration
                .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
                .WriteTo.Console()
                .MinimumLevel.Debug(),
        };
    }
}

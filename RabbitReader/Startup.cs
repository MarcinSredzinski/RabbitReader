using Core.Library.Api;
using JwtAuth.Library.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
                .AddScoped<ILogger>(x => Log.Logger)
                .AddSingleton<HttpClient>()
                .AddSingleton<IApiClient, ApiClient>()
                .AddSingleton<IApiHandler, ApiHandler>()
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
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
                .WriteTo.Console()
                .MinimumLevel.Debug()                                               //ToDo make dependent on the current environment.
            .CreateLogger();
            loggingBuilder.AddSerilog(Log.Logger);
            return loggingBuilder;
        }
    }
}

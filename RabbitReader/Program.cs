using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitReader;
using RabbitBase.Library.RabbitMQ;
using RabbitBase.Library.Contracts;

Console.WriteLine("Rabbit Reader starting.");
var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(Startup.BuildConfiguration)
    .ConfigureLogging(x => x.ConfigureLogger())
    .ConfigureServices((_, services) => Startup.ConfigureServices(services))
    .Build();

var apiHandler = host.Services.GetService<IMessageReceivedHandler>();
var queue = host.Services.GetService<IQueueReaderDeclaration>();
if (apiHandler == null || queue == null)
{
    throw new Exception("Queue or api handler did not initialize properly.");
}

queue.Declare(apiHandler.OnMessageReceived);

Console.WriteLine(" Press enter to exit.");
Console.ReadLine();
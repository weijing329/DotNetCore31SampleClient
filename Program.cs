using DotNetCore31SampleClient.Example;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DotNetCore31SampleClient
{
  class Program
  {
    public static void Main(string[] args)
    {
      CreateHostBuilder(args).Build().Run();
    }
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((_, services) =>
            {
              services.AddLogging();
              services.AddSingleton<IGoogleCloudPubSubClient>(serviceProvider =>
                new GoogleCloudPubSubClient(serviceProvider.GetService<ILoggerFactory>().CreateLogger<GoogleCloudPubSubClient>()));
              services.AddSingleton<IReactiveRpcClient>(serviceProvider =>
                new ReactiveRpcClient(
                  serviceProvider.GetService<ILoggerFactory>().CreateLogger<ReactiveRpcClient>(),
                  serviceProvider.GetRequiredService<IGoogleCloudPubSubClient>()));
              services.AddHostedService<HostedService>();
            });
  }
}

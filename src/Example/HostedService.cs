using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DotNetCore31SampleClient.Example
{
  public class HostedService : IHostedService
  {
    private readonly ILogger _logger;
    private IReactiveRpcClient _reactiveRpcClient;

    public HostedService(
        ILogger<HostedService> logger,
        IHostApplicationLifetime appLifetime,
        IReactiveRpcClient reactiveRpcClient)
    {
      _logger = logger;
      _reactiveRpcClient = reactiveRpcClient;

      appLifetime.ApplicationStarted.Register(OnStarted);
      appLifetime.ApplicationStopping.Register(OnStopping);
      appLifetime.ApplicationStopped.Register(OnStopped);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
      _logger.LogInformation("1. StartAsync has been called.");

      return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
      _logger.LogInformation("4. StopAsync has been called.");

      return Task.CompletedTask;
    }

    private void OnStarted()
    {
      _logger.LogInformation("2. OnStarted has been called.");

      // _reactiveRpcClient.RunTest1();
      // _reactiveRpcClient.RunTest2().Wait();
      _reactiveRpcClient.RunTest3().Wait();
    }

    private void OnStopping()
    {
      _logger.LogInformation("3. OnStopping has been called.");
    }

    private void OnStopped()
    {
      _logger.LogInformation("5. OnStopped has been called.");
    }

  }
}
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace DotNetCore31SampleClient.Example.Quartz
{
  [DisallowConcurrentExecution]
  public class PullingJob : IJob
  {
    // private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PullingJob> _logger;

    // public PullingJob(IServiceProvider serviceProvider)
    // {
    //   _serviceProvider = serviceProvider;
    //   _logger = _serviceProvider.GetService<ILoggerFactory>().CreateLogger<PullingJob>();
    // }
    public PullingJob(ILogger<PullingJob> logger)
    {
      _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
      // using (var scope = _serviceProvider.CreateScope())
      // {{
      _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss.ffff")}: {context.JobDetail.Key} job executing, triggered by {context.Trigger.Key}");
      await Task.Delay(TimeSpan.FromSeconds(2));
      // TODO add actual task here
      // }
    }
  }
}
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Quartz;

namespace DotNetCore31SampleClient.Example.Quartz
{
  [DisallowConcurrentExecution]
  public class PullingJob : IJob
  {
    // private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PullingJob> _logger;
    private readonly IGoogleCloudPubSubClient _googleCloudPubSubClient;
    private SourceCache<RemoteTask, Guid> _remoteTasksCache;
    // public PullingJob(IServiceProvider serviceProvider)
    // {
    //   _serviceProvider = serviceProvider;
    //   _logger = _serviceProvider.GetService<ILoggerFactory>().CreateLogger<PullingJob>();
    // }
    public PullingJob(ILogger<PullingJob> logger, IGoogleCloudPubSubClient googleCloudPubSubClient, IReactiveRpcClient reactiveRpcClient)
    {
      _logger = logger;
      _googleCloudPubSubClient = googleCloudPubSubClient;
      _remoteTasksCache = reactiveRpcClient.RemoteTasksCache;
    }

    public async Task Execute(IJobExecutionContext context)
    {
      const string projectId = "sunkang-iot-monitor-service";
      const string subscriptionId = "pull-test-message-in-order";

      // using (var scope = _serviceProvider.CreateScope())
      // {{
      _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss.ffff")}: {context.JobDetail.Key} job executing, triggered by {context.Trigger.Key}");

      // Add PubSub Client integration - Pull messages in order
      _logger.LogInformation($"Start pulling messages from subscription");
      var numberOfMessageProcessed = await _googleCloudPubSubClient.PullMessagesAsync(projectId, subscriptionId, (_, pubsubMessage) =>
      {
        string decodedMessageText = Encoding.UTF8.GetString(pubsubMessage.Data.ToArray());

        // retrieve the custom attributes from metadata
        if (pubsubMessage.Attributes.ContainsKey("type"))
        {
          RemoteTask remoteTask = JsonConvert.DeserializeObject<RemoteTask>(decodedMessageText);
          Console.WriteLine($"Pulled message {decodedMessageText} with messageId {pubsubMessage.MessageId}, OrderingKey: {pubsubMessage.OrderingKey}, Type: {pubsubMessage.Attributes["type"]}");
          if (pubsubMessage.Attributes["type"] == "Response")
          {
            remoteTask.completed = true;
            _remoteTasksCache.AddOrUpdate(remoteTask);
          }
          //  foreach (var attribute in pubsubMessage.Attributes)
          //  {
          //    Console.WriteLine($"{attribute.Key} = {attribute.Value}");
          //  }
        }
      }, 5000);
      _logger.LogInformation($"Total {numberOfMessageProcessed} messages processed");
    }
  }
}
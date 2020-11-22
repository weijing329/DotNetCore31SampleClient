using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetCore31SampleClient.Example.Dto;
using DotNetCore31SampleClient.Example.GoogleCloud.PubSub;
using DynamicData;
using Google.Protobuf.Collections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Quartz;

namespace DotNetCore31SampleClient.Example.Quartz
{
  public class PublishingJob : IJob
  {
    // private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PublishingJob> _logger;
    private readonly IPubSubClient _googleCloudPubSubClient;
    private SourceCache<RemoteTask, Guid> _remoteTasksCache;
    // public PullingJob(IServiceProvider serviceProvider)
    // {
    //   _serviceProvider = serviceProvider;
    //   _logger = _serviceProvider.GetService<ILoggerFactory>().CreateLogger<PullingJob>();
    // }
    public PublishingJob(ILogger<PublishingJob> logger, IPubSubClient googleCloudPubSubClient, IReactiveRpcClient reactiveRpcClient)
    {
      _logger = logger;
      _googleCloudPubSubClient = googleCloudPubSubClient;
      _remoteTasksCache = reactiveRpcClient.RemoteTasksCache;
    }

    public async Task Execute(IJobExecutionContext context)
    {
      const string projectId = "sunkang-iot-monitor-service";
      const string topicId = "test";

      // add task to cache
      RemoteTask newRemoteTask = new RemoteTask() { id = Guid.NewGuid(), name = $"task-{DateTime.Now.ToString("mssfff")}", completed = false };
      _remoteTasksCache.AddOrUpdate(newRemoteTask);

      // Manual test on the same Topic
      // Add PubSub Client integration - Publish messages with ordering key
      var messagesWithOrderingKey = new List<(string, string, MapField<string, string>)>()
        {
          ("OrderingKey", JsonConvert.SerializeObject(newRemoteTask).ToString(), new MapField<string, string>{{"type", "Response"}}),
        };
      await _googleCloudPubSubClient.PublishOrderedMessagesAsync(projectId, topicId, messagesWithOrderingKey);
    }
  }
}
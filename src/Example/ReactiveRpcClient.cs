using DynamicData;
using System.Reactive.Concurrency;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Google.Protobuf.Collections;
using System.Text;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace DotNetCore31SampleClient.Example
{
  public class RemoteTask
  {
    public Guid id;
    public string name;
    public bool completed;
  }

  public class ReactiveRpcClient : IReactiveRpcClient
  {
    private readonly ILogger<ReactiveRpcClient> _logger;
    private IGoogleCloudPubSubClient _googleCloudPubSubClient;

    private SourceCache<RemoteTask, Guid> _remoteTasksCache;

    public SourceCache<RemoteTask, Guid> RemoteTasksCache => _remoteTasksCache;

    private TimeSpan? RemoveFunc(RemoteTask t)
    {
      if (t.completed)
      {
        return TimeSpan.FromMilliseconds(0);
      }

      return null;
    }

    public ReactiveRpcClient(ILogger<ReactiveRpcClient> logger, IGoogleCloudPubSubClient googleCloudPubSubClient)
    {
      _logger = logger;
      _googleCloudPubSubClient = googleCloudPubSubClient;
      // initialize cache
      _remoteTasksCache = new SourceCache<RemoteTask, Guid>(remoteTask => remoteTask.id);

      // subscribe to task id events
      _remoteTasksCache.Connect()
                            // .Filter(remoteTask => remoteTask.id == remoteTask1.id)
                            .OnItemAdded(remoteTask =>
                            {
                              Console.WriteLine($"Added remoteTask: {remoteTask.name}");
                            })
                            .OnItemUpdated((current, previous) =>
                            {
                              Console.WriteLine($"Updated remoteTask: {current.name}, completed = {current.completed}");
                              if (current.completed)
                              {
                                this._remoteTasksCache.Remove(current);
                              }
                            })
                            .OnItemRemoved(remoteTask =>
                            {
                              Console.WriteLine($"Removed remoteTask: {remoteTask.name}");
                            })
                            .Subscribe();
    }

    public void RunTest1()
    {
      // add task to cache
      RemoteTask remoteTask1 = new RemoteTask() { id = Guid.NewGuid(), name = "task1", completed = false };
      _remoteTasksCache.AddOrUpdate(remoteTask1);

      // update task
      remoteTask1.completed = true;
      _remoteTasksCache.AddOrUpdate(remoteTask1);

      // ExpireAfter seems only to work when all caches meet the remove function condition
      // if one return null (no expiry), no cache will be deleted (bug?)
      // this behavior can be reproduced by commenting out 
      // remoteTask1.completed = true;
      var _remover = _remoteTasksCache.ExpireAfter(RemoveFunc, Scheduler.Default).Subscribe();

      // task2
      RemoteTask remoteTask2 = new RemoteTask() { id = Guid.NewGuid(), name = "task2", completed = true };
      _remoteTasksCache.AddOrUpdate(remoteTask2);
    }

    public async Task RunTest2()
    {
      const string projectId = "sunkang-iot-monitor-service";
      const string topicId = "test";
      const string subscriptionId = "pull-test-message-in-order";

      // add task to cache
      RemoteTask remoteTask1 = new RemoteTask() { id = Guid.NewGuid(), name = "task1", completed = false };
      _remoteTasksCache.AddOrUpdate(remoteTask1);
      RemoteTask remoteTask2 = new RemoteTask() { id = Guid.NewGuid(), name = "task2", completed = false };
      _remoteTasksCache.AddOrUpdate(remoteTask2);

      // Manual test on the same Topic
      // Add PubSub Client integration - Publish messages with ordering key
      var messagesWithOrderingKey = new List<(string, string, MapField<string, string>)>()
      {
        ("OrderingKey2", JsonConvert.SerializeObject(remoteTask2).ToString(), new MapField<string, string>{{"type", "Notify"}}),
        ("OrderingKey1", JsonConvert.SerializeObject(remoteTask1).ToString(), new MapField<string, string>{{"type", "Response"}}),
        ("OrderingKey2", JsonConvert.SerializeObject(remoteTask2).ToString(), new MapField<string, string>{{"type", "Notify"}}),
        ("OrderingKey1", JsonConvert.SerializeObject(remoteTask1).ToString(), new MapField<string, string>{{"type", "Notify"}}),
        ("OrderingKey2", JsonConvert.SerializeObject(remoteTask2).ToString(), new MapField<string, string>{{"type", "Response"}}),
      };
      _remoteTasksCache.AddOrUpdate(remoteTask1);
      _remoteTasksCache.AddOrUpdate(remoteTask2);
      await _googleCloudPubSubClient.PublishOrderedMessagesAsync(projectId, topicId, messagesWithOrderingKey);

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
       }, 10000);
      _logger.LogInformation($"Total {numberOfMessageProcessed} messages processed");
    }

    public async Task RunTest3()
    {
      // Manual test with Subscriber and Scheduled pulling job
      // Add PubSub Client integration - Pull messages schedule job
      // DONE with Quartz PullingJob

      // Add PubSub Client integration - Publish messages 
      // publish new message every 0.5 second
      // DONE with Quartz PublishingJob
      await Task.Delay(TimeSpan.FromSeconds(0));
    }
  }
}
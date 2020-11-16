using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Cloud.PubSub.V1;
using Google.Protobuf.Collections;
using Microsoft.Extensions.Logging;

namespace DotNetCore31SampleClient.Example
{
  public interface IGoogleCloudPubSubClient
  {
    Task<int> PublishOrderedMessagesAsync(string projectId, string topicId, IEnumerable<(string, string, MapField<string, string>)> keysMessagesAndAttributes);
    Task<int> PullMessagesAsync(string projectId, string subscriptionId, Action<ILogger, PubsubMessage> messageProcess, int pullForMilliseconds = 5000, bool acknowledge = true);
  }
}
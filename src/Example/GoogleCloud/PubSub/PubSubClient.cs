using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetCore31SampleClient.Example.GoogleCloud.PubSub
{
  public class PubSubClient : IPubSubClient
  {
    private readonly ILogger<PubSubClient> _logger;

    public PubSubClient(ILogger<PubSubClient> logger)
    {
      _logger = logger;
    }

    public async Task<int> PublishOrderedMessagesAsync(string projectId, string topicId, IEnumerable<(string, string, MapField<string, string>)> keysMessagesAndAttributes)
    {
      TopicName topicName = TopicName.FromProjectTopic(projectId, topicId);

      var customSettings = new PublisherClient.Settings
      {
        EnableMessageOrdering = true
      };

      int publishedMessageCount = 0;

      try
      {
        PublisherClient publisher = await PublisherClient.CreateAsync(topicName, settings: customSettings);

        var publishTasks = keysMessagesAndAttributes.Select(async keyMessageAndAttributes =>
        {
          var (orderingKey, messageText, attributes) = keyMessageAndAttributes;
          var pubsubMessage = new PubsubMessage
          {
            OrderingKey = orderingKey,
            Data = ByteString.CopyFromUtf8(messageText),
            Attributes = {
          attributes,
            }
          };
          try
          {
            string messageId = await publisher.PublishAsync(pubsubMessage);
            Console.WriteLine($"Published message {messageText} with messageId {messageId}");
            Interlocked.Increment(ref publishedMessageCount);
          }
          catch (Exception exception)
          {
            _logger.LogError($"An error occurred when publishing message {messageText}: {exception.Message}");
          }
        });

        await Task.WhenAll(publishTasks);
      }
      catch (System.Exception exception)
      {
        _logger.LogError($"An error occurred when creating publisher client: {exception.Message}");
      }

      return publishedMessageCount;
    }


    public async Task<int> PullMessagesAsync(string projectId, string subscriptionId, Action<ILogger, PubsubMessage> messageProcess, int pullForMilliseconds = 5000, bool acknowledge = true)
    {
      int messageCount = 0;

      try
      {
        SubscriptionName subscriptionName = SubscriptionName.FromProjectSubscription(projectId, subscriptionId);
        SubscriberClient subscriber = await SubscriberClient.CreateAsync(subscriptionName);

        // SubscriberClient runs your message handle function on multiple
        // threads to maximize throughput.
        try
        {
          Task startTask = subscriber.StartAsync((PubsubMessage message, CancellationToken cancel) =>
          {
            messageProcess(this._logger, message);
            // string decodedMessageText = Encoding.UTF8.GetString(message.Data.ToArray());
            // Console.WriteLine($"Message {message.MessageId}: {decodedMessageText}");
            // // retrieve the custom attributes from metadata
            // if (message.Attributes != null)
            // {
            //   foreach (var attribute in message.Attributes)
            //   {
            //     Console.WriteLine($"{attribute.Key} = {attribute.Value}");
            //   }
            // }
            Interlocked.Increment(ref messageCount);
            return Task.FromResult(acknowledge ? SubscriberClient.Reply.Ack : SubscriberClient.Reply.Nack);
          });
          // Run for 5 seconds.
          await Task.Delay(pullForMilliseconds);
          await subscriber.StopAsync(CancellationToken.None);
          // Lets make sure that the start task finished successfully after the call to stop.
          await startTask;
        }
        catch (Exception exception)
        {
          _logger.LogError($"An error occurred when pulling messages from subscription: {exception.Message}");
          // throw;
        }

      }
      catch (System.Exception exception)
      {
        _logger.LogError($"An error occurred when creating subscriber client: {exception.Message}");
      }

      return messageCount;
    }
  }
}
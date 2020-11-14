using Google.Cloud.PubSub.V1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class ExamplePubSubClient
{
  public static async Task<int> PublishOrderedMessagesAsync(string projectId, string topicId, IEnumerable<(string, string)> keysAndMessages)
  {
    TopicName topicName = TopicName.FromProjectTopic(projectId, topicId);

    var customSettings = new PublisherClient.Settings
    {
      EnableMessageOrdering = true
    };

    PublisherClient publisher = await PublisherClient.CreateAsync(topicName, settings: customSettings);

    int publishedMessageCount = 0;
    var publishTasks = keysAndMessages.Select(async keyAndMessage =>
    {
      var (orderingKey, message) = keyAndMessage;
      try
      {
        string messageId = await publisher.PublishAsync(orderingKey, message);
        Console.WriteLine($"Published message {message} with messageId {messageId}");
        Interlocked.Increment(ref publishedMessageCount);
      }
      catch (Exception exception)
      {
        Console.WriteLine($"An error occurred when publishing message {message}: {exception.Message}");
      }
    });
    await Task.WhenAll(publishTasks);
    return publishedMessageCount;
  }
  public static async Task<int> PullMessagesAsync(string projectId, string subscriptionId, bool acknowledge = true)
  {
    SubscriptionName subscriptionName = SubscriptionName.FromProjectSubscription(projectId, subscriptionId);
    SubscriberClient subscriber = await SubscriberClient.CreateAsync(subscriptionName);
    // SubscriberClient runs your message handle function on multiple
    // threads to maximize throughput.
    int messageCount = 0;
    Task startTask = subscriber.StartAsync((PubsubMessage message, CancellationToken cancel) =>
    {
      string text = Encoding.UTF8.GetString(message.Data.ToArray());
      Console.WriteLine($"Message {message.MessageId}: {text}");
      // retrieve the custom attributes from metadata
      if (message.Attributes != null)
      {
        foreach (var attribute in message.Attributes)
        {
          Console.WriteLine($"{attribute.Key} = {attribute.Value}");
        }
      }
      Interlocked.Increment(ref messageCount);
      return Task.FromResult(acknowledge ? SubscriberClient.Reply.Ack : SubscriberClient.Reply.Nack);
    });
    // Run for 5 seconds.
    await Task.Delay(5000);
    await subscriber.StopAsync(CancellationToken.None);
    // Lets make sure that the start task finished successfully after the call to stop.
    await startTask;
    return messageCount;
  }
}
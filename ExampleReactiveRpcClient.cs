using DynamicData;
using System.Reactive.Concurrency;
using System;
using System.Threading.Tasks;

namespace DotNetCoreGoogleCloudPubSubSimpleClient
{
  public class RemoteTask
  {
    public Guid id;
    public string name;
    public bool completed;
  }

  public class ExampleReactiveRpcClient
  {
    SourceCache<RemoteTask, Guid> remoteTasksCache;

    TimeSpan? RemoveFunc(RemoteTask t)
    {
      if (t.completed)
      {
        return TimeSpan.FromMilliseconds(0);
      }

      return null;
    }

    public ExampleReactiveRpcClient()
    {
      // initialize cache
      this.remoteTasksCache = new SourceCache<RemoteTask, Guid>(remoteTask => remoteTask.id);

      // subscribe to task id events
      this.remoteTasksCache.Connect()
                            // .Filter(remoteTask => remoteTask.id == remoteTask1.id)
                            .OnItemAdded(remoteTask =>
                            {
                              Console.WriteLine($"Added remoteTask: {remoteTask.name}");
                            })
                            .OnItemUpdated((current, previous) =>
                            {
                              Console.WriteLine($"Updated remoteTask: {current.name}, completed = {current.completed}");
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
      this.remoteTasksCache.AddOrUpdate(remoteTask1);

      // update task
      remoteTask1.completed = true;
      this.remoteTasksCache.AddOrUpdate(remoteTask1);

      // ExpireAfter seems only to work when all caches meet the remove function condition
      // if one return null (no expiry), no cache will be deleted (bug?)
      // this behavior can be reproduced by commenting out 
      // remoteTask1.completed = true;
      var _remover = this.remoteTasksCache.ExpireAfter(RemoveFunc, Scheduler.Default).Subscribe();

      // task2
      RemoteTask remoteTask2 = new RemoteTask() { id = Guid.NewGuid(), name = "task2", completed = true };
      this.remoteTasksCache.AddOrUpdate(remoteTask2);
    }

    public async Task RunTest2()
    {
      // Manual test on the same Topic
      // TODO Add PubSub Client integration - Publish messages with ordering key
      // TODO Add PubSub Client integration - Pull messages in order
    }

    public async Task RunTest3()
    {
      // Manual test with Subscriber and Scheduled pulling job
      // TODO Add PubSub Client integration - Pull messages schedule job
      // TODO Add PubSub Client integration - Publish messages 
    }
  }
}
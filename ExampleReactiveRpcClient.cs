using DynamicData;
using System.Reactive.Concurrency;
using System;

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
    public static void Run()
    {
      TimeSpan? RemoveFunc(RemoteTask t)
      {
        if (t.completed)
        {
          return TimeSpan.FromMilliseconds(0);
        }

        return null;
      }

      // initialize cache
      var remoteTasksCache = new SourceCache<RemoteTask, Guid>(remoteTask => remoteTask.id);

      // add task to cache
      RemoteTask remoteTask1 = new RemoteTask() { id = Guid.NewGuid(), name = "task1", completed = false };
      remoteTasksCache.AddOrUpdate(remoteTask1);

      // subscribe to task id events
      remoteTasksCache.Connect()
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

      // update task
      remoteTask1.completed = true;
      remoteTasksCache.AddOrUpdate(remoteTask1);

      // ExpireAfter seems only to work when all caches meet the remove function condition
      // if one return null (no expiry), no cache will be deleted (bug?)
      // this behavior can be reproduced by commenting out 
      // remoteTask1.completed = true;
      var _remover = remoteTasksCache.ExpireAfter(RemoveFunc, Scheduler.Default).Subscribe();

      // task2
      RemoteTask remoteTask2 = new RemoteTask() { id = Guid.NewGuid(), name = "task2", completed = true };
      remoteTasksCache.AddOrUpdate(remoteTask2);
    }
  }
}
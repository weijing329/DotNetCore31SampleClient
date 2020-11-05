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

  public class ExampleClient
  {
    public static void Run()
    {
      TimeSpan? RemoveFunc(RemoteTask t)
      {
        if (!t.completed)
        {
          return TimeSpan.FromSeconds(3);
        }

        return null;
      }

      // initialize cache
      var remoteTasksCache = new SourceCache<RemoteTask, Guid>(remoteTask => remoteTask.id);
      var _remover = remoteTasksCache.ExpireAfter(RemoveFunc, Scheduler.Default).Subscribe();

      // add task to cache
      RemoteTask remoteTask1 = new RemoteTask() { id = Guid.NewGuid(), name = "task1", completed = false };
      remoteTasksCache.AddOrUpdate(remoteTask1);

      // subscribe to task id events
      remoteTasksCache.Connect()
                      .Filter(remoteTask => remoteTask.id == remoteTask1.id)
                      .OnItemUpdated((current, previous) =>
                      {
                        Console.WriteLine($"remoteTask: {current.id}, completed = {current.completed}");
                      })
                      .OnItemRemoved(remoteTask =>
                      {
                        Console.WriteLine($"Removing remoteTask: {remoteTask.id}");
                      })
                      .Subscribe();

      // update task by id
      remoteTask1.completed = true;
      remoteTasksCache.AddOrUpdate(remoteTask1);
    }
  }
}
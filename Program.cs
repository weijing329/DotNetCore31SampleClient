using System;
using System.Threading.Tasks;
using DynamicData;

namespace DotNetCoreGoogleCloudPubSubSimpleClient
{
  public class RemoteTask
  {
    public Guid id;
    public string name;
    public bool completed;
  }
  class Program
  {
    static void Main()
    {
      MainAsync().Wait();
    }
    static async Task MainAsync()
    {
      var remoteTasksCache = new SourceCache<RemoteTask, Guid>(remoteTask => remoteTask.id);

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
                      .Subscribe();

      // update task by id
      remoteTask1.completed = true;
      remoteTasksCache.AddOrUpdate(remoteTask1);
    }
  }
}

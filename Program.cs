using System;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using Akavache;

namespace DotNetCoreGoogleCloudPubSubSimpleClient
{
  public class RemoteTask
  {
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
      Akavache.Registrations.Start("DotNetCoreGoogleCloudPubSubSimpleClient");

      var localCache = BlobCache.LocalMachine;

      Console.WriteLine($"LocalMachine.GetAllKeys = {localCache.GetAllKeys().Wait().Count()}");

      Console.WriteLine("Inserting Object");
      RemoteTask task1 = new RemoteTask() { name = "task1", completed = false };
      await localCache.InsertObject<RemoteTask>(key: "task1", task1);

      Console.WriteLine($"LocalMachine.GetAllKeys = {localCache.GetAllKeys().Wait().Count()}");
      // foreach (var key in await BlobCache.LocalMachine.GetAllKeys())
      // {
      //   Console.WriteLine($"key: {key}");
      // }


      // var futureInvalidateObject = localCache.InvalidateObject<RemoteTask>(key: "task1");
      var futureInvalidateObject = localCache.InvalidateObject<RemoteTask>(key: "task1").Publish().RefCount();
      futureInvalidateObject.Subscribe(invalidatedObject =>
      {
        var jsonString = JsonSerializer.Serialize(invalidatedObject, new JsonSerializerOptions() { WriteIndented = true });
        Console.WriteLine($"InvalidatedObject: {jsonString}");
      },
      () => Console.WriteLine("InvalidatedObject: Done!"));


      Console.WriteLine("Invalidating Object");
      // await localCache.InvalidateObject<RemoteTask>(key: "task1").Publish().RefCount();
      await futureInvalidateObject;



      Console.WriteLine($"LocalMachine.GetAllKeys = {localCache.GetAllKeys().Wait().Count()}");
      // var number_of_caches = keys.Count();
      // Console.WriteLine($"LocalMachine.GetAllKeys = {number_of_caches}");
    }
  }
}

using System;
using System.Threading.Tasks;
using DynamicData;

namespace DotNetCore31SampleClient.Example
{
  public interface IReactiveRpcClient
  {
    SourceCache<RemoteTask, Guid> RemoteTasksCache { get; }
    void RunTest1();
    Task RunTest2();
    Task RunTest3();
  }
}
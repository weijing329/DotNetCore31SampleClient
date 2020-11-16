using System.Threading.Tasks;

namespace DotNetCore31SampleClient.Example
{
  public interface IReactiveRpcClient
  {
    void RunTest1();
    Task RunTest2();
    Task RunTest3();
  }
}
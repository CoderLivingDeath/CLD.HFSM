using BenchmarkDotNet.Running;

namespace StateMachineBenchmarkRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<StateMachineBenchmarks>();
        }
    }
}
using BenchmarkDotNet.Running;
using Core.Benchmarks;
using System;
using System.Numerics;
using System.Threading.Tasks;

namespace StateMachineBenchmarkRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<StateMachineGenericTypesBenchmarks>();
        }
    }
}
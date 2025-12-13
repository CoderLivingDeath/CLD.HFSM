using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using CLD.HFSM;

[MemoryDiagnoser]
[MinColumn, Q1Column, MedianColumn, Q3Column, MaxColumn]
[GcForce]
[EventPipeProfiler(EventPipeProfile.CpuSampling)]
[ThreadingDiagnoser]
[SimpleJob(
    RuntimeMoniker.Net80,
    warmupCount: 100,      // Длинный прогрев для стабильности
    iterationCount: 1000,  // Много итераций для точности
    launchCount: 5,        // Многократные запуски
    invocationCount: 10   // Вызовов на итерацию
)]
[HardwareCounters]

public class HFSMBenchmarks
{
    public enum TestState
    {
        Root, A1, A2, A3, B1, B2, B3, Idle, MoveToA3, Attack, MoveToA2
    }

    public enum TestTrigger
    {
        ToIdle, ToMoveA3, ToAttack, ToMoveA2, InvalidTrigger
    }

    private StateMachineConfiguration<TestState, TestTrigger>? _sharedConfig;

    private StateMachine<TestState, TestTrigger>? _fsmNoCache;
    private StateMachine<TestState, TestTrigger>? _fsmWithCache;

    [GlobalSetup]
    public void Setup()
    {
        _sharedConfig = BuildConfig();

        _fsmNoCache = new StateMachine<TestState, TestTrigger>(TestState.Idle, _sharedConfig, false);
        _fsmWithCache = new StateMachine<TestState, TestTrigger>(TestState.Idle, _sharedConfig, true);
    }

    [IterationSetup]
    public void IterationSetup()
    {
        ResetToIdle(_fsmNoCache!);
        ResetToIdle(_fsmWithCache!);
    }

    private static void ResetToIdle(StateMachine<TestState, TestTrigger> fsm)
    {
        if (fsm.CurrentState == TestState.Idle)
            return;

        if (fsm.CurrentState == TestState.MoveToA3 || fsm.CurrentState == TestState.Attack)
        {
            fsm.TryFire(TestTrigger.ToIdle);
            return;
        }

        if (fsm.CurrentState == TestState.MoveToA2)
        {
            fsm.TryFire(TestTrigger.ToAttack);
            fsm.TryFire(TestTrigger.ToIdle);
            return;
        }

        fsm.TryFire(TestTrigger.ToIdle);
    }

    private static (int Passed, int Failed) One(bool ok) => ok ? (1, 0) : (0, 1);

    private static (int Passed, int Failed) Add((int Passed, int Failed) a, (int Passed, int Failed) b)
        => (a.Passed + b.Passed, a.Failed + b.Failed);

    // ----------------- Configuration -----------------

    [Benchmark]
    public (int Passed, int Failed) Configuration_NoCache()
    {
        _ = new StateMachine<TestState, TestTrigger>(TestState.Idle, BuildConfig(), false);
        return (1, 0);
    }

    [Benchmark]
    public (int Passed, int Failed) Configuration_WithCache()
    {
        _ = new StateMachine<TestState, TestTrigger>(TestState.Idle, BuildConfig(), true);
        return (1, 0);
    }

    // ----------------- Single operations -----------------

    [Benchmark]
    public (int Passed, int Failed) ValidTryFire_NoCache()
    {
        // Ожидаем: Idle -> MoveToA3 (true), затем вернёмся в Idle чтобы следующий вызов был стабильным.
        bool ok1 = _fsmNoCache!.TryFire(TestTrigger.ToMoveA3);
        bool ok2 = ok1 ? _fsmNoCache.TryFire(TestTrigger.ToIdle) : false;
        return One(ok1 && ok2);
    }

    [Benchmark]
    public (int Passed, int Failed) ValidTryFire_WithCache()
    {
        bool ok1 = _fsmWithCache!.TryFire(TestTrigger.ToMoveA3);
        bool ok2 = ok1 ? _fsmWithCache.TryFire(TestTrigger.ToIdle) : false;
        return One(ok1 && ok2);
    }

    [Benchmark]
    public (int Passed, int Failed) InvalidTryFire_NoCache()
    {
        // Ожидаем false
        bool ok = !_fsmNoCache!.TryFire(TestTrigger.InvalidTrigger);
        return One(ok);
    }

    [Benchmark]
    public (int Passed, int Failed) InvalidTryFire_WithCache()
    {
        bool ok = !_fsmWithCache!.TryFire(TestTrigger.InvalidTrigger);
        return One(ok);
    }

    [Benchmark]
    public (int Passed, int Failed) Fire_WithHandlers_NoCache()
    {
        // Ожидаем: TryFire true и после Fire(ToIdle) окажемся в Idle
        bool ok1 = _fsmNoCache!.TryFire(TestTrigger.ToMoveA3);
        _fsmNoCache.Fire(TestTrigger.ToIdle);
        bool ok2 = _fsmNoCache.CurrentState == TestState.Idle;
        return One(ok1 && ok2);
    }

    [Benchmark]
    public (int Passed, int Failed) Fire_WithHandlers_WithCache()
    {
        bool ok1 = _fsmWithCache!.TryFire(TestTrigger.ToMoveA3);
        _fsmWithCache.Fire(TestTrigger.ToIdle);
        bool ok2 = _fsmWithCache.CurrentState == TestState.Idle;
        return One(ok1 && ok2);
    }

    // ----------------- Batch operations -----------------

    [Benchmark]
    public (int Passed, int Failed) Batch_1_Valid_NoCache()
    {
        bool ok1 = _fsmNoCache!.TryFire(TestTrigger.ToMoveA3);
        _fsmNoCache.Fire(TestTrigger.ToIdle);
        bool ok2 = _fsmNoCache.CurrentState == TestState.Idle;
        return One(ok1 && ok2);
    }

    [Benchmark]
    public (int Passed, int Failed) Batch_1_Invalid_NoCache()
    {
        bool ok = !_fsmNoCache!.TryFire(TestTrigger.InvalidTrigger);
        return One(ok);
    }

    [Benchmark]
    public (int Passed, int Failed) Batch_1_Valid_WithCache()
    {
        bool ok1 = _fsmWithCache!.TryFire(TestTrigger.ToMoveA3);
        _fsmWithCache.Fire(TestTrigger.ToIdle);
        bool ok2 = _fsmWithCache.CurrentState == TestState.Idle;
        return One(ok1 && ok2);
    }

    [Benchmark]
    public (int Passed, int Failed) Batch_1_Invalid_WithCache()
    {
        bool ok = !_fsmWithCache!.TryFire(TestTrigger.InvalidTrigger);
        return One(ok);
    }

    [Benchmark]
    public (int Passed, int Failed) Batch_10_Valid_NoCache()
    {
        var res = (Passed: 0, Failed: 0);

        for (int i = 0; i < 10; i++)
        {
            bool ok1 = _fsmNoCache!.TryFire(TestTrigger.ToMoveA3);
            _fsmNoCache.Fire(TestTrigger.ToIdle);
            bool ok2 = _fsmNoCache.CurrentState == TestState.Idle;

            res = Add(res, One(ok1 && ok2));
        }

        return res;
    }

    [Benchmark]
    public (int Passed, int Failed) Batch_10_Valid_WithCache()
    {
        var res = (Passed: 0, Failed: 0);

        for (int i = 0; i < 10; i++)
        {
            bool ok1 = _fsmWithCache!.TryFire(TestTrigger.ToMoveA3);
            _fsmWithCache.Fire(TestTrigger.ToIdle);
            bool ok2 = _fsmWithCache.CurrentState == TestState.Idle;

            res = Add(res, One(ok1 && ok2));
        }

        return res;
    }

    [Benchmark]
    public (int Passed, int Failed) Batch_1000_Valid_NoCache()
    {
        var res = (Passed: 0, Failed: 0);

        for (int i = 0; i < 1000; i++)
        {
            bool ok1 = _fsmNoCache!.TryFire(TestTrigger.ToMoveA3);
            _fsmNoCache.Fire(TestTrigger.ToIdle);
            bool ok2 = _fsmNoCache.CurrentState == TestState.Idle;

            res = Add(res, One(ok1 && ok2));
        }

        return res;
    }

    [Benchmark]
    public (int Passed, int Failed) Batch_1000_Invalid_NoCache()
    {
        var res = (Passed: 0, Failed: 0);

        for (int i = 0; i < 1000; i++)
        {
            bool ok = !_fsmNoCache!.TryFire(TestTrigger.InvalidTrigger);
            res = Add(res, One(ok));
        }

        return res;
    }

    [Benchmark]
    public (int Passed, int Failed) Batch_1000_Valid_WithCache()
    {
        var res = (Passed: 0, Failed: 0);

        for (int i = 0; i < 1000; i++)
        {
            bool ok1 = _fsmWithCache!.TryFire(TestTrigger.ToMoveA3);
            _fsmWithCache.Fire(TestTrigger.ToIdle);
            bool ok2 = _fsmWithCache.CurrentState == TestState.Idle;

            res = Add(res, One(ok1 && ok2));
        }

        return res;
    }

    [Benchmark]
    public (int Passed, int Failed) Batch_1000_Invalid_WithCache()
    {
        var res = (Passed: 0, Failed: 0);

        for (int i = 0; i < 1000; i++)
        {
            bool ok = !_fsmWithCache!.TryFire(TestTrigger.InvalidTrigger);
            res = Add(res, One(ok));
        }

        return res;
    }

    // ----------------- Game scenarios -----------------

    [Benchmark]
    public (int Passed, int Failed) GameScenario_FPS120_NoCache()
    {
        return RunScenario(_fsmNoCache!, fps: 120);
    }

    [Benchmark]
    public (int Passed, int Failed) GameScenario_FPS120_WithCache()
    {
        return RunScenario(_fsmWithCache!, fps: 120);
    }

    [Benchmark]
    public (int Passed, int Failed) GameScenario_FPS60_NoCache()
    {
        return RunScenario(_fsmNoCache!, fps: 60);
    }

    [Benchmark]
    public (int Passed, int Failed) GameScenario_FPS60_WithCache()
    {
        return RunScenario(_fsmWithCache!, fps: 60);
    }

    [Benchmark]
    public (int Passed, int Failed) GameScenario_FPS30_NoCache()
    {
        return RunScenario(_fsmNoCache!, fps: 30);
    }

    [Benchmark]
    public (int Passed, int Failed) GameScenario_FPS30_WithCache()
    {
        return RunScenario(_fsmWithCache!, fps: 30);
    }

    private static (int Passed, int Failed) RunScenario(StateMachine<TestState, TestTrigger> fsm, int fps)
    {
        int frames = fps * 5;
        var res = (Passed: 0, Failed: 0);

        for (int i = 0; i < frames; i++)
        {
            TestTrigger trigger =
                (i % 6 == 0) ? TestTrigger.ToMoveA3 :
                (i % 6 == 3) ? TestTrigger.ToIdle :
                TestTrigger.InvalidTrigger;

            // Ожидаемое поведение на основе текущего состояния
            var before = fsm.CurrentState;
            bool expected =
                trigger == TestTrigger.ToMoveA3 ? before == TestState.Idle :
                trigger == TestTrigger.ToIdle ? before == TestState.MoveToA3 :
                false;

            bool actual = fsm.TryFire(trigger);
            res = Add(res, One(actual == expected));
        }

        return res;
    }

    // ----------------- Config builder -----------------

    private static StateMachineConfiguration<TestState, TestTrigger> BuildConfig()
    {
        var b = new StateMachineConfigurationBuilder<TestState, TestTrigger>();

        b.ConfigureState(TestState.Root);
        b.ConfigureState(TestState.A1).SubstateOf(TestState.Root);
        b.ConfigureState(TestState.A2).SubstateOf(TestState.A1);
        b.ConfigureState(TestState.A3).SubstateOf(TestState.A2);
        b.ConfigureState(TestState.B1).SubstateOf(TestState.Root);
        b.ConfigureState(TestState.B2).SubstateOf(TestState.B1);
        b.ConfigureState(TestState.B3).SubstateOf(TestState.B2);

        b.ConfigureState(TestState.Idle)
            .SubstateOf(TestState.A3)
            .Permit(TestTrigger.ToMoveA3, TestState.MoveToA3);

        b.ConfigureState(TestState.MoveToA3)
            .SubstateOf(TestState.A3)
            .Permit(TestTrigger.ToIdle, TestState.Idle);

        b.ConfigureState(TestState.Attack)
            .SubstateOf(TestState.B3)
            .Permit(TestTrigger.ToIdle, TestState.Idle)
            .Permit(TestTrigger.ToMoveA2, TestState.MoveToA2);

        b.ConfigureState(TestState.MoveToA2)
            .SubstateOf(TestState.A2)
            .Permit(TestTrigger.ToAttack, TestState.Attack);

        return b.GetConfiguration();
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        _ = BenchmarkRunner.Run<HFSMBenchmarks>();
    }
}

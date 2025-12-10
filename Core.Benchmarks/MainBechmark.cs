using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using CLD.HFSM;
using System;
using System.Drawing;
using System.Threading.Tasks;

[MemoryDiagnoser]
[MinColumn, MaxColumn, Q1Column, Q3Column, MeanColumn, MedianColumn]
[GcForce]
[ThreadingDiagnoser]
[HardwareCounters]
[HideColumns("Job", "Environment")]
[SimpleJob(RuntimeMoniker.Net80, baseline: true)]
[SimpleJob(RuntimeMoniker.Net80,
    id: "HighAccuracy",
    warmupCount: 30,
    iterationCount: 30,
    invocationCount: 10000)]
[SimpleJob(RuntimeMoniker.Net80,
    id: "ExtremeAccuracy",
    warmupCount: 100,
    iterationCount: 200,
    invocationCount: 50000)]

public class StateMachineBenchmarks
{
    #region Test Types

    // Enum types
    public enum GameState { Idle, Running, Paused, GameOver }
    public enum GameTrigger { Start, Pause, Resume, Stop, Restart }
    public enum EnumState { Idle, Running, Paused, GameOver }
    public enum EnumTrigger { Start, Pause, Resume, Stop }

    // Struct types
    public readonly struct StructState : IEquatable<StructState>
    {
        public readonly int Id;
        public StructState(int id) => Id = id;
        public bool Equals(StructState other) => Id == other.Id;
        public override bool Equals(object? obj) => obj is StructState s && Equals(s);
        public override int GetHashCode() => Id;
        public override string ToString() => $"S{Id}";
    }

    public readonly struct StructTrigger : IEquatable<StructTrigger>
    {
        public readonly int Id;
        public StructTrigger(int id) => Id = id;
        public bool Equals(StructTrigger other) => Id == other.Id;
        public override bool Equals(object? obj) => obj is StructTrigger t && Equals(t);
        public override int GetHashCode() => Id;
        public override string ToString() => $"T{Id}";
    }

    // Class types
    public sealed class ClassState : IEquatable<ClassState>
    {
        public string Name { get; }
        public ClassState(string name) => Name = name;
        public bool Equals(ClassState? other) => other != null && Name == other.Name;
        public override bool Equals(object? obj) => Equals(obj as ClassState);
        public override int GetHashCode() => Name.GetHashCode();
        public override string ToString() => Name;
    }

    public sealed class ClassTrigger : IEquatable<ClassTrigger>
    {
        public string Name { get; }
        public ClassTrigger(string name) => Name = name;
        public bool Equals(ClassTrigger? other) => other != null && Name == other.Name;
        public override bool Equals(object? obj) => Equals(obj as ClassTrigger);
        public override int GetHashCode() => Name.GetHashCode();
        public override string ToString() => Name;
    }

    #endregion

    #region StateMachine Instances

    // Game scenario state machines
    public StateMachine<GameState, GameTrigger> stateMachineBase;
    public StateMachine<GameState, GameTrigger> stateMachineSyncHandlers;
    public StateMachine<GameState, GameTrigger> stateMachineAsyncHandlers;
    public StateMachine<GameState, GameTrigger> stateMachineGameScenario;
    public StateMachine<GameState, GameTrigger> stateMachineMinimalTransitions;
    public StateMachine<GameState, GameTrigger> stateMachineNoTransitions;

    // Generic types state machines
    public StateMachine<EnumState, EnumTrigger> _enumSm;
    public StateMachine<StructState, StructTrigger> _structSm;
    public StateMachine<ClassState, ClassTrigger> _classSm;

    #endregion

    #region Reusable Values

    public readonly GameTrigger gameTriggerStart = GameTrigger.Start;
    public readonly GameTrigger gameTriggerPause = GameTrigger.Pause;
    public readonly EnumTrigger _eStart = EnumTrigger.Start;
    public readonly EnumTrigger _eInvalid = EnumTrigger.Resume;
    public readonly StructTrigger _sStart = new StructTrigger(1);
    public readonly StructTrigger _sInvalid = new StructTrigger(99);
    public readonly StructState _sIdle = new StructState(0);
    public readonly StructState _sRunning = new StructState(1);
    public readonly ClassTrigger _cStart = new ClassTrigger("Start");
    public readonly ClassTrigger _cInvalid = new ClassTrigger("Invalid");
    public readonly ClassState _cIdle = new ClassState("Idle");
    public readonly ClassState _cRunning = new ClassState("Running");

    #endregion

    #region GlobalSetup

    [GlobalSetup]
    public void GlobalSetup()
    {
        var baseConfig = BuildBaseConfiguration();
        var syncHandlerConfig = BuildSyncHandlerConfiguration();
        var asyncHandlerConfig = BuildAsyncHandlerConfiguration();
        var gameScenarioConfig = BuildGameScenarioConfiguration();
        var minimalTransitionsConfig = BuildMinimalTransitionsConfiguration();
        var noTransitionsConfig = BuildNoTransitionsConfiguration();

        stateMachineBase = new StateMachine<GameState, GameTrigger>(GameState.Idle, baseConfig);
        stateMachineSyncHandlers = new StateMachine<GameState, GameTrigger>(GameState.Idle, syncHandlerConfig);
        stateMachineAsyncHandlers = new StateMachine<GameState, GameTrigger>(GameState.Idle, asyncHandlerConfig);
        stateMachineGameScenario = new StateMachine<GameState, GameTrigger>(GameState.Idle, gameScenarioConfig);
        stateMachineMinimalTransitions = new StateMachine<GameState, GameTrigger>(GameState.Idle, minimalTransitionsConfig);
        stateMachineNoTransitions = new StateMachine<GameState, GameTrigger>(GameState.Idle, noTransitionsConfig);

        _enumSm = new StateMachine<EnumState, EnumTrigger>(EnumState.Idle, BuildEnumConfig());
        _structSm = new StateMachine<StructState, StructTrigger>(_sIdle, BuildStructConfig());
        _classSm = new StateMachine<ClassState, ClassTrigger>(_cIdle, BuildClassConfig());
    }

    [IterationSetup]
    public void IterationSetup()
    {
        stateMachineBase.ForceTransition(GameState.Idle);
        stateMachineSyncHandlers.ForceTransition(GameState.Idle);
        stateMachineAsyncHandlers.ForceTransition(GameState.Idle);
        stateMachineGameScenario.ForceTransition(GameState.Idle);
        stateMachineMinimalTransitions.ForceTransition(GameState.Idle);
        stateMachineNoTransitions.ForceTransition(GameState.Idle);

        _enumSm.ForceTransition(EnumState.Idle);
        _structSm.ForceTransition(_sIdle);
        _classSm.ForceTransition(_cIdle);
    }

    #endregion

    #region Configuration Builders (Game Scenario)

    public static StateMachineConfiguration<GameState, GameTrigger> BuildBaseConfiguration()
    {
        var builder = new StateMachineConfigurationBuilder<GameState, GameTrigger>();
        builder.ConfigureState(GameState.Idle).Permit(GameTrigger.Start, GameState.Running);
        builder.ConfigureState(GameState.Running)
            .Permit(GameTrigger.Pause, GameState.Paused)
            .Permit(GameTrigger.Stop, GameState.GameOver);
        builder.ConfigureState(GameState.Paused)
            .Permit(GameTrigger.Resume, GameState.Running)
            .Permit(GameTrigger.Stop, GameState.GameOver);
        builder.ConfigureState(GameState.GameOver)
            .Permit(GameTrigger.Restart, GameState.Idle);
        return builder.GetConfiguration();
    }

    public static StateMachineConfiguration<GameState, GameTrigger> BuildSyncHandlerConfiguration()
    {
        var builder = new StateMachineConfigurationBuilder<GameState, GameTrigger>();
        builder.ConfigureState(GameState.Idle)
            .OnEnter(() => { })
            .OnExit(() => { })
            .Permit(GameTrigger.Start, GameState.Running);
        builder.ConfigureState(GameState.Running)
            .OnEnter(() => { })
            .OnExit(() => { })
                        .Permit(GameTrigger.Pause, GameState.Paused)
            .Permit(GameTrigger.Stop, GameState.GameOver);
        builder.ConfigureState(GameState.Paused)
            .OnEnter(() => { })
            .OnExit(() => { })
            .Permit(GameTrigger.Resume, GameState.Running)
            .Permit(GameTrigger.Stop, GameState.GameOver);
        builder.ConfigureState(GameState.GameOver)
            .OnEnter(() => { })
            .OnExit(() => { })
            .Permit(GameTrigger.Restart, GameState.Idle);
        return builder.GetConfiguration();
    }

    public static StateMachineConfiguration<GameState, GameTrigger> BuildAsyncHandlerConfiguration()
    {
        var builder = new StateMachineConfigurationBuilder<GameState, GameTrigger>();
        builder.ConfigureState(GameState.Idle)
            .OnEnterAsync(() => ValueTask.CompletedTask)
            .OnExitAsync(() => ValueTask.CompletedTask)
            .Permit(GameTrigger.Start, GameState.Running);
        builder.ConfigureState(GameState.Running)
            .OnEnterAsync(() => ValueTask.CompletedTask)
            .OnExitAsync(() => ValueTask.CompletedTask)
            .Permit(GameTrigger.Pause, GameState.Paused)
            .Permit(GameTrigger.Stop, GameState.GameOver);
        builder.ConfigureState(GameState.Paused)
            .OnEnterAsync(() => ValueTask.CompletedTask)
            .OnExitAsync(() => ValueTask.CompletedTask)
            .Permit(GameTrigger.Resume, GameState.Running)
            .Permit(GameTrigger.Stop, GameState.GameOver);
        builder.ConfigureState(GameState.GameOver)
            .OnEnterAsync(() => ValueTask.CompletedTask)
            .OnExitAsync(() => ValueTask.CompletedTask)
            .Permit(GameTrigger.Restart, GameState.Idle);
        return builder.GetConfiguration();
    }

    public static StateMachineConfiguration<GameState, GameTrigger> BuildGameScenarioConfiguration()
    {
        var builder = new StateMachineConfigurationBuilder<GameState, GameTrigger>();
        builder.ConfigureState(GameState.Idle)
            .OnEnter(() => { })
            .OnExit(() => { })
            .Permit(GameTrigger.Start, GameState.Running);
        builder.ConfigureState(GameState.Running)
            .OnEnter(() => { })
            .OnExit(() => { })
            .Permit(GameTrigger.Pause, GameState.Paused)
            .Permit(GameTrigger.Stop, GameState.GameOver);
        builder.ConfigureState(GameState.Paused)
            .OnEnter(() => { })
            .OnExit(() => { })
            .Permit(GameTrigger.Resume, GameState.Running)
            .Permit(GameTrigger.Stop, GameState.GameOver);
        builder.ConfigureState(GameState.GameOver)
            .OnEnter(() => { })
            .OnExit(() => { })
            .Permit(GameTrigger.Restart, GameState.Idle);
        builder.OnTransition((source, target) => { });
        return builder.GetConfiguration();
    }

    private static StateMachineConfiguration<GameState, GameTrigger> BuildMinimalTransitionsConfiguration()
    {
        var builder = new StateMachineConfigurationBuilder<GameState, GameTrigger>();
        builder.ConfigureState(GameState.Idle)
            .Permit(GameTrigger.Start, GameState.Running);
        builder.ConfigureState(GameState.Running);
        builder.ConfigureState(GameState.Paused);
        builder.ConfigureState(GameState.GameOver);
        return builder.GetConfiguration();
    }

    private static StateMachineConfiguration<GameState, GameTrigger> BuildNoTransitionsConfiguration()
    {
        var builder = new StateMachineConfigurationBuilder<GameState, GameTrigger>();
        builder.ConfigureState(GameState.Idle);
        builder.ConfigureState(GameState.Running);
        builder.ConfigureState(GameState.Paused);
        builder.ConfigureState(GameState.GameOver);
        return builder.GetConfiguration();
    }

    #endregion

    #region Configuration Builders (Generic Types)

    public static StateMachineConfiguration<EnumState, EnumTrigger> BuildEnumConfig()
    {
        var b = new StateMachineConfigurationBuilder<EnumState, EnumTrigger>();
        b.ConfigureState(EnumState.Idle).Permit(EnumTrigger.Start, EnumState.Running);
        b.ConfigureState(EnumState.Running)
            .Permit(EnumTrigger.Pause, EnumState.Paused)
            .Permit(EnumTrigger.Stop, EnumState.GameOver);
        b.ConfigureState(EnumState.Paused)
            .Permit(EnumTrigger.Resume, EnumState.Running)
            .Permit(EnumTrigger.Stop, EnumState.GameOver);
        b.ConfigureState(EnumState.GameOver);
        return b.GetConfiguration();
    }

    private StateMachineConfiguration<StructState, StructTrigger> BuildStructConfig()
    {
        var b = new StateMachineConfigurationBuilder<StructState, StructTrigger>();
        b.ConfigureState(_sIdle).Permit(_sStart, _sRunning);
        b.ConfigureState(_sRunning);
        return b.GetConfiguration();
    }

    private StateMachineConfiguration<ClassState, ClassTrigger> BuildClassConfig()
    {
        var b = new StateMachineConfigurationBuilder<ClassState, ClassTrigger>();
        b.ConfigureState(_cIdle).Permit(_cStart, _cRunning);
        b.ConfigureState(_cRunning);
        return b.GetConfiguration();
    }

    #endregion

    #region Configuration Benchmarks

    [Benchmark(Description = "Config - Game Base")]
    public void BenchmarkConfigGameBase() => _ = BuildBaseConfiguration();

    [Benchmark(Description = "Config - Game Minimal")]
    public void BenchmarkConfigGameMinimal() => _ = BuildMinimalTransitionsConfiguration();

    [Benchmark(Description = "Config - Game No Transitions")]
    public void BenchmarkConfigGameNoTransitions() => _ = BuildNoTransitionsConfiguration();

    [Benchmark(Description = "Config - Enum<TState,TTrigger>")]
    public StateMachineConfiguration<EnumState, EnumTrigger> Config_Enum() => BuildEnumConfig();

    [Benchmark(Description = "Config - Struct<TState,TTrigger>")]
    public StateMachineConfiguration<StructState, StructTrigger> Config_Struct() => BuildStructConfig();

    [Benchmark(Description = "Config - Class<TState,TTrigger>")]
    public StateMachineConfiguration<ClassState, ClassTrigger> Config_Class() => BuildClassConfig();


    #endregion

    #region Single Fire Benchmarks

    [Benchmark(Description = "Single Fire - Game Valid")]
    public void BenchmarkSingleFireGameValid() => stateMachineBase.TryFire(gameTriggerStart);

    [Benchmark(Description = "Single Fire - Game Invalid")]
    public void BenchmarkSingleFireGameInvalid() => stateMachineBase.TryFire(gameTriggerPause);

    [Benchmark(Description = "Single Fire - Enum Valid")]
    public void Enum_SingleValid() => _enumSm.TryFire(_eStart);

    [Benchmark(Description = "Single Fire - Enum Invalid")]
    public void Enum_SingleInvalid() => _enumSm.TryFire(_eInvalid);

    [Benchmark(Description = "Single Fire - Struct Valid")]
    public void Struct_SingleValid() => _structSm.TryFire(_sStart);

    [Benchmark(Description = "Single Fire - Struct Invalid")]
    public void Struct_SingleInvalid() => _structSm.TryFire(_sInvalid);

    [Benchmark(Description = "Single Fire - Class Valid")]
    public void Class_SingleValid() => _classSm.TryFire(_cStart);

    [Benchmark(Description = "Single Fire - Class Invalid")]
    public void Class_SingleInvalid() => _classSm.TryFire(_cInvalid);

    #endregion

    #region Stress Tests (100x)

    [Benchmark(Description = "Stress 100x - Game Valid")]
    public void BenchmarkStress100GameValid()
    {
        for (int i = 0; i < 100; i++)
        {
            stateMachineBase.TryFire(gameTriggerStart);
            stateMachineBase.ForceTransition(GameState.Idle);
        }
    }

    [Benchmark(Description = "Stress 100x - Game Invalid")]
    public void BenchmarkStress100GameInvalid()
    {
        for (int i = 0; i < 100; i++)
            stateMachineBase.TryFire(gameTriggerPause);
    }

    [Benchmark(Description = "Stress 100x - Enum Valid")]
    public void Enum_100Valid()
    {
        for (int i = 0; i < 100; i++)
        {
            _enumSm.TryFire(_eStart);
            _enumSm.ForceTransition(EnumState.Idle);
        }
    }

    [Benchmark(Description = "Stress 100x - Enum Invalid")]
    public void Enum_100Invalid()
    {
        for (int i = 0; i < 100; i++)
            _enumSm.TryFire(_eInvalid);
    }

    [Benchmark(Description = "Stress 100x - Struct Valid")]
    public void Struct_100Valid()
    {
        for (int i = 0; i < 100; i++)
        {
            _structSm.TryFire(_sStart);
            _structSm.ForceTransition(_sIdle);
        }
    }

    [Benchmark(Description = "Stress 100x - Struct Invalid")]
    public void Struct_100Invalid()
    {
        for (int i = 0; i < 100; i++)
            _structSm.TryFire(_sInvalid);
    }

    [Benchmark(Description = "Stress 100x - Class Valid")]
    public void Class_100Valid()
    {
        for (int i = 0; i < 100; i++)
        {
            _classSm.TryFire(_cStart);
            _classSm.ForceTransition(_cIdle);
        }
    }

    [Benchmark(Description = "Stress 100x - Class Invalid")]
    public void Class_100Invalid()
    {
        for (int i = 0; i < 100; i++)
            _classSm.TryFire(_cInvalid);
    }

    #endregion

    #region Stress Tests (1000x)

    [Benchmark(Description = "Stress 1000x - Game Invalid")]
    public void BenchmarkStress1000GameInvalid()
    {
        for (int i = 0; i < 1000; i++)
            stateMachineBase.TryFire(gameTriggerPause);
    }

    [Benchmark(Description = "Stress 1000x - Enum Invalid")]
    public void Enum_1000Invalid()
    {
        for (int i = 0; i < 1000; i++)
            _enumSm.TryFire(_eInvalid);
    }

    [Benchmark(Description = "Stress 1000x - Struct Invalid")]
    public void Struct_1000Invalid()
    {
        for (int i = 0; i < 1000; i++)
            _structSm.TryFire(_sInvalid);
    }

    [Benchmark(Description = "Stress 1000x - Class Invalid")]
    public void Class_1000Invalid()
    {
        for (int i = 0; i < 1000; i++)
            _classSm.TryFire(_cInvalid);
    }

    #endregion

    #region Game Scenario

    [Benchmark(Description = "Game 60FPS - Mixed")]
    public void BenchmarkGameScenario60FPS()
    {
        for (int frame = 0; frame < 60; frame++)
        {
            if (frame == 5) stateMachineGameScenario.TryFire(GameTrigger.Start);
            if (frame == 20) stateMachineGameScenario.TryFire(GameTrigger.Pause);
            if (frame == 40) stateMachineGameScenario.TryFire(GameTrigger.Resume);
            if (frame == 55) stateMachineGameScenario.TryFire(GameTrigger.Stop);
            stateMachineGameScenario.TryFire(GameTrigger.Pause);
        }
    }

    #endregion

    #region Handlers Benchmark

    [Benchmark(Description = "Handlers + OnTransition - Enum")]
    public void Enum_WithHandlersAndTransitionCallback()
    {
        bool exitCalled = false, enterCalled = false, transitionCalled = false;

        var builder = new StateMachineConfigurationBuilder<EnumState, EnumTrigger>();
        builder.ConfigureState(EnumState.Idle)
               .OnExit(() => exitCalled = true)
               .Permit(EnumTrigger.Start, EnumState.Running);
        builder.ConfigureState(EnumState.Running)
               .OnEnter(() => enterCalled = true);
        builder.OnTransition((s, t) => transitionCalled = true);

        var config = builder.GetConfiguration();
        var sm = new StateMachine<EnumState, EnumTrigger>(EnumState.Idle, config);
        sm.TryFire(EnumTrigger.Start);

        if (!exitCalled || !enterCalled || !transitionCalled)
            throw new InvalidOperationException("handlers not called");
    }

    #endregion
}




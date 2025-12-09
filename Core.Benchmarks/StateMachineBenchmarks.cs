using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using CLD.HFSM;
using System.Threading.Tasks;

[MemoryDiagnoser]
[HideColumns("Job", "Environment", "LaunchCount", "WarmupCount", "IterationCount")]
[SimpleJob(RuntimeMoniker.Net80, baseline: true)]
[SimpleJob(RuntimeMoniker.Net80, id: "HighAccuracy",
           warmupCount: 10, iterationCount: 15)]
public class StateMachineBenchmarks
{
    // Test states
    private enum GameState { Idle, Running, Paused, GameOver }

    // Test triggers
    private enum GameTrigger { Start, Pause, Resume, Stop, Restart }

    // StateMachine instances with PROPER GENERICS
    private StateMachine<GameState, GameTrigger> stateMachineBase;
    private StateMachine<GameState, GameTrigger> stateMachineSyncHandlers;
    private StateMachine<GameState, GameTrigger> stateMachineAsyncHandlers;
    private StateMachine<GameState, GameTrigger> stateMachineGameScenario;
    private StateMachine<GameState, GameTrigger> stateMachineMinimalTransitions;
    private StateMachine<GameState, GameTrigger> stateMachineNoTransitions;

    // Configurations
    private StateMachineConfiguration<GameState, GameTrigger> baseConfig;
    private StateMachineConfiguration<GameState, GameTrigger> syncHandlerConfig;
    private StateMachineConfiguration<GameState, GameTrigger> asyncHandlerConfig;
    private StateMachineConfiguration<GameState, GameTrigger> gameScenarioConfig;
    private StateMachineConfiguration<GameState, GameTrigger> minimalTransitionsConfig;
    private StateMachineConfiguration<GameState, GameTrigger> noTransitionsConfig;

    [GlobalSetup]
    public void GlobalSetup()
    {
        baseConfig = BuildBaseConfiguration();
        syncHandlerConfig = BuildSyncHandlerConfiguration();
        asyncHandlerConfig = BuildAsyncHandlerConfiguration();
        gameScenarioConfig = BuildGameScenarioConfiguration();
        minimalTransitionsConfig = BuildMinimalTransitionsConfiguration();
        noTransitionsConfig = BuildNoTransitionsConfiguration();

        stateMachineBase = new StateMachine<GameState, GameTrigger>(GameState.Idle, baseConfig);
        stateMachineSyncHandlers = new StateMachine<GameState, GameTrigger>(GameState.Idle, syncHandlerConfig);
        stateMachineAsyncHandlers = new StateMachine<GameState, GameTrigger>(GameState.Idle, asyncHandlerConfig);
        stateMachineGameScenario = new StateMachine<GameState, GameTrigger>(GameState.Idle, gameScenarioConfig);
        stateMachineMinimalTransitions = new StateMachine<GameState, GameTrigger>(GameState.Idle, minimalTransitionsConfig);
        stateMachineNoTransitions = new StateMachine<GameState, GameTrigger>(GameState.Idle, noTransitionsConfig);
    }

    /// <summary>
    /// Reset all state machines to Idle before each iteration
    /// This ensures clean measurement without state carryover between iterations
    /// CRITICAL: Without this, 'Single Fire - With sync handlers' would execute mostly invalid transitions
    /// </summary>
    [IterationSetup]
    public void IterationSetup()
    {
        stateMachineBase.ForceTransition(GameState.Idle);
        stateMachineSyncHandlers.ForceTransition(GameState.Idle);
        stateMachineAsyncHandlers.ForceTransition(GameState.Idle);
        stateMachineGameScenario.ForceTransition(GameState.Idle);
        stateMachineMinimalTransitions.ForceTransition(GameState.Idle);
        stateMachineNoTransitions.ForceTransition(GameState.Idle);
    }

    #region Configuration Builders

    private static StateMachineConfiguration<GameState, GameTrigger> BuildBaseConfiguration()
    {
        var builder = new StateMachineConfigurationBuilder<GameState, GameTrigger>();
        builder.ConfigureState(GameState.Idle)
            .Permit(GameTrigger.Start, GameState.Running);

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

    private static StateMachineConfiguration<GameState, GameTrigger> BuildSyncHandlerConfiguration()
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

    private static StateMachineConfiguration<GameState, GameTrigger> BuildAsyncHandlerConfiguration()
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

    private static StateMachineConfiguration<GameState, GameTrigger> BuildGameScenarioConfiguration()
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

    /// <summary>
    /// Configuration with minimal transitions:
    /// Only Idle -> Running, no other transitions allowed
    /// All other triggers are invalid
    /// </summary>
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

    /// <summary>
    /// Configuration with NO transitions at all
    /// All triggers are invalid in all states
    /// </summary>
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

    #region Configuration Benchmarks

    [Benchmark(Description = "Configuration - Create new configuration")]
    public void BenchmarkConfiguration() =>
        _ = BuildBaseConfiguration();

    [Benchmark(Description = "Reconfiguration - Reconfigure existing state machine")]
    public void BenchmarkReconfiguration()
    {
        var newConfig = BuildBaseConfiguration();
        stateMachineBase.Configure(newConfig);
    }

    [Benchmark(Description = "Configuration - Build minimal transitions config")]
    public void BenchmarkConfigurationMinimalTransitions() =>
        _ = BuildMinimalTransitionsConfiguration();

    [Benchmark(Description = "Configuration - Build no transitions config")]
    public void BenchmarkConfigurationNoTransitions() =>
        _ = BuildNoTransitionsConfiguration();

    #endregion

    #region Single Fire Benchmarks

    [Benchmark(Description = "Single Fire - No transition (invalid trigger)")]
    public void BenchmarkSingleFireNoTransition() =>
        stateMachineBase.TryFire(GameTrigger.Pause);

    [Benchmark(Description = "Single Fire - With transition")]
    public void BenchmarkSingleFireWithTransition()
    {
        stateMachineBase.TryFire(GameTrigger.Start);
        stateMachineBase.TryFire(GameTrigger.Pause);
        stateMachineBase.TryFire(GameTrigger.Stop);
    }

    [Benchmark(Description = "Single Fire - With sync handlers")]
    public void BenchmarkSingleFireWithSyncHandler()
    {
        stateMachineSyncHandlers.TryFire(GameTrigger.Start);
        stateMachineSyncHandlers.TryFire(GameTrigger.Pause);
        stateMachineSyncHandlers.TryFire(GameTrigger.Stop);
    }

    [Benchmark(Description = "Single Fire - With async handlers")]
    public void BenchmarkSingleFireWithAsyncHandler()
    {
        stateMachineAsyncHandlers.TryFire(GameTrigger.Start);
        stateMachineAsyncHandlers.TryFire(GameTrigger.Pause);
        stateMachineAsyncHandlers.TryFire(GameTrigger.Stop);
    }

    #endregion

    #region Invalid Transition Benchmarks (Missing Transitions)

    [Benchmark(Description = "Invalid Fire - From Idle (no Resume trigger)")]
    public void BenchmarkInvalidFireFromIdle() =>
        stateMachineBase.TryFire(GameTrigger.Resume);

    [Benchmark(Description = "Invalid Fire - From Running (no Start trigger)")]
    public void BenchmarkInvalidFireFromRunning()
    {
        stateMachineBase.TryFire(GameTrigger.Start);
        stateMachineBase.TryFire(GameTrigger.Start);
    }

    [Benchmark(Description = "Invalid Fire - From Paused (no Start trigger)")]
    public void BenchmarkInvalidFireFromPaused()
    {
        stateMachineBase.TryFire(GameTrigger.Start);
        stateMachineBase.TryFire(GameTrigger.Pause);
        stateMachineBase.TryFire(GameTrigger.Start);
    }

    [Benchmark(Description = "Invalid Fire - From GameOver (invalid trigger)")]
    public void BenchmarkInvalidFireFromGameOver()
    {
        stateMachineBase.TryFire(GameTrigger.Start);
        stateMachineBase.TryFire(GameTrigger.Stop);
        stateMachineBase.TryFire(GameTrigger.Pause);
    }

    [Benchmark(Description = "Invalid Fire - Minimal config (all invalid after first transition)")]
    public void BenchmarkInvalidFireMinimalConfig()
    {
        stateMachineMinimalTransitions.TryFire(GameTrigger.Start);
        stateMachineMinimalTransitions.TryFire(GameTrigger.Pause);
        stateMachineMinimalTransitions.TryFire(GameTrigger.Stop);
    }

    [Benchmark(Description = "Invalid Fire - No transitions config (all invalid)")]
    public void BenchmarkInvalidFireNoTransitionsConfig() =>
        stateMachineNoTransitions.TryFire(GameTrigger.Start);

    #endregion

    #region Invalid Transitions - Stress Tests

    [Benchmark(Description = "Stress Test 10x - No transition (invalid trigger)")]
    public void BenchmarkStress10NoTransition()
    {
        for (int i = 0; i < 10; i++)
            stateMachineBase.TryFire(GameTrigger.Pause);
    }

    [Benchmark(Description = "Stress Test 100x - No transition (invalid trigger)")]
    public void BenchmarkStress100NoTransition()
    {
        for (int i = 0; i < 100; i++)
            stateMachineBase.TryFire(GameTrigger.Pause);
    }

    [Benchmark(Description = "Stress Test 1000x - No transition (invalid trigger)")]
    public void BenchmarkStress1000NoTransition()
    {
        for (int i = 0; i < 1000; i++)
            stateMachineBase.TryFire(GameTrigger.Pause);
    }

    [Benchmark(Description = "Stress Test 10x - Minimal transitions config")]
    public void BenchmarkStress10MinimalTransitions()
    {
        for (int i = 0; i < 10; i++)
        {
            stateMachineMinimalTransitions.TryFire(GameTrigger.Start);
            stateMachineMinimalTransitions.TryFire(GameTrigger.Pause);
            stateMachineMinimalTransitions.TryFire(GameTrigger.Restart);
        }
    }

    [Benchmark(Description = "Stress Test 100x - Minimal transitions config")]
    public void BenchmarkStress100MinimalTransitions()
    {
        for (int i = 0; i < 100; i++)
        {
            stateMachineMinimalTransitions.TryFire(GameTrigger.Start);
            stateMachineMinimalTransitions.TryFire(GameTrigger.Pause);
            stateMachineMinimalTransitions.TryFire(GameTrigger.Restart);
        }
    }

    [Benchmark(Description = "Stress Test 1000x - Minimal transitions config")]
    public void BenchmarkStress1000MinimalTransitions()
    {
        for (int i = 0; i < 1000; i++)
        {
            stateMachineMinimalTransitions.TryFire(GameTrigger.Start);
            stateMachineMinimalTransitions.TryFire(GameTrigger.Pause);
            stateMachineMinimalTransitions.TryFire(GameTrigger.Restart);
        }
    }

    [Benchmark(Description = "Stress Test 10x - No transitions config (all invalid)")]
    public void BenchmarkStress10NoTransitionsConfig()
    {
        for (int i = 0; i < 10; i++)
        {
            stateMachineNoTransitions.TryFire(GameTrigger.Start);
            stateMachineNoTransitions.TryFire(GameTrigger.Pause);
            stateMachineNoTransitions.TryFire(GameTrigger.Stop);
        }
    }

    [Benchmark(Description = "Stress Test 100x - No transitions config (all invalid)")]
    public void BenchmarkStress100NoTransitionsConfig()
    {
        for (int i = 0; i < 100; i++)
        {
            stateMachineNoTransitions.TryFire(GameTrigger.Start);
            stateMachineNoTransitions.TryFire(GameTrigger.Pause);
            stateMachineNoTransitions.TryFire(GameTrigger.Stop);
        }
    }

    [Benchmark(Description = "Stress Test 1000x - No transitions config (all invalid)")]
    public void BenchmarkStress1000NoTransitionsConfig()
    {
        for (int i = 0; i < 1000; i++)
        {
            stateMachineNoTransitions.TryFire(GameTrigger.Start);
            stateMachineNoTransitions.TryFire(GameTrigger.Pause);
            stateMachineNoTransitions.TryFire(GameTrigger.Stop);
        }
    }

    #endregion

    #region Invalid Transitions - Stress Tests With Handlers

    [Benchmark(Description = "Stress Test 10x - No transition with sync handlers")]
    public void BenchmarkStress10NoTransitionSyncHandlers()
    {
        for (int i = 0; i < 10; i++)
            stateMachineSyncHandlers.TryFire(GameTrigger.Pause);
    }

    [Benchmark(Description = "Stress Test 100x - No transition with sync handlers")]
    public void BenchmarkStress100NoTransitionSyncHandlers()
    {
        for (int i = 0; i < 100; i++)
            stateMachineSyncHandlers.TryFire(GameTrigger.Pause);
    }

    [Benchmark(Description = "Stress Test 1000x - No transition with sync handlers")]
    public void BenchmarkStress1000NoTransitionSyncHandlers()
    {
        for (int i = 0; i < 1000; i++)
            stateMachineSyncHandlers.TryFire(GameTrigger.Pause);
    }

    [Benchmark(Description = "Stress Test 10x - No transition with async handlers")]
    public void BenchmarkStress10NoTransitionAsyncHandlers()
    {
        for (int i = 0; i < 10; i++)
            stateMachineAsyncHandlers.TryFire(GameTrigger.Pause);
    }

    [Benchmark(Description = "Stress Test 100x - No transition with async handlers")]
    public void BenchmarkStress100NoTransitionAsyncHandlers()
    {
        for (int i = 0; i < 100; i++)
            stateMachineAsyncHandlers.TryFire(GameTrigger.Pause);
    }

    [Benchmark(Description = "Stress Test 1000x - No transition with async handlers")]
    public void BenchmarkStress1000NoTransitionAsyncHandlers()
    {
        for (int i = 0; i < 1000; i++)
            stateMachineAsyncHandlers.TryFire(GameTrigger.Pause);
    }

    #endregion

    #region Comparison Benchmarks (Valid vs Invalid)

    [Benchmark(Description = "Compare - 10x Valid transitions")]
    public void BenchmarkCompare10ValidTransitions()
    {
        for (int i = 0; i < 10; i++)
        {
            stateMachineBase.TryFire(GameTrigger.Start);
            stateMachineBase.TryFire(GameTrigger.Pause);
            stateMachineBase.TryFire(GameTrigger.Resume);
        }
    }

    [Benchmark(Description = "Compare - 10x Invalid transitions")]
    public void BenchmarkCompare10InvalidTransitions()
    {
        for (int i = 0; i < 10; i++)
            stateMachineBase.TryFire(GameTrigger.Restart);
    }

    [Benchmark(Description = "Compare - 100x Valid transitions")]
    public void BenchmarkCompare100ValidTransitions()
    {
        for (int i = 0; i < 100; i++)
        {
            stateMachineBase.TryFire(GameTrigger.Start);
            stateMachineBase.TryFire(GameTrigger.Pause);
            stateMachineBase.TryFire(GameTrigger.Resume);
        }
    }

    [Benchmark(Description = "Compare - 100x Invalid transitions")]
    public void BenchmarkCompare100InvalidTransitions()
    {
        for (int i = 0; i < 100; i++)
            stateMachineBase.TryFire(GameTrigger.Restart);
    }

    [Benchmark(Description = "Compare - 1000x Valid transitions")]
    public void BenchmarkCompare1000ValidTransitions()
    {
        for (int i = 0; i < 1000; i++)
        {
            stateMachineBase.TryFire(GameTrigger.Start);
            stateMachineBase.TryFire(GameTrigger.Pause);
            stateMachineBase.TryFire(GameTrigger.Resume);
        }
    }

    [Benchmark(Description = "Compare - 1000x Invalid transitions")]
    public void BenchmarkCompare1000InvalidTransitions()
    {
        for (int i = 0; i < 1000; i++)
            stateMachineBase.TryFire(GameTrigger.Restart);
    }

    #endregion

    #region Game Scenario Benchmarks

    [Benchmark(Description = "Game Scenario - 60 FPS with mixed transitions")]
    public void BenchmarkGameScenario60FPS()
    {
        for (int frame = 0; frame < 60; frame++)
        {
            if (frame == 5) stateMachineGameScenario.TryFire(GameTrigger.Start);
            if (frame == 20) stateMachineGameScenario.TryFire(GameTrigger.Pause);
            if (frame == 40) stateMachineGameScenario.TryFire(GameTrigger.Resume);
            if (frame == 55) stateMachineGameScenario.TryFire(GameTrigger.Stop);
            if (frame == 59) stateMachineGameScenario.TryFire(GameTrigger.Restart);

            stateMachineGameScenario.TryFire(GameTrigger.Pause);
        }
    }

    [Benchmark(Description = "Game Scenario - 60 FPS all invalid transitions")]
    public void BenchmarkGameScenario60FPSAllInvalid()
    {
        for (int frame = 0; frame < 60; frame++)
        {
            stateMachineGameScenario.TryFire(GameTrigger.Stop);
            stateMachineGameScenario.TryFire(GameTrigger.Resume);
            stateMachineGameScenario.TryFire(GameTrigger.Restart);
        }
    }

    #endregion
}
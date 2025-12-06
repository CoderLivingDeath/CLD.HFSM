using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using CLD.HFSM;
using System;
using System.Threading.Tasks;

// Маленький enum (3 состояния)
public enum SmallGameState
{
    Idle,
    Running,
    Paused
}

// Большой enum (20+ состояний для стресс-теста)
public enum LargeGameState
{
    State0, State1, State2, State3, State4,
    State5, State6, State7, State8, State9,
    State10, State11, State12, State13, State14,
    State15, State16, State17, State18, State19,
    State20, State21, State22, State23, State24
}

public enum GameTrigger
{
    Start,
    Pause,
    Resume,
    Next,
    Invalid
}

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class StateMachineBenchmarks
{
    // Маленькая конфигурация
    private StateMachine<SmallGameState, GameTrigger> _smallSM = null!;
    private StateMachine<SmallGameState, GameTrigger> _smallSMWithHandlers = null!;
    private StateMachine<SmallGameState, GameTrigger> _smallSMWithAsyncHandlers = null!;

    // Большая конфигурация
    private StateMachine<LargeGameState, GameTrigger> _largeSM = null!;
    private StateMachine<LargeGameState, GameTrigger> _largeSMWithHandlers = null!;

    // Для тестов реконфигурации
    private StateMachineConfiguration<SmallGameState, GameTrigger> _config1 = default!;
    private StateMachineConfiguration<SmallGameState, GameTrigger> _config2 = default!;
    private StateMachine<SmallGameState, GameTrigger> _reconfigurableSM = null!;

    // Счетчики для обработчиков
    private int _enterCount;
    private int _exitCount;

    [GlobalSetup]
    public void Setup()
    {
        SetupSmallStateMachines();
        SetupLargeStateMachines();
        SetupReconfigurableStateMachine();
    }

    private void SetupSmallStateMachines()
    {
        // 1. Маленькая SM без обработчиков
        var builder = new StateMachineConfigurationBuilder<SmallGameState, GameTrigger>();
        builder.ConfigureState(SmallGameState.Idle)
            .Permit(GameTrigger.Start, SmallGameState.Running);
        builder.ConfigureState(SmallGameState.Running)
            .Permit(GameTrigger.Pause, SmallGameState.Paused);
        builder.ConfigureState(SmallGameState.Paused)
            .Permit(GameTrigger.Resume, SmallGameState.Running);
        _smallSM = new StateMachine<SmallGameState, GameTrigger>(SmallGameState.Idle, builder.GetConfiguration());

        // 2. Маленькая SM с синхронными обработчиками
        var builderSync = new StateMachineConfigurationBuilder<SmallGameState, GameTrigger>();
        builderSync.ConfigureState(SmallGameState.Idle)
            .OnEnter(() => _enterCount++)
            .OnExit(() => _exitCount++)
            .Permit(GameTrigger.Start, SmallGameState.Running);
        builderSync.ConfigureState(SmallGameState.Running)
            .OnEnter(() => _enterCount++)
            .OnExit(() => _exitCount++)
            .Permit(GameTrigger.Pause, SmallGameState.Paused);
        builderSync.ConfigureState(SmallGameState.Paused)
            .OnEnter(() => _enterCount++)
            .OnExit(() => _exitCount++)
            .Permit(GameTrigger.Resume, SmallGameState.Running);
        _smallSMWithHandlers = new StateMachine<SmallGameState, GameTrigger>(SmallGameState.Idle, builderSync.GetConfiguration());

        // 3. Маленькая SM с асинхронными обработчиками
        var builderAsync = new StateMachineConfigurationBuilder<SmallGameState, GameTrigger>();
        builderAsync.ConfigureState(SmallGameState.Idle)
            .OnEnterAsync(async () => { await Task.Yield(); _enterCount++; })
            .OnExitAsync(async () => { await Task.Yield(); _exitCount++; })
            .Permit(GameTrigger.Start, SmallGameState.Running);
        builderAsync.ConfigureState(SmallGameState.Running)
            .OnEnterAsync(async () => { await Task.Yield(); _enterCount++; })
            .OnExitAsync(async () => { await Task.Yield(); _exitCount++; })
            .Permit(GameTrigger.Pause, SmallGameState.Paused);
        builderAsync.ConfigureState(SmallGameState.Paused)
            .OnEnterAsync(async () => { await Task.Yield(); _enterCount++; })
            .OnExitAsync(async () => { await Task.Yield(); _exitCount++; })
            .Permit(GameTrigger.Resume, SmallGameState.Running);
        _smallSMWithAsyncHandlers = new StateMachine<SmallGameState, GameTrigger>(SmallGameState.Idle, builderAsync.GetConfiguration());
    }

    private void SetupLargeStateMachines()
    {
        // 1. Большая SM без обработчиков (25 состояний по кругу)
        var builder = new StateMachineConfigurationBuilder<LargeGameState, GameTrigger>();
        for (int i = 0; i < 25; i++)
        {
            var current = (LargeGameState)i;
            var next = (LargeGameState)((i + 1) % 25);
            builder.ConfigureState(current).Permit(GameTrigger.Next, next);
        }
        _largeSM = new StateMachine<LargeGameState, GameTrigger>(LargeGameState.State0, builder.GetConfiguration());

        // 2. Большая SM с синхронными обработчиками
        var builderSync = new StateMachineConfigurationBuilder<LargeGameState, GameTrigger>();
        for (int i = 0; i < 25; i++)
        {
            var current = (LargeGameState)i;
            var next = (LargeGameState)((i + 1) % 25);
            builderSync.ConfigureState(current)
                .OnEnter(() => _enterCount++)
                .OnExit(() => _exitCount++)
                .Permit(GameTrigger.Next, next);
        }
        _largeSMWithHandlers = new StateMachine<LargeGameState, GameTrigger>(LargeGameState.State0, builderSync.GetConfiguration());
    }

    private void SetupReconfigurableStateMachine()
    {
        // Конфигурация 1
        var builder1 = new StateMachineConfigurationBuilder<SmallGameState, GameTrigger>();
        builder1.ConfigureState(SmallGameState.Idle)
            .Permit(GameTrigger.Start, SmallGameState.Running);
        builder1.ConfigureState(SmallGameState.Running)
            .Permit(GameTrigger.Pause, SmallGameState.Paused);
        _config1 = builder1.GetConfiguration();

        // Конфигурация 2
        var builder2 = new StateMachineConfigurationBuilder<SmallGameState, GameTrigger>();
        builder2.ConfigureState(SmallGameState.Idle)
            .Permit(GameTrigger.Start, SmallGameState.Paused);
        builder2.ConfigureState(SmallGameState.Paused)
            .Permit(GameTrigger.Resume, SmallGameState.Running);
        _config2 = builder2.GetConfiguration();

        _reconfigurableSM = new StateMachine<SmallGameState, GameTrigger>(SmallGameState.Idle, _config1);
    }

    // ============== БАЗОВЫЕ ТЕСТЫ (малая конфигурация) ==============

    [Benchmark]
    public void Small_Fire_NoHandlers()
    {
        _smallSM.ForceTransition(SmallGameState.Idle);
        _smallSM.Fire(GameTrigger.Start);
    }

    [Benchmark]
    public void Small_Fire_WithSyncHandlers()
    {
        _enterCount = 0;
        _exitCount = 0;
        _smallSMWithHandlers.ForceTransition(SmallGameState.Idle);
        _smallSMWithHandlers.Fire(GameTrigger.Start);
    }

    [Benchmark]
    public void Small_TryFire_NoHandlers()
    {
        _smallSM.ForceTransition(SmallGameState.Idle);
        _smallSM.TryFire(GameTrigger.Start);
    }

    // ============== БОЛЬШАЯ КОНФИГУРАЦИЯ ==============

    [Benchmark]
    public void Large_Fire_NoHandlers()
    {
        _largeSM.ForceTransition(LargeGameState.State0);
        _largeSM.Fire(GameTrigger.Next);
    }

    [Benchmark]
    public void Large_Fire_WithSyncHandlers()
    {
        _enterCount = 0;
        _exitCount = 0;
        _largeSMWithHandlers.ForceTransition(LargeGameState.State0);
        _largeSMWithHandlers.Fire(GameTrigger.Next);
    }

    // ============== ВЫСОКОЧАСТОТНЫЕ ПЕРЕХОДЫ (симуляция 60fps) ==============

    [Benchmark]
    public void Small_HighFrequency_60Transitions_NoHandlers()
    {
        for (int i = 0; i < 60; i++)
        {
            _smallSM.ForceTransition(SmallGameState.Idle);
            _smallSM.TryFire(GameTrigger.Start);
            _smallSM.TryFire(GameTrigger.Pause);
            _smallSM.TryFire(GameTrigger.Resume);
        }
    }

    [Benchmark]
    public void Small_HighFrequency_60Transitions_WithHandlers()
    {
        _enterCount = 0;
        _exitCount = 0;
        for (int i = 0; i < 60; i++)
        {
            _smallSMWithHandlers.ForceTransition(SmallGameState.Idle);
            _smallSMWithHandlers.TryFire(GameTrigger.Start);
            _smallSMWithHandlers.TryFire(GameTrigger.Pause);
            _smallSMWithHandlers.TryFire(GameTrigger.Resume);
        }
    }

    [Benchmark]
    public void Large_HighFrequency_60Transitions_NoHandlers()
    {
        for (int i = 0; i < 60; i++)
        {
            _largeSM.ForceTransition(LargeGameState.State0);
            _largeSM.TryFire(GameTrigger.Next);
        }
    }

    [Benchmark]
    public void Large_HighFrequency_60Transitions_WithHandlers()
    {
        _enterCount = 0;
        _exitCount = 0;
        for (int i = 0; i < 60; i++)
        {
            _largeSMWithHandlers.ForceTransition(LargeGameState.State0);
            _largeSMWithHandlers.TryFire(GameTrigger.Next);
        }
    }

    // ============== ЦЕПОЧКА ПЕРЕХОДОВ ==============

    [Benchmark]
    public void Small_TransitionChain_10Steps()
    {
        for (int i = 0; i < 10; i++)
        {
            _smallSM.ForceTransition(SmallGameState.Idle);
            _smallSM.TryFire(GameTrigger.Start);    // Idle -> Running
            _smallSM.TryFire(GameTrigger.Pause);     // Running -> Paused
            _smallSM.TryFire(GameTrigger.Resume);    // Paused -> Running
        }
    }

    [Benchmark]
    public void Large_TransitionChain_FullCircle()
    {
        _largeSM.ForceTransition(LargeGameState.State0);
        // Проходим весь круг (25 состояний)
        for (int i = 0; i < 25; i++)
        {
            _largeSM.TryFire(GameTrigger.Next);
        }
    }

    // ============== РЕКОНФИГУРАЦИЯ В РАНТАЙМЕ ==============

    [Benchmark]
    public void Reconfigure_SwitchConfiguration()
    {
        _reconfigurableSM.Configure(_config1);
        _reconfigurableSM.ForceTransition(SmallGameState.Idle);
        _reconfigurableSM.TryFire(GameTrigger.Start);

        _reconfigurableSM.Configure(_config2);
        _reconfigurableSM.ForceTransition(SmallGameState.Idle);
        _reconfigurableSM.TryFire(GameTrigger.Start);
    }

    [Benchmark]
    public void Reconfigure_HighFrequency_30Times()
    {
        for (int i = 0; i < 30; i++)
        {
            var config = i % 2 == 0 ? _config1 : _config2;
            _reconfigurableSM.Configure(config);
            _reconfigurableSM.ForceTransition(SmallGameState.Idle);
            _reconfigurableSM.TryFire(GameTrigger.Start);
        }
    }

    // ============== WORST CASE: БОЛЬШАЯ КОНФИГУРАЦИЯ + ОБРАБОТЧИКИ + ЦЕПОЧКИ ==============

    [Benchmark]
    public void Large_WorstCase_100TransitionsWithHandlers()
    {
        _enterCount = 0;
        _exitCount = 0;

        for (int i = 0; i < 100; i++)
        {
            _largeSMWithHandlers.ForceTransition((LargeGameState)(i % 25));
            _largeSMWithHandlers.TryFire(GameTrigger.Next);
        }
    }

    // ============== СТРЕСС-ТЕСТ: СИМУЛЯЦИЯ ИГРОВОГО ЦИКЛА ==============

    [Benchmark]
    public void GameLoop_Simulation_1000Frames()
    {
        // Симулируем 1000 кадров игры с переходами каждые несколько кадров
        for (int frame = 0; frame < 1000; frame++)
        {
            if (frame % 10 == 0)
            {
                _smallSM.ForceTransition(SmallGameState.Idle);
                _smallSM.TryFire(GameTrigger.Start);
            }
            else if (frame % 15 == 0)
            {
                _smallSM.TryFire(GameTrigger.Pause);
            }
            else if (frame % 20 == 0)
            {
                _smallSM.TryFire(GameTrigger.Resume);
            }
        }
    }

    [Benchmark]
    public void GameLoop_Simulation_1000Frames_WithHandlers()
    {
        _enterCount = 0;
        _exitCount = 0;

        for (int frame = 0; frame < 1000; frame++)
        {
            if (frame % 10 == 0)
            {
                _smallSMWithHandlers.ForceTransition(SmallGameState.Idle);
                _smallSMWithHandlers.TryFire(GameTrigger.Start);
            }
            else if (frame % 15 == 0)
            {
                _smallSMWithHandlers.TryFire(GameTrigger.Pause);
            }
            else if (frame % 20 == 0)
            {
                _smallSMWithHandlers.TryFire(GameTrigger.Resume);
            }
        }
    }

    // ============== НЕВАЛИДНЫЕ ПЕРЕХОДЫ (проверка обработки ошибок) ==============

    [Benchmark]
    public void Small_InvalidTransitions_TryFire()
    {
        for (int i = 0; i < 100; i++)
        {
            _smallSM.ForceTransition(SmallGameState.Idle);
            _smallSM.TryFire(GameTrigger.Invalid); // вернёт false
        }
    }

    // Тест влияния количества guards
    [Benchmark]
    public void Guards_MultipleConditions_10Guards()
    {
        var builder = new StateMachineConfigurationBuilder<SmallGameState, GameTrigger>();
        builder.ConfigureState(SmallGameState.Idle)
            .PermitIf(GameTrigger.Start, SmallGameState.Running, () => _enterCount > 5)
            .PermitIf(GameTrigger.Start, SmallGameState.Running, () => _exitCount > 10)
            .PermitIf(GameTrigger.Start, SmallGameState.Running, () => true);

        var sm = new StateMachine<SmallGameState, GameTrigger>(SmallGameState.Idle, builder.GetConfiguration());
        sm.TryFire(GameTrigger.Start);
    }

    // Тест аллокаций при создании SM
    [Benchmark]
    public StateMachine<SmallGameState, GameTrigger> Create_NewStateMachine()
    {
        var builder = new StateMachineConfigurationBuilder<SmallGameState, GameTrigger>();
        builder.ConfigureState(SmallGameState.Idle)
            .Permit(GameTrigger.Start, SmallGameState.Running);
        return new StateMachine<SmallGameState, GameTrigger>(SmallGameState.Idle, builder.GetConfiguration());
    }

    private volatile int _sideEffect; // Предотвращает DCE

    [Benchmark]
    public void Small_Fire_NoHandlers_WithSideEffect()
    {
        _smallSM.ForceTransition(SmallGameState.Idle);
        _smallSM.Fire(GameTrigger.Start);

        // Используем результат, чтобы предотвратить оптимизацию
        _sideEffect = _smallSM.СurrentState.GetHashCode();
        _sideEffect.GetHashCode();
    }
}

public static class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<StateMachineBenchmarks>();
    }
}

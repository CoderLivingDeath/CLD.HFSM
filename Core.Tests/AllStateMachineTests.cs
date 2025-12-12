using CLD.HFSM;

[TestClass]
public class StateMachineTests
{
    private enum GameState
    {
        Idle,
        Running,
        Paused,
        GameOver
    }

    private enum GameTrigger
    {
        Start,
        Pause,
        Resume,
        Stop,
        Complete,
        Fail
    }

    private StateMachine<GameState, GameTrigger> CreateBasicStateMachine()
    {
        var builder = new StateMachineConfigurationBuilder<GameState, GameTrigger>();

        builder.ConfigureState(GameState.Idle)
            .Permit(GameTrigger.Start, GameState.Running);

        builder.ConfigureState(GameState.Running)
            .Permit(GameTrigger.Pause, GameState.Paused)
            .Permit(GameTrigger.Complete, GameState.GameOver)
            .Permit(GameTrigger.Stop, GameState.Idle);

        builder.ConfigureState(GameState.Paused)
            .Permit(GameTrigger.Resume, GameState.Running)
            .Permit(GameTrigger.Stop, GameState.Idle);

        builder.ConfigureState(GameState.GameOver)
            .Permit(GameTrigger.Start, GameState.Idle);

        var config = builder.GetConfiguration();
        return new StateMachine<GameState, GameTrigger>(GameState.Idle, config);
    }

    // ==================== БАЗОВЫЕ ТЕСТЫ ====================

    [TestMethod]
    public void Constructor_WithConfiguration_InitializesWithFirstState()
    {
        var sm = CreateBasicStateMachine();
        Assert.AreEqual(GameState.Idle, sm.CurrentState);
    }

    // ==================== ПРОСТЫЕ ПЕРЕХОДЫ ====================

    [TestMethod]
    public void Fire_WithValidTrigger_TransitionsToTargetState()
    {
        var sm = CreateBasicStateMachine();
        sm.Fire(GameTrigger.Start);
        Assert.AreEqual(GameState.Running, sm.CurrentState);
    }

    [TestMethod]
    public void Fire_WithInvalidTrigger_ThrowsInvalidOperationException()
    {
        var sm = CreateBasicStateMachine();

        Assert.Throws<InvalidOperationException>(
            () => sm.Fire(GameTrigger.Resume, true));
    }

    [TestMethod]
    public void Fire_MultipleValidTransitions_MaintainsState()
    {
        var sm = CreateBasicStateMachine();
        sm.Fire(GameTrigger.Start);
        sm.Fire(GameTrigger.Pause);
        sm.Fire(GameTrigger.Resume);
        sm.Fire(GameTrigger.Complete);
        Assert.AreEqual(GameState.GameOver, sm.CurrentState);
    }

    [TestMethod]
    public void TryFire_WithValidTrigger_ReturnsTrue()
    {
        var sm = CreateBasicStateMachine();
        var result = sm.TryFire(GameTrigger.Start);
        Assert.IsTrue(result);
        Assert.AreEqual(GameState.Running, sm.CurrentState);
    }

    [TestMethod]
    public void TryFire_WithInvalidTrigger_ReturnsFalse()
    {
        var sm = CreateBasicStateMachine();
        var result = sm.TryFire(GameTrigger.Resume);
        Assert.IsFalse(result);
        Assert.AreEqual(GameState.Idle, sm.CurrentState);
    }

    [TestMethod]
    public void TryFire_WithInvalidTrigger_DoesNotChangeState()
    {
        var sm = CreateBasicStateMachine();
        sm.Fire(GameTrigger.Start);
        var initialState = sm.CurrentState;
        sm.TryFire(GameTrigger.Start);
        Assert.AreEqual(initialState, sm.CurrentState);
    }

    // ==================== ОБРАБОТЧИКИ СОСТОЯНИЙ ====================

    [TestMethod]
    public void Fire_WithEnterHandler_CallsHandlerOnEnter()
    {
        var enterCalled = false;
        var builder = new StateMachineConfigurationBuilder<GameState, GameTrigger>();

        builder.ConfigureState(GameState.Idle)
            .Permit(GameTrigger.Start, GameState.Running);

        builder.ConfigureState(GameState.Running)
            .OnEnter(() => enterCalled = true);

        var config = builder.GetConfiguration();
        var sm = new StateMachine<GameState, GameTrigger>(GameState.Idle, config);

        sm.Fire(GameTrigger.Start);
        Assert.IsTrue(enterCalled);
    }

    [TestMethod]
    public void Fire_WithExitHandler_CallsHandlerOnExit()
    {
        var exitCalled = false;
        var builder = new StateMachineConfigurationBuilder<GameState, GameTrigger>();

        builder.ConfigureState(GameState.Idle)
            .OnExit(() => exitCalled = true)
            .Permit(GameTrigger.Start, GameState.Running);

        builder.ConfigureState(GameState.Running);

        var config = builder.GetConfiguration();
        var sm = new StateMachine<GameState, GameTrigger>(GameState.Idle, config);

        sm.Fire(GameTrigger.Start);
        Assert.IsTrue(exitCalled);
    }

    [TestMethod]
    public void Fire_WithBothHandlers_CallsExitThenEnter()
    {
        var callOrder = new List<string>();
        var builder = new StateMachineConfigurationBuilder<GameState, GameTrigger>();

        builder.ConfigureState(GameState.Idle)
            .OnExit(() => callOrder.Add("Exit"))
            .Permit(GameTrigger.Start, GameState.Running);

        builder.ConfigureState(GameState.Running)
            .OnEnter(() => callOrder.Add("Enter"));

        var config = builder.GetConfiguration();
        var sm = new StateMachine<GameState, GameTrigger>(GameState.Idle, config);

        sm.Fire(GameTrigger.Start);
        Assert.AreEqual(2, callOrder.Count);
        Assert.AreEqual("Exit", callOrder[0]);
        Assert.AreEqual("Enter", callOrder[1]);
    }

    [TestMethod]
    public void Fire_WithoutHandlers_DoesNotThrow()
    {
        var builder = new StateMachineConfigurationBuilder<GameState, GameTrigger>();

        builder.ConfigureState(GameState.Idle)
            .Permit(GameTrigger.Start, GameState.Running);

        builder.ConfigureState(GameState.Running);

        var config = builder.GetConfiguration();
        var sm = new StateMachine<GameState, GameTrigger>(GameState.Idle, config);

        sm.Fire(GameTrigger.Start);
        Assert.AreEqual(GameState.Running, sm.CurrentState);
    }

    [TestMethod]
    public void Fire_WithMultipleHandlers_AllHandlersCalled()
    {
        var exitCount = 0;
        var enterCount = 0;
        var builder = new StateMachineConfigurationBuilder<GameState, GameTrigger>();

        builder.ConfigureState(GameState.Idle)
            .OnExit(() => exitCount++)
            .Permit(GameTrigger.Start, GameState.Running);

        builder.ConfigureState(GameState.Running)
            .OnEnter(() => enterCount++);

        builder.ConfigureState(GameState.Paused);

        var config = builder.GetConfiguration();
        var sm = new StateMachine<GameState, GameTrigger>(GameState.Idle, config);

        sm.Fire(GameTrigger.Start);
        Assert.AreEqual(1, exitCount);
        Assert.AreEqual(1, enterCount);
    }

    // ==================== УСЛОВНЫЕ ПЕРЕХОДЫ (GUARDS) ====================

    [TestMethod]
    public void TryFire_WithGuardedTransition_AndGuardTrue_Transitions()
    {
        var builder = new StateMachineConfigurationBuilder<GameState, GameTrigger>();

        builder.ConfigureState(GameState.Idle)
            .PermitIf(GameTrigger.Start, GameState.Running, () => true);

        builder.ConfigureState(GameState.Running);

        var config = builder.GetConfiguration();
        var sm = new StateMachine<GameState, GameTrigger>(GameState.Idle, config);

        var result = sm.TryFire(GameTrigger.Start);
        Assert.IsTrue(result);
        Assert.AreEqual(GameState.Running, sm.CurrentState);
    }

    [TestMethod]
    public void TryFire_WithGuardedTransition_AndGuardFalse_DoesNotTransition()
    {
        var builder = new StateMachineConfigurationBuilder<GameState, GameTrigger>();

        builder.ConfigureState(GameState.Idle)
            .PermitIf(GameTrigger.Start, GameState.Running, () => false);

        builder.ConfigureState(GameState.Running);

        var config = builder.GetConfiguration();
        var sm = new StateMachine<GameState, GameTrigger>(GameState.Idle, config);

        var result = sm.TryFire(GameTrigger.Start);
        Assert.IsFalse(result);
        Assert.AreEqual(GameState.Idle, sm.CurrentState);
    }

    [TestMethod]
    public void TryFire_WithGuardedTransition_CallsGuardFunction()
    {
        var guardCalled = false;
        var builder = new StateMachineConfigurationBuilder<GameState, GameTrigger>();

        builder.ConfigureState(GameState.Idle)
            .PermitIf(GameTrigger.Start, GameState.Running, () =>
            {
                guardCalled = true;
                return true;
            });

        builder.ConfigureState(GameState.Running);

        var config = builder.GetConfiguration();
        var sm = new StateMachine<GameState, GameTrigger>(GameState.Idle, config);

        sm.TryFire(GameTrigger.Start);
        Assert.IsTrue(guardCalled);
    }

    [TestMethod]
    public void TryFire_WithMultipleGuards_FirstTrueGuardWins()
    {
        var firstGuardCalled = false;
        var secondGuardCalled = false;

        var builder = new StateMachineConfigurationBuilder<GameState, GameTrigger>();

        builder.ConfigureState(GameState.Idle)
            .PermitIf(GameTrigger.Start, GameState.Paused, () =>
            {
                firstGuardCalled = true;
                return false;
            })
            .PermitIf(GameTrigger.Start, GameState.Running, () =>
            {
                secondGuardCalled = true;
                return true;
            });

        builder.ConfigureState(GameState.Paused);
        builder.ConfigureState(GameState.Running);

        var config = builder.GetConfiguration();
        var sm = new StateMachine<GameState, GameTrigger>(GameState.Idle, config);

        sm.TryFire(GameTrigger.Start);
        Assert.IsTrue(firstGuardCalled);
        Assert.IsTrue(secondGuardCalled);
        Assert.AreEqual(GameState.Running, sm.CurrentState);
    }

    [TestMethod]
    public void TryFire_WithGuardedTransition_AndGuardThrows_PropagatesException()
    {
        // arrange
        var builder = new StateMachineConfigurationBuilder<GameState, GameTrigger>();

        builder.ConfigureState(GameState.Idle)
            .PermitIf(
                GameTrigger.Start,
                GameState.Running,
                () => throw new InvalidOperationException("Guard error")
            );

        builder.ConfigureState(GameState.Running);

        var config = builder.GetConfiguration();
        var sm = new StateMachine<GameState, GameTrigger>(GameState.Idle, config);

        // act + assert
        var ex = Assert.Throws<InvalidOperationException>(
            () => sm.TryFire(GameTrigger.Start));

        Assert.AreEqual("Guard error", ex.Message);
    }


    // ==================== FORCE TRANSITION ====================

    [TestMethod]
    public void ForceTransition_WithValidState_ChangesState()
    {
        var sm = CreateBasicStateMachine();
        sm.ForceTransition(GameState.GameOver);
        Assert.AreEqual(GameState.GameOver, sm.CurrentState);
    }

    [TestMethod]
    public void ForceTransition_WithoutValidConfiguration_ChangeStateAnyway()
    {
        var sm = CreateBasicStateMachine();
        sm.ForceTransition(GameState.Paused);
        Assert.AreEqual(GameState.Paused, sm.CurrentState);
    }

    [TestMethod]
    public void ForceTransition_DoesNotCallHandlers()
    {
        var enterCalled = false;
        var exitCalled = false;

        var builder = new StateMachineConfigurationBuilder<GameState, GameTrigger>();

        builder.ConfigureState(GameState.Idle)
            .OnExit(() => exitCalled = true);

        builder.ConfigureState(GameState.Running)
            .OnEnter(() => enterCalled = true);

        var config = builder.GetConfiguration();
        var sm = new StateMachine<GameState, GameTrigger>(GameState.Idle, config);

        sm.ForceTransition(GameState.Running);
        Assert.IsFalse(enterCalled);
        Assert.IsFalse(exitCalled);
    }

    // ==================== BUILDER API ====================

    [TestMethod]
    public void StateConfigurationBuilder_Fluent_ChainsCalls()
    {
        var builder = new StateConfigurationBuilder<GameState, GameTrigger>(GameState.Idle);
        var result = builder
            .Permit(GameTrigger.Start, GameState.Running)
            .Permit(GameTrigger.Pause, GameState.Paused)
            .OnEnter(() => { })
            .OnExit(() => { });

        Assert.IsNotNull(result);
        var config = result.GetConfiguration();
        Assert.AreEqual(GameState.Idle, config.State);
        Assert.AreEqual(2, config.GuardedTransitions.Length);
    }

    [TestMethod]
    public void StateMachineConfigurationBuilder_Fluent_BuildsConfiguration()
    {
        var builder = new StateMachineConfigurationBuilder<GameState, GameTrigger>();

        builder.ConfigureState(GameState.Idle)
            .Permit(GameTrigger.Start, GameState.Running);

        builder.ConfigureState(GameState.Running)
            .Permit(GameTrigger.Stop, GameState.Idle);

        var config = builder.GetConfiguration();
        Assert.HasCount(2, config.StateConfigurations);
    }

    [TestMethod]
    public void StateConfigurationBuilder_WithNoTransitions_CreatesEmptyArray()
    {
        var builder = new StateConfigurationBuilder<GameState, GameTrigger>(GameState.Idle);
        var config = builder.GetConfiguration();
        Assert.AreEqual(0, config.GuardedTransitions.Length);
    }

    [TestMethod]
    public void StateConfigurationBuilder_WithMultipleTransitions_StoresAll()
    {
        var builder = new StateConfigurationBuilder<GameState, GameTrigger>(GameState.Running);

        builder
            .Permit(GameTrigger.Pause, GameState.Paused)
            .Permit(GameTrigger.Stop, GameState.Idle)
            .Permit(GameTrigger.Complete, GameState.GameOver);

        var config = builder.GetConfiguration();
        Assert.AreEqual(3, config.GuardedTransitions.Length);
    }

    // ==================== TRANSITION CONTEXT ====================

    [TestMethod]
    public void TransitionContext_Constructor_StoresValues()
    {
        var ctx = new TransitionContext<GameState, GameTrigger>(
            GameState.Idle,
            GameState.Running,
            GameTrigger.Start
        );

        Assert.AreEqual(GameState.Idle, ctx.SourceState);
        Assert.AreEqual(GameState.Running, ctx.TargetState);
        Assert.AreEqual(GameTrigger.Start, ctx.Trigger);
    }

    // ==================== STATE HANDLERS ====================

    [TestMethod]
    public void StateHandlers_OnEnter_CallsEnterAction()
    {
        var called = false;
        var handlers = new StateHandlers(enter: () => called = true);
        handlers.OnEnter();
        Assert.IsTrue(called);
    }

    [TestMethod]
    public void StateHandlers_OnExit_CallsExitAction()
    {
        var called = false;
        var handlers = new StateHandlers(exit: () => called = true);
        handlers.OnExit();
        Assert.IsTrue(called);
    }

    [TestMethod]
    public void StateHandlers_WithNullActions_DoesNotThrow()
    {
        var handlers = new StateHandlers(null, null);
        handlers.OnEnter();
        handlers.OnExit();
        Assert.IsNotNull(handlers);
    }

    [TestMethod]
    public void StateHandlersAsync_OnEnter_WithAsyncAction()
    {
        async ValueTask AsyncEnter() => await Task.Delay(1);

        var handlers = new StateHandlersAsync(
            AsyncEnter,
            null
        );

        handlers.OnEnter();
        Assert.IsNotNull(handlers.Enter);
    }

    [TestMethod]
    public void StateHandlersAsync_WithNullActions_DoesNotThrow()
    {
        var handlers = new StateHandlersAsync(null, null);
        handlers.OnEnter();
        handlers.OnExit();
        Assert.IsNotNull(handlers);
    }

    // ==================== EDGE CASES ====================


    [TestMethod]
    public void Fire_SelfTransition_WorksCorrectly()
    {
        var enterCount = 0;
        var exitCount = 0;

        var builder = new StateMachineConfigurationBuilder<GameState, GameTrigger>();

        builder.ConfigureState(GameState.Idle)
            .OnEnter(() => enterCount++)
            .OnExit(() => exitCount++)
            .Permit(GameTrigger.Start, GameState.Idle);

        var config = builder.GetConfiguration();
        var sm = new StateMachine<GameState, GameTrigger>(GameState.Idle, config);

        sm.Fire(GameTrigger.Start);
        Assert.AreEqual(GameState.Idle, sm.CurrentState);
        Assert.AreEqual(1, exitCount);
        Assert.AreEqual(1, enterCount);
    }

    [TestMethod]
    public void TryFire_MultipleTriggers_EachTreatedIndependently()
    {
        var builder = new StateMachineConfigurationBuilder<GameState, GameTrigger>();

        builder.ConfigureState(GameState.Idle)
            .Permit(GameTrigger.Start, GameState.Running);

        builder.ConfigureState(GameState.Running)
            .Permit(GameTrigger.Pause, GameState.Paused)
            .Permit(GameTrigger.Complete, GameState.GameOver);

        builder.ConfigureState(GameState.Paused)
            .Permit(GameTrigger.Resume, GameState.Running);

        builder.ConfigureState(GameState.GameOver);

        var config = builder.GetConfiguration();
        var sm = new StateMachine<GameState, GameTrigger>(GameState.Idle, config);

        Assert.IsTrue(sm.TryFire(GameTrigger.Start));
        Assert.IsTrue(sm.TryFire(GameTrigger.Pause));
        Assert.IsFalse(sm.TryFire(GameTrigger.Pause));
        Assert.IsTrue(sm.TryFire(GameTrigger.Resume));
        Assert.IsTrue(sm.TryFire(GameTrigger.Complete));
        Assert.AreEqual(GameState.GameOver, sm.CurrentState);
    }

    [TestMethod]
    public void Builder_AllowsReconfiguringState()
    {
        var builder = new StateMachineConfigurationBuilder<GameState, GameTrigger>();

        builder.ConfigureState(GameState.Idle)
            .Permit(GameTrigger.Start, GameState.Running);

        var config1 = builder.GetConfiguration();

        builder.ConfigureState(GameState.Paused)
            .Permit(GameTrigger.Resume, GameState.Running);

        var config2 = builder.GetConfiguration();

        Assert.HasCount(1, config1.StateConfigurations);
        Assert.HasCount(2, config2.StateConfigurations);
    }

    // ==================== STRESS TESTS ====================

    [TestMethod]
    public void Fire_ManyTransitions_MaintainsCorrectState()
    {
        var builder = new StateMachineConfigurationBuilder<GameState, GameTrigger>();

        builder.ConfigureState(GameState.Idle)
            .Permit(GameTrigger.Start, GameState.Running);

        builder.ConfigureState(GameState.Running)
            .Permit(GameTrigger.Pause, GameState.Paused)
            .Permit(GameTrigger.Resume, GameState.Running)
            .Permit(GameTrigger.Stop, GameState.Idle);

        builder.ConfigureState(GameState.Paused)
            .Permit(GameTrigger.Resume, GameState.Running)
            .Permit(GameTrigger.Stop, GameState.Idle);

        var config = builder.GetConfiguration();
        var sm = new StateMachine<GameState, GameTrigger>(GameState.Idle, config);

        for (int i = 0; i < 100; i++)
        {
            sm.Fire(GameTrigger.Start);
            sm.Fire(GameTrigger.Pause);
            sm.Fire(GameTrigger.Resume);
            sm.Fire(GameTrigger.Stop);
        }

        Assert.AreEqual(GameState.Idle, sm.CurrentState);
    }

    [TestMethod]
    public void TryFire_ManyInvalidTransitions_ReturnsFalse()
    {
        var sm = CreateBasicStateMachine();

        var count = 0;
        for (int i = 0; i < 100; i++)
        {
            if (!sm.TryFire(GameTrigger.Resume))
                count++;
        }

        Assert.AreEqual(100, count);
        Assert.AreEqual(GameState.Idle, sm.CurrentState);
    }
}

// ==================== ИНТЕГРАЦИОННЫЕ ТЕСТЫ ====================

[TestClass]
public class StateMachineIntegrationTests
{
    private enum PlayerState
    {
        Idle,
        Walking,
        Running,
        Jumping,
        Falling
    }

    private enum PlayerInput
    {
        Move,
        Stop,
        Jump,
        Land,
        Sprint
    }

    [TestMethod]
    public void ComplexGameScenario_PlayerMovement_WorksCorrectly()
    {
        var states = new List<string>();
        var builder = new StateMachineConfigurationBuilder<PlayerState, PlayerInput>();

        builder.ConfigureState(PlayerState.Idle)
            .OnEnter(() => states.Add("Idle:Enter"))
            .OnExit(() => states.Add("Idle:Exit"))
            .Permit(PlayerInput.Move, PlayerState.Walking)
            .Permit(PlayerInput.Jump, PlayerState.Jumping);

        builder.ConfigureState(PlayerState.Walking)
            .OnEnter(() => states.Add("Walking:Enter"))
            .OnExit(() => states.Add("Walking:Exit"))
            .Permit(PlayerInput.Stop, PlayerState.Idle)
            .Permit(PlayerInput.Sprint, PlayerState.Running)
            .Permit(PlayerInput.Jump, PlayerState.Jumping);

        builder.ConfigureState(PlayerState.Running)
            .OnEnter(() => states.Add("Running:Enter"))
            .OnExit(() => states.Add("Running:Exit"))
            .Permit(PlayerInput.Stop, PlayerState.Idle)
            .Permit(PlayerInput.Jump, PlayerState.Jumping);

        builder.ConfigureState(PlayerState.Jumping)
            .OnEnter(() => states.Add("Jumping:Enter"))
            .OnExit(() => states.Add("Jumping:Exit"))
            .Permit(PlayerInput.Land, PlayerState.Falling);

        builder.ConfigureState(PlayerState.Falling)
            .OnEnter(() => states.Add("Falling:Enter"))
            .OnExit(() => states.Add("Falling:Exit"))
            .Permit(PlayerInput.Land, PlayerState.Idle);

        var config = builder.GetConfiguration();
        var sm = new StateMachine<PlayerState, PlayerInput>(PlayerState.Idle, config);

        sm.Fire(PlayerInput.Move);
        sm.Fire(PlayerInput.Sprint);
        sm.Fire(PlayerInput.Jump);
        sm.Fire(PlayerInput.Land);
        sm.Fire(PlayerInput.Land);

        Assert.AreEqual(PlayerState.Idle, sm.CurrentState);
        Assert.IsTrue(states.Contains("Walking:Enter"));
        Assert.IsTrue(states.Contains("Running:Enter"));
        Assert.IsTrue(states.Contains("Jumping:Enter"));
        Assert.IsTrue(states.Contains("Falling:Enter"));
    }

    [TestMethod]
    public void GuardCondition_PlayerStamina_PreventsTransition()
    {
        var stamina = 10.0f;
        var builder = new StateMachineConfigurationBuilder<PlayerState, PlayerInput>();

        builder.ConfigureState(PlayerState.Idle)
            .Permit(PlayerInput.Move, PlayerState.Walking);

        builder.ConfigureState(PlayerState.Walking)
            .PermitIf(PlayerInput.Sprint, PlayerState.Running, () => stamina > 20)
            .Permit(PlayerInput.Stop, PlayerState.Idle);

        builder.ConfigureState(PlayerState.Running);

        var config = builder.GetConfiguration();
        var sm = new StateMachine<PlayerState, PlayerInput>(PlayerState.Idle, config);

        sm.Fire(PlayerInput.Move);
        var result = sm.TryFire(PlayerInput.Sprint);

        Assert.IsFalse(result);
        Assert.AreEqual(PlayerState.Walking, sm.CurrentState);

        stamina = 50;
        result = sm.TryFire(PlayerInput.Sprint);

        Assert.IsTrue(result);
        Assert.AreEqual(PlayerState.Running, sm.CurrentState);
    }
}

// ==================== РЕАЛЬНЫЕ СЦЕНАРИИ ====================

[TestClass]
public class RealWorldScenariosTests
{
    // МЕДИА-ПЛЕЕР
    private enum MediaPlayerState { Stopped, Playing, Paused, Buffering }
    private enum MediaTrigger { Play, Pause, Stop, BufferingStarted, BufferingCompleted }

    [TestMethod]
    public void MediaPlayer_PlayPauseStop_WorksCorrectly()
    {
        var builder = new StateMachineConfigurationBuilder<MediaPlayerState, MediaTrigger>();

        builder.ConfigureState(MediaPlayerState.Stopped)
            .Permit(MediaTrigger.Play, MediaPlayerState.Playing);

        builder.ConfigureState(MediaPlayerState.Playing)
            .Permit(MediaTrigger.Pause, MediaPlayerState.Paused)
            .Permit(MediaTrigger.Stop, MediaPlayerState.Stopped)
            .Permit(MediaTrigger.BufferingStarted, MediaPlayerState.Buffering);

        builder.ConfigureState(MediaPlayerState.Paused)
            .Permit(MediaTrigger.Play, MediaPlayerState.Playing)
            .Permit(MediaTrigger.Stop, MediaPlayerState.Stopped);

        builder.ConfigureState(MediaPlayerState.Buffering)
            .Permit(MediaTrigger.BufferingCompleted, MediaPlayerState.Playing);

        var config = builder.GetConfiguration();
        var sm = new StateMachine<MediaPlayerState, MediaTrigger>(MediaPlayerState.Stopped, config);

        sm.Fire(MediaTrigger.Play);
        Assert.AreEqual(MediaPlayerState.Playing, sm.CurrentState);

        sm.Fire(MediaTrigger.Pause);
        Assert.AreEqual(MediaPlayerState.Paused, sm.CurrentState);

        sm.Fire(MediaTrigger.Play);
        Assert.AreEqual(MediaPlayerState.Playing, sm.CurrentState);

        sm.Fire(MediaTrigger.Stop);
        Assert.AreEqual(MediaPlayerState.Stopped, sm.CurrentState);
    }

    // ИНТЕРНЕТ-ЗАКАЗЫ
    private enum OrderState { Pending, Processing, Shipped, Delivered, Cancelled }
    private enum OrderTrigger { Process, Ship, Deliver, Cancel }

    [TestMethod]
    public void OrderFlow_CompleteOrderProcess_WorksCorrectly()
    {
        var builder = new StateMachineConfigurationBuilder<OrderState, OrderTrigger>();

        builder.ConfigureState(OrderState.Pending)
            .Permit(OrderTrigger.Process, OrderState.Processing)
            .Permit(OrderTrigger.Cancel, OrderState.Cancelled);

        builder.ConfigureState(OrderState.Processing)
            .Permit(OrderTrigger.Ship, OrderState.Shipped)
            .Permit(OrderTrigger.Cancel, OrderState.Cancelled);

        builder.ConfigureState(OrderState.Shipped)
            .Permit(OrderTrigger.Deliver, OrderState.Delivered);

        builder.ConfigureState(OrderState.Delivered);
        builder.ConfigureState(OrderState.Cancelled);

        var config = builder.GetConfiguration();
        var sm = new StateMachine<OrderState, OrderTrigger>(OrderState.Pending, config);

        sm.Fire(OrderTrigger.Process);
        Assert.AreEqual(OrderState.Processing, sm.CurrentState);

        sm.Fire(OrderTrigger.Ship);
        Assert.AreEqual(OrderState.Shipped, sm.CurrentState);

        sm.Fire(OrderTrigger.Deliver);
        Assert.AreEqual(OrderState.Delivered, sm.CurrentState);
    }

    // TCP СОЕДИНЕНИЕ
    private enum ConnectionState { Disconnected, Connecting, Connected, Error }
    private enum ConnectionTrigger { Connect, Disconnect, Error, Retry }

    [TestMethod]
    public void TCPConnection_ConnectDisconnect_WorksCorrectly()
    {
        var builder = new StateMachineConfigurationBuilder<ConnectionState, ConnectionTrigger>();

        builder.ConfigureState(ConnectionState.Disconnected)
            .Permit(ConnectionTrigger.Connect, ConnectionState.Connecting);

        builder.ConfigureState(ConnectionState.Connecting)
            .Permit(ConnectionTrigger.Error, ConnectionState.Error)
            .Permit(ConnectionTrigger.Connect, ConnectionState.Connected);

        builder.ConfigureState(ConnectionState.Connected)
            .Permit(ConnectionTrigger.Disconnect, ConnectionState.Disconnected)
            .Permit(ConnectionTrigger.Error, ConnectionState.Error);

        builder.ConfigureState(ConnectionState.Error)
            .Permit(ConnectionTrigger.Retry, ConnectionState.Connecting);

        var config = builder.GetConfiguration();
        var sm = new StateMachine<ConnectionState, ConnectionTrigger>(ConnectionState.Disconnected, config);

        sm.Fire(ConnectionTrigger.Connect);
        Assert.AreEqual(ConnectionState.Connecting, sm.CurrentState);

        sm.Fire(ConnectionTrigger.Connect);
        Assert.AreEqual(ConnectionState.Connected, sm.CurrentState);

        sm.Fire(ConnectionTrigger.Disconnect);
        Assert.AreEqual(ConnectionState.Disconnected, sm.CurrentState);
    }
}


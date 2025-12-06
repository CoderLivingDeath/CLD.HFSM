using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CLD.HFSM
{
    public delegate void TransitionAction<TState>(TState Source, TState Target);

    public delegate void StateEnterAction();
    public delegate void StateExitAction();

    public delegate ValueTask StateEnterActionAsync();
    public delegate ValueTask StateExitActionAsync();

    public sealed class StateMachine<TState, TTrigger>
    {
        private StateMachineConfiguration<TState, TTrigger> _currentConfiguration;
        private TState _currentStateField;

        private TransitionAction<TState>? _onTransitionCallback;

        // Кэш конфигураций по состоянию
        private Dictionary<TState, StateConfiguration<TState, TTrigger>>? _stateLookup;
        private StateConfiguration<TState, TTrigger>? _currentStateConfig;

        public TState СurrentState => _currentStateField;

        public StateMachine(
            TState initialState,
            StateMachineConfiguration<TState, TTrigger> config,
            bool invokeInitialHandlers = false)
        {
            Configure(config);

            // Устанавливаем начальное состояние
            if (_stateLookup != null && _stateLookup.TryGetValue(initialState, out var initialConfig))
            {
                _currentStateField = initialState;
                _currentStateConfig = initialConfig;

                // Опционально вызываем OnEnter для начального состояния
                if (invokeInitialHandlers)
                {
                    initialConfig.SyncHandlers.OnEnter();
                    initialConfig.AsyncHandlers.OnEnter();
                }
            }
            else
            {
                throw new InvalidOperationException(
                    $"Initial state '{initialState}' is not configured in this state machine");
            }
        }

        public void Configure(StateMachineConfiguration<TState, TTrigger> config)
        {
            _currentConfiguration = config;
            _onTransitionCallback = config.OnTransition;

            _stateLookup = new Dictionary<TState, StateConfiguration<TState, TTrigger>>(
                config.StateConfigurations.Length);

            foreach (var sc in config.StateConfigurations)
                _stateLookup[sc.State] = sc;

            if (_stateLookup.TryGetValue(_currentStateField, out var currentConfig))
            {
                _currentStateConfig = currentConfig;
            }
            else if (config.StateConfigurations.Length > 0)
            {
                // Если текущее состояние не найдено, берём первое
                _currentStateField = config.StateConfigurations[0].State;
                _currentStateConfig = config.StateConfigurations[0];
            }
            else
            {
                // Конфигурация пустая
                _currentStateConfig = null;
            }
        }


        public void Fire(in TTrigger trigger, bool throwException = true)
        {
            if (TryFire(trigger))
                return;

            if (throwException)
                throw new InvalidOperationException(
                    $"No transition found for trigger {trigger} from state {СurrentState}");
        }

        public bool TryFire(in TTrigger trigger)
        {
            if (_currentStateConfig is null)
                return false;

            var stateConfig = _currentStateConfig.Value;

            // 1. Простые переходы
            if (stateConfig.TryGetTransition(trigger, out var targetState))
            {
                ExecuteTransition(new TransitionContext<TState, TTrigger>(
                    СurrentState, targetState, trigger));
                return true;
            }

            // 2. Guarded переходы
            if (stateConfig.TryGetGuardedTransition(trigger, out targetState))
            {
                ExecuteTransition(new TransitionContext<TState, TTrigger>(
                    СurrentState, targetState, trigger));
                return true;
            }

            return false;
        }

        private void ExecuteTransition(in TransitionContext<TState, TTrigger> ctx)
        {
            if (_currentStateConfig is { } sourceConfig)
                sourceConfig.SyncHandlers.OnExit();

            _currentStateField = ctx.TargetState;

            _onTransitionCallback?.Invoke(ctx.SourceState, ctx.TargetState);

            if (_stateLookup != null &&
                _stateLookup.TryGetValue(ctx.TargetState, out var targetConfig))
            {
                _currentStateConfig = targetConfig;
                targetConfig.SyncHandlers.OnEnter();
            }
            else
            {
                _currentStateConfig = null;
            }
        }

        public void ForceTransition(TState targetState, bool validate = false)
        {
            if (validate && _stateLookup != null && !_stateLookup.ContainsKey(targetState))
            {
                throw new InvalidOperationException(
                    $"State '{targetState}' is not configured in this state machine");
            }

            _currentStateField = targetState;

            if (_stateLookup != null && _stateLookup.TryGetValue(targetState, out var config))
                _currentStateConfig = config;
            else
                _currentStateConfig = null;
        }

        public void ForceTransitionWithHandlers(TState targetState, bool validate = false)
        {
            if (validate && _stateLookup != null && !_stateLookup.ContainsKey(targetState))
            {
                throw new InvalidOperationException(
                    $"State '{targetState}' is not configured in this state machine");
            }

            if (_currentStateConfig is { } sourceConfig)
            {
                sourceConfig.SyncHandlers.OnExit();
                sourceConfig.AsyncHandlers.OnExit();
            }

            var previousState = _currentStateField;
            _currentStateField = targetState;

            _onTransitionCallback?.Invoke(previousState, targetState);

            if (_stateLookup != null && _stateLookup.TryGetValue(targetState, out var targetConfig))
            {
                _currentStateConfig = targetConfig;
                targetConfig.SyncHandlers.OnEnter();
                targetConfig.AsyncHandlers.OnEnter();
            }
            else
            {
                _currentStateConfig = null;
            }
        }
    }
}

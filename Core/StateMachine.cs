using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
        private StateMachineIndex<TState, TTrigger> _index;
        private TState _currentStateField;

        public TState СurrentState => _currentStateField;

        public StateMachine(
                TState initialState,
                StateMachineConfiguration<TState, TTrigger> config,
                bool invokeInitialHandlers = false)
        {
            Configure(config);

            if (!_index.HasTransition(initialState))
                throw new InvalidOperationException(
                    $"Initial state '{initialState}' is not configured in this state machine");

            _currentStateField = initialState;

            if (invokeInitialHandlers)
            {
                if (_index.States.TryGetValue(initialState, out var stateConfig))
                {
                    stateConfig.SyncHandlers.OnEnter();
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Configure(StateMachineConfiguration<TState, TTrigger> config)
        {
            _currentConfiguration = config;
            _index = config.CreateIndex();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Fire(in TTrigger trigger, bool throwException = true)
        {
            if (TryFire(trigger))
                return;

            if (throwException)
                throw new InvalidOperationException(
                    $"No transition found for trigger {trigger} from state {СurrentState}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryFire(in TTrigger trigger)
        {
            // Используем TryGetTransitionFor (O(1))
            if (_index.TryGetTransitionFor(_currentStateField, trigger, out var simpleTrans))
            {
                ExecuteTransition(simpleTrans);
                return true;
            }

            // Используем TryGetGuardedTransitionFor (O(1))
            if (_index.TryGetGuardedTransitionsFor(_currentStateField, trigger, out var guardedTrans))
            {
                foreach (var item in guardedTrans)
                {
                    if (item.Guard())
                    {
                        ExecuteTransition(item);

                        return true;
                    }
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ExecuteTransition(in Transition<TState, TTrigger> transition)
        {
            transition.SourceHandlers.OnExit();

            _currentStateField = transition.TargetState;

            transition.TargetHandlers.OnEnter();

            _currentConfiguration.OnTransition?.Invoke(transition.SourceState, transition.TargetState);

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ForceTransition(TState targetState, bool validate = false)
        {
            if (validate && !_index.HasTransition(targetState))
                throw new InvalidOperationException(
                    $"State '{targetState}' is not configured in this state machine");

            _currentStateField = targetState;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ForceTransitionWithHandlers(TState targetState, bool validate = false)
        {
            if (validate && !_index.HasTransition(targetState))
                throw new InvalidOperationException(
                    $"State '{targetState}' is not configured in this state machine");

            var previousState = _currentStateField;

            // OnExit предыдущего состояния
            if (_index.States.TryGetValue(previousState, out var prevConfig))
                prevConfig.SyncHandlers.OnExit();

            // переход
            _currentStateField = targetState;

            // OnEnter нового состояния
            if (_index.States.TryGetValue(targetState, out var targetConfig))
                targetConfig.SyncHandlers.OnEnter();
        }
    }
}

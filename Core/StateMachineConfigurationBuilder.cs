using System;
using System.Collections.Generic;

namespace CLD.HFSM
{
    public class StateMachineConfigurationBuilder<TState, TTrigger>
    {

        private IList<StateConfigurationBuilder<TState, TTrigger>> _stateBuilders;
        private AnyStateConfigurationBuilder<TState, TTrigger>? _anyStateBuilder;
        private TransitionAction<TState>? _onTransition;

        public StateMachineConfigurationBuilder()
        {
            _stateBuilders = new List<StateConfigurationBuilder<TState, TTrigger>>();
        }

        public StateConfigurationBuilder<TState, TTrigger> ConfigureState(TState state)
        {
            var builder = new StateConfigurationBuilder<TState, TTrigger>(state);
            _stateBuilders.Add(builder);
            return builder;
        }

        public AnyStateConfigurationBuilder<TState, TTrigger> ConfigureAnyState()
        {
            var builder = new AnyStateConfigurationBuilder<TState, TTrigger>();
            _anyStateBuilder = builder;
            return builder;
        }

        /// <summary>
        /// Устанавливает глобальный callback, который вызывается при ЛЮБОМ переходе.
        /// Полезен для логирования, аналитики, дебага.
        /// </summary>
        public StateMachineConfigurationBuilder<TState, TTrigger> OnTransition(TransitionAction<TState> callback)
        {
            _onTransition = callback;
            return this;
        }

        public StateMachineConfiguration<TState, TTrigger> GetConfiguration()
        {
            var stateConfigurations = new StateConfiguration<TState, TTrigger>[_stateBuilders.Count];
            for (int i = 0; i < _stateBuilders.Count; i++)
            {
                stateConfigurations[i] = _stateBuilders[i].GetConfiguration();
            }

            var anyStateConfiguration = _anyStateBuilder?.GetConfiguration();

            return new StateMachineConfiguration<TState, TTrigger>(
                stateConfigurations,
                anyStateConfiguration,
                _onTransition);
        }
    }

    public class AnyStateConfigurationBuilder<TState, TTrigger>
    {
        private StateEnterAction? _enterHandler = null;
        private StateExitAction? _exitHandler = null;

        private StateEnterActionAsync? _enterAsyncHandler = null;
        private StateExitActionAsync? _exitAsyncHandler = null;

        private readonly List<(TTrigger trigger, TState target)> _transitions;
        private readonly List<(TTrigger trigger, Func<bool> guard, TState target)> _guardedTransitions;

        public AnyStateConfigurationBuilder()
        {
            _transitions = new List<(TTrigger trigger, TState target)>();
            _guardedTransitions = new List<(TTrigger trigger, Func<bool> guard, TState target)>();
        }

        public AnyStateConfigurationBuilder<TState, TTrigger> Permit(TTrigger trigger, TState targetState)
        {
            _transitions.Add((trigger, targetState));
            return this;
        }

        public AnyStateConfigurationBuilder<TState, TTrigger> PermitIf(TTrigger trigger, TState targetState, Func<bool> guard)
        {
            _guardedTransitions.Add((trigger, guard, targetState));
            return this;
        }

        public AnyStateConfigurationBuilder<TState, TTrigger> OnEnter(StateEnterAction onEnter)
        {
            _enterHandler += onEnter;
            return this;
        }

        public AnyStateConfigurationBuilder<TState, TTrigger> OnExit(StateExitAction onExit)
        {
            _exitHandler += onExit;
            return this;
        }

        public AnyStateConfigurationBuilder<TState, TTrigger> OnEnterAsync(StateEnterActionAsync onEnterAsync)
        {
            _enterAsyncHandler += onEnterAsync;
            return this;
        }

        public AnyStateConfigurationBuilder<TState, TTrigger> OnExitAsync(StateExitActionAsync onExitAsync)
        {
            _exitAsyncHandler += onExitAsync;
            return this;
        }

        public AnyStateConfigurationBuilder<TState, TTrigger> AddHandler(IStateHandler handler)
        {
            _enterHandler += handler.OnEnter;
            _exitHandler += handler.OnExit;
            return this;
        }

        public AnyStateConfigurationBuilder<TState, TTrigger> AddHandlerAsync(IStateHandlerAsync handler)
        {
            _enterAsyncHandler += handler.OnEnter;
            _exitAsyncHandler += handler.OnExit;
            return this;
        }

        public AnyStateConfiguration<TState, TTrigger> GetConfiguration()
        {
            var transitions = _transitions.Count > 0 ? _transitions.ToArray() : Array.Empty<(TTrigger, TState)>();

            var guardedTransitions = _guardedTransitions.Count > 0 ? _guardedTransitions.ToArray() : Array.Empty<(TTrigger, Func<bool>, TState)>();

            return new AnyStateConfiguration<TState, TTrigger>(
                new StateHandlers(_enterHandler, _enterAsyncHandler, _exitHandler, _exitAsyncHandler),
                new StateHandlersAsync(_enterAsyncHandler, _exitAsyncHandler),
                transitions,
                guardedTransitions);

        }
    }

    public class AnyStateConfiguration<TState, TTrigger>
    {
        public readonly StateHandlers SyncHandlers;
        public readonly StateHandlersAsync AsyncHandlers;

        public readonly (TTrigger trigger, TState target)[] Transitions;
        public readonly (TTrigger trigger, Func<bool> guard, TState target)[] GuardedTransitions;

        public AnyStateConfiguration(StateHandlers syncHandlers, StateHandlersAsync asyncHandlers, (TTrigger trigger, TState target)[] transitions, (TTrigger trigger, Func<bool> guard, TState target)[] guardedTransitions)
        {
            SyncHandlers = syncHandlers;
            AsyncHandlers = asyncHandlers;
            Transitions = transitions;
            GuardedTransitions = guardedTransitions;
        }
    }
}

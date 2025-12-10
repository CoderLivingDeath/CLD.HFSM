using System;
using System.Collections.Generic;

namespace CLD.HFSM
{
    public class StateConfigurationBuilder<TState, TTrigger>
    {
        private readonly TState _state;

        private StateEnterAction? _enterHandler = null;
        private StateExitAction? _exitHandler = null;

        private StateEnterActionAsync? _enterAsyncHandler = null;
        private StateExitActionAsync? _exitAsyncHandler = null;

        private bool IsSubstate = false;
        private TState SuperState = default;

        private readonly List<(TTrigger trigger, TState target)> _transitions;
        private readonly List<(TTrigger trigger, Func<bool> guard, TState target)> _guardedTransitions;

        public StateConfigurationBuilder(TState state)
        {
            _state = state;

            _transitions = new List<(TTrigger trigger, TState target)>(0);
            _guardedTransitions = new List<(TTrigger trigger, Func<bool> guard, TState target)>(0);
        }

        public StateConfigurationBuilder<TState, TTrigger> SubstateOf(TState state)
        {
            if (IsSubstate) throw new InvalidOperationException($"State already substate of {SuperState.ToString()}");

            IsSubstate = true;
            SuperState = state;
            return this;
        }

        public StateConfigurationBuilder<TState, TTrigger> Permit(TTrigger trigger, TState targetState)
        {
            _transitions.Add((trigger, targetState));
            return this;
        }

        public StateConfigurationBuilder<TState, TTrigger> PermitIf(TTrigger trigger, TState targetState, Func<bool> guard)
        {
            _guardedTransitions.Add((trigger, guard, targetState));
            return this;
        }

        public StateConfigurationBuilder<TState, TTrigger> OnEnter(StateEnterAction onEnter)
        {
            _enterHandler += onEnter;
            return this;
        }

        public StateConfigurationBuilder<TState, TTrigger> OnExit(StateExitAction onExit)
        {
            _exitHandler += onExit;
            return this;
        }

        public StateConfigurationBuilder<TState, TTrigger> OnEnterAsync(StateEnterActionAsync onEnterAsync)
        {
            _enterAsyncHandler += onEnterAsync;
            return this;
        }

        public StateConfigurationBuilder<TState, TTrigger> OnExitAsync(StateExitActionAsync onExitAsync)
        {
            _exitAsyncHandler += onExitAsync;
            return this;
        }

        public StateConfigurationBuilder<TState, TTrigger> AddHandler(IStateHandler handler)
        {
            _enterHandler += handler.OnEnter;
            _exitHandler += handler.OnExit;
            return this;
        }

        public StateConfigurationBuilder<TState, TTrigger> AddHandlerAsync(IStateHandlerAsync handler)
        {
            _enterAsyncHandler += handler.OnEnter;
            _exitAsyncHandler += handler.OnExit;
            return this;
        }

        public StateConfiguration<TState, TTrigger> GetConfiguration()
        {
            if (IsSubstate)
            {
                var transitions = _transitions.Count > 0 ? _transitions.ToArray() : Array.Empty<(TTrigger, TState)>();

                var guardedTransitions = _guardedTransitions.Count > 0 ? _guardedTransitions.ToArray() : Array.Empty<(TTrigger, Func<bool>, TState)>();

                return new StateConfiguration<TState, TTrigger>(
                    _state,
                    new StateHandlers(_enterHandler, _exitHandler),
                    new StateHandlersAsync(_enterAsyncHandler, _exitAsyncHandler),
                    transitions,
                    guardedTransitions, SuperState);
            }
            else
            {
                var transitions = _transitions.Count > 0 ? _transitions.ToArray() : Array.Empty<(TTrigger, TState)>();

                var guardedTransitions = _guardedTransitions.Count > 0 ? _guardedTransitions.ToArray() : Array.Empty<(TTrigger, Func<bool>, TState)>();

                return new StateConfiguration<TState, TTrigger>(
                    _state,
                    new StateHandlers(_enterHandler, _exitHandler),
                    new StateHandlersAsync(_enterAsyncHandler, _exitAsyncHandler),
                    transitions,
                    guardedTransitions);
            }
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

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

        private readonly List<(TTrigger trigger, Func<bool> guard, TState target)> _guardedTransitions;

        public StateConfigurationBuilder(TState state)
        {
            _state = state;

            _guardedTransitions = new List<(TTrigger trigger, Func<bool> guard, TState target)>(0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StateConfigurationBuilder<TState, TTrigger> SubstateOf(TState state)
        {
            if (IsSubstate) throw new InvalidOperationException($"State already substate of {SuperState.ToString()}");

            IsSubstate = true;
            SuperState = state;
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StateConfigurationBuilder<TState, TTrigger> Permit(TTrigger trigger, TState targetState)
        {
            _guardedTransitions.Add((trigger, StateHandlers.EmptyGuard, targetState));
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StateConfigurationBuilder<TState, TTrigger> PermitIf(TTrigger trigger, TState targetState, Func<bool> guard)
        {
            _guardedTransitions.Add((trigger, guard, targetState));
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StateConfigurationBuilder<TState, TTrigger> OnEnter(StateEnterAction onEnter)
        {
            _enterHandler += onEnter;
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StateConfigurationBuilder<TState, TTrigger> OnExit(StateExitAction onExit)
        {
            _exitHandler += onExit;
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StateConfigurationBuilder<TState, TTrigger> OnEnterAsync(StateEnterActionAsync onEnterAsync)
        {
            _enterAsyncHandler += onEnterAsync;
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StateConfigurationBuilder<TState, TTrigger> OnExitAsync(StateExitActionAsync onExitAsync)
        {
            _exitAsyncHandler += onExitAsync;
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StateConfigurationBuilder<TState, TTrigger> AddHandler(IStateHandler handler)
        {
            _enterHandler += handler.OnEnter;
            _exitHandler += handler.OnExit;
            return this;
        }

        public StateConfiguration<TState, TTrigger> GetConfiguration()
        {
            if (IsSubstate)
            {

                var guardedTransitions = _guardedTransitions.Count > 0 ? _guardedTransitions.ToArray() : Array.Empty<(TTrigger, Func<bool>, TState)>();

                return new StateConfiguration<TState, TTrigger>(
                    _state,
                    new StateHandlers(_enterHandler, _enterAsyncHandler, _exitHandler, _exitAsyncHandler),
                    new StateHandlersAsync(_enterAsyncHandler, _exitAsyncHandler),
                    guardedTransitions, SuperState);
            }
            else
            {

                var guardedTransitions = _guardedTransitions.Count > 0 ? _guardedTransitions.ToArray() : Array.Empty<(TTrigger, Func<bool>, TState)>();

                return new StateConfiguration<TState, TTrigger>(
                    _state,
                    new StateHandlers(_enterHandler, _enterAsyncHandler, _exitHandler, _exitAsyncHandler),
                    new StateHandlersAsync(_enterAsyncHandler, _exitAsyncHandler),
                    guardedTransitions);
            }
        }

    }
}

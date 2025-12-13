using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace CLD.HFSM
{
    public class AnyStateConfigurationBuilder<TState, TTrigger>
    {
        private StateEnterAction? _enterHandler = null;
        private StateExitAction? _exitHandler = null;

        private StateEnterActionAsync? _enterAsyncHandler = null;
        private StateExitActionAsync? _exitAsyncHandler = null;

        private readonly List<(TTrigger trigger, Func<bool> guard, TState target)> _guardedTransitions;

        public AnyStateConfigurationBuilder()
        {
            _guardedTransitions = new List<(TTrigger trigger, Func<bool> guard, TState target)>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AnyStateConfigurationBuilder<TState, TTrigger> Permit(TTrigger trigger, TState targetState)
        {
            _guardedTransitions.Add((trigger,StateHandlers.EmptyGuard, targetState));
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AnyStateConfigurationBuilder<TState, TTrigger> PermitIf(TTrigger trigger, TState targetState, Func<bool> guard)
        {
            _guardedTransitions.Add((trigger, guard, targetState));
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AnyStateConfigurationBuilder<TState, TTrigger> OnEnter(StateEnterAction onEnter)
        {
            _enterHandler += onEnter;
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AnyStateConfigurationBuilder<TState, TTrigger> OnExit(StateExitAction onExit)
        {
            _exitHandler += onExit;
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AnyStateConfigurationBuilder<TState, TTrigger> OnEnterAsync(StateEnterActionAsync onEnterAsync)
        {
            _enterAsyncHandler += onEnterAsync;
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AnyStateConfigurationBuilder<TState, TTrigger> OnExitAsync(StateExitActionAsync onExitAsync)
        {
            _exitAsyncHandler += onExitAsync;
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AnyStateConfigurationBuilder<TState, TTrigger> AddHandler(IStateHandler handler)
        {
            _enterHandler += handler.OnEnter;
            _exitHandler += handler.OnExit;
            return this;
        }

        public AnyStateConfiguration<TState, TTrigger> GetConfiguration()
        {
            var guardedTransitions = _guardedTransitions.Count > 0 ? _guardedTransitions.ToArray() : Array.Empty<(TTrigger, Func<bool>, TState)>();

            return new AnyStateConfiguration<TState, TTrigger>(
                new StateHandlers(_enterHandler, _enterAsyncHandler, _exitHandler, _exitAsyncHandler),
                new StateHandlersAsync(_enterAsyncHandler, _exitAsyncHandler),
                guardedTransitions);

        }
    }
}

using System;
using System.Collections.Generic;

namespace CLD.HFSM
{
    public class StateConfiguration<TState, TTrigger>
    {
        public readonly TState State;
        public readonly StateHandlers SyncHandlers;
        public readonly StateHandlersAsync AsyncHandlers;

        // 🔹 Публичные массивы для прямого доступа
        public readonly (TTrigger trigger, TState target)[] Transitions;
        public readonly (TTrigger trigger, Func<bool> guard, TState target)[] GuardedTransitions;

        public StateConfiguration(
            TState state,
            StateHandlers syncHandlers,
            StateHandlersAsync asyncHandlers,
            (TTrigger trigger, TState target)[] transitions,
            (TTrigger trigger, Func<bool> guard, TState target)[] guardedTransitions)
        {
            State = state;
            SyncHandlers = syncHandlers;
            AsyncHandlers = asyncHandlers;
            Transitions = transitions;
            GuardedTransitions = guardedTransitions;
        }

        public int TransitionCount => Transitions.Length;
        public int GuardedTransitionCount => GuardedTransitions.Length;

        public override string ToString()
        {
            return $"State: {State}, Transitions: {TransitionCount}, Guarded: {GuardedTransitionCount}";
        }
    }
}

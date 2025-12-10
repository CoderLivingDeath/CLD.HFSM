using System;

namespace CLD.HFSM
{
    public class StateConfiguration<TState, TTrigger>
    {
        public readonly TState State;
        public readonly StateHandlers SyncHandlers;
        public readonly StateHandlersAsync AsyncHandlers;

        public readonly (TTrigger trigger, TState target)[] Transitions;
        public readonly (TTrigger trigger, Func<bool> guard, TState target)[] GuardedTransitions;

        public readonly bool IsSubstate;
        public readonly TState SuperState;

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

            IsSubstate = false;
            SuperState = default;
        }

        public StateConfiguration(
            TState state,
            StateHandlers syncHandlers,
            StateHandlersAsync asyncHandlers,
            (TTrigger trigger, TState target)[] transitions,
            (TTrigger trigger, Func<bool> guard, TState target)[] guardedTransitions,
            TState superState)
        {
            State = state;
            SyncHandlers = syncHandlers;
            AsyncHandlers = asyncHandlers;
            Transitions = transitions;
            GuardedTransitions = guardedTransitions;

            IsSubstate = true;
            SuperState = superState;
        }

        public int TransitionCount => Transitions.Length;
        public int GuardedTransitionCount => GuardedTransitions.Length;

        public override string ToString()
        {
            return $"State: {State}, Transitions: {TransitionCount}, Guarded: {GuardedTransitionCount}";
        }
    }
}

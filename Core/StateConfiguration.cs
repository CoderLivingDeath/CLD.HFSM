using System;

namespace CLD.HFSM
{
    public class StateConfiguration<TState, TTrigger>
    {
        public readonly TState State;
        public readonly StateHandlers SyncHandlers;
        public readonly StateHandlersAsync AsyncHandlers;

        public readonly (TTrigger trigger, Func<bool> guard, TState target)[] GuardedTransitions;

        public readonly bool IsSubstate;
        public readonly TState SuperState;

        public StateConfiguration(
            TState state,
            StateHandlers syncHandlers,
            StateHandlersAsync asyncHandlers,
            (TTrigger trigger, Func<bool> guard, TState target)[] guardedTransitions)
        {
            State = state;
            SyncHandlers = syncHandlers;
            AsyncHandlers = asyncHandlers;
            GuardedTransitions = guardedTransitions;

            IsSubstate = false;
            SuperState = default;
        }

        public StateConfiguration(
            TState state,
            StateHandlers syncHandlers,
            StateHandlersAsync asyncHandlers,
            (TTrigger trigger, Func<bool> guard, TState target)[] guardedTransitions,
            TState superState)
        {
            State = state;
            SyncHandlers = syncHandlers;
            AsyncHandlers = asyncHandlers;
            GuardedTransitions = guardedTransitions;

            IsSubstate = true;
            SuperState = superState;
        }

        public int GuardedTransitionCount => GuardedTransitions.Length;

        public override string ToString()
        {
            return $"State: {State}, Guarded: {GuardedTransitionCount}";
        }
    }
}

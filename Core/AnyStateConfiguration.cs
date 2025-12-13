using System;

namespace CLD.HFSM
{
    public class AnyStateConfiguration<TState, TTrigger>
    {
        public readonly StateHandlers SyncHandlers;
        public readonly StateHandlersAsync AsyncHandlers;

        public readonly (TTrigger trigger, Func<bool> guard, TState target)[] GuardedTransitions;

        public AnyStateConfiguration(StateHandlers syncHandlers, StateHandlersAsync asyncHandlers, (TTrigger trigger, Func<bool> guard, TState target)[] guardedTransitions)
        {
            SyncHandlers = syncHandlers;
            AsyncHandlers = asyncHandlers;
            GuardedTransitions = guardedTransitions;
        }
    }
}

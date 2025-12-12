using System;

namespace CLD.HFSM
{
    public readonly struct GuardedTransition<TState, TTrigger>
    {
        public readonly TState SourceState;
        public readonly TState TargetState;
        public readonly TTrigger Trigger;
        public readonly StateHandlers Handlers;
        public readonly Func<bool> Guard;

        public GuardedTransition(TState sourceState, TState targetState, TTrigger trigger, StateHandlers handlers, Func<bool> guard)
        {
            SourceState = sourceState;
            TargetState = targetState;
            Trigger = trigger;
            Handlers = handlers;
            Guard = guard;
        }

        public static implicit operator Transition<TState, TTrigger>(GuardedTransition<TState, TTrigger> guarded) =>
            new Transition<TState, TTrigger>(guarded.SourceState, guarded.TargetState, guarded.Trigger, guarded.Handlers);
    }
}

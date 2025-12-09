using System;

namespace CLD.HFSM
{
    public readonly struct GuardedTransition<TState, TTrigger>
    {
        public readonly TState SourceState;
        public readonly TState TargetState;
        public readonly TTrigger Trigger;
        public readonly StateHandlers SourceHandlers;
        public readonly StateHandlers TargetHandlers;
        public readonly Func<bool> Guard;

        public GuardedTransition(TState sourceState, TState targetState, TTrigger trigger, StateHandlers sourceHandlers, StateHandlers targetHandlers, Func<bool> guard)
        {
            SourceState = sourceState;
            TargetState = targetState;
            Trigger = trigger;
            SourceHandlers = sourceHandlers;
            TargetHandlers = targetHandlers;
            Guard = guard;
        }

        public static implicit operator Transition<TState, TTrigger>(GuardedTransition<TState, TTrigger> guarded) =>
            new Transition<TState, TTrigger>(guarded.SourceState, guarded.TargetState, guarded.Trigger, guarded.SourceHandlers, guarded.TargetHandlers);
    }
}

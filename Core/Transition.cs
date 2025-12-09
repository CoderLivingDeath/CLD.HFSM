namespace CLD.HFSM
{
    public readonly struct Transition<TState, TTrigger>
    {
        public readonly TState SourceState;
        public readonly TState TargetState;
        public readonly TTrigger Trigger;

        public readonly StateHandlers SourceHandlers;
        public readonly StateHandlers TargetHandlers;

        public Transition(TState sourceState, TState targetState, TTrigger trigger, StateHandlers sourceHandlers, StateHandlers targetHandlers)
        {
            SourceState = sourceState;
            TargetState = targetState;
            Trigger = trigger;
            SourceHandlers = sourceHandlers;
            TargetHandlers = targetHandlers;
        }
    }
}

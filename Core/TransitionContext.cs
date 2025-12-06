namespace CLD.HFSM
{
    public readonly struct TransitionContext<TState, TTrigger>
    {
        public readonly TState SourceState;     // Откуда
        public readonly TState TargetState;     // Куда  
        public readonly TTrigger Trigger;       // Что вызвало

        public TransitionContext(TState sourceState, TState targetState, TTrigger trigger)
        {
            SourceState = sourceState;
            TargetState = targetState;
            Trigger = trigger;
        }
    }
}

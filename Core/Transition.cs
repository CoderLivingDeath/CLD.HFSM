namespace CLD.HFSM
{
    public readonly struct Transition<TState, TTrigger>
    {
        public readonly TState SourceState;
        public readonly TState TargetState;
        public readonly TTrigger Trigger;

        public readonly StateHandlers Handlers;

        public Transition(TState sourceState, TState targetState, TTrigger trigger, StateHandlers handlers)
        {
            SourceState = sourceState;
            TargetState = targetState;
            Trigger = trigger;
            Handlers = handlers;
        }
    }
}

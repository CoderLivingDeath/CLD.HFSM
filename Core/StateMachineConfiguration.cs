namespace CLD.HFSM
{
    public class StateMachineConfiguration<TState, TTrigger>
    {
        public readonly StateConfiguration<TState, TTrigger>[] StateConfigurations;
        public readonly TransitionAction<TState>? OnTransition;

        public StateMachineConfiguration(
            StateConfiguration<TState, TTrigger>[] stateConfigurations,
            TransitionAction<TState>? onTransition = null)
        {
            StateConfigurations = stateConfigurations;
            OnTransition = onTransition;
        }

        public IStateMachineIndex<TState, TTrigger> CreateIndex()
        {
            return new StateMachineIndex<TState, TTrigger>(this);
        }

    }
}

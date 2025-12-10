using System;

namespace CLD.HFSM
{
    public class StateMachineConfiguration<TState, TTrigger>
    {
        public readonly StateConfiguration<TState, TTrigger>[] StateConfigurations;
        public readonly AnyStateConfiguration<TState, TTrigger>? AnyStateConfiguration;
        public readonly TransitionAction<TState>? OnTransition;

        public StateMachineConfiguration(
            StateConfiguration<TState, TTrigger>[] stateConfigurations,
            AnyStateConfiguration<TState, TTrigger>? anyStateConfiguration = null,
            TransitionAction<TState>? onTransition = null)
        {
            StateConfigurations = stateConfigurations;
            AnyStateConfiguration = anyStateConfiguration;
            OnTransition = onTransition;
        }

        public IStateMachineIndex<TState, TTrigger> CreateIndex()
        {
            return new StateMachineIndex<TState, TTrigger>(this);
        }

    }
}

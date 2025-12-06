using System.Collections.Generic;

namespace CLD.HFSM
{
    public readonly struct StateMachineConfiguration<TState, TTrigger>
    {
        public readonly StateConfiguration<TState, TTrigger>[] StateConfigurations;

        // 🔹 Глобальный callback для всех переходов
        public readonly TransitionAction<TState>? OnTransition;

        public StateMachineConfiguration(
            StateConfiguration<TState, TTrigger>[] stateConfigurations,
            TransitionAction<TState>? onTransition = null)
        {
            StateConfigurations = stateConfigurations;
            OnTransition = onTransition;
        }
    }
}

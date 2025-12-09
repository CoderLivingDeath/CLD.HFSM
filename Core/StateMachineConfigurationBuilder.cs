using System.Collections.Generic;

namespace CLD.HFSM
{
    public class StateMachineConfigurationBuilder<TState, TTrigger>
    {
        private IList<StateConfigurationBuilder<TState, TTrigger>> _stateBuilders;
        private TransitionAction<TState>? _onTransition;

        public StateMachineConfigurationBuilder()
        {
            _stateBuilders = new List<StateConfigurationBuilder<TState, TTrigger>>();
        }

        public StateConfigurationBuilder<TState, TTrigger> ConfigureState(TState state)
        {
            var builder = new StateConfigurationBuilder<TState, TTrigger>(state);
            _stateBuilders.Add(builder);
            return builder;
        }

        /// <summary>
        /// Устанавливает глобальный callback, который вызывается при ЛЮБОМ переходе.
        /// Полезен для логирования, аналитики, дебага.
        /// </summary>
        public StateMachineConfigurationBuilder<TState, TTrigger> OnTransition(TransitionAction<TState> callback)
        {
            _onTransition = callback;
            return this;
        }

        public StateMachineConfiguration<TState, TTrigger> GetConfiguration()
        {
            var stateConfigurations = new StateConfiguration<TState, TTrigger>[_stateBuilders.Count];
            for (int i = 0; i < _stateBuilders.Count; i++)
            {
                stateConfigurations[i] = _stateBuilders[i].GetConfiguration();
            }

            return new StateMachineConfiguration<TState, TTrigger>(
                stateConfigurations,
                _onTransition);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace CLD.HFSM
{
    public class StateMachineIndex<TState, TTrigger>
    {
        // Индексация: State -> конфигурация
        private readonly Dictionary<TState, StateConfiguration<TState, TTrigger>> _states;

        // State -> Trigger -> target (простые переходы) - для IsPermited*
        private readonly Dictionary<TState, Dictionary<TTrigger, TState>> _simpleTransitions;

        // State -> Trigger -> список guards/targets (guarded переходы) - для IsPermited*
        private readonly Dictionary<TState, Dictionary<TTrigger, List<(Func<bool> guard, TState target)>>> _guardedTransitions;

        // Предварительно построенные переходы (0 аллокаций в runtime)
        private readonly Dictionary<TState, Dictionary<TTrigger, Transition<TState, TTrigger>>> _prebuiltSimpleTransitions;
        private readonly Dictionary<TState, Dictionary<TTrigger, GuardedTransition<TState, TTrigger>[]>> _prebuiltGuardedTransitions;

        public IReadOnlyDictionary<TState, StateConfiguration<TState, TTrigger>> States => _states;

        public StateMachineIndex(StateMachineConfiguration<TState, TTrigger> config)
        {
            _states = new Dictionary<TState, StateConfiguration<TState, TTrigger>>();
            _simpleTransitions = new Dictionary<TState, Dictionary<TTrigger, TState>>();
            _guardedTransitions = new Dictionary<TState, Dictionary<TTrigger, List<(Func<bool> guard, TState target)>>>();
            _prebuiltSimpleTransitions = new Dictionary<TState, Dictionary<TTrigger, Transition<TState, TTrigger>>>();
            _prebuiltGuardedTransitions = new Dictionary<TState, Dictionary<TTrigger, GuardedTransition<TState, TTrigger>[]>>();

            // сначала регистрируем все состояния, чтобы можно было брать таргет‑хендлеры
            foreach (var stateConfig in config.StateConfigurations)
                _states[stateConfig.State] = stateConfig;

            foreach (var stateConfig in config.StateConfigurations)
            {
                var sourceState = stateConfig.State;
                var sourceHandlers = stateConfig.SyncHandlers;

                // ---------- простые переходы ----------
                var simpleDict = new Dictionary<TTrigger, TState>();
                var prebuiltSimpleDict = new Dictionary<TTrigger, Transition<TState, TTrigger>>();

                foreach (var (trigger, target) in stateConfig.Transitions)
                {
                    simpleDict[trigger] = target;

                    // таргет‑конфиг/хендлеры
                    var targetConfig = _states[target];
                    var targetHandlers = targetConfig.SyncHandlers;

                    prebuiltSimpleDict[trigger] = new Transition<TState, TTrigger>(
                        sourceState,
                        target,
                        trigger,
                        sourceHandlers,
                        targetHandlers);
                }

                _simpleTransitions[sourceState] = simpleDict;
                _prebuiltSimpleTransitions[sourceState] = prebuiltSimpleDict;

                // ---------- guarded переходы ----------
                var guardedDict = new Dictionary<TTrigger, List<(Func<bool>, TState)>>();
                var prebuiltGuardedDict = new Dictionary<TTrigger, GuardedTransition<TState, TTrigger>[]>();

                foreach (var (trigger, guard, target) in stateConfig.GuardedTransitions)
                {
                    if (!guardedDict.TryGetValue(trigger, out var list))
                    {
                        list = new List<(Func<bool>, TState)>();
                        guardedDict[trigger] = list;
                    }
                    list.Add((guard, target));
                }
                _guardedTransitions[sourceState] = guardedDict;

                foreach (var kvp in guardedDict)
                {
                    var trigger = kvp.Key;
                    var guards = kvp.Value;
                    var prebuiltArray = new GuardedTransition<TState, TTrigger>[guards.Count];

                    for (int i = 0; i < guards.Count; i++)
                    {
                        var (guard, target) = guards[i];
                        var targetConfig = _states[target];
                        var targetHandlers = targetConfig.SyncHandlers;

                        prebuiltArray[i] = new GuardedTransition<TState, TTrigger>(
                            sourceState,
                            target,
                            trigger,
                            sourceHandlers,
                            targetHandlers,
                            guard);
                    }

                    prebuiltGuardedDict[trigger] = prebuiltArray;
                }

                _prebuiltGuardedTransitions[sourceState] = prebuiltGuardedDict;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasTransition(TState state)
        {
            return _simpleTransitions.ContainsKey(state) || _guardedTransitions.ContainsKey(state);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsPermitedTrigger(TState state, TTrigger trigger)
        {
            return _simpleTransitions.TryGetValue(state, out var simple) && simple.ContainsKey(trigger);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsPermitedGuardTrigger(TState state, TTrigger trigger)
        {
            return _guardedTransitions.TryGetValue(state, out var guarded) && guarded.ContainsKey(trigger);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetTransitionFor(TState state, TTrigger trigger, out Transition<TState, TTrigger> transition)
        {
            if (_prebuiltSimpleTransitions.TryGetValue(state, out var triggerDict) &&
                triggerDict.TryGetValue(trigger, out transition))
            {
                return true;
            }

            transition = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetGuardedTransitionsFor(
            TState state,
            TTrigger trigger,
            out ReadOnlySpan<GuardedTransition<TState, TTrigger>> transitions)
        {
            if (_prebuiltGuardedTransitions.TryGetValue(state, out var triggerDict) &&
                triggerDict.TryGetValue(trigger, out var array))
            {
                transitions = array.AsSpan();
                return true;
            }

            transitions = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Transition<TState, TTrigger> GetTransitionFor(TState state, TTrigger trigger)
        {
            if (TryGetTransitionFor(state, trigger, out var transition))
                return transition;

            throw new InvalidOperationException($"Transition from {state} by {trigger} not found");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GuardedTransition<TState, TTrigger> GetGuardedTransitionFor(TState state, TTrigger trigger)
        {
            if (TryGetGuardedTransitionsFor(state, trigger, out var transitions))
            {
                foreach (var trans in transitions)
                {
                    if (trans.Guard())
                        return trans;
                }
            }

            throw new InvalidOperationException($"Guarded transition from {state} by {trigger} not found");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<TTrigger> GetAvailableTriggers(TState state)
        {
            var triggers = new HashSet<TTrigger>();

            if (_simpleTransitions.TryGetValue(state, out var simple))
                foreach (var trigger in simple.Keys)
                    triggers.Add(trigger);

            if (_guardedTransitions.TryGetValue(state, out var guarded))
                foreach (var trigger in guarded.Keys)
                    triggers.Add(trigger);

            return triggers;
        }
    }
}

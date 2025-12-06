using System;
using System.Collections.Generic;

namespace CLD.HFSM
{
    public readonly struct StateConfiguration<TState, TTrigger>
    {
        public readonly TState State;
        public readonly StateHandlers SyncHandlers;
        public readonly StateHandlersAsync AsyncHandlers;

        // 🔹 Публичные массивы для прямого доступа
        public readonly (TTrigger trigger, TState target)[] Transitions;
        public readonly (TTrigger trigger, Func<bool> guard, TState target)[] GuardedTransitions;

        // 🔹 Dictionary создаётся только для больших конфигураций (>8 переходов)
        private readonly Dictionary<TTrigger, TState>? _transitionsLookup;
        private readonly Dictionary<TTrigger, List<(Func<bool> guard, TState target)>>? _guardedTransitionsLookup;

        private const int DICTIONARY_THRESHOLD = 8;

        public StateConfiguration(
            TState state,
            StateHandlers syncHandlers,
            StateHandlersAsync asyncHandlers,
            (TTrigger trigger, TState target)[] transitions,
            (TTrigger trigger, Func<bool> guard, TState target)[] guardedTransitions)
        {
            State = state;
            SyncHandlers = syncHandlers;
            AsyncHandlers = asyncHandlers;
            Transitions = transitions;
            GuardedTransitions = guardedTransitions;

            // Создаём Dictionary только если переходов много
            if (transitions.Length > DICTIONARY_THRESHOLD)
            {
                _transitionsLookup = new Dictionary<TTrigger, TState>(transitions.Length);
                foreach (var (trigger, target) in transitions)
                    _transitionsLookup[trigger] = target;
            }
            else
            {
                _transitionsLookup = null;
            }

            if (guardedTransitions.Length > DICTIONARY_THRESHOLD)
            {
                _guardedTransitionsLookup = new Dictionary<TTrigger, List<(Func<bool>, TState)>>(
                    guardedTransitions.Length);

                foreach (var (trigger, guard, target) in guardedTransitions)
                {
                    if (!_guardedTransitionsLookup.TryGetValue(trigger, out var guardList))
                    {
                        guardList = new List<(Func<bool>, TState)>(2);
                        _guardedTransitionsLookup[trigger] = guardList;
                    }
                    guardList.Add((guard, target));
                }
            }
            else
            {
                _guardedTransitionsLookup = null;
            }
        }

        /// <summary>
        /// Гибридный поиск: линейный для малых массивов (&lt;=8), Dictionary для больших (&gt;8)
        /// </summary>
        public bool TryGetTransition(TTrigger trigger, out TState target)
        {
            // Если есть Dictionary - используем его (большая конфигурация)
            if (_transitionsLookup != null)
            {
                return _transitionsLookup.TryGetValue(trigger, out target!);
            }

            // Линейный поиск для малых конфигураций
            var transitions = Transitions;
            for (int i = 0; i < transitions.Length; i++)
            {
                if (EqualityComparer<TTrigger>.Default.Equals(transitions[i].trigger, trigger))
                {
                    target = transitions[i].target;
                    return true;
                }
            }

            target = default!;
            return false;
        }

        /// <summary>
        /// Гибридный поиск для guarded переходов с поддержкой множественных guards на триггер
        /// </summary>
        public bool TryGetGuardedTransition(TTrigger trigger, out TState target)
        {
            // Если есть Dictionary - используем его
            if (_guardedTransitionsLookup != null)
            {
                if (_guardedTransitionsLookup.TryGetValue(trigger, out var guardList))
                {
                    // Проверяем guards по порядку
                    for (int i = 0; i < guardList.Count; i++)
                    {
                        var (guard, state) = guardList[i];
                        if (guard())
                        {
                            target = state;
                            return true;
                        }
                    }
                }

                target = default!;
                return false;
            }

            // Линейный поиск для малых конфигураций
            var guardedTransitions = GuardedTransitions;
            for (int i = 0; i < guardedTransitions.Length; i++)
            {
                ref readonly var trans = ref guardedTransitions[i];
                if (EqualityComparer<TTrigger>.Default.Equals(trans.trigger, trigger) &&
                    trans.guard())
                {
                    target = trans.target;
                    return true;
                }
            }

            target = default!;
            return false;
        }

        /// <summary>
        /// Проверяет наличие хотя бы одного перехода для триггера
        /// </summary>
        public bool HasTransitionFor(TTrigger trigger)
        {
            if (_transitionsLookup != null)
                return _transitionsLookup.ContainsKey(trigger);

            var transitions = Transitions;
            for (int i = 0; i < transitions.Length; i++)
            {
                if (EqualityComparer<TTrigger>.Default.Equals(transitions[i].trigger, trigger))
                    return true;
            }

            if (_guardedTransitionsLookup != null)
                return _guardedTransitionsLookup.ContainsKey(trigger);

            var guardedTransitions = GuardedTransitions;
            for (int i = 0; i < guardedTransitions.Length; i++)
            {
                if (EqualityComparer<TTrigger>.Default.Equals(guardedTransitions[i].trigger, trigger))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Возвращает все доступные триггеры из этого состояния
        /// </summary>
        public IEnumerable<TTrigger> GetAvailableTriggers()
        {
            var triggers = new HashSet<TTrigger>();

            if (_transitionsLookup != null)
            {
                foreach (var trigger in _transitionsLookup.Keys)
                    triggers.Add(trigger);
            }
            else
            {
                foreach (var (trigger, _) in Transitions)
                    triggers.Add(trigger);
            }

            if (_guardedTransitionsLookup != null)
            {
                foreach (var trigger in _guardedTransitionsLookup.Keys)
                    triggers.Add(trigger);
            }
            else
            {
                foreach (var (trigger, _, _) in GuardedTransitions)
                    triggers.Add(trigger);
            }

            return triggers;
        }

        public int TransitionCount => Transitions.Length;
        public int GuardedTransitionCount => GuardedTransitions.Length;

        public override string ToString()
        {
            return $"State: {State}, Transitions: {TransitionCount}, Guarded: {GuardedTransitionCount}";
        }
    }
}

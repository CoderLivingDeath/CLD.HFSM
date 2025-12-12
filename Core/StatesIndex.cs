using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CLD.HFSM
{
    public class StatesIndex<TState, TTrigger>
    {
        private readonly Dictionary<TState, StateConfiguration<TState, TTrigger>> _stateConfigurations;
        private readonly Dictionary<TState, TState> _superState;
        private readonly Dictionary<(TState state, TTrigger trigger), (TState state, Func<bool> guard)[]> _transitions;

        private readonly sbyte[] _superStateIndex;
        private readonly TState[] _stateByIndex;
        private readonly Dictionary<TState, byte> _stateToIndex;

        #region Constructor
        public StatesIndex(StateMachineConfiguration<TState, TTrigger> config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            (_stateConfigurations, _superState, var tmpTransitions) = BuildStatesAndSuperStates(config);

            _transitions = BuildTransitions(tmpTransitions);

            (_stateToIndex, _stateByIndex) = BuildStateIndexes(_stateConfigurations);

            _superStateIndex = BuildSuperStateIndexes(_superState, _stateToIndex, _stateByIndex);
        }

        private static (Dictionary<TState, StateConfiguration<TState, TTrigger>> stateConfigs,
                        Dictionary<TState, TState> superStates,
                        Dictionary<(TState, TTrigger), List<(TState, Func<bool>)>> transitions) BuildStatesAndSuperStates(StateMachineConfiguration<TState, TTrigger> config)
        {
            var stateConfigurations = new Dictionary<TState, StateConfiguration<TState, TTrigger>>();
            var superState = new Dictionary<TState, TState>();
            var tmpTransitions = new Dictionary<(TState, TTrigger), List<(TState, Func<bool>)>>();

            foreach (var sc in config.StateConfigurations)
            {
                if (stateConfigurations.ContainsKey(sc.State))
                    throw new InvalidOperationException($"Duplicate state: {sc.State}");

                stateConfigurations[sc.State] = sc;

                if (sc.IsSubstate)
                {
                    if (superState.ContainsKey(sc.State))
                        throw new InvalidOperationException($"State '{sc.State}' already has superstate.");
                    superState[sc.State] = sc.SuperState;
                }

                foreach (var (trigger, guard, target) in sc.GuardedTransitions)
                {
                    var key = (sc.State, trigger);
                    if (!tmpTransitions.TryGetValue(key, out var list))
                    {
                        list = new List<(TState, Func<bool>)>();
                        tmpTransitions[key] = list;
                    }
                    list.Add((target, guard));
                }
            }

            return (stateConfigurations, superState, tmpTransitions);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Dictionary<(TState, TTrigger), (TState, Func<bool>)[]> BuildTransitions(Dictionary<(TState, TTrigger), List<(TState, Func<bool>)>> tmpTransitions)
        {
            return tmpTransitions.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (Dictionary<TState, byte> stateToIndex, TState[] stateByIndex) BuildStateIndexes(Dictionary<TState, StateConfiguration<TState, TTrigger>> stateConfigurations)
        {
            var stateToIndex = new Dictionary<TState, byte>();
            var stateList = new List<TState>(stateConfigurations.Count);

            foreach (var kvp in stateConfigurations)
            {
                byte index = (byte)stateList.Count;
                stateToIndex[kvp.Key] = index;
                stateList.Add(kvp.Key);
            }

            return (stateToIndex, stateList.ToArray());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static sbyte[] BuildSuperStateIndexes(
            Dictionary<TState, TState> superState,
            Dictionary<TState, byte> stateToIndex,
            TState[] stateByIndex)
        {
            var superStateIndex = new sbyte[stateByIndex.Length];
            Array.Fill(superStateIndex, (sbyte)-1);

            foreach (var kvp in superState)
            {
                var childIdx = stateToIndex[kvp.Key];
                var parentIdx = stateToIndex[kvp.Value];
                superStateIndex[childIdx] = (sbyte)parentIdx;
            }

            return superStateIndex;
        }
        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private StateHandlers BuildSelfTransitionHandlers(TState state)
        {
            if (!_stateConfigurations.TryGetValue(state, out var cfg))
                throw new InvalidOperationException();

            return new StateHandlers(
                cfg.SyncHandlers.Enter,
                cfg.AsyncHandlers.Enter,
                cfg.SyncHandlers.Exit,
                cfg.AsyncHandlers.Exit
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanFire(TState state, TTrigger trigger)
        {
            var key = (state, trigger);
            return _transitions.ContainsKey(key);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasState(TState state) => _stateConfigurations.ContainsKey(state);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TState TryGetSuperState(TState state)
        {
            return _superState.TryGetValue(state, out var parent) ? parent : default!;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetTransition(TState state, TTrigger trigger, out Transition<TState, TTrigger> transition)
        {
            transition = default;

            if (_transitions.TryGetValue((state, trigger), out var transitions))
            {
                foreach (var item in transitions)
                {
                    if (!item.guard()) continue;

                    var targetState = item.state;

                    if (EqualityComparer<TState>.Default.Equals(state, targetState))
                    {
                        transition = new Transition<TState, TTrigger>(state, targetState, trigger,
                            BuildSelfTransitionHandlers(state));
                        return true;
                    }

                    // Hierarchical transition с индексами!
                    var handlers = GetHandlerFromHierarchy(state, targetState);
                    transition = new Transition<TState, TTrigger>(state, targetState, trigger, handlers);
                    return true;
                }
            }
            return false;
        }

        // Возвращает индексы стейтов в порядке от leaf до root
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<int> GetBranchIndexes(in TState state, ref Span<int> buffer)
        {
            int count = 0;
            sbyte parentIndex;
            int stateIndex = _stateToIndex[state];

            do
            {
                buffer[count++] = stateIndex;
                parentIndex = _superStateIndex[stateIndex];
                stateIndex = parentIndex; 
            }
            while (parentIndex != -1);
            buffer[..count].Reverse();
            return buffer[..count];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StateHandlers GetHandlerFromHierarchy(in TState fromState, in TState toState)
        {
            Span<int> leftBuffer = stackalloc int[32];
            Span<int> rightBuffer = stackalloc int[32];

            ReadOnlySpan<int> leftBranch = GetBranchIndexes(fromState, ref leftBuffer);
            ReadOnlySpan<int> rightBranch = GetBranchIndexes(toState, ref rightBuffer);

            int leftCount = leftBranch.Length;
            int rightCount = rightBranch.Length;

            int minLen = Math.Min(leftCount, rightCount);
            int divergeIndex = 0;

            while (divergeIndex < minLen &&
                   leftBranch[divergeIndex] == rightBranch[divergeIndex])
            {
                divergeIndex++;
            }

            StateExitAction? commonExit = null;
            StateExitActionAsync? commonExitAsync = null;
            StateEnterAction? commonEnter = null;
            StateEnterActionAsync? commonEnterAsync = null;

            for (int i = leftCount - 1; i >= divergeIndex; i--)
            {
                int stateIndex = leftBranch[i];
                TState state = _stateByIndex[stateIndex];
                if (_stateConfigurations.TryGetValue(state, out var cfg))
                {
                    commonExit += cfg.SyncHandlers.Exit;
                    commonExitAsync += cfg.AsyncHandlers.Exit;
                }
            }

            for (int i = divergeIndex; i < rightCount; i++)
            {
                int stateIndex = rightBranch[i];
                TState state = _stateByIndex[stateIndex];
                if (_stateConfigurations.TryGetValue(state, out var cfg))
                {
                    commonEnter += cfg.SyncHandlers.Enter;
                    commonEnterAsync += cfg.AsyncHandlers.Enter;
                }
            }

            return new StateHandlers(commonEnter, commonEnterAsync, commonExit, commonExitAsync);
        }
    }

    public readonly struct TransitionHandlers
    {
        public readonly StateExitAction? Exit;
        public readonly StateExitActionAsync? ExitAsync;
        public readonly StateEnterAction? Enter;
        public readonly StateEnterActionAsync? EnterAsync;

        public TransitionHandlers(
            StateExitAction? exit,
            StateExitActionAsync? exitAsync,
            StateEnterAction? enter,
            StateEnterActionAsync? enterAsync)
        {
            Exit = exit;
            ExitAsync = exitAsync;
            Enter = enter;
            EnterAsync = enterAsync;
        }
    }
}

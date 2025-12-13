using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CLD.HFSM
{
    public class StatesIndex<TState, TTrigger>
    {
        private const int BUFFERS_SIZE = 32;

        #region Fields

        private readonly Dictionary<TState, StateConfiguration<TState, TTrigger>> _stateConfigurations;
        private readonly Dictionary<TState, TState> _superState;
        private readonly Dictionary<(TState state, TTrigger trigger), (TState state, Func<bool> guard)[]> _transitions;
        private readonly sbyte[] _superStateIndex;
        private readonly TState[] _stateByIndex;
        private readonly Dictionary<TState, byte> _stateToIndex;
        private readonly bool _precomputed;
        private readonly Dictionary<TState, (TState[] hierarchy, TransitionInfo[] transitions)> _precomputedCache;
        private readonly AnyStateConfiguration<TState, TTrigger>? _anyStateConfiguration;

        #endregion

        #region Properties

        public bool HasAnyState => _anyStateConfiguration != null;
        public bool IsPrecomputed => _precomputed;

        #endregion

        #region Constructor

        public StatesIndex(StateMachineConfiguration<TState, TTrigger> config, bool precompute = false)
        {
            _precomputed = precompute;

            if (config == null)
                throw new ArgumentNullException(nameof(config));

            var statesAndSuperStates = BuildStatesAndSuperStates(config);
            _stateConfigurations = statesAndSuperStates.stateConfigs;
            _superState = statesAndSuperStates.superStates;
            var tmpTransitions = statesAndSuperStates.transitions;

            _transitions = BuildTransitions(tmpTransitions);
            (_stateToIndex, _stateByIndex) = BuildStateIndexes(_stateConfigurations);
            _superStateIndex = BuildSuperStateIndexes(_superState, _stateToIndex, _stateByIndex);

            if (_precomputed)
                _precomputedCache = PrecomputeHierarchyCache();

            _anyStateConfiguration = config.AnyStateConfiguration;
        }

        #endregion

        #region State Building

        private static (Dictionary<TState, StateConfiguration<TState, TTrigger>> stateConfigs, Dictionary<TState, TState> superStates, Dictionary<(TState, TTrigger), List<(TState, Func<bool>)>> transitions) BuildStatesAndSuperStates(StateMachineConfiguration<TState, TTrigger> config)
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
        private static sbyte[] BuildSuperStateIndexes(Dictionary<TState, TState> superState, Dictionary<TState, byte> stateToIndex,TState[] stateByIndex)
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

        #region Precomputation

        private Dictionary<TState, (TState[] hierarchy, TransitionInfo[] transitions)> PrecomputeHierarchyCache()
        {
            var cache = new Dictionary<TState, (TState[] hierarchy, TransitionInfo[] transitions)>();
            Span<int> buffer = stackalloc int[32];

            foreach (var kvp in _stateConfigurations)
            {
                var state = kvp.Key;
                int count = GetBranchIndexes(state, ref buffer);
                var hierarchy = new TState[count];

                for (int i = 0; i < count; i++)
                    hierarchy[i] = _stateByIndex[buffer[i]];

                var transitions = PrecomputeTransitionsForHierarchy(state, hierarchy);
                cache[state] = (hierarchy, transitions);
            }

            return cache;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private TransitionInfo[] PrecomputeTransitionsForHierarchy(TState state, TState[] hierarchy)
        {
            var allTransitions = new List<TransitionInfo>();

            foreach (var currentState in hierarchy)
            {
                foreach (var kvp in _transitions)
                {
                    var (stateKey, _) = kvp.Key;
                    if (!EqualityComparer<TState>.Default.Equals(stateKey, currentState))
                        continue;

                    var (triggerState, trigger) = kvp.Key;
                    var targets = kvp.Value;

                    foreach (var item in targets)
                    {
                        TState targetState = item.state;
                        Func<bool> guard = item.guard;

                        StateHandlers handlers;
                        if (EqualityComparer<TState>.Default.Equals(currentState, targetState))
                            handlers = BuildSelfTransitionHandlers(currentState);
                        else
                            handlers = GetHandlerFromHierarchy(currentState, targetState);

                        allTransitions.Add(new TransitionInfo(
                            currentState, targetState, trigger, guard, handlers));
                    }
                }
            }

            return allTransitions.ToArray();
        }

        #endregion

        #region Public API

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanFire(TState state, TTrigger trigger)
        {
            var key = (state, trigger);
            return _transitions.ContainsKey(key);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasState(TState state) => _stateConfigurations.ContainsKey(state);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TState TryGetSuperState(TState state) =>
            _superState.TryGetValue(state, out var parent) ? parent : default!;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetTransition(TState state, TTrigger trigger, out Transition<TState, TTrigger> transition)
        {
            transition = default;

            // 1. Precomputed cache (priority)
            if (_precomputed && _precomputedCache.TryGetValue(state, out var cache))
            {
                if (TryGetTransitionFromCache(cache.transitions, trigger, out transition))
                    return true;
            }

            // 2. Runtime hierarchy search
            if (TryGetTransitionRuntime(state, trigger, out transition))
                return true;

            // 3. FINAL FALLBACK: AnyState
            return _anyStateConfiguration != null && TryGetAnyStateTransition(state, trigger, out transition);
        }

        #endregion

        #region Transition Search

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryGetTransitionFromCache(TransitionInfo[] transitions, TTrigger trigger, out Transition<TState, TTrigger> transition)
        {
            transition = default;

            foreach (var info in transitions)
            {
                if (!EqualityComparer<TTrigger>.Default.Equals(info.trigger, trigger))
                    continue;
                if (!info.guard())
                    continue;

                transition = info; // implicit conversion
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryGetTransitionRuntime(TState state, TTrigger trigger, out Transition<TState, TTrigger> transition)
        {
            transition = default;

            Span<int> stateBuffer = stackalloc int[32];
            int hierarchyCount = GetBranchIndexes(state, ref stateBuffer);

            // Search up the hierarchy
            for (int i = 0; i < hierarchyCount; i++)
            {
                int stateIndex = stateBuffer[i];
                TState currentState = _stateByIndex[stateIndex];

                if (_transitions.TryGetValue((currentState, trigger), out var transTargets))
                {
                    foreach (var item in transTargets)
                    {
                        if (!item.guard()) continue;

                        TState targetState = item.state;

                        StateHandlers handlers;
                        if (EqualityComparer<TState>.Default.Equals(currentState, targetState))
                            handlers = BuildSelfTransitionHandlers(currentState);
                        else
                            handlers = GetHandlerFromHierarchy(currentState, targetState);

                        transition = new Transition<TState, TTrigger>(currentState, targetState, trigger, handlers);
                        return true;
                    }
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryGetAnyStateTransition(TState fromState, TTrigger trigger, out Transition<TState, TTrigger> transition)
        {
            transition = default;

            if (_anyStateConfiguration?.GuardedTransitions is null)
                return false;

            foreach (var anyTrans in _anyStateConfiguration.GuardedTransitions)
            {
                if (!EqualityComparer<TTrigger>.Default.Equals(anyTrans.trigger, trigger))
                    continue;
                if (!anyTrans.guard())
                    continue;

                var handlers = GetHandlerFromHierarchy(fromState, anyTrans.target);
                transition = new Transition<TState, TTrigger>(fromState, anyTrans.target, trigger, handlers);
                return true;
            }

            return false;
        }

        #endregion

        #region Hierarchy Traversal

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetBranchIndexes(in TState state, ref Span<int> buffer)
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

            return count;
        }

        #endregion

        #region Handler Resolution

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private StateHandlers BuildSelfTransitionHandlers(TState state)
        {
            if (!_stateConfigurations.TryGetValue(state, out var cfg))
                throw new InvalidOperationException();

            return new StateHandlers(
                cfg.SyncHandlers.Enter,
                cfg.AsyncHandlers.Enter,
                cfg.SyncHandlers.Exit,
                cfg.AsyncHandlers.Exit);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StateHandlers GetHandlerFromHierarchy(in TState fromState, in TState toState)
        {
            Span<int> leftBuffer = stackalloc int[BUFFERS_SIZE];
            Span<int> rightBuffer = stackalloc int[32];

            int leftCount = GetBranchIndexes(fromState, ref leftBuffer);
            int rightCount = GetBranchIndexes(toState, ref rightBuffer);

            if (TryFindCommonAncestor(leftCount, leftBuffer, rightCount, rightBuffer, out var lca))
                return GetHandlersWithCommonAncestor(leftBuffer, leftCount, rightBuffer, rightCount, lca);
            else
                return GetHandlersNoCommonAncestor(leftBuffer, leftCount, rightBuffer, rightCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryFindCommonAncestor(int leftCount, Span<int> leftBuffer, int rightCount, Span<int> rightBuffer, out (int leftIndex, int rightIndex, int commonStateIndex) result)
        {
            int minLen = Math.Min(leftCount, rightCount);
            int commonTailLength = 0;

            while (commonTailLength < minLen &&
                   leftBuffer[leftCount - 1 - commonTailLength] ==
                   rightBuffer[rightCount - 1 - commonTailLength])
            {
                commonTailLength++;
            }

            if (commonTailLength > 0)
            {
                int leftLcaIndex = leftCount - commonTailLength;
                int rightLcaIndex = rightCount - commonTailLength;
                result = (leftLcaIndex, rightLcaIndex, leftBuffer[leftLcaIndex]);
                return true;
            }

            result = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private StateHandlers GetHandlersWithCommonAncestor(Span<int> leftBuffer, int leftCount, Span<int> rightBuffer, int rightCount, (int leftIndex, int rightIndex, int commonStateIndex) lca)
        {
            var (leftLcaIndex, rightLcaIndex, _) = lca;

            StateExitAction? commonExit = null;
            StateExitActionAsync? commonExitAsync = null;
            StateEnterAction? commonEnter = null;
            StateEnterActionAsync? commonEnterAsync = null;

            for (int i = 0; i <= leftLcaIndex - 1; i++)
            {
                int stateIndex = leftBuffer[i];
                TState state = _stateByIndex[stateIndex];
                if (_stateConfigurations.TryGetValue(state, out var cfg))
                {
                    commonExit += cfg.SyncHandlers.Exit;
                    commonExitAsync += cfg.AsyncHandlers.Exit;
                }
            }

            for (int i = 0; i <= rightLcaIndex - 1; i++)
            {
                int stateIndex = rightBuffer[i];
                TState state = _stateByIndex[stateIndex];
                if (_stateConfigurations.TryGetValue(state, out var cfg))
                {
                    commonEnter += cfg.SyncHandlers.Enter;
                    commonEnterAsync += cfg.AsyncHandlers.Enter;
                }
            }

            return new StateHandlers(commonEnter, commonEnterAsync, commonExit, commonExitAsync);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private StateHandlers GetHandlersNoCommonAncestor(Span<int> leftBuffer, int leftCount, Span<int> rightBuffer, int rightCount)
        {
            StateExitAction? commonExit = null;
            StateExitActionAsync? commonExitAsync = null;
            StateEnterAction? commonEnter = null;
            StateEnterActionAsync? commonEnterAsync = null;

            for (int i = 0; i < leftCount; i++)
            {
                int stateIndex = leftBuffer[i];
                TState state = _stateByIndex[stateIndex];
                if (_stateConfigurations.TryGetValue(state, out var cfg))
                {
                    commonExit += cfg.SyncHandlers.Exit;
                    commonExitAsync += cfg.AsyncHandlers.Exit;
                }
            }

            for (int i = 0; i < rightCount; i++)
            {
                int stateIndex = rightBuffer[i];
                TState state = _stateByIndex[stateIndex];
                if (_stateConfigurations.TryGetValue(state, out var cfg))
                {
                    commonEnter += cfg.SyncHandlers.Enter;
                    commonEnterAsync += cfg.AsyncHandlers.Enter;
                }
            }

            return new StateHandlers(commonEnter, commonEnterAsync, commonExit, commonExitAsync);
        }

        #endregion

        #region Types

        public readonly struct TransitionInfo
        {
            public readonly TState fromState;
            public readonly TState toState;
            public readonly TTrigger trigger;
            public readonly Func<bool> guard;
            public readonly StateHandlers handlers;

            public TransitionInfo(TState fromState, TState toState, TTrigger trigger, Func<bool> guard, StateHandlers handlers)
            {
                this.fromState = fromState;
                this.toState = toState;
                this.trigger = trigger;
                this.guard = guard;
                this.handlers = handlers;
            }

            public static implicit operator Transition<TState, TTrigger>(TransitionInfo info) =>
                new Transition<TState, TTrigger>(info.fromState, info.toState, info.trigger, info.handlers);
        }

        #endregion
    }
}

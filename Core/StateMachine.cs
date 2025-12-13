using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CLD.HFSM
{
    /// <summary>
    /// Delegate invoked on state transitions with source and target states.
    /// </summary>
    public delegate void TransitionAction<TState>(TState source, TState target);

    /// <summary>
    /// Synchronous state entry action.
    /// </summary>
    public delegate void StateEnterAction();

    /// <summary>
    /// Synchronous state exit action.
    /// </summary>
    public delegate void StateExitAction();

    /// <summary>
    /// Asynchronous state entry action.
    /// </summary>
    public delegate ValueTask StateEnterActionAsync();

    /// <summary>
    /// Asynchronous state exit action.
    /// </summary>
    public delegate ValueTask StateExitActionAsync();

    /// <summary>
    /// Configuration callback for state machine builder.
    /// </summary>
    public delegate void ConfigurationAction<TState, TTrigger>(StateMachineConfigurationBuilder<TState, TTrigger> builder);

    /// <summary>
    /// Hierarchical Finite State Machine with zero-allocation transition lookup and precomputed caching.
    /// Supports AnyState transitions as final fallback after hierarchy search.
    /// </summary>
    public sealed class StateMachine<TState, TTrigger>
    {
        private readonly StateMachineConfiguration<TState, TTrigger> _configuration;
        private readonly StatesIndex<TState, TTrigger> _index;
        private TState _currentState;

        /// <summary>
        /// Gets the current active state.
        /// </summary>
        public TState CurrentState => _currentState;

        /// <summary>
        /// Initializes the state machine with initial state and shared configuration.
        /// </summary>
        /// <param name="initialState">Starting state</param>
        /// <param name="sharedConfiguration">Pre-built configuration</param>
        /// <param name="precompute">Enable transition precomputation for O(1) lookup</param>
        public StateMachine(TState initialState, StateMachineConfiguration<TState, TTrigger> sharedConfiguration, bool precompute = false)
        {
            _configuration = sharedConfiguration ?? throw new ArgumentNullException(nameof(sharedConfiguration));
            _index = new StatesIndex<TState, TTrigger>(sharedConfiguration, precompute);

            if (!_index.HasState(initialState))
                throw new InvalidOperationException($"Initial state '{initialState}' is not configured");

            _currentState = initialState;
        }

        /// <summary>
        /// Executes state transition with hierarchical enter/exit handlers.
        /// Order: Exit handlers → State change → Global callback → Enter handlers.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ExecuteTransition(Transition<TState, TTrigger> transition)
        {
            var source = transition.SourceState;
            var target = transition.TargetState;
            var handlers = transition.Handlers;

            handlers.OnExit();
            handlers.OnExitAsync();

            _currentState = target;

            _configuration.OnTransition?.Invoke(source, target);

            handlers.OnEnter();
            handlers.OnEnterAsync();

            return true;
        }

        /// <summary>
        /// Checks if transition for trigger is available from current state.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanFire(TTrigger trigger) => _index.CanFire(_currentState, trigger);

        /// <summary>
        /// Fires trigger transition or throws if unavailable.
        /// </summary>
        /// <param name="trigger">Transition trigger</param>
        /// <param name="throwException">Throw on missing transition</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Fire(TTrigger trigger, bool throwException = true)
        {
            if (TryFire(trigger))
                return;

            throw new InvalidOperationException($"No transition for trigger '{trigger}' from state '{CurrentState}'");
        }

        /// <summary>
        /// Attempts to fire trigger transition. Returns success status.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryFire(TTrigger trigger) =>
            _index.TryGetTransition(_currentState, trigger, out var transition) &&
            ExecuteTransition(transition);

        /// <summary>
        /// Forces immediate state change without transition validation or handlers.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ForceTransition(TState targetState, bool validate = false)
        {
            if (validate && !_index.HasState(targetState))
                throw new InvalidOperationException($"State '{targetState}' is not configured");

            _currentState = targetState;
        }

        /// <summary>
        /// Forces state change with hierarchical enter/exit handlers execution.
        /// Maintains full transition semantics without trigger validation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ForceTransitionWithHandlers(TState targetState, bool validate = false)
        {
            if (validate && !_index.HasState(targetState))
                throw new InvalidOperationException($"State '{targetState}' is not configured");

            var previousState = _currentState;
            var handlers = _index.GetHandlerFromHierarchy(previousState, targetState);

            handlers.OnExit();
            handlers.OnExitAsync();

            _currentState = targetState;

            handlers.OnEnter();
            handlers.OnEnterAsync();
        }
    }
}

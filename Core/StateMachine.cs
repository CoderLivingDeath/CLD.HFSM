using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CLD.HFSM
{
    public delegate void TransitionAction<TState>(TState source, TState target);

    public delegate void StateEnterAction();
    public delegate void StateExitAction();

    public delegate ValueTask StateEnterActionAsync();
    public delegate ValueTask StateExitActionAsync();

    public delegate void ConfiguratioinAction<TState, TTrigger>(StateMachineConfigurationBuilder<TState, TTrigger> build);

    // сделать обработку anystate
    // сделать наследование переходов детьми от родителей
    public sealed class StateMachine<TState, TTrigger>
    {
        private readonly StateMachineConfiguration<TState, TTrigger> _configuration;
        private readonly StatesIndex<TState, TTrigger> _index;
        private TState _currentStateField;

        public TState CurrentState => _currentStateField;

        public StateMachine(TState initialState,StateMachineConfiguration<TState, TTrigger> sharedConfiguration, bool precomputedTransition = false)
        {
            _configuration = sharedConfiguration ?? throw new ArgumentNullException(nameof(sharedConfiguration));
            _index = new StatesIndex<TState, TTrigger>(sharedConfiguration, precomputedTransition);
        
            // проверяем, что начальное состояние сконфигурировано
            if (!_index.HasState(initialState))
                throw new InvalidOperationException(
                    $"Initial state '{initialState}' is not configured in this state machine");

            _currentStateField = initialState;
        }

        public bool CanFire(TTrigger trigger)
        {
            return _index.CanFire(_currentStateField, trigger);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Fire(in TTrigger trigger, bool throwException = true)
        {
            if (TryFire(trigger))
                return;

            if (throwException)
                throw new InvalidOperationException(
                    $"No transition found for trigger {trigger} from state {CurrentState}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryFire(in TTrigger trigger)
        {
            // Вся логика guarded‑переходов уже внутри StatesIndex.TryGetTransition
            if (_index.TryGetTransition(_currentStateField, trigger, out var transition))
            {
                ExecuteTransition(transition);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ExecuteTransition(in Transition<TState, TTrigger> transition)
        {
            var source = transition.SourceState;
            var target = transition.TargetState;
            var handlers = transition.Handlers; // StateHandlers

            // 1. Exit по иерархии
            handlers.OnExit();
            handlers.OnExitAsync();

            // 2. собственно смена состояния
            _currentStateField = target;

            // 3. глобальный колбэк машины, если есть
            _configuration.OnTransition?.Invoke(source, target);

            // 4. Enter по иерархии
            handlers.OnEnter();
            handlers.OnEnterAsync();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ForceTransition(TState targetState, bool validate = false)
        {
            if (validate && !_index.HasState(targetState))
                throw new InvalidOperationException(
                    $"State '{targetState}' is not configured in this state machine");

            _currentStateField = targetState;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ForceTransitionWithHandlers(TState targetState, bool validate = false)
        {
            if (validate && !_index.HasState(targetState))
                throw new InvalidOperationException($"State '{targetState}' is not configured");

            var previousState = _currentStateField;

            // ✅ Индексация: 0 аллокаций List!
            var handlers = _index.GetHandlerFromHierarchy(previousState, targetState);

            // Exit → Enter атомарно
            handlers.OnExit();
            handlers.OnExitAsync();

            _currentStateField = targetState;

            handlers.OnEnter();
            handlers.OnEnterAsync();
        }

    }
}

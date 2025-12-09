using CLD.HFSM;
using System;
using System.Collections.Generic;

public interface IStateMachineIndex<TState, TTrigger>
{
    IReadOnlyDictionary<TState, StateConfiguration<TState, TTrigger>> States { get; }

    bool HasTransition(TState state);
    bool IsPermitedTrigger(TState state, TTrigger trigger);
    bool IsPermitedGuardTrigger(TState state, TTrigger trigger);

    bool TryGetTransitionFor(TState state, TTrigger trigger, out Transition<TState, TTrigger> transition);
    bool TryGetGuardedTransitionsFor(TState state, TTrigger trigger,
        out ReadOnlySpan<GuardedTransition<TState, TTrigger>> transitions);

    Transition<TState, TTrigger> GetTransitionFor(TState state, TTrigger trigger);
    GuardedTransition<TState, TTrigger> GetGuardedTransitionFor(TState state, TTrigger trigger);

    IEnumerable<TTrigger> GetAvailableTriggers(TState state);
}


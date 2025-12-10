using CLD.HFSM;
using Test;

namespace Test
{
    public enum State
    {
        None,
        S1, S2, S3, S4, S5,   // глубинная цепочка
        Active, Inactive, Idle, Run, Jump
    }

    public enum Trigger
    {
        DoIdle, DoRun, DoJump, Stop, Start
    }

    internal class Program
    {
        public static StateMachine<State, Trigger> stateMachine;
        static void Main(string[] args)
        {
            StateMachineConfigurationBuilder<State, Trigger> builder = new();

            builder.OnTransition(LogHelper.LogTransition);

            // корневые
            builder.ConfigureState(State.Inactive)
                .Permit(Trigger.Start, State.Active);

            builder.ConfigureState(State.Active)
                .Permit(Trigger.Stop, State.Inactive)

            builder.ConfigureState(State.S1)
                .SubstateOf(State.Inactive)
                .Permit(Trigger.DoRun, State.S2);

            builder.ConfigureState(State.S2)
                .SubstateOf(State.S1)
                .Permit(Trigger.DoRun, State.S3);

            builder.ConfigureState(State.S3)
                .SubstateOf(State.S2)
                .Permit(Trigger.DoRun, State.S4);

            builder.ConfigureState(State.S4)
                .SubstateOf(State.S3)
                .Permit(Trigger.DoRun, State.S5);

            builder.ConfigureState(State.S5)
                .SubstateOf(State.S4)
                .Permit(Trigger.DoIdle, State.Idle);

            // конечное Idle под Inactive
            builder.ConfigureState(State.Idle)
                .SubstateOf(State.Inactive);

            var SharedConfiguration = builder.GetConfiguration();

            // создание state machine
            stateMachine = new StateMachine<State, Trigger>(State.S1, SharedConfiguration);

            // тестовый сценарий
            stateMachine.TryFire(Trigger.DoRun);   // S1 -> S2
            stateMachine.TryFire(Trigger.DoRun);   // S2 -> S3
            stateMachine.TryFire(Trigger.DoRun);   // S3 -> S4
            stateMachine.TryFire(Trigger.DoRun);   // S4 -> S5
            stateMachine.TryFire(Trigger.DoIdle);  // S5 -> Idle (через Inactive как общий родитель)
        }

        public static void Log(string message)
        {
            Console.WriteLine(message);
        }
    }
}

public static class LogHelper
{
    public static int GetDepth(State state)
    {
        return state switch
        {
            State.S1 => 1,
            State.S2 => 2,
            State.S3 => 3,
            State.S4 => 4,
            State.S5 => 5,

            State.None => 1,      // под Inactive
            State.Idle => 1,      // под Inactive
            State.Run => 1,       // под Active
            State.Jump => 1,      // под Active
            State.Inactive => 0,
            State.Active => 0,
            _ => 0
        };
    }

    public static void LogStateEnter(State state)
    {
        var depth = GetDepth(state);
        var indent = new string(' ', depth * 2);
        Console.WriteLine($"{indent}[ENTER] {state}");
    }

    public static void LogStateExit(State state)
    {
        var depth = GetDepth(state);
        var indent = new string(' ', depth * 2);
        Console.WriteLine($"{indent}[EXIT] {state}");
    }

    public static void LogTransition(State from, State to)
    {
        Console.WriteLine($"------ TRANSITION {from} -> {to} ------");
    }
}

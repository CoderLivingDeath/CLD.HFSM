using CLD.HFSM;

namespace Test
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var unit = new DeepUnit();

            Console.WriteLine($"Initial: {unit.Current}");

            await unit.RunScenario();

            Console.WriteLine("Done. Press any key...");
            Console.ReadKey();
        }
    }

    public enum DeepState
    {
        Root,
        A1, A2, A3,
        B1, B2, B3,
        Idle,
        MoveToA3,
        Attack,
        MoveToA2
    }

    public enum DeepTrigger
    {
        TEST,
        ToIdle,
        ToMoveA3,
        ToAttack,
        ToMoveA2
    }

    public class DeepUnit
    {
        private readonly StateMachine<DeepState, DeepTrigger> _fsm;
        private readonly CancellationTokenSource _cts = new();

        public DeepState Current => _fsm.CurrentState;

        public DeepUnit()
        {
            _fsm = new StateMachine<DeepState, DeepTrigger>(DeepState.Idle, BuildDeepConfig(), false);
        }

        private static StateMachineConfiguration<DeepState, DeepTrigger> BuildDeepConfig()
        {
            var b = new StateMachineConfigurationBuilder<DeepState, DeepTrigger>();

            b.ConfigureState(DeepState.Root)
                .OnEnter(() => Console.WriteLine("[Root] enter"))
                .OnExit(() => Console.WriteLine("[Root] exit"));

            b.ConfigureState(DeepState.A1)
                .SubstateOf(DeepState.Root)
                .OnEnter(() => Console.WriteLine("[A1] enter"))
                .OnExit(() => Console.WriteLine("[A1] exit"));

            b.ConfigureState(DeepState.A2)
                .SubstateOf(DeepState.A1)
                .OnEnter(() => Console.WriteLine("[A2] enter"))
                .OnExit(() => Console.WriteLine("[A2] exit"));

            b.ConfigureState(DeepState.A3)
                .SubstateOf(DeepState.A2)
                .OnEnter(() => Console.WriteLine("[A3] enter"))
                .OnExit(() => Console.WriteLine("[A3] exit"));

            b.ConfigureState(DeepState.B1)
                .SubstateOf(DeepState.Root)
                .OnEnter(() => Console.WriteLine("[B1] enter"))
                .OnExit(() => Console.WriteLine("[B1] exit"));

            b.ConfigureState(DeepState.B2)
                .SubstateOf(DeepState.B1)
                .OnEnter(() => Console.WriteLine("[B2] enter"))
                .OnExit(() => Console.WriteLine("[B2] exit"));

            b.ConfigureState(DeepState.B3)
                .SubstateOf(DeepState.B2)
                .OnEnter(() => Console.WriteLine("[B3] enter"))
                .OnExit(() => Console.WriteLine("[B3] exit"));

            b.ConfigureState(DeepState.Idle)
                .SubstateOf(DeepState.A3)
                .Permit(DeepTrigger.ToMoveA3, DeepState.MoveToA3)
                .Permit(DeepTrigger.ToAttack, DeepState.Attack)
                .OnEnter(() => Console.WriteLine("[Idle_A3] enter (Root -> A1 -> A2 -> A3 -> Idle)"))
                .OnExit(() => Console.WriteLine("[Idle_A3] exit"));

            b.ConfigureState(DeepState.MoveToA3)
                .SubstateOf(DeepState.A3)
                .Permit(DeepTrigger.ToIdle, DeepState.Idle)
                .OnEnter(() => Console.WriteLine("[MoveToA3] enter (Root -> A1 -> A2 -> A3 -> MoveToA3)"))
                .OnExit(() => Console.WriteLine("[MoveToA3] exit"));

            b.ConfigureState(DeepState.Attack)
                .SubstateOf(DeepState.B3)
                .Permit(DeepTrigger.ToIdle, DeepState.Idle)
                .OnEnter(() => Console.WriteLine("[Attack_B3] enter (Root -> B1 -> B2 -> B3 -> Attack)"))
                .OnExit(() => Console.WriteLine("[Attack_B3] exit"));

            b.ConfigureState(DeepState.MoveToA2)
                .SubstateOf(DeepState.A2)
                .Permit(DeepTrigger.ToIdle, DeepState.Idle)
                .OnEnter(() => Console.WriteLine("[MoveToA2] enter (Root -> A1 -> A2 -> MoveToA2)"))
                .OnExit(() => Console.WriteLine("[MoveToA2] exit"));

            b.ConfigureAnyState()
                .Permit(DeepTrigger.TEST, DeepState.B3);

            return b.GetConfiguration();
        }

        public async Task RunScenario()
        {
            Console.WriteLine(_fsm.CurrentState);
            _fsm.Fire(DeepTrigger.TEST);
            await Task.Delay(1000);
            Console.WriteLine(_fsm.CurrentState);


            //Console.WriteLine($"\n=== INITIAL ENTER CHAIN ===");
            //// При создании FSM должны сработать Root, A1, A2, A3, Idle

            //Console.WriteLine($"\n=== SCENARIO 1: Idle -> MoveToA3 -> Idle ===");
            //_fsm.Fire(DeepTrigger.ToMoveA3);
            //Console.WriteLine($"State after ToMoveA3: {Current}");
            //await Task.Delay(1000, _cts.Token);
            //_fsm.Fire(DeepTrigger.ToIdle);
            //Console.WriteLine($"State after ToIdle: {Current}");

            //Console.WriteLine($"\n=== SCENARIO 2: Idle -> Attack -> Idle ===");
            //_fsm.Fire(DeepTrigger.ToAttack);
            //Console.WriteLine($"State after ToAttack: {Current}");
            //await Task.Delay(1000, _cts.Token);
            //_fsm.Fire(DeepTrigger.ToIdle);
            //Console.WriteLine($"State after ToIdle: {Current}");

            //Console.WriteLine($"\n=== SCENARIO 3: Idle -> Attack -> MoveToA2 -> Idle ===");
            //_fsm.Fire(DeepTrigger.ToAttack);
            //Console.WriteLine($"State after ToAttack: {Current}");
            //await Task.Delay(1000, _cts.Token);

            //// форс‑переход в другую ветку с другой глубиной
            //_fsm.ForceTransition(DeepState.MoveToA2);
            //Console.WriteLine($"State after Force MoveToA2: {Current}");
            //await Task.Delay(1000, _cts.Token);

            //_fsm.Fire(DeepTrigger.ToIdle);
            //Console.WriteLine($"State after ToIdle: {Current}");


            //_fsm.Fire(DeepTrigger.TEST);
            //Console.WriteLine($"State after ToIdle: {Current}");

        }
    }
}

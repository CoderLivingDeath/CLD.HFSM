По клику игрок может переместится, посадить урожай, собрать, отнести в церковь и каждое действие задается конечным автоматом собираемым async/await и в процессе происходит вброс триггеров в стейт машину: StartMove, EndMove, Harvest... и стейт машина уже решает возможно ли переключиться в состояние или нет и как обратная реакция возможно проверить произошёл ли переход: метод bool TryFire(Trigger trigger).

Конфигурация

Idle:

StartMove : idle -> Move

StartPlanting : idle -> Planting

StartHarvest : idle -> Harvest

Move:

EndMove : Move -> idle

StartPlanting : Move -> Planting

StartHarvest : Move -> Harvest

StartCarriesToChurch : Move -> CarriesToChurch

Planting:
EndPlanting : Planting -> idle

Harvest:

EndHarvest : Harvest -> idle

StartMove : Harvest -> Move

CarriesToChurch:

StopCarriesToChurch : CarriesToChurch -> idle

```cs
using CLD.HFSM;
using System.Numerics;
using System.Threading.Tasks;
using Test;

namespace Test
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var unit = new Unit();

            // Симуляция кликов игрока
            unit.OnPlayerClick(new Vector2(1, 1), ClickTarget.Ground); // Move

            unit.OnPlayerClick(new Vector2(2, 2), ClickTarget.Field); // Planting

            Console.ReadKey();
        }
    }

    public enum ClickTarget
    {
        Ground, Field, Plant, Church
    }

    public class Unit
    {
        public enum State
        {
            idle, Move, Planting, Harvest, CarriesToChurch
        }

        public enum Trigger
        {
            StartIdle, StartMove, EndMove, StartPlanting, EndPlanting,
            StartHarvest, EndHarvest, StartCarriesToChurch, StopCarriesToChurch
        }

        private StateMachine<State, Trigger> stateMachine;
        private CancellationTokenSource _cts = new();

        public State CurrentState => stateMachine.СurrentState;

        public Unit()
        {
            stateMachine = new(State.idle, GetConfiguration());
        }

        public StateMachineConfiguration<State, Trigger> GetConfiguration()
        {
            var builder = new StateMachineConfigurationBuilder<State, Trigger>();

            builder.ConfigureState(State.idle)
                .Permit(Trigger.StartPlanting, State.Planting)
                .Permit(Trigger.StartHarvest, State.Harvest)
                .Permit(Trigger.StartMove, State.Move)
                .OnEnter(() => Console.WriteLine("юнит отдыхает"));

            builder.ConfigureState(State.Move)
                .Permit(Trigger.EndMove, State.idle)
                .Permit(Trigger.StartPlanting, State.Planting)
                .Permit(Trigger.StartHarvest, State.Harvest)
                .Permit(Trigger.StartCarriesToChurch, State.CarriesToChurch)
                .OnEnter(() => Console.WriteLine("двигаемся к цели"));

            builder.ConfigureState(State.Planting)
                .Permit(Trigger.EndPlanting, State.idle)
                .OnEnter(() => Console.WriteLine("сажаем"));

            builder.ConfigureState(State.Harvest)
                .Permit(Trigger.EndHarvest, State.idle)
                .Permit(Trigger.StartMove, State.Move)
                .OnEnter(() => Console.WriteLine("собираем урожай"));

            builder.ConfigureState(State.CarriesToChurch)
                .Permit(Trigger.StopCarriesToChurch, State.idle)
                .OnEnter(() => Console.WriteLine("несем в церковь"));

            return builder.GetConfiguration();
        }

        public void Update()
        {
        }

        public void OnPlayerClick(Vector2 point, ClickTarget target)
        {
            switch (target)
            {
                case ClickTarget.Ground:
                    _ = MoveTo(point, _cts.Token);
                    break;
                case ClickTarget.Field:
                    _ = Plant(point, _cts.Token);
                    break;
                case ClickTarget.Plant:
                    _ = Harvest(point, _cts.Token);
                    break;
                case ClickTarget.Church:
                    _ = CarriesToChurch(point, _cts.Token);
                    break;
            }
        }

        private async Task MoveTo(Vector2 point, CancellationToken token)
        {
            if (stateMachine.TryFire(Trigger.StartMove))
            {

                try
                {
                    await Task.Delay(2000, token);
                    Console.WriteLine("Дошли до цели");
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                stateMachine.Fire(Trigger.EndMove);
            }
            else
            {
                Console.WriteLine("Невозможно начать движение");
            }
        }

        private async Task Plant(Vector2 point, CancellationToken token)
        {
            if (stateMachine.TryFire(Trigger.StartPlanting))
            {
                try
                {
                    await Task.Delay(1500, token);
                    Console.WriteLine("Посадили растения");
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                stateMachine.Fire(Trigger.EndPlanting);
            }
        }

        private async Task Harvest(Vector2 point, CancellationToken token)
        {
            if (stateMachine.TryFire(Trigger.StartHarvest))
            {
                Console.WriteLine($"Клик по урожаю в {point}");

                try
                {
                    await Task.Delay(1500, token);
                    Console.WriteLine("Урожай собран");
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                stateMachine.Fire(Trigger.EndHarvest);
            }
        }

        private async Task CarriesToChurch(Vector2 point, CancellationToken token)
        {
            if (stateMachine.TryFire(Trigger.StartCarriesToChurch))
            {
                Console.WriteLine($"Клик по церкви в {point}");

                try
                {
                    await Task.Delay(2000, token);
                    Console.WriteLine("Доставили в церковь");
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                stateMachine.Fire(Trigger.StopCarriesToChurch);
            }
        }
    }
}

```

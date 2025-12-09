using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using CLD.HFSM;
using System;

namespace Core.Benchmarks
{
    [MemoryDiagnoser]
    [HideColumns("Job", "Environment", "LaunchCount", "WarmupCount", "IterationCount")]
    [SimpleJob(RuntimeMoniker.Net80, baseline: true)]
    [SimpleJob(RuntimeMoniker.Net80, id: "HighAccuracy",
               warmupCount: 10, iterationCount: 15)]
    public class StateMachineGenericTypesBenchmarks
    {
        #region helper types

        public readonly struct StructState : IEquatable<StructState>
        {
            public readonly int Id;
            public StructState(int id) => Id = id;
            public bool Equals(StructState other) => Id == other.Id;
            public override bool Equals(object? obj) => obj is StructState s && Equals(s);
            public override int GetHashCode() => Id;
            public override string ToString() => $"S{Id}";
        }

        public readonly struct StructTrigger : IEquatable<StructTrigger>
        {
            public readonly int Id;
            public StructTrigger(int id) => Id = id;
            public bool Equals(StructTrigger other) => Id == other.Id;
            public override bool Equals(object? obj) => obj is StructTrigger t && Equals(t);
            public override int GetHashCode() => Id;
            public override string ToString() => $"T{Id}";
        }

        public sealed class ClassState : IEquatable<ClassState>
        {
            public string Name { get; }
            public ClassState(string name) => Name = name;
            public bool Equals(ClassState? other) => other != null && Name == other.Name;
            public override bool Equals(object? obj) => Equals(obj as ClassState);
            public override int GetHashCode() => Name.GetHashCode();
            public override string ToString() => Name;
        }

        public sealed class ClassTrigger : IEquatable<ClassTrigger>
        {
            public string Name { get; }
            public ClassTrigger(string name) => Name = name;
            public bool Equals(ClassTrigger? other) => other != null && Name == other.Name;
            public override bool Equals(object? obj) => Equals(obj as ClassTrigger);
            public override int GetHashCode() => Name.GetHashCode();
            public override string ToString() => Name;
        }

        public enum EnumState { Idle, Running, Paused, GameOver }
        public enum EnumTrigger { Start, Pause, Resume, Stop }

        #endregion

        // state machines
        public StateMachine<EnumState, EnumTrigger> _enumSm = null!;
        public StateMachine<StructState, StructTrigger> _structSm = null!;
        public StateMachine<ClassState, ClassTrigger> _classSm = null!;

        // reusable values
        public readonly EnumTrigger _eStart = EnumTrigger.Start;
        public readonly EnumTrigger _eInvalid = EnumTrigger.Resume; // invalid in Idle
        public readonly StructTrigger _sStart = new StructTrigger(1);
        public readonly StructTrigger _sInvalid = new StructTrigger(99);
        public readonly ClassTrigger _cStart = new ClassTrigger("Start");
        public readonly ClassTrigger _cInvalid = new ClassTrigger("Invalid");

        public readonly StructState _sIdle = new StructState(0);
        public readonly StructState _sRunning = new StructState(1);
        public readonly ClassState _cIdle = new ClassState("Idle");
        public readonly ClassState _cRunning = new ClassState("Running");

        #region configuration builders

        private static StateMachineConfiguration<EnumState, EnumTrigger> BuildEnumConfig()
        {
            var b = new StateMachineConfigurationBuilder<EnumState, EnumTrigger>();
            b.ConfigureState(EnumState.Idle)
             .Permit(EnumTrigger.Start, EnumState.Running);

            b.ConfigureState(EnumState.Running)
             .Permit(EnumTrigger.Pause, EnumState.Paused)
             .Permit(EnumTrigger.Stop, EnumState.GameOver);

            b.ConfigureState(EnumState.Paused)
             .Permit(EnumTrigger.Resume, EnumState.Running)
             .Permit(EnumTrigger.Stop, EnumState.GameOver);

            b.ConfigureState(EnumState.GameOver);

            return b.GetConfiguration();
        }

        private StateMachineConfiguration<StructState, StructTrigger> BuildStructConfig()
        {
            var b = new StateMachineConfigurationBuilder<StructState, StructTrigger>();
            b.ConfigureState(_sIdle)
             .Permit(_sStart, _sRunning);

            b.ConfigureState(_sRunning);
            return b.GetConfiguration();
        }

        private StateMachineConfiguration<ClassState, ClassTrigger> BuildClassConfig()
        {
            var b = new StateMachineConfigurationBuilder<ClassState, ClassTrigger>();
            b.ConfigureState(_cIdle)
             .Permit(_cStart, _cRunning);

            b.ConfigureState(_cRunning);
            return b.GetConfiguration();
        }

        #endregion

        #region global setup / reset

        [GlobalSetup]
        public void GlobalSetup()
        {
            _enumSm = new StateMachine<EnumState, EnumTrigger>(EnumState.Idle, BuildEnumConfig());
            _structSm = new StateMachine<StructState, StructTrigger>(_sIdle, BuildStructConfig());
            _classSm = new StateMachine<ClassState, ClassTrigger>(_cIdle, BuildClassConfig());
        }

        [IterationSetup]
        public void IterationSetup()
        {
            _enumSm.ForceTransition(EnumState.Idle);
            _structSm.ForceTransition(_sIdle);
            _classSm.ForceTransition(_cIdle);
        }

        #endregion

        #region configuration benchmarks

        [Benchmark(Description = "Config Enum<TState,TTrigger>")]
        public StateMachineConfiguration<EnumState, EnumTrigger> Config_Enum()
            => BuildEnumConfig();

        [Benchmark(Description = "Config Struct<TState,TTrigger>")]
        public StateMachineConfiguration<StructState, StructTrigger> Config_Struct()
            => BuildStructConfig();

        [Benchmark(Description = "Config Class<TState,TTrigger>")]
        public StateMachineConfiguration<ClassState, ClassTrigger> Config_Class()
            => BuildClassConfig();

        [Benchmark(Description = "Reconfigure Enum state machine")]
        public void Reconfigure_Enum()
        {
            var cfg = BuildEnumConfig();
            _enumSm.Configure(cfg);
        }

        [Benchmark(Description = "Reconfigure Struct state machine")]
        public void Reconfigure_Struct()
        {
            var cfg = BuildStructConfig();
            _structSm.Configure(cfg);
        }

        [Benchmark(Description = "Reconfigure Class state machine")]
        public void Reconfigure_Class()
        {
            var cfg = BuildClassConfig();
            _classSm.Configure(cfg);
        }

        #endregion

        #region single Fire - valid

        [Benchmark(Description = "Enum - single valid Fire")]
        public void Enum_SingleValid()
        {
            _enumSm.TryFire(_eStart); // Idle -> Running
        }

        [Benchmark(Description = "Struct - single valid Fire")]
        public void Struct_SingleValid()
        {
            _structSm.TryFire(_sStart); // Idle -> Running
        }

        [Benchmark(Description = "Class - single valid Fire")]
        public void Class_SingleValid()
        {
            _classSm.TryFire(_cStart); // Idle -> Running
        }

        #endregion

        #region single Fire - invalid

        [Benchmark(Description = "Enum - single invalid Fire")]
        public void Enum_SingleInvalid()
        {
            _enumSm.TryFire(_eInvalid); // Resume in Idle
        }

        [Benchmark(Description = "Struct - single invalid Fire")]
        public void Struct_SingleInvalid()
        {
            _structSm.TryFire(_sInvalid);
        }

        [Benchmark(Description = "Class - single invalid Fire")]
        public void Class_SingleInvalid()
        {
            _classSm.TryFire(_cInvalid);
        }

        #endregion

        #region stress: 100 valid Fire

        [Benchmark(Description = "Enum - 100 valid Fire")]
        public void Enum_100Valid()
        {
            for (int i = 0; i < 100; i++)
            {
                _enumSm.TryFire(_eStart);
                _enumSm.ForceTransition(EnumState.Idle);
            }
        }

        [Benchmark(Description = "Struct - 100 valid Fire")]
        public void Struct_100Valid()
        {
            for (int i = 0; i < 100; i++)
            {
                _structSm.TryFire(_sStart);
                _structSm.ForceTransition(_sIdle);
            }
        }

        [Benchmark(Description = "Class - 100 valid Fire")]
        public void Class_100Valid()
        {
            for (int i = 0; i < 100; i++)
            {
                _classSm.TryFire(_cStart);
                _classSm.ForceTransition(_cIdle);
            }
        }

        #endregion

        #region stress: 1000 invalid Fire

        [Benchmark(Description = "Enum - 1000 invalid Fire")]
        public void Enum_1000Invalid()
        {
            for (int i = 0; i < 1000; i++)
                _enumSm.TryFire(_eInvalid);
        }

        [Benchmark(Description = "Struct - 1000 invalid Fire")]
        public void Struct_1000Invalid()
        {
            for (int i = 0; i < 1000; i++)
                _structSm.TryFire(_sInvalid);
        }

        [Benchmark(Description = "Class - 1000 invalid Fire")]
        public void Class_1000Invalid()
        {
            for (int i = 0; i < 1000; i++)
                _classSm.TryFire(_cInvalid);
        }

        #endregion

        #region handlers + OnTransition

        [Benchmark(Description = "Enum - valid Fire with handlers + OnTransition")]
        public void Enum_WithHandlersAndTransitionCallback()
        {
            bool exitCalled = false;
            bool enterCalled = false;
            bool transitionCalled = false;

            var builder = new StateMachineConfigurationBuilder<EnumState, EnumTrigger>();
            builder.ConfigureState(EnumState.Idle)
                   .OnExit(() => exitCalled = true)
                   .Permit(EnumTrigger.Start, EnumState.Running);

            builder.ConfigureState(EnumState.Running)
                   .OnEnter(() => enterCalled = true);

            builder.OnTransition((s, t) => transitionCalled = true);

            var config = builder.GetConfiguration();
            var sm = new StateMachine<EnumState, EnumTrigger>(EnumState.Idle, config);

            sm.TryFire(EnumTrigger.Start);

            // защита от агрессивного inlining/JIT‑выбрасывания
            if (!exitCalled || !enterCalled || !transitionCalled)
                throw new InvalidOperationException("handlers not called");
        }

        #endregion
    }
}

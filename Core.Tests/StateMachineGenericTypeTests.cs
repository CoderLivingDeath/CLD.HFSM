using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CLD.HFSM;

namespace Core.Tests
{
    [TestClass]
    public class StateMachineGenericTypeTests
    {
        #region Вспомогательные типы

        private readonly struct StructState : IEquatable<StructState>
        {
            public readonly int Id;
            public StructState(int id) => Id = id;

            public bool Equals(StructState other) => Id == other.Id;
            public override bool Equals(object? obj) => obj is StructState s && Equals(s);
            public override int GetHashCode() => Id;
            public override string ToString() => $"S{Id}";
        }

        private readonly struct StructTrigger : IEquatable<StructTrigger>
        {
            public readonly int Id;
            public StructTrigger(int id) => Id = id;

            public bool Equals(StructTrigger other) => Id == other.Id;
            public override bool Equals(object? obj) => obj is StructTrigger t && Equals(t);
            public override int GetHashCode() => Id;
            public override string ToString() => $"T{Id}";
        }

        private sealed class ClassState : IEquatable<ClassState>
        {
            public string Name { get; }
            public ClassState(string name) => Name = name;
            public bool Equals(ClassState? other) => other != null && Name == other.Name;
            public override bool Equals(object? obj) => Equals(obj as ClassState);
            public override int GetHashCode() => Name.GetHashCode();
            public override string ToString() => Name;
        }

        private sealed class ClassTrigger : IEquatable<ClassTrigger>
        {
            public string Name { get; }
            public ClassTrigger(string name) => Name = name;
            public bool Equals(ClassTrigger? other) => other != null && Name == other.Name;
            public override bool Equals(object? obj) => Equals(obj as ClassTrigger);
            public override int GetHashCode() => Name.GetHashCode();
            public override string ToString() => Name;
        }

        private enum EnumState { A, B }
        private enum EnumTrigger { Go }

        #endregion

        #region Enum -> sanity‑check

        [TestMethod]
        public void EnumTypes_WorkCorrectly()
        {
            var builder = new StateMachineConfigurationBuilder<EnumState, EnumTrigger>();
            builder.ConfigureState(EnumState.A).Permit(EnumTrigger.Go, EnumState.B);
            builder.ConfigureState(EnumState.B);

            var config = builder.GetConfiguration();
            var sm = new StateMachine<EnumState, EnumTrigger>(EnumState.A, config);

            Assert.AreEqual(EnumState.A, sm.СurrentState);
            Assert.IsTrue(sm.TryFire(EnumTrigger.Go));
            Assert.AreEqual(EnumState.B, sm.СurrentState);
        }

        #endregion

        #region Struct стейты / триггеры

        [TestMethod]
        public void StructTypes_SimpleTransition_Works()
        {
            var sIdle = new StructState(0);
            var sRunning = new StructState(1);
            var tStart = new StructTrigger(10);

            var builder = new StateMachineConfigurationBuilder<StructState, StructTrigger>();
            builder.ConfigureState(sIdle).Permit(tStart, sRunning);
            builder.ConfigureState(sRunning);

            var config = builder.GetConfiguration();
            var sm = new StateMachine<StructState, StructTrigger>(sIdle, config);

            Assert.AreEqual(sIdle, sm.СurrentState);
            Assert.IsTrue(sm.TryFire(tStart));
            Assert.AreEqual(sRunning, sm.СurrentState);
        }

        [TestMethod]
        public void StructTypes_InvalidTrigger_DoesNotChangeState()
        {
            var sIdle = new StructState(0);
            var sRunning = new StructState(1);
            var tStart = new StructTrigger(10);
            var tPause = new StructTrigger(20); // не сконфигурирован

            var builder = new StateMachineConfigurationBuilder<StructState, StructTrigger>();
            builder.ConfigureState(sIdle).Permit(tStart, sRunning);
            builder.ConfigureState(sRunning);

            var config = builder.GetConfiguration();
            var sm = new StateMachine<StructState, StructTrigger>(sIdle, config);

            Assert.IsFalse(sm.TryFire(tPause));
            Assert.AreEqual(sIdle, sm.СurrentState);
        }

        #endregion

        #region Class стейты / триггеры

        [TestMethod]
        public void ClassTypes_SimpleTransition_Works()
        {
            var idle = new ClassState("Idle");
            var running = new ClassState("Running");
            var start = new ClassTrigger("Start");

            var builder = new StateMachineConfigurationBuilder<ClassState, ClassTrigger>();
            builder.ConfigureState(idle).Permit(start, running);
            builder.ConfigureState(running);

            var config = builder.GetConfiguration();
            var sm = new StateMachine<ClassState, ClassTrigger>(idle, config);

            Assert.AreSame(idle, sm.СurrentState);
            Assert.IsTrue(sm.TryFire(start));
            Assert.AreSame(running, sm.СurrentState);
        }

        [TestMethod]
        public void ClassTypes_Handlers_AreCalled()
        {
            var idle = new ClassState("Idle");
            var running = new ClassState("Running");
            var start = new ClassTrigger("Start");

            bool idleExitCalled = false;
            bool runningEnterCalled = false;

            var builder = new StateMachineConfigurationBuilder<ClassState, ClassTrigger>();
            builder.ConfigureState(idle)
                   .OnExit(() => idleExitCalled = true)
                   .Permit(start, running);

            builder.ConfigureState(running)
                   .OnEnter(() => runningEnterCalled = true);

            var config = builder.GetConfiguration();
            var sm = new StateMachine<ClassState, ClassTrigger>(idle, config);

            Assert.IsTrue(sm.TryFire(start));
            Assert.AreSame(running, sm.СurrentState);
            Assert.IsTrue(idleExitCalled);
            Assert.IsTrue(runningEnterCalled);
        }

        [TestMethod]
        public void ClassTypes_InvalidTrigger_DoesNotChangeState()
        {
            var idle = new ClassState("Idle");
            var running = new ClassState("Running");
            var start = new ClassTrigger("Start");
            var pause = new ClassTrigger("Pause"); // не сконфигурирован

            var builder = new StateMachineConfigurationBuilder<ClassState, ClassTrigger>();
            builder.ConfigureState(idle).Permit(start, running);
            builder.ConfigureState(running);

            var config = builder.GetConfiguration();
            var sm = new StateMachine<ClassState, ClassTrigger>(idle, config);

            Assert.IsFalse(sm.TryFire(pause));
            Assert.AreSame(idle, sm.СurrentState);
        }

        #endregion
    }
}

using Microsoft.VisualStudio.TestTools.UnitTesting;
using CLD.HFSM;

namespace CLD.HFSM.Tests;

[TestClass]
public class HFSMBenchmarksCorrectnessTests
{
    public enum TestState
    {
        Root, A1, A2, A3, B1, B2, B3, Idle, MoveToA3, Attack, MoveToA2
    }

    public enum TestTrigger
    {
        ToIdle, ToMoveA3, ToAttack, ToMoveA2, InvalidTrigger
    }

    private static StateMachineConfiguration<TestState, TestTrigger> BuildConfig()
    {
        var b = new StateMachineConfigurationBuilder<TestState, TestTrigger>();

        b.ConfigureState(TestState.Root);
        b.ConfigureState(TestState.A1).SubstateOf(TestState.Root);
        b.ConfigureState(TestState.A2).SubstateOf(TestState.A1);
        b.ConfigureState(TestState.A3).SubstateOf(TestState.A2);
        b.ConfigureState(TestState.B1).SubstateOf(TestState.Root);
        b.ConfigureState(TestState.B2).SubstateOf(TestState.B1);
        b.ConfigureState(TestState.B3).SubstateOf(TestState.B2);

        b.ConfigureState(TestState.Idle)
            .SubstateOf(TestState.A3)
            .Permit(TestTrigger.ToMoveA3, TestState.MoveToA3);

        b.ConfigureState(TestState.MoveToA3)
            .SubstateOf(TestState.A3)
            .Permit(TestTrigger.ToIdle, TestState.Idle);

        b.ConfigureState(TestState.Attack)
            .SubstateOf(TestState.B3)
            .Permit(TestTrigger.ToIdle, TestState.Idle)
            .Permit(TestTrigger.ToMoveA2, TestState.MoveToA2);

        b.ConfigureState(TestState.MoveToA2)
            .SubstateOf(TestState.A2)
            .Permit(TestTrigger.ToAttack, TestState.Attack);

        return b.GetConfiguration();
    }

    private static StateMachine<TestState, TestTrigger> Create(bool useCache)
        => new(TestState.Idle, BuildConfig(), useCache);

    private static void ResetToIdle(StateMachine<TestState, TestTrigger> fsm)
    {
        if (fsm.CurrentState == TestState.Idle)
            return;

        if (fsm.CurrentState is TestState.MoveToA3 or TestState.Attack)
        {
            fsm.TryFire(TestTrigger.ToIdle);
            return;
        }

        if (fsm.CurrentState == TestState.MoveToA2)
        {
            fsm.TryFire(TestTrigger.ToAttack);
            fsm.TryFire(TestTrigger.ToIdle);
            return;
        }

        fsm.TryFire(TestTrigger.ToIdle);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Configuration_CreatesMachine(bool useCache)
    {
        var fsm = Create(useCache);
        Assert.AreEqual(TestState.Idle, fsm.CurrentState);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void ValidTryFire_Idle_ToMoveA3_Then_ToIdle(bool useCache)
    {
        var fsm = Create(useCache);

        Assert.IsTrue(fsm.TryFire(TestTrigger.ToMoveA3));
        Assert.AreEqual(TestState.MoveToA3, fsm.CurrentState);

        Assert.IsTrue(fsm.TryFire(TestTrigger.ToIdle));
        Assert.AreEqual(TestState.Idle, fsm.CurrentState);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void InvalidTryFire_AlwaysFalse(bool useCache)
    {
        var fsm = Create(useCache);

        Assert.IsFalse(fsm.TryFire(TestTrigger.InvalidTrigger));
        Assert.AreEqual(TestState.Idle, fsm.CurrentState);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Fire_WithHandlers_SetsIdle(bool useCache)
    {
        var fsm = Create(useCache);

        Assert.IsTrue(fsm.TryFire(TestTrigger.ToMoveA3));
        fsm.Fire(TestTrigger.ToIdle);
        Assert.AreEqual(TestState.Idle, fsm.CurrentState);
    }

    [TestMethod]
    [DataRow(false, 10)]
    [DataRow(true, 10)]
    [DataRow(false, 1000)]
    [DataRow(true, 1000)]
    public void Batch_Valid_TryFireThenFire_AllOk(bool useCache, int count)
    {
        var fsm = Create(useCache);

        for (int i = 0; i < count; i++)
        {
            Assert.AreEqual(TestState.Idle, fsm.CurrentState);
            Assert.IsTrue(fsm.TryFire(TestTrigger.ToMoveA3));
            fsm.Fire(TestTrigger.ToIdle);
            Assert.AreEqual(TestState.Idle, fsm.CurrentState);
        }
    }

    [TestMethod]
    [DataRow(false, 1000)]
    [DataRow(true, 1000)]
    public void Batch_Invalid_AllFalse(bool useCache, int count)
    {
        var fsm = Create(useCache);

        for (int i = 0; i < count; i++)
        {
            Assert.IsFalse(fsm.TryFire(TestTrigger.InvalidTrigger));
            Assert.AreEqual(TestState.Idle, fsm.CurrentState);
        }
    }

    [TestMethod]
    [DataRow(false, 120)]
    [DataRow(true, 120)]
    [DataRow(false, 60)]
    [DataRow(true, 60)]
    [DataRow(false, 30)]
    [DataRow(true, 30)]
    public void GameScenario_ExpectedSuccessFailCounts(bool useCache, int fps)
    {
        var fsm = Create(useCache);
        ResetToIdle(fsm);

        int frames = fps * 5;
        int success = 0, failed = 0;

        for (int i = 0; i < frames; i++)
        {
            var trigger =
                (i % 6 == 0) ? TestTrigger.ToMoveA3 :
                (i % 6 == 3) ? TestTrigger.ToIdle :
                TestTrigger.InvalidTrigger;

            bool actual = fsm.TryFire(trigger);
            if (actual) success++; else failed++;
        }

        // На каждые 6 кадров: 2 успеха (ToMoveA3, ToIdle) и 4 провала (InvalidTrigger)
        int expectedSuccess = (frames / 6) * 2;
        int expectedFailed = frames - expectedSuccess;

        Assert.AreEqual(expectedSuccess, success);
        Assert.AreEqual(expectedFailed, failed);
    }
}

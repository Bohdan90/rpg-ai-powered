using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RPG.Core;

namespace RPG.Tests
{
    public class StrategicSaveTests
    {
        private static void Same(StrategicScenario a,StrategicScenario b)
        {
            Assert.That(b.DebugState(),Is.EqualTo(a.DebugState()));
            Assert.That(b.CaptureSave().checksum,Is.EqualTo(a.CaptureSave().checksum));
            CollectionAssert.AreEqual(a.Events,b.Events);
            Assert.That(b.Player,Is.Not.SameAs(a.Player));
        }
        [TestCase("initial")][TestCase("actors")][TestCase("intercept")][TestCase("hard-guard")]
        [TestCase("area-guard")][TestCase("death")][TestCase("withdrawal")][TestCase("waystation")]
        [TestCase("village")][TestCase("provisions")][TestCase("formation-lost")][TestCase("success")]
        public void StableStateRoundTripPreservesAllWorldAndPersistentFields(string phase)
        {
            var s=new StrategicScenario(718);
            if(phase=="actors")s.EndActivation();
            if(phase=="intercept"){StrategicScenarioTests.Move(s,5);s.EndActivation();StrategicScenarioTests.Move(s,6,10);}
            if(new[]{"hard-guard","area-guard","death","withdrawal","formation-lost","success"}.Contains(phase))
            {
                StrategicScenarioTests.Move(s,3,8);
                var final=StrategicScenarioTests.Finish(s,phase!="withdrawal"&&phase!="formation-lost",phase=="withdrawal");
                if(phase=="death")
                {
                    var commander=final.FindUnit(new UnitId(1));commander.Hp=0;commander.Status=UnitStatus.Dead;
                    var escaped=final.FindUnit(new UnitId(3));escaped.Hp=17;escaped.Armor=2;escaped.Status=UnitStatus.Escaped;
                }
                s.ResolveBattle(final);
                if(phase=="area-guard"||phase=="success")
                {s.EndActivation();StrategicScenarioTests.Move(s,10);s.Attack(StrategicActorKind.AreaGuard);StrategicScenarioTests.Win(s);}
                if(phase=="success")
                {s.PortalInvestigated=true;s.Actor(StrategicActorKind.IncursionB).Objective=StrategicObjective.Exited;s.PlayerNode=2;s.Tempo=100;s.Move(1);}
            }
            if(phase=="waystation"){s.EndActivation();s.EndActivation();}
            if(phase=="village")for(int i=0;i<5;i++)s.EndActivation();
            if(phase=="provisions"){StrategicScenarioTests.Move(s,5);s.EndActivation();}
            Assert.That(s.CanSave,Is.True);var restored=s.CaptureSave().Restore();Same(s,restored);
            Assert.That(restored.Player.Members.Select(c=>c.CharacterId),Is.EqualTo(s.Player.Members.Select(c=>c.CharacterId)));
            Assert.That(restored.Player.Commanderless,Is.EqualTo(s.Player.Commanderless));
            Assert.That(restored.Actors.Select(a=>a.Objective),Is.EqualTo(s.Actors.Select(a=>a.Objective)));
        }
        [Test]
        public void ProgressionOverflowAndFractionalRecoveryRemainExact()
        {
            var s=new StrategicScenario();StrategicScenarioTests.Move(s,3,8);var f=StrategicScenarioTests.Finish(s);
            f.FindUnit(new UnitId(5)).Hp=10;s.ResolveBattle(f);s.Player.Commander.AddPersonalXp(23.125m);s.Player.Commander.AddCommandXp(35.125m);
            s.EndActivation();Assert.That(s.Player.Members[4].FieldRecoveryRemainderHundredths,Is.EqualTo(20));
            var copy=s.CaptureSave().Restore();Same(s,copy);
            Assert.That(copy.Player.Commander.PersonalLevel,Is.EqualTo(s.Player.Commander.PersonalLevel));
            Assert.That(copy.Player.Commander.CommandLevel,Is.EqualTo(2));
            Assert.That(copy.Player.Commander.CommandXp,Is.GreaterThan(35m));
        }
        [Test]
        public void LoadedStateProducesIdenticalActorsBattlesRngAndFollowingSeed()
        {
            var a=new StrategicScenario();StrategicScenarioTests.Move(a,14);var b=a.CaptureSave().Restore();
            a.EndActivation();b.EndActivation();var left=a.Encounter.Battle.State;var right=b.Encounter.Battle.State;
            Assert.That(BattleStateHash.Compute(right),Is.EqualTo(BattleStateHash.Compute(left)));
            for(int i=0;i<1200&&!left.Outcome.IsEnded;i++)
            {
                var command=TacticalAi.Choose(left).Command;
                left=BattleResolver.Apply(left,command).State;right=BattleResolver.Apply(right,command).State;
                Assert.That(BattleStateHash.Compute(right),Is.EqualTo(BattleStateHash.Compute(left)));
            }
            Assert.That(left.Outcome.IsEnded,Is.True);a.ResolveBattle(left);b.ResolveBattle(right);Same(a,b);
            var c=b.CaptureSave().Restore();
            StrategicScenarioTests.Move(a,17,10);StrategicScenarioTests.Move(c,17,10);a.EndActivation();c.EndActivation();
            Assert.That(BattleStateHash.Compute(c.Encounter.Battle.State),Is.EqualTo(BattleStateHash.Compute(a.Encounter.Battle.State)));
        }
        [Test]
        public void MidBattleCaptureRejectedWithoutMutation()
        {
            var s=new StrategicScenario();StrategicScenarioTests.Move(s,3,8);var before=s.DebugState();
            Assert.That(s.CanSave,Is.False);Assert.Throws<InvalidOperationException>(()=>s.CaptureSave());Assert.That(s.DebugState(),Is.EqualTo(before));
        }
        [TestCase("version")][TestCase("checksum")][TestCase("identity")][TestCase("status")][TestCase("missing")][TestCase("phase")]
        public void InvalidSnapshotFailsWithoutTouchingOriginal(string kind)
        {
            var s=new StrategicScenario();var d=s.CaptureSave();string before=s.DebugState();
            if(kind=="version")d.version++;
            if(kind=="checksum")d.tempo--;
            if(kind=="identity")d.player.members[0].id="replacement";
            if(kind=="status")d.player.members[0].status=99;
            if(kind=="missing")d.actors=null;
            if(kind=="phase")d.worldPhase=true;
            Assert.Throws<InvalidDataException>(()=>d.Restore());Assert.That(s.DebugState(),Is.EqualTo(before));
        }
    }
}

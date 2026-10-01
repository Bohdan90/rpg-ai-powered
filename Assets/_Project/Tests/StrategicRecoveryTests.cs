using System;
using System.Linq;
using NUnit.Framework;
using RPG.Core;

namespace RPG.Tests
{
    public class StrategicRecoveryTests
    {
        private static void Damage(PersistentCharacter c,int hp,int armor=0,PersistentCharacterStatus status=PersistentCharacterStatus.Alive)
        {
            c.SetBattleResult(new UnitState(new UnitId(1),Side.West,c.Profile,new GridPosition(1,1),Facing.East,hp,armor,
                status==PersistentCharacterStatus.Dead?UnitStatus.Dead:status==PersistentCharacterStatus.EscapedSafe?UnitStatus.Escaped:UnitStatus.Active));
        }
        [TestCase(1,40,33)][TestCase(2,15,23)]
        public void RecoveryUsesPhysicalCheckpointLocationAndNeverStacks(int node,int percent,int expected)
        {
            var s=new StrategicScenario();Damage(s.Player.Commander,17,5);if(node==2)s.Move(2);
            Assert.That(s.Player.Commander.Hp,Is.EqualTo(17),"Movement alone cannot heal");
            Assert.That(s.RecoveryPercent,Is.EqualTo(percent));s.EndActivation();
            Assert.That(s.Player.Commander.Hp,Is.EqualTo(expected));Assert.That(s.Player.Commander.Armor,Is.EqualTo(5));
            Assert.That(s.Refresh,Is.EqualTo(2));
        }
        [Test]
        public void ClampDeadCommanderSafeIdentityAndProgressionArePreserved()
        {
            var s=new StrategicScenario();var ids=s.Player.Members.Select(c=>c.CharacterId).ToArray();
            Damage(s.Player.Commander,0,0,PersistentCharacterStatus.Dead);s.Player.RefreshCommanderState();
            Damage(s.Player.Members[1],39,3);Damage(s.Player.Members[2],17,2,PersistentCharacterStatus.EscapedSafe);
            s.Player.Members[2].AddPersonalXp(23.125m);s.EndActivation();
            Assert.That(s.Player.Commander.Hp,Is.Zero);Assert.That(s.Player.Commander.Status,Is.EqualTo(PersistentCharacterStatus.Dead));
            Assert.That(s.Player.Commanderless&&s.Player.RosterLocked,Is.True);Assert.That(s.Player.Members[1].Hp,Is.EqualTo(40));
            var safe=s.Player.Members[2];Assert.That(safe.Hp,Is.EqualTo(33));Assert.That(safe.Armor,Is.EqualTo(2));
            Assert.That(safe.Status,Is.EqualTo(PersistentCharacterStatus.EscapedSafe));Assert.That(safe.PersonalXp,Is.EqualTo(23.125m));Assert.That(safe.PersonalLevel,Is.EqualTo(3));
            CollectionAssert.AreEqual(ids,s.Player.Members.Select(c=>c.CharacterId));Assert.That(s.Player.LivingMembers.Count(),Is.EqualTo(5));
            var restored=s.CaptureSave().Restore();Assert.That(restored.Player.Commanderless&&restored.Player.RosterLocked,Is.True);
        }
        [Test]
        public void FractionalRemainderSurvivesRateChangeSaveLoadAndClamp()
        {
            var s=new StrategicScenario();var archer=s.Player.Members[4];Damage(archer,1);s.Move(2);s.EndActivation();
            Assert.That(archer.Hp,Is.EqualTo(5));Assert.That(archer.FieldRecoveryRemainderHundredths,Is.EqualTo(20));
            s.Move(1);var loaded=s.CaptureSave().Restore();s.EndActivation();loaded.EndActivation();
            Assert.That(archer.Hp,Is.EqualTo(16));Assert.That(archer.FieldRecoveryRemainderHundredths,Is.EqualTo(40));
            Assert.That(loaded.CaptureSave().checksum,Is.EqualTo(s.CaptureSave().checksum));
            s.EndActivation();Assert.That(archer.Hp,Is.EqualTo(27));Assert.That(archer.FieldRecoveryRemainderHundredths,Is.EqualTo(60));
            s.EndActivation();Assert.That(archer.Hp,Is.EqualTo(28));Assert.That(archer.FieldRecoveryRemainderHundredths,Is.Zero);
        }
        [Test]
        public void BattleExitNoHealThenDamagedReturnSaveRecoverAndRedeploy()
        {
            var s=new StrategicScenario();StrategicScenarioTests.Move(s,3,8);var end=StrategicScenarioTests.Finish(s);
            end.FindUnit(new UnitId(1)).Hp=7;end.FindUnit(new UnitId(1)).Armor=2;s.ResolveBattle(end);
            Assert.That(s.Player.Commander.Hp,Is.EqualTo(7));Assert.That(s.Refresh,Is.EqualTo(1));
            s.EndActivation();Assert.That(s.Player.Commander.Hp,Is.EqualTo(13));StrategicScenarioTests.Move(s,1);
            Assert.That(s.Player.Commander.Hp,Is.EqualTo(13));var copy=s.CaptureSave().Restore();
            s.EndActivation();copy.EndActivation();Assert.That(s.Player.Commander.Hp,Is.EqualTo(29));Assert.That(s.Player.Commander.Armor,Is.EqualTo(2));
            Assert.That(copy.CaptureSave().checksum,Is.EqualTo(s.CaptureSave().checksum));StrategicScenarioTests.Move(copy,3);
            Assert.That(copy.Player.Commander.CharacterId,Is.EqualTo("baron-1"));Assert.That(copy.Player.Commander.Hp,Is.EqualTo(29));
        }
        [Test]
        public void WaitingAtKeepDoesNotFreezeActorsSupplyOrThreatFailure()
        {
            var s=new StrategicScenario();Damage(s.Player.Commander,1,2);s.Provisions=18;s.EndActivation();
            Assert.That(s.Player.Commander.Hp,Is.EqualTo(17));Assert.That(s.Provisions,Is.EqualTo(18));
            Assert.That(s.Actor(StrategicActorKind.Patrol).Node,Is.EqualTo(7));Assert.That(s.Actor(StrategicActorKind.IncursionA).RaidArmed,Is.True);
            s.EndActivation();Assert.That(s.Player.Commander.Hp,Is.EqualTo(33));Assert.That(s.Waystation,Is.EqualTo(StrategicSiteCondition.Ravaged));
            s.EndActivation();Assert.That(s.Actor(StrategicActorKind.IncursionB).Objective,Is.EqualTo(StrategicObjective.Raid));
            s.EndActivation();Assert.That(s.Actor(StrategicActorKind.IncursionB).RaidArmed,Is.True);
            s.EndActivation();Assert.That(s.Result,Is.EqualTo(StrategicMissionResult.VillageRavaged));
        }
        [Test]
        public void PhysicalWithdrawalToKeepUsesKeepRateOnlyOnNextRefresh()
        {
            var s=new StrategicScenario();StrategicScenarioTests.Move(s,3,8);var end=StrategicScenarioTests.Finish(s,false,true);
            end.FindUnit(new UnitId(1)).Hp=10;end.FindUnit(new UnitId(1)).Armor=1;s.ResolveBattle(end);
            Assert.That(s.PlayerNode,Is.EqualTo(1));Assert.That(s.Player.Commander.Hp,Is.EqualTo(10));Assert.That(s.Tempo,Is.EqualTo(-40));
            var copy=s.CaptureSave().Restore();copy.EndActivation();Assert.That(copy.Player.Commander.Hp,Is.EqualTo(26));
            Assert.That(copy.Player.Commander.Armor,Is.EqualTo(1));Assert.That(copy.Tempo,Is.EqualTo(60));
        }
        [Test]
        public void InterruptedActorPhaseDefersRecoveryThenAppliesItOnce()
        {
            var s=new StrategicScenario();s.Move(14);Damage(s.Player.Commander,10,2);s.EndActivation();
            Assert.That(s.Encounter,Is.Not.Null);Assert.That(s.Player.Commander.Hp,Is.EqualTo(10));Assert.That(s.Refresh,Is.EqualTo(1));
            StrategicScenarioTests.Win(s);Assert.That(s.Refresh,Is.EqualTo(2));Assert.That(s.Player.Commander.Hp,Is.EqualTo(16));
            var copy=s.CaptureSave().Restore();Assert.That(copy.Player.Commander.Hp,Is.EqualTo(16));
        }
        [Test]
        public void RecoveryPreviewAndSaveArePureAndExactlyMatchNextCheckpoint()
        {
            var s=new StrategicScenario();Damage(s.Player.Commander,17,5);string before=s.CaptureSave().checksum;
            for(int i=0;i<20;i++)Assert.That(s.Player.Commander.PreviewHpRecovery(s.RecoveryPercent),Is.EqualTo(16));
            Assert.That(s.CaptureSave().checksum,Is.EqualTo(before));var loaded=s.CaptureSave().Restore();
            Assert.That(loaded.CaptureSave().checksum,Is.EqualTo(before));loaded.EndActivation();
            Assert.That(loaded.Player.Commander.Hp,Is.EqualTo(33));Assert.That(s.Player.Commander.Hp,Is.EqualTo(17));
        }
    }
}

using System;
using System.Linq;
using NUnit.Framework;
using RPG.Core;

namespace RPG.Tests
{
    public class CrossroadsEconomyTests
    {
        private static CrossroadsScenario Fixture(Action<CrossroadsSaveData> edit=null)
        {var d=new CrossroadsScenario(economy:true).CaptureSave();edit?.Invoke(d);d.checksum=d.ComputeHash();return d.Restore();}
        private static void Kill(StrategicSaveFormation f,int i)
        {f.members[i].hp=0;f.members[i].status=(int)PersistentCharacterStatus.Dead;if(i==0)f.commanderless=f.rosterLocked=true;}
        private static void Cycle(CrossroadsScenario s){CrossroadsTests.End(s);CrossroadsTests.End(s);}
        [Test] public void ExactPrototypeStartingValues()
        {var s=Fixture();Assert.That(s.West.Gold,Is.EqualTo(300));Assert.That(s.East.KeepFood,Is.EqualTo(36));Assert.That(s.WaystationFood,Is.EqualTo(24));Assert.That(s.West.FreeCapacity,Is.EqualTo(2));Assert.That(s.West.Provisions,Is.EqualTo(30));}
        [Test] public void MinePaysOnceToControllerThenRecapturedOwner()
        {
            var s=Fixture(d=>{d.west.node=6;d.east.node=9;});CrossroadsTests.End(s);Assert.That(s.West.Gold,Is.EqualTo(300));CrossroadsTests.End(s);Assert.That(s.West.Gold,Is.EqualTo(375));
            s.Move(Side.West,4);CrossroadsTests.End(s);s.Move(Side.East,6);CrossroadsTests.End(s);Assert.That(s.West.Gold,Is.EqualTo(375));Assert.That(s.East.Gold,Is.EqualTo(375));
        }
        [Test] public void WaystationFiniteSharedStockTransfersOnlyActualProvisions()
        {var s=Fixture(d=>d.west.node=8);for(int i=0;i<4;i++)Cycle(s);Assert.That(s.West.Provisions,Is.EqualTo(30));Assert.That(s.WaystationFood,Is.Zero);Cycle(s);Assert.That(s.West.Provisions,Is.EqualTo(24));}
        [Test] public void SupplyNeedsPhysicalOwnerAndFood()
        {var s=Fixture(d=>{d.west.node=8;d.owners[2]=(int)Side.East;d.west.keepFood=0;});Assert.That(s.Owner(8),Is.EqualTo(Side.East));Cycle(s);Assert.That(s.Owner(8),Is.EqualTo(Side.West));Assert.That(s.WaystationFood,Is.EqualTo(18));Assert.That(s.East.KeepFood,Is.EqualTo(30));}
        [Test] public void EmptyKeepFoodDoesNotCreateProvisionsOrStopRecovery()
        {var s=Fixture(d=>{d.west.keepFood=0;d.west.formation.members[0].hp=10;});Cycle(s);Assert.That(s.West.Provisions,Is.EqualTo(24));Assert.That(s.West.Formation.Commander.Hp,Is.EqualTo(26));}
        [TestCase(1,26)][TestCase(2,16)] public void FieldVsKeepRecoveryNoStackArmorAndDeadPersist(int node,int hp)
        {var s=Fixture(d=>{d.west.node=node;d.west.formation.members[0].hp=10;d.west.formation.members[0].armor=3;Kill(d.west.formation,1);});Cycle(s);Assert.That(s.West.Formation.Commander.Hp,Is.EqualTo(hp));Assert.That(s.West.Formation.Commander.Armor,Is.EqualTo(3));Assert.That(s.West.Formation.Members[1].Hp,Is.Zero);}
        [Test] public void EnemyKeepOccupationDisablesServiceWithoutOwnershipTransfer()
        {var s=Fixture(d=>{d.west.node=2;d.east.node=1;d.east.formation.members[0].hp=10;});Assert.That(s.KeepAvailable(Side.West),Is.False);Cycle(s);Assert.That(s.East.Formation.Commander.Hp,Is.EqualTo(16));Assert.That(s.East.Provisions,Is.EqualTo(24));Assert.That(s.West.KeepFood,Is.EqualTo(36));}
        [TestCase(UnitProfileId.HumanWarriorTI)][TestCase(UnitProfileId.HumanArcherTI)] public void PaidL1ReplacementHasNewStableIdentityAndDeadIsNotReused(UnitProfileId profile)
        {
            var s=Fixture(d=>Kill(d.west.formation,1));string dead=s.West.Formation.Members[1].CharacterId;
            Assert.That(s.Recruit(Side.West,profile),Is.True);Assert.That(s.West.Gold,Is.EqualTo(200));Assert.That(s.East.Gold,Is.EqualTo(300));Assert.That(s.West.Formation.Members.Count,Is.EqualTo(6));Assert.That(s.HandoffPending,Is.True);
            string hash=s.CaptureSave().checksum;Assert.That(s.Recruit(Side.West,profile),Is.False);Assert.That(s.CaptureSave().checksum,Is.EqualTo(hash));
            s.ContinueHandoff(Side.East);CrossroadsTests.End(s);var c=s.West.Formation.Members.Last();
            Assert.That(c.CharacterId,Is.EqualTo("duel-West-recruit-1").And.Not.EqualTo(dead));Assert.That(c.PersonalLevel,Is.EqualTo(1));Assert.That(c.PersonalXp,Is.Zero);Assert.That(c.Hp,Is.EqualTo(c.Profile.MaxHp));Assert.That(c.Armor,Is.EqualTo(c.Profile.MaxArmor));Assert.That(c.IsCommander,Is.False);
            Assert.That(s.West.Formation.Members[1].Status,Is.EqualTo(PersistentCharacterStatus.Dead));Assert.That(s.West.Formation.LivingMembers.Count(),Is.EqualTo(6));
        }
        [TestCase("capacity")][TestCase("gold")][TestCase("away")][TestCase("commanderless")][TestCase("foreign")][TestCase("authority")]
        public void InvalidRecruitHasNoStateMutation(string kind)
        {
            var s=Fixture(d=>{if(kind!="capacity")Kill(d.west.formation,1);if(kind=="gold")d.west.gold=99;if(kind=="away")d.west.node=2;if(kind=="commanderless")Kill(d.west.formation,0);});
            string hash=s.CaptureSave().checksum;Assert.That(s.Recruit(kind=="authority"?Side.East:Side.West,kind=="foreign"?UnitProfileId.ElfWarriorTI:UnitProfileId.HumanWarriorTI),Is.False);Assert.That(s.CaptureSave().checksum,Is.EqualTo(hash));
        }
        [Test] public void PaidPendingSurvivesIllegalCompletionThenJoinsOnceWhenLegal()
        {
            var s=Fixture(d=>Kill(d.west.formation,1));s.Recruit(Side.West,UnitProfileId.HumanArcherTI);var data=s.CaptureSave();
            data.west.node=2;data.east.node=1;data.checksum=data.ComputeHash();s=data.Restore();s.ContinueHandoff(Side.East);CrossroadsTests.End(s);
            Assert.That(s.West.PendingRecruit.HasValue,Is.True);Assert.That(s.West.Gold,Is.EqualTo(200));Assert.That(s.West.Formation.Members.Count,Is.EqualTo(6));
            CrossroadsTests.End(s);Assert.That(s.Withdraw(Side.East),Is.True);CrossroadsTests.End(s);Assert.That(s.Move(Side.West,1),Is.True);Cycle(s);
            Assert.That(s.West.PendingRecruit,Is.Null);Assert.That(s.West.Formation.Members.Last().CharacterId,Is.EqualTo("duel-West-recruit-1"));Assert.That(s.West.Gold,Is.EqualTo(200));
        }
        [Test] public void SavePendingThenRefreshNoDoubleIncomeChargeSupplyOrRecruit()
        {
            var s=Fixture(d=>{Kill(d.west.formation,1);d.owners[0]=(int)Side.West;d.west.provisions=13;});s.Recruit(Side.West,UnitProfileId.HumanWarriorTI);var loaded=s.CaptureSave().Restore();
            foreach(var w in new[]{s,loaded}){w.ContinueHandoff(Side.East);CrossroadsTests.End(w);}
            Assert.That(s.CaptureSave().checksum,Is.EqualTo(loaded.CaptureSave().checksum));Assert.That(s.West.Gold,Is.EqualTo(275));Assert.That(s.West.Provisions,Is.EqualTo(14));Assert.That(s.West.KeepFood,Is.EqualTo(30));Assert.That(s.West.Formation.Members.Count,Is.EqualTo(7));
        }
        [Test] public void RecruitedIdentityDeploysAndBattleExitPreservesEconomy()
        {
            var s=Fixture(d=>Kill(d.west.formation,1));s.Recruit(Side.West,UnitProfileId.HumanWarriorTI);s.ContinueHandoff(Side.East);CrossroadsTests.End(s);
            s.Move(Side.West,7);CrossroadsTests.End(s);s.Move(Side.East,11);var saved=s.CaptureSave();var loaded=saved.Restore();
            s.Attack(Side.East);loaded.Attack(Side.East);Assert.That(BattleStateHash.Compute(s.Encounter.Battle.State),Is.EqualTo(BattleStateHash.Compute(loaded.Encounter.Battle.State)));
            Assert.That(s.Encounter.Ids.Values,Does.Contain("duel-West-recruit-1"));var result=CrossroadsTests.Evacuate(s,Side.East);s.ResolveBattle(result);
            Assert.That(s.West.Gold,Is.EqualTo(saved.west.gold));Assert.That(s.West.KeepFood,Is.EqualTo(saved.west.keepFood));Assert.That(s.West.Provisions,Is.EqualTo(saved.west.provisions));
            Assert.That(s.CaptureSave().Restore().CaptureSave().checksum,Is.EqualTo(s.CaptureSave().checksum));
        }
        [Test] public void RankFourCapacityCanDeployNineFiguresLegally()
        {
            var s=Fixture(d=>{var c=d.west.formation.members[0];c.commandXp="190";c.commandLevel=20;c.commandRank=3;});
            for(int i=0;i<3;i++){Assert.That(s.Recruit(Side.West,UnitProfileId.HumanWarriorTI),Is.True);s.ContinueHandoff(Side.East);CrossroadsTests.End(s);}
            Assert.That(s.West.Formation.LivingMembers.Count(),Is.EqualTo(9));Assert.That(s.West.FreeCapacity,Is.EqualTo(2));
            s.Move(Side.West,7);CrossroadsTests.End(s);s.Move(Side.East,11);Assert.That(s.Attack(Side.East),Is.True);Assert.That(s.Encounter.Battle.State.Units.Count(u=>u.Side==Side.West),Is.EqualTo(9));
        }
        [Test] public void CorruptPendingIdentityRejectedEvenWithRecomputedChecksum()
        {var s=Fixture(d=>Kill(d.west.formation,1));s.Recruit(Side.West,UnitProfileId.HumanWarriorTI);var d=s.CaptureSave();d.west.pendingId="duplicate";d.checksum=d.ComputeHash();Assert.Throws<System.IO.InvalidDataException>(()=>d.Restore());}
    }
}

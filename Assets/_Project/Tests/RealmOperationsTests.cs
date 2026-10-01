using System;
using System.Linq;
using NUnit.Framework;
using RPG.Core;
namespace RPG.Tests
{
    public class RealmOperationsTests
    {
        internal static CrossroadsScenario World(Side first=Side.West)=>new CrossroadsScenario(first,realm:true);
        internal static void Cycle(CrossroadsScenario w){CityFoundationsTests.Cycle(w);}
        private static DuelForce Arc(CrossroadsScenario w,Side s=Side.West)=>w.Realm.Armies.Single(f=>f.Formation.FormationId=="realm06-"+s+"-army-2");
        [Test] public void InitialNumbersProfilesFoodAndHistoricResearchAreScopedTo06()
        {
            var w=World();var r=w.Realm;Assert.That(w.Graph.Nodes.Count,Is.EqualTo(23));Assert.That(r.Armies.Count,Is.EqualTo(4));Assert.That(r.Armies.All(f=>f.Formation.Members.Count==3&&f.RealmProvisions==30&&RealmOperations.Consumption(f)==5),Is.True);
            Assert.That(w.West.KeepFood,Is.EqualTo(120));Assert.That(w.Foundations.City(Side.West).Development,Is.EqualTo(2));Assert.That(w.Foundations.West.Research.Single(t=>t.Tech==CityTech.ElementalDrills).Provenance[1],Is.EqualTo(6));
            var food=w.Foundations.PreviewSources(w).Single(s=>s.Node==17);Assert.That(food.Output,Is.EqualTo(12));Assert.That(food.Reference,Is.EqualTo(12));Assert.That(food.Regional,Is.EqualTo(10));
            Assert.That(new CrossroadsScenario(combined:true).West.KeepFood,Is.EqualTo(36));Assert.That(new CrossroadsScenario(combined:true).Foundations.City(Side.West).MageTower,Is.Zero);
        }
        [Test] public void SelectionIsNotRefreshAndBothArmiesActWithinOneSideTurn()
        {
            var w=World();var r=w.Realm;Assert.That(r.Move(Side.West,3),Is.True);Assert.That(w.West.Tempo,Is.EqualTo(80));Assert.That(r.Select(Side.West,Arc(w).Formation.FormationId),Is.True);Assert.That(r.Move(Side.West,2),Is.True);Assert.That(Arc(w).Tempo,Is.EqualTo(80));
            Assert.That(w.Refresh,Is.EqualTo(1));Assert.That(w.West.Gold,Is.EqualTo(300));Assert.That(w.West.RealmProvisions,Is.EqualTo(30));Assert.That(w.EndActivation(Side.West),Is.True);Assert.That(w.Refresh,Is.EqualTo(1));w.ContinueHandoff(Side.East);w.EndActivation(Side.East);Assert.That(w.Refresh,Is.EqualTo(2));
        }
        [Test] public void PreviewAndInvalidCommandsDoNotMutateWorldOrPersistentState()
        {
            var w=World();var r=w.Realm;string hash=w.CaptureSave().checksum;
            r.PreviewMove(Side.West,13);r.PreviewEncounter(Side.West,w.East.Formation.FormationId);r.PreviewTransfer(Side.West,"Reserve",w.West.Formation.FormationId,new[]{"missing"});
            Assert.That(r.Move(Side.West,13),Is.False);Assert.That(r.Select(Side.East,w.East.Formation.FormationId),Is.False);Assert.That(w.CaptureSave().checksum,Is.EqualTo(hash));
        }
        [Test] public void RoundTripInitialAndMovedActorTurnAreExactAndContinueDeterministically()
        {
            var w=World();w.Realm.Move(Side.West,3);w.EndActivation(Side.West);var d=w.CaptureSave();var copy=d.Restore();Assert.That(copy.CaptureSave().checksum,Is.EqualTo(d.checksum));
            foreach(var x in new[]{w,copy}){x.ContinueHandoff(Side.East);x.Realm.Move(Side.East,11);x.EndActivation(Side.East);}
            Assert.That(copy.CaptureSave().checksum,Is.EqualTo(w.CaptureSave().checksum));
        }
        [Test] public void FieldHandoverChargesBothAndAntiRelayKeepsSpentCeiling()
        {
            var w=World();var r=w.Realm;var a=Arc(w);var id=a.Formation.Members[2].CharacterId;
            a.Tempo=45;r.LowerCeilings(a);var p=r.PreviewTransfer(Side.West,a.Formation.FormationId,w.West.Formation.FormationId,new[]{id});Assert.That(p.Legal,Is.True,p.Reason);Assert.That(p.ResultTempo,Is.EqualTo(25));
            decimal food=r.Armies.Sum(f=>f.RealmProvisions)+w.West.KeepFood;
            Assert.That(r.Transfer(Side.West,a.Formation.FormationId,w.West.Formation.FormationId,new[]{id}),Is.True);Assert.That(a.Tempo,Is.EqualTo(25));Assert.That(w.West.Tempo,Is.EqualTo(25));Assert.That(r.Cycles[id].Ceiling,Is.EqualTo(25));Assert.That(r.Armies.Sum(f=>f.RealmProvisions)+w.West.KeepFood,Is.EqualTo(food));
            Assert.That(r.Transfer(Side.West,w.West.Formation.FormationId,a.Formation.FormationId,new[]{id}),Is.True);Assert.That(a.Tempo,Is.EqualTo(5));
            Assert.That(r.Transfer(Side.West,a.Formation.FormationId,w.West.Formation.FormationId,new[]{id}),Is.False);
        }
        [Test] public void CityReserveReformKeepsDebtAndIdentityWithoutFreeProvision()
        {
            var w=World();var r=w.Realm;var a=Arc(w);var ids=a.Formation.LivingMembers.Select(c=>c.CharacterId).ToArray();var commander=a.Formation.Commander.CharacterId;
            a.Tempo=-40;r.LowerCeilings(a);Assert.That(r.Disband(Side.West,a.Formation.FormationId),Is.True);Assert.That(r.West.CityStaffDue,Is.EqualTo(2));
            Assert.That(r.FormArmy(Side.West,commander,ids),Is.True);var formed=r.Selected(Side.West);Assert.That(formed.Tempo,Is.EqualTo(-40));Assert.That(formed.RealmProvisions,Is.Zero);Assert.That(formed.Formation.Members.Select(c=>c.CharacterId),Is.EquivalentTo(ids));
            var restored=w.CaptureSave().Restore();Cycle(w);Cycle(restored);Assert.That(w.CaptureSave().checksum,Is.EqualTo(restored.CaptureSave().checksum));Assert.That(formed.Tempo,Is.EqualTo(60));
        }
        [Test] public void CommissionExistingElfAndThirdArmyPreservesHistoryNotFreeSoldiers()
        {
            var w=World();var r=w.Realm;var a=Arc(w);var elf=a.Formation.LivingMembers.Single(c=>c.Profile.IsElf);string id=elf.CharacterId;
            Assert.That(r.Transfer(Side.West,a.Formation.FormationId,"Reserve",new[]{id}),Is.True);Assert.That(r.QueueCommission(Side.West,id),Is.True);Assert.That(w.West.Gold,Is.EqualTo(100));Cycle(w);Assert.That(elf.IsCommander,Is.False);Cycle(w);Assert.That(elf.IsCommander,Is.True);Assert.That(elf.PersonalLevel,Is.EqualTo(3));Assert.That(elf.CommandLevel,Is.EqualTo(1));
            Assert.That(r.FormArmy(Side.West,id,new[]{id}),Is.True);Assert.That(r.Armies.Count(f=>f.Formation.Side==Side.West&&f.Continues),Is.EqualTo(3));Assert.That(r.Selected(Side.West).Formation.Commander,Is.SameAs(elf));Assert.That(r.Selected(Side.West).Tempo,Is.Zero);Assert.That(r.Selected(Side.West).RealmProvisions,Is.Zero);
        }
        [Test] public void ReceiverRaceCapacityIsRealAndTierDoesNotIncreaseLoad()
        {
            var w=World();var r=w.Realm;var human=Arc(w).Formation.Commander;var elf=Arc(w).Formation.LivingMembers.Single(c=>c.Profile.IsElf);
            Assert.That(r.Familiarity(human,elf),Is.EqualTo(CommandFamiliarity.Unfamiliar));r.familiarity[human.CharacterId+":Elf"]=CommandFamiliarity.Trained;Assert.That(r.Load(human,new[]{human,elf}),Is.EqualTo(7));Assert.That(r.Load(elf,new[]{elf,human}),Is.EqualTo(9));
        }
        [Test] public void SharedRecruitQueueTargetsReserveThenPhysicalTransferWithoutAdditionalChannel()
        {
            var w=World();var r=w.Realm;Assert.That(r.Recruit(Side.West,UnitProfileId.HumanWarriorTI,"Reserve"),Is.True);Assert.That(r.Recruit(Side.West,UnitProfileId.HumanArcherTI,Arc(w).Formation.FormationId),Is.False);Assert.That(w.ActiveSide,Is.EqualTo(Side.West));
            Cycle(w);Assert.That(r.West.Reserve.Count,Is.EqualTo(1));var recruit=r.West.Reserve[0];Assert.That(recruit.PersonalLevel,Is.EqualTo(1));Assert.That(r.Transfer(Side.West,"Reserve",Arc(w).Formation.FormationId,new[]{recruit.CharacterId}),Is.True);Assert.That(Arc(w).Formation.Members.Contains(recruit),Is.True);
        }
        [Test] public void CitySupplyProportionateConservesFractionsAndReserveEatsFirst()
        {
            var w=World();var r=w.Realm;w.West.Node=1;w.West.RealmProvisions=Arc(w).RealmProvisions=0;w.West.KeepFood=1;
            foreach(var source in w.Foundations.Locations.Where(l=>!l.IsCity&&!l.IsMinor))source.Functioning=false;
            Cycle(w);Assert.That(w.West.RealmProvisions,Is.EqualTo(.5m));Assert.That(Arc(w).RealmProvisions,Is.EqualTo(.5m));Assert.That(w.West.KeepFood,Is.Zero);
            var copy=w.CaptureSave().Restore();Assert.That(copy.Realm.Armies.Sum(f=>f.RealmProvisions),Is.EqualTo(r.Armies.Sum(f=>f.RealmProvisions)));
        }
        [Test] public void OneHopParticipationChargesEveryAttackerAndDoesNotChain()
        {
            var w=World();var r=w.Realm;w.West.Node=6;Arc(w).Node=3;w.East.Node=7;Arc(w,Side.East).Node=11;
            w.West.Tempo=30;Arc(w).Tempo=0;var p=r.PreviewEncounter(Side.West,w.East.Formation.FormationId);Assert.That(p.Legal,Is.True,p.Reason);Assert.That(p.Attackers.Length,Is.EqualTo(2));Assert.That(p.Defenders.Length,Is.EqualTo(2));
            Assert.That(r.Attack(Side.West,w.East.Formation.FormationId),Is.True);Assert.That(w.West.Tempo,Is.Zero);Assert.That(Arc(w).Tempo,Is.Zero);Assert.That(w.East.Tempo,Is.EqualTo(100));Assert.That(w.RespondToContact(Side.East,false),Is.True);Assert.That(w.Encounter.Battle.State.Units.Count,Is.EqualTo(12));Assert.That(w.Encounter.Battle.State.Units.Select(u=>u.Position).Distinct().Count(),Is.EqualTo(12));
        }
        [Test] public void DeadFirstArmyDoesNotDefeatSideAndCommanderlessPartialTransferRemainsLegal()
        {
            var w=World();var r=w.Realm;var dead=w.West.Formation.Commander;dead.SetBattleResult(new UnitState(new UnitId(1),Side.West,dead.Profile,new GridPosition(1,1),Facing.East,0,dead.Armor,UnitStatus.Dead));w.West.Formation.RefreshCommanderState();
            Assert.That(w.West.Formation.Commanderless,Is.True);string id=w.West.Formation.LivingMembers.First().CharacterId;w.West.RealmProvisions=24; // Controlled depleted-supply case: transfer must conserve the reduced capacity.
            Assert.That(r.Transfer(Side.West,w.West.Formation.FormationId,Arc(w).Formation.FormationId,new[]{id}),Is.True);Assert.That(w.West.Formation.LivingMembers.Count(),Is.EqualTo(1));r.CheckVictory();Assert.That(w.Winner,Is.Null);Assert.That(r.HasMilitary(Side.West),Is.True);
        }
        [Test] public void DuplicatePhysicalIdentityLoadRejectedWithoutTouchingLiveState()
        {
            var w=World();string before=w.CaptureSave().checksum;var d=w.CaptureSave();d.realm.west.reserve=new[]{d.realm.armies[0].units[0]};d.checksum=d.ComputeHash();Assert.Throws<System.IO.InvalidDataException>(()=>d.Restore());Assert.That(w.CaptureSave().checksum,Is.EqualTo(before));
        }
    }
}

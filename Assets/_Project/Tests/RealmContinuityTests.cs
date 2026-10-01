using System;
using System.Linq;
using NUnit.Framework;
using RPG.Core;
namespace RPG.Tests
{
    public class RealmContinuityTests
    {
        private static CrossroadsScenario W()=>RealmOperationsTests.World();
        private static void Cycle(CrossroadsScenario w)=>RealmOperationsTests.Cycle(w);
        private static DuelForce Arc(CrossroadsScenario w,Side s=Side.West)=>w.Realm.Armies.Single(f=>f.Formation.FormationId=="realm06-"+s+"-army-2");
        private static void Contact(CrossroadsScenario w)
        {w.West.Node=6;Arc(w).Node=3;w.East.Node=7;Arc(w,Side.East).Node=11;Assert.That(w.Realm.Attack(Side.West,w.East.Formation.FormationId),Is.True);Assert.That(w.RespondToContact(Side.East,false),Is.True);}
        private static void Kill(PersistentCharacter c)
        {c.SetBattleResult(new UnitState(new UnitId(1),Side.West,c.Profile,new GridPosition(1,1),Facing.East,0,c.Armor,UnitStatus.Dead));}
        [Test] public void CommissionRejectsTierIAlreadyCommissionedAbsentAndWrongAuthority()
        {
            var w=W();var r=w.Realm;var a=Arc(w);Assert.That(r.QueueCommission(Side.West,a.Formation.Commander.CharacterId),Is.False);
            var archer=a.Formation.LivingMembers.Single(c=>c.Profile.IsArcher);Assert.That(r.Transfer(Side.West,a.Formation.FormationId,"Reserve",new[]{archer.CharacterId}),Is.True);
            string before=w.CaptureSave().checksum;Assert.That(r.QueueCommission(Side.West,archer.CharacterId),Is.False);Assert.That(r.QueueCommission(Side.East,archer.CharacterId),Is.False);Assert.That(r.QueueCommission(Side.West,"missing"),Is.False);Assert.That(w.CaptureSave().checksum,Is.EqualTo(before));
        }
        [Test] public void CommissionPauseReloadFeeOnceAndDeathDoesNotSubstituteCandidate()
        {
            var w=W();var r=w.Realm;var elf=Arc(w).Formation.LivingMembers.Single(c=>c.Profile.IsElf);r.Transfer(Side.West,Arc(w).Formation.FormationId,"Reserve",new[]{elf.CharacterId});r.QueueCommission(Side.West,elf.CharacterId);
            w.Foundations.City(Side.West).Functioning=false;Cycle(w);Cycle(w);Assert.That(elf.IsCommander,Is.False);Assert.That(r.West.Commission,Is.Not.Null);
            var copy=w.CaptureSave().Restore();foreach(var world in new[]{w,copy}){world.Foundations.City(Side.West).Functioning=true;Cycle(world);}Assert.That(w.CaptureSave().checksum,Is.EqualTo(copy.CaptureSave().checksum));Assert.That(elf.IsCommander,Is.True);
            var dead=W();var e=Arc(dead).Formation.LivingMembers.Single(c=>c.Profile.IsElf);dead.Realm.Transfer(Side.West,Arc(dead).Formation.FormationId,"Reserve",new[]{e.CharacterId});dead.Realm.QueueCommission(Side.West,e.CharacterId);Kill(e);Cycle(dead);Assert.That(dead.Realm.West.Commission,Is.Null);Assert.That(e.IsCommander,Is.False);Assert.That(dead.West.Gold,Is.EqualTo(130));
        }
        [Test] public void TransferFailsAtomicallyForRemoteOrOverCapacityAndDoesNotDestroyFood()
        {
            var w=W();var r=w.Realm;var a=Arc(w);var id=a.Formation.Members[1].CharacterId;
            a.Node=8;string hash=w.CaptureSave().checksum;Assert.That(r.Transfer(Side.West,a.Formation.FormationId,w.West.Formation.FormationId,new[]{id}),Is.False);Assert.That(w.CaptureSave().checksum,Is.EqualTo(hash));
            a.Node=1;Kill(w.West.Formation.Members[1]);hash=w.CaptureSave().checksum;
            Assert.That(r.PreviewTransfer(Side.West,a.Formation.FormationId,w.West.Formation.FormationId,new[]{id}).Reason,Does.Contain("preserve"));Assert.That(r.Transfer(Side.West,a.Formation.FormationId,w.West.Formation.FormationId,new[]{id}),Is.False);Assert.That(w.CaptureSave().checksum,Is.EqualTo(hash));
        }
        [Test] public void AssignRealOfficerToCommanderlessRetainsDeadHistoryAndBudget()
        {
            var w=W();var r=w.Realm;var a=Arc(w);var officer=a.Formation.Commander;officer.FireballUsed=2;a.Tempo=-30;r.LowerCeilings(a);r.Disband(Side.West,a.Formation.FormationId);
            w.West.Node=1;var old=w.West.Formation.Commander;Kill(old);w.West.Formation.RefreshCommanderState();
            Assert.That(r.AssignCommander(Side.West,w.West.Formation.FormationId,officer.CharacterId),Is.True);Assert.That(w.West.Formation.Commander,Is.SameAs(officer));Assert.That(w.West.Formation.Commanderless,Is.False);Assert.That(old.Status,Is.EqualTo(PersistentCharacterStatus.Dead));Assert.That(w.West.Tempo,Is.EqualTo(-30));Assert.That(officer.FireballUsed,Is.EqualTo(2));
            Assert.That(w.CaptureSave().Restore().West.Formation.Commander.CharacterId,Is.EqualTo(officer.CharacterId));
        }
        [Test] public void PersonalServicesTargetSameIdsConflictAndRespectPresenceAndNoFreeHeal()
        {
            var w=W();var r=w.Realm;var a=Arc(w);w.Foundations.City(Side.West).Forge=true;var c=a.Formation.Members[1];
            c.SetBattleResult(new UnitState(new UnitId(1),Side.West,c.Profile,new GridPosition(1,1),Facing.East,11,0,UnitStatus.Active));
            var q=r.QuoteRepair(Side.West,new[]{c.CharacterId});Assert.That(q.Quotes[c.CharacterId],Is.EqualTo(4));Assert.That(r.Repair(Side.West,new[]{c.CharacterId}),Is.True);Assert.That(r.Repair(Side.West,new[]{c.CharacterId}),Is.False);Assert.That(r.Transfer(Side.West,a.Formation.FormationId,"Reserve",new[]{c.CharacterId}),Is.False);
            var copy=w.CaptureSave().Restore();Cycle(w);Cycle(copy);Assert.That(c.Armor,Is.Zero);Cycle(w);Cycle(copy);Assert.That(c.Armor,Is.EqualTo(4));Assert.That(w.CaptureSave().checksum,Is.EqualTo(copy.CaptureSave().checksum));
            var recruit=new PersistentCharacter("realm06-West-test-mage",UnitProfile.FireMageTI,personalXp:20,hp:7);r.West.Reserve.Add(recruit);r.cycles[recruit.CharacterId]=new RealmUnitCycle();Assert.That(r.Train(Side.West,recruit.CharacterId,true),Is.True);Cycle(w);int before=recruit.Hp;int recovery=(recruit.FieldRecoveryRemainderHundredths+32*40)/100;Cycle(w);Assert.That(recruit.Profile.Id,Is.EqualTo(UnitProfileId.FireMageTII));Assert.That(recruit.CharacterId,Is.EqualTo("realm06-West-test-mage"));Assert.That(recruit.Hp,Is.EqualTo(Math.Min(32,before+recovery))); // Training itself gave no heal; ordinary Keep Refresh still runs.
        }
        [Test] public void RetiredStaffStillConsumesAndReserveDeprivationPersistsWithoutInventedHpPenalty()
        {
            var w=W();var r=w.Realm;var a=Arc(w);r.Disband(Side.West,a.Formation.FormationId);w.West.KeepFood=0;foreach(var l in w.Foundations.Locations.Where(l=>!l.IsCity&&!l.IsMinor))l.Functioning=false;
            var c=r.West.Reserve[0];c.SetBattleResult(new UnitState(new UnitId(1),Side.West,c.Profile,new GridPosition(1,1),Facing.East,7,0,UnitStatus.Active));Cycle(w);Assert.That(c.Hp,Is.EqualTo(7));Assert.That(r.Cycles[c.CharacterId].Deprivation,Is.EqualTo(1));Assert.That(r.West.CityStaffDue,Is.Zero);Assert.That(w.CaptureSave().Restore().Realm.Cycles[c.CharacterId].Deprivation,Is.EqualTo(1));
        }
        [Test] public void NonrecursiveParticipationAndNegativeAttackerExcludedDefenderIncluded()
        {
            var w=W();w.West.Node=6;Arc(w).Node=4;w.East.Node=7;Arc(w,Side.East).Node=11;Arc(w,Side.East).Tempo=-40;
            var p=w.Realm.PreviewEncounter(Side.West,w.East.Formation.FormationId);Assert.That(p.Legal,Is.True);Assert.That(p.Attackers.Length,Is.EqualTo(1));Assert.That(p.Defenders.Length,Is.EqualTo(2));
            Arc(w).Node=3;Arc(w).Tempo=-1;Assert.That(w.Realm.PreviewEncounter(Side.West,w.East.Formation.FormationId).Attackers.Length,Is.EqualTo(1));
        }
        [Test] public void EighteenVsEighteenAndThreeSameDirectionFormationsFullyDeploy()
        {
            var w=W();foreach(var f in w.Realm.Armies) {
                string id=f.Formation.FormationId;var chars=Enumerable.Range(0,9).Select(i=>new PersistentCharacter(id+"-stress-"+i,UnitProfile.HumanWarriorTI,i==0,commandXp:i==0?190:0,commandLevel:i==0?20:1)).ToArray();f.Formation=new PersistentFormation(id,f.Formation.Side,chars,chars[0].CharacterId,true);foreach(var c in chars)w.Realm.cycles[c.CharacterId]=new RealmUnitCycle();
            }
            w.West.Node=6;Arc(w).Node=3;w.East.Node=7;Arc(w,Side.East).Node=11;var p=w.Realm.PreviewEncounter(Side.West,w.East.Formation.FormationId);Assert.That(p.Legal,Is.True,p.Reason);Assert.That(p.Deployments.Length,Is.EqualTo(36));Assert.That(p.Deployments.Select(d=>d.Position).Distinct().Count(),Is.EqualTo(36));
            var shared=Enumerable.Range(0,3).Select(n=>new DuelForce(Side.West){Node=1,Formation=new PersistentFormation("stress-"+n,Side.West,Enumerable.Range(0,3).Select(i=>new PersistentCharacter("stress-"+n+"-"+i,UnitProfile.HumanWarriorTI,i==0)))}).ToArray();
            w.East.Node=2;var plan=DuelEncounter.DeploymentPlan(w.Graph,shared[0],w.East,shared.Concat(new[]{w.East}),SizeExperimentFixture.Board(SizeExperimentMap.Field_23x17_Full_9v9));Assert.That(plan.Length,Is.EqualTo(18));Assert.That(plan.Select(d=>d.Position).Distinct().Count(),Is.EqualTo(18));Assert.That(plan.Where(d=>d.CharacterId.StartsWith("stress-")).All(d=>d.OwnRetreatEdge==RetreatEdge.West),Is.True);

        }
        [Test] public void OneArmyWithdrawsWhileAllyContinuesReplayPerArmyDebtNoSubstituteLead()
        {
            var w=W();Contact(w);var encounter=w.Encounter;var lead=w.West;var partner=Arc(w);var journal=new BattleJournal(encounter.Battle.State,"06-joint","test");bool observed=false;
            for(int i=0;i<1200&&!journal.State.Outcome.IsEnded;i++) {
                var leadUnits=journal.State.Units.Where(u=>lead.Formation.Members.Any(c=>c.CharacterId==encounter.Ids[u.Id])).ToArray();bool leadGone=leadUnits.All(u=>u.Status==UnitStatus.Escaped);
                if(leadGone){observed=true;Assert.That(journal.State.Units.Any(u=>u.Side==Side.West&&u.IsActive),Is.True);}
                var u=journal.State.FindUnit(journal.State.CurrentUnitId.Value);bool shouldEscape=leadGone?u.Side==Side.East:leadUnits.Contains(u);BattleCommand command=new EndActivationCommand(u.Id);
                if(shouldEscape&&u.MovementRemaining>0){var board=journal.State.Battlefield;var paths=Enumerable.Range(0,board.Columns*board.Rows).Select(k=>new GridPosition(k%board.Columns,k/board.Columns)).Where(p=>board.IsRetreatZone(u,p)).Select(p=>Pathfinder.FindPath(journal.State,u.Id,p)).Where(p=>p.Found&&p.Cost>0).OrderBy(p=>p.Cost).ToArray();if(paths.Length>0)command=new MoveCommand(u.Id,paths[0].Steps);}
                Assert.That(journal.Apply(command).IsApplied,Is.True);
            }
            Assert.That(observed,Is.True);Assert.That(journal.State.Outcome.IsEnded,Is.True);Assert.That(ReplayVerification.Verify(journal.Header,journal.Records,journal.Footer()).Matches,Is.True);
            Assert.That(w.ResolveBattle(journal.State),Is.True);Assert.That(w.ResolveBattle(journal.State),Is.False);Assert.That(lead.Tempo,Is.EqualTo(10));Assert.That(partner.Tempo,Is.EqualTo(50));Assert.That(partner.Node,Is.EqualTo(3));Assert.That(w.Refresh,Is.EqualTo(1));Assert.That(w.Realm.LastMessage,Does.Contain("fully withdrew"));Assert.That(w.CaptureSave().Restore().CaptureSave().checksum,Is.EqualTo(w.CaptureSave().checksum));
            Assert.That(lead.Formation.Commander.CommandXp,Is.LessThan(partner.Formation.Commander.CommandXp));
            Assert.That(w.Realm.LastBattle.unitIds.Length,Is.EqualTo(12));Assert.That(w.Realm.LastBattle.advanced,Is.Empty);Assert.That(w.Realm.LastBattle.withdrew.Count(b=>b),Is.EqualTo(3));
            var audit=w.CaptureSave();audit.realm.lastBattle.unitIds[0]="changed snapshot only";Assert.That(w.Realm.LastBattle.unitIds[0],Is.Not.EqualTo("changed snapshot only"));
        }
        [Test] public void PrebattleWithdrawalIsPerTargetAndSaveBlockedDuringCommitment()
        {
            var w=W();w.West.Node=6;Arc(w).Node=3;w.East.Node=7;Arc(w,Side.East).Node=11;w.East.Tempo=0;w.Realm.LowerCeilings(w.East);
            Assert.That(w.Realm.Attack(Side.West,w.East.Formation.FormationId),Is.True);Assert.That(w.CanSave,Is.False);Assert.That(w.RespondToContact(Side.East,true),Is.True);Assert.That(w.East.Tempo,Is.EqualTo(-40));Assert.That(Arc(w,Side.East).Tempo,Is.EqualTo(100));Assert.That(w.CanSave,Is.True);Assert.That(w.Refresh,Is.EqualTo(1));
        }
        [TestCase(Side.West)][TestCase(Side.East)] public void PressureLegalEndingOncePerObjectiveAndMirroredStartingSide(Side first)
        {
            var w=RealmOperationsTests.World(first);w.Realm.Select(first,Arc(w,first).Formation.FormationId);var winning=w.Force(first);winning.Node=6;Arc(w,first).Node=8;
            for(int i=0;i<12&&!w.Winner.HasValue;i++)Cycle(w);Assert.That(w.Winner,Is.EqualTo(first));Assert.That(winning.Pressure,Is.EqualTo(24));Assert.That(w.Refresh,Is.EqualTo(13));
        }
        [Test] public void DestroyedSelectedArmyNormalizesToSurvivingArmyAfterBattle()
        {
            var w=W();Contact(w);var r=w.Encounter.Battle.State.Copy();
            foreach(var u in r.Units.Where(u=>u.Side==Side.East)){u.Hp=0;u.Status=UnitStatus.Dead;}
            // Isolate one surviving nonparticipant after the battle; other tests cover real command outcomes.
            var extra=new DuelForce(Side.East){Node=13,Formation=new PersistentFormation("realm06-East-army-3",Side.East,new[]{new PersistentCharacter("realm06-East-extra",UnitProfile.HumanWarriorTI,true)},"realm06-East-extra",true)};
            w.Realm.armies.Add(extra);w.Realm.cycles["realm06-East-extra"]=new RealmUnitCycle();w.Realm.staffDue[extra.Formation.FormationId]=2;
            w.Realm.East.Selected=w.East.Formation.FormationId;r.Outcome=new BattleOutcome(Side.West,Side.East,BattleEndReason.Eliminated);
            Assert.That(w.ResolveBattle(r),Is.True);Assert.That(w.Realm.Selected(Side.East),Is.SameAs(extra));Assert.That(w.Winner,Is.Null);
        }
        [Test] public void FullyUnsuppliedReserveSuppressesRecoveryButPartialFoodDoesNotInventNewPenalty()
        {
            var w=W();var r=w.Realm;var a=Arc(w);var c=a.Formation.Members[1];r.Transfer(Side.West,a.Formation.FormationId,"Reserve",new[]{c.CharacterId});
            c.SetBattleResult(new UnitState(new UnitId(1),Side.West,c.Profile,new GridPosition(1,1),Facing.East,10,0,UnitStatus.Active));
            foreach(var l in w.Foundations.Locations.Where(l=>!l.IsCity&&!l.IsMinor))l.Functioning=false;
            w.West.KeepFood=.5m;Cycle(w);Assert.That(c.Hp,Is.GreaterThan(10));int hp=c.Hp;w.West.KeepFood=0;Cycle(w);Assert.That(c.Hp,Is.EqualTo(hp));Assert.That(r.Cycles[c.CharacterId].Deprivation,Is.EqualTo(1));
        }
    }
}

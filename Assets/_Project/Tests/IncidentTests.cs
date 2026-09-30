using System;
using System.Linq;
using NUnit.Framework;
using RPG.Core;

namespace RPG.Tests
{
    public class IncidentTests
    {
        public static CrossroadsScenario Fixture(Action<CrossroadsSaveData> edit=null)
        {var d=new CrossroadsScenario(incident:true).CaptureSave();edit?.Invoke(d);d.checksum=d.ComputeHash();return d.Restore();}
        public static void HumanEnd(CrossroadsScenario s){Assert.That(s.EndActivation(s.ActiveSide),Is.True);if(s.HandoffPending)Assert.That(s.ContinueHandoff(s.ActiveSide),Is.True);}
        public static void Cycle(CrossroadsScenario s)
        {HumanEnd(s);HumanEnd(s);for(int n=0;n<4&&s.Incident.WorldPhase;n++)Assert.That(s.ContinueWorldPhase(),Is.True);if(s.HandoffPending)s.ContinueHandoff(s.ActiveSide);}
        public static BattleState Evacuate(CrossroadsScenario s,Side side)
        {
            var j=new BattleJournal(s.Encounter.Battle.State,"incident","test");
            for(int n=0;n<1500&&!j.State.Outcome.IsEnded;n++)
            {
                var u=j.State.FindUnit(j.State.CurrentUnitId.Value);BattleCommand c=new EndActivationCommand(u.Id);
                if(u.Side==side&&u.MovementRemaining>0)
                {
                    var b=j.State.Battlefield;var paths=Enumerable.Range(0,b.Columns*b.Rows).Select(k=>new GridPosition(k%b.Columns,k/b.Columns)).Where(p=>b.IsRetreatZone(u,p)).Select(p=>Pathfinder.FindPath(j.State,u.Id,p)).Where(p=>p.Found&&p.Cost>0).OrderBy(p=>p.Cost).ToArray();
                    if(paths.Length>0)c=new MoveCommand(u.Id,paths[0].Steps);
                }
                Assert.That(j.Apply(c).IsApplied,Is.True);
            }
            Assert.That(j.State.Outcome.IsEnded,Is.True);Assert.That(ReplayVerification.Verify(j.Header,j.Records,j.Footer()).Matches,Is.True);return j.State;
        }
        public static void Defend(CrossroadsScenario s){if(s.PendingContact!=null)Assert.That(s.RespondToContact(s.PendingContact.Target.Formation.Side,false),Is.True);}
        static void Actor(CrossroadsSaveData d,int index,int node,RaiderState state=RaiderState.AdvanceToTarget,int tempo=100)
        {var r=d.incident.raiders[index];r.state=(int)state;r.node=node;r.tempo=tempo;r.armedRefresh=state==RaiderState.RaidArmed?d.refresh:0;}
        [Test] public void SeparateSymmetricGraphAndOriginalRules()
        {
            var s=Fixture();Assert.That(s.Graph.Nodes.Count,Is.EqualTo(16));Assert.That(s.Graph.Edges.Count,Is.EqualTo(19));Assert.That(s.TargetPressure,Is.EqualTo(16));Assert.That(new CrossroadsScenario(economy:true).TargetPressure,Is.EqualTo(8));
            Assert.That(s.Graph.PathCost(s.Graph.Path(1,16)),Is.EqualTo(s.Graph.PathCost(s.Graph.Path(13,16))));Assert.That(s.Incident.Contract,Is.EqualTo(HoldState.Open));Assert.That(s.Incident.Raiders.SelectMany(r=>r.Force.Formation.Members).Select(c=>c.CharacterId).Distinct().Count(),Is.EqualTo(6));
        }
        [Test] public void BothHumansThenStableActorCursorThenSingleEconomyTick()
        {
            var s=Fixture();HumanEnd(s);Assert.That(s.Refresh,Is.EqualTo(1));HumanEnd(s);Assert.That(s.Incident.WorldPhase,Is.True);Assert.That(s.Refresh,Is.EqualTo(1));Assert.That(s.EndActivation(s.ActiveSide),Is.False);Assert.That(s.ContinueWorldPhase(),Is.True);Assert.That(s.Refresh,Is.EqualTo(2));Assert.That(s.Incident.Raiders[0].Force.Node,Is.EqualTo(16));Assert.That(s.Incident.Raiders[0].State,Is.EqualTo(RaiderState.AdvanceToTarget));
        }
        [Test] public void IgnoreIncidentArmsThenRavagesKeepsFoodAndMatchContinues()
        {
            var s=Fixture();Cycle(s);Cycle(s);Assert.That(s.Incident.Raiders[0].State,Is.EqualTo(RaiderState.RaidArmed));Assert.That(s.Incident.Ravaged,Is.False);Assert.That(s.Refresh,Is.EqualTo(3));
            Cycle(s);Assert.That(s.Incident.Ravaged,Is.True);Assert.That(s.WaystationFood,Is.EqualTo(24));Assert.That(s.Incident.Contract,Is.EqualTo(HoldState.Failed));Assert.That(s.Winner,Is.Null);
            Assert.That(s.Incident.Phase,Is.EqualTo(PortalPhase.Whiteout));Assert.That(s.Incident.WhiteoutRemaining,Is.EqualTo(2));
            Cycle(s);Assert.That(s.Incident.WhiteoutRemaining,Is.EqualTo(1));Cycle(s);Assert.That(s.Incident.Phase,Is.EqualTo(PortalPhase.Closure));Cycle(s);Assert.That(s.Incident.Phase,Is.EqualTo(PortalPhase.Recovery));Cycle(s);Assert.That(s.Incident.Phase,Is.EqualTo(PortalPhase.Stable));
            Assert.That(s.Incident.Raiders.All(r=>r.State==RaiderState.ExitedThroughPortal),Is.True);Assert.That(s.Incident.Ravaged,Is.True);Cycle(s);Assert.That(s.Incident.Raiders.All(r=>r.State==RaiderState.ExitedThroughPortal),Is.True);
        }
        [Test] public void OccupiedAnchorDefersOldestThenWhiteoutNeverSpawnsUnreleased()
        {
            var s=Fixture(d=>d.west.node=16);Cycle(s);Assert.That(s.Incident.Raiders[0].State,Is.EqualTo(RaiderState.PendingEntry));Cycle(s);Assert.That(s.Incident.Raiders.All(r=>r.State==RaiderState.PendingEntry),Is.True);
            Assert.That(s.Move(Side.West,15),Is.True);Cycle(s);Assert.That(s.Incident.Raiders.All(r=>r.State==RaiderState.UnreleasedForThisIncident),Is.True);Assert.That(s.Incident.Raiders.All(r=>r.Force.Provisions==30),Is.True);
        }
        [Test] public void DelayedEntryNeverBursts()
        {
            var s=Fixture(d=>d.west.node=16);Cycle(s);s.Move(Side.West,1); // not enough Tempo, remains blocking
            Assert.That(s.Move(Side.West,14),Is.True);Cycle(s);Assert.That(s.Incident.Raiders[0].OnMap,Is.True);Assert.That(s.Incident.Raiders[1].State,Is.EqualTo(RaiderState.PendingEntry));
        }
        [Test] public void PreviewInvalidCommandsAndSaveDoNotMutate()
        {
            var s=Fixture();var hash=s.CaptureSave().checksum;s.PreviewMove(Side.West,16);s.PreviewMove(Side.West,8);Assert.That(s.Move(Side.East,8),Is.False);Assert.That(s.AttackNode(Side.West,16),Is.False);Assert.That(s.ContinueWorldPhase(),Is.False);Assert.That(s.CaptureSave().checksum,Is.EqualTo(hash));
        }
        [TestCase(Side.West)][TestCase(Side.East)] public void HumanRaiderMappingAndPersistentEscapeNoHeal(Side human)
        {
            var s=Fixture(d=>{(human==Side.West?d.west:d.east).node=14;d.startingSide=d.activeSide=(int)human;Actor(d,0,15);});var f=s.Force(human);var ids=f.Formation.Members.Select(c=>c.CharacterId).ToArray();
            Assert.That(s.AttackNode(human,15),Is.True);Assert.That(s.Encounter.HasRaiders,Is.True);Assert.That(s.Encounter.AiSide,Is.EqualTo(Side.East));Assert.That(s.Encounter.Participants.Count,Is.EqualTo(2));
            var result=Evacuate(s,Side.East);Assert.That(s.ResolveBattle(result),Is.True);Assert.That(s.ResolveBattle(result),Is.False);Assert.That(f.Node,Is.EqualTo(15));Assert.That(f.Formation.Members.Select(c=>c.CharacterId),Is.EqualTo(ids));Assert.That(s.Refresh,Is.EqualTo(1));Assert.That(s.Incident.Raiders[0].Force.Formation.Members.All(c=>c.Status==PersistentCharacterStatus.EscapedSafe),Is.True);
            Assert.That(s.CaptureSave().Restore().CaptureSave().checksum,Is.EqualTo(s.CaptureSave().checksum));
        }
        [Test] public void AlliedDefenseJoinsEvenNegativeButThirdHumanExcluded()
        {
            var s=Fixture(d=>{d.west.node=8;d.east.node=5;Actor(d,0,14);Actor(d,1,15,tempo:-40);});s.AttackNode(Side.West,14);
            Assert.That(s.Encounter.Participants.Count,Is.EqualTo(3));Assert.That(s.Encounter.Battle.State.Units.Count,Is.EqualTo(12));Assert.That(s.East.Tempo,Is.EqualTo(100));Assert.That(s.Encounter.Participants.Contains(s.East),Is.False);
            Assert.That(s.Encounter.Battle.State.Units.Where(u=>s.Encounter.Ids[u.Id].Contains("raider-B")).All(u=>u.OwnRetreatEdge==RetreatEdge.Unavailable),Is.True);
            Assert.That(s.Encounter.Battle.State.Units.Select(u=>u.Position).Distinct().Count(),Is.EqualTo(12));
        }
        [TestCase(100,50,3)][TestCase(0,0,3)][TestCase(-1,-1,2)] public void RaiderOffensePaysEachEligibleArmy(int tempo,int after,int participants)
        {
            var s=Fixture(d=>{d.west.node=14;Actor(d,0,15);Actor(d,1,8,tempo:tempo);});HumanEnd(s);HumanEnd(s);s.ContinueWorldPhase();Defend(s);
            Assert.That(s.Encounter.Participants.Count,Is.EqualTo(participants));Assert.That(s.Incident.Raiders[0].Force.Tempo,Is.EqualTo(50));Assert.That(s.Incident.Raiders[1].Force.Tempo,Is.EqualTo(after));Assert.That(s.Encounter.AiSide,Is.EqualTo(Side.West));
        }
        [Test] public void NoRecursiveParticipantsAndInactiveHumanControlsNpcDefense()
        {
            var s=Fixture(d=>{d.west.node=8;Actor(d,0,14);Actor(d,1,15);});HumanEnd(s);HumanEnd(s);s.ContinueWorldPhase();Defend(s);Assert.That(s.ActiveSide,Is.EqualTo(Side.East));Assert.That(s.Encounter.Target,Is.SameAs(s.West));Assert.That(s.Encounter.Participants.Count,Is.EqualTo(2));Assert.That(s.Encounter.TacticalSides[s.West.Formation.FormationId],Is.EqualTo(Side.East));
        }
        [Test] public void WorldBattleResultCursorSaveContinuesWithoutSecondTickOrApplication()
        {
            var s=Fixture(d=>{d.west.node=8;Actor(d,0,14);Actor(d,1,15);});HumanEnd(s);HumanEnd(s);s.ContinueWorldPhase();Defend(s);var result=Evacuate(s,Side.West);s.ResolveBattle(result);
            Assert.That(s.Incident.Cursor,Is.EqualTo(1));Assert.That(s.Incident.WorldPhase,Is.True);Assert.That(s.Refresh,Is.EqualTo(1));var copy=s.CaptureSave().Restore();Assert.That(copy.ResolveBattle(result),Is.False);
            foreach(var x in new[]{s,copy}){Assert.That(x.ContinueWorldPhase(),Is.True);Defend(x);if(x.Encounter!=null)x.ResolveBattle(Evacuate(x,Side.West));Assert.That(x.ContinueWorldPhase(),Is.True);}
            Assert.That(copy.CaptureSave().checksum,Is.EqualTo(s.CaptureSave().checksum));
        }
        [Test] public void SuccessfulHoldFullCyclesPaysOnceNotAtArrival()
        {
            var s=Fixture(d=>{d.west.node=8;d.owners[2]=(int)Side.West;d.east.node=16;});Cycle(s);Assert.That(s.Incident.HoldCycles,Is.Zero);Cycle(s);Assert.That(s.Incident.HoldCycles,Is.EqualTo(1));Cycle(s);Assert.That(s.Incident.Contract,Is.EqualTo(HoldState.Completed));Assert.That(s.West.Gold,Is.EqualTo(450));
            s=s.CaptureSave().Restore();Cycle(s);Assert.That(s.West.Gold,Is.EqualTo(450));Assert.That(s.West.Pressure,Is.EqualTo(4));
        }
        [Test] public void LeaveReturnBreaksContinuityAndOpponentDoesNotInherit()
        {
            var s=Fixture(d=>{d.west.node=8;d.owners[2]=(int)Side.West;d.east.node=16;});Cycle(s);Cycle(s);s.Move(Side.West,5);s.Move(Side.West,8);Cycle(s);Assert.That(s.Incident.HoldCycles,Is.Zero);Assert.That(s.West.Gold,Is.EqualTo(300));
        }
        [Test] public void ArrivalMidCycleDoesNotCountAndClosureExpires()
        {
            var s=Fixture(d=>{d.east.node=16;});Cycle(s);s.Move(Side.West,8);Cycle(s);Assert.That(s.Incident.HoldCycles,Is.Zero);s.Move(Side.West,5);while(s.Refresh<6)Cycle(s);Assert.That(s.Incident.Contract,Is.EqualTo(HoldState.Expired));
        }
        [Test] public void CommanderlessHolderQualifiesWithoutRecruitment()
        {
            var s=Fixture(d=>{d.west.node=8;d.owners[2]=(int)Side.West;d.east.node=16;d.west.formation.members[0].hp=0;d.west.formation.members[0].status=(int)PersistentCharacterStatus.Dead;d.west.formation.commanderless=d.west.formation.rosterLocked=true;});
            Cycle(s);Cycle(s);Cycle(s);Assert.That(s.West.Formation.Commanderless,Is.True);Assert.That(s.West.Gold,Is.EqualTo(450));Assert.That(s.West.Consumption,Is.EqualTo(5));
        }
        [Test] public void ClosedAnchorRemnantNeverVanishesAndNegativeTempoCannotAttack()
        {
            var s=Fixture(d=>{d.refresh=6;d.incident.phase=(int)PortalPhase.Closure;d.incident.contract=(int)HoldState.Expired;Actor(d,0,15,RaiderState.StrandedReturn,-40);d.east.node=16;});HumanEnd(s);HumanEnd(s);s.ContinueWorldPhase();Assert.That(s.Encounter,Is.Null);Assert.That(s.Incident.Raiders[0].OnMap,Is.True);Assert.That(s.Incident.Raiders[0].Force.Node,Is.EqualTo(15));
        }
        [Test] public void StrandedArrivalGuardsClosedAnchorThroughStable()
        {
            var s=Fixture(d=>{d.refresh=6;d.incident.phase=(int)PortalPhase.Closure;d.incident.contract=(int)HoldState.Expired;Actor(d,0,15,RaiderState.StrandedReturn);});Cycle(s);Assert.That(s.Incident.Raiders[0].State,Is.EqualTo(RaiderState.GuardingClosedAnchor));Cycle(s);Cycle(s);Assert.That(s.Incident.Phase,Is.EqualTo(PortalPhase.Stable));Assert.That(s.Incident.Raiders[0].OnMap,Is.True);
        }
        [TestCase(Side.West)][TestCase(Side.East)] public void ControlHasNoIncidentAndLegalPressureVictory(Side first)
        {
            var s=new CrossroadsScenario(first,incident:true,incidentsEnabled:false);s.Move(first,6);HumanEnd(s);s.Move(s.ActiveSide,8);HumanEnd(s);s.ContinueWorldPhase();s.ContinueHandoff(first);s.Move(first,7);
            for(int n=0;n<20&&!s.Winner.HasValue;n++)Cycle(s);
            Assert.That(s.Winner,Is.EqualTo(first));Assert.That(s.Incident.Phase,Is.EqualTo(PortalPhase.Stable));Assert.That(s.Incident.Contract,Is.EqualTo(HoldState.Disabled));Assert.That(s.Incident.Raiders.All(r=>r.State==RaiderState.Staged),Is.True);
        }
        [Test] public void EveryLifecycleStableBoundaryRoundTripsAndContinuesIdentically()
        {
            var a=Fixture();for(int r=0;r<10;r++){var b=a.CaptureSave().Restore();Cycle(a);Cycle(b);Assert.That(a.CaptureSave().checksum,Is.EqualTo(b.CaptureSave().checksum));}
        }
        [Test] public void PreBattleWithdrawalPaysOnceDoesNotGenerateBattleAndBlocksSave()
        {
            var s=Fixture(d=>{d.west.node=8;Actor(d,0,14);});HumanEnd(s);HumanEnd(s);s.ContinueWorldPhase();
            Assert.That(s.PendingContact,Is.Not.Null);Assert.That(s.Encounter,Is.Null);Assert.That(s.CanSave,Is.False);Assert.That(s.ContinueWorldPhase(),Is.False);
            Assert.That(s.RespondToContact(Side.East,true),Is.False);Assert.That(s.RespondToContact(Side.West,true),Is.True);Assert.That(s.West.Tempo,Is.EqualTo(60));Assert.That(s.West.Node,Is.Not.EqualTo(8));Assert.That(s.CanSave,Is.True);Assert.That(s.Incident.Cursor,Is.EqualTo(1));
        }
        [Test] public void NoFreeRetreatDestinationStaysPaysDebtAndNoLeadStacking()
        {
            var s=Fixture(d=>{d.west.node=16;d.west.tempo=0;Actor(d,0,15);Actor(d,1,14);});HumanEnd(s);HumanEnd(s);
            // A's objective path does not chase this off-path human; human can withdraw from legal contact.
            var d=s.CaptureSave();d.completed=0;d.activeSide=(int)Side.West;d.incident.worldPhase=false;d.incident.cursor=0;d.incident.actorOrder=Array.Empty<string>();d.checksum=d.ComputeHash();s=d.Restore();
            Assert.That(s.WithdrawFromContact(Side.West),Is.True);Assert.That(s.West.Node,Is.EqualTo(16));Assert.That(s.West.Tempo,Is.EqualTo(-40));
        }
        [Test] public void TwoCommanderSharesUseArmyDenominatorAndNoInterBattleRecovery()
        {
            var s=Fixture(d=>{d.west.node=8;d.west.formation.members[1].hp=17;d.west.formation.members[1].armor=5;Actor(d,0,14);Actor(d,1,15);});s.AttackNode(Side.West,14);
            var result=s.Encounter.Battle.State.Copy();foreach(var u in result.Units.Where(u=>u.Side==Side.West)){u.Status=UnitStatus.Escaped;}
            result.Outcome=new BattleOutcome(Side.East,Side.West,BattleEndReason.Eliminated);
            s.ResolveBattle(result);var a=s.Incident.Raiders[0].Force.Formation.Commander;var b=s.Incident.Raiders[1].Force.Formation.Commander;
            Assert.That(a.CommandXp,Is.GreaterThan(0));Assert.That(a.CommandXp,Is.EqualTo(b.CommandXp));Assert.That(a.CommandXp,Is.EqualTo(a.PersonalXp*6m/14m).Within(.000001m));
            Assert.That(s.West.Formation.Members[1].Hp,Is.EqualTo(17));Assert.That(s.West.Formation.Members[1].Armor,Is.EqualTo(5));
            var snap=s.CaptureSave();snap.west.node=8;snap.west.tempo=100;snap.checksum=snap.ComputeHash();s=snap.Restore();s.AttackNode(Side.West,14);
            Assert.That(s.Encounter.Battle.State.Units.Single(u=>s.Encounter.Ids[u.Id]=="duel-West-2").Hp,Is.EqualTo(17));
        }
        [Test] public void DefensiveHoldUnderTwoRealRaiderAttacksRetainsContinuity()
        {
            var s=Fixture(d=>{d.west.node=8;d.owners[2]=(int)Side.West;});Cycle(s);
            for(int r=2;r<=3;r++)
            {
                HumanEnd(s);HumanEnd(s);while(s.Incident.WorldPhase){s.ContinueWorldPhase();Defend(s);if(s.Encounter!=null)s.ResolveBattle(Evacuate(s,Side.West));}
                if(s.HandoffPending)s.ContinueHandoff(s.ActiveSide);
            }
            Assert.That(s.Incident.Contract,Is.EqualTo(HoldState.Completed));Assert.That(s.West.Gold,Is.EqualTo(450));Assert.That(s.West.Node,Is.EqualTo(8));
        }
        [Test] public void RavagedObjectiveStillScoresButNeverSupplies()
        {
            var s=Fixture(d=>{d.west.node=8;d.west.provisions=20;d.owners[2]=(int)Side.West;d.incident.ravaged=true;d.incident.contract=(int)HoldState.Failed;});Cycle(s);
            Assert.That(s.West.Pressure,Is.EqualTo(1));Assert.That(s.West.Provisions,Is.EqualTo(14));Assert.That(s.WaystationFood,Is.EqualTo(24));
        }
        [Test] public void EliminationFinishesEconomyOnceWithoutReleasingNextCycleActor()
        {
            var s=Fixture(d=>{d.west.node=12;d.east.node=13;d.east.provisions=20;});s.AttackNode(Side.West,13);Defend(s);
            var result=s.Encounter.Battle.State.Copy();foreach(var u in result.Units.Where(u=>u.Side==Side.East)){u.Status=UnitStatus.Dead;u.Hp=0;u.Armor=0;}
            result.Outcome=new BattleOutcome(Side.West,Side.East,BattleEndReason.Eliminated);s.ResolveBattle(result);
            Assert.That(s.Winner,Is.EqualTo(Side.West));Assert.That(s.Refresh,Is.EqualTo(1));Assert.That(s.West.Provisions,Is.EqualTo(24));Assert.That(s.East.Provisions,Is.EqualTo(20));Assert.That(s.East.KeepFood,Is.EqualTo(36));Assert.That(s.Incident.Raiders.All(r=>r.State==RaiderState.Staged),Is.True);
            Assert.That(s.CaptureSave().Restore().CaptureSave().checksum,Is.EqualTo(s.CaptureSave().checksum));Assert.That(s.ContinueWorldPhase(),Is.False);
        }
        [Test] public void RealCommandIncidentBattleAttritionAndLegalFullMatch()
        {
            var s=Fixture();s.Move(Side.West,8);HumanEnd(s);s.Move(Side.East,6);HumanEnd(s);s.ContinueWorldPhase();s.ContinueHandoff(Side.West);
            int battles=0;
            for(int cycle=0;cycle<25&&!s.Winner.HasValue;cycle++)
            {
                if(cycle>=2&&s.CanAct(Side.West)&&s.West.Node==8)s.Move(Side.West,7);
                HumanEnd(s);HumanEnd(s);
                while(s.Incident.WorldPhase&&!s.Winner.HasValue)
                {
                    s.ContinueWorldPhase();Defend(s);
                    if(s.Encounter!=null)
                    {
                        battles++;var journal=new BattleJournal(s.Encounter.Battle.State,"incident-real-commands","test");
                        for(int n=0;n<2000&&!journal.State.Outcome.IsEnded;n++)Assert.That(journal.Apply(TacticalAi.Choose(journal.State).Command).IsApplied,Is.True);
                        Assert.That(journal.State.Outcome.IsEnded,Is.True);Assert.That(ReplayVerification.Verify(journal.Header,journal.Records,journal.Footer()).Matches,Is.True);
                        Assert.That(s.ResolveBattle(journal.State),Is.True);Assert.That(s.ResolveBattle(journal.State),Is.False);
                        s=s.CaptureSave().Restore();
                    }
                }
                if(s.HandoffPending)s.ContinueHandoff(s.ActiveSide);
            }
            Assert.That(battles,Is.GreaterThanOrEqualTo(1));Assert.That(s.Winner,Is.Not.Null);
            Assert.That(s.Incident.Raiders.SelectMany(r=>r.Force.Formation.Members).Any(c=>c.Armor<c.Profile.MaxArmor||c.Status==PersistentCharacterStatus.Dead),Is.True);
            Assert.That(s.CaptureSave().Restore().CaptureSave().checksum,Is.EqualTo(s.CaptureSave().checksum));
        }
        [Test] public void StrandedGuardInterceptsWithinAreaAndPhysicallyReturnsHomeWithoutPursuit()
        {
            var s=Fixture(d=>{d.refresh=8;d.incident.phase=(int)PortalPhase.Stable;d.incident.contract=(int)HoldState.Expired;d.west.node=14;Actor(d,0,16,RaiderState.GuardingClosedAnchor);});
            HumanEnd(s);HumanEnd(s);s.ContinueWorldPhase();Assert.That(s.PendingContact,Is.Not.Null);Assert.That(s.Incident.Raiders[0].Force.Node,Is.EqualTo(15));Assert.That(s.Incident.Raiders[0].Force.Tempo,Is.EqualTo(30));
            s.RespondToContact(Side.West,true);Assert.That(s.West.Node,Is.LessThan(14));s.ContinueWorldPhase();s.ContinueHandoff(s.ActiveSide);HumanEnd(s);HumanEnd(s);s.ContinueWorldPhase();
            Assert.That(s.PendingContact,Is.Null);Assert.That(s.Incident.Raiders[0].Force.Node,Is.EqualTo(16));Assert.That(s.Incident.Raiders[0].Force.Tempo,Is.EqualTo(80));Assert.That(s.Incident.Raiders[0].State,Is.EqualTo(RaiderState.GuardingClosedAnchor));
        }
        [Test] public void CorruptLoadAndWrongPhaseFailWithoutLiveMutation()
        {
            var s=Fixture();string hash=s.CaptureSave().checksum;var d=s.CaptureSave();d.incident.cursor=20;Assert.Throws<System.IO.InvalidDataException>(()=>d.Restore());Assert.That(s.CaptureSave().checksum,Is.EqualTo(hash));
        }
    }
}

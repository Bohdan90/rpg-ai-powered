using System;
using System.Linq;
using NUnit.Framework;
using RPG.Core;

namespace RPG.Tests
{
    public class StrategicScenarioTests
    {
        internal static void Move(StrategicScenario s,params int[] nodes)
        {foreach(int n in nodes)Assert.That(s.Move(n),Is.True,"Move "+n+": "+s.PreviewMove(n).Reason);}
        internal static BattleState Finish(StrategicScenario s,bool victory=true,bool withdrawal=false)
        {
            // Controlled aftermath fixture; route playthroughs use real ordered tactical commands separately.
            var state=s.Encounter.Battle.State.Copy();var losing=victory?Side.East:Side.West;
            foreach(var u in state.Units.Where(u=>u.Side==losing))
            {u.Status=withdrawal?UnitStatus.Escaped:UnitStatus.Dead;if(!withdrawal)u.Hp=0;}
            state.Outcome=new BattleOutcome(victory?Side.West:Side.East,losing,withdrawal?BattleEndReason.Withdrawal:BattleEndReason.Eliminated);
            return state;
        }
        internal static void Win(StrategicScenario s){Assert.That(s.ResolveBattle(Finish(s)),Is.True);}
        [Test]
        public void GraphRouteCostsAndNoCrossLink()
        {
            var g=StrategicGraph.Mission01;Assert.That(g.Nodes.Count,Is.EqualTo(18));Assert.That(g.Cost(14,9),Is.EqualTo(-1));
            Assert.That(g.PathCost(new[]{1,2,3,8,9,10}),Is.EqualTo(95));
            Assert.That(g.PathCost(new[]{1,2,3,4,5,6,10}),Is.EqualTo(145));
            Assert.That(g.PathCost(new[]{1,2,12,13,14,15,16,17,10}),Is.EqualTo(180));
        }
        [Test]
        public void PreviewAndInvalidCommandsArePureAndMovementSpendsExactBudget()
        {
            var s=new StrategicScenario();string before=s.DebugState();Assert.That(s.PreviewMove(5).Cost,Is.EqualTo(90));
            Assert.That(s.Move(11),Is.False);Assert.That(s.Move(0),Is.False);Assert.That(s.InteractPortal(),Is.False);
            Assert.That(s.DebugState(),Is.EqualTo(before));Move(s,5);Assert.That(s.Tempo,Is.EqualTo(10));
            before=s.DebugState();Assert.That(s.Move(6),Is.False);Assert.That(s.DebugState(),Is.EqualTo(before));
        }
        [TestCase(100,50)][TestCase(50,0)][TestCase(49,0)][TestCase(1,0)][TestCase(0,0)]
        public void AttackCost(int before,int after)=>Assert.That(StrategicScenario.AfterAttackCost(before),Is.EqualTo(after));
        [Test] public void NegativeTempoForbidsAttack()=>Assert.Throws<InvalidOperationException>(()=>StrategicScenario.AfterAttackCost(-1));
        [Test]
        public void NorthTimingAndDelayedPatrolContact()
        {
            var s=new StrategicScenario();Move(s,5);s.EndActivation();Assert.That(s.Actor(StrategicActorKind.Patrol).Node,Is.EqualTo(7));
            Assert.That(s.PreviewMove(6).IsLegal,Is.True);s.EndActivation();Assert.That(s.Actor(StrategicActorKind.Patrol).Node,Is.EqualTo(6));
            Assert.That(s.PreviewMove(6).IsLegal,Is.False);Assert.That(s.CanAttack(StrategicActorKind.Patrol),Is.True);
        }
        [Test]
        public void IncursionAArmsThenRavagesAndPhysicallyExits()
        {
            var s=new StrategicScenario();s.EndActivation();var a=s.Actor(StrategicActorKind.IncursionA);
            Assert.That(a.Node,Is.EqualTo(14));Assert.That(a.RaidArmed,Is.True);Assert.That(s.Waystation,Is.EqualTo(StrategicSiteCondition.Intact));
            s.EndActivation();Assert.That(s.Waystation,Is.EqualTo(StrategicSiteCondition.Ravaged));Assert.That(a.Node,Is.EqualTo(17));Assert.That(a.Objective,Is.EqualTo(StrategicObjective.Exited));
            Assert.That(s.Result,Is.EqualTo(StrategicMissionResult.Ongoing));Assert.That(s.Events.Any(e=>e.Contains("IncursionA moved to Forest Track")),Is.True);
        }
        [Test]
        public void SouthInterceptionPausesActorPhaseAndResupplyUsesRealFood()
        {
            var s=new StrategicScenario();Move(s,14);Assert.That(s.Tempo,Is.EqualTo(20));s.EndActivation();
            Assert.That(s.Encounter.LeadActor,Is.EqualTo(StrategicActorKind.IncursionA));Assert.That(s.Actor(StrategicActorKind.IncursionA).Node,Is.EqualTo(15));
            Assert.That(s.Tempo,Is.EqualTo(20));Assert.That(s.Refresh,Is.EqualTo(1));Assert.That(s.Actor(StrategicActorKind.Patrol).Node,Is.EqualTo(7));
            Win(s);Assert.That(s.Refresh,Is.EqualTo(2));Assert.That(s.WaystationFood,Is.EqualTo(6));Assert.That(s.Provisions,Is.EqualTo(36));
            Assert.That(s.Actor(StrategicActorKind.Patrol).Node,Is.EqualTo(7),"Earlier actors must not repeat after battle");
            s.EndActivation();Assert.That(s.WaystationFood,Is.Zero);Assert.That(s.Provisions,Is.EqualTo(36));
        }
        [Test]
        public void BridgeAndAreaGuardStayRemovedAcrossBattlesAndPortalEndsActivation()
        {
            var s=new StrategicScenario();Move(s,3,8);Assert.That(s.PlayerNode,Is.EqualTo(3));Assert.That(s.Tempo,Is.Zero);Win(s);
            Assert.That(s.PlayerNode,Is.EqualTo(8));Assert.That(s.BridgeGuardDefeated,Is.True);s.EndActivation();Move(s,10);
            Assert.That(s.InteractPortal(),Is.False);s.EndActivation();Assert.That(s.Encounter.LeadActor,Is.EqualTo(StrategicActorKind.AreaGuard));Win(s);
            Assert.That(s.PortalAccessOpen,Is.True);Assert.That(s.BridgeGuardDefeated,Is.True);int r=s.Refresh;
            Assert.That(s.InteractPortal(),Is.True);Assert.That(s.PortalInvestigated,Is.True);Assert.That(s.Refresh,Is.EqualTo(r+1));
            Assert.That(s.Result,Is.EqualTo(StrategicMissionResult.Ongoing));
        }
        [Test]
        public void AreaGuardCancelsOutsideLeashWithoutTeleport()
        {
            var s=new StrategicScenario();s.PlayerNode=10;s.Actor(StrategicActorKind.Patrol).Objective=StrategicObjective.Removed;Move(s,6);s.EndActivation();
            Assert.That(s.Actor(StrategicActorKind.AreaGuard).Node,Is.EqualTo(18));Assert.That(s.Actor(StrategicActorKind.AreaGuard).InterceptPending,Is.False);
            var a=s.Actor(StrategicActorKind.AreaGuard);a.Node=9;s.PlayerNode=3;s.EndActivation();
            Assert.That(a.Node,Is.EqualTo(18));Assert.That(s.Events.Any(e=>e.Contains("AreaGuard moved to Portal Verge")),Is.True);
        }
        [Test]
        public void IncursionBWorldTimingAndVillageFailureDoNotDependOnPortal()
        {
            var s=new StrategicScenario();s.EndActivation();s.EndActivation();s.EndActivation();
            var b=s.Actor(StrategicActorKind.IncursionB);Assert.That(s.Refresh,Is.EqualTo(4));Assert.That(b.Node,Is.EqualTo(11));
            Assert.That(s.PortalInvestigated,Is.False);s.EndActivation();Assert.That(b.Node,Is.EqualTo(12));Assert.That(b.RaidArmed,Is.True);
            Assert.That(s.Village,Is.EqualTo(StrategicSiteCondition.Intact));s.EndActivation();Assert.That(s.Result,Is.EqualTo(StrategicMissionResult.VillageRavaged));
        }
        [Test]
        public void IncursionBWithdrawalResolvesThreatWithoutErasingSurvivors()
        {
            var s=new StrategicScenario();s.EndActivation();s.EndActivation();s.EndActivation();s.EndActivation();Move(s,2);
            s.Attack(StrategicActorKind.IncursionB);Assert.That(s.ResolveBattle(Finish(s,withdrawal:true)),Is.True);
            Assert.That(s.VillageThreatResolved,Is.True);Assert.That(s.Actor(StrategicActorKind.IncursionB).Formation.LivingMembers.Count(),Is.EqualTo(3));
            s.PortalInvestigated=true;s.EndActivation();Move(s,1);Assert.That(s.Result,Is.EqualTo(StrategicMissionResult.CouncilAssistanceRequested));
        }
        [Test]
        public void HungerAndRavagedSupplyAreRealWithoutArmorRepairOrHpStarvation()
        {
            var s=new StrategicScenario();Assert.That(s.Consumption,Is.EqualTo(6));s.EndActivation();s.EndActivation();
            s.PlayerNode=14;s.Provisions=0;s.EndActivation();Assert.That(s.Provisions,Is.Zero);Assert.That(s.Hungry,Is.True);
            Assert.That(s.PreviewMove(15).Cost,Is.EqualTo(32));Assert.That(s.Player.Members.All(c=>c.Hp==c.Profile.MaxHp),Is.True);
        }
        [Test]
        public void BattleBridgePreservesIdentityPoolsDeathSafeAndProgressionWithoutFreeRefresh()
        {
            var s=new StrategicScenario();Move(s,3,8);var ids=s.Player.Members.Select(c=>c.CharacterId).ToArray();var final=Finish(s);
            final.FindUnit(new UnitId(1)).Hp=17;final.FindUnit(new UnitId(1)).Armor=5;
            final.FindUnit(new UnitId(2)).Hp=0;final.FindUnit(new UnitId(2)).Status=UnitStatus.Dead;
            final.FindUnit(new UnitId(3)).Status=UnitStatus.Escaped;final.FindUnit(new UnitId(3)).Hp=13;final.FindUnit(new UnitId(3)).Armor=2;
            Assert.That(s.ResolveBattle(final),Is.True);Assert.That(s.Refresh,Is.EqualTo(1));Assert.That(s.Provisions,Is.EqualTo(36));Assert.That(s.Consumption,Is.EqualTo(5));
            CollectionAssert.AreEqual(ids,s.Player.Members.Select(c=>c.CharacterId));Assert.That(s.Player.Commander.Hp,Is.EqualTo(17));Assert.That(s.Player.Commander.Armor,Is.EqualTo(5));
            Assert.That(s.Player.Members[2].Status,Is.EqualTo(PersistentCharacterStatus.EscapedSafe));Assert.That(s.Player.Commander.PersonalXp,Is.GreaterThan(0));
            Assert.That(s.ResolveBattle(final),Is.False); // cannot pay XP twice
            s.EndActivation();Assert.That(s.Player.Commander.Hp,Is.EqualTo(23));Assert.That(s.Player.Commander.Armor,Is.EqualTo(5));
            Move(s,10);s.Attack(StrategicActorKind.AreaGuard);Assert.That(s.Encounter.Battle.State.FindUnit(new UnitId(2)),Is.Null);
            Assert.That(s.Encounter.Battle.State.FindUnit(new UnitId(3)).Hp,Is.EqualTo(19));Assert.That(s.Encounter.Battle.State.FindUnit(new UnitId(3)).Armor,Is.EqualTo(2));
        }
        [Test]
        public void CommanderDeathKeepsRemnantAndTotalLossFails()
        {
            var s=new StrategicScenario();Move(s,3,8);var f=Finish(s);f.FindUnit(new UnitId(1)).Hp=0;f.FindUnit(new UnitId(1)).Status=UnitStatus.Dead;s.ResolveBattle(f);
            Assert.That(s.Player.Commanderless&&s.Player.RosterLocked,Is.True);Assert.That(s.Result,Is.EqualTo(StrategicMissionResult.Ongoing));
            s.EndActivation();Move(s,10);s.Attack(StrategicActorKind.AreaGuard);s.ResolveBattle(Finish(s,false));Assert.That(s.Result,Is.EqualTo(StrategicMissionResult.FormationLost));
        }
        [Test]
        public void WithdrawalDisplacesCostsDebtAndNegativeTempoDisablesRetreatOnNextBattle()
        {
            var s=new StrategicScenario();Move(s,3,8);s.ResolveBattle(Finish(s,false,true));
            Assert.That(s.Tempo,Is.EqualTo(-40));Assert.That(s.PlayerNode,Is.Not.EqualTo(3));Assert.That(s.Move(2),Is.False);
            s.EndActivation();Assert.That(s.Tempo,Is.EqualTo(60));
            var u=new UnitState(new UnitId(1),Side.West,UnitProfile.HumanWarriorTI,new GridPosition(1,1),Facing.East,ownRetreatEdge:RetreatEdge.Unavailable);
            Assert.That(Battlefield.ControlMap.IsRetreatZone(u,new GridPosition(0,1)),Is.False);
        }
        [Test]
        public void EqualCommandsResultsAndActorPhasesRemainDeterministic()
        {
            var a=new StrategicScenario();var b=new StrategicScenario();Move(a,14);Move(b,14);a.EndActivation();b.EndActivation();
            Assert.That(a.DebugState(),Is.EqualTo(b.DebugState()));Win(a);Win(b);Assert.That(a.DebugState(),Is.EqualTo(b.DebugState()));
            CollectionAssert.AreEqual(a.Events,b.Events);
        }
        [Test]
        public void AttackSnapshotsAdjacentParticipantsAndDoesNotRecursivelyExpand()
        {
            var s=new StrategicScenario();s.PlayerNode=10;s.Actor(StrategicActorKind.IncursionA).Node=17;
            Assert.That(s.Attack(StrategicActorKind.AreaGuard),Is.True);
            CollectionAssert.AreEquivalent(new[]{StrategicActorKind.AreaGuard,StrategicActorKind.IncursionA},s.Encounter.Participants);
            Assert.That(s.Encounter.ApproachEdges[StrategicActorKind.AreaGuard],Is.EqualTo(RetreatEdge.East));
            Assert.That(s.Encounter.ApproachEdges[StrategicActorKind.IncursionA],Is.EqualTo(RetreatEdge.South));
            Assert.That(s.Encounter.Battle.State.Units.Select(u=>u.Position).Distinct().Count(),Is.EqualTo(s.Encounter.Battle.State.Units.Count));
            Assert.That(s.Encounter.UnitIds.Values.Distinct().Count(),Is.EqualTo(s.Encounter.UnitIds.Count));
            Win(s);Assert.That(s.Actor(StrategicActorKind.IncursionA).Active,Is.False);Assert.That(s.PortalAccessOpen,Is.True);
        }
        [Test]
        public void PortalCannotBeOccupiedEvenAfterDefeatingAnActorAtSpawn()
        {
            var s=new StrategicScenario();s.EndActivation();s.EndActivation();s.EndActivation();s.PlayerNode=10;
            s.Actor(StrategicActorKind.AreaGuard).Objective=StrategicObjective.Removed;s.Attack(StrategicActorKind.IncursionB);Win(s);
            Assert.That(s.PlayerNode,Is.EqualTo(10));Assert.That(s.Move(11),Is.False);
        }
        [Test]
        public void NegativeTempoDefenderCannotEscapeAndSnapshotReplays()
        {
            var s=new StrategicScenario();s.PlayerNode=14;s.Tempo=-40;s.EndActivation();
            Assert.That(s.Encounter.Battle.State.Units.Where(u=>u.Side==Side.West).All(u=>u.OwnRetreatEdge==RetreatEdge.Unavailable),Is.True);
            var snapshot=ReplaySnapshot.Capture(s.Encounter.Battle.State);
            Assert.That(BattleStateHash.Compute(snapshot.Restore()),Is.EqualTo(BattleStateHash.Compute(s.Encounter.Battle.State)));
        }
        [Test]
        public void XpValuationDoesNotChangeWhenTheFirstSideLevelsDuringResolution()
        {
            var west=new PersistentFormation("w",Side.West,new[]{new PersistentCharacter("w1",UnitProfile.HumanWarriorTI,personalXp:9.99m),new PersistentCharacter("w2",UnitProfile.HumanWarriorTI)});
            var east=new PersistentFormation("e",Side.East,new[]{new PersistentCharacter("e1",UnitProfile.HumanWarriorTI)});
            var battle=PersistentBattle.Start(west,east,new[]{new PersistentDeployment("w1",new UnitId(1),new GridPosition(1,2),Facing.East),
                new PersistentDeployment("w2",new UnitId(2),new GridPosition(2,2),Facing.East),new PersistentDeployment("e1",new UnitId(3),new GridPosition(10,2),Facing.West)},1,Battlefield.ControlMap);
            var f=battle.State.Copy();f.FindUnit(new UnitId(1)).Status=UnitStatus.Escaped;f.FindUnit(new UnitId(3)).Status=UnitStatus.Dead;f.FindUnit(new UnitId(3)).Hp=0;
            f.Outcome=new BattleOutcome(Side.West,Side.East,BattleEndReason.Eliminated);var r=battle.Resolve(f);
            Assert.That(west.Members[0].PersonalLevel,Is.EqualTo(2));Assert.That(r.East.EarnedEnemyWeight,Is.EqualTo(10.2m*.2m*.15m));
        }
        [Test]
        public void PortalExactBudgetAndSuccessPrerequisites()
        {
            var s=new StrategicScenario();s.PlayerNode=10;s.Actor(StrategicActorKind.AreaGuard).Objective=StrategicObjective.Removed;s.Tempo=4;
            string before=s.DebugState();Assert.That(s.InteractPortal(),Is.False);Assert.That(s.DebugState(),Is.EqualTo(before));
            s.Tempo=5;Assert.That(s.InteractPortal(),Is.True);Assert.That(s.Refresh,Is.EqualTo(2));Assert.That(s.Tempo,Is.EqualTo(100));
            s.PlayerNode=1;s.EndActivation();Assert.That(s.Result,Is.EqualTo(StrategicMissionResult.Ongoing),"B remains unresolved");
        }
        [Test]
        public void TrappedRetreatStaysAtOriginAndIgnoresNonparticipatingEnemiesForSeparation()
        {
            var s=new StrategicScenario();s.PlayerNode=1;s.Actor(StrategicActorKind.Patrol).Node=2;s.Actor(StrategicActorKind.IncursionA).Node=3;
            s.Actor(StrategicActorKind.IncursionB).Objective=StrategicObjective.Raid;s.Actor(StrategicActorKind.IncursionB).Node=12;
            Assert.That(s.RetreatDestination(1,new[]{2},true,null),Is.EqualTo(1));
        }
        [Test]
        public void KeepTransfersAtMostSixAndForeignBattleResultIsRejectedWithoutMutation()
        {
            var s=new StrategicScenario();s.Provisions=10;s.EndActivation();Assert.That(s.Provisions,Is.EqualTo(10));
            Move(s,3,8);var another=new StrategicScenario(123);Move(another,3,8);string before=s.DebugState();
            Assert.That(s.ResolveBattle(Finish(another)),Is.False);Assert.That(s.DebugState(),Is.EqualTo(before));
        }
        [Test]
        public void WithdrawnLeadIsNotAdvancedWhenItsCoalitionWins()
        {
            var s=new StrategicScenario();s.PlayerNode=10;s.EndActivation();
            Assert.That(s.Encounter.Participants,Does.Contain(StrategicActorKind.IncursionA));
            var final=Finish(s,false,true);var e=s.Encounter;
            foreach(var c in s.Actor(StrategicActorKind.AreaGuard).Formation.Members)final.FindUnit(e.UnitIds[c.CharacterId]).Status=UnitStatus.Escaped;
            s.Actor(StrategicActorKind.IncursionA).Tempo=0; // no ordinary movement after the battle, isolate placement
            Assert.That(s.ResolveBattle(final),Is.True);
            Assert.That(s.Actor(StrategicActorKind.AreaGuard).Node,Is.Not.EqualTo(10));
            Assert.That(s.Actor(StrategicActorKind.IncursionA).Node,Is.EqualTo(17),"No substitute attacker advance");
        }
    }
}

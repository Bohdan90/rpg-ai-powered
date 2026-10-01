using System;
using System.Linq;
using NUnit.Framework;
namespace RPG.Core.Tests
{
    public class StrategicJourneyTests
    {
        private static void Cycle(CrossroadsScenario w){var side=w.ActiveSide;Assert.That(w.EndActivation(side),Is.True);Assert.That(w.ContinueHandoff(CrossroadsScenario.Other(side)),Is.True);Assert.That(w.EndActivation(w.ActiveSide),Is.True);Assert.That(w.ContinueHandoff(side),Is.True);}
        [Test] public void DistantKnownDestinationMovesPartialRetainsThenResumesOnlyOnOwnHandoff()
        {
            var w=ProductionRoads.Create();var s=w.Seamless;var f=w.West;f.Tempo=35;
            Assert.That(s.SetDestination(Side.West,new WorldAddress(WorldId.Frontier,6)),Is.True);Assert.That(f.Node,Is.Not.EqualTo(6));Assert.That(f.Tempo,Is.GreaterThanOrEqualTo(0));Assert.That(s.Journey(f.Formation.FormationId),Is.Not.Null);
            int at=f.Node,tempo=f.Tempo;w.EndActivation(Side.West);w.ContinueHandoff(Side.East);Assert.That(f.Node,Is.EqualTo(at));Assert.That(f.Tempo,Is.EqualTo(tempo));
            Assert.That(s.SetDestination(Side.West,new WorldAddress(WorldId.Frontier,8)),Is.False);w.EndActivation(Side.East);Assert.That(f.Node,Is.EqualTo(at));w.ContinueHandoff(Side.West);Assert.That(f.Node,Is.Not.EqualTo(at));Assert.That(f.Tempo,Is.InRange(0,100));
        }
        [Test] public void CancelAndNewDestinationOverrideAreExplicitAndInvalidIsPure()
        {
            var w=ProductionRoads.Create();w.West.Tempo=0;var s=w.Seamless;string id=w.West.Formation.FormationId;
            Assert.That(s.SetDestination(Side.West,new WorldAddress(WorldId.Frontier,6)),Is.True);string hash=w.CaptureSave().checksum;
            Assert.That(s.SetDestination(Side.West,new WorldAddress(WorldId.StoneValley,4)),Is.False);Assert.That(w.CaptureSave().checksum,Is.EqualTo(hash));
            Assert.That(s.SetDestination(Side.West,new WorldAddress(WorldId.Frontier,8)),Is.True);Assert.That(s.Journey(id).destination,Is.EqualTo(8));Assert.That(s.CancelDestination(Side.West,id),Is.True);Assert.That(s.Journey(id),Is.Null);
            int node=w.West.Node;Cycle(w);Assert.That(w.West.Node,Is.EqualTo(node));
        }
        [Test] public void PortalArrivalPausesEvenWithTempoAndNeverAutoTraverses()
        {
            var w=SeamlessWorlds.Create();var s=w.Seamless;Assert.That(s.SetDestination(Side.West,new WorldAddress(WorldId.Frontier,24)),Is.True);
            Assert.That(w.West.Node,Is.EqualTo(24));Assert.That(w.West.WorldId,Is.EqualTo(WorldId.Frontier));Assert.That(s.Journey(w.West.Formation.FormationId).paused,Does.Contain("Portal"));Cycle(w);Assert.That(w.West.Node,Is.EqualTo(24));
            var q=s.PreviewTraverse(Side.West,w.West.Formation.FormationId,w.West.Address);Assert.That(q.Legal,Is.True);Assert.That(q.Cost,Is.EqualTo(20));Assert.That(q.Destination,Is.EqualTo("Unknown destination"));
            s.Traverse(Side.West,w.West.Formation.FormationId,w.West.Address);Assert.That(s.Journey(w.West.Formation.FormationId),Is.Null);Assert.That(w.West.WorldId,Is.EqualTo(WorldId.StoneValley));
        }
        [Test] public void NewlyObservedHostileInterruptsRatherThanReroutes()
        {
            var w=ProductionRoads.Create();w.East.Node=6;w.Seamless.Observe();Assert.That(w.Seamless.IsVisibleEnemy(Side.West,w.East.Formation.FormationId),Is.False);
            Assert.That(w.Seamless.SetDestination(Side.West,new WorldAddress(WorldId.Frontier,19)),Is.True);for(int i=0;i<4&&w.Seamless.Journey(w.West.Formation.FormationId)?.paused=="";i++)Cycle(w);
            Assert.That(w.Seamless.Journey(w.West.Formation.FormationId).paused,Does.Contain("hostile"));int at=w.West.Node;Cycle(w);Assert.That(w.West.Node,Is.EqualTo(at));
        }
        [Test] public void HiddenOccupancyDoesNotChangePreviewRouteOrRevealComposition()
        {
            var a=ProductionRoads.Create();var b=ProductionRoads.Create();a.East.Node=6;b.East.Node=19;a.Seamless.Observe();b.Seamless.Observe();var to=new WorldAddress(WorldId.Frontier,6);
            Assert.That(a.Seamless.PreviewJourney(Side.West,to).Path,Is.EqualTo(b.Seamless.PreviewJourney(Side.West,to).Path));Assert.That(a.Seamless.PreviewJourney(Side.West,to).Reason,Is.EqualTo(b.Seamless.PreviewJourney(Side.West,to).Reason));
        }
        [Test] public void InvalidSavedLegPausesAndNeverTeleports()
        {
            var w=ProductionRoads.Create();w.West.Tempo=0;w.Seamless.SetDestination(Side.West,new WorldAddress(WorldId.Frontier,6));var j=w.Seamless.journeys[w.West.Formation.FormationId];j.route[1]=60;int at=w.West.Node;Cycle(w);Assert.That(w.West.Node,Is.EqualTo(at));Assert.That(j.paused,Does.Contain("legal"));
        }
        [Test] public void FormationChangesPauseAndDontNormalizeOrHeal()
        {
            var w=ProductionRoads.Create();w.West.Tempo=0;w.Seamless.SetDestination(Side.West,new WorldAddress(WorldId.Frontier,6));var id=w.West.Formation.FormationId;var j=w.Seamless.journeys[id];j.formation="different surviving roster";int at=w.West.Node;Cycle(w);Assert.That(w.West.Node,Is.EqualTo(at));Assert.That(j.paused,Does.Contain("Formation changed"));
        }
        [Test] public void JourneyRoundTripAndContinuationHaveIdenticalHashes()
        {
            var a=ProductionRoads.Create();a.West.Tempo=35;a.Seamless.SetDestination(Side.West,new WorldAddress(WorldId.Frontier,6));var b=a.CaptureSave().Restore();Assert.That(b.CaptureSave().checksum,Is.EqualTo(a.CaptureSave().checksum));
            for(int i=0;i<3;i++){Cycle(a);Cycle(b);Assert.That(b.CaptureSave().checksum,Is.EqualTo(a.CaptureSave().checksum));}Assert.That(b.Seamless.ProductionTopology,Is.True);
        }
        [Test] public void PurePreviewsAndDefensiveJourneyCopiesDoNotMutateState()
        {
            var w=ProductionRoads.Create();w.West.Tempo=0;w.Seamless.SetDestination(Side.West,new WorldAddress(WorldId.Frontier,6));string hash=w.CaptureSave().checksum;
            var j=w.Seamless.Journey(w.West.Formation.FormationId);j.route[1]=999;j.paused="changed";w.Seamless.PreviewJourney(Side.West,new WorldAddress(WorldId.Frontier,8));w.Seamless.PreviewTraverse(Side.West,w.West.Formation.FormationId,w.West.Address);Assert.That(w.CaptureSave().checksum,Is.EqualTo(hash));
        }
        [Test] public void Legacy07SnapshotRestoresWithoutNewRuleHashChanges()
        {
            var w=SeamlessWorlds.Create();w.Seamless.TravelRules=1;var d=w.CaptureSave();Assert.That(d.version,Is.EqualTo(6));Assert.That(d.Restore().CaptureSave().checksum,Is.EqualTo(d.checksum));
        }
        [Test] public void TopologyIsConnectedRichDeterministicAndOnlyDeliberateSpurIsBridge()
        {
            var g=ProductionRoads.Map;var d=GraphDiagnostics.Inspect(g);Assert.That(d.Nodes,Is.EqualTo(60));Assert.That(d.Components,Is.EqualTo(1));Assert.That(d.CycleRank,Is.GreaterThan(10));Assert.That(d.Articulations,Is.EqualTo(new[]{59}));Assert.That(d.Bridges,Is.EqualTo(new[]{"25-59"}));Assert.That(d.ToString(),Is.EqualTo(GraphDiagnostics.Inspect(g).ToString()));
            Assert.That(g.Nodes.Select(n=>n.Id).Distinct().Count(),Is.EqualTo(60));Assert.That(g.Edges.Select(e=>Math.Min(e.A,e.B)+":"+Math.Max(e.A,e.B)).Distinct().Count(),Is.EqualTo(g.Edges.Count));Assert.That(g.Edges.All(e=>e.A!=e.B&&g.Node(e.A)!=null&&g.Node(e.B)!=null&&e.Tempo>0),Is.True);
            foreach(int poi in ProductionRoads.PointsOfInterest)Assert.That(g.Path(1,poi).Length,Is.GreaterThan(0));TestContext.WriteLine(d);
        }
        [TestCase(1,6)] [TestCase(13,6)] [TestCase(1,7)] [TestCase(13,7)] [TestCase(1,8)] [TestCase(13,8)]
        public void KeyAreasHaveAlternateRoadsAfterOrdinaryConnectionRemoval(int from,int to)
        {
            var g=ProductionRoads.Map;var route=g.Path(from,to);var removed=g.Edges.Single(e=>e.Connects(route[1],route[2]));var alternate=new StrategicGraph(g.Nodes.ToArray(),g.Edges.Where(e=>!e.Connects(removed.A,removed.B)).ToArray());
            Assert.That(alternate.Path(from,to).Length,Is.GreaterThan(0));TestContext.WriteLine(from+" -> "+to+" : "+string.Join(",",route)+" cost "+g.PathCost(route)+"; alternative "+alternate.PathCost(alternate.Path(from,to)));
        }
        [Test] public void ActualBattlePausesCommittedOrdersAndReturnsSamePersistentIdsOnce()
        {
            var w=ProductionRoads.Create();w.West.Node=7;w.East.Node=9;w.Seamless.Observe();w.West.Tempo=0;
            Assert.That(w.Seamless.SetDestination(Side.West,new WorldAddress(WorldId.Frontier,8)),Is.True);
            var ids=w.West.Formation.Members.Select(c=>c.CharacterId).ToArray();Assert.That(w.Realm.Attack(Side.West,w.East.Formation.FormationId),Is.True);
            Assert.That(w.Seamless.Journey(w.West.Formation.FormationId).paused,Does.Contain("Battle"));Assert.That(w.RespondToContact(Side.East,false),Is.True);
            var journal=new BattleJournal(w.Encounter.Battle.State,"08 controlled contact","automated regression");
            for(int i=0;i<1800&&!journal.State.Outcome.IsEnded;i++)Assert.That(journal.Apply(TacticalAi.Choose(journal.State).Command).IsApplied,Is.True);
            Assert.That(journal.State.Outcome.IsEnded,Is.True);Assert.That(ReplayVerification.Verify(journal.Header,journal.Records,journal.Footer()).Matches,Is.True);
            Assert.That(w.ResolveBattle(journal.State),Is.True);Assert.That(w.ResolveBattle(journal.State),Is.False);Assert.That(w.Refresh,Is.EqualTo(1));Assert.That(w.West.Formation.Members.Select(c=>c.CharacterId),Is.EqualTo(ids));
            Assert.That(w.CaptureSave().Restore().CaptureSave().checksum,Is.EqualTo(w.CaptureSave().checksum));
        }
        [Test] public void HiddenPhysicalBlockStopsBeforeOccupancyWithoutChoosingAnotherRoute()
        {
            var w=ProductionRoads.Create();w.West.Tempo=0;w.Seamless.SetDestination(Side.West,new WorldAddress(WorldId.Frontier,6));var j=w.Seamless.journeys[w.West.Formation.FormationId];var original=j.route.ToArray();int at=w.West.Node;w.East.Node=j.route[1];
            // Controlled hidden blocker: no artificial disclosure/replan before authoritative leg.
            w.West.Tempo=100;w.Seamless.ResumeJourneys(Side.West);Assert.That(w.West.Node,Is.EqualTo(at));Assert.That(w.West.Tempo,Is.EqualTo(100));Assert.That(j.route,Is.EqualTo(original));Assert.That(j.paused,Does.Contain("obstructed"));Assert.That(j.paused,Does.Not.Contain(w.East.Formation.FormationId));
        }
        [Test] public void NewlyObservedOpponentDuringItsTurnPausesSavedJourneyBeforeNextOwnTurn()
        {
            var w=ProductionRoads.Create();w.West.Tempo=0;w.Seamless.SetDestination(Side.West,new WorldAddress(WorldId.Frontier,6));w.EndActivation(Side.West);w.ContinueHandoff(Side.East);
            w.East.Node=3;w.Seamless.Observe();var j=w.Seamless.Journey(w.West.Formation.FormationId);Assert.That(j.paused,Does.Contain("hostile"));int at=w.West.Node;
            w.EndActivation(Side.East);w.ContinueHandoff(Side.West);Assert.That(w.West.Node,Is.EqualTo(at));
        }
        [Test] public void AlreadyObservedEnemyEnteringContactPausesWithoutWaitingForNewDiscovery()
        {
            var w=ProductionRoads.Create();w.West.Node=50;w.East.Node=11;w.Seamless.Observe();Assert.That(w.Seamless.IsVisibleEnemy(Side.West,w.East.Formation.FormationId),Is.True);w.West.Tempo=0;
            w.Seamless.SetDestination(Side.West,new WorldAddress(WorldId.Frontier,8));w.EndActivation(Side.West);w.ContinueHandoff(Side.East);
            Assert.That(w.Seamless.Move(Side.East,new WorldAddress(WorldId.Frontier,10)),Is.True);Assert.That(w.Seamless.Journey(w.West.Formation.FormationId).paused,Does.Contain("Hostile contact"));
            w.EndActivation(Side.East);w.ContinueHandoff(Side.West);Assert.That(w.West.Node,Is.EqualTo(50));
        }
        [Test] public void OldTopologyAndNewScenarioHaveSeparateIdentity()
        {
            Assert.That(SeamlessWorlds.Frontier.Nodes.Count,Is.EqualTo(25));Assert.That(SeamlessWorlds.Valley.Nodes.Count,Is.EqualTo(7));Assert.That(CityFoundations.Map.Nodes.Count,Is.EqualTo(23));Assert.That(StrategicGraph.Mission01.Nodes.Count,Is.EqualTo(18));
            Assert.That(SeamlessWorlds.Create().CaptureSave().scenario,Is.EqualTo(SeamlessWorlds.ScenarioId));Assert.That(ProductionRoads.Create().CaptureSave().scenario,Is.EqualTo(ProductionRoads.ScenarioId));
        }
    }
}

using System;
using System.Linq;
using NUnit.Framework;
namespace RPG.Core.Tests
{
    public class TravelScale08Tests
    {
        private static void Cycle(CrossroadsScenario w){var side=w.ActiveSide;Assert.That(w.EndActivation(side),Is.True);Assert.That(w.ContinueHandoff(CrossroadsScenario.Other(side)),Is.True);Assert.That(w.EndActivation(w.ActiveSide),Is.True);Assert.That(w.ContinueHandoff(side),Is.True);}
        [Test] public void ExpansionPreservesMacroCyclesAndHasStableIdsAndDistanceCosts()
        {
            var a=TravelScale08.Mainland;var again=TravelScale08.Expand(ProductionRoads.Map);
            Assert.That(a.Nodes.Select(n=>n.Id),Is.EqualTo(again.Nodes.Select(n=>n.Id)));
            Assert.That(a.Nodes.Select(n=>n.Id).Distinct().Count(),Is.EqualTo(a.Nodes.Count));
            Assert.That(a.Nodes.Count,Is.InRange(300,600));Assert.That(a.Edges.Count-a.Nodes.Count+1,Is.EqualTo(16));
            Assert.That(a.PathCost(a.Path(1,2)),Is.LessThan(a.PathCost(a.Path(35,53))));
            foreach(var n in ProductionRoads.Map.Nodes)Assert.That(a.Node(n.Id).Name,Is.EqualTo(n.Name));
            Assert.That(a.Edges.All(e=>e.Tempo==10),Is.True);
        }
        [Test] public void DiagnosticsDistinguishMacroAndTravelGraphs()
        {
            foreach(var g in new[]{ProductionRoads.Map,TravelScale08.Mainland,TravelScale08.ValleyMacro,TravelScale08.Valley})TestContext.WriteLine(GraphDiagnostics.Inspect(g));
            var a=TravelScale08.Mainland;
            foreach(var pair in new[]{(1,6),(13,6),(1,7),(13,7),(1,8),(13,8),(2,25)}) {
                var route=a.Path(pair.Item1,pair.Item2);var removed=a.Edges.Single(e=>e.Connects(route[1],route[2]));var alternative=new StrategicGraph(a.Nodes.ToArray(),a.Edges.Where(e=>!e.Connects(removed.A,removed.B)).ToArray());
                Assert.That(alternative.Path(pair.Item1,pair.Item2).Length,Is.GreaterThan(0));TestContext.WriteLine(pair+" cost "+a.PathCost(route)+" alternative "+alternative.PathCost(alternative.Path(pair.Item1,pair.Item2)));
            }
        }
        [Test] public void OrdinaryDepthAndValleyRatioAreMeasuredOnShortestLandRoutes()
        {
            var a=TravelScale08.Mainland;var b=TravelScale08.Valley;
            int ca=a.PathCost(a.Path(1,13)),cb=b.PathCost(b.Path(1,7));
            TestContext.WriteLine("A keep-to-keep="+ca+"; A west-to-Ash="+a.PathCost(a.Path(1,25))+"; B portal-to-portal="+cb+"; ratio="+(double)cb/ca+"; nodes="+a.Nodes.Count+"/"+b.Nodes.Count+"; edges="+a.Edges.Count+"/"+b.Edges.Count);
            Assert.That(ca/100d,Is.InRange(8,12));Assert.That(cb/(double)ca,Is.InRange(.5,.7));Assert.That(b.Edges.Count-b.Nodes.Count+1,Is.GreaterThanOrEqualTo(3));
        }
        [Test] public void IntermediatePositionAndOrderRoundTripResumeWithoutOpponentMovementOrDuplicateEvents()
        {
            var w=TravelScale08.Create();var f=w.West;f.Tempo=35;
            Assert.That(w.Seamless.SetDestination(Side.West,new WorldAddress(WorldId.Frontier,6)),Is.True);
            Assert.That(TravelScale08.IsTravel(f.Node),Is.True);Assert.That(f.Tempo,Is.EqualTo(5));
            var save=w.CaptureSave();Assert.That(save.version,Is.EqualTo(8));var b=save.Restore();Assert.That(b.West.Node,Is.EqualTo(f.Node));Assert.That(b.CaptureSave().checksum,Is.EqualTo(save.checksum));
            int at=f.Node;w.EndActivation(Side.West);w.ContinueHandoff(Side.East);Assert.That(f.Node,Is.EqualTo(at));Assert.That(f.Tempo,Is.EqualTo(5));w.EndActivation(Side.East);w.ContinueHandoff(Side.West);Cycle(b);
            Assert.That(b.CaptureSave().checksum,Is.EqualTo(w.CaptureSave().checksum));Assert.That(f.Node,Is.Not.EqualTo(at));
            Assert.That(w.Seamless.Knowledge(Side.West).At(f.Address),Is.EqualTo(KnowledgeLevel.CurrentlyObserved));
            string hash=w.CaptureSave().checksum;Assert.That(w.Seamless.SetDestination(Side.East,new WorldAddress(WorldId.Frontier,6)),Is.False);Assert.That(w.CaptureSave().checksum,Is.EqualTo(hash));
        }
        [Test] public void CancelOverrideAndExactArrivalUseRealSegments()
        {
            var w=TravelScale08.Create();w.West.Tempo=0;var s=w.Seamless;string id=w.West.Formation.FormationId;
            s.SetDestination(Side.West,new WorldAddress(WorldId.Frontier,6));s.SetDestination(Side.West,new WorldAddress(WorldId.Frontier,8));Assert.That(s.Journey(id).destination,Is.EqualTo(8));s.CancelDestination(Side.West,id);Cycle(w);Assert.That(w.West.Node,Is.EqualTo(2));
            int target=w.Graph.Neighbors(w.West.Node).First();w.West.Tempo=10;Assert.That(s.SetDestination(Side.West,new WorldAddress(WorldId.Frontier,target)),Is.True);Assert.That(w.West.Node,Is.EqualTo(target));Assert.That(w.West.Tempo,Is.Zero);Assert.That(s.Journey(id),Is.Null);
        }
        [Test] public void PortalRemainsExplicitAndDenseValleyIsSeparate()
        {
            var w=TravelScale08.Create();w.West.Node=24;w.Seamless.Observe();var ids=w.West.Formation.Members.Select(c=>c.CharacterId).ToArray();int tempo=w.West.Tempo;
            Assert.That(w.Seamless.Traverse(Side.West,w.West.Formation.FormationId,w.West.Address),Is.True);Assert.That(w.West.WorldId,Is.EqualTo(WorldId.StoneValley));Assert.That(w.West.Tempo,Is.EqualTo(tempo-20));Assert.That(w.West.Formation.Members.Select(c=>c.CharacterId),Is.EqualTo(ids));
            Assert.That(w.GraphFor(WorldId.StoneValley).Nodes.Count,Is.GreaterThan(100));Assert.That(w.Seamless.Knowledge(Side.West).At(new WorldAddress(WorldId.StoneValley,7)),Is.EqualTo(KnowledgeLevel.Unexplored));Assert.That(w.CaptureSave().Restore().CaptureSave().checksum,Is.EqualTo(w.CaptureSave().checksum));
        }
        [Test] public void IntermediateContactObservationsAndBattleUseExistingBridge()
        {
            var w=TravelScale08.Create();var path=w.Graph.Path(31,41);w.West.Node=path[2];w.East.Node=path[3];w.Seamless.Observe();
            Assert.That(TravelScale08.IsTravel(w.West.Node)&&TravelScale08.IsTravel(w.East.Node),Is.True);
            Assert.That(w.Seamless.IsVisibleEnemy(Side.West,w.East.Formation.FormationId),Is.True);
            Assert.That(w.Seamless.Knowledge(Side.West).History.Any(e=>e.nodes.Contains(w.East.Node)),Is.True);
            Assert.That(w.Realm.Attack(Side.West,w.East.Formation.FormationId),Is.True);Assert.That(w.RespondToContact(Side.East,false),Is.True);
            var journal=new BattleJournal(w.Encounter.Battle.State,"08B road contact","controlled automated");
            for(int i=0;i<1800&&!journal.State.Outcome.IsEnded;i++)Assert.That(journal.Apply(TacticalAi.Choose(journal.State).Command).IsApplied,Is.True);
            Assert.That(journal.State.Outcome.IsEnded,Is.True);Assert.That(ReplayVerification.Verify(journal.Header,journal.Records,journal.Footer()).Matches,Is.True);
            Assert.That(w.ResolveBattle(journal.State),Is.True);Assert.That(w.ResolveBattle(journal.State),Is.False);Assert.That(w.Refresh,Is.EqualTo(1));Assert.That(w.CaptureSave().Restore().CaptureSave().checksum,Is.EqualTo(w.CaptureSave().checksum));
        }
        [Test] public void NormalSuppliedThenHungryJourneyMeasuresActualActivations()
        {
            var w=TravelScale08.Create();int dest=25;int activations=1;
            Assert.That(w.Seamless.SetDestination(Side.West,new WorldAddress(WorldId.Frontier,dest)),Is.True);
            while(w.West.Node!=dest&&activations<20){Cycle(w);activations++;var j=w.Seamless.Journey(w.West.Formation.FormationId);if(w.West.Node!=dest&&j?.paused!=""&&j!=null)Assert.Fail(j.paused);}
            TestContext.WriteLine("Normal West start A2 to A25, activations="+activations+" finalTempo="+w.West.Tempo+" provisions="+w.West.RealmProvisions);
            Assert.That(w.West.Node,Is.EqualTo(dest));Assert.That(activations,Is.InRange(8,12));
        }
        [Test] public void ExplicitLandDepartureFromPortalCanWaitForNextOwnActivation()
        {
            var w=TravelScale08.Create();w.West.Node=24;w.West.Tempo=0;w.Seamless.Observe();int target=w.Graph.Neighbors(24).First();
            Assert.That(w.Seamless.SetDestination(Side.West,new WorldAddress(WorldId.Frontier,target)),Is.True);Cycle(w);
            Assert.That(w.West.Node,Is.EqualTo(target));Assert.That(w.West.WorldId,Is.EqualTo(WorldId.Frontier));Assert.That(w.Seamless.Journey(w.West.Formation.FormationId),Is.Null);
        }
        [Test] public void Legacy08SaveRetainsCoarseTopology()
        {var old=ProductionRoads.Create();var save=old.CaptureSave();var restored=save.Restore();Assert.That(save.version,Is.EqualTo(7));Assert.That(restored.Graph.Nodes.Count,Is.EqualTo(60));Assert.That(restored.Seamless.DenseTravel,Is.False);Assert.That(restored.CaptureSave().checksum,Is.EqualTo(save.checksum));}
        [TestCase(UnitProfileId.FireMageTI,8)][TestCase(UnitProfileId.FireMageTII,8)][TestCase(UnitProfileId.IceMageTI,8)][TestCase(UnitProfileId.IceMageTII,8)][TestCase(UnitProfileId.HumanHealerTI,9)][TestCase(UnitProfileId.HumanWarriorTI,10)][TestCase(UnitProfileId.HumanArcherTI,12)][TestCase(UnitProfileId.ElfWarriorTI,14)]
        public void AuthoredInitiative(UnitProfileId profile,int expected)=>Assert.That(UnitProfile.Get(profile).Initiative,Is.EqualTo(expected));
        [Test] public void MixedActivationOrderingAndLegacyReplayProfiles()
        {
            var profiles=new[]{UnitProfile.FireMageTI,UnitProfile.IceMageTI,UnitProfile.HumanHealerTI,UnitProfile.HumanWarriorTI,UnitProfile.HumanArcherTI,UnitProfile.ElfWarriorTI};
            var units=profiles.Select((p,i)=>new UnitState(new UnitId(i+1),i%2==0?Side.West:Side.East,p,new GridPosition(i+2,3),Facing.East)).ToArray();
            var state=BattleResolver.StartBattle(units,42).State;Assert.That(state.ActivationOrder.Select(id=>state.FindUnit(id).Profile.Initiative),Is.EqualTo(new[]{14,12,10,9,8,8}));
            var snapshot=ReplaySnapshot.Capture(state);Assert.That(BattleStateHash.Compute(snapshot.Restore()),Is.EqualTo(BattleStateHash.Compute(state)));
            foreach(var u in snapshot.units)u.initiative=0;var old=snapshot.Restore();Assert.That(old.FindUnit(new UnitId(1)).Profile.Initiative,Is.EqualTo(11));Assert.That(old.FindUnit(new UnitId(3)).Profile.Initiative,Is.EqualTo(12));
        }
    }
}

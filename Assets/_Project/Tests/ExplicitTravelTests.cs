using System;
using System.Linq;
using NUnit.Framework;
namespace RPG.Core.Tests
{
    public class ExplicitTravelTests
    {
        static void Cycle(CrossroadsScenario w){var side=w.ActiveSide;Assert.That(w.EndActivation(side));Assert.That(w.ContinueHandoff(CrossroadsScenario.Other(side)));Assert.That(w.EndActivation(w.ActiveSide));Assert.That(w.ContinueHandoff(side));}
        static CrossroadsScenario Queued(int tempo=100){var w=TravelScale08.Create();w.West.Tempo=tempo;Assert.That(w.Seamless.SetDestination(Side.West,new WorldAddress(WorldId.Frontier,25)));return w;}
        [Test] public void QueueEndTurnAndOwnHandoffAreStationaryThenExplicitContinueSpendsManyLegs()
        {
            var w=Queued(45);var s=w.Seamless;var id=w.West.Formation.FormationId;Assert.That(w.West.Node,Is.EqualTo(2));Assert.That(w.West.Tempo,Is.EqualTo(45));
            int[] route=s.Journey(id).route;var preview=s.PreviewContinueTravel(Side.West);Assert.That(preview.Path,Is.EqualTo(route.Take(5)));Assert.That(preview.Cost,Is.EqualTo(40));
            string hash=w.CaptureSave().checksum;s.PreviewContinueTravel(Side.West);Assert.That(w.CaptureSave().checksum,Is.EqualTo(hash));
            Assert.That(s.ContinueTravel(Side.West));Assert.That(w.West.Node,Is.EqualTo(route[4]));Assert.That(w.West.Tempo,Is.EqualTo(5));Assert.That(s.Journey(id).destination,Is.EqualTo(25));
            int at=w.West.Node;Assert.That(w.EndActivation(Side.West));Assert.That(w.West.Node,Is.EqualTo(at));w.ContinueHandoff(Side.East);hash=w.CaptureSave().checksum;Assert.That(s.ContinueTravel(Side.West),Is.False);Assert.That(w.CaptureSave().checksum,Is.EqualTo(hash));w.EndActivation(Side.East);w.ContinueHandoff(Side.West);Assert.That(w.West.Node,Is.EqualTo(at));
            Assert.That(s.ContinueTravel(Side.West));Assert.That(w.West.Node,Is.Not.EqualTo(at));
        }
        [Test] public void UnusedTempoIsNotSpentByEitherHandoff()
        {var w=Queued();Cycle(w);Assert.That(w.West.Node,Is.EqualTo(2));Assert.That(w.Seamless.Journey(w.West.Formation.FormationId).next,Is.EqualTo(1));}
        [Test] public void ContinueCrossesMacroJunctionAndKeepsGoing()
        {
            var w=TravelScale08.Create();var g=w.Graph;var route=g.Path(2,25);int junction=Array.FindIndex(route,1,n=>!TravelScale08.IsTravel(n));
            w.West.Node=route[junction-1];w.Seamless.Observe();w.West.Tempo=30;Assert.That(w.Seamless.SetDestination(Side.West,new WorldAddress(WorldId.Frontier,25)));
            Assert.That(w.Seamless.ContinueTravel(Side.West));Assert.That(w.West.Node,Is.EqualTo(route[junction+2]));Assert.That(w.West.Tempo,Is.Zero);
        }
        [Test] public void CancelOverrideAndInsufficientTempoDoNotMoveOrBorrow()
        {
            var w=Queued(9);var s=w.Seamless;string hash=w.CaptureSave().checksum;Assert.That(s.ContinueTravel(Side.West),Is.False);Assert.That(w.CaptureSave().checksum,Is.EqualTo(hash));
            Assert.That(s.SetDestination(Side.West,new WorldAddress(WorldId.Frontier,8)));Assert.That(w.West.Node,Is.EqualTo(2));Assert.That(s.Journey(w.West.Formation.FormationId).destination,Is.EqualTo(8));
            Assert.That(s.CancelDestination(Side.West,w.West.Formation.FormationId));Assert.That(s.ContinueTravel(Side.West),Is.False);Cycle(w);Assert.That(w.West.Node,Is.EqualTo(2));
        }
        [Test] public void IntermediateSaveLoadAndSameExplicitCommandsAreDeterministic()
        {
            var a=Queued(35);a.Seamless.ContinueTravel(Side.West);var save=a.CaptureSave();var b=save.Restore();Assert.That(b.West.Node,Is.EqualTo(a.West.Node));Assert.That(b.CaptureSave().checksum,Is.EqualTo(save.checksum));
            for(int i=0;i<3;i++){int at=a.West.Node;Cycle(a);Cycle(b);Assert.That(a.West.Node,Is.EqualTo(at));Assert.That(b.West.Node,Is.EqualTo(at));a.Seamless.ContinueTravel(Side.West);b.Seamless.ContinueTravel(Side.West);Assert.That(b.CaptureSave().checksum,Is.EqualTo(a.CaptureSave().checksum));}
        }
        [Test] public void PortalArrivalPausesUntilExplicitTraverse()
        {
            var w=TravelScale08.Create();w.West.Node=w.Graph.Neighbors(24).First();w.Seamless.Observe();Assert.That(w.Seamless.SetDestination(Side.West,new WorldAddress(WorldId.Frontier,24)));Assert.That(w.Seamless.ContinueTravel(Side.West));
            Assert.That(w.West.Node,Is.EqualTo(24));Assert.That(w.West.WorldId,Is.EqualTo(WorldId.Frontier));Assert.That(w.Seamless.ContinueTravel(Side.West),Is.False);Cycle(w);Assert.That(w.West.Node,Is.EqualTo(24));Assert.That(w.Seamless.Traverse(Side.West,w.West.Formation.FormationId,w.West.Address));Assert.That(w.West.WorldId,Is.EqualTo(WorldId.StoneValley));
        }
        [Test] public void HiddenHostileInterruptsWithoutHiddenPreviewReroute()
        {
            var a=Queued();var b=Queued();var route=a.Seamless.Journey(a.West.Formation.FormationId).route;a.East.Node=route[5];b.East.Node=route[7];a.Seamless.Observe();b.Seamless.Observe();
            Assert.That(a.Seamless.PreviewContinueTravel(Side.West).Path,Is.EqualTo(b.Seamless.PreviewContinueTravel(Side.West).Path));Assert.That(a.Seamless.ContinueTravel(Side.West));Assert.That(a.West.Node,Is.EqualTo(route[3]));Assert.That(a.Seamless.Journey(a.West.Formation.FormationId).paused,Does.Contain("hostile"));
            Assert.That(a.Seamless.Knowledge(Side.West).History.Any(e=>e.nodes.Contains(a.East.Node)),Is.True);int at=a.West.Node;Cycle(a);Assert.That(a.West.Node,Is.EqualTo(at));Assert.That(a.Seamless.ContinueTravel(Side.West),Is.False);
        }
        [Test] public void InvalidRouteAndChangedFormationArePureRefusals()
        {
            var w=Queued();var j=w.Seamless.journeys[w.West.Formation.FormationId];j.route[1]=60;Assert.That(w.Seamless.ContinueTravel(Side.West),Is.False);Assert.That(w.West.Node,Is.EqualTo(2));Assert.That(w.West.Tempo,Is.EqualTo(100));
            w=Queued();j=w.Seamless.journeys[w.West.Formation.FormationId];j.formation="changed";Assert.That(w.Seamless.PreviewContinueTravel(Side.West).Reason,Does.Contain("Formation changed"));Assert.That(w.Seamless.ContinueTravel(Side.West),Is.False);
        }
        [Test] public void ValleyNormalJourneyDepthAndMainRatio()
        {
            var w=TravelScale08.Create();w.West.WorldId=WorldId.StoneValley;w.West.Node=1;
            // Controlled known-map scale measurement; no resource or movement overrides after initialization.
            foreach(var n in w.GraphFor(WorldId.StoneValley).Nodes)w.Seamless.Knowledge(Side.West).explored.Add(new WorldAddress(WorldId.StoneValley,n.Id));
            w.Seamless.Observe();Assert.That(w.Seamless.SetDestination(Side.West,new WorldAddress(WorldId.StoneValley,7)));int activations=0;
            do{if(activations>0)Cycle(w);activations++;Assert.That(w.Seamless.ContinueTravel(Side.West));}while(w.West.Node!=7&&activations<15);
            TestContext.WriteLine("B ordinary activation depth="+activations+"; A measured normal=10; ratio="+activations/10d);Assert.That(w.West.Node,Is.EqualTo(7));Assert.That(activations,Is.InRange(5,7));
        }
    }
}

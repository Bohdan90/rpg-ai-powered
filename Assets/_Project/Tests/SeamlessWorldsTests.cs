using System;
using System.Linq;
using NUnit.Framework;
using RPG.Core;
namespace RPG.Tests
{
    public class SeamlessWorldsTests
    {
        static WorldAddress A(int n)=>new WorldAddress(WorldId.Frontier,n);
        static WorldAddress B(int n)=>new WorldAddress(WorldId.StoneValley,n);
        internal static void Cycle(CrossroadsScenario w){for(int i=0;i<2;i++){if(w.HandoffPending)Assert.That(w.ContinueHandoff(w.ActiveSide),Is.True);Assert.That(w.EndActivation(w.ActiveSide),Is.True);}if(w.HandoffPending)Assert.That(w.ContinueHandoff(w.ActiveSide),Is.True);}
        internal static void Place(CrossroadsScenario w,DuelForce f,WorldAddress a){f.WorldId=a.World;f.Node=a.Node;w.Seamless.Observe();}
        [Test] public void TypedMapAndInitialFogHaveNoCrossWorldAdjacencyOrReveal()
        {
            var w=SeamlessWorlds.Create();Assert.That(A(1),Is.Not.EqualTo(B(1)));Assert.That(SeamlessWorlds.Frontier.Nodes.Count,Is.EqualTo(25));Assert.That(SeamlessWorlds.Valley.Nodes.Count,Is.EqualTo(7));
            var graph=SeamlessWorlds.Frontier;Assert.That(graph.PathCost(graph.Path(24,25)),Is.EqualTo(80));var b=SeamlessWorlds.Valley;Assert.That(b.PathCost(b.Path(1,7))+40,Is.EqualTo(120));
            Assert.That(w.Seamless.Knowledge(Side.West).Worlds,Is.EqualTo(new[]{WorldId.Frontier}));Assert.That(w.Seamless.Knowledge(Side.West).At(B(1)),Is.EqualTo(KnowledgeLevel.Unexplored));
            Place(w,w.East,B(1));Assert.That(w.ContactCost(w.West,w.East),Is.EqualTo(-1));Assert.That(w.Seamless.Knowledge(Side.West).Worlds,Is.EqualTo(new[]{WorldId.Frontier}));
        }
        [Test] public void LegalPortalKeepsIdentityPoolsBudgetsAndCostsExactlyTwenty()
        {
            var w=SeamlessWorlds.Create();var s=w.Seamless;var f=w.West;var ids=f.Formation.Members.Select(c=>c.CharacterId).ToArray();var before=f.Formation.Members.Select(c=>(c.Hp,c.Armor,c.PersonalXp,c.FireballUsed)).ToArray();decimal food=f.RealmProvisions;
            Assert.That(s.Move(Side.West,A(24)),Is.True);Assert.That(f.Tempo,Is.EqualTo(40));var hash=w.CaptureSave().checksum;var p=s.PreviewTraverse(Side.West,f.Formation.FormationId,A(24));Assert.That(p.Legal,Is.True);Assert.That(p.Destination,Is.EqualTo("Unknown destination"));Assert.That(w.CaptureSave().checksum,Is.EqualTo(hash));
            Assert.That(s.Traverse(Side.West,f.Formation.FormationId,A(24)),Is.True);Assert.That(f.Address,Is.EqualTo(B(1)));Assert.That(f.Tempo,Is.EqualTo(20));Assert.That(w.Refresh,Is.EqualTo(1));Assert.That(f.RealmProvisions,Is.EqualTo(food));Assert.That(f.Formation.Members.Select(c=>c.CharacterId),Is.EqualTo(ids));Assert.That(f.Formation.Members.Select(c=>(c.Hp,c.Armor,c.PersonalXp,c.FireballUsed)),Is.EqualTo(before));Assert.That(w.Realm.Cycles.Values.Where(c=>ids.Contains(w.Realm.Cycles.Single(k=>k.Value==c).Key)).All(c=>c.Ceiling==20),Is.True);
            Assert.That(s.Knowledge(Side.West).KnowsLink("West"),Is.True);Assert.That(s.Knowledge(Side.West).KnowsLink("East"),Is.False);Assert.That(s.Knowledge(Side.West).At(B(7)),Is.EqualTo(KnowledgeLevel.Unexplored));Assert.That(s.Knowledge(Side.East).Worlds,Is.EqualTo(new[]{WorldId.Frontier}));
            Assert.That(s.Traverse(Side.West,f.Formation.FormationId,B(1)),Is.True);Assert.That(f.Address,Is.EqualTo(A(24)));Assert.That(f.Tempo,Is.Zero);
        }
        [TestCase(false)][TestCase(true)] public void BlockedExitIsPaidWithoutOccupancyPreviewOracle(bool friendly)
        {
            var w=SeamlessWorlds.Create();var f=w.West;var s=w.Seamless;Place(w,f,A(24));var empty=s.PreviewTraverse(Side.West,f.Formation.FormationId,A(24));var blocker=friendly?w.Realm.Armies.Last(a=>a.Formation.Side==Side.West):w.East;Place(w,blocker,B(1));var occupied=s.PreviewTraverse(Side.West,f.Formation.FormationId,A(24));Assert.That(occupied.Legal,Is.EqualTo(empty.Legal));Assert.That(occupied.Destination,Is.EqualTo(empty.Destination));
            Assert.That(s.Traverse(Side.West,f.Formation.FormationId,A(24)),Is.True);Assert.That(f.Tempo,Is.EqualTo(80));Assert.That(f.Address,Is.EqualTo(A(24)));Assert.That(s.LastMessage,Does.Contain("PassageBlocked"));Assert.That(s.Knowledge(Side.West).KnowsLink("West"),Is.False);if(!friendly)Assert.That(s.Knowledge(Side.West).Worlds,Is.EqualTo(new[]{WorldId.Frontier}));
            Assert.That(w.CaptureSave().Restore().CaptureSave().checksum,Is.EqualTo(w.CaptureSave().checksum));
        }
        [TestCase(-40)][TestCase(0)][TestCase(19)] public void InvalidPortalAndStaleCommandArePure(int tempo)
        {
            var w=SeamlessWorlds.Create();Place(w,w.West,A(24));w.West.Tempo=tempo;w.Realm.LowerCeilings(w.West);string hash=w.CaptureSave().checksum;
            Assert.That(w.Seamless.Traverse(Side.West,w.West.Formation.FormationId,A(24)),Is.False);Assert.That(w.Seamless.Traverse(Side.East,w.West.Formation.FormationId,A(24)),Is.False);Assert.That(w.Seamless.Traverse(Side.West,w.West.Formation.FormationId,A(25)),Is.False);Assert.That(w.CaptureSave().checksum,Is.EqualTo(hash));
            w.West.Tempo=100;hash=w.CaptureSave().checksum;Assert.That(w.Seamless.Traverse(Side.West,w.West.Formation.FormationId,A(24),w.Seamless.Revision+1),Is.False);Assert.That(w.CaptureSave().checksum,Is.EqualTo(hash));
        }
        [Test] public void CrossWorldServicesTransferAndParticipationRemainPhysical()
        {
            var w=SeamlessWorlds.Create();var mage=w.Realm.Armies.Last(a=>a.Formation.Side==Side.West);Place(w,w.West,B(2));Place(w,w.East,B(3));Place(w,mage,A(3));w.Seamless.Observe();
            var encounter=w.Realm.PreviewEncounter(Side.West,w.East.Formation.FormationId);Assert.That(encounter.Legal,Is.True,encounter.Reason);Assert.That(encounter.Attackers,Has.No.Member(mage));
            Place(w,mage,B(6));Assert.That(w.Realm.PreviewEncounter(Side.West,w.East.Formation.FormationId).Attackers,Does.Contain(mage));
            Place(w,mage,B(1));Assert.That(w.AtOwnCity(mage),Is.False);Assert.That(w.Realm.RecruitBlocker(Side.West,UnitProfileId.HumanWarriorTI,mage.Formation.FormationId),Is.Not.Null);Assert.That(w.Realm.PreviewTransfer(Side.West,mage.Formation.FormationId,"Reserve",new[]{mage.Formation.Members[1].CharacterId}).Legal,Is.False);
        }
        [Test] public void PerStepSightStopsOnNewEnemyAndOpaqueEdgesDoNotBlockMovement()
        {
            var w=SeamlessWorlds.Create();Place(w,w.East,A(9));Assert.That(w.Seamless.IsVisibleEnemy(Side.West,w.East.Formation.FormationId),Is.False);
            Assert.That(w.Seamless.Move(Side.West,A(24)),Is.True);Assert.That(w.West.Node,Is.EqualTo(4));Assert.That(w.West.Tempo,Is.EqualTo(60));Assert.That(w.Seamless.LastMessage,Does.Contain("new hostile"));
            var z=SeamlessWorlds.Create();z.Seamless.opaque.Add(SeamlessWorlds.EdgeKey(WorldId.Frontier,3,4));Place(z,z.West,A(3));Place(z,z.East,A(4));Assert.That(z.Seamless.IsVisibleEnemy(Side.West,z.East.Formation.FormationId),Is.False);Assert.That(z.Graph.Cost(3,4),Is.EqualTo(20));
        }
        [Test] public void TemporaryRouteWhiteoutStrandedAndSaveContinuationDoNotRefill()
        {
            var w=SeamlessWorlds.Create(temporary:true);w.Seamless.Move(Side.West,A(24));w.Seamless.Traverse(Side.West,w.West.Formation.FormationId,A(24));Cycle(w);Cycle(w);Cycle(w);Assert.That(w.Refresh,Is.EqualTo(4));Assert.That(w.Seamless.Phase("West"),Does.Contain("2 full"));
            var copy=w.CaptureSave().Restore();foreach(var s in new[]{w,copy}){Cycle(s);Assert.That(s.Seamless.Phase("West"),Does.Contain("1 full"));Cycle(s);Assert.That(s.Refresh,Is.EqualTo(6));string hash=s.CaptureSave().checksum;Assert.That(s.Seamless.Traverse(Side.West,s.West.Formation.FormationId,B(1)),Is.False);Assert.That(s.CaptureSave().checksum,Is.EqualTo(hash));Assert.That(s.West.Continues,Is.True);Assert.That(s.Seamless.Knowledge(Side.West).At(B(1)),Is.EqualTo(KnowledgeLevel.CurrentlyObserved));}
            Assert.That(copy.CaptureSave().checksum,Is.EqualTo(w.CaptureSave().checksum));
        }
        [Test] public void ReadingObservationsChangesNeitherSimulationNorKnowledgeHash()
        {
            var w=SeamlessWorlds.Create();w.Seamless.Move(Side.West,A(24));w.Seamless.Traverse(Side.West,w.West.Formation.FormationId,A(24));var sim=w.Seamless.SimulationHash();var k=w.Seamless.KnowledgeHash(Side.West);w.Seamless.MarkRead(Side.West,999);Assert.That(w.Seamless.SimulationHash(),Is.EqualTo(sim));Assert.That(w.Seamless.KnowledgeHash(Side.West),Is.EqualTo(k));Assert.That(w.CaptureSave().Restore().Seamless.ReadThrough(Side.West),Is.EqualTo(w.Seamless.ReadThrough(Side.West)));
        }
        [Test] public void ReadingOneWorldDoesNotClearAnotherWorldsUnreadHistory()
        {
            var w=SeamlessWorlds.Create();var s=w.Seamless;s.Move(Side.West,A(24));s.Traverse(Side.West,w.West.Formation.FormationId,A(24));s.Traverse(Side.West,w.West.Formation.FormationId,B(1));
            var last=s.Knowledge(Side.West).History.Last();string hash=s.SimulationHash();s.MarkRead(Side.West,last.sequence,WorldId.Frontier);
            Assert.That(s.ReadThrough(Side.West,WorldId.StoneValley),Is.Zero);Assert.That(s.SimulationHash(),Is.EqualTo(hash));
            var copy=w.CaptureSave().Restore().Seamless;Assert.That(copy.ReadThrough(Side.West,WorldId.StoneValley),Is.Zero);Assert.That(copy.ReadThrough(Side.West,WorldId.Frontier),Is.EqualTo(last.sequence));
        }
        [Test] public void LabelledOpaqueProbeHasOnlyLawfulSightAndStableSave()
        {
            var w=SeamlessWorlds.CreateOpaqueProbe();Assert.That(w.ActiveSide,Is.EqualTo(Side.East));Assert.That(w.Seamless.Message(Side.East),Does.Contain("CONTROLLED"));
            Assert.That(w.Seamless.Knowledge(Side.West).KnownNodes.Where(a=>a.World==WorldId.StoneValley),Is.EquivalentTo(new[]{B(2),B(3)}));
            Assert.That(w.CaptureSave().Restore().CaptureSave().checksum,Is.EqualTo(w.CaptureSave().checksum));
        }
    }
}

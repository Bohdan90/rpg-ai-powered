using System;
using System.Linq;
using NUnit.Framework;
using RPG.Core;
namespace RPG.Tests
{
    public class SeamlessContinuityTests
    {
        static WorldAddress B(int n)=>new WorldAddress(WorldId.StoneValley,n);
        static void Place(CrossroadsScenario w,DuelForce f,int n)=>SeamlessWorldsTests.Place(w,f,B(n));
        [TestCase(Side.West)][TestCase(Side.East)]
        public void OrdinaryLegalTwoWorldMatchCanFinishAndMirrorWithoutPresentation(Side first)
        {
            var w=SeamlessWorlds.Create(first);
            // Real legal commands from the ordinary initial scenario; this is an automated match, not GUI evidence.
            for(int turn=0;turn<2;turn++){
                var side=w.ActiveSide;var army=w.Realm.Selected(side);int portal=side==Side.West?24:25;
                Assert.That(w.Seamless.Move(side,new WorldAddress(WorldId.Frontier,portal)),Is.True);
                if(army.Node!=portal)Assert.That(w.Seamless.Move(side,new WorldAddress(WorldId.Frontier,portal)),Is.True);
                Assert.That(w.Seamless.Traverse(side,army.Formation.FormationId,army.Address),Is.True);
                Assert.That(w.EndActivation(side),Is.True);Assert.That(w.ContinueHandoff(w.ActiveSide),Is.True);
            }
            var contender=w.Realm.Selected(first);
            int approach=first==Side.West?3:6;
            Assert.That(w.Seamless.Move(first,B(approach)),Is.True);
            Assert.That(w.EndActivation(first),Is.True);Assert.That(w.ContinueHandoff(w.ActiveSide),Is.True);
            var other=w.Realm.Selected(w.ActiveSide);int next=first==Side.West?6:3;
            Assert.That(w.Seamless.Move(w.ActiveSide,B(next)),Is.True);
            if(other.Node!=next)Assert.That(w.Seamless.Move(w.ActiveSide,B(next)),Is.True,"After new contact the player deliberately confirms the remaining approach.");
            Assert.That(w.Realm.Attack(w.ActiveSide,contender.Formation.FormationId),Is.True);
            Assert.That(w.RespondToContact(first,false),Is.True);
            var encounter=w.Encounter;var j=new BattleJournal(encounter.Battle.State,"07 ordinary mirrored automated match","test");
            for(int i=0;i<1800&&!j.State.Outcome.IsEnded;i++)Assert.That(j.Apply(TacticalAi.Choose(j.State).Command).IsApplied,Is.True);
            Assert.That(j.State.Outcome.IsEnded,Is.True);Assert.That(ReplayVerification.Verify(j.Header,j.Records,j.Footer()).Matches,Is.True);
            var winner=j.State.Outcome.VictorySide==Side.West?other:contender;
            Assert.That(w.ResolveBattle(j.State),Is.True);
            bool claimed=false;
            for(int i=0;i<100&&!w.Winner.HasValue;i++){
                if(w.HandoffPending)Assert.That(w.ContinueHandoff(w.ActiveSide),Is.True);
                if(w.ActiveSide==winner.Formation.Side&&!claimed){w.Realm.Select(w.ActiveSide,winner.Formation.FormationId);var p=w.Seamless.PreviewMove(w.ActiveSide,B(4));if(p.IsLegal){Assert.That(w.Seamless.Move(w.ActiveSide,B(4)),Is.True);claimed=winner.Node==4;}}
                Assert.That(w.EndActivation(w.ActiveSide),Is.True);
                if(i==5){var restored=w.CaptureSave().Restore();Assert.That(restored.CaptureSave().checksum,Is.EqualTo(w.CaptureSave().checksum));w=restored;winner=w.Realm.Army(winner.Formation.FormationId);}
            }
            Assert.That(claimed,Is.True);Assert.That(w.Winner,Is.EqualTo(winner.Formation.Side));Assert.That(w.Force(w.Winner.Value).Pressure,Is.EqualTo(24));
        }
        [Test] public void VisibleFragmentsNeverJoinAcrossHiddenNodesOrLeakSideMessages()
        {
            var w=SeamlessWorlds.Create(Side.East);var s=w.Seamless;
            foreach(var pair in new[]{(3,6),(2,1),(2,4),(2,5)})s.opaque.Add(SeamlessWorlds.EdgeKey(WorldId.StoneValley,pair.Item1,pair.Item2));
            Place(w,w.West,3);Place(w,w.East,2);Place(w,w.East,5);Place(w,w.East,1);
            int firstSequence=s.Knowledge(Side.West).History.Length;
            var initial=s.Message(Side.West);
            Assert.That(s.Move(Side.East,B(2)),Is.True);Assert.That(s.Move(Side.East,B(5)),Is.True);
            var events=s.Knowledge(Side.West).History.Skip(firstSequence).Where(e=>e.actor==w.East.Formation.FormationId).ToArray();
            Assert.That(events.Select(e=>e.kind),Is.EqualTo(new[]{"Appeared","Lost"}));
            Assert.That(events.SelectMany(e=>e.nodes),Is.All.EqualTo(2));
            Assert.That(s.Knowledge(Side.West).At(B(5)),Is.EqualTo(KnowledgeLevel.Unexplored));
            Assert.That(s.Knowledge(Side.West).LastKnown,Is.Empty,"Observed empty old location removes a current-looking marker; history remains.");
            Assert.That(s.Message(Side.West),Is.EqualTo(initial));
            Assert.That(w.CaptureSave().Restore().CaptureSave().checksum,Is.EqualTo(w.CaptureSave().checksum));
        }
        [Test] public void VisibleMovementGroupsOnlyContiguousObservedSteps()
        {
            var w=SeamlessWorlds.Create(Side.East);Place(w,w.West,3);Place(w,w.East,1);
            int before=w.Seamless.Knowledge(Side.West).History.Length;
            Assert.That(w.Seamless.Move(Side.East,B(5)),Is.True);
            var events=w.Seamless.Knowledge(Side.West).History.Skip(before).ToArray();
            Assert.That(events.Length,Is.EqualTo(1));Assert.That(events[0].kind,Is.EqualTo("Move"));Assert.That(events[0].nodes,Is.EqualTo(new[]{1,2,5}));
        }
        [Test] public void ValleyRefreshUsesOneFiniteCacheAndGlobalTickAfterReload()
        {
            var w=SeamlessWorlds.Create();Place(w,w.West,3);w.West.RealmProvisions=0;
            var copy=w.CaptureSave().Restore();
            foreach(var z in new[]{w,copy}){
                Assert.That(z.EndActivation(Side.West),Is.True);Assert.That(z.Refresh,Is.EqualTo(1));Assert.That(z.Seamless.NorthFood,Is.EqualTo(24));
                Assert.That(z.ContinueHandoff(Side.East),Is.True);Assert.That(z.EndActivation(Side.East),Is.True);
                Assert.That(z.Refresh,Is.EqualTo(2));Assert.That(z.Seamless.NorthFood,Is.EqualTo(18));Assert.That(z.West.RealmProvisions,Is.EqualTo(6));
                Assert.That(z.Seamless.SouthFood,Is.EqualTo(24));
            }
            Assert.That(copy.CaptureSave().checksum,Is.EqualTo(w.CaptureSave().checksum));
        }
        [Test] public void RealValleyBattleReplayReturnsSameIdsOnceWithoutRefresh()
        {
            var w=SeamlessWorlds.Create();Place(w,w.West,2);Place(w,w.East,3);
            var ids=w.West.Formation.Members.Select(c=>c.CharacterId).ToArray();
            Assert.That(w.Realm.Attack(Side.West,w.East.Formation.FormationId),Is.True);
            Assert.That(w.RespondToContact(Side.East,false),Is.True);
            Assert.That(w.Encounter.WorldId,Is.EqualTo(WorldId.StoneValley));
            var j=new BattleJournal(w.Encounter.Battle.State,"07-Valley","test");
            for(int i=0;i<1800&&!j.State.Outcome.IsEnded;i++)Assert.That(j.Apply(TacticalAi.Choose(j.State).Command).IsApplied,Is.True);
            Assert.That(j.State.Outcome.IsEnded,Is.True);Assert.That(ReplayVerification.Verify(j.Header,j.Records,j.Footer()).Matches,Is.True);
            Assert.That(w.ResolveBattle(j.State),Is.True);Assert.That(w.ResolveBattle(j.State),Is.False);
            Assert.That(w.Refresh,Is.EqualTo(1));Assert.That(w.West.WorldId,Is.EqualTo(WorldId.StoneValley));Assert.That(w.East.WorldId,Is.EqualTo(WorldId.StoneValley));
            Assert.That(w.West.Formation.Members.Select(c=>c.CharacterId),Is.EqualTo(ids));Assert.That(w.Realm.LastBattle.world,Is.EqualTo(1));
            Assert.That(w.CaptureSave().Restore().CaptureSave().checksum,Is.EqualTo(w.CaptureSave().checksum));
        }
        [Test] public void CorruptKnowledgeAndCrossWorldDuplicateIdentityCannotReplaceLiveWorld()
        {
            var w=SeamlessWorlds.Create();string hash=w.CaptureSave().checksum;
            var data=w.CaptureSave();data.seamless.west.observed=data.seamless.west.observed.Skip(1).ToArray();data.checksum=data.ComputeHash();
            Assert.Throws<System.IO.InvalidDataException>(()=>data.Restore());Assert.That(w.CaptureSave().checksum,Is.EqualTo(hash));
            data=w.CaptureSave();data.realm.armies[1].units[0].id=data.realm.armies[0].units[0].id;data.checksum=data.ComputeHash();
            Assert.Throws<System.IO.InvalidDataException>(()=>data.Restore());Assert.That(w.CaptureSave().checksum,Is.EqualTo(hash));
            var old=new CrossroadsScenario(realm:true).CaptureSave();Assert.That(old.version,Is.EqualTo(5));Assert.That(old.Restore().CaptureSave().checksum,Is.EqualTo(old.checksum));
        }
    }
}

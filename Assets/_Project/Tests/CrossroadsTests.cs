using System;
using System.Linq;
using NUnit.Framework;
using RPG.Core;

namespace RPG.Tests
{
    public class CrossroadsTests
    {
        public static CrossroadsScenario Fixture(Action<CrossroadsSaveData> edit=null)
        {var d=new CrossroadsScenario().CaptureSave();edit?.Invoke(d);d.checksum=d.ComputeHash();return d.Restore();}
        public static void End(CrossroadsScenario s){Assert.That(s.EndActivation(s.ActiveSide),Is.True);if(s.HandoffPending)Assert.That(s.ContinueHandoff(s.ActiveSide),Is.True);}
        [TestCase(Side.West)][TestCase(Side.East)] public void StartingSideAuthorityAndOneGlobalRefresh(Side first)
        {
            var s=new CrossroadsScenario(first);string h=s.CaptureSave().checksum;
            Assert.That(s.Move(CrossroadsScenario.Other(first),12),Is.False);Assert.That(s.CaptureSave().checksum,Is.EqualTo(h));
            Assert.That(s.EndActivation(first),Is.True);Assert.That(s.Refresh,Is.EqualTo(1));Assert.That(s.CompletedActivations,Is.EqualTo(1));
            h=s.CaptureSave().checksum;Assert.That(s.EndActivation(s.ActiveSide),Is.False);Assert.That(s.Move(s.ActiveSide,7),Is.False);Assert.That(s.CaptureSave().checksum,Is.EqualTo(h));
            Assert.That(s.ContinueHandoff(first),Is.False);Assert.That(s.ContinueHandoff(s.ActiveSide),Is.True);End(s);
            Assert.That(s.Refresh,Is.EqualTo(2));Assert.That(s.ActiveSide,Is.EqualTo(first));Assert.That(s.West.Provisions,Is.EqualTo(30));
        }
        [Test] public void GraphSymmetryAndPreviewPurity()
        {
            var s=new CrossroadsScenario();var mirror=new[]{0,13,12,11,9,10,6,7,8,4,5,3,2,1};
            foreach(var e in CrossroadsScenario.Map.Edges)Assert.That(CrossroadsScenario.Map.Cost(mirror[e.A],mirror[e.B]),Is.EqualTo(e.Tempo));
            string h=s.CaptureSave().checksum;Assert.That(s.PreviewMove(Side.West,6).Cost,Is.EqualTo(80));s.PreviewMove(Side.West,13);Assert.That(s.Move(Side.West,13),Is.False);Assert.That(s.CaptureSave().checksum,Is.EqualTo(h));
        }
        [Test] public void ClaimPersistsAndRecaptureScoresOnlyAfterBothSides()
        {
            var s=Fixture(d=>{d.west.node=6;d.east.node=9;});End(s);Assert.That(s.Owner(6),Is.EqualTo(Side.West));Assert.That(s.West.Pressure,Is.Zero);
            End(s);Assert.That(s.West.Pressure,Is.EqualTo(1));Assert.That(s.Move(Side.West,4),Is.True);End(s);
            Assert.That(s.Owner(6),Is.EqualTo(Side.West));Assert.That(s.Move(Side.East,6),Is.True);End(s);Assert.That(s.Owner(6),Is.EqualTo(Side.East));Assert.That(s.East.Pressure,Is.EqualTo(1));
        }
        [Test] public void PressureTieContinuesUntilLeader()
        {
            var s=Fixture(d=>{d.west.pressure=7;d.east.pressure=7;d.owners=new[]{(int)Side.West,-1,(int)Side.East};});End(s);End(s);
            Assert.That(s.Winner,Is.Null);Assert.That(s.West.Pressure,Is.EqualTo(8));Assert.That(s.Move(Side.West,7),Is.True);End(s);End(s);Assert.That(s.Winner,Is.EqualTo(Side.West));
        }
        [TestCase(100,50)][TestCase(49,0)][TestCase(0,0)][TestCase(-1,-1)] public void ContactAttackCostAndDefenderNoCost(int tempo,int after)
        {
            var s=Fixture(d=>{d.west.node=7;d.east.node=11;d.west.tempo=tempo;});string h=s.CaptureSave().checksum;
            Assert.That(s.Attack(Side.West),Is.EqualTo(tempo>=0));Assert.That(s.West.Tempo,Is.EqualTo(after));Assert.That(s.East.Tempo,Is.EqualTo(100));
            if(tempo<0)Assert.That(s.CaptureSave().checksum,Is.EqualTo(h));else Assert.Throws<InvalidOperationException>(()=>s.CaptureSave());
        }
        [Test] public void StrategicWithdrawalDebtAndRefresh()
        {
            var s=Fixture(d=>{d.west.node=7;d.east.node=11;d.west.tempo=0;});int dest=s.RetreatDestination(Side.West,7,11);
            Assert.That(s.Withdraw(Side.West),Is.True);Assert.That(s.West.Node,Is.EqualTo(dest));Assert.That(CrossroadsScenario.Map.Hops(7,dest),Is.LessThanOrEqualTo(2));Assert.That(s.West.Tempo,Is.EqualTo(-40));
            Assert.That(s.Withdraw(Side.West),Is.False);End(s);End(s);Assert.That(s.West.Tempo,Is.EqualTo(60));
        }
        public static BattleState Evacuate(CrossroadsScenario s,Side escaping)
        {
            var j=new BattleJournal(s.Encounter.Battle.State,"duel-test","test");
            for(int n=0;n<500&&!j.State.Outcome.IsEnded;n++)
            {
                var u=j.State.FindUnit(j.State.CurrentUnitId.Value);BattleCommand command=new EndActivationCommand(u.Id);
                if(u.Side==escaping&&u.MovementRemaining>0)
                {
                    var route=Enumerable.Range(0,17).Select(y=>Pathfinder.FindPath(j.State,u.Id,new GridPosition(escaping==Side.West?0:22,y))).Where(r=>r.Found&&r.Cost>0).OrderBy(r=>r.Cost).FirstOrDefault();
                    if(route!=null)command=new MoveCommand(u.Id,route.Steps);
                }
                Assert.That(j.Apply(command).IsApplied,Is.True);
            }
            Assert.That(j.State.Outcome.IsEnded,Is.True);Assert.That(ReplayVerification.Verify(j.Header,j.Records,j.Footer()).Matches,Is.True);return j.State;
        }
        [TestCase(Side.West)][TestCase(Side.East)] public void PhysicalWithdrawalPlacementIdsXpAndNoExitHealing(Side escaping)
        {
            var s=Fixture(d=>{d.west.node=7;d.east.node=11;d.west.formation.members[1].hp=17;d.west.formation.members[1].armor=5;});
            var ids=s.West.Formation.Members.Select(c=>c.CharacterId).ToArray();s.Attack(Side.West);var result=Evacuate(s,escaping);
            Assert.That(s.ResolveBattle(result),Is.True);Assert.That(s.ResolveBattle(result),Is.False);Assert.That(s.West.Formation.Members.Select(c=>c.CharacterId),Is.EqualTo(ids));
            Assert.That(s.West.Formation.Members[1].Hp,Is.EqualTo(17));Assert.That(s.West.Formation.Members[1].Armor,Is.EqualTo(5));
            if(escaping==Side.East){Assert.That(s.West.Node,Is.EqualTo(11));Assert.That(s.East.Tempo,Is.EqualTo(60));Assert.That(s.West.Formation.Commander.PersonalXp,Is.GreaterThan(0));}
            else{Assert.That(s.East.Node,Is.EqualTo(11));Assert.That(s.West.Tempo,Is.EqualTo(10));}
            Assert.That(s.CaptureSave().Restore().CaptureSave().checksum,Is.EqualTo(s.CaptureSave().checksum));
        }
        [Test] public void CommanderlessDeadRecordsPersistAndDoNotDeploy()
        {
            var s=Fixture(d=>{d.west.node=7;d.east.node=11;var c=d.west.formation.members[0];c.hp=0;c.status=(int)PersistentCharacterStatus.Dead;d.west.formation.commanderless=true;d.west.formation.rosterLocked=true;});
            Assert.That(s.West.Formation.Commanderless,Is.True);s.Attack(Side.West);Assert.That(s.Encounter.Battle.State.Units.Count(u=>u.Side==Side.West),Is.EqualTo(5));
        }
        [TestCase(false)][TestCase(true)] public void SaveContinuationInitialAndBetweenActivations(bool mid)
        {
            var s=new CrossroadsScenario();s.Move(Side.West,6);if(mid)s.EndActivation(Side.West);
            var loaded=s.CaptureSave().Restore();Assert.That(loaded.CaptureSave().checksum,Is.EqualTo(s.CaptureSave().checksum));
            foreach(var w in new[]{s,loaded}){if(w.HandoffPending)w.ContinueHandoff(w.ActiveSide);End(w);End(w);}
            Assert.That(loaded.CaptureSave().checksum,Is.EqualTo(s.CaptureSave().checksum));
        }
        [Test] public void InvalidSaveIsIsolatedFromLiveState()
        {var s=new CrossroadsScenario();string h=s.CaptureSave().checksum;var d=s.CaptureSave();d.west.node=999;Assert.Throws<System.IO.InvalidDataException>(()=>d.Restore());Assert.That(s.CaptureSave().checksum,Is.EqualTo(h));}
        [Test] public void CompletePressureMatchAndMirroredResult()
        {
            foreach(var first in new[]{Side.West,Side.East})
            {var s=new CrossroadsScenario(first);s.Move(first,6);End(s);s.Move(s.ActiveSide,8);End(s);s.Move(first,7);End(s);End(s);
                while(!s.Winner.HasValue&&s.Refresh<10)End(s);Assert.That(s.Winner,Is.EqualTo(first));Assert.That(s.Refresh,Is.EqualTo(6));}
        }
    }
}

using System.Linq;
using NUnit.Framework;
using RPG.Core;
namespace RPG.Tests
{
    public class SiegeApproachTests
    {
        [TestCase(SizeExperimentMap.Siege_39x37_West_9v9)]
        [TestCase(SizeExperimentMap.Siege_39x37_East_9v9)]
        [TestCase(SizeExperimentMap.Siege_39x37_North_9v9)]
        [TestCase(SizeExperimentMap.Siege_39x37_South_9v9)]
        [TestCase(SizeExperimentMap.Siege_39x37_WestEast_18v9)]
        [TestCase(SizeExperimentMap.Siege_39x37_NorthSouth_18v9)]
        public void DirectionalSectorsFitWithoutOverlapOutsideBowReach(SizeExperimentMap map)
        {
            var b=SizeExperimentFixture.Board(map);var units=SizeExperimentFixture.Units(map);
            Assert.That(b.Columns,Is.EqualTo(39));Assert.That(b.Rows,Is.EqualTo(37));
            CollectionAssert.AreEqual(SizeExperimentFixture.Board(SizeExperimentMap.Siege_35x27_Full_9v9).SolidCells.Select(p=>new GridPosition(p.X+2,p.Y+5)),b.SolidCells);
            var edges=DirectionalSiegeFixture.Approaches(map);
            Assert.That(units.Length,Is.EqualTo(9*(edges.Length+1)));
            Assert.That(units.Select(u=>u.Id).Distinct().Count(),Is.EqualTo(units.Length));
            Assert.That(units.Select(u=>u.Position).Distinct().Count(),Is.EqualTo(units.Length));
            Assert.That(units.All(u=>b.IsWalkable(u.Position)&&!b.IsRetreatZone(u,u.Position)),Is.True);
            var sectors=edges.SelectMany(DirectionalSiegeFixture.DeploymentCells).ToArray();
            Assert.That(sectors.Distinct().Count(),Is.EqualTo(27*edges.Length));
            foreach(var edge in edges)
            {
                var cells=DirectionalSiegeFixture.DeploymentCells(edge).ToArray();
                Assert.That(cells.All(b.IsWalkable),Is.True);
                var army=units.Where(u=>u.OwnRetreatEdge==edge).ToArray();
                Assert.That(army.Length,Is.EqualTo(9));Assert.That(army.All(u=>cells.Contains(u.Position)),Is.True);
                Assert.That(DirectionalSiegeFixture.MinimumWallSeparation(edge),Is.EqualTo(11));
                Assert.That(DirectionalSiegeFixture.MinimumWallSeparation(edge),Is.GreaterThan(UnitProfile.HumanArcherTI.Range));
            }
            Assert.That(units.Where(u=>u.Side==Side.East).Count(),Is.EqualTo(9));
            Assert.That(units.Where(u=>u.Side==Side.East).All(u=>u.Position.X>14&&u.Position.X<24&&u.Position.Y>14&&u.Position.Y<22),Is.True);
            CollectionAssert.AreEqual(units.Select(u=>u.Position),SizeExperimentFixture.Units(map).Select(u=>u.Position));
        }
        [TestCase(RetreatEdge.West,1,18,0,18,12,18,16,18)]
        [TestCase(RetreatEdge.East,37,18,38,18,26,18,22,18)]
        [TestCase(RetreatEdge.North,19,35,19,36,19,24,19,20)]
        [TestCase(RetreatEdge.South,19,1,19,0,19,12,19,16)]
        public void OwnRetreatAndCrossingAreExecutableAndReplayable(RetreatEdge edge,int x,int y,int ex,int ey,int ax,int ay,int tx,int ty)
        {
            var b=SizeExperimentFixture.Board(SizeExperimentMap.Siege_39x37_West_9v9);
            var actor=new UnitState(new UnitId(5),Side.West,UnitProfile.ElfWarriorTI,new GridPosition(x,y),Facing.East,ownRetreatEdge:edge);
            var enemy=BattleTestFixtures.Unit(7,UnitProfile.HumanWarriorTI,Side.East,20,18);
            var s=BattleResolver.StartBattle(new[]{actor,enemy},2,b).State;
            foreach(var exit in new[]{new GridPosition(0,18),new GridPosition(38,18),new GridPosition(19,0),new GridPosition(19,36)})
            { Assert.That(b.IsRetreatZone(enemy,exit),Is.True);Assert.That(b.IsRetreatZone(actor,exit),Is.EqualTo(exit==new GridPosition(ex,ey))); }
            var journal=new BattleJournal(s,"directional","test");
            Assert.That(journal.Apply(new MoveCommand(actor.Id,new[]{new GridPosition(ex,ey)})).State.FindUnit(actor.Id).Status,Is.EqualTo(UnitStatus.Escaped));
            Assert.That(ReplayVerification.Verify(journal.Header,journal.Records,journal.Footer()).Matches,Is.True);
            actor=new UnitState(actor.Id,Side.West,actor.Profile,new GridPosition(ax,ay),actor.Facing,ownRetreatEdge:edge);
            s=BattleResolver.StartBattle(new[]{actor,enemy},2,b).State;
            var path=Pathfinder.FindPath(s,actor.Id,new GridPosition(tx,ty));Assert.That(path.Found,Is.True);
            CollectionAssert.AreEqual(path.Steps,Pathfinder.FindPath(s,actor.Id,new GridPosition(tx,ty)).Steps);
            Assert.That(BattleResolver.Apply(s,new MoveCommand(actor.Id,path.Steps)).IsApplied,Is.True);
        }
        [TestCase(SizeExperimentMap.Siege_39x37_West_9v9)]
        [TestCase(SizeExperimentMap.Siege_39x37_East_9v9)]
        [TestCase(SizeExperimentMap.Siege_39x37_North_9v9)]
        [TestCase(SizeExperimentMap.Siege_39x37_South_9v9)]
        public void AiLeavesEachApproachWithLegalCommands(SizeExperimentMap map)
        {
            var s=BattleResolver.StartBattle(SizeExperimentFixture.Units(map),20260921,SizeExperimentFixture.Board(map)).State;
            var initial=s.FindUnit(new UnitId(5)).Position;int ended=0;
            for(int i=0;i<12&&ended<2;i++)
            {var actor=s.CurrentUnitId;var result=BattleResolver.Apply(s,TacticalAi.Choose(s).Command);Assert.That(result.IsApplied,Is.True);s=result.State;if(s.CurrentUnitId!=actor)ended++;}
            Assert.That(ended,Is.EqualTo(2));Assert.That(s.FindUnit(new UnitId(5)).Position.DistanceTo(new GridPosition(19,18)),Is.LessThan(initial.DistanceTo(new GridPosition(19,18))));
        }
    }
}

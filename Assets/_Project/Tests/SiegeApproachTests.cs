using System.Linq;
using NUnit.Framework;
using RPG.Core;
namespace RPG.Tests
{
    public class SiegeApproachTests
    {
        [TestCase(SizeExperimentMap.Siege_41x39_West_9v9)]
        [TestCase(SizeExperimentMap.Siege_41x39_East_9v9)]
        [TestCase(SizeExperimentMap.Siege_41x39_North_9v9)]
        [TestCase(SizeExperimentMap.Siege_41x39_South_9v9)]
        [TestCase(SizeExperimentMap.Siege_41x39_WestEast_18v9)]
        [TestCase(SizeExperimentMap.Siege_41x39_NorthSouth_18v9)]
        public void DirectionalSectorsFitWithoutOverlapOutsideBowReach(SizeExperimentMap map)
        {
            var b=SizeExperimentFixture.Board(map);var units=SizeExperimentFixture.Units(map);
            Assert.That(b.Columns,Is.EqualTo(41));Assert.That(b.Rows,Is.EqualTo(39));
            CollectionAssert.AreEqual(SizeExperimentFixture.Board(SizeExperimentMap.Siege_35x27_Full_9v9).SolidCells.Select(p=>new GridPosition(p.X+3,p.Y+6)),b.SolidCells);
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
                Assert.That(DirectionalSiegeFixture.MinimumWallSeparation(edge),Is.EqualTo(12));
                Assert.That(DirectionalSiegeFixture.MinimumWallSeparation(edge),Is.GreaterThan(UnitProfile.HumanArcherTI.Range));
            }
            Assert.That(units.Where(u=>u.Side==Side.East).Count(),Is.EqualTo(9));
            Assert.That(units.Where(u=>u.Side==Side.East).All(u=>u.Position.X>15&&u.Position.X<25&&u.Position.Y>15&&u.Position.Y<23),Is.True);
            CollectionAssert.AreEqual(units.Select(u=>u.Position),SizeExperimentFixture.Units(map).Select(u=>u.Position));
        }
        [TestCase(RetreatEdge.West,1,19,0,19,13,19,17,19)]
        [TestCase(RetreatEdge.East,39,19,40,19,27,19,23,19)]
        [TestCase(RetreatEdge.North,20,37,20,38,20,25,20,21)]
        [TestCase(RetreatEdge.South,20,1,20,0,20,13,20,17)]
        public void OwnRetreatAndCrossingAreExecutableAndReplayable(RetreatEdge edge,int x,int y,int ex,int ey,int ax,int ay,int tx,int ty)
        {
            var b=SizeExperimentFixture.Board(SizeExperimentMap.Siege_41x39_West_9v9);
            var actor=new UnitState(new UnitId(5),Side.West,UnitProfile.ElfWarriorTI,new GridPosition(x,y),Facing.East,ownRetreatEdge:edge);
            var enemy=BattleTestFixtures.Unit(7,UnitProfile.HumanWarriorTI,Side.East,21,19);
            var s=BattleResolver.StartBattle(new[]{actor,enemy},2,b).State;
            foreach(var exit in new[]{new GridPosition(0,19),new GridPosition(40,19),new GridPosition(20,0),new GridPosition(20,38)})
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
        [TestCase(SizeExperimentMap.Siege_41x39_West_9v9)]
        [TestCase(SizeExperimentMap.Siege_41x39_East_9v9)]
        [TestCase(SizeExperimentMap.Siege_41x39_North_9v9)]
        [TestCase(SizeExperimentMap.Siege_41x39_South_9v9)]
        public void AiLeavesEachApproachWithLegalCommands(SizeExperimentMap map)
        {
            var s=BattleResolver.StartBattle(SizeExperimentFixture.Units(map),20260921,SizeExperimentFixture.Board(map)).State;
            // Stronger ranged contact can justify Defend on the first activation.
            // The same attacker must still approach within two of its activations.
            var attacker=new UnitId(5);var initial=s.FindUnit(attacker).Position;int ended=0;
            for(int i=0;i<216&&ended<2;i++)
            {var actor=s.CurrentUnitId;var result=BattleResolver.Apply(s,TacticalAi.Choose(s).Command);Assert.That(result.IsApplied,Is.True);s=result.State;if(actor==attacker&&s.CurrentUnitId!=actor)ended++;}
            Assert.That(ended,Is.EqualTo(2));Assert.That(s.FindUnit(new UnitId(5)).Position.DistanceTo(new GridPosition(20,19)),Is.LessThan(initial.DistanceTo(new GridPosition(20,19))));
        }
    }
}

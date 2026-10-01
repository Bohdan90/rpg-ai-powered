using System.Linq;
using NUnit.Framework;
using RPG.Core;

namespace RPG.Tests
{
    public class DensityFixtureTests
    {
        [TestCase(SizeExperimentMap.Field_23x17_Full_9v9,23,17)]
        [TestCase(SizeExperimentMap.Siege_35x27_Full_9v9,35,27)]
        public void DeploymentIsUniqueDeterministicAndHasNinePerSide(SizeExperimentMap map,int width,int height)
        {
            var b=SizeExperimentFixture.Board(map); var units=SizeExperimentFixture.Units(map);
            Assert.That(b.Columns,Is.EqualTo(width)); Assert.That(b.Rows,Is.EqualTo(height));
            Assert.That(units.Select(u=>u.Id).Distinct().Count(),Is.EqualTo(18));
            Assert.That(units.Select(u=>u.Position).Distinct().Count(),Is.EqualTo(18));
            Assert.That(units.All(u=>b.IsWalkable(u.Position)&&!b.IsRetreatZone(u.Side,u.Position)),Is.True);
            foreach(var side in new[]{Side.West,Side.East})
            {
                var army=units.Where(u=>u.Side==side).ToArray(); Assert.That(army.Length,Is.EqualTo(9));
                Assert.That(army.Count(u=>u.Profile==UnitProfile.HumanWarriorTI),Is.EqualTo(4));
                Assert.That(army.Count(u=>u.Profile==UnitProfile.HumanArcherTI),Is.EqualTo(3));
                Assert.That(army.Count(u=>u.Profile==UnitProfile.ElfWarriorTI),Is.EqualTo(2));
            }
            var a=BattleResolver.StartBattle(units,20260921,b).State;
            var repeat=BattleResolver.StartBattle(SizeExperimentFixture.Units(map),20260921,SizeExperimentFixture.Board(map)).State;
            Assert.That(BattleTestFixtures.Snapshot(a),Is.EqualTo(BattleTestFixtures.Snapshot(repeat)));
        }

        [Test]
        public void GeometryIsCenteredFieldOrUnchangedFullSiege()
        {
            CollectionAssert.AreEqual(new[]{new GridPosition(11,7),new GridPosition(11,8),new GridPosition(11,9)},
                SizeExperimentFixture.Board(SizeExperimentMap.Field_23x17_Full_9v9).SolidCells);
            CollectionAssert.AreEqual(SizeExperimentFixture.Board(SizeExperimentMap.Siege_35x27_Large).SolidCells,
                SizeExperimentFixture.Board(SizeExperimentMap.Siege_35x27_Full_9v9).SolidCells);
        }

        [TestCase(SizeExperimentMap.Field_23x17_Full_9v9)]
        [TestCase(SizeExperimentMap.Siege_35x27_Full_9v9)]
        public void EveryInitialUnitHasDeterministicExecutableMovement(SizeExperimentMap map)
        {
            var initial=BattleResolver.StartBattle(SizeExperimentFixture.Units(map),20260921,SizeExperimentFixture.Board(map)).State;
            foreach(var unit in initial.Units)
            {
                var state=BattleTestFixtures.ToActor(initial,unit.Id); var before=BattleTestFixtures.Snapshot(state);
                var destinations=from x in Enumerable.Range(-1,3) from y in Enumerable.Range(-1,3)
                    where x!=0||y!=0 select new GridPosition(unit.Position.X+x,unit.Position.Y+y);
                var path=destinations.Select(p=>Pathfinder.FindPath(state,unit.Id,p)).FirstOrDefault(p=>p.Found);
                Assert.That(path,Is.Not.Null,"No initial route for "+unit.Id);
                CollectionAssert.AreEqual(path.Steps,Pathfinder.FindPath(state,unit.Id,path.Steps.Last()).Steps);
                Assert.That(BattleTestFixtures.Snapshot(state),Is.EqualTo(before));
                Assert.That(BattleResolver.Apply(state,new MoveCommand(unit.Id,path.Steps)).IsApplied,Is.True);
            }
        }

        [TestCase(3,0)]
        [TestCase(8,22)]
        public void FieldArcherCanPhysicallyRetreatWithoutEndingArmyBattle(int id,int edge)
        {
            var map=SizeExperimentMap.Field_23x17_Full_9v9;
            var state=BattleTestFixtures.ToActor(BattleResolver.StartBattle(SizeExperimentFixture.Units(map),2,SizeExperimentFixture.Board(map)).State,new UnitId(id));
            var unit=state.FindUnit(new UnitId(id));
            var path=Pathfinder.FindPath(state,unit.Id,new GridPosition(edge,unit.Position.Y));
            Assert.That(path.Found,Is.True);
            var result=BattleResolver.Apply(state,new MoveCommand(unit.Id,path.Steps));
            Assert.That(result.IsApplied,Is.True);Assert.That(result.State.FindUnit(unit.Id).Status,Is.EqualTo(UnitStatus.Escaped));
            Assert.That(result.State.FindUnit(unit.Id).Hp,Is.EqualTo(unit.Hp));Assert.That(result.State.Outcome.IsEnded,Is.False);
        }

        [Test]
        public void NineDefenderFormationAllowsPhysicalNorthEscapeThroughCrossing()
        {
            var map=SizeExperimentMap.Siege_35x27_Full_9v9; var board=SizeExperimentFixture.Board(map);
            var id=new UnitId(10);
            var state=BattleTestFixtures.ToActor(BattleResolver.StartBattle(SizeExperimentFixture.Units(map),2,board).State,id);
            foreach(var destination in new[]{new GridPosition(17,16),new GridPosition(17,20),new GridPosition(17,26)})
            {
                state=BattleTestFixtures.ToActor(state,id);
                var path=Pathfinder.FindPath(state,id,destination); Assert.That(path.Found,Is.True);
                var result=BattleResolver.Apply(state,new MoveCommand(id,path.Steps));Assert.That(result.IsApplied,Is.True);state=result.State;
                if(state.FindUnit(id).IsActive) state=BattleResolver.Apply(state,new EndActivationCommand(id)).State;
            }
            Assert.That(state.FindUnit(id).Status,Is.EqualTo(UnitStatus.Escaped));Assert.That(state.Outcome.IsEnded,Is.False);
            foreach(var p in new[]{new GridPosition(0,13),new GridPosition(34,13),new GridPosition(17,0),new GridPosition(17,26)})
            {
                Assert.That(board.IsRetreatZone(Side.East,p),Is.True);
                Assert.That(board.IsRetreatZone(Side.West,p),Is.EqualTo(p.X==0));
            }
        }
    }
}

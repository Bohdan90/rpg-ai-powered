using System.Linq;
using NUnit.Framework;
using RPG.Core;

namespace RPG.Tests
{
    public class SizeExperimentTests
    {
        [TestCase(SizeExperimentMap.Field_13x9_Control,13,9)]
        [TestCase(SizeExperimentMap.Field_17x11_Expanded,17,11)]
        [TestCase(SizeExperimentMap.Field_19x13_ExpandedV2,19,13)]
        [TestCase(SizeExperimentMap.Siege_23x17_Tight,23,17)]
        [TestCase(SizeExperimentMap.Siege_27x21_Roomy,27,21)]
        public void FixturesHaveValidDeterministicDeployment(SizeExperimentMap map,int width,int height)
        {
            var board=SizeExperimentFixture.Board(map); var units=SizeExperimentFixture.Units(map);
            Assert.That(board.Columns,Is.EqualTo(width)); Assert.That(board.Rows,Is.EqualTo(height));
            Assert.That(units.Length,Is.EqualTo(10)); Assert.That(units.Select(u=>u.Position).Distinct().Count(),Is.EqualTo(10));
            Assert.That(units.All(u=>board.IsWalkable(u.Position)&&!board.IsRetreatZone(u.Side,u.Position)),Is.True);
            Assert.That(board.SolidCells.All(board.Contains),Is.True);
            CollectionAssert.AreEqual(units.Select(u=>u.Position),SizeExperimentFixture.Units(map).Select(u=>u.Position));
            var state=BattleResolver.StartBattle(units,20260921,board).State;
            var actor=state.FindUnit(state.CurrentUnitId.Value);
            var destination=new GridPosition(actor.Position.X+1,actor.Position.Y+1);
            var path=Pathfinder.FindPath(state,actor.Id,destination);
            Assert.That(path.Found,Is.True);
            CollectionAssert.AreEqual(path.Steps,Pathfinder.FindPath(state,actor.Id,destination).Steps);
            Assert.That(BattleResolver.Apply(state,new MoveCommand(actor.Id,path.Steps)).IsApplied,Is.True);
        }
        [Test]
        public void ExpandedV2KeepsRearDeploymentAndCenteredThreeCellWall()
        {
            var map = SizeExperimentMap.Field_19x13_ExpandedV2;
            var board = SizeExperimentFixture.Board(map);
            CollectionAssert.AreEqual(new[] { new GridPosition(9,5), new GridPosition(9,6), new GridPosition(9,7) }, board.SolidCells);
            CollectionAssert.AreEqual(new[] { new GridPosition(2,6), new GridPosition(2,5), new GridPosition(1,4), new GridPosition(1,8), new GridPosition(2,7),
                new GridPosition(16,6), new GridPosition(16,5), new GridPosition(17,4), new GridPosition(17,8), new GridPosition(16,7) },
                SizeExperimentFixture.Units(map).Select(u => u.Position));
            Assert.That(board.EastRetreatUsesPerimeter, Is.False);
            for (int y = 0; y < board.Rows; y++)
            {
                Assert.That(board.IsRetreatZone(Side.West, new GridPosition(0,y)), Is.True);
                Assert.That(board.IsRetreatZone(Side.East, new GridPosition(18,y)), Is.True);
            }
            Assert.That(board.IsRetreatZone(Side.East, new GridPosition(9,0)), Is.False);
        }
        [Test]
        public void ControlPreservesOriginalWallAndProfiles()
        {
            CollectionAssert.AreEqual(Battlefield.BaseMap.SolidCells,SizeExperimentFixture.Board(SizeExperimentMap.Field_13x9_Control).SolidCells);
            var baseline=SizeExperimentFixture.Units(SizeExperimentMap.Field_13x9_Control);
            foreach(SizeExperimentMap map in System.Enum.GetValues(typeof(SizeExperimentMap)))
                CollectionAssert.AreEqual(baseline.Select(u=>u.Profile),SizeExperimentFixture.Units(map).Select(u=>u.Profile));
        }
        [Test]
        public void SiegeProxyIsIdenticalAndHasFourThreeCellOpenings()
        {
            var a=SizeExperimentFixture.Board(SizeExperimentMap.Siege_23x17_Tight);
            var b=SizeExperimentFixture.Board(SizeExperimentMap.Siege_27x21_Roomy);
            CollectionAssert.AreEqual(a.SolidCells.Select(p=>new GridPosition(p.X-a.Columns/2,p.Y-a.Rows/2)),
                b.SolidCells.Select(p=>new GridPosition(p.X-b.Columns/2,p.Y-b.Rows/2)));
            Assert.That(a.SolidCells.Count,Is.EqualTo(24));
            for(int d=-1;d<=1;d++)
            foreach(var p in new[]{new GridPosition(11-5,8+d),new GridPosition(11+5,8+d),new GridPosition(11+d,8-4),new GridPosition(11+d,8+4)})
                Assert.That(a.IsWalkable(p),Is.True);
            Assert.That(a.IsSolid(new GridPosition(6,6)),Is.True);
        }
        [TestCase(SizeExperimentMap.Siege_23x17_Tight)]
        [TestCase(SizeExperimentMap.Siege_27x21_Roomy)]
        public void DefenderCanEscapeEveryOuterEdgeWhileAttackerOnlyWest(SizeExperimentMap map)
        {
            var b=SizeExperimentFixture.Board(map);
            var exits=new[]{new GridPosition(0,2),new GridPosition(b.Columns-1,2),new GridPosition(2,0),new GridPosition(2,b.Rows-1)};
            foreach(var exit in exits)
            {
                Assert.That(b.IsRetreatZone(Side.East,exit),Is.True);
                Assert.That(b.IsRetreatZone(Side.West,exit),Is.EqualTo(exit.X==0));
                var from=new GridPosition(exit.X==0?1:exit.X==b.Columns-1?exit.X-1:exit.X,
                    exit.Y==0?1:exit.Y==b.Rows-1?exit.Y-1:exit.Y);
                var east=new UnitState(new UnitId(10),Side.East,UnitProfile.ElfWarriorTI,from,Facing.North,hp:20,armor:3);
                var west=new UnitState(new UnitId(1),Side.West,UnitProfile.HumanWarriorTI,new GridPosition(3,3),Facing.East);
                var state=BattleResolver.StartBattle(new[]{east,west},2,b).State;
                var result=BattleResolver.Apply(state,new MoveCommand(east.Id,new[]{exit}));
                Assert.That(result.IsApplied,Is.True); var escaped=result.State.FindUnit(east.Id);
                Assert.That(escaped.Status,Is.EqualTo(UnitStatus.Escaped)); Assert.That(escaped.Hp,Is.EqualTo(20)); Assert.That(escaped.Armor,Is.EqualTo(3));
                Assert.That(result.State.Outcome.Reason,Is.EqualTo(BattleEndReason.Withdrawal));
            }
        }
        [TestCase(SizeExperimentMap.Siege_23x17_Tight)]
        [TestCase(SizeExperimentMap.Siege_27x21_Roomy)]
        public void FortressBlocksSolidShotsButAllowsOpeningsInteriorAndExteriorRoutes(SizeExperimentMap map)
        {
            var board = SizeExperimentFixture.Board(map);
            int cx = board.Columns / 2, cy = board.Rows / 2;
            var fixture = BattleResolver.StartBattle(SizeExperimentFixture.Units(map), 2, board).State;
            Assert.That(LineOfSight.IsClear(fixture, new GridPosition(cx - 6, cy - 2), new GridPosition(cx - 4, cy - 2)), Is.False);
            Assert.That(LineOfSight.IsClear(fixture, new GridPosition(cx - 6, cy), new GridPosition(cx - 4, cy)), Is.True);
            var routes = new[] {
                new[] { new GridPosition(cx - 6, cy), new GridPosition(cx - 2, cy) },
                new[] { new GridPosition(cx, cy - 2), new GridPosition(cx, cy - 6) },
                new[] { new GridPosition(cx - 6, cy - 3), new GridPosition(cx - 2, cy - 5) }
            };
            foreach (var route in routes)
            {
                var mover = new UnitState(new UnitId(5), Side.West, UnitProfile.ElfWarriorTI, route[0], Facing.East);
                var enemy = new UnitState(new UnitId(7), Side.East, UnitProfile.HumanWarriorTI, new GridPosition(cx + 2, cy), Facing.West);
                var state = BattleResolver.StartBattle(new[] { mover, enemy }, 2, board).State;
                var path = Pathfinder.FindPath(state, mover.Id, route[1]);
                Assert.That(path.Found, Is.True);
                var result = BattleResolver.Apply(state, new MoveCommand(mover.Id, path.Steps));
                Assert.That(result.IsApplied, Is.True);
                Assert.That(result.State.FindUnit(mover.Id).Position, Is.EqualTo(route[1]));
            }
        }
        [Test]
        public void IllegalSolidOuterCellIsNotAnEscapeEndpoint()
        {
            var p=new GridPosition(0,2); var board=new Battlefield(17,11,new[]{p},true);
            Assert.That(board.IsRetreatZone(Side.East,p),Is.False); Assert.That(board.IsRetreatZone(Side.West,p),Is.False);
        }
    }
}

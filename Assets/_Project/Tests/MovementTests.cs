using System;
using System.Linq;
using NUnit.Framework;
using RPG.Core;
using static RPG.Tests.BattleTestFixtures;

namespace RPG.Tests
{
    internal static class GridTestFixtures
    {
        internal static GridPosition P(int x, int y) => new GridPosition(x, y);
        internal static BattleState Grid(Battlefield board = null, params UnitState[] extra)
        {
            var units = new[] { Unit(1, UnitProfile.ElfWarriorTI, x: 2, y: 2) }.Concat(extra).ToArray();
            return BattleResolver.StartBattle(units, 1, board).State;
        }
        internal static MoveCommand Move(params GridPosition[] path) => new MoveCommand(Attacker, path);
        internal static void AssertRejected(BattleState state, BattleCommand command, CommandError expected)
        {
            string before = Snapshot(state);
            uint rng = state.RngState;
            var result = BattleResolver.Apply(state, command);
            Assert.That(result.Error, Is.EqualTo(expected));
            Assert.That(result.State, Is.SameAs(state));
            Assert.That(Snapshot(state), Is.EqualTo(before));
            Assert.That(result.State.RngState, Is.EqualTo(rng));
            Assert.That(result.Events, Is.Empty);
        }
    }

    public class MovementTests
    {
        [TestCase(3, 2, Facing.East)]
        [TestCase(3, 3, Facing.NorthEast)]
        public void OrthogonalAndDiagonalStepsCostOne(int x, int y, Facing facing)
        {
            var state = GridTestFixtures.Grid();
            var result = BattleResolver.Apply(state, GridTestFixtures.Move(new GridPosition(x, y)));
            var unit = result.State.FindUnit(Attacker);
            Assert.That(result.IsApplied, Is.True);
            Assert.That(unit.MovementRemaining, Is.EqualTo(5));
            Assert.That(unit.MovementSpentThisActivation, Is.EqualTo(1));
            Assert.That(unit.Position, Is.EqualTo(new GridPosition(x, y)));
            Assert.That(unit.Facing, Is.EqualTo(facing));
            Assert.That(unit.ActionAvailable, Is.True);
            Assert.That(result.State.RngState, Is.EqualTo(state.RngState));
            Assert.That(result.Events.Single(e => e.Kind == BattleEventKind.StepMoved).From, Is.EqualTo(new GridPosition(2, 2)));
            Assert.That(result.Events.Single(e => e.Kind == BattleEventKind.StepMoved).To, Is.EqualTo(new GridPosition(x, y)));
        }

        [Test]
        public void InvalidLaterStepRejectsWholePathWithoutPartialExecution()
        {
            var state = GridTestFixtures.Grid();
            GridTestFixtures.AssertRejected(state, GridTestFixtures.Move(
                new GridPosition(1, 2), new GridPosition(0, 2), new GridPosition(-1, 2)), CommandError.OutOfBounds);
            var obstacle = GridTestFixtures.Grid(new Battlefield(new[] { new GridPosition(4, 2) }));
            GridTestFixtures.AssertRejected(obstacle, GridTestFixtures.Move(
                new GridPosition(3, 2), new GridPosition(4, 2)), CommandError.SolidCell);
        }

        [TestCase(Side.West)]
        [TestCase(Side.East)]
        public void CannotEnterOrPassThroughAnotherActiveUnit(Side side)
        {
            var state = GridTestFixtures.Grid(null, Unit(2, UnitProfile.HumanWarriorTI, side, x: 3, y: 2));
            GridTestFixtures.AssertRejected(state, GridTestFixtures.Move(new GridPosition(3, 2)), CommandError.OccupiedCell);
            GridTestFixtures.AssertRejected(state, GridTestFixtures.Move(new GridPosition(3, 2), new GridPosition(4, 2)), CommandError.OccupiedCell);
        }

        [TestCase(UnitStatus.Dead)]
        [TestCase(UnitStatus.Escaped)]
        public void InactiveUnitsDoNotOccupyCells(UnitStatus status)
        {
            var state = GridTestFixtures.Grid(null, Unit(2, UnitProfile.HumanWarriorTI, x: 3, y: 2,
                hp: status == UnitStatus.Dead ? 0 : 8, status: status));
            Assert.That(state.OccupantAt(new GridPosition(3, 2)), Is.Null);
            var result = BattleResolver.Apply(state, GridTestFixtures.Move(new GridPosition(3, 2)));
            Assert.That(result.IsApplied, Is.True);
            Assert.That(result.State.OccupantAt(new GridPosition(3, 2)).Id, Is.EqualTo(Attacker));
        }

        [TestCase(3, 2, false)]
        [TestCase(2, 3, false)]
        [TestCase(3, 2, true)]
        [TestCase(2, 3, true)]
        public void EitherBlockedOrthogonalNeighbourPreventsDiagonal(int x, int y, bool occupied)
        {
            var state = occupied
                ? GridTestFixtures.Grid(null, Unit(2, UnitProfile.HumanWarriorTI, x: x, y: y))
                : GridTestFixtures.Grid(new Battlefield(new[] { new GridPosition(x, y) }));
            GridTestFixtures.AssertRejected(state, GridTestFixtures.Move(new GridPosition(3, 3)), CommandError.BlockedCorner);
        }

        [Test]
        public void SplitMovementThenAttackThenEndUsesRealPositionsAndResources()
        {
            var state = GridTestFixtures.Grid(null, Unit(2, UnitProfile.HumanArcherTI, Side.East, x: 5, y: 3));
            string initial = Snapshot(state);
            var first = BattleResolver.Apply(state, GridTestFixtures.Move(new GridPosition(3, 2), new GridPosition(4, 2)));
            Assert.That(first.IsApplied, Is.True);
            Assert.That(Snapshot(state), Is.EqualTo(initial));
            var second = BattleResolver.Apply(first.State, GridTestFixtures.Move(new GridPosition(4, 3)));
            var actor = second.State.FindUnit(Attacker);
            Assert.That(actor.Position, Is.EqualTo(new GridPosition(4, 3)));
            Assert.That(actor.MovementRemaining, Is.EqualTo(3));
            Assert.That(actor.MovementSpentThisActivation, Is.EqualTo(3));
            Assert.That(actor.Facing, Is.EqualTo(Facing.North));
            Assert.That(first.Events.Count(e => e.Kind == BattleEventKind.MovementConsumed), Is.EqualTo(2));
            Assert.That(first.Events.Where(e => e.Kind == BattleEventKind.MovementConsumed).Sum(e => e.Amount), Is.EqualTo(2));
            var attack = BattleResolver.Apply(second.State, Attack());
            Assert.That(attack.IsApplied, Is.True);
            Assert.That(attack.State.FindUnit(Attacker).Facing, Is.EqualTo(Facing.East));
            Assert.That(attack.State.FindUnit(Target).Hp, Is.EqualTo(21)); // Elf damage 11, Armor 4.
            var end = BattleResolver.Apply(attack.State, new EndActivationCommand(Attacker));
            Assert.That(end.State.CurrentUnitId, Is.EqualTo(Target));
            Assert.That(end.State.FindUnit(Attacker).MovementRemaining, Is.Zero);
        }

        [Test]
        public void CannotExceedRemainingMovementAcrossCommands()
        {
            var state = GridTestFixtures.Grid();
            state = BattleResolver.Apply(state, GridTestFixtures.Move(new GridPosition(3, 2), new GridPosition(4, 2),
                new GridPosition(5, 2), new GridPosition(6, 2), new GridPosition(7, 2))).State;
            Assert.That(state.FindUnit(Attacker).MovementRemaining, Is.EqualTo(1));
            GridTestFixtures.AssertRejected(state, GridTestFixtures.Move(new GridPosition(8, 2), new GridPosition(9, 2)), CommandError.InsufficientMovement);
        }

        [Test]
        public void ReturningToOriginStillPreventsDefendAndUsesMovement()
        {
            var state = GridTestFixtures.Grid();
            state = BattleResolver.Apply(state, GridTestFixtures.Move(new GridPosition(3, 2), new GridPosition(2, 2))).State;
            Assert.That(state.FindUnit(Attacker).MovementSpentThisActivation, Is.EqualTo(2));
            Assert.That(state.FindUnit(Attacker).MovementRemaining, Is.EqualTo(4));
            GridTestFixtures.AssertRejected(state, new DefendCommand(Attacker), CommandError.MovementAlreadySpent);
        }

        [Test]
        public void ActionConsumedBlocksMovementEvenWithBudgetRemaining()
        {
            var state = GridTestFixtures.Grid(null, Unit(2, UnitProfile.HumanArcherTI, Side.East, x: 3, y: 2));
            state = BattleResolver.Apply(state, Attack()).State;
            Assert.That(state.FindUnit(Attacker).MovementRemaining, Is.EqualTo(6));
            GridTestFixtures.AssertRejected(state, GridTestFixtures.Move(new GridPosition(2, 3)), CommandError.NoAction);
        }

        [Test]
        public void MovementRejectsWrongActorInactiveActorAndMalformedPaths()
        {
            var state = GridTestFixtures.Grid(null, Unit(2, UnitProfile.HumanWarriorTI, x: 8, y: 3),
                Unit(3, UnitProfile.HumanWarriorTI, x: 9, y: 3, status: UnitStatus.Escaped));
            GridTestFixtures.AssertRejected(state, new MoveCommand(Target, new[] { new GridPosition(8, 4) }), CommandError.NotCurrentActor);
            GridTestFixtures.AssertRejected(state, new MoveCommand(new UnitId(3), new[] { new GridPosition(9, 4) }), CommandError.ActorInactive);
            GridTestFixtures.AssertRejected(state, GridTestFixtures.Move(), CommandError.InvalidPath);
            GridTestFixtures.AssertRejected(state, new MoveCommand(Attacker, null), CommandError.InvalidPath);
            GridTestFixtures.AssertRejected(state, GridTestFixtures.Move(new GridPosition(2, 2)), CommandError.InvalidStep);
            GridTestFixtures.AssertRejected(state, GridTestFixtures.Move(new GridPosition(4, 2)), CommandError.InvalidStep);
        }

        [Test]
        public void CommandCopiesCallerPathAndMoveThenEndDiscardsRemainingResources()
        {
            var path = new[] { new GridPosition(3, 2), new GridPosition(3, 3) };
            var command = new MoveCommand(Attacker, path);
            path[0] = new GridPosition(12, 8);
            var state = GridTestFixtures.Grid(null, Unit(2, UnitProfile.HumanWarriorTI, x: 8, y: 3));
            state = BattleResolver.Apply(state, command).State;
            Assert.That(state.FindUnit(Attacker).Position, Is.EqualTo(new GridPosition(3, 3)));
            Assert.That(state.FindUnit(Attacker).Facing, Is.EqualTo(Facing.North));
            state = BattleResolver.Apply(state, new EndActivationCommand(Attacker, Facing.West)).State;
            Assert.That(state.FindUnit(Attacker).MovementRemaining, Is.Zero);
            Assert.That(state.FindUnit(Attacker).ActionAvailable, Is.False);
            Assert.That(state.FindUnit(Attacker).Facing, Is.EqualTo(Facing.West));
        }

        [Test]
        public void BattlefieldFixturesAreImmutableAndRejectInvalidActivePlacements()
        {
            Assert.That(Battlefield.BaseMap.SolidCells, Is.EqualTo(new[] {
                new GridPosition(6, 3), new GridPosition(6, 4), new GridPosition(6, 5)
            }));
            Assert.That(Battlefield.ControlMap.SolidCells, Is.Empty);
            Assert.That(Battlefield.BaseMap.Contains(new GridPosition(12, 8)), Is.True);
            Assert.That(Battlefield.BaseMap.Contains(new GridPosition(13, 8)), Is.False);
            var cells = new[] { new GridPosition(1, 1) };
            var board = new Battlefield(cells); cells[0] = new GridPosition(2, 2);
            Assert.That(board.IsSolid(new GridPosition(1, 1)), Is.True);
            Assert.Throws<ArgumentOutOfRangeException>(() => new Battlefield(new[] { new GridPosition(-1, 0) }));
            Assert.Throws<ArgumentException>(() => BattleResolver.StartBattle(new[] { Unit(1, UnitProfile.HumanWarriorTI, x: 13) }, 1));
            Assert.Throws<ArgumentException>(() => BattleResolver.StartBattle(new[] { Unit(1, UnitProfile.HumanWarriorTI, x: 6, y: 4) }, 1, Battlefield.BaseMap));
        }

        [Test]
        public void MovementAndAttackReplayProducesIdenticalEventsAndFinalState()
        {
            var a = GridTestFixtures.Grid(null, Unit(2, UnitProfile.HumanArcherTI, Side.East, x: 5, y: 3));
            var b = GridTestFixtures.Grid(null, Unit(2, UnitProfile.HumanArcherTI, Side.East, x: 5, y: 3));
            BattleCommand[] commands = {
                GridTestFixtures.Move(new GridPosition(3, 2), new GridPosition(4, 2)),
                GridTestFixtures.Move(new GridPosition(4, 3)), Attack(), new EndActivationCommand(Attacker)
            };
            foreach (var command in commands)
            {
                Pathfinder.FindPath(b, b.CurrentUnitId.Value, new GridPosition(0, 0));
                LineOfSight.IsClear(b, b.FindUnit(Attacker).Position, b.FindUnit(Target).Position);
                var ra = BattleResolver.Apply(a, command); var rb = BattleResolver.Apply(b, command);
                Assert.That(ra.IsApplied, Is.True); CollectionAssert.AreEqual(ra.Events, rb.Events);
                a = ra.State; b = rb.State;
            }
            Assert.That(Snapshot(a), Is.EqualTo(Snapshot(b)));
        }
    }
}

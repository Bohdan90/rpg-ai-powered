using NUnit.Framework;
using RPG.Core;
using static RPG.Tests.BattleTestFixtures;
using static RPG.Tests.GridTestFixtures;

namespace RPG.Tests
{
    public class PathfinderTests
    {
        [Test]
        public void EqualCostPathsUseClockwiseFinalTieBreakAndDoNotMutateState()
        {
            var state = Grid(); string before = Snapshot(state);
            for (int i = 0; i < 3; i++)
            {
                var path = Pathfinder.FindPath(state, Attacker, P(4, 3));
                Assert.That(path.Found, Is.True);
                Assert.That(path.Steps, Is.EqualTo(new[] { P(3, 3), P(4, 3) }));
                Assert.That(path.Cost, Is.EqualTo(2));
            }
            Assert.That(Snapshot(state), Is.EqualTo(before));
        }

        [TestCase(2, 0)]
        [TestCase(-2, 0)]
        [TestCase(0, 2)]
        [TestCase(0, -2)]
        [TestCase(2, 2)]
        public void UnobstructedStraightRoutesDoNotZigzag(int dx, int dy)
        {
            var state = BattleResolver.StartBattle(new[] { Unit(1, UnitProfile.ElfWarriorTI, x: 5, y: 4) }, 1, Battlefield.ControlMap).State;
            var path = Pathfinder.FindPath(state, Attacker, P(5 + dx, 4 + dy));
            Assert.That(path.Steps, Is.EqualTo(new[] { P(5 + dx / 2, 4 + dy / 2), P(5 + dx, 4 + dy) }));
            var result = BattleResolver.Apply(state, new MoveCommand(Attacker, path.Steps));
            Assert.That(result.IsApplied, Is.True);
            Assert.That(result.State.FindUnit(Attacker).MovementRemaining, Is.EqualTo(4));
        }

        [Test]
        public void CloserToLineWinsEvenWhenAnotherShortestRouteHasFewerTurns()
        {
            var state = BattleResolver.StartBattle(new[] { Unit(1, UnitProfile.ElfWarriorTI, x: 2, y: 2) }, 1, Battlefield.ControlMap).State;
            var path = Pathfinder.FindPath(state, Attacker, P(6, 3));
            // E, NE, E, E: summed cross-product deviation 4, two turns.
            // NE, E, E, E: deviation 6, one turn. Closeness has priority.
            Assert.That(path.Steps, Is.EqualTo(new[] { P(3, 2), P(4, 3), P(5, 3), P(6, 3) }));
            Assert.That(path.Cost, Is.EqualTo(4));
        }

        [Test]
        public void EqualLengthAndDeviationPreferFewerTurnsBeforeDirectionOrder()
        {
            var state = BattleResolver.StartBattle(new[] { Unit(1, UnitProfile.ElfWarriorTI, x: 2, y: 2) }, 1, Battlefield.ControlMap).State;
            var path = Pathfinder.FindPath(state, Attacker, P(6, 4));
            // NE, E, E, NE has deviation 4 and two turns.
            // The lexically earlier NE, E, NE, E also has deviation 4, but three turns.
            Assert.That(path.Steps, Is.EqualTo(new[] { P(3, 3), P(4, 3), P(5, 3), P(6, 4) }));
            Assert.That(path.Cost, Is.EqualTo(4));
        }

        [Test]
        public void BaseMapRouteAvoidsWallAndCornersAndHasMinimumSixStepCost()
        {
            var state = BattleResolver.StartBattle(new[] { Unit(1, UnitProfile.ElfWarriorTI, x: 5, y: 4) }, 1, Battlefield.BaseMap).State;
            var path = Pathfinder.FindPath(state, Attacker, P(7, 4));
            Assert.That(path.Found, Is.True); Assert.That(path.Cost, Is.EqualTo(6));
            Assert.That(path.Steps[0], Is.EqualTo(P(5, 5))); // North wins over equal south detour.
            var result = BattleResolver.Apply(state, new MoveCommand(Attacker, path.Steps));
            Assert.That(result.IsApplied, Is.True);
            Assert.That(result.State.FindUnit(Attacker).Position, Is.EqualTo(P(7, 4)));
            Assert.That(result.State.FindUnit(Attacker).MovementRemaining, Is.Zero);
            var control = BattleResolver.StartBattle(new[] { Unit(1, UnitProfile.ElfWarriorTI, x: 5, y: 4) }, 1, Battlefield.ControlMap).State;
            Assert.That(Pathfinder.FindPath(control, Attacker, P(7, 4)).Cost, Is.EqualTo(2));
        }

        [Test]
        public void ActiveUnitsForceDetoursIncludingTheirBlockedDiagonalCorners()
        {
            var state = Grid(null, Unit(2, UnitProfile.HumanWarriorTI, x: 3, y: 2));
            var path = Pathfinder.FindPath(state, Attacker, P(4, 2));
            Assert.That(path.Found, Is.True); Assert.That(path.Cost, Is.EqualTo(4));
            Assert.That(path.Steps, Has.No.Member(P(3, 2)));
            Assert.That(BattleResolver.Apply(state, new MoveCommand(Attacker, path.Steps)).IsApplied, Is.True);
        }

        [Test]
        public void UnreachableOverBudgetOccupiedAndOutOfBoundsQueriesReturnNoPath()
        {
            var state = Grid(null, Unit(2, UnitProfile.HumanWarriorTI, x: 4, y: 4));
            foreach (var cell in new[] { P(12, 8), P(4, 4), P(-1, 0), P(13, 0) })
            {
                var result = Pathfinder.FindPath(state, Attacker, cell);
                Assert.That(result.Found, Is.False); Assert.That(result.Steps, Is.Empty);
            }
            var trapped = Grid(new Battlefield(new[] { P(2, 3), P(3, 2), P(2, 1), P(1, 2) }));
            Assert.That(Pathfinder.FindPath(trapped, Attacker, P(4, 4)).Found, Is.False);
            Assert.That(Pathfinder.FindPath(trapped, Attacker, P(2, 3)).Found, Is.False);
        }

        [Test]
        public void QueryToOriginIsZeroCostAndActionSpentHasNoExecutablePath()
        {
            var state = Grid();
            var path = Pathfinder.FindPath(state, Attacker, P(2, 2));
            Assert.That(path.Found, Is.True); Assert.That(path.Cost, Is.Zero); Assert.That(path.Steps, Is.Empty);
            state = BattleResolver.Apply(state, new DefendCommand(Attacker)).State;
            Assert.That(Pathfinder.FindPath(state, Attacker, P(2, 3)).Found, Is.False);
        }
    }
}

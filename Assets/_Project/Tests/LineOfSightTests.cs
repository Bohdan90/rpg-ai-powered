using NUnit.Framework;
using RPG.Core;
using static RPG.Tests.BattleTestFixtures;
using static RPG.Tests.GridTestFixtures;

namespace RPG.Tests
{
    public class LineOfSightTests
    {
        private static BattleState Shot(GridPosition target, Battlefield board = null, UnitState blocker = null)
        {
            var units = new System.Collections.Generic.List<UnitState> {
                Unit(1, UnitProfile.HumanArcherTI, x: 2, y: 2),
                Unit(2, UnitProfile.HumanWarriorTI, Side.East, x: target.X, y: target.Y)
            };
            if (blocker != null) units.Add(blocker);
            return ToActor(BattleResolver.StartBattle(units, 1, board).State, Attacker);
        }

        [Test]
        public void ClearShotExcludesBothOccupiedEndpointsAndQueriesDoNotMutate()
        {
            var state = Shot(P(8, 2)); string before = Snapshot(state);
            Assert.That(state.OccupantAt(P(2, 2)), Is.Not.Null);
            Assert.That(state.OccupantAt(P(8, 2)), Is.Not.Null);
            Assert.That(LineOfSight.IsClear(state, P(2, 2), P(8, 2)), Is.True);
            Assert.That(LineOfSight.IsClear(state, P(8, 2), P(2, 2)), Is.True);
            Assert.That(BattleResolver.Validate(state, Attack()), Is.EqualTo(CommandError.None));
            BattleResolver.PreviewAttack(state, Attack());
            Assert.That(Snapshot(state), Is.EqualTo(before));
        }

        [TestCase(Side.West)]
        [TestCase(Side.East)]
        public void IntermediateActiveUnitGivesCoverWithoutBlockingRangedAttack(Side side)
        {
            var state = Shot(P(8, 2), blocker: Unit(3, UnitProfile.HumanWarriorTI, side, x: 5, y: 2));
            Assert.That(BattleResolver.PreviewAttack(state, Attack()).Cover, Is.EqualTo(CoverLevel.Light));
            Assert.That(BattleResolver.Apply(state, Attack()).IsApplied, Is.True);
        }

        [TestCase(UnitStatus.Dead)]
        [TestCase(UnitStatus.Escaped)]
        public void InactiveUnitsDoNotScreenShots(UnitStatus status)
        {
            var state = Shot(P(8, 2), blocker: Unit(3, UnitProfile.HumanWarriorTI, x: 5, y: 2,
                hp: status == UnitStatus.Dead ? 0 : 8, status: status));
            Assert.That(BattleResolver.Validate(state, Attack()), Is.EqualTo(CommandError.None));
        }

        [TestCase(8, 2, 5, 2)] // Horizontal.
        [TestCase(2, 8, 2, 5)] // Vertical.
        [TestCase(4, 4, 3, 2)] // Diagonal touches this side cell only at its corner.
        [TestCase(4, 4, 2, 3)] // Other side of the same corner.
        [TestCase(5, 3, 3, 3)] // Shallow slope, exact non-45-degree corner.
        [TestCase(3, 5, 3, 3)] // Steep slope, transposed traversal.
        public void SolidSupercoverCellsBlockBothDirections(int tx, int ty, int bx, int by)
        {
            var state = Shot(P(tx, ty), new Battlefield(new[] { P(bx, by) }));
            Assert.That(LineOfSight.IsClear(state, P(2, 2), P(tx, ty)), Is.False);
            Assert.That(LineOfSight.IsClear(state, P(tx, ty), P(2, 2)), Is.False);
            AssertRejected(state, Attack(), CommandError.BlockedLineOfSight);
        }

        [Test]
        public void OccupiedCornerTouchDoesNotBlockRangedShot()
        {
            var occupied = Shot(P(4, 4), blocker: Unit(3, UnitProfile.HumanWarriorTI, x: 3, y: 2));
            Assert.That(BattleResolver.Validate(occupied, Attack()), Is.EqualTo(CommandError.None));
            var clear = Shot(P(5, 3), new Battlefield(new[] { P(2, 3), P(4, 4) }));
            Assert.That(LineOfSight.IsClear(clear, P(2, 2), P(5, 3)), Is.True);
            Assert.That(LineOfSight.IsClear(clear, P(5, 3), P(2, 2)), Is.True);
            Assert.That(LineOfSight.IsClear(clear, P(-1, 2), P(5, 3)), Is.False);
        }

        [Test]
        public void AdjacentMeleeIgnoresUnitScreeningButRespectsSolidCorners()
        {
            var units = new[] {
                Unit(1, UnitProfile.ElfWarriorTI, x: 2, y: 2),
                Unit(2, UnitProfile.HumanWarriorTI, Side.East, x: 3, y: 3),
                Unit(3, UnitProfile.HumanWarriorTI, x: 3, y: 2)
            };
            var state = BattleResolver.StartBattle(units, 1).State;
            Assert.That(LineOfSight.IsClear(state, P(2, 2), P(3, 3)), Is.True);
            Assert.That(BattleResolver.Validate(state, Attack()), Is.EqualTo(CommandError.None));
            AssertRejected(state, Move(P(3, 3)), CommandError.OccupiedCell);
            foreach (var corner in new[] { P(3, 2), P(2, 3) })
            {
                var solid = BattleResolver.StartBattle(new[] { units[0], units[1] }, 1, new Battlefield(new[] { corner })).State;
                Assert.That(BattleResolver.Validate(solid, Attack()), Is.EqualTo(CommandError.None));
                Assert.That(LineOfSight.IsClear(solid, P(2, 2), P(3, 3)), Is.False);
            }
            var sealedCorner = BattleResolver.StartBattle(new[] { units[0], units[1] }, 1,
                new Battlefield(new[] { P(3, 2), P(2, 3) })).State;
            AssertRejected(sealedCorner, Attack(), CommandError.BlockedCorner);
        }

        [Test]
        public void ArcherAfterMovingHasRangeSixAndUnchangedDistancePenalty()
        {
            var state = Shot(P(8, 3));
            state = BattleResolver.Apply(state, Move(P(2, 3))).State;
            var preview = BattleResolver.PreviewAttack(state, Attack());
            Assert.That(preview.IsLegal, Is.True);
            Assert.That(preview.MaximumRange, Is.EqualTo(6));
            Assert.That(preview.ContactChance, Is.EqualTo(65));
            Assert.That(preview.SteadyAim, Is.False);
            var far = Shot(P(9, 3)); far = BattleResolver.Apply(far, Move(P(2, 3))).State;
            AssertRejected(far, Attack(), CommandError.OutOfRange);
        }

        [Test]
        public void SteadyAimExtendsRangeToSevenButNeverEight()
        {
            var state = Shot(P(9, 2));
            Assert.That(BattleResolver.PreviewAttack(state, Attack()).MaximumRange, Is.EqualTo(7));
            Assert.That(BattleResolver.Validate(state, Attack()), Is.EqualTo(CommandError.None));
            AssertRejected(Shot(P(10, 2)), Attack(), CommandError.OutOfRange);
        }

        [Test]
        public void MovementChangesFrontalEvasionThroughPositionsWithoutDamageBonusOrTargetRotation()
        {
            var state = ToActor(BattleResolver.StartBattle(new[] {
                Unit(1, UnitProfile.HumanWarriorTI, x: 2, y: 2),
                Unit(2, UnitProfile.ElfWarriorTI, Side.East, x: 3, y: 3, facing: Facing.West)
            }, 1).State, Attacker);
            Assert.That(BattleResolver.PreviewAttack(state, Attack()).ContactChance, Is.EqualTo(60));
            state = BattleResolver.Apply(state, Move(P(3, 2), P(4, 2), P(4, 3))).State;
            var preview = BattleResolver.PreviewAttack(state, Attack());
            Assert.That(preview.ContactChance, Is.EqualTo(75));
            Assert.That(preview.PhysicalDamage, Is.EqualTo(12));
            state = BattleResolver.Apply(state, Attack()).State;
            Assert.That(state.FindUnit(Attacker).Facing, Is.EqualTo(Facing.West));
            Assert.That(state.FindUnit(Target).Facing, Is.EqualTo(Facing.West));
        }
    }
}

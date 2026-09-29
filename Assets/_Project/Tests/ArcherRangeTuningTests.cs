using System.Linq;
using NUnit.Framework;
using RPG.Core;
using static RPG.Tests.BattleTestFixtures;

namespace RPG.Tests
{
    public class ArcherRangeTuningTests
    {
        [TestCase(4, 0)]
        [TestCase(5, -5)]
        [TestCase(7, -15)]
        [TestCase(8, -20)]
        [TestCase(10, -30)]
        public void DistanceAndAimModifiersMatchExecutionBeforeAndAfterMovement(int distance, int penalty)
        {
            foreach (bool moved in new[] { false, true })
            {
                var state = BattleResolver.StartBattle(new[] {
                    Unit(1, UnitProfile.HumanArcherTI, x:2, y:2),
                    Unit(2, UnitProfile.HumanWarriorTI, Side.East, x:2+distance, y:2)
                }, 1, new Battlefield(19,13)).State;
                if (moved)
                    state = BattleResolver.Apply(state, new MoveCommand(Attacker, new[] { new GridPosition(2,3) })).State;
                string before = Snapshot(state);
                var preview = BattleResolver.PreviewAttack(state, Attack());
                Assert.That(preview.IsLegal, Is.True);
                Assert.That(preview.MaximumRange, Is.EqualTo(10));
                Assert.That(preview.DistanceModifier, Is.EqualTo(penalty));
                Assert.That(preview.SteadyAim, Is.EqualTo(!moved));
                Assert.That(preview.AimModifier, Is.EqualTo(moved ? 0 : 15));
                Assert.That(preview.ContactChance, Is.EqualTo(System.Math.Min(95, 85 + penalty + (moved ? 0 : 15) - 5)));
                Assert.That(Snapshot(state), Is.EqualTo(before));
                var result = BattleResolver.Apply(state, Attack());
                Assert.That(result.IsApplied, Is.True);
                Assert.That(result.Events.Single(e => e.Kind == BattleEventKind.ContactRolled).ChancePercent,
                    Is.EqualTo(preview.ContactChance));
                Assert.That(result.Events.Count(e => e.Kind == BattleEventKind.SteadyAimApplied), Is.EqualTo(moved ? 0 : 1));
                Assert.That(result.State.FindUnit(Attacker).MovementRemaining, Is.EqualTo(moved ? 3 : 0));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ElevenIsRejectedWithoutResourcesOrRngConsumption(bool moved)
        {
            var state = BattleResolver.StartBattle(new[] {
                Unit(1, UnitProfile.HumanArcherTI, x:2, y:2),
                Unit(2, UnitProfile.HumanWarriorTI, Side.East, x:13, y:2)
            }, 1, new Battlefield(19,13)).State;
            if (moved)
                state = BattleResolver.Apply(state, new MoveCommand(Attacker, new[] { new GridPosition(2,3) })).State;
            string before = Snapshot(state);
            Assert.That(BattleResolver.PreviewAttack(state, Attack()).Error, Is.EqualTo(CommandError.OutOfRange));
            var result = BattleResolver.Apply(state, Attack());
            Assert.That(result.Error, Is.EqualTo(CommandError.OutOfRange));
            Assert.That(result.State, Is.SameAs(state));
            Assert.That(result.Events, Is.Empty);
            Assert.That(Snapshot(state), Is.EqualTo(before));
        }

        [Test]
        public void LongRangeCoverAndSteadyAimRemainAdditive()
        {
            var state = BattleResolver.StartBattle(new[] {
                Unit(1, UnitProfile.HumanArcherTI, x:2, y:2),
                Unit(2, UnitProfile.HumanWarriorTI, Side.East, x:12, y:2),
                Unit(3, UnitProfile.HumanWarriorTI, Side.East, x:10, y:2)
            }, 1, new Battlefield(19,13)).State;
            var preview = BattleResolver.PreviewAttack(state, Attack());
            Assert.That(preview.IsLegal, Is.True);
            Assert.That(preview.Cover, Is.EqualTo(CoverLevel.Light));
            Assert.That(preview.CoverAccuracyModifier, Is.EqualTo(-15));
            Assert.That(preview.DistanceModifier, Is.EqualTo(-30));
            Assert.That(preview.ContactChance, Is.EqualTo(50)); // 85 + 15 - 30 - 15 - 5.
        }

        [Test]
        public void ElfCannotReachMeleeFromExtremeBowRangeInOneActivation()
        {
            var state = BattleResolver.StartBattle(new[] {
                Unit(1, UnitProfile.HumanArcherTI, x:2, y:2),
                Unit(2, UnitProfile.ElfWarriorTI, Side.East, x:12, y:2)
            }, 1, new Battlefield(19,13)).State;
            var path = Enumerable.Range(0,6).Select(i => new GridPosition(11-i,2)).ToArray();
            var move = BattleResolver.Apply(state, new MoveCommand(Target,path));
            Assert.That(move.IsApplied, Is.True);
            Assert.That(move.State.FindUnit(Target).MovementRemaining, Is.Zero);
            Assert.That(move.State.FindUnit(Target).Position.DistanceTo(move.State.FindUnit(Attacker).Position), Is.EqualTo(4));
            Assert.That(BattleResolver.Validate(move.State, new BasicAttackCommand(Target,Attacker)), Is.EqualTo(CommandError.OutOfRange));
        }
    }
}

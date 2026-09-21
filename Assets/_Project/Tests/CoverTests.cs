using NUnit.Framework;
using RPG.Core;
using static RPG.Tests.BattleTestFixtures;
using static RPG.Tests.GridTestFixtures;

namespace RPG.Tests
{
    public class CoverTests
    {
        private static BattleState Shot(params UnitState[] screens)
        {
            var units = new System.Collections.Generic.List<UnitState> {
                Unit(1, UnitProfile.HumanArcherTI, x: 2, y: 2),
                Unit(2, UnitProfile.HumanArcherTI, Side.East, x: 8, y: 2)
            };
            units.AddRange(screens);
            return ToActor(BattleResolver.StartBattle(units, 1).State, Attacker);
        }

        [TestCase(3, CoverLevel.None)]
        [TestCase(5, CoverLevel.Light)]
        [TestCase(7, CoverLevel.Light)]
        public void ProximityAndMidpointDetermineTargetCover(int x, CoverLevel expected)
        {
            var state = Shot(Unit(3, UnitProfile.HumanWarriorTI, x: x, y: 2));
            var preview = BattleResolver.PreviewAttack(state, Attack());
            Assert.That(preview.IsLegal, Is.True);
            Assert.That(preview.Cover, Is.EqualTo(expected));
            Assert.That(LineOfSight.IsClear(state, P(2, 2), P(8, 2)), Is.True);
        }

        [TestCase(1, 2, CoverLevel.Light)]
        [TestCase(1, 1, CoverLevel.Light)]
        [TestCase(2, 1, CoverLevel.Strong)]
        public void SizeClassificationDoesNotInventStrongTuning(int screenSize, int targetSize, CoverLevel expected)
        {
            Assert.That(Cover.Classify(P(2, 2), P(8, 2), P(7, 2), screenSize, targetSize), Is.EqualTo(expected));
            Assert.That(Cover.AccuracyModifier(CoverLevel.Strong), Is.Null);
        }

        [Test]
        public void ReverseShotChangesCoverOwnership()
        {
            var state = Shot(Unit(3, UnitProfile.HumanWarriorTI, x: 7, y: 2));
            Assert.That(Cover.Query(state, state.FindUnit(Attacker), state.FindUnit(Target)), Is.EqualTo(CoverLevel.Light));
            state = ToActor(state, Target);
            var reverse = BattleResolver.PreviewAttack(state, new BasicAttackCommand(Target, Attacker));
            Assert.That(reverse.IsLegal, Is.True);
            Assert.That(reverse.Cover, Is.EqualTo(CoverLevel.None));
        }

        [Test]
        public void LightCoverDoesNotStackAndContactUsesExactlyMinus15()
        {
            var state = Shot(Unit(3, UnitProfile.HumanWarriorTI, x: 6, y: 2), Unit(4, UnitProfile.HumanWarriorTI, Side.East, x: 7, y: 2));
            var preview = BattleResolver.PreviewAttack(state, Attack());
            Assert.That(preview.BaseAccuracy, Is.EqualTo(80));
            Assert.That(preview.AimModifier, Is.EqualTo(15));
            Assert.That(preview.DistanceModifier, Is.EqualTo(-10));
            Assert.That(preview.TargetDodge, Is.EqualTo(5));
            Assert.That(preview.CoverAccuracyModifier, Is.EqualTo(-15));
            Assert.That(preview.ContactChance, Is.EqualTo(65));
            Assert.That(BattleResolver.PreviewAttack(Shot(), Attack()).ContactChance - preview.ContactChance, Is.EqualTo(15));
            var result = BattleResolver.Apply(state, Attack());
            Assert.That(result.IsApplied, Is.True);
            Assert.That(result.State.FindUnit(new UnitId(3)).Hp, Is.EqualTo(40));
            Assert.That(result.State.FindUnit(new UnitId(4)).Hp, Is.EqualTo(40));
        }

        [Test]
        public void StrongestClassificationWinsIndependentOfOrder()
        {
            var light = Cover.Classify(P(2, 2), P(8, 2), P(6, 2), 1, 1);
            var strong = Cover.Classify(P(2, 2), P(8, 2), P(7, 2), 2, 1);
            Assert.That(Cover.Strongest(light, strong), Is.EqualTo(CoverLevel.Strong));
            Assert.That(Cover.Strongest(strong, light), Is.EqualTo(CoverLevel.Strong));
        }

        [Test]
        public void CoverAndPreviewAreDeterministicAndDoNotMutateStateOrRng()
        {
            var state = Shot(Unit(3, UnitProfile.HumanWarriorTI, x: 7, y: 2));
            var before = Snapshot(state);
            for (int i = 0; i < 5; i++)
            {
                Assert.That(Cover.Query(state, state.FindUnit(Attacker), state.FindUnit(Target)), Is.EqualTo(CoverLevel.Light));
                Assert.That(BattleResolver.PreviewAttack(state, Attack()).ContactChance, Is.EqualTo(65));
            }
            Assert.That(Snapshot(state), Is.EqualTo(before));
        }

        [TestCase(UnitStatus.Dead)]
        [TestCase(UnitStatus.Escaped)]
        public void InactiveBodiesDoNotGiveCover(UnitStatus status)
        {
            var state = Shot(Unit(3, UnitProfile.HumanWarriorTI, x: 7, y: 2, hp: status == UnitStatus.Dead ? 0 : 40, status: status));
            Assert.That(BattleResolver.PreviewAttack(state, Attack()).Cover, Is.EqualTo(CoverLevel.None));
        }

        [Test]
        public void SupercoverCornerTouchesCountButOffLineUnitsDoNot()
        {
            var state = ToActor(BattleResolver.StartBattle(new[] {
                Unit(1, UnitProfile.HumanArcherTI, x: 2, y: 2),
                Unit(2, UnitProfile.HumanWarriorTI, Side.East, x: 4, y: 4),
                Unit(3, UnitProfile.HumanWarriorTI, x: 4, y: 3)
            }, 1).State, Attacker);
            Assert.That(BattleResolver.PreviewAttack(state, Attack()).Cover, Is.EqualTo(CoverLevel.Light));
            Assert.That(BattleResolver.PreviewAttack(Shot(Unit(3, UnitProfile.HumanWarriorTI, x: 7, y: 3)), Attack()).Cover, Is.EqualTo(CoverLevel.None));
        }
    }
}

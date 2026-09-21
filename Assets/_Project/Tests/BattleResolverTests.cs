using System;
using System.Linq;
using NUnit.Framework;
using RPG.Core;
using static RPG.Tests.BattleTestFixtures;

namespace RPG.Tests
{
    public class BattleResolverTests
    {
        [Test]
        public void ArmorAbsorbsDamageBeforeHp()
        {
            var result = BattleResolver.Apply(Duel(), Attack());
            Assert.That(result.IsApplied, Is.True);
            Assert.That(result.State.FindUnit(Target).Armor, Is.Zero);
            Assert.That(result.State.FindUnit(Target).Hp, Is.EqualTo(20));
            var losses = result.Events.Where(e => e.Kind == BattleEventKind.ArmorLost || e.Kind == BattleEventKind.HpLost).ToArray();
            Assert.That(losses.Select(e => e.Kind), Is.EqualTo(new[] { BattleEventKind.ArmorLost, BattleEventKind.HpLost }));
            Assert.That(losses[0].Before, Is.EqualTo(4)); Assert.That(losses[0].After, Is.Zero);
            Assert.That(losses[1].Before, Is.EqualTo(28)); Assert.That(losses[1].After, Is.EqualTo(20));
        }

        [Test]
        public void DefendingReducesTwelveDamageToNineBeforeArmor()
        {
            var state = ToActor(Duel(), Target);
            state = BattleResolver.Apply(state, new DefendCommand(Target)).State;
            state = BattleResolver.Apply(state, new EndActivationCommand(Target)).State;
            var result = BattleResolver.Apply(state, Attack());
            Assert.That(result.State.FindUnit(Target).Armor, Is.Zero);
            Assert.That(result.State.FindUnit(Target).Hp, Is.EqualTo(23));
            Assert.That(result.Events.Single(e => e.Kind == BattleEventKind.DamageApplied).Amount, Is.EqualTo(9));
            Assert.That(result.Events.Any(e => e.Kind == BattleEventKind.GuardRolled), Is.False);
        }

        [Test]
        public void ResistanceRoundsDownFractionalDamage()
        {
            var state = Duel(attacker: UnitProfile.ElfWarriorTI);
            state.FindUnit(Target).IsDefending = true; // Prepared defensive fixture.
            var result = BattleResolver.Apply(state, Attack());
            Assert.That(result.Events.Single(e => e.Kind == BattleEventKind.DamageApplied).Amount, Is.EqualTo(8));
            Assert.That(result.State.FindUnit(Target).Hp, Is.EqualTo(24));
        }

        [Test]
        public void DefendConsumesActionAndAllRemainingMovement()
        {
            var state = Duel();
            var result = BattleResolver.Apply(state, new DefendCommand(Attacker));
            var unit = result.State.FindUnit(Attacker);
            Assert.That(unit.IsDefending, Is.True);
            Assert.That(unit.PhysicalResistance, Is.EqualTo(25));
            Assert.That(unit.ActionAvailable, Is.False);
            Assert.That(unit.MovementRemaining, Is.Zero);
            Assert.That(result.Events.Single(e => e.Kind == BattleEventKind.MovementConsumed).Amount, Is.EqualTo(4));
            Assert.That(result.State.RngState, Is.EqualTo(state.RngState));
        }

        [Test]
        public void CannotDefendAfterMovementWasSpentEvenIfRemainingWasRestored()
        {
            var state = Duel();
            state.FindUnit(Attacker).MovementSpentThisActivation = 1;
            // Deliberately keep full remaining Movement: legality uses history, not position/pool.
            string before = Snapshot(state);
            var result = BattleResolver.Apply(state, new DefendCommand(Attacker));
            Assert.That(result.Error, Is.EqualTo(CommandError.MovementAlreadySpent));
            Assert.That(Snapshot(result.State), Is.EqualTo(before));
            Assert.That(result.Events, Is.Empty);
        }

        [TestCase(1u)]
        [TestCase(5u)]
        public void BasicAttackConsumesActionExactlyOnceOnHitOrMiss(uint seed)
        {
            var first = BattleResolver.Apply(Duel(seed: seed), Attack());
            Assert.That(first.Events.Count(e => e.Kind == BattleEventKind.ActionConsumed), Is.EqualTo(1));
            Assert.That(first.State.FindUnit(Attacker).ActionAvailable, Is.False);
            var second = BattleResolver.Apply(first.State, Attack());
            Assert.That(second.Error, Is.EqualTo(CommandError.NoAction));
            Assert.That(second.State, Is.SameAs(first.State));
            Assert.That(second.Events, Is.Empty);
        }

        [Test]
        public void OverkillClampsHpAndArmorToZeroAndEmitsDeathOnce()
        {
            var result = BattleResolver.Apply(Duel(targetHp: 3, targetArmor: 1), Attack());
            var target = result.State.FindUnit(Target);
            Assert.That(target.Hp, Is.Zero); Assert.That(target.Armor, Is.Zero);
            Assert.That(target.Status, Is.EqualTo(UnitStatus.Dead));
            Assert.That(result.Events.Count(e => e.Kind == BattleEventKind.UnitDied), Is.EqualTo(1));
            Assert.That(result.Events.Single(e => e.Kind == BattleEventKind.HpLost).Amount, Is.EqualTo(3));
            Assert.That(result.State.ActivationOrder, Has.No.Member(Target));
        }

        [Test]
        public void GuardSuccessPreventsAllDamage()
        {
            var state = Duel(target: UnitProfile.HumanWarriorTI, targetFacing: Facing.West, seed: 31);
            var result = BattleResolver.Apply(state, Attack());
            Assert.That(result.Events.Single(e => e.Kind == BattleEventKind.ContactRolled).Roll, Is.EqualTo(26));
            Assert.That(result.Events.Single(e => e.Kind == BattleEventKind.GuardRolled).Roll, Is.EqualTo(5));
            Assert.That(result.Events.Count(e => e.Kind == BattleEventKind.GuardSucceeded), Is.EqualTo(1));
            Assert.That(result.Events.Any(e => e.Kind == BattleEventKind.DamageApplied), Is.False);
            Assert.That(result.State.FindUnit(Target).Armor, Is.EqualTo(16));
            Assert.That(result.State.FindUnit(Target).Hp, Is.EqualTo(40));
        }

        [Test]
        public void GuardUsesSeparateSecondRollOnlyAfterContact()
        {
            var state = Duel(target: UnitProfile.HumanWarriorTI, targetFacing: Facing.West);
            var result = BattleResolver.Apply(state, Attack());
            var rolls = result.Events.Where(e => e.Kind == BattleEventKind.ContactRolled || e.Kind == BattleEventKind.GuardRolled).ToArray();
            Assert.That(rolls.Select(e => e.Kind), Is.EqualTo(new[] { BattleEventKind.ContactRolled, BattleEventKind.GuardRolled }));
            Assert.That(rolls.Select(e => e.Roll), Is.EqualTo(new[] { 61, 95 }));
            Assert.That(result.State.RngState, Is.EqualTo(307599695u));
            Assert.That(result.State.FindUnit(Target).Armor, Is.EqualTo(4));
        }

        [Test]
        public void MissHasNoGuardRollOrDamageAndConsumesOnlyOneRandomRoll()
        {
            var state = Duel(target: UnitProfile.HumanWarriorTI, targetFacing: Facing.West, seed: 5);
            var result = BattleResolver.Apply(state, Attack());
            Assert.That(result.Events.Count(e => e.Kind == BattleEventKind.AttackMissed), Is.EqualTo(1));
            Assert.That(result.Events.Any(e => e.Kind == BattleEventKind.GuardRolled || e.Kind == BattleEventKind.DamageApplied), Is.False);
            Assert.That(result.State.RngState, Is.EqualTo(3472693697u));
        }

        [TestCase(Facing.West, 20)]
        [TestCase(Facing.NorthWest, 20)]
        [TestCase(Facing.SouthWest, 20)]
        [TestCase(Facing.North, 0)]
        [TestCase(Facing.East, 0)]
        public void ShieldGuardIsFrontalOnly(Facing facing, int chance)
        {
            var state = Duel(target: UnitProfile.HumanWarriorTI, targetFacing: facing);
            Assert.That(BattleResolver.PreviewAttack(state, Attack()).GuardChance, Is.EqualTo(chance));
        }

        [TestCase(Facing.West, 60)]
        [TestCase(Facing.North, 75)]
        [TestCase(Facing.East, 75)]
        public void ElfFrontalEvasionChangesContactNotDamage(Facing facing, int chance)
        {
            var preview = BattleResolver.PreviewAttack(Duel(target: UnitProfile.ElfWarriorTI, targetFacing: facing), Attack());
            Assert.That(preview.ContactChance, Is.EqualTo(chance));
            Assert.That(preview.PhysicalDamage, Is.EqualTo(12));
            Assert.That(preview.GuardChance, Is.Zero);
        }

        [Test]
        public void SteadyAimExtendsRangeAppliesDistancePenaltyAndConsumesMovement()
        {
            var state = Duel(attacker: UnitProfile.HumanArcherTI, targetX: 7);
            var preview = BattleResolver.PreviewAttack(state, Attack());
            Assert.That(preview.IsLegal, Is.True);
            Assert.That(preview.MaximumRange, Is.EqualTo(7));
            Assert.That(preview.ContactChance, Is.EqualTo(75)); // 80 + 15 - 5 - 15.
            var result = BattleResolver.Apply(state, Attack());
            Assert.That(result.State.FindUnit(Attacker).MovementRemaining, Is.Zero);
            Assert.That(result.Events.Single(e => e.Kind == BattleEventKind.DamageApplied).Amount, Is.EqualTo(10));
            Assert.That(result.Events.Count(e => e.Kind == BattleEventKind.SteadyAimApplied), Is.EqualTo(1));
        }

        [Test]
        public void SpentMovementDisablesSteadyAimRangeAndAccuracy()
        {
            var state = Duel(attacker: UnitProfile.HumanArcherTI, targetX: 6);
            state.FindUnit(Attacker).MovementSpentThisActivation = 1;
            state.FindUnit(Attacker).MovementRemaining = 3;
            var preview = BattleResolver.PreviewAttack(state, Attack());
            Assert.That(preview.MaximumRange, Is.EqualTo(6));
            Assert.That(preview.ContactChance, Is.EqualTo(65));
            Assert.That(preview.SteadyAim, Is.False);
            var far = Duel(attacker: UnitProfile.HumanArcherTI, targetX: 7);
            far.FindUnit(Attacker).MovementSpentThisActivation = 1;
            Assert.That(BattleResolver.Validate(far, Attack()), Is.EqualTo(CommandError.OutOfRange));
        }

        [Test]
        public void PrototypeProfilesMatchContractTuning()
        {
            var profiles = new[] { UnitProfile.HumanWarriorTI, UnitProfile.HumanArcherTI, UnitProfile.ElfWarriorTI };
            var expected = new[] {
                new[] { 40, 16, 4, 10, 85, 5, 20, 12, 1 },
                new[] { 28, 4, 4, 12, 80, 5, 0, 10, 6 },
                new[] { 32, 6, 6, 14, 85, 10, 0, 11, 1 }
            };
            for (int i = 0; i < profiles.Length; i++)
            {
                var p = profiles[i];
                Assert.That(new[] { p.MaxHp, p.MaxArmor, p.Movement, p.Initiative, p.Accuracy, p.Dodge, p.Guard, p.BasicDamage, p.Range }, Is.EqualTo(expected[i]));
            }
        }

        [Test]
        public void FriendlyTargetRequiresExplicitConfirmationAndSelfTargetIsNeverLegal()
        {
            var state = Duel(targetSide: Side.West);
            Assert.That(BattleResolver.Apply(state, Attack()).Error, Is.EqualTo(CommandError.FriendlyFireNotConfirmed));
            Assert.That(BattleResolver.Apply(state, new BasicAttackCommand(Attacker, Target, true)).IsApplied, Is.True);
            Assert.That(BattleResolver.Apply(state, new BasicAttackCommand(Attacker, Attacker, true)).Error, Is.EqualTo(CommandError.SelfTarget));
        }

        [TestCase(1u)]
        [TestCase(2u)]
        [TestCase(31u)]
        public void UnguardedAttackHasOnlyFixedPhysicalDamageAndNoRetaliation(uint seed)
        {
            var state = Duel(seed: seed);
            var result = BattleResolver.Apply(state, Attack());
            Assert.That(result.Events.Select(e => e.Kind), Is.EqualTo(new[] {
                BattleEventKind.ActionConsumed, BattleEventKind.ContactRolled, BattleEventKind.DamageApplied,
                BattleEventKind.ArmorLost, BattleEventKind.HpLost
            }));
            Assert.That(result.Events.Single(e => e.Kind == BattleEventKind.DamageApplied).Amount, Is.EqualTo(12));
            Assert.That(result.State.FindUnit(Attacker).Hp, Is.EqualTo(40));
            Assert.That(result.State.FindUnit(Attacker).Armor, Is.EqualTo(16));
            Assert.That(result.State.FindUnit(Target).Armor, Is.Zero);
            Assert.That(result.State.FindUnit(Target).Hp, Is.EqualTo(20));
        }

        [Test]
        public void AttackTurnsOnlyTheAttackerEvenOnMiss()
        {
            var state = Duel(seed: 5, targetX: 0, targetY: 1, targetFacing: Facing.South);
            var result = BattleResolver.Apply(state, Attack());
            Assert.That(result.State.FindUnit(Attacker).Facing, Is.EqualTo(Facing.North));
            Assert.That(result.State.FindUnit(Target).Facing, Is.EqualTo(Facing.South));
        }

        [Test]
        public void RangeUsesChebyshevDistance()
        {
            Assert.That(BattleResolver.Validate(Duel(targetX: 1, targetY: 1), Attack()), Is.EqualTo(CommandError.None));
            Assert.That(BattleResolver.Validate(Duel(targetX: 2, targetY: 1), Attack()), Is.EqualTo(CommandError.OutOfRange));
        }
    }
}

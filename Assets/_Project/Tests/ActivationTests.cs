using System;
using System.Linq;
using NUnit.Framework;
using RPG.Core;
using static RPG.Tests.BattleTestFixtures;

namespace RPG.Tests
{
    public class ActivationTests
    {
        [Test]
        public void EachActiveUnitActsOncePerRoundInDescendingInitiative()
        {
            var state = BattleResolver.StartBattle(new[] {
                Unit(1, UnitProfile.HumanWarriorTI),
                Unit(2, UnitProfile.HumanArcherTI, x: 1),
                Unit(3, UnitProfile.ElfWarriorTI, x: 2)
            }, 1).State;
            var order = new[] { new UnitId(3), new UnitId(2), new UnitId(1) };
            for (int round = 1; round <= 2; round++)
            foreach (var actor in order)
            {
                Assert.That(state.Round, Is.EqualTo(round));
                Assert.That(state.CurrentUnitId, Is.EqualTo(actor));
                state = BattleResolver.Apply(state, new EndActivationCommand(actor)).State;
            }
            Assert.That(state.Round, Is.EqualTo(3));
        }

        [Test]
        public void EqualInitiativeUsesSeededKeysThatPersistAcrossRounds()
        {
            var start = BattleResolver.StartBattle(new[] {
                Unit(1, UnitProfile.HumanWarriorTI), Unit(2, UnitProfile.HumanWarriorTI, x: 1)
            }, 12345);
            Assert.That(start.State.FindUnit(Attacker).TieKey, Is.EqualTo(3336926330u));
            Assert.That(start.State.FindUnit(Target).TieKey, Is.EqualTo(1697253807u));
            Assert.That(start.State.CurrentUnitId, Is.EqualTo(Target)); // Seeded key outranks numeric ID.
            var keys = start.State.Units.Select(u => u.TieKey).ToArray();
            var state = start.State;
            for (int i = 0; i < 4; i++)
                state = BattleResolver.Apply(state, new EndActivationCommand(state.CurrentUnitId.Value)).State;
            Assert.That(state.Units.Select(u => u.TieKey), Is.EqualTo(keys));
            Assert.That(state.RngState, Is.EqualTo(start.State.RngState));
            Assert.That(state.CurrentUnitId, Is.EqualTo(start.State.CurrentUnitId));
        }

        [Test]
        public void DefendingExpiresOnlyAtTheUnitsNextActivationAndResourcesRefresh()
        {
            var state = Duel();
            state = BattleResolver.Apply(state, new DefendCommand(Attacker)).State;
            state = BattleResolver.Apply(state, new EndActivationCommand(Attacker)).State;
            Assert.That(state.CurrentUnitId, Is.EqualTo(Target));
            Assert.That(state.FindUnit(Attacker).IsDefending, Is.True);
            var next = BattleResolver.Apply(state, new EndActivationCommand(Target));
            var unit = next.State.FindUnit(Attacker);
            Assert.That(unit.IsDefending, Is.False); Assert.That(unit.PhysicalResistance, Is.Zero);
            Assert.That(unit.ActionAvailable, Is.True); Assert.That(unit.MovementRemaining, Is.EqualTo(4));
            Assert.That(unit.MovementSpentThisActivation, Is.Zero);
            Assert.That(next.Events.Where(e => e.Actor == Attacker).Select(e => e.Kind),
                Is.EqualTo(new[] { BattleEventKind.DefendExpired, BattleEventKind.ActivationStarted }));
        }

        [Test]
        public void ActivationStartClearsMovementHistory()
        {
            var state = Duel();
            state.FindUnit(Attacker).MovementSpentThisActivation = 2;
            state.FindUnit(Attacker).MovementRemaining = 2;
            state = BattleResolver.Apply(state, new EndActivationCommand(Attacker)).State;
            state = ToActor(state, Attacker);
            Assert.That(state.FindUnit(Attacker).MovementSpentThisActivation, Is.Zero);
            Assert.That(BattleResolver.Validate(state, new DefendCommand(Attacker)), Is.EqualTo(CommandError.None));
        }

        [Test]
        public void DeadAndEscapedUnitsAreSkippedWithoutChangingTheirPools()
        {
            var state = BattleResolver.StartBattle(new[] {
                Unit(1, UnitProfile.HumanWarriorTI),
                Unit(2, UnitProfile.ElfWarriorTI, x: 1, hp: 0),
                Unit(3, UnitProfile.HumanArcherTI, x: 2, hp: 8, armor: 2, status: UnitStatus.Escaped),
                Unit(4, UnitProfile.HumanWarriorTI, Side.East, x: 3)
            }, 1).State;
            for (int i = 0; i < 4; i++)
            {
                Assert.That(state.FindUnit(state.CurrentUnitId.Value).IsActive, Is.True);
                Assert.That(state.ActivationOrder.Count, Is.EqualTo(2));
                state = BattleResolver.Apply(state, new EndActivationCommand(state.CurrentUnitId.Value)).State;
            }
            Assert.That(state.FindUnit(new UnitId(3)).Hp, Is.EqualTo(8));
            Assert.That(state.FindUnit(new UnitId(3)).Armor, Is.EqualTo(2));
        }

        [Test]
        public void UnitKilledBeforeItsTurnDoesNotReceiveAnActivation()
        {
            var state = Duel(attacker: UnitProfile.ElfWarriorTI, target: UnitProfile.HumanWarriorTI, targetHp: 1, targetArmor: 0);
            state = BattleResolver.Apply(state, Attack()).State;
            Assert.That(state.FindUnit(Target).Status, Is.EqualTo(UnitStatus.Dead));
            var result = BattleResolver.Apply(state, new EndActivationCommand(Attacker));
            Assert.That(result.State.Round, Is.EqualTo(2));
            Assert.That(result.State.CurrentUnitId, Is.EqualTo(Attacker));
            Assert.That(result.Events.Any(e => e.Actor == Target && e.Kind == BattleEventKind.ActivationStarted), Is.False);
        }

        [Test]
        public void EndDiscardsUnusedResourcesAndAllowsOneFinalFacingChoice()
        {
            var state = Duel();
            var result = BattleResolver.Apply(state, new EndActivationCommand(Attacker, Facing.NorthWest));
            var ended = result.State.FindUnit(Attacker);
            Assert.That(ended.ActionAvailable, Is.False); Assert.That(ended.MovementRemaining, Is.Zero);
            Assert.That(ended.Facing, Is.EqualTo(Facing.NorthWest));
            Assert.That(BattleResolver.Validate(result.State, new EndActivationCommand(Attacker, Facing.South)),
                Is.EqualTo(CommandError.NotCurrentActor));
        }

        [TestCase(5, 2, Facing.East)]
        [TestCase(5, 3, Facing.NorthEast)]
        [TestCase(-5, 2, Facing.West)]
        [TestCase(2, -5, Facing.South)]
        public void FacingRoundsToNearestOctantRatherThanSignOnly(int x, int y, Facing expected)
        {
            Assert.That(FacingDirections.Toward(new GridPosition(0, 0), new GridPosition(x, y)), Is.EqualTo(expected));
        }

        [Test]
        public void ExtremeCoordinatesDoNotOverflowRangeOrFacing()
        {
            var from = new GridPosition(int.MinValue, int.MinValue);
            var to = new GridPosition(int.MaxValue, int.MaxValue);
            Assert.That(from.DistanceTo(to), Is.EqualTo(4294967295L));
            Assert.That(FacingDirections.Toward(from, to), Is.EqualTo(Facing.NorthEast));
        }

        [Test]
        public void InvalidInitialStatesAreRejected()
        {
            Assert.Throws<ArgumentException>(() => BattleResolver.StartBattle(new[] {
                Unit(1, UnitProfile.HumanWarriorTI), Unit(1, UnitProfile.HumanArcherTI, x: 1)
            }, 1));
            Assert.Throws<ArgumentException>(() => BattleResolver.StartBattle(new[] {
                Unit(1, UnitProfile.HumanWarriorTI), Unit(2, UnitProfile.HumanArcherTI)
            }, 1));
            Assert.Throws<ArgumentException>(() => BattleResolver.StartBattle(new[] {
                Unit(1, UnitProfile.HumanWarriorTI, hp: 0)
            }, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Unit(1, UnitProfile.HumanWarriorTI, hp: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Unit(1, UnitProfile.HumanWarriorTI, armor: -1));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RPG.Core;
using static RPG.Tests.BattleTestFixtures;

namespace RPG.Tests
{
    public class DeterminismTests
    {
        [Test]
        public void RandomHasStableGoldenSequenceAndDefinedZeroSeed()
        {
            var random = new CombatRandom(1);
            Assert.That(new[] { random.NextUInt(), random.NextUInt(), random.NextUInt(), random.NextUInt() },
                Is.EqualTo(new uint[] { 270369, 67634689, 2647435461, 307599695 }));
            var zero = new CombatRandom(0);
            var mapped = new CombatRandom(0x6D2B79F5u);
            Assert.That(zero.State, Is.EqualTo(mapped.State));
            Assert.That(zero.NextUInt(), Is.EqualTo(mapped.NextUInt()));
        }

        [TestCase("missing actor", CommandError.ActorNotFound)]
        [TestCase("wrong actor", CommandError.NotCurrentActor)]
        [TestCase("missing target", CommandError.TargetNotFound)]
        [TestCase("self", CommandError.SelfTarget)]
        [TestCase("out of range", CommandError.OutOfRange)]
        [TestCase("dead target", CommandError.TargetInactive)]
        [TestCase("escaped target", CommandError.TargetInactive)]
        [TestCase("dead actor", CommandError.ActorInactive)]
        [TestCase("no action", CommandError.NoAction)]
        [TestCase("invalid facing", CommandError.InvalidFacing)]
        [TestCase("null", CommandError.InvalidCommand)]
        public void InvalidCommandsPreserveEntireStateAndRng(string scenario, CommandError expected)
        {
            var state = Duel(targetX: scenario == "out of range" ? 2 : 1);
            BattleCommand command = Attack();
            switch (scenario)
            {
                case "missing actor": command = new DefendCommand(new UnitId(99)); break;
                case "wrong actor": command = new DefendCommand(Target); break;
                case "missing target": command = new BasicAttackCommand(Attacker, new UnitId(99)); break;
                case "self": command = new BasicAttackCommand(Attacker, Attacker); break;
                case "dead target": state.FindUnit(Target).Hp = 0; state.FindUnit(Target).Status = UnitStatus.Dead; break;
                case "escaped target": state.FindUnit(Target).Status = UnitStatus.Escaped; break;
                case "dead actor": state.FindUnit(Attacker).Hp = 0; state.FindUnit(Attacker).Status = UnitStatus.Dead; break;
                case "no action": state.FindUnit(Attacker).ActionAvailable = false; break;
                case "invalid facing": command = new EndActivationCommand(Attacker, (Facing)99); break;
                case "null": command = null; break;
            }
            string before = Snapshot(state);
            var result = BattleResolver.Apply(state, command);
            Assert.That(result.Error, Is.EqualTo(expected));
            Assert.That(result.State, Is.SameAs(state));
            Assert.That(Snapshot(result.State), Is.EqualTo(before));
            Assert.That(result.Events, Is.Empty);
        }

        [Test]
        public void RepeatedPreviewsAndQueriesDoNotMutateStateOrRng()
        {
            var state = Duel(target: UnitProfile.HumanWarriorTI, targetFacing: Facing.West);
            string before = Snapshot(state);
            for (int i = 0; i < 5; i++)
            {
                var preview = BattleResolver.PreviewAttack(state, Attack());
                Assert.That(preview.ContactChance, Is.EqualTo(80));
                Assert.That(preview.GuardChance, Is.EqualTo(20));
                Assert.That(preview.PhysicalDamage, Is.EqualTo(12));
                Assert.That(BattleResolver.Validate(state, Attack()), Is.EqualTo(CommandError.None));
                Assert.That(state.ActivationOrder.Count, Is.EqualTo(2));
            }
            Assert.That(BattleResolver.PreviewAttack(state, new BasicAttackCommand(Attacker, new UnitId(99))).IsLegal, Is.False);
            Assert.That(Snapshot(state), Is.EqualTo(before));
        }

        [Test]
        public void AppliedCommandDoesNotMutateItsInputSnapshotOrInitialUnits()
        {
            var initial = new[] { Unit(1, UnitProfile.HumanWarriorTI), Unit(2, UnitProfile.HumanArcherTI, Side.East, x: 1) };
            var state = ToActor(BattleResolver.StartBattle(initial, 1).State, Attacker);
            string before = Snapshot(state);
            var result = BattleResolver.Apply(state, Attack());
            Assert.That(result.State, Is.Not.SameAs(state));
            Assert.That(Snapshot(state), Is.EqualTo(before));
            Assert.That(initial[1].Hp, Is.EqualTo(28));
            Assert.That(initial[0].TieKey, Is.Zero);
            Assert.That(initial[0].ActionAvailable, Is.False);
        }

        [Test]
        public void SameInitialStateSeedAndCommandsProduceIdenticalEventsAndFinalState()
        {
            var initial = new[] { Unit(1, UnitProfile.HumanWarriorTI), Unit(2, UnitProfile.HumanArcherTI, Side.East, x: 1, facing: Facing.West) };
            var first = BattleResolver.StartBattle(initial, 31);
            var second = BattleResolver.StartBattle(initial.Reverse(), 31);
            CollectionAssert.AreEqual(first.Events, second.Events);
            var commands = new List<BattleCommand>();
            var recordedEvents = new List<BattleEvent>();
            var state = first.State;
            // Build a short command log, then replay those exact objects against a fresh state.
            for (int turn = 0; turn < 8; turn++)
            {
                UnitId actor = state.CurrentUnitId.Value;
                UnitId target = actor == Attacker ? Target : Attacker;
                BattleCommand action = turn % 3 == 0 || !state.FindUnit(target).IsActive
                    ? (BattleCommand)new DefendCommand(actor) : new BasicAttackCommand(actor, target);
                var result = BattleResolver.Apply(state, action);
                Assert.That(result.IsApplied, Is.True);
                commands.Add(action); recordedEvents.AddRange(result.Events); state = result.State;
                var end = new EndActivationCommand(actor, Facing.East);
                result = BattleResolver.Apply(state, end);
                commands.Add(end); recordedEvents.AddRange(result.Events); state = result.State;
            }
            var replay = second.State;
            var replayEvents = new List<BattleEvent>();
            foreach (var command in commands)
            {
                BattleResolver.Validate(replay, command);
                if (command is BasicAttackCommand attack) BattleResolver.PreviewAttack(replay, attack);
                // Invalid attempts interleaved during replay must not perturb its random stream.
                var invalid = BattleResolver.Apply(replay, new DefendCommand(new UnitId(99)));
                Assert.That(invalid.State, Is.SameAs(replay));
                var result = BattleResolver.Apply(replay, command);
                replayEvents.AddRange(result.Events); replay = result.State;
            }
            CollectionAssert.AreEqual(recordedEvents, replayEvents);
            Assert.That(Snapshot(replay), Is.EqualTo(Snapshot(state)));
        }
    }
}

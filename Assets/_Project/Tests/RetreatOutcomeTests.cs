using System;
using System.Linq;
using NUnit.Framework;
using RPG.Core;
using static RPG.Tests.BattleTestFixtures;
using static RPG.Tests.GridTestFixtures;

namespace RPG.Tests
{
    public class RetreatOutcomeTests
    {
        private static BattleState Retreat(Side side = Side.West, params UnitState[] allies)
        {
            int x = side == Side.West ? 1 : 11;
            var units = new[] {
                Unit(1, UnitProfile.ElfWarriorTI, side, x: x, y: 4, hp: 8, armor: 3),
                Unit(2, UnitProfile.HumanArcherTI, side == Side.West ? Side.East : Side.West, x: 6, y: 7)
            }.Concat(allies);
            return ToActor(BattleResolver.StartBattle(units, 2).State, Attacker);
        }

        [TestCase(Side.West, 0)]
        [TestCase(Side.East, 12)]
        public void OwnEdgeEscapesImmediatelyPreservesPoolsAndTerminatesPath(Side side, int x)
        {
            var state = Retreat(side); string before = Snapshot(state);
            var result = BattleResolver.Apply(state, Move(P(x, 4), P(x, 5)));
            var unit = result.State.FindUnit(Attacker);
            Assert.That(result.IsApplied, Is.True);
            Assert.That(Snapshot(state), Is.EqualTo(before));
            Assert.That(unit.Status, Is.EqualTo(UnitStatus.Escaped));
            Assert.That(unit.Hp, Is.EqualTo(8)); Assert.That(unit.Armor, Is.EqualTo(3));
            Assert.That(unit.Position, Is.EqualTo(P(x, 4)));
            Assert.That(unit.MovementSpentThisActivation, Is.EqualTo(1));
            Assert.That(unit.MovementRemaining, Is.EqualTo(5));
            Assert.That(unit.ActionAvailable, Is.False); Assert.That(unit.OpportunityAttackAvailable, Is.False);
            Assert.That(result.State.OccupantAt(P(x, 4)), Is.Null);
            Assert.That(result.State.ActivationOrder, Has.No.Member(Attacker));
            Assert.That(result.State.Outcome.DefeatedSide, Is.EqualTo(side));
            Assert.That(result.State.Outcome.VictorySide, Is.Not.EqualTo(side));
            Assert.That(result.State.Outcome.Reason, Is.EqualTo(BattleEndReason.Withdrawal));
            Assert.That(result.Events.Count(e => e.Kind == BattleEventKind.StepMoved), Is.EqualTo(1));
            Assert.That(result.Events.Single(e => e.Kind == BattleEventKind.BattleEnded).Outcome, Is.EqualTo(result.State.Outcome));
            Assert.That(result.State.RngState, Is.EqualTo(state.RngState));
        }

        [TestCase(Side.West, 11, 12)]
        [TestCase(Side.East, 1, 0)]
        public void OpponentEdgeDoesNotEscape(Side side, int fromX, int toX)
        {
            var state = Retreat(side); state.FindUnit(Attacker).Position = P(fromX, 4);
            var result = BattleResolver.Apply(state, Move(P(toX, 4)));
            Assert.That(result.State.FindUnit(Attacker).Status, Is.EqualTo(UnitStatus.Active));
            Assert.That(result.State.Outcome.IsEnded, Is.False);
        }

        [Test]
        public void PartialRetreatAutomaticallyAdvancesAndNeverReactivatesEscapedUnit()
        {
            var state = Retreat(Side.West, Unit(3, UnitProfile.HumanWarriorTI, x: 5, y: 1));
            state = BattleResolver.Apply(state, Move(P(0, 4))).State;
            Assert.That(state.Outcome.IsEnded, Is.False);
            Assert.That(state.CurrentUnitId, Is.EqualTo(Target));
            for (int i = 0; i < 6; i++)
            {
                Assert.That(state.CurrentUnitId, Is.Not.EqualTo(Attacker));
                state = BattleResolver.Apply(state, new EndActivationCommand(state.CurrentUnitId.Value)).State;
            }
            Assert.That(state.FindUnit(Attacker).Hp, Is.EqualTo(8));
            Assert.That(state.FindUnit(Attacker).Armor, Is.EqualTo(3));
        }

        [Test]
        public void LastActiveDeathGivesEliminationAndFurtherCommandsCannotMutateFinishedBattle()
        {
            var state = Duel(attacker: UnitProfile.ElfWarriorTI, targetHp: 1, targetArmor: 0);
            var result = BattleResolver.Apply(state, Attack());
            Assert.That(result.State.Outcome.IsEnded, Is.True);
            Assert.That(result.State.Outcome.VictorySide, Is.EqualTo(Side.West));
            Assert.That(result.State.Outcome.DefeatedSide, Is.EqualTo(Side.East));
            Assert.That(result.State.Outcome.Reason, Is.EqualTo(BattleEndReason.Eliminated));
            AssertRejected(result.State, new EndActivationCommand(Attacker), CommandError.BattleAlreadyEnded);
            AssertRejected(result.State, Move(P(1, 1)), CommandError.BattleAlreadyEnded);
            Assert.That(OpportunityAttackPreview.Query(result.State, Move(P(1, 1))).Error, Is.EqualTo(CommandError.BattleAlreadyEnded));
            Assert.That(Pathfinder.FindPath(result.State, Attacker, P(1, 1)).Found, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CommanderRemovalHasNoSpecialArmyDefeatRule(bool escape)
        {
            // ID 1 is the fixture's Commander identity; Core has no special Commander-loss rule.
            var initial = new[] {
                Unit(1, UnitProfile.HumanWarriorTI, x: 1, y: 4, hp: 1, armor: 0),
                Unit(2, UnitProfile.ElfWarriorTI, Side.East, x: 2, y: 4),
                Unit(3, UnitProfile.HumanWarriorTI, x: 7, y: 1)
            };
            var state = BattleResolver.StartBattle(initial, 2).State;
            if (escape)
            {
                state.FindUnit(Target).OpportunityAttackAvailable = false;
                state = ToActor(state, Attacker);
                state = BattleResolver.Apply(state, Move(P(0, 4))).State;
            }
            else state = BattleResolver.Apply(state, new BasicAttackCommand(Target, Attacker)).State;
            Assert.That(state.FindUnit(Attacker).Status, Is.EqualTo(escape ? UnitStatus.Escaped : UnitStatus.Dead));
            Assert.That(state.Outcome.IsEnded, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EarlierEvacueesStaySafeWhetherLastAllyWithdrawsOrDies(bool withdraw)
        {
            var state = Retreat(Side.West, Unit(3, UnitProfile.HumanWarriorTI, x: 1, y: 7, hp: 1, armor: 0));
            state = BattleResolver.Apply(state, Move(P(0, 4))).State;
            if (withdraw)
            {
                state = ToActor(state, new UnitId(3));
                state = BattleResolver.Apply(state, new MoveCommand(new UnitId(3), new[] { P(0, 7) })).State;
            }
            else
            {
                var result = BattleResolver.Apply(state, new BasicAttackCommand(Target, new UnitId(3)));
                // If the fixed first roll misses, take the next activation and fire again.
                if (result.State.FindUnit(new UnitId(3)).IsActive)
                {
                    state = ToActor(BattleResolver.Apply(result.State, new EndActivationCommand(Target)).State, Target);
                    result = BattleResolver.Apply(state, new BasicAttackCommand(Target, new UnitId(3)));
                }
                state = result.State;
            }
            Assert.That(state.Outcome.Reason, Is.EqualTo(withdraw ? BattleEndReason.Withdrawal : BattleEndReason.Eliminated));
            Assert.That(state.FindUnit(Attacker).Status, Is.EqualTo(UnitStatus.Escaped));
            Assert.That(state.FindUnit(Attacker).Hp, Is.EqualTo(8)); Assert.That(state.FindUnit(Attacker).Armor, Is.EqualTo(3));
        }

        [Test]
        public void OaKillsBeforeSafetyAndDoesNotPayTheRetreatStep()
        {
            var state = ToActor(BattleResolver.StartBattle(new[] {
                Unit(1, UnitProfile.ElfWarriorTI, x: 1, y: 4, facing: Facing.West, hp: 1, armor: 0),
                Unit(2, UnitProfile.HumanWarriorTI, Side.East, x: 2, y: 4)
            }, 2).State, Attacker);
            var result = BattleResolver.Apply(state, Move(P(0, 4)));
            Assert.That(result.State.FindUnit(Attacker).Status, Is.EqualTo(UnitStatus.Dead));
            Assert.That(result.State.FindUnit(Attacker).Position, Is.EqualTo(P(1, 4)));
            Assert.That(result.State.FindUnit(Attacker).MovementRemaining, Is.EqualTo(6));
            Assert.That(result.State.Outcome.Reason, Is.EqualTo(BattleEndReason.Eliminated));
            Assert.That(result.Events.Any(e => e.Kind == BattleEventKind.UnitEscaped), Is.False);
        }

        [Test]
        public void SurvivingOaReachesSafetyWithDamagedPools()
        {
            var state = BattleResolver.StartBattle(new[] {
                Unit(1, UnitProfile.ElfWarriorTI, x: 1, y: 4, facing: Facing.West),
                Unit(2, UnitProfile.HumanWarriorTI, Side.East, x: 2, y: 4)
            }, 2).State;
            var result = BattleResolver.Apply(state, Move(P(0, 4)));
            Assert.That(result.State.FindUnit(Attacker).Status, Is.EqualTo(UnitStatus.Escaped));
            Assert.That(result.State.FindUnit(Attacker).Hp, Is.EqualTo(26));
            Assert.That(result.State.FindUnit(Attacker).Armor, Is.Zero);
            Assert.That(result.State.Outcome.Reason, Is.EqualTo(BattleEndReason.Withdrawal));
        }

        [Test]
        public void PreviewStopsAtEscapeAndPathfinderDoesNotRouteThroughOwnRetreatEdge()
        {
            var state = Retreat();
            var preview = OpportunityAttackPreview.Query(state, Move(P(0, 4), P(0, 5)));
            Assert.That(preview.IsLegal, Is.True); Assert.That(preview.Exposures, Is.Empty);
            // Own edge could otherwise bypass the solid cell, but it terminates execution.
            var board = new Battlefield(new[] { P(1, 5), P(2, 5) });
            state = BattleResolver.StartBattle(new[] { Unit(1, UnitProfile.HumanWarriorTI, x: 1, y: 4) }, 2, board).State;
            var path = Pathfinder.FindPath(state, Attacker, P(1, 6));
            Assert.That(path.Found, Is.False); // The non-escape route around x=3 costs more than 4.
        }

        [Test]
        public void PreviewDoesNotReportHypotheticalExitAfterUnitHasAlreadyEscaped()
        {
            var state = BattleResolver.StartBattle(new[] {
                Unit(1, UnitProfile.ElfWarriorTI, x: 1, y: 4),
                Unit(2, UnitProfile.HumanWarriorTI, Side.East, x: 1, y: 3)
            }, 2).State;
            Assert.That(ZoneOfControl.Reactors(state, Attacker, P(0, 4), P(0, 5)), Contains.Item(Target));
            var command = Move(P(0, 4), P(0, 5), P(0, 6));
            Assert.That(OpportunityAttackPreview.Query(state, command).Exposures, Is.Empty);
            var result = BattleResolver.Apply(state, command);
            Assert.That(result.State.FindUnit(Attacker).Status, Is.EqualTo(UnitStatus.Escaped));
            Assert.That(result.Events.Any(e => e.Kind == BattleEventKind.OpportunityAttackTriggered), Is.False);
            Assert.That(result.State.FindUnit(Target).OpportunityAttackAvailable, Is.True);
        }

        [Test]
        public void DeathDuringMovementAdvancesToNextActorWhenAlliesRemain()
        {
            var state = BattleResolver.StartBattle(new[] {
                Unit(1, UnitProfile.ElfWarriorTI, x: 2, y: 2, hp: 1, armor: 0),
                Unit(2, UnitProfile.HumanWarriorTI, Side.East, x: 3, y: 2),
                Unit(3, UnitProfile.HumanArcherTI, x: 8, y: 6)
            }, 2).State;
            var result = BattleResolver.Apply(state, Move(P(1, 2)));
            Assert.That(result.State.FindUnit(Attacker).Status, Is.EqualTo(UnitStatus.Dead));
            Assert.That(result.State.Outcome.IsEnded, Is.False);
            Assert.That(result.State.CurrentUnitId, Is.EqualTo(new UnitId(3)));
        }

        [Test]
        public void NoActiveUnitsIsScenarioErrorNotDrawAndFailedScenarioDoesNotMutateSource()
        {
            Assert.Throws<ArgumentException>(() => BattleResolver.StartBattle(new[] { Unit(1, UnitProfile.HumanWarriorTI, hp: 0) }, 2));
            var state = BattleResolver.StartBattle(new[] { Unit(1, UnitProfile.HumanWarriorTI, x: 1, y: 4) }, 2).State;
            string before = Snapshot(state);
            Assert.Throws<InvalidOperationException>(() => BattleResolver.Apply(state, Move(P(0, 4))));
            Assert.That(Snapshot(state), Is.EqualTo(before));
        }

        [Test]
        public void MovementOaEscapeReplayHasIdenticalEventsAndFinalOutcomeIncludingQueries()
        {
            var units = new[] {
                Unit(1, UnitProfile.ElfWarriorTI, x: 2, y: 2, facing: Facing.West),
                Unit(2, UnitProfile.HumanWarriorTI, Side.East, x: 3, y: 2)
            };
            var a = BattleResolver.StartBattle(units, 2).State;
            var b = BattleResolver.StartBattle(units.Reverse(), 2).State;
            foreach (var command in new[] { Move(P(1, 2)), Move(P(0, 2)) })
            {
                OpportunityAttackPreview.Query(b, command);
                ZoneOfControl.Reactors(b, Attacker, b.FindUnit(Attacker).Position, command.Path[0]);
                AssertRejected(b, Move(P(12, 8)), CommandError.InvalidStep);
                var ra = BattleResolver.Apply(a, command); var rb = BattleResolver.Apply(b, command);
                Assert.That(ra.IsApplied, Is.True); CollectionAssert.AreEqual(ra.Events, rb.Events);
                a = ra.State; b = rb.State;
            }
            Assert.That(Snapshot(a), Is.EqualTo(Snapshot(b)));
            Assert.That(a.Outcome.Reason, Is.EqualTo(BattleEndReason.Withdrawal));
        }
    }
}

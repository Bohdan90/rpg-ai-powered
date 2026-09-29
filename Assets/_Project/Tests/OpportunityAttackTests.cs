using System;
using System.Linq;
using NUnit.Framework;
using RPG.Core;
using static RPG.Tests.BattleTestFixtures;
using static RPG.Tests.GridTestFixtures;

namespace RPG.Tests
{
    public class OpportunityAttackTests
    {
        private static BattleState Engagement(UnitProfile moverProfile = null, Facing facing = Facing.West,
            int? hp = null, int? armor = null, uint seed = 2, params UnitState[] enemies)
        {
            if (enemies.Length == 0) enemies = new[] { Unit(2, UnitProfile.HumanWarriorTI, Side.East, x: 3, y: 2, facing: Facing.North) };
            return ToActor(BattleResolver.StartBattle(new[] {
                Unit(1, moverProfile ?? UnitProfile.ElfWarriorTI, x: 2, y: 2, facing: facing, hp: hp, armor: armor)
            }.Concat(enemies), seed).State, Attacker);
        }
        private static BattleEvent[] Of(BattleResult result, BattleEventKind kind) => result.Events.Where(e => e.Kind == kind).ToArray();

        [TestCase(UnitProfileId.HumanWarriorTI, true)]
        [TestCase(UnitProfileId.ElfWarriorTI, true)]
        [TestCase(UnitProfileId.HumanArcherTI, false)]
        public void OnlyMeleeProfilesExertZocAndStartWithAvailability(UnitProfileId profile, bool expected)
        {
            var p = profile == UnitProfileId.HumanWarriorTI ? UnitProfile.HumanWarriorTI
                : profile == UnitProfileId.ElfWarriorTI ? UnitProfile.ElfWarriorTI : UnitProfile.HumanArcherTI;
            var state = Engagement(enemies: new[] { Unit(2, p, Side.East, x: 3, y: 2) });
            Assert.That(ZoneOfControl.Exerts(state, state.FindUnit(Target), P(2, 2)), Is.EqualTo(expected));
            Assert.That(state.FindUnit(Target).OpportunityAttackAvailable, Is.EqualTo(expected));
            Assert.That(ZoneOfControl.Sources(state, Side.West, P(2, 2)).Contains(Target), Is.EqualTo(expected));
            Assert.That(ZoneOfControl.Sources(state, Side.East, P(2, 2)), Is.Empty);
        }

        [Test]
        public void ZocHasEightNeighboursButNoSealedCornerOrSolidCell()
        {
            var unit = Unit(1, UnitProfile.HumanWarriorTI, x: 4, y: 4);
            var open = BattleResolver.StartBattle(new[] { unit }, 2).State;
            int count = 0;
            for (int x = 3; x <= 5; x++) for (int y = 3; y <= 5; y++)
                if (ZoneOfControl.Exerts(open, open.FindUnit(Attacker), P(x, y))) count++;
            Assert.That(count, Is.EqualTo(8));
            var wall = BattleResolver.StartBattle(new[] { unit }, 2, new Battlefield(new[] { P(5, 4) })).State;
            Assert.That(ZoneOfControl.Exerts(wall, wall.FindUnit(Attacker), P(5, 5)), Is.True);
            Assert.That(ZoneOfControl.Exerts(wall, wall.FindUnit(Attacker), P(5, 4)), Is.False);
            Assert.That(ZoneOfControl.Exerts(wall, wall.FindUnit(Attacker), P(3, 5)), Is.True);
            Assert.That(ZoneOfControl.Exerts(open, open.FindUnit(Attacker), P(6, 4)), Is.False);
            var sealedCorner = BattleResolver.StartBattle(new[] { unit }, 2, new Battlefield(new[] { P(5, 4), P(4, 5) })).State;
            Assert.That(ZoneOfControl.Exerts(sealedCorner, sealedCorner.FindUnit(Attacker), P(5, 5)), Is.False);
        }

        [TestCase(1, 2, 2, 2)] // Enter.
        [TestCase(2, 2, 2, 3)] // Remain adjacent.
        public void EnteringOrRemainingAdjacentDoesNotTrigger(int fx, int fy, int tx, int ty)
        {
            var state = Engagement();
            state.FindUnit(Attacker).Position = P(fx, fy);
            Assert.That(ZoneOfControl.Reactors(state, Attacker, P(fx, fy), P(tx, ty)), Is.Empty);
            var result = BattleResolver.Apply(state, Move(P(tx, ty)));
            Assert.That(result.IsApplied, Is.True);
            Assert.That(Of(result, BattleEventKind.OpportunityAttackTriggered), Is.Empty);
            Assert.That(result.State.RngState, Is.EqualTo(state.RngState));
        }

        [Test]
        public void LeavingResolvesOrdinaryHitBeforeStepAndSurvivorContinuesWithoutResponderRotation()
        {
            var state = Engagement(); string before = Snapshot(state);
            var result = BattleResolver.Apply(state, Move(P(1, 2), P(1, 3)));
            Assert.That(result.IsApplied, Is.True);
            Assert.That(Snapshot(state), Is.EqualTo(before));
            var mover = result.State.FindUnit(Attacker); var enemy = result.State.FindUnit(Target);
            Assert.That(mover.Armor, Is.Zero); Assert.That(mover.Hp, Is.EqualTo(26)); // 12 into Armor 6.
            Assert.That(mover.Position, Is.EqualTo(P(1, 3)));
            Assert.That(mover.MovementRemaining, Is.EqualTo(4));
            Assert.That(mover.ActionAvailable, Is.True);
            Assert.That(enemy.Facing, Is.EqualTo(Facing.North));
            Assert.That(enemy.ActionAvailable, Is.EqualTo(state.FindUnit(Target).ActionAvailable));
            Assert.That(enemy.MovementRemaining, Is.EqualTo(state.FindUnit(Target).MovementRemaining));
            Assert.That(enemy.OpportunityAttackAvailable, Is.False);
            var kinds = result.Events.Select(e => e.Kind).ToList();
            Assert.That(kinds.IndexOf(BattleEventKind.OpportunityAttackResolved), Is.LessThan(kinds.IndexOf(BattleEventKind.StepMoved)));
            Assert.That(Of(result, BattleEventKind.ContactRolled).Single().ChancePercent, Is.EqualTo(80));
            Assert.That(Of(result, BattleEventKind.DamageApplied).Single().Amount, Is.EqualTo(12));
            Assert.That(Of(result, BattleEventKind.ActionConsumed), Is.Empty);
            Assert.That(Of(result, BattleEventKind.OpportunityAttackTriggered).Length, Is.EqualTo(1));
            Assert.That(result.State.FindUnit(Attacker).OpportunityAttackAvailable, Is.True); // No recursive retaliation.
        }

        [TestCase(Facing.East, 70)]
        [TestCase(Facing.West, 80)]
        public void MoverFacingBeforeExitDeterminesFrontalEvasion(Facing facing, int chance)
        {
            var state = Engagement(facing: facing);
            var result = BattleResolver.Apply(state, Move(P(1, 2)));
            Assert.That(Of(result, BattleEventKind.ContactRolled).Single().ChancePercent, Is.EqualTo(chance));
            Assert.That(result.State.FindUnit(Attacker).Facing, Is.EqualTo(Facing.West));
            Assert.That(result.State.FindUnit(Target).Facing, Is.EqualTo(Facing.North));
        }

        [Test]
        public void GuardIsSeparateFromOaAndDoesNotSpendTargetsAvailability()
        {
            var state = Engagement(UnitProfile.HumanWarriorTI, Facing.East, seed: 31);
            // Seed 31 gives contact roll 26 then Guard roll 5 after initial tie keys.
            var result = BattleResolver.Apply(state, Move(P(1, 2)));
            Assert.That(Of(result, BattleEventKind.GuardSucceeded).Length, Is.EqualTo(1));
            Assert.That(Of(result, BattleEventKind.DamageApplied), Is.Empty);
            Assert.That(result.State.FindUnit(Attacker).OpportunityAttackAvailable, Is.True);
            Assert.That(result.State.FindUnit(Attacker).Position, Is.EqualTo(P(1, 2)));
        }

        [Test]
        public void MissStillSpendsOaButDoesNotDamageOrStopMover()
        {
            // Seed 1 now hits (61 < 70); seed 5 gives 97, still a miss after WP-01.
            var state = Engagement(facing: Facing.East, seed: 5);
            var result = BattleResolver.Apply(state, Move(P(1, 2)));
            Assert.That(Of(result, BattleEventKind.ContactRolled).Single().Roll, Is.EqualTo(97));
            Assert.That(Of(result, BattleEventKind.AttackMissed).Length, Is.EqualTo(1));
            Assert.That(result.State.FindUnit(Target).OpportunityAttackAvailable, Is.False);
            Assert.That(result.State.FindUnit(Attacker).Hp, Is.EqualTo(32));
            Assert.That(result.State.FindUnit(Attacker).Armor, Is.EqualTo(6));
            Assert.That(result.State.FindUnit(Attacker).Position, Is.EqualTo(P(1, 2)));
        }

        [Test]
        public void EarlierSuccessfulStepsRemainPaidWhenLaterStepIsInterrupted()
        {
            var state = Engagement(hp: 1, armor: 0);
            state.FindUnit(Attacker).Position = P(1, 2);
            var result = BattleResolver.Apply(state, Move(P(2, 2), P(1, 2), P(1, 3)));
            Assert.That(result.IsApplied, Is.True);
            var mover = result.State.FindUnit(Attacker);
            Assert.That(mover.Status, Is.EqualTo(UnitStatus.Dead));
            Assert.That(mover.Position, Is.EqualTo(P(2, 2)));
            Assert.That(mover.Facing, Is.EqualTo(Facing.East));
            Assert.That(mover.MovementRemaining, Is.EqualTo(5));
            Assert.That(mover.MovementSpentThisActivation, Is.EqualTo(1));
            Assert.That(Of(result, BattleEventKind.MovementConsumed).Sum(e => e.Amount), Is.EqualTo(1));
            Assert.That(Of(result, BattleEventKind.StepMoved).Length, Is.EqualTo(1));
        }

        [Test]
        public void MeleeOaIgnoresRangedCoverFromOccupiedCornerCell()
        {
            var state = Engagement(enemies: new[] {
                Unit(2, UnitProfile.HumanWarriorTI, Side.East, x: 3, y: 3),
                Unit(3, UnitProfile.HumanArcherTI, Side.East, x: 3, y: 2)
            });
            Assert.That(Cover.Query(state, state.FindUnit(Target), state.FindUnit(Attacker)), Is.EqualTo(CoverLevel.Light));
            var result = BattleResolver.Apply(state, Move(P(1, 2)));
            Assert.That(result.IsApplied, Is.True);
            Assert.That(Of(result, BattleEventKind.ContactRolled).Single().ChancePercent, Is.EqualTo(80));
            Assert.That(Of(result, BattleEventKind.DamageApplied).Single().Amount, Is.EqualTo(12));
        }

        [Test]
        public void NormalActionDoesNotSpendOpportunityAndOpportunityDoesNotSpendAction()
        {
            var state = Engagement();
            var hit = BattleResolver.Apply(state, Attack());
            Assert.That(hit.State.FindUnit(Attacker).OpportunityAttackAvailable, Is.True);
            var next = BattleResolver.Apply(hit.State, new EndActivationCommand(Attacker)).State;
            Assert.That(next.CurrentUnitId, Is.EqualTo(Target));
            var result = BattleResolver.Apply(next, new MoveCommand(Target, new[] { P(4, 2) }));
            Assert.That(result.IsApplied, Is.True);
            Assert.That(result.State.FindUnit(Attacker).OpportunityAttackAvailable, Is.False);
            Assert.That(result.State.FindUnit(Attacker).ActionAvailable, Is.False);
            Assert.That(result.State.FindUnit(Target).ActionAvailable, Is.True);
        }

        [Test]
        public void RepeatedExitSpendsOnceThenOwnActivationRefreshes()
        {
            var state = Engagement();
            var result = BattleResolver.Apply(state, Move(P(1, 2), P(2, 2), P(1, 2)));
            Assert.That(Of(result, BattleEventKind.OpportunityAttackTriggered).Length, Is.EqualTo(1));
            Assert.That(result.State.FindUnit(Target).OpportunityAttackAvailable, Is.False);
            Assert.That(ZoneOfControl.Sources(result.State, Side.West, P(2, 2)), Contains.Item(Target));
            Assert.That(ZoneOfControl.Reactors(result.State, Attacker, P(2, 2), P(1, 2)), Is.Empty);
            var next = BattleResolver.Apply(result.State, new EndActivationCommand(Attacker)).State;
            Assert.That(next.CurrentUnitId, Is.EqualTo(Target));
            Assert.That(next.FindUnit(Target).OpportunityAttackAvailable, Is.True);
        }

        [TestCase(UnitStatus.Dead)]
        [TestCase(UnitStatus.Escaped)]
        public void InactiveResponderCannotReact(UnitStatus status)
        {
            var state = Engagement(enemies: new[] { Unit(2, UnitProfile.HumanWarriorTI, Side.East, x: 3, y: 2,
                hp: status == UnitStatus.Dead ? 0 : 40, status: status) });
            Assert.That(state.FindUnit(Target).OpportunityAttackAvailable, Is.False);
            Assert.That(ZoneOfControl.Reactors(state, Attacker, P(2, 2), P(1, 2)), Is.Empty);
            Assert.That(BattleResolver.Apply(state, Move(P(1, 2))).State.RngState, Is.EqualTo(state.RngState));
        }

        [Test]
        public void KillingMoverCancelsPathWithoutChargingOrRotatingUnexecutedStep()
        {
            var state = Engagement(facing: Facing.East, hp: 1, armor: 0);
            var result = BattleResolver.Apply(state, Move(P(1, 2), P(1, 3)));
            Assert.That(result.IsApplied, Is.True);
            var dead = result.State.FindUnit(Attacker);
            Assert.That(dead.Status, Is.EqualTo(UnitStatus.Dead));
            Assert.That(dead.Position, Is.EqualTo(P(2, 2)));
            Assert.That(dead.Facing, Is.EqualTo(Facing.East));
            Assert.That(dead.MovementRemaining, Is.EqualTo(6));
            Assert.That(dead.MovementSpentThisActivation, Is.Zero);
            Assert.That(Of(result, BattleEventKind.MovementConsumed), Is.Empty);
            Assert.That(Of(result, BattleEventKind.StepMoved), Is.Empty);
            Assert.That(Of(result, BattleEventKind.MovementInterruptedByDeath).Length, Is.EqualTo(1));
            Assert.That(result.State.Outcome.Reason, Is.EqualTo(BattleEndReason.Eliminated));
        }

        [Test]
        public void MultipleRespondersUseExistingInitiativePriorityAndStopSpendingAfterKill()
        {
            var enemies = new[] {
                Unit(2, UnitProfile.HumanWarriorTI, Side.East, x: 3, y: 1),
                Unit(3, UnitProfile.ElfWarriorTI, Side.East, x: 3, y: 3)
            };
            var state = Engagement(enemies: enemies);
            var result = BattleResolver.Apply(state, Move(P(1, 2)));
            Assert.That(Of(result, BattleEventKind.OpportunityAttackTriggered).Select(e => e.Actor),
                Is.EqualTo(new UnitId?[] { new UnitId(3), Target }));
            var fragile = Engagement(hp: 1, armor: 0, enemies: enemies);
            var killed = BattleResolver.Apply(fragile, Move(P(1, 2)));
            Assert.That(Of(killed, BattleEventKind.OpportunityAttackTriggered).Length, Is.EqualTo(1));
            Assert.That(killed.State.FindUnit(Target).OpportunityAttackAvailable, Is.True);
            Assert.That(killed.State.FindUnit(new UnitId(3)).OpportunityAttackAvailable, Is.False);
        }

        [Test]
        public void EqualInitiativeResponseFollowsSeededPriorityThenIdNotInputOrder()
        {
            var enemies = new[] {
                Unit(2, UnitProfile.HumanWarriorTI, Side.East, x: 3, y: 1),
                Unit(3, UnitProfile.HumanWarriorTI, Side.East, x: 3, y: 3)
            };
            var state = Engagement(enemies: enemies);
            var result = BattleResolver.Apply(state, Move(P(1, 2)));
            Assert.That(Of(result, BattleEventKind.OpportunityAttackTriggered).Select(e => e.Actor.Value),
                Is.EqualTo(state.PriorityOrder.Where(id => id != Attacker)));
            var reverse = BattleResolver.Apply(Engagement(enemies: enemies.Reverse().ToArray()), Move(P(1, 2)));
            CollectionAssert.AreEqual(result.Events, reverse.Events);
            Assert.That(Snapshot(result.State), Is.EqualTo(Snapshot(reverse.State)));
        }

        [Test]
        public void PathPreviewTracksAvailabilityAcrossExitsWithoutPredictingHitsOrChangingState()
        {
            var state = Engagement(); string before = Snapshot(state);
            var command = Move(P(1, 2), P(2, 2), P(1, 2));
            var preview = OpportunityAttackPreview.Query(state, command);
            Assert.That(preview.IsLegal, Is.True);
            Assert.That(preview.Exposures.Select(e => e.StepIndex), Is.EqualTo(new[] { 0, 2 }));
            Assert.That(preview.Exposures[0].Threats.Single().Responder, Is.EqualTo(Target));
            Assert.That(preview.Exposures[0].Threats.Single().WouldReact, Is.True);
            Assert.That(preview.Exposures[1].Threats.Single().AvailableNow, Is.True);
            Assert.That(preview.Exposures[1].Threats.Single().WouldReact, Is.False);
            OpportunityAttackPreview.Query(state, command);
            Assert.That(Snapshot(state), Is.EqualTo(before));
            state.FindUnit(Target).OpportunityAttackAvailable = false;
            preview = OpportunityAttackPreview.Query(state, command);
            Assert.That(preview.Exposures[0].Threats.Single().AvailableNow, Is.False);
            Assert.That(preview.Exposures[0].Threats.Single().WouldReact, Is.False);
        }

        [Test]
        public void InvalidTailIsRejectedBeforeAnyExposureRollOrMovement()
        {
            var state = Engagement();
            var command = Move(P(1, 2), P(0, 2), P(-1, 2));
            AssertRejected(state, command, CommandError.OutOfBounds);
            string before = Snapshot(state);
            var preview = OpportunityAttackPreview.Query(state, command);
            Assert.That(preview.Error, Is.EqualTo(CommandError.OutOfBounds));
            Assert.That(preview.Exposures, Is.Empty);
            Assert.That(Snapshot(state), Is.EqualTo(before));
        }

        [Test]
        public void MovingIntoAndOutOfEngagementWithinOneCommandTriggersAtExitStepOnly()
        {
            var state = Engagement(); state.FindUnit(Attacker).Position = P(1, 2);
            var result = BattleResolver.Apply(state, Move(P(2, 2), P(1, 2)));
            var kinds = result.Events.Select(e => e.Kind).ToList();
            Assert.That(kinds.IndexOf(BattleEventKind.StepMoved), Is.LessThan(kinds.IndexOf(BattleEventKind.OpportunityAttackTriggered)));
            Assert.That(Of(result, BattleEventKind.OpportunityAttackTriggered).Length, Is.EqualTo(1));
        }
    }
}

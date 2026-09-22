using System;
using System.Collections.Generic;

namespace RPG.Core
{
    public static class BattleResolver
    {
        public static BattleResult StartBattle(IEnumerable<UnitState> units, uint seed, Battlefield battlefield = null)
        {
            var state = new BattleState(units, seed, battlefield);
            var events = new List<BattleEvent> { new BattleEvent(BattleEventKind.BattleStarted, 0) };
            StartNextActivation(state, events);
            return new BattleResult(state, CommandError.None, events);
        }

        // Engagement follows threat geometry, even when the source has spent its OA.
        public static bool IsArcherEngaged(BattleState state, UnitId actorId)
        {
            var actor = state.FindUnit(actorId);
            return actor != null && actor.IsActive && actor.Profile.IsArcher
                && ZoneOfControl.Sources(state, actor.Side, actor.Position).Count > 0;
        }

        public static BasicAttackKind AvailableBasicAttack(BattleState state, UnitId actorId) =>
            IsArcherEngaged(state, actorId) ? BasicAttackKind.MeleeStrike : BasicAttackKind.ProfileBasic;

        public static CommandError Validate(BattleState state, BattleCommand command)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (command == null) return CommandError.InvalidCommand;
            if (state.Outcome.IsEnded) return CommandError.BattleAlreadyEnded;
            var actor = state.FindUnit(command.Actor);
            if (actor == null) return CommandError.ActorNotFound;
            if (!actor.IsActive) return CommandError.ActorInactive;
            if (state.CurrentUnitId != actor.Id) return CommandError.NotCurrentActor;
            if (command is EndActivationCommand end)
                return end.FinalFacing.HasValue && !FacingDirections.IsValid(end.FinalFacing.Value)
                    ? CommandError.InvalidFacing : CommandError.None;
            if (!(command is BasicAttackCommand) && !(command is DefendCommand) && !(command is MoveCommand)) return CommandError.InvalidCommand;
            if (!actor.ActionAvailable) return CommandError.NoAction;
            if (command is MoveCommand move) return MovementRules.ValidatePath(state, actor, move);
            if (command is DefendCommand)
                return actor.MovementSpentThisActivation > 0 ? CommandError.MovementAlreadySpent : CommandError.None;
            var attack = (BasicAttackCommand)command;
            var target = state.FindUnit(attack.Target);
            if (target == null) return CommandError.TargetNotFound;
            if (!target.IsActive) return CommandError.TargetInactive;
            if (actor.Id == target.Id) return CommandError.SelfTarget;
            if (actor.Side == target.Side && !attack.FriendlyFireConfirmed) return CommandError.FriendlyFireNotConfirmed;
            if (attack.Kind != BasicAttackKind.ProfileBasic && attack.Kind != BasicAttackKind.MeleeStrike)
                return CommandError.InvalidCommand;
            bool meleeStrike = attack.Kind == BasicAttackKind.MeleeStrike;
            bool engaged = IsArcherEngaged(state, actor.Id);
            if (meleeStrike && !engaged) return CommandError.MeleeStrikeUnavailable;
            if (!meleeStrike && engaged) return CommandError.Engaged;
            int range = meleeStrike ? 1 : actor.Profile.Range;
            if (actor.Position.DistanceTo(target.Position) > range) return CommandError.OutOfRange;
            if (actor.Profile.IsArcher && !meleeStrike)
            {
                if (!LineOfSight.IsClear(state, actor.Position, target.Position)) return CommandError.BlockedLineOfSight;
            }
            else if (!LineOfSight.IsMeleeCornerClear(state, actor.Position, target.Position)) return CommandError.BlockedCorner;
            return CommandError.None;
        }

        public static AttackPreview PreviewAttack(BattleState state, BasicAttackCommand command)
        {
            var preview = new AttackPreview { Error = Validate(state, command) };
            if (!preview.IsLegal) return preview;
            return CalculateAttack(state, state.FindUnit(command.Actor), state.FindUnit(command.Target), command.Kind);
        }

        // Pure OA math query; does not spend availability or rotate either unit.
        public static AttackPreview PreviewOpportunityAttack(BattleState state, UnitId responderId, UnitId moverId)
        {
            var responder = state.FindUnit(responderId); var mover = state.FindUnit(moverId);
            if (responder == null || mover == null || !mover.IsActive || responder.Side == mover.Side
                || !responder.OpportunityAttackAvailable || !ZoneOfControl.Exerts(state, responder, mover.Position))
                return new AttackPreview { Error = CommandError.InvalidCommand };
            return CalculateAttack(state, responder, mover);
        }

        private static AttackPreview CalculateAttack(BattleState state, UnitState actor, UnitState target,
            BasicAttackKind kind = BasicAttackKind.ProfileBasic)
        {
            bool meleeStrike = kind == BasicAttackKind.MeleeStrike;
            bool ranged = actor.Profile.IsArcher && !meleeStrike;
            var preview = new AttackPreview { Kind = kind };
            preview.Distance = actor.Position.DistanceTo(target.Position);
            preview.SteadyAim = ranged && HasSteadyAim(actor);
            preview.MaximumRange = meleeStrike ? 1 : actor.Profile.Range;
            preview.TargetFacesAttacker = FacingDirections.IsFrontal(target.Facing, target.Position, actor.Position);
            int evasion = preview.TargetFacesAttacker ? target.Profile.FrontalEvasion : 0;
            int penalty = ranged ? 5 * (int)Math.Max(0, preview.Distance - 4) : 0;
            preview.BaseAccuracy = actor.Profile.Accuracy;
            preview.AimModifier = preview.SteadyAim ? 15 : 0;
            preview.DistanceModifier = -penalty;
            preview.TargetDodge = target.Profile.Dodge;
            preview.FrontalEvasion = evasion;
            preview.Cover = ranged ? Cover.Query(state, actor, target) : CoverLevel.None;
            // Current profiles can only yield None/Light. Do not silently invent Strong tuning.
            preview.CoverAccuracyModifier = Cover.AccuracyModifier(preview.Cover)
                ?? throw new InvalidOperationException("Strong Cover tuning is deferred.");
            preview.ContactChance = Math.Max(5, Math.Min(95,
                preview.BaseAccuracy + preview.AimModifier - preview.TargetDodge - preview.FrontalEvasion
                + preview.DistanceModifier + preview.CoverAccuracyModifier));
            preview.GuardChance = preview.TargetFacesAttacker ? target.Profile.Guard : 0;
            preview.PhysicalDamage = (meleeStrike ? 5 : actor.Profile.BasicDamage) * (100 - target.PhysicalResistance) / 100;
            preview.ArmorLossOnUnguardedHit = Math.Min(target.Armor, preview.PhysicalDamage);
            preview.HpLossOnUnguardedHit = Math.Min(target.Hp, preview.PhysicalDamage - preview.ArmorLossOnUnguardedHit);
            return preview;
        }

        public static BattleResult Apply(BattleState state, BattleCommand command)
        {
            CommandError error = Validate(state, command);
            if (error != CommandError.None) return new BattleResult(state, error, Array.Empty<BattleEvent>());
            var next = state.Copy();
            var events = new List<BattleEvent>();
            var actor = next.FindUnit(command.Actor);
            if (command is MoveCommand move)
                Move(next, actor, move, events);
            else if (command is BasicAttackCommand attack)
                Attack(next, actor, next.FindUnit(attack.Target), PreviewAttack(state, attack), events);
            else if (command is DefendCommand)
            {
                ConsumeAction(next, actor, events);
                ConsumeMovement(next, actor, events);
                actor.IsDefending = true;
                events.Add(new BattleEvent(BattleEventKind.DefendApplied, next.Round, actor.Id,
                    amount: 25, before: 0, after: 25));
            }
            else if (command is EndActivationCommand end)
            {
                if (end.FinalFacing.HasValue) SetFacing(next, actor, end.FinalFacing.Value, events);
                ConsumeAction(next, actor, events);
                ConsumeMovement(next, actor, events);
                events.Add(new BattleEvent(BattleEventKind.ActivationEnded, next.Round, actor.Id));
                StartNextActivation(next, events);
            }
            EvaluateOutcome(next, events);
            if (!actor.IsActive)
            {
                events.Add(new BattleEvent(BattleEventKind.ActivationEnded, next.Round, actor.Id));
                if (!next.Outcome.IsEnded) StartNextActivation(next, events);
            }
            return new BattleResult(next, CommandError.None, events);
        }

        private static void EvaluateOutcome(BattleState state, List<BattleEvent> events)
        {
            // Initial one-sided geometry fixtures remain usable. A tactical outcome follows
            // an actual battlefield removal, never a fabricated defeat on fixture startup.
            BattleEvent? removal = null;
            foreach (var e in events)
                if (e.Kind == BattleEventKind.UnitDied || e.Kind == BattleEventKind.UnitEscaped) removal = e;
            if (!removal.HasValue) return;
            bool west = false, east = false;
            foreach (var unit in state.Units)
                if (unit.IsActive) { if (unit.Side == Side.West) west = true; else east = true; }
            if (!west && !east) throw new InvalidOperationException("Scenario error: both sides have no active units.");
            if (west && east) return;
            var removedId = removal.Value.Kind == BattleEventKind.UnitEscaped ? removal.Value.Actor : removal.Value.Target;
            var removedSide = state.FindUnit(removedId.Value).Side;
            if ((removedSide == Side.West && west) || (removedSide == Side.East && east)) return;
            var reason = removal.Value.Kind == BattleEventKind.UnitEscaped ? BattleEndReason.Withdrawal : BattleEndReason.Eliminated;
            state.Outcome = new BattleOutcome(west ? Side.West : Side.East, west ? Side.East : Side.West, reason);
            // CurrentUnitId is retained as the last actor for existing snapshot/view compatibility.
            // Outcome gates commands and activation advancement; it is authoritative for completion.
            events.Add(new BattleEvent(BattleEventKind.BattleEnded, state.Round, outcome: state.Outcome));
        }

        private static void Move(BattleState state, UnitState actor, MoveCommand command, List<BattleEvent> events)
        {
            events.Add(new BattleEvent(BattleEventKind.MovementStarted, state.Round, actor.Id, amount: command.Path.Count));
            foreach (var step in command.Path)
            {
                var from = actor.Position;
                foreach (var responderId in ZoneOfControl.Reactors(state, actor.Id, from, step))
                {
                    if (!actor.IsActive) break;
                    var responder = state.FindUnit(responderId);
                    if (!responder.IsActive || !responder.OpportunityAttackAvailable
                        || !ZoneOfControl.Exerts(state, responder, from) || ZoneOfControl.Exerts(state, responder, step)) continue;
                    events.Add(new BattleEvent(BattleEventKind.ZoCExitDetected, state.Round, actor.Id, responder.Id, from: from, to: step));
                    events.Add(new BattleEvent(BattleEventKind.OpportunityAttackTriggered, state.Round, responder.Id, actor.Id));
                    responder.OpportunityAttackAvailable = false;
                    events.Add(new BattleEvent(BattleEventKind.OpportunityAttackSpent, state.Round, responder.Id, amount: 1, before: 1, after: 0));
                    ResolveContactAndDamage(state, responder, actor, CalculateAttack(state, responder, actor), events);
                    events.Add(new BattleEvent(BattleEventKind.OpportunityAttackResolved, state.Round, responder.Id, actor.Id));
                }
                if (!actor.IsActive)
                {
                    events.Add(new BattleEvent(BattleEventKind.MovementInterruptedByDeath, state.Round, actor.Id, from: from, to: step));
                    break;
                }
                int before = actor.MovementRemaining;
                actor.Position = step;
                actor.MovementRemaining--;
                actor.MovementSpentThisActivation++;
                SetFacing(state, actor, FacingDirections.Toward(from, step), events);
                events.Add(new BattleEvent(BattleEventKind.MovementConsumed, state.Round, actor.Id,
                    amount: 1, before: before, after: actor.MovementRemaining));
                events.Add(new BattleEvent(BattleEventKind.StepMoved, state.Round, actor.Id,
                    amount: 1, from: from, to: step));
                if (state.Battlefield.IsRetreatZone(actor.Side, step))
                {
                    actor.Status = UnitStatus.Escaped;
                    actor.ActionAvailable = false;
                    actor.OpportunityAttackAvailable = false;
                    events.Add(new BattleEvent(BattleEventKind.UnitEscaped, state.Round, actor.Id, from: from, to: step));
                    break;
                }
            }
        }

        private static bool HasSteadyAim(UnitState actor) => actor.Profile.IsArcher && actor.MovementSpentThisActivation == 0;

        private static void Attack(BattleState state, UnitState actor, UnitState target,
            AttackPreview preview, List<BattleEvent> events)
        {
            ConsumeAction(state, actor, events);
            SetFacing(state, actor, FacingDirections.Toward(actor.Position, target.Position), events);
            if (preview.SteadyAim)
            {
                ConsumeMovement(state, actor, events);
                events.Add(new BattleEvent(BattleEventKind.SteadyAimApplied, state.Round, actor.Id, target.Id, amount: 15));
            }
            ResolveContactAndDamage(state, actor, target, preview, events);
        }

        private static void ResolveContactAndDamage(BattleState state, UnitState actor, UnitState target,
            AttackPreview preview, List<BattleEvent> events)
        {
            int roll = state.Random.NextPercent();
            events.Add(new BattleEvent(BattleEventKind.ContactRolled, state.Round, actor.Id, target.Id,
                chancePercent: preview.ContactChance, roll: roll));
            if (roll >= preview.ContactChance)
            {
                events.Add(new BattleEvent(BattleEventKind.AttackMissed, state.Round, actor.Id, target.Id));
                return;
            }
            if (preview.GuardChance > 0)
            {
                roll = state.Random.NextPercent();
                events.Add(new BattleEvent(BattleEventKind.GuardRolled, state.Round, actor.Id, target.Id,
                    chancePercent: preview.GuardChance, roll: roll));
                if (roll < preview.GuardChance)
                {
                    events.Add(new BattleEvent(BattleEventKind.GuardSucceeded, state.Round, actor.Id, target.Id));
                    return;
                }
            }
            events.Add(new BattleEvent(BattleEventKind.DamageApplied, state.Round, actor.Id, target.Id, amount: preview.PhysicalDamage));
            if (preview.ArmorLossOnUnguardedHit > 0)
            {
                int before = target.Armor;
                target.Armor -= preview.ArmorLossOnUnguardedHit;
                events.Add(new BattleEvent(BattleEventKind.ArmorLost, state.Round, actor.Id, target.Id,
                    amount: preview.ArmorLossOnUnguardedHit, before: before, after: target.Armor));
            }
            if (preview.HpLossOnUnguardedHit > 0)
            {
                int before = target.Hp;
                target.Hp -= preview.HpLossOnUnguardedHit;
                events.Add(new BattleEvent(BattleEventKind.HpLost, state.Round, actor.Id, target.Id,
                    amount: preview.HpLossOnUnguardedHit, before: before, after: target.Hp));
            }
            if (target.Hp == 0)
            {
                target.Status = UnitStatus.Dead;
                target.ActionAvailable = false;
                target.OpportunityAttackAvailable = false;
                // Preserve unspent Movement as history: unexecuted steps are never charged.
                // Position is retained as history, but inactive units do not occupy the field.
                events.Add(new BattleEvent(BattleEventKind.UnitDied, state.Round, actor.Id, target.Id));
            }
        }

        private static void ConsumeAction(BattleState state, UnitState actor, List<BattleEvent> events)
        {
            if (!actor.ActionAvailable) return;
            actor.ActionAvailable = false;
            events.Add(new BattleEvent(BattleEventKind.ActionConsumed, state.Round, actor.Id, amount: 1, before: 1, after: 0));
        }
        private static void ConsumeMovement(BattleState state, UnitState actor, List<BattleEvent> events)
        {
            int before = actor.MovementRemaining;
            actor.MovementRemaining = 0;
            if (before > 0) events.Add(new BattleEvent(BattleEventKind.MovementConsumed, state.Round, actor.Id, amount: before, before: before, after: 0));
        }
        private static void SetFacing(BattleState state, UnitState actor, Facing facing, List<BattleEvent> events)
        {
            if (actor.Facing == facing) return;
            Facing before = actor.Facing;
            actor.Facing = facing;
            events.Add(new BattleEvent(BattleEventKind.FacingChanged, state.Round, actor.Id, before: (int)before, after: (int)facing));
        }
        private static void StartNextActivation(BattleState state, List<BattleEvent> events)
        {
            // Called only for ongoing battles with at least one active unit.
            bool anyActive = false;
            foreach (var candidate in state.Units) if (candidate.IsActive) { anyActive = true; break; }
            if (!anyActive) throw new InvalidOperationException("Scenario error: no active unit can activate.");
            while (true)
            {
                state.PriorityIndex++;
                if (state.Round == 0 || state.PriorityIndex == state.PriorityOrder.Count)
                {
                    state.Round++; state.PriorityIndex = 0;
                    events.Add(new BattleEvent(BattleEventKind.RoundStarted, state.Round));
                }
                var unit = state.FindUnit(state.PriorityOrder[state.PriorityIndex]);
                if (!unit.IsActive) continue;
                state.CurrentUnitId = unit.Id;
                if (unit.IsDefending)
                {
                    unit.IsDefending = false;
                    events.Add(new BattleEvent(BattleEventKind.DefendExpired, state.Round, unit.Id, before: 25, after: 0));
                }
                unit.ActionAvailable = true;
                unit.OpportunityAttackAvailable = unit.Profile.HasMeleeBasic;
                unit.MovementRemaining = unit.Profile.Movement;
                unit.MovementSpentThisActivation = 0;
                events.Add(new BattleEvent(BattleEventKind.ActivationStarted, state.Round, unit.Id,
                    amount: 1, after: unit.MovementRemaining));
                return;
            }
        }
    }
}

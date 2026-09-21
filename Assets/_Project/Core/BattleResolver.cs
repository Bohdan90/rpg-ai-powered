using System;
using System.Collections.Generic;

namespace RPG.Core
{
    public static class BattleResolver
    {
        public static BattleResult StartBattle(IEnumerable<UnitState> units, uint seed)
        {
            var state = new BattleState(units, seed);
            var events = new List<BattleEvent> { new BattleEvent(BattleEventKind.BattleStarted, 0) };
            StartNextActivation(state, events);
            return new BattleResult(state, CommandError.None, events);
        }

        public static CommandError Validate(BattleState state, BattleCommand command)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (command == null) return CommandError.InvalidCommand;
            var actor = state.FindUnit(command.Actor);
            if (actor == null) return CommandError.ActorNotFound;
            if (!actor.IsActive) return CommandError.ActorInactive;
            if (state.CurrentUnitId != actor.Id) return CommandError.NotCurrentActor;
            if (command is EndActivationCommand end)
                return end.FinalFacing.HasValue && !FacingDirections.IsValid(end.FinalFacing.Value)
                    ? CommandError.InvalidFacing : CommandError.None;
            if (!(command is BasicAttackCommand) && !(command is DefendCommand)) return CommandError.InvalidCommand;
            if (!actor.ActionAvailable) return CommandError.NoAction;
            if (command is DefendCommand)
                return actor.MovementSpentThisActivation > 0 ? CommandError.MovementAlreadySpent : CommandError.None;
            var attack = (BasicAttackCommand)command;
            var target = state.FindUnit(attack.Target);
            if (target == null) return CommandError.TargetNotFound;
            if (!target.IsActive) return CommandError.TargetInactive;
            if (actor.Id == target.Id) return CommandError.SelfTarget;
            if (actor.Side == target.Side && !attack.FriendlyFireConfirmed) return CommandError.FriendlyFireNotConfirmed;
            int range = actor.Profile.Range + (HasSteadyAim(actor) ? 1 : 0);
            if (actor.Position.DistanceTo(target.Position) > range) return CommandError.OutOfRange;
            // M1 has no board: LoS, occupied screening and solid-corner checks belong to M2.
            return CommandError.None;
        }

        public static AttackPreview PreviewAttack(BattleState state, BasicAttackCommand command)
        {
            var preview = new AttackPreview { Error = Validate(state, command) };
            if (!preview.IsLegal) return preview;
            var actor = state.FindUnit(command.Actor);
            var target = state.FindUnit(command.Target);
            preview.Distance = actor.Position.DistanceTo(target.Position);
            preview.SteadyAim = HasSteadyAim(actor);
            preview.MaximumRange = actor.Profile.Range + (preview.SteadyAim ? 1 : 0);
            preview.TargetFacesAttacker = FacingDirections.IsFrontal(target.Facing, target.Position, actor.Position);
            int evasion = preview.TargetFacesAttacker ? target.Profile.FrontalEvasion : 0;
            int penalty = actor.Profile.IsArcher ? 5 * (int)Math.Max(0, preview.Distance - 4) : 0;
            preview.ContactChance = Math.Max(5, Math.Min(95,
                actor.Profile.Accuracy + (preview.SteadyAim ? 15 : 0) - target.Profile.Dodge - evasion - penalty));
            preview.GuardChance = preview.TargetFacesAttacker ? target.Profile.Guard : 0;
            preview.PhysicalDamage = actor.Profile.BasicDamage * (100 - target.PhysicalResistance) / 100;
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
            if (command is BasicAttackCommand attack)
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
            return new BattleResult(next, CommandError.None, events);
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
                target.MovementRemaining = 0;
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
            // Every valid End has a living actor, so at least one active unit exists.
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
                unit.MovementRemaining = unit.Profile.Movement;
                unit.MovementSpentThisActivation = 0;
                events.Add(new BattleEvent(BattleEventKind.ActivationStarted, state.Round, unit.Id,
                    amount: 1, after: unit.MovementRemaining));
                return;
            }
        }
    }
}

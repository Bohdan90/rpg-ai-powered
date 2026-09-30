using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RPG.Core
{
    public static class ZoneOfControl
    {
        public static bool Exerts(BattleState state, UnitState source, GridPosition cell) =>
            source != null && source.IsActive && source.Profile.HasMeleeBasic
            && state.Battlefield.IsWalkable(cell)
            && LineOfSight.IsMeleeCornerClear(state, source.Position, cell);

        // Uses the existing initiative priority (including persistent seeded ties and final ID).
        // Availability does not remove a unit's ZoC; it only controls whether it can attack.
        public static ReadOnlyCollection<UnitId> Sources(BattleState state, Side threatenedSide, GridPosition cell)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var result = new List<UnitId>();
            foreach (var id in state.PriorityOrder)
            {
                var unit = state.FindUnit(id);
                if (unit.Side != threatenedSide && Exerts(state, unit, cell)) result.Add(id);
            }
            return result.AsReadOnly();
        }

        public static ReadOnlyCollection<UnitId> Reactors(BattleState state, UnitId moverId, GridPosition from, GridPosition to)
        {
            var result = new List<UnitId>();
            var mover = state.FindUnit(moverId);
            if (state.Outcome.IsEnded || mover == null || !mover.IsActive
                || MovementRules.ValidateStep(state, moverId, from, to) != CommandError.None) return result.AsReadOnly();
            foreach (var id in Sources(state, mover.Side, from))
            {
                var source = state.FindUnit(id);
                if (!source.IsFrozen && source.OpportunityAttackAvailable && mover.GracefulExitTarget!=id && !Exerts(state, source, to)) result.Add(id);
            }
            return result.AsReadOnly();
        }
    }
}

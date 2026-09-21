namespace RPG.Core
{
    public static class MovementRules
    {
        public static CommandError ValidateStep(BattleState state, UnitId mover, GridPosition from, GridPosition to)
        {
            if (!state.Battlefield.Contains(from) || !state.Battlefield.Contains(to)) return CommandError.OutOfBounds;
            if (from.DistanceTo(to) != 1) return CommandError.InvalidStep;
            if (state.Battlefield.IsSolid(to)) return CommandError.SolidCell;
            if (OccupiedByOther(state, to, mover)) return CommandError.OccupiedCell;
            if (from.X != to.X && from.Y != to.Y)
            {
                var horizontal = new GridPosition(to.X, from.Y);
                var vertical = new GridPosition(from.X, to.Y);
                if (!state.Battlefield.IsWalkable(horizontal) || !state.Battlefield.IsWalkable(vertical)
                    || OccupiedByOther(state, horizontal, mover) || OccupiedByOther(state, vertical, mover))
                    return CommandError.BlockedCorner;
            }
            return CommandError.None;
        }

        internal static CommandError ValidatePath(BattleState state, UnitState actor, MoveCommand command)
        {
            if (command.Path == null || command.Path.Count == 0) return CommandError.InvalidPath;
            if (command.Path.Count > actor.MovementRemaining) return CommandError.InsufficientMovement;
            var previous = actor.Position;
            foreach (var step in command.Path)
            {
                var error = ValidateStep(state, actor.Id, previous, step);
                if (error != CommandError.None) return error;
                previous = step;
            }
            return CommandError.None;
        }

        private static bool OccupiedByOther(BattleState state, GridPosition cell, UnitId mover)
        {
            var occupant = state.OccupantAt(cell);
            // The actor's original cell becomes empty along a submitted path, including loops.
            return occupant != null && occupant.Id != mover;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RPG.Core
{
    public sealed class PathResult
    {
        public bool Found { get; }
        public ReadOnlyCollection<GridPosition> Steps { get; }
        public int Cost => Steps.Count;
        internal PathResult(bool found, IEnumerable<GridPosition> steps)
        { Found = found; Steps = new List<GridPosition>(steps).AsReadOnly(); }
    }

    public static class Pathfinder
    {
        // FIFO breadth-first search; fixed clockwise neighbour order is the equal-cost tie-break.
        private static readonly GridPosition[] Directions = {
            new GridPosition(0, 1), new GridPosition(1, 1), new GridPosition(1, 0), new GridPosition(1, -1),
            new GridPosition(0, -1), new GridPosition(-1, -1), new GridPosition(-1, 0), new GridPosition(-1, 1)
        };

        // Query executable movement for the current actor, within its remaining budget.
        // A zero-cost path to the current cell is found, but is not a Move command.
        public static PathResult FindPath(BattleState state, UnitId actorId, GridPosition destination)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var actor = state.FindUnit(actorId);
            if (actor == null || !actor.IsActive || state.CurrentUnitId != actorId || !actor.ActionAvailable
                || !state.Battlefield.IsWalkable(destination)) return NotFound();
            if (destination == actor.Position) return new PathResult(true, Array.Empty<GridPosition>());
            if (state.OccupantAt(destination) != null) return NotFound();
            var visited = new bool[Battlefield.Width, Battlefield.Height];
            var distance = new int[Battlefield.Width, Battlefield.Height];
            var previous = new GridPosition[Battlefield.Width, Battlefield.Height];
            var queue = new Queue<GridPosition>();
            queue.Enqueue(actor.Position);
            visited[actor.Position.X, actor.Position.Y] = true;
            while (queue.Count > 0)
            {
                var from = queue.Dequeue();
                int cost = distance[from.X, from.Y];
                if (cost >= actor.MovementRemaining) continue;
                foreach (var direction in Directions)
                {
                    var next = new GridPosition(from.X + direction.X, from.Y + direction.Y);
                    if (MovementRules.ValidateStep(state, actorId, from, next) != CommandError.None
                        || visited[next.X, next.Y]) continue;
                    visited[next.X, next.Y] = true;
                    previous[next.X, next.Y] = from;
                    distance[next.X, next.Y] = cost + 1;
                    if (next == destination)
                    {
                        var path = new List<GridPosition>();
                        for (var at = next; at != actor.Position; at = previous[at.X, at.Y]) path.Add(at);
                        path.Reverse();
                        return new PathResult(true, path);
                    }
                    queue.Enqueue(next);
                }
            }
            return NotFound();
        }
        private static PathResult NotFound() => new PathResult(false, Array.Empty<GridPosition>());
    }
}

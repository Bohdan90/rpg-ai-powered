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
        // Clockwise order is the final tie-break after cost, line deviation and turns.
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
            if (state.Outcome.IsEnded || actor == null || !actor.IsActive || state.CurrentUnitId != actorId || !actor.CanMove
                || !state.Battlefield.IsWalkable(destination)) return NotFound();
            if (destination == actor.Position) return new PathResult(true, Array.Empty<GridPosition>());
            if (state.OccupantAt(destination) != null) return NotFound();
            // First find minimum step counts. Secondary preferences must never add a step.
            var distance = new int[state.Battlefield.Columns, state.Battlefield.Rows];
            for (int x = 0; x < state.Battlefield.Columns; x++)
            for (int y = 0; y < state.Battlefield.Rows; y++) distance[x, y] = -1;
            var order = new List<GridPosition> { actor.Position };
            distance[actor.Position.X, actor.Position.Y] = 0;
            for (int i = 0; i < order.Count; i++)
            {
                var from = order[i];
                int cost = distance[from.X, from.Y];
                if (cost >= actor.MovementRemaining || (from != actor.Position && state.Battlefield.IsRetreatZone(actor, from))) continue;
                foreach (var direction in Directions)
                {
                    var next = new GridPosition(from.X + direction.X, from.Y + direction.Y);
                    if (MovementRules.ValidateStep(state, actorId, from, next) != CommandError.None
                        || distance[next.X, next.Y] >= 0) continue;
                    distance[next.X, next.Y] = cost + 1;
                    order.Add(next);
                }
            }
            int length = distance[destination.X, destination.Y];
            if (length < 0) return NotFound();

            // Dynamic programming over shortest-path edges. Keep each incoming direction:
            // equal prefixes can incur different turn counts on their next step.
            const int noDirection = 8;
            var routes = new Route[state.Battlefield.Columns, state.Battlefield.Rows, 9];
            routes[actor.Position.X, actor.Position.Y, noDirection] = new Route(0, 0, "");
            long dx = destination.X - actor.Position.X, dy = destination.Y - actor.Position.Y;
            foreach (var from in order)
            {
                int cost = distance[from.X, from.Y];
                if (cost >= length || (from != actor.Position && state.Battlefield.IsRetreatZone(actor, from))) continue;
                for (int direction = 0; direction < Directions.Length; direction++)
                {
                    var next = new GridPosition(from.X + Directions[direction].X, from.Y + Directions[direction].Y);
                    if (MovementRules.ValidateStep(state, actorId, from, next) != CommandError.None
                        || distance[next.X, next.Y] != cost + 1) continue;
                    // Absolute cross product = perpendicular distance times the fixed line length.
                    // Summing it ranks closeness to this query's straight line without floating point.
                    long deviation = Math.Abs(dx * (next.Y - actor.Position.Y) - dy * (next.X - actor.Position.X));
                    for (int incoming = 0; incoming <= noDirection; incoming++)
                    {
                        var prefix = routes[from.X, from.Y, incoming];
                        if (prefix == null) continue;
                        var candidate = new Route(prefix.Deviation + deviation,
                            prefix.Turns + (incoming == noDirection || incoming == direction ? 0 : 1),
                            prefix.DirectionOrder + (char)('0' + direction));
                        if (candidate.IsBetterThan(routes[next.X, next.Y, direction]))
                            routes[next.X, next.Y, direction] = candidate;
                    }
                }
            }
            Route best = null;
            for (int direction = 0; direction < Directions.Length; direction++)
            {
                var candidate = routes[destination.X, destination.Y, direction];
                if (candidate != null && candidate.IsBetterThan(best)) best = candidate;
            }
            var path = new List<GridPosition>();
            var at = actor.Position;
            foreach (char step in best.DirectionOrder)
            {
                var direction = Directions[step - '0'];
                at = new GridPosition(at.X + direction.X, at.Y + direction.Y);
                path.Add(at);
            }
            return new PathResult(true, path);
        }

        private sealed class Route
        {
            public readonly long Deviation;
            public readonly int Turns;
            // Digits 0..7 encode the fixed direction order, for an ordinal whole-path tie-break.
            public readonly string DirectionOrder;
            public Route(long deviation, int turns, string directionOrder)
            { Deviation = deviation; Turns = turns; DirectionOrder = directionOrder; }
            public bool IsBetterThan(Route other) => other == null || Deviation < other.Deviation
                || (Deviation == other.Deviation && (Turns < other.Turns
                    || (Turns == other.Turns && string.CompareOrdinal(DirectionOrder, other.DirectionOrder) < 0)));
        }
        private static PathResult NotFound() => new PathResult(false, Array.Empty<GridPosition>());
    }
}

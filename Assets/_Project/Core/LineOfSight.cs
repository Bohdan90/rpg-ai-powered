using System;
using System.Collections.Generic;

namespace RPG.Core
{
    public static class LineOfSight
    {
        // Center-to-center supercover. Integer cross-products order cell-boundary crossings.
        // On an exact corner crossing both side cells AND the diagonal cell are visited.
        public static bool IsClear(BattleState state, GridPosition source, GridPosition target)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (!state.Battlefield.Contains(source) || !state.Battlefield.Contains(target)) return false;
            foreach (var cell in Supercover(source, target))
                if (Blocked(state, cell, source, target)) return false;
            return true;
        }

        internal static IEnumerable<GridPosition> Supercover(GridPosition source, GridPosition target)
        {
            int nx = Math.Abs(target.X - source.X), ny = Math.Abs(target.Y - source.Y);
            int sx = Math.Sign(target.X - source.X), sy = Math.Sign(target.Y - source.Y);
            int x = source.X, y = source.Y, ix = 0, iy = 0;
            while (ix < nx || iy < ny)
            {
                int horizontal = (1 + 2 * ix) * ny;
                int vertical = (1 + 2 * iy) * nx;
                if (horizontal == vertical)
                {
                    yield return new GridPosition(x + sx, y);
                    yield return new GridPosition(x, y + sy);
                    x += sx; y += sy; ix++; iy++;
                }
                else if (horizontal < vertical) { x += sx; ix++; }
                else { y += sy; iy++; }
                yield return new GridPosition(x, y);
            }
        }

        public static bool IsMeleeCornerClear(BattleState state, GridPosition source, GridPosition target)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (!state.Battlefield.Contains(source) || !state.Battlefield.Contains(target)
                || source.DistanceTo(target) != 1) return false;
            if (source.X == target.X || source.Y == target.Y) return true;
            // Melee can reach around one exposed corner; only two solid side cells seal it.
            // Shared by Basic/preview and ZoC/OA. Movement and ranged supercover stay stricter.
            return !state.Battlefield.IsSolid(new GridPosition(target.X, source.Y))
                || !state.Battlefield.IsSolid(new GridPosition(source.X, target.Y));
        }

        private static bool Blocked(BattleState state, GridPosition cell, GridPosition source, GridPosition target)
        {
            if (cell == source || cell == target) return false;
            return state.Battlefield.IsSolid(cell);
        }
    }
}

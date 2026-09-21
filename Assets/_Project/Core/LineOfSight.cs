using System;
using System.Collections.Generic;

namespace RPG.Core
{
    public static class LineOfSight
    {
        // Solid LoS blocks open-cell interior intersections and sealed diagonal vertices.
        // Single-wall grazing is legal; a shared vertex of diagonal solids seals the shot.
        // Keep inclusive supercover for unit Cover.
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
            // Shared by Basic/preview and ZoC/OA. Movement and ranged LoS have separate contracts.
            return !state.Battlefield.IsSolid(new GridPosition(target.X, source.Y))
                || !state.Battlefield.IsSolid(new GridPosition(source.X, target.Y));
        }

        // Doubled coordinates represent cell centers (even) and boundaries (odd) exactly.
        // Clip a finite segment against the OPEN square using rational t intervals, no epsilon.
        internal static bool CrossesCellInterior(long sx2, long sy2, long tx2, long ty2, GridPosition cell)
        {
            long enterN = 0, enterD = 1, exitN = 1, exitD = 1;
            return ClipOpenAxis(sx2, tx2, 2L * cell.X - 1, 2L * cell.X + 1, ref enterN, ref enterD, ref exitN, ref exitD)
                && ClipOpenAxis(sy2, ty2, 2L * cell.Y - 1, 2L * cell.Y + 1, ref enterN, ref enterD, ref exitN, ref exitD);
        }

        private static bool ClipOpenAxis(long start, long end, long min, long max,
            ref long enterN, ref long enterD, ref long exitN, ref long exitD)
        {
            long delta = end - start;
            if (delta == 0) return start > min && start < max;
            long denominator = Math.Abs(delta);
            long low = delta > 0 ? min - start : start - max;
            long high = delta > 0 ? max - start : start - min;
            if (low * enterD > enterN * denominator) { enterN = low; enterD = denominator; }
            if (high * exitD < exitN * denominator) { exitN = high; exitD = denominator; }
            return enterN * exitD < exitN * enterD;
        }

        private static bool SealsTouchedVertex(BattleState state, GridPosition source, GridPosition target, GridPosition solid)
        {
            long dx = 2L * (target.X - source.X), dy = 2L * (target.Y - source.Y);
            long lengthSquared = dx * dx + dy * dy;
            for (int sx = -1; sx <= 1; sx += 2)
            for (int sy = -1; sy <= 1; sy += 2)
            {
                var opposite = new GridPosition(solid.X + sx, solid.Y + sy);
                if (opposite == source || opposite == target || !state.Battlefield.IsSolid(opposite)) continue;
                long vx = 2L * (solid.X - source.X) + sx, vy = 2L * (solid.Y - source.Y) + sy;
                long projection = vx * dx + vy * dy;
                if (vx * dy == vy * dx && projection > 0 && projection < lengthSquared) return true;
            }
            return false;
        }

        private static bool Blocked(BattleState state, GridPosition cell, GridPosition source, GridPosition target)
        {
            if (cell == source || cell == target) return false;
            return state.Battlefield.IsSolid(cell) && (CrossesCellInterior(
                2L * source.X, 2L * source.Y, 2L * target.X, 2L * target.Y, cell)
                || SealsTouchedVertex(state, source, target, cell));
        }
    }
}

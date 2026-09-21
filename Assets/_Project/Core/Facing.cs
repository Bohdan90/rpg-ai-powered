using System;

namespace RPG.Core
{
    // Clockwise; positive Y is north. No Unity coordinates or floating-point angles.
    public enum Facing { North, NorthEast, East, SouthEast, South, SouthWest, West, NorthWest }

    public static class FacingDirections
    {
        public static Facing Toward(GridPosition from, GridPosition to)
        {
            long dx = (long)to.X - from.X, dy = (long)to.Y - from.Y;
            if (dx == 0 && dy == 0) throw new ArgumentException("A direction needs distinct positions.");
            decimal x = Math.Abs(dx), y = Math.Abs(dy);
            // Nearest octant: minor/major < tan(22.5 degrees).
            // Squaring (major + minor) vs sqrt(2)*major avoids floating-point boundaries.
            // Decimal holds these squared integer coordinates exactly, even at int limits.
            decimal square = (x + y) * (x + y);
            if (x > y && square < 2 * x * x) return dx > 0 ? Facing.East : Facing.West;
            if (y > x && square < 2 * y * y) return dy > 0 ? Facing.North : Facing.South;
            // A nonzero integer vector cannot lie exactly on an irrational octant boundary.
            if (dx > 0) return dy > 0 ? Facing.NorthEast : Facing.SouthEast;
            return dy > 0 ? Facing.NorthWest : Facing.SouthWest;
        }

        public static bool IsFrontal(Facing facing, GridPosition target, GridPosition source)
        {
            int difference = Math.Abs((int)facing - (int)Toward(target, source));
            return Math.Min(difference, 8 - difference) <= 1;
        }

        internal static bool IsValid(Facing facing) => facing >= Facing.North && facing <= Facing.NorthWest;
    }
}

using System;

namespace RPG.Core
{
    public enum CoverLevel { None, Light, Strong }

    public static class Cover
    {
        // T — PROTOTYPE TUNING. Strong Cover has no numerical behavior yet.
        public const int LightAccuracyModifier = -15;
        public static int? AccuracyModifier(CoverLevel level) =>
            level == CoverLevel.None ? 0 : level == CoverLevel.Light ? LightAccuracyModifier : (int?)null;

        public static CoverLevel Strongest(CoverLevel a, CoverLevel b) => a > b ? a : b;

        // Caller supplies a cell intersected by the supercover. Sizes are ordered body-size ranks,
        // not footprints. All current profiles have rank 1; no larger creature is introduced.
        public static CoverLevel Classify(GridPosition source, GridPosition target, GridPosition screen,
            int screeningSize, int targetSize)
        {
            if (screeningSize <= 0 || targetSize <= 0) throw new ArgumentOutOfRangeException(nameof(screeningSize));
            if (screen == source || screen == target) return CoverLevel.None;
            long dx = target.X - source.X, dy = target.Y - source.Y;
            long projection = (screen.X - source.X) * dx + (screen.Y - source.Y) * dy;
            // P — compare projection along shot; exact midpoint belongs to target.
            if (2 * projection < dx * dx + dy * dy) return CoverLevel.None;
            return screeningSize <= targetSize ? CoverLevel.Light : CoverLevel.Strong;
        }

        public static CoverLevel Query(BattleState state, UnitState shooter, UnitState target)
        {
            var result = CoverLevel.None;
            foreach (var cell in LineOfSight.Supercover(shooter.Position, target.Position))
            {
                var screen = state.OccupantAt(cell);
                if (screen != null)
                    result = Strongest(result, Classify(shooter.Position, target.Position, cell,
                        screen.Profile.CoverSize, target.Profile.CoverSize));
            }
            return result;
        }
    }
}

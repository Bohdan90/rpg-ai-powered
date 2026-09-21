using System;
using System.Collections.Generic;

namespace RPG.Core
{
    public enum SizeExperimentMap { Field_13x9_Control, Field_17x11_Expanded, Siege_23x17_Tight, Siege_27x21_Roomy }

    // Experimental geometry only: no siege mechanics or new profile tuning.
    public static class SizeExperimentFixture
    {
        public static Battlefield Board(SizeExperimentMap map)
        {
            Dimensions(map, out int width, out int height);
            int cx = width / 2, cy = height / 2;
            var solids = new List<GridPosition>();
            if (map <= SizeExperimentMap.Field_17x11_Expanded)
                for (int y = cy - 1; y <= cy + 1; y++) solids.Add(new GridPosition(cx, y));
            else
                for (int x = -5; x <= 5; x++)
                for (int y = -4; y <= 4; y++)
                    if ((Math.Abs(x) == 5 && Math.Abs(y) > 1) || (Math.Abs(y) == 4 && Math.Abs(x) > 1))
                        solids.Add(new GridPosition(cx + x, cy + y));
            return new Battlefield(width, height, solids, map >= SizeExperimentMap.Siege_23x17_Tight);
        }

        public static UnitState[] Units(SizeExperimentMap map)
        {
            Dimensions(map, out int width, out int height);
            int cy = height / 2;
            var positions = new[] { new GridPosition(2, cy), new GridPosition(2, cy - 1),
                new GridPosition(1, cy - 2), new GridPosition(1, cy + 2), new GridPosition(2, cy + 1) };
            var profiles = new[] { UnitProfile.HumanWarriorTI, UnitProfile.HumanWarriorTI,
                UnitProfile.HumanArcherTI, UnitProfile.HumanArcherTI, UnitProfile.ElfWarriorTI };
            var units = new UnitState[10];
            for (int side = 0; side < 2; side++)
            for (int i = 0; i < 5; i++)
            {
                var p = positions[i];
                if (side == 1) p = new GridPosition(map >= SizeExperimentMap.Siege_23x17_Tight
                    ? width / 2 + 3 - p.X : width - 1 - p.X, p.Y);
                units[side * 5 + i] = new UnitState(new UnitId(side * 5 + i + 1), side == 0 ? Side.West : Side.East,
                    profiles[i], p, side == 0 ? Facing.East : Facing.West);
            }
            return units;
        }
        private static void Dimensions(SizeExperimentMap map, out int width, out int height)
        {
            switch (map)
            {
                case SizeExperimentMap.Field_13x9_Control: width = 13; height = 9; break;
                case SizeExperimentMap.Field_17x11_Expanded: width = 17; height = 11; break;
                case SizeExperimentMap.Siege_23x17_Tight: width = 23; height = 17; break;
                case SizeExperimentMap.Siege_27x21_Roomy: width = 27; height = 21; break;
                default: throw new ArgumentOutOfRangeException(nameof(map));
            }
        }
    }
}

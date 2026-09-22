using System;
using System.Collections.Generic;

namespace RPG.Core
{
    public enum SizeExperimentMap { Field_13x9_Control, Field_17x11_Expanded, Siege_23x17_Tight, Siege_27x21_Roomy, Field_19x13_ExpandedV2, Siege_31x25_Medium, Siege_35x27_Large, Field_23x17_Full_9v9, Siege_35x27_Full_9v9 }

    // Experimental geometry only: no siege mechanics or new profile tuning.
    public static class SizeExperimentFixture
    {
        public static bool IsSiege(SizeExperimentMap map) => map == SizeExperimentMap.Siege_23x17_Tight || map == SizeExperimentMap.Siege_27x21_Roomy || HasMoatProxy(map);

        public static bool HasMoatProxy(SizeExperimentMap map) => map == SizeExperimentMap.Siege_31x25_Medium || map == SizeExperimentMap.Siege_35x27_Large || map == SizeExperimentMap.Siege_35x27_Full_9v9;

        public static bool IsDensity(SizeExperimentMap map) => map == SizeExperimentMap.Field_23x17_Full_9v9 || map == SizeExperimentMap.Siege_35x27_Full_9v9;

        public static Battlefield Board(SizeExperimentMap map)
        {
            Dimensions(map, out int width, out int height);
            int cx = width / 2, cy = height / 2;
            var solids = new List<GridPosition>();
            if (!IsSiege(map))
                for (int y = cy - 1; y <= cy + 1; y++) solids.Add(new GridPosition(cx, y));
            else
                for (int x = -5; x <= 5; x++)
                for (int y = -4; y <= 4; y++)
                    if ((Math.Abs(x) == 5 && Math.Abs(y) > 1) || (Math.Abs(y) == 4 && Math.Abs(x) > 1))
                        solids.Add(new GridPosition(cx + x, cy + y));
            // P: static solid/opaque spatial proxy only, not a real moat mechanic.
            // Identical one-cell outer ring, with four three-cell crossings aligned to openings.
            if (HasMoatProxy(map))
                for (int x = -6; x <= 6; x++)
                for (int y = -5; y <= 5; y++)
                    if ((Math.Abs(x) == 6 && Math.Abs(y) > 1) || (Math.Abs(y) == 5 && Math.Abs(x) > 1))
                        solids.Add(new GridPosition(cx + x, cy + y));
            return new Battlefield(width, height, solids, IsSiege(map));
        }

        public static UnitState[] Units(SizeExperimentMap map)
        {
            Dimensions(map, out int width, out int height);
            if (IsDensity(map)) return DensityUnits(map, width, height);
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
                if (side == 1) p = new GridPosition(IsSiege(map)
                    ? width / 2 + 3 - p.X : width - 1 - p.X, p.Y);
                units[side * 5 + i] = new UnitState(new UnitId(side * 5 + i + 1), side == 0 ? Side.West : Side.East,
                    profiles[i], p, side == 0 ? Facing.East : Facing.West);
            }
            return units;
        }
        // Synthetic tactical density roster, not a strategic Capacity legality claim.
        private static UnitState[] DensityUnits(SizeExperimentMap map, int width, int height)
        {
            int cx = width / 2, cy = height / 2;
            var west = new[] { new GridPosition(2,cy), new GridPosition(3,cy-2),
                new GridPosition(1,cy-3), new GridPosition(1,cy+3), new GridPosition(3,cy+4),
                new GridPosition(3,cy), new GridPosition(3,cy+2), new GridPosition(1,cy), new GridPosition(3,cy-4) };
            var defenders = new[] { new GridPosition(cx+1,cy), new GridPosition(cx,cy-2),
                new GridPosition(cx+2,cy-3), new GridPosition(cx+2,cy+3), new GridPosition(cx-2,cy+3),
                new GridPosition(cx,cy), new GridPosition(cx,cy+2), new GridPosition(cx+2,cy), new GridPosition(cx-2,cy-3) };
            var profiles = new[] { UnitProfile.HumanWarriorTI, UnitProfile.HumanWarriorTI,
                UnitProfile.HumanArcherTI, UnitProfile.HumanArcherTI, UnitProfile.ElfWarriorTI,
                UnitProfile.HumanWarriorTI, UnitProfile.HumanWarriorTI, UnitProfile.HumanArcherTI, UnitProfile.ElfWarriorTI };
            var units = new UnitState[18];
            for (int side = 0; side < 2; side++)
            for (int i = 0; i < 9; i++)
            {
                var p = side == 0 ? west[i] : IsSiege(map) ? defenders[i] : new GridPosition(width-1-west[i].X,west[i].Y);
                // Preserve original names/Commander IDs 1 and 6 across comparison fixtures.
                int id = i < 5 ? side*5+i+1 : (side == 0 ? 11 : 15)+i-5;
                units[side*9+i] = new UnitState(new UnitId(id),side == 0 ? Side.West : Side.East,
                    profiles[i],p,side == 0 ? Facing.East : Facing.West);
            }
            Array.Sort(units, (a,b) => a.Id.Value.CompareTo(b.Id.Value));
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
                case SizeExperimentMap.Field_19x13_ExpandedV2: width = 19; height = 13; break;
                case SizeExperimentMap.Siege_31x25_Medium: width = 31; height = 25; break;
                case SizeExperimentMap.Field_23x17_Full_9v9: width = 23; height = 17; break;
                case SizeExperimentMap.Siege_35x27_Full_9v9:
                case SizeExperimentMap.Siege_35x27_Large: width = 35; height = 27; break;
                default: throw new ArgumentOutOfRangeException(nameof(map));
            }
        }
    }
}

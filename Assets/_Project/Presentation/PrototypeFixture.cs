using RPG.Core;

namespace RPG.Presentation
{
    public static class PrototypeFixture
    {
        public const uint Seed = 20260921;
        private static readonly string[] Names = { "HW-Commander *", "HW-Infantry", "HA-Left", "HA-Right", "EW-Flanker" };
        public static string Name(UnitId id) => id.Value >= 1 && id.Value <= 10
            ? (id.Value <= 5 ? "W " : "E ") + Names[(id.Value - 1) % 5] : "Unit " + id;

        public static UnitState[] Units()
        {
            var units = new UnitState[10];
            var positions = new[] { new GridPosition(2, 4), new GridPosition(2, 3), new GridPosition(1, 2), new GridPosition(1, 6), new GridPosition(2, 5) };
            var profiles = new[] { UnitProfile.HumanWarriorTI, UnitProfile.HumanWarriorTI, UnitProfile.HumanArcherTI, UnitProfile.HumanArcherTI, UnitProfile.ElfWarriorTI };
            for (int side = 0; side < 2; side++)
            for (int i = 0; i < 5; i++)
            {
                var position = positions[i];
                if (side == 1) position = new GridPosition(12 - position.X, position.Y);
                units[side * 5 + i] = new UnitState(new UnitId(side * 5 + i + 1), side == 0 ? Side.West : Side.East,
                    profiles[i], position, side == 0 ? Facing.East : Facing.West);
            }
            return units;
        }
    }
}

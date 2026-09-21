using RPG.Core;

namespace RPG.Presentation
{
    public static class PrototypeFixture
    {
        public const uint Seed = 20260921;
        private static readonly string[] Names = { "HW-Commander *", "HW-Infantry", "HA-Left", "HA-Right", "EW-Flanker" };
        public static string Name(UnitId id) => id.Value >= 1 && id.Value <= 10
            ? (id.Value <= 5 ? "W " : "E ") + Names[(id.Value - 1) % 5] : "Unit " + id;

        public static UnitState[] Units() => SizeExperimentFixture.Units(SizeExperimentMap.Field_13x9_Control);
    }
}

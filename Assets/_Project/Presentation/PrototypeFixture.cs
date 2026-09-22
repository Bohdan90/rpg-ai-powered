using RPG.Core;

namespace RPG.Presentation
{
    public static class PrototypeFixture
    {
        public const uint Seed = 20260921;
        private static readonly string[] Names = { "HW-Commander *", "HW-Infantry", "HA-Left", "HA-Right", "EW-Flanker" };
        private static readonly string[] ExtraNames = { "HW-Infantry 2", "HW-Infantry 3", "HA-Center", "EW-Flanker 2" };
        public static string Name(UnitId id) => id.Value >= 1 && id.Value <= 10
            ? (id.Value <= 5 ? "W " : "E ") + Names[(id.Value - 1) % 5] : id.Value >= 11 && id.Value <= 18 ? (id.Value <= 14 ? "W " : "E ") + ExtraNames[(id.Value-11)%4] : "Unit " + id;

        public static UnitState[] Units() => SizeExperimentFixture.Units(SizeExperimentMap.Field_13x9_Control);
    }
}

namespace RPG.Core
{
    public enum UnitProfileId { HumanWarriorTI, HumanArcherTI, ElfWarriorTI }

    // Contract v0.1, section 4. T — prototype tuning, not final class balance.
    public sealed class UnitProfile
    {
        public static readonly UnitProfile HumanWarriorTI = new UnitProfile(UnitProfileId.HumanWarriorTI, 40, 16, 4, 10, 85, 5, 20, 12, 1);
        public static readonly UnitProfile HumanArcherTI = new UnitProfile(UnitProfileId.HumanArcherTI, 28, 4, 4, 12, 80, 5, 0, 10, 6);
        public static readonly UnitProfile ElfWarriorTI = new UnitProfile(UnitProfileId.ElfWarriorTI, 32, 6, 6, 14, 85, 10, 0, 11, 1);

        public UnitProfileId Id { get; }
        public int MaxHp { get; }
        public int MaxArmor { get; }
        public int Movement { get; }
        public int Initiative { get; }
        public int Accuracy { get; }
        public int Dodge { get; }
        public int Guard { get; }
        public int BasicDamage { get; }
        public int Range { get; }
        public int CoverSize => 1; // All current profiles are ordinary, same-size bodies.
        public bool HasMeleeBasic => Id == UnitProfileId.HumanWarriorTI || Id == UnitProfileId.ElfWarriorTI;
        public bool IsArcher => Id == UnitProfileId.HumanArcherTI;
        public int FrontalEvasion => Id == UnitProfileId.ElfWarriorTI ? 15 : 0;

        private UnitProfile(UnitProfileId id, int hp, int armor, int movement, int initiative,
            int accuracy, int dodge, int guard, int damage, int range)
        {
            Id = id; MaxHp = hp; MaxArmor = armor; Movement = movement; Initiative = initiative;
            Accuracy = accuracy; Dodge = dodge; Guard = guard; BasicDamage = damage; Range = range;
        }
    }
}

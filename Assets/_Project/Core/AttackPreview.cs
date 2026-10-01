namespace RPG.Core
{
    // Conditional damage assumes contact succeeds and Guard does not; no RNG draws.
    public sealed class AttackPreview
    {
        public CommandError Error { get; internal set; }
        public BasicAttackKind Kind { get; internal set; }
        public bool IsLegal => Error == CommandError.None;
        public long Distance { get; internal set; }
        public int MaximumRange { get; internal set; }
        public bool SteadyAim { get; internal set; }
        public bool TargetFacesAttacker { get; internal set; }
        public int BaseAccuracy { get; internal set; }
        public int AimModifier { get; internal set; }
        public int DistanceModifier { get; internal set; }
        public int TargetDodge { get; internal set; }
        public int FrontalEvasion { get; internal set; }
        public CoverLevel Cover { get; internal set; }
        public int CoverAccuracyModifier { get; internal set; }
        public int ContactChance { get; internal set; }
        public int GuardChance { get; internal set; }
        public int PhysicalDamage { get; internal set; }
        public int ArmorLossOnUnguardedHit { get; internal set; }
        public int HpLossOnUnguardedHit { get; internal set; }
    }
}

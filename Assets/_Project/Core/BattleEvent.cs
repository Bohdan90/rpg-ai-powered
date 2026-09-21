namespace RPG.Core
{
    public enum BattleEventKind
    {
        BattleStarted, RoundStarted, ActivationStarted, ActivationEnded,
        ActionConsumed, MovementConsumed, FacingChanged, SteadyAimApplied,
        ContactRolled, AttackMissed, GuardRolled, GuardSucceeded,
        DamageApplied, ArmorLost, HpLost, DefendApplied, DefendExpired, UnitDied
    }

    // A small value record for this resolver, not an event bus or persistence format.
    // Roll/chance use [0,99]/[0,100]; Before/After identify changed pools/resources/facing.
    public readonly struct BattleEvent
    {
        public BattleEventKind Kind { get; }
        public int Round { get; }
        public UnitId? Actor { get; }
        public UnitId? Target { get; }
        public int Amount { get; }
        public int Before { get; }
        public int After { get; }
        public int ChancePercent { get; }
        public int Roll { get; }
        public BattleEvent(BattleEventKind kind, int round, UnitId? actor = null, UnitId? target = null,
            int amount = 0, int before = 0, int after = 0, int chancePercent = 0, int roll = -1)
        {
            Kind = kind; Round = round; Actor = actor; Target = target; Amount = amount;
            Before = before; After = after; ChancePercent = chancePercent; Roll = roll;
        }
    }
}

namespace RPG.Core
{
    public enum BattleEndReason { None, Withdrawal, Eliminated }

    // Tactical result only. BattleResult remains the result of applying a command.
    public readonly struct BattleOutcome
    {
        public static readonly BattleOutcome Ongoing = new BattleOutcome();
        public bool IsEnded => Reason != BattleEndReason.None;
        public Side? VictorySide { get; }
        public Side? DefeatedSide { get; }
        public BattleEndReason Reason { get; }
        internal BattleOutcome(Side victorySide, Side defeatedSide, BattleEndReason reason)
        { VictorySide = victorySide; DefeatedSide = defeatedSide; Reason = reason; }
    }
}

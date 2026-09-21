namespace RPG.Core
{
    public abstract class BattleCommand
    {
        public UnitId Actor { get; }
        protected BattleCommand(UnitId actor) { Actor = actor; }
    }
    public sealed class BasicAttackCommand : BattleCommand
    {
        public UnitId Target { get; }
        public bool FriendlyFireConfirmed { get; }
        public BasicAttackCommand(UnitId actor, UnitId target, bool friendlyFireConfirmed = false) : base(actor)
        { Target = target; FriendlyFireConfirmed = friendlyFireConfirmed; }
    }
    public sealed class DefendCommand : BattleCommand
    {
        public DefendCommand(UnitId actor) : base(actor) { }
    }
    public sealed class EndActivationCommand : BattleCommand
    {
        public Facing? FinalFacing { get; }
        public EndActivationCommand(UnitId actor, Facing? finalFacing = null) : base(actor) { FinalFacing = finalFacing; }
    }
}

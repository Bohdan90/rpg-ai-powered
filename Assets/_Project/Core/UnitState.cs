using System;

namespace RPG.Core
{
    public enum RetreatEdge { West, East, North, South, Unavailable }
    public enum UnitStatus { Active, Dead, Escaped }

    public sealed class UnitState
    {
        public RetreatEdge? OwnRetreatEdge { get; }
        public UnitId Id { get; }
        public Side Side { get; }
        public UnitProfile Profile { get; }
        public GridPosition Position { get; internal set; }
        public Facing Facing { get; internal set; }
        public int Hp { get; internal set; }
        public int Armor { get; internal set; }
        public UnitStatus Status { get; internal set; }
        public bool IsActive => Status == UnitStatus.Active;
        public bool ActionAvailable { get; internal set; }
        public bool OpportunityAttackAvailable { get; internal set; }
        public int MovementRemaining { get; internal set; }
        // Historical spend, distinct from MovementRemaining (Defend/End also clear remaining).
        public int MovementSpentThisActivation { get; internal set; }
        public bool IsDefending { get; internal set; }
        public int PhysicalResistance => IsDefending ? 25 : 0;
        public uint TieKey { get; internal set; }

        public UnitState(UnitId id, Side side, UnitProfile profile, GridPosition position, Facing facing,
            int? hp = null, int? armor = null, UnitStatus status = UnitStatus.Active, RetreatEdge? ownRetreatEdge = null)
        {
            if (id.Value <= 0) throw new ArgumentOutOfRangeException(nameof(id));
            if (side != Side.West && side != Side.East) throw new ArgumentOutOfRangeException(nameof(side));
            if (!FacingDirections.IsValid(facing)) throw new ArgumentOutOfRangeException(nameof(facing));
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            int initialHp = hp ?? profile.MaxHp, initialArmor = armor ?? profile.MaxArmor;
            if (initialHp < 0 || initialHp > profile.MaxHp) throw new ArgumentOutOfRangeException(nameof(hp));
            if (initialArmor < 0 || initialArmor > profile.MaxArmor) throw new ArgumentOutOfRangeException(nameof(armor));
            if (status < UnitStatus.Active || status > UnitStatus.Escaped) throw new ArgumentOutOfRangeException(nameof(status));
            if (status == UnitStatus.Dead && initialHp != 0) throw new ArgumentException("Dead units must have zero HP.");
            if (status == UnitStatus.Escaped && initialHp == 0) throw new ArgumentException("Escaped units must be alive.");
            if (ownRetreatEdge.HasValue && (ownRetreatEdge < RetreatEdge.West || ownRetreatEdge > RetreatEdge.Unavailable)) throw new ArgumentOutOfRangeException(nameof(ownRetreatEdge));
            OwnRetreatEdge=ownRetreatEdge;
            Id = id; Side = side; Position = position; Facing = facing; Hp = initialHp; Armor = initialArmor;
            Status = initialHp == 0 ? UnitStatus.Dead : status;
        }

        internal UnitState Copy() => (UnitState)MemberwiseClone();
    }
}

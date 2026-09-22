using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RPG.Core
{
    public enum CommandError
    {
        None, InvalidCommand, ActorNotFound, ActorInactive, NotCurrentActor, NoAction,
        TargetNotFound, TargetInactive, SelfTarget, FriendlyFireNotConfirmed, OutOfRange,
        MovementAlreadySpent, InvalidFacing, InvalidPath, InvalidStep, OutOfBounds, SolidCell,
        OccupiedCell, BlockedCorner, InsufficientMovement, BlockedLineOfSight, BattleAlreadyEnded, Engaged, MeleeStrikeUnavailable
    }

    public sealed class BattleResult
    {
        public bool IsApplied => Error == CommandError.None;
        public CommandError Error { get; }
        public BattleState State { get; }
        public ReadOnlyCollection<BattleEvent> Events { get; }
        internal BattleResult(BattleState state, CommandError error, IEnumerable<BattleEvent> events)
        { State = state; Error = error; Events = new List<BattleEvent>(events).AsReadOnly(); }
    }
}

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RPG.Core
{
    public sealed class MoveCommand : BattleCommand
    {
        // Excludes the starting cell. Each entry is one destination step, not a waypoint.
        // Copy at construction so later caller edits cannot change command meaning.
        public ReadOnlyCollection<GridPosition> Path { get; }
        public MoveCommand(UnitId actor, IEnumerable<GridPosition> explicitPath) : base(actor)
        {
            Path = explicitPath == null ? null : new List<GridPosition>(explicitPath).AsReadOnly();
        }
    }
}

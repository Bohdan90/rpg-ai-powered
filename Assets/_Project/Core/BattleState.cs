using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace RPG.Core
{
    public sealed class BattleState
    {
        private readonly List<UnitState> units;
        internal CombatRandom Random;
        internal int PriorityIndex = -1;
        internal readonly List<UnitId> PriorityOrder;
        public ReadOnlyCollection<UnitState> Units { get; }
        public uint InitialSeed { get; }
        public int FireRulesVersion { get; internal set; } = 2;
        public Battlefield Battlefield { get; }
        public uint RngState => Random.State;
        public int Round { get; internal set; }
        public BattleOutcome Outcome { get; internal set; } = BattleOutcome.Ongoing;
        public UnitId? CurrentUnitId { get; internal set; }
        // Full live priority for a round; already-activated units are not reinserted mid-round.
        public IReadOnlyList<UnitId> ActivationOrder => PriorityOrder.Where(id => FindUnit(id).IsActive).ToList().AsReadOnly();

        internal BattleState(IEnumerable<UnitState> initialUnits, uint seed, Battlefield battlefield,bool completedMutualElimination=false)
        {
            Battlefield = battlefield ?? Battlefield.ControlMap;
            if (initialUnits == null) throw new ArgumentNullException(nameof(initialUnits));
            var input = initialUnits.ToList();
            if (input.Count == 0 || input.Any(u => u == null)) throw new ArgumentException("Provide non-null units.");
            if (input.Select(u => u.Id).Distinct().Count() != input.Count) throw new ArgumentException("Unit IDs must be unique.");
            var active = input.Where(u => u.IsActive).ToList();
            if (active.Select(u => u.Position).Distinct().Count() != active.Count) throw new ArgumentException("Active units cannot share a cell.");
            if (active.Any(u => !Battlefield.IsWalkable(u.Position))) throw new ArgumentException("Active units must be on walkable battlefield cells.");
            if (active.Count == 0 && !completedMutualElimination) throw new ArgumentException("At least one active unit is required.");
            units = input.OrderBy(u => u.Id).Select(u => u.Copy()).ToList();
            Units = units.AsReadOnly(); InitialSeed = seed; Random = new CombatRandom(seed);
            // P: assign keys in ascending numeric ID order, independent of collection enumeration.
            foreach (var unit in units)
            {
                unit.TieKey = Random.NextUInt();
                unit.OpportunityAttackAvailable = unit.IsActive && unit.Profile.HasMeleeBasic;
            }
            PriorityOrder = units.OrderByDescending(u => u.Profile.Initiative)
                .ThenBy(u => u.TieKey).ThenBy(u => u.Id).Select(u => u.Id).ToList();
        }

        private BattleState(BattleState source)
        {
            units = source.units.Select(u => u.Copy()).ToList(); Units = units.AsReadOnly();
            PriorityOrder = new List<UnitId>(source.PriorityOrder);
            FireRulesVersion=source.FireRulesVersion; Battlefield = source.Battlefield; InitialSeed = source.InitialSeed; Random = source.Random; Round = source.Round;
            PriorityIndex = source.PriorityIndex; CurrentUnitId = source.CurrentUnitId; Outcome = source.Outcome;
        }
        public UnitState FindUnit(UnitId id) => units.Find(u => u.Id == id);
        public UnitState OccupantAt(GridPosition cell) => units.Find(u => u.IsActive && u.Position == cell);
        internal BattleState Copy() => new BattleState(this);
    }
}

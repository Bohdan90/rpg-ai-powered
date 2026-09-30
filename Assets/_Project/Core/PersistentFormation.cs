using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace RPG.Core
{
    public enum PersistentCharacterStatus { Alive, EscapedSafe, Dead }
    public enum CommandRank { I, II, III, IV }

    // Minimal strategic identity for the persistence slice. It deliberately owns no map, economy,
    // recruitment, artifact, doctrine, grave or resurrection state.
    public sealed class PersistentCharacter
    {
        public string CharacterId { get; }
        public UnitProfile Profile { get; private set; }
        public bool IsCommander { get; }
        public int Hp { get; private set; }
        public int Armor { get; private set; }
        public PersistentCharacterStatus Status { get; private set; }
        public decimal PersonalXp { get; private set; }
        public int PersonalLevel => 1 + (int)(PersonalXp / 10m);
        public decimal CommandXp { get; private set; }
        public int CommandLevel { get; private set; }
        public CommandRank CommandRank => CommandLevel <= 3 ? CommandRank.I : CommandLevel <= 9 ? CommandRank.II : CommandLevel <= 19 ? CommandRank.III : CommandRank.IV;
        // Tactical pools are integers. This keeps the exact fractional part of each canonical
        // percentage recovery for following Refreshes instead of silently losing it.
        public int FieldRecoveryRemainderHundredths { get; private set; }

        public PersistentCharacter(string characterId, UnitProfile profile, bool isCommander = false,
            int? hp = null, int? armor = null, PersistentCharacterStatus status = PersistentCharacterStatus.Alive,
            decimal personalXp = 0m, decimal commandXp = 0m, int commandLevel = 1, int fieldRecoveryRemainderHundredths = 0)
        {
            if (string.IsNullOrWhiteSpace(characterId)) throw new ArgumentException("Persistent ID is required.", nameof(characterId));
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            if (hp.HasValue && (hp < 0 || hp > profile.MaxHp)) throw new ArgumentOutOfRangeException(nameof(hp));
            if (armor.HasValue && (armor < 0 || armor > profile.MaxArmor)) throw new ArgumentOutOfRangeException(nameof(armor));
            if (status == PersistentCharacterStatus.Dead && (hp ?? profile.MaxHp) != 0) throw new ArgumentException("Dead character must have zero HP.");
            if (status != PersistentCharacterStatus.Dead && (hp ?? profile.MaxHp) == 0) throw new ArgumentException("Living character needs HP.");
            if (personalXp < 0m || commandXp < 0m || commandLevel < 1 || fieldRecoveryRemainderHundredths < 0 || fieldRecoveryRemainderHundredths >= 100)
                throw new ArgumentOutOfRangeException(nameof(personalXp));
            CharacterId = characterId; IsCommander = isCommander; Hp = hp ?? profile.MaxHp; Armor = armor ?? profile.MaxArmor;
            Status = status; PersonalXp = personalXp; CommandXp = commandXp; CommandLevel = isCommander ? commandLevel : 1;
            FieldRecoveryRemainderHundredths = fieldRecoveryRemainderHundredths;
        }

        public int FireballUsed { get; internal set; }
        public int FreezeUsed { get; internal set; }
        public int CloseHealUsed { get; internal set; }
        internal void ResetSourceBudgets() { FireballUsed=0;FreezeUsed=0;CloseHealUsed=0; }
        internal void TrainMageTierII() {
            if(Status==PersistentCharacterStatus.Dead || PersonalLevel<3 || Profile.Tier!=1 || (!Profile.IsFireMage&&!Profile.IsIceMage))throw new InvalidOperationException("Ineligible persistent mage.");
            Profile=Profile.IsFireMage?UnitProfile.FireMageTII:UnitProfile.IceMageTII;
            Hp=Math.Min(Hp,Profile.MaxHp);Armor=Math.Min(Armor,Profile.MaxArmor);
        }
        internal void SetBattleResult(UnitState unit)
        {
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            Hp = unit.Hp; Armor = unit.Armor; FireballUsed=unit.FireballUsed;FreezeUsed=unit.FreezeUsed;CloseHealUsed=unit.CloseHealUsed;
            Status = unit.Status == UnitStatus.Dead ? PersistentCharacterStatus.Dead
                : unit.Status == UnitStatus.Escaped ? PersistentCharacterStatus.EscapedSafe : PersistentCharacterStatus.Alive;
        }
        internal void ReturnFromSafety() { if (Status == PersistentCharacterStatus.EscapedSafe) Status = PersistentCharacterStatus.Alive; }
        public const int FieldRecoveryPercent=15;
        public const int HealingBuildingRecoveryPercent=40;
        public int PreviewHpRecovery(int percent)
        {
            if(percent!=FieldRecoveryPercent&&percent!=HealingBuildingRecoveryPercent)throw new ArgumentOutOfRangeException(nameof(percent));
            return Status==PersistentCharacterStatus.Dead?0:Math.Min(Profile.MaxHp-Hp,(FieldRecoveryRemainderHundredths+Profile.MaxHp*percent)/100);
        }
        internal void ApplyHpRefresh(int percent)
        {
            int restored=PreviewHpRecovery(percent);
            if (Status == PersistentCharacterStatus.Dead) return;
            int total = FieldRecoveryRemainderHundredths + Profile.MaxHp * percent;
            FieldRecoveryRemainderHundredths = total % 100;
            Hp = Math.Min(Profile.MaxHp, Hp + restored);
            if (Hp == Profile.MaxHp) FieldRecoveryRemainderHundredths = 0;
        }
        internal void AddPersonalXp(decimal amount) { if (amount < 0m) throw new ArgumentOutOfRangeException(nameof(amount)); PersonalXp += amount; }
        internal void AddCommandXp(decimal amount)
        {
            if (!IsCommander) throw new InvalidOperationException("Only a Commander receives Command XP.");
            if (amount < 0m) throw new ArgumentOutOfRangeException(nameof(amount));
            CommandXp += amount;
            int target = 1 + (int)(CommandXp / 10m);
            CommandLevel = Math.Min(CommandLevel + 1, target); // Canon: one Command Level per tactical battle.
        }
    }

    public sealed class PersistentFormation
    {
        private readonly List<PersistentCharacter> members;
        public string FormationId { get; }
        public Side Side { get; }
        public ReadOnlyCollection<PersistentCharacter> Members { get; }
        public bool Commanderless { get; private set; }
        public bool RosterLocked => Commanderless;
        public PersistentCharacter Commander => members.SingleOrDefault(c => c.IsCommander);
        public IEnumerable<PersistentCharacter> LivingMembers => members.Where(c => c.Status != PersistentCharacterStatus.Dead);

        public PersistentFormation(string formationId, Side side, IEnumerable<PersistentCharacter> characters)
        {
            if (string.IsNullOrWhiteSpace(formationId)) throw new ArgumentException("Formation ID is required.", nameof(formationId));
            if (side != Side.West && side != Side.East) throw new ArgumentOutOfRangeException(nameof(side));
            members = (characters ?? throw new ArgumentNullException(nameof(characters))).ToList();
            if (members.Count == 0 || members.Any(c => c == null) || members.Select(c => c.CharacterId).Distinct().Count() != members.Count)
                throw new ArgumentException("Persistent roster needs unique non-null characters.");
            if (members.Count(c => c.IsCommander) > 1) throw new ArgumentException("Slice supports at most one Commander.");
            FormationId = formationId; Side = side; Members = members.AsReadOnly(); RefreshCommanderState();
        }
        internal void AddRecruit(PersistentCharacter recruit)
        {
            if(RosterLocked||Commander==null||recruit==null||recruit.IsCommander||members.Any(c=>c.CharacterId==recruit.CharacterId))
                throw new InvalidOperationException("Invalid persistent recruit addition.");
            members.Add(recruit);
        }
        internal void RefreshCommanderState() { Commanderless = Commander != null && Commander.Status == PersistentCharacterStatus.Dead; }
        public void ReturnSafeMembersForNextBattle() { foreach (var member in members) member.ReturnFromSafety(); }
        public void ApplyOneFieldStrategicRefresh() { foreach (var member in members) member.ApplyHpRefresh(PersistentCharacter.FieldRecoveryPercent); }
        public void ApplyOneHealingBuildingStrategicRefresh() { foreach (var member in members) member.ApplyHpRefresh(PersistentCharacter.HealingBuildingRecoveryPercent); }
    }

    public readonly struct PersistentDeployment
    {
        public string CharacterId { get; }
        public UnitId UnitId { get; }
        public GridPosition Position { get; }
        public Facing Facing { get; }
        public RetreatEdge? OwnRetreatEdge { get; }
        public PersistentDeployment(string characterId, UnitId unitId, GridPosition position, Facing facing, RetreatEdge? ownRetreatEdge = null)
        { CharacterId = characterId; UnitId = unitId; Position = position; Facing = facing; OwnRetreatEdge = ownRetreatEdge; }
    }

    public sealed class PersistentBattle
    {
        private readonly PersistentFormation[] west, east;
        private readonly Dictionary<UnitId, PersistentCharacter> characters;
        private readonly Dictionary<Side, decimal> startEffectivePower;
        private readonly Dictionary<Side, int> startParticipants;
        private readonly Dictionary<UnitId,decimal> startBasePower;
        public BattleState State { get; }
        public IReadOnlyDictionary<Side, decimal> StartEffectivePower => startEffectivePower;

        private PersistentBattle(PersistentFormation[] west, PersistentFormation[] east, BattleState state, Dictionary<UnitId, PersistentCharacter> characters)
        {
            this.west = west; this.east = east; State = state; this.characters = characters;
            startBasePower=characters.ToDictionary(k=>k.Key,k=>BasePower(k.Value));
            startEffectivePower = new Dictionary<Side, decimal> {
                { Side.West, EffectivePower(state.Units.Where(u => u.Side == Side.West), characters) },
                { Side.East, EffectivePower(state.Units.Where(u => u.Side == Side.East), characters) } };
            startParticipants = new Dictionary<Side, int> {{Side.West, state.Units.Count(u => u.Side == Side.West)}, {Side.East, state.Units.Count(u => u.Side == Side.East)}};
        }

        public static PersistentBattle Start(PersistentFormation west, PersistentFormation east, IEnumerable<PersistentDeployment> deployments, uint seed, Battlefield board)
        {
            return Start(new[]{west},new[]{east},deployments,seed,board);
        }
        // Tactical sides are coalition slots, independent of persistent strategic ownership.
        public static PersistentBattle Start(PersistentFormation[] west, PersistentFormation[] east, IEnumerable<PersistentDeployment> deployments, uint seed, Battlefield board)
        {
            if (west == null || east == null || deployments == null || west.Length==0 || east.Length==0) throw new ArgumentNullException();
            foreach(var f in west.Concat(east)) f.ReturnSafeMembersForNextBattle();
            var lookup = west.Concat(east).SelectMany(f=>f.Members).ToDictionary(c => c.CharacterId);
            var units = new List<UnitState>(); var map = new Dictionary<UnitId, PersistentCharacter>();
            foreach (var d in deployments)
            {
                if (!lookup.TryGetValue(d.CharacterId, out var character)) throw new ArgumentException("Unknown persistent character " + d.CharacterId);
                if (character.Status == PersistentCharacterStatus.Dead) continue;
                if (map.ContainsKey(d.UnitId)) throw new ArgumentException("Duplicate tactical unit ID.");
                var side = west.Any(f=>f.Members.Contains(character)) ? Side.West : Side.East;
                units.Add(new UnitState(d.UnitId, side, character.Profile, d.Position, d.Facing, character.Hp, character.Armor, UnitStatus.Active, d.OwnRetreatEdge) { FireballUsed=character.FireballUsed,FreezeUsed=character.FreezeUsed,CloseHealUsed=character.CloseHealUsed });
                map.Add(d.UnitId, character);
            }
            if (units.Count(u => u.Side == Side.West) == 0 || units.Count(u => u.Side == Side.East) == 0) throw new InvalidOperationException("Both persistent formations need a living deployed member.");
            return new PersistentBattle(west, east, BattleResolver.StartBattle(units, seed, board).State, map);
        }

        public PersistenceBattleResolution Resolve(BattleState finalState)
        {
            if (finalState == null || !finalState.Outcome.IsEnded) throw new InvalidOperationException("Resolve only a completed tactical battle.");
            foreach (var unit in finalState.Units) if (characters.TryGetValue(unit.Id, out var character)) character.SetBattleResult(unit);
            foreach(var f in west.Concat(east)) f.RefreshCommanderState();
            var westPool = ResolveSide(Side.West, finalState); var eastPool = ResolveSide(Side.East, finalState);
            return new PersistenceBattleResolution(westPool, eastPool);
        }

        private PersistenceSideResolution ResolveSide(Side side, BattleState finalState)
        {
            var own = side == Side.West ? west : east; var enemy = side == Side.West ? east : west;
            var participants = finalState.Units.Where(u => u.Side == side).ToArray();
            decimal enemyWeight = finalState.Units.Where(u => u.Side != side).Sum(u => EnemyXpWeight(u));
            bool won = finalState.Outcome.VictorySide == side;
            decimal pool = won ? enemyWeight * UnderdogMultiplier(startEffectivePower[side], startEffectivePower[side == Side.West ? Side.East : Side.West]) : enemyWeight * .5m;
            decimal personalReserve = pool / startParticipants[side];
            foreach (var unit in participants)
            {
                var character = characters[unit.Id]; character.AddPersonalXp(personalReserve * OutcomeShare(unit.Status));
            }
            decimal commandReserve = pool / (7m * own.Length);
            foreach(var formation in own)
            {
                var commander = formation.Commander;
                if(commander==null)continue;
                var commanderUnit = participants.SingleOrDefault(u => characters[u.Id] == commander);
                if (commanderUnit != null && OutcomeShare(commanderUnit.Status) > 0m)
                    commander.AddCommandXp(commandReserve * OutcomeShare(commanderUnit.Status));
            }
            return new PersistenceSideResolution(side, pool, personalReserve, commandReserve, enemyWeight, won);
        }

        private decimal EnemyXpWeight(UnitState unit)
        {
            // Resolving West XP may raise a level before East XP is awarded. Valuation is a battle-start snapshot.
            decimal basePower = startBasePower[unit.Id];
            return basePower * .2m * (unit.Status == UnitStatus.Escaped ? .15m : unit.Status == UnitStatus.Dead ? 1m : 0m);
        }
        private static decimal OutcomeShare(UnitStatus status) => status == UnitStatus.Active ? 1m : status == UnitStatus.Escaped ? .25m : 0m;
        private static decimal UnderdogMultiplier(decimal own, decimal enemy)
        {
            decimal ratio = enemy / own; return Math.Min(1.5m, Math.Max(1m, 1m + .5m * (ratio - 1m)));
        }
        public static decimal BasePower(PersistentCharacter character) => (character.Profile.Tier==2?13m:10m) + .2m * character.PersonalLevel;
        private static decimal EffectivePower(IEnumerable<UnitState> units, Dictionary<UnitId, PersistentCharacter> characters) => units.Sum(u => {
            decimal denominator = 1.5m * u.Profile.MaxHp + u.Profile.MaxArmor;
            decimal durability = (1.5m * u.Hp + u.Armor) / denominator;
            decimal readiness = .5m + .5m * Math.Min(1m, Math.Max(0m, durability));
            return BasePower(characters[u.Id]) * readiness;
        });
    }

    public readonly struct PersistenceSideResolution
    {
        public Side Side { get; } public decimal Pool { get; } public decimal PersonalReserve { get; } public decimal CommandReserve { get; } public decimal EarnedEnemyWeight { get; } public bool Victory { get; }
        public PersistenceSideResolution(Side side, decimal pool, decimal personalReserve, decimal commandReserve, decimal earnedEnemyWeight, bool victory)
        { Side = side; Pool = pool; PersonalReserve = personalReserve; CommandReserve = commandReserve; EarnedEnemyWeight = earnedEnemyWeight; Victory = victory; }
    }
    public readonly struct PersistenceBattleResolution
    {
        public PersistenceSideResolution West { get; } public PersistenceSideResolution East { get; }
        public PersistenceBattleResolution(PersistenceSideResolution west, PersistenceSideResolution east) { West = west; East = east; }
    }
}

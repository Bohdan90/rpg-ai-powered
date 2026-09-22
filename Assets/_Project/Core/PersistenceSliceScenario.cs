using System;
using System.Collections.Generic;
using System.Linq;

namespace RPG.Core
{
    // Developer-only three-battle sequence. The opponent is a fresh deterministic scenario force
    // each time; only the West formation is the persistent subject under test.
    public sealed class PersistenceSliceScenario
    {
        // Same 9-figure reference composition and readable 23×17 field deployment as Gate C.
        private static readonly UnitProfile[] Profiles = { UnitProfile.HumanWarriorTI, UnitProfile.HumanWarriorTI,
            UnitProfile.HumanArcherTI, UnitProfile.HumanArcherTI, UnitProfile.ElfWarriorTI, UnitProfile.HumanWarriorTI,
            UnitProfile.HumanWarriorTI, UnitProfile.HumanArcherTI, UnitProfile.ElfWarriorTI };
        private static readonly GridPosition[] WestCells = { new GridPosition(2,8), new GridPosition(3,6), new GridPosition(1,5),
            new GridPosition(1,11), new GridPosition(3,12), new GridPosition(3,8), new GridPosition(3,10), new GridPosition(1,8), new GridPosition(3,4) };
        private static readonly GridPosition[] EastCells = { new GridPosition(20,8), new GridPosition(19,6), new GridPosition(21,5),
            new GridPosition(21,11), new GridPosition(19,12), new GridPosition(19,8), new GridPosition(19,10), new GridPosition(21,8), new GridPosition(19,4) };
        public PersistentFormation Formation { get; }
        public int BattleNumber { get; private set; }
        public PersistentBattle CurrentBattle { get; private set; }
        public PersistenceBattleResolution? LastResolution { get; private set; }
        public string LastRefresh { get; private set; } = "No Strategic Refresh before Battle 2.";
        public bool IsResolved { get; private set; }

        public PersistenceSliceScenario()
        {
            Formation = new PersistentFormation("persistence-west", Side.West, Profiles.Select((p,i) =>
                new PersistentCharacter("west-" + (i + 1), p, i == 0)).ToArray());
        }
        public PersistentBattle StartFirstBattle() { BattleNumber = 1; IsResolved = false; return CurrentBattle = CreateBattle(); }
        public PersistentBattle StartNextBattle()
        {
            if (!IsResolved || BattleNumber >= 3) throw new InvalidOperationException("Resolve the current persistence battle before continuing.");
            if (BattleNumber == 2) ApplyFieldRefresh(); else LastRefresh = "0 Strategic Refresh between Battle 1 and Battle 2.";
            BattleNumber++; IsResolved = false; return CurrentBattle = CreateBattle();
        }
        public PersistenceBattleResolution Resolve(BattleState finalState)
        {
            if (IsResolved) throw new InvalidOperationException("Current persistence battle was already resolved.");
            LastResolution = CurrentBattle.Resolve(finalState); IsResolved = true; return LastResolution.Value;
        }
        private PersistentBattle CreateBattle()
        {
            var enemy = new PersistentFormation("scenario-east-" + BattleNumber, Side.East, Profiles.Select((p,i) =>
                new PersistentCharacter("east-" + BattleNumber + "-" + (i + 1), p, i == 0)).ToArray());
            var deployments = new List<PersistentDeployment>();
            for (int i=0;i<Formation.Members.Count;i++)
                deployments.Add(new PersistentDeployment(Formation.Members[i].CharacterId,new UnitId(i < 5 ? i + 1 : i + 6),WestCells[i],Facing.East));
            for (int i=0;i<enemy.Members.Count;i++)
                deployments.Add(new PersistentDeployment(enemy.Members[i].CharacterId,new UnitId(i < 5 ? i + 6 : i + 10),EastCells[i],Facing.West));
            return PersistentBattle.Start(Formation,enemy,deployments,20260921u+(uint)BattleNumber,SizeExperimentFixture.Board(SizeExperimentMap.Field_23x17_Full_9v9));
        }
        private void ApplyFieldRefresh()
        {
            var before = Formation.LivingMembers.ToDictionary(c => c.CharacterId, c => c.Hp);
            Formation.ApplyOneFieldStrategicRefresh();
            LastRefresh = "1 Field Strategic Refresh applied: " + string.Join(", ", Formation.LivingMembers.Select(c => c.CharacterId + " " + before[c.CharacterId] + "→" + c.Hp + " HP")) + ". Armor unchanged.";
        }
        public string Summary()
        {
            string result = IsResolved && LastResolution.HasValue ? (LastResolution.Value.West.Victory ? "Victory" : "Defeat") : "In progress";
            var commander = Formation.Commander;
            string commanderText = commander == null ? "No Commander record" : "Commander " + commander.CharacterId + " · " + commander.Status
                + " · Command XP " + commander.CommandXp.ToString("0.##") + " · Command L" + commander.CommandLevel + " · Rank " + commander.CommandRank;
            return "PERSISTENCE SLICE · Battle " + BattleNumber + "\nResult: " + result + "\n" + LastRefresh
                + "\nFormation: " + (Formation.Commanderless ? "Commanderless · roster locked" : "Commanded")
                + "\n" + commanderText + "\n\nPersistent roster:\n" + string.Join("\n", Formation.Members.Select(c => c.CharacterId + " · " + c.Profile.Id
                    + " · " + c.Status + " · HP " + c.Hp + "/" + c.Profile.MaxHp + " · Armor " + c.Armor + "/" + c.Profile.MaxArmor
                    + " · Personal XP " + c.PersonalXp.ToString("0.##") + " · L" + c.PersonalLevel));
        }
    }
}

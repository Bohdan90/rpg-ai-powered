using System.Linq;
using NUnit.Framework;
using RPG.Core;

namespace RPG.Tests
{
    public class PersistentFormationTests
    {
        private static PersistentCharacter C(string id, UnitProfile profile = null, bool commander = false, int? hp = null, int? armor = null,
            PersistentCharacterStatus status = PersistentCharacterStatus.Alive, decimal xp = 0m, decimal commandXp = 0m, int commandLevel = 1) =>
            new PersistentCharacter(id, profile ?? UnitProfile.HumanWarriorTI, commander, hp, armor, status, xp, commandXp, commandLevel);
        private static PersistentBattle Battle(PersistentFormation west, PersistentFormation east)
        {
            var d = west.LivingMembers.Select((c, i) => new PersistentDeployment(c.CharacterId, new UnitId(i + 1), new GridPosition(1 + i, 2), Facing.East))
                .Concat(east.LivingMembers.Select((c, i) => new PersistentDeployment(c.CharacterId, new UnitId(20 + i), new GridPosition(11 - i, 2), Facing.West)));
            return PersistentBattle.Start(west, east, d, 7, new Battlefield(13, 9));
        }
        private static BattleState End(PersistentBattle battle, Side winner, params (UnitId id, UnitStatus status, int hp, int armor)[] results)
        {
            var s = battle.State.Copy();
            foreach (var r in results)
            {
                var u = s.FindUnit(r.id); u.Status = r.status; u.Hp = r.hp; u.Armor = r.armor;
            }
            s.Outcome = new BattleOutcome(winner, winner == Side.West ? Side.East : Side.West, BattleEndReason.Eliminated);
            return s;
        }

        [Test]
        public void AttritionEscapesDeathsAndIdentityCarryAcrossBattlesWithoutNormalization()
        {
            var commander = C("west-commander", commander: true, hp: 17, armor: 5);
            var safe = C("west-safe", UnitProfile.HumanArcherTI, hp: 9, armor: 1);
            var dead = C("west-dead", UnitProfile.ElfWarriorTI);
            var west = new PersistentFormation("west", Side.West, new[] { commander, safe, dead });
            var east = new PersistentFormation("east", Side.East, new[] { C("east", hp: 1, armor: 0) });
            var first = Battle(west, east);
            var final = End(first, Side.West, (new UnitId(1), UnitStatus.Active, 17, 5), (new UnitId(2), UnitStatus.Escaped, 9, 1), (new UnitId(3), UnitStatus.Dead, 0, 0), (new UnitId(20), UnitStatus.Dead, 0, 0));
            first.Resolve(final);
            Assert.That(commander.CharacterId, Is.EqualTo("west-commander"));
            Assert.That(commander.Hp, Is.EqualTo(17)); Assert.That(commander.Armor, Is.EqualTo(5));
            Assert.That(safe.Status, Is.EqualTo(PersistentCharacterStatus.EscapedSafe)); Assert.That(safe.Hp, Is.EqualTo(9)); Assert.That(safe.Armor, Is.EqualTo(1));
            Assert.That(dead.Status, Is.EqualTo(PersistentCharacterStatus.Dead));
            var next = Battle(west, new PersistentFormation("east-2", Side.East, new[] { C("east-2") }));
            Assert.That(next.State.Units.Count(u => u.Side == Side.West), Is.EqualTo(2));
            Assert.That(next.State.Units.Select(u => u.Id.Value), Has.No.Member(3));
            Assert.That(next.State.FindUnit(new UnitId(1)).Hp, Is.EqualTo(17)); Assert.That(next.State.FindUnit(new UnitId(1)).Armor, Is.EqualTo(5));
            Assert.That(next.State.FindUnit(new UnitId(2)).Hp, Is.EqualTo(9)); Assert.That(next.State.FindUnit(new UnitId(2)).Armor, Is.EqualTo(1));
        }

        [Test]
        public void FieldRefreshUsesExactPercentAccumulatorAndNeverRepairsArmor()
        {
            var warrior = C("warrior", hp: 17, armor: 5);
            var archer = C("archer", UnitProfile.HumanArcherTI, hp: 17, armor: 1);
            var west = new PersistentFormation("west", Side.West, new[] { warrior, archer });
            west.ApplyOneFieldStrategicRefresh();
            Assert.That(warrior.Hp, Is.EqualTo(23)); Assert.That(warrior.Armor, Is.EqualTo(5));
            Assert.That(archer.Hp, Is.EqualTo(21)); Assert.That(archer.FieldRecoveryRemainderHundredths, Is.EqualTo(20)); Assert.That(archer.Armor, Is.EqualTo(1));
            west.ApplyOneFieldStrategicRefresh();
            Assert.That(archer.Hp, Is.EqualTo(25)); Assert.That(archer.FieldRecoveryRemainderHundredths, Is.EqualTo(40));
        }

        [Test]
        public void CommanderDeathLocksButDoesNotDeleteRemnant()
        {
            var commander = C("commander", commander: true); var troop = C("troop");
            var west = new PersistentFormation("west", Side.West, new[] { commander, troop });
            var east = new PersistentFormation("east", Side.East, new[] { C("east", hp: 1, armor: 0) }); var battle = Battle(west, east);
            battle.Resolve(End(battle, Side.West, (new UnitId(1), UnitStatus.Dead, 0, 0), (new UnitId(2), UnitStatus.Active, 12, 3), (new UnitId(20), UnitStatus.Dead, 0, 0)));
            Assert.That(west.Commanderless, Is.True); Assert.That(west.RosterLocked, Is.True); Assert.That(troop.Status, Is.EqualTo(PersistentCharacterStatus.Alive));
            var later = Battle(west, new PersistentFormation("east-2", Side.East, new[] { C("east-2") }));
            Assert.That(later.State.Units.Count(u => u.Side == Side.West), Is.EqualTo(1)); Assert.That(later.State.FindUnit(new UnitId(1)).Hp, Is.EqualTo(12));
        }

        [Test]
        public void PoolsUseStartReadinessOutcomeSharesAndProgressionCaps()
        {
            var commander = C("commander", commander: true, hp: 10, armor: 0, xp: 9.9m, commandXp: 19m);
            var safe = C("safe", hp: 40, armor: 16); var dead = C("dead");
            var west = new PersistentFormation("west", Side.West, new[] { commander, safe, dead });
            var east = new PersistentFormation("east", Side.East, new[] { C("enemy", hp: 1, armor: 0) }); var battle = Battle(west, east);
            Assert.That(battle.StartEffectivePower[Side.West], Is.LessThan(30m));
            var resolved = battle.Resolve(End(battle, Side.West, (new UnitId(1), UnitStatus.Active, 10, 0), (new UnitId(2), UnitStatus.Escaped, 40, 16), (new UnitId(3), UnitStatus.Dead, 0, 0), (new UnitId(20), UnitStatus.Dead, 0, 0)));
            Assert.That(resolved.West.Victory, Is.True); Assert.That(resolved.West.Pool, Is.GreaterThan(0m));
            Assert.That(commander.PersonalXp, Is.GreaterThan(9.9m)); Assert.That(safe.PersonalXp, Is.LessThan(commander.PersonalXp)); Assert.That(dead.PersonalXp, Is.EqualTo(0m));
            Assert.That(commander.PersonalLevel, Is.EqualTo(2)); Assert.That(commander.CommandXp, Is.GreaterThan(19m)); Assert.That(commander.CommandLevel, Is.EqualTo(2)); Assert.That(commander.CommandRank, Is.EqualTo(CommandRank.I));
            Assert.That(PersistentBattle.BasePower(commander), Is.EqualTo(10.4m));
        }
    }
}

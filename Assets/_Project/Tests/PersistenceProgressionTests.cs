using System;
using System.Linq;
using NUnit.Framework;
using RPG.Core;

namespace RPG.Tests
{
    public class PersistenceProgressionTests
    {
        private static PersistentCharacter Character(string id, bool commander = false, int? hp = null, int? armor = null,
            decimal personalXp = 0m, decimal commandXp = 0m, int commandLevel = 1) =>
            new PersistentCharacter(id, UnitProfile.HumanWarriorTI, commander, hp, armor,
                PersistentCharacterStatus.Alive, personalXp, commandXp, commandLevel);

        private static PersistentBattle Start(PersistentFormation west, PersistentFormation east)
        {
            var deployments = west.LivingMembers.Select((c, i) => new PersistentDeployment(c.CharacterId, new UnitId(i + 1), new GridPosition(1 + i, 2), Facing.East))
                .Concat(east.LivingMembers.Select((c, i) => new PersistentDeployment(c.CharacterId, new UnitId(i + 20), new GridPosition(11 - i, 2), Facing.West)));
            return PersistentBattle.Start(west, east, deployments, 91, new Battlefield(13, 9));
        }

        private static BattleState Finish(PersistentBattle battle, Side winner, params UnitStatus[] westStatuses)
        {
            var state = battle.State.Copy(); int west = 0;
            foreach (var unit in state.Units)
            {
                if (unit.Side == Side.West)
                {
                    unit.Status = westStatuses[west++]; unit.Hp = unit.Status == UnitStatus.Dead ? 0 : unit.Hp;
                    if (unit.Status == UnitStatus.Dead) unit.Armor = 0;
                }
                else { unit.Status = UnitStatus.Dead; unit.Hp = 0; unit.Armor = 0; }
            }
            state.Outcome = new BattleOutcome(winner, winner == Side.West ? Side.East : Side.West, BattleEndReason.Eliminated);
            return state;
        }

        [Test]
        public void VictorySharesUseStartDenominatorAndOutcomeFractionsWithoutRedistribution()
        {
            var commander = Character("commander", true); var escaped = Character("escaped"); var dead = Character("dead");
            var west = new PersistentFormation("west", Side.West, new[] { commander, escaped, dead });
            var east = new PersistentFormation("east", Side.East, new[] { Character("enemy") }); var battle = Start(west, east);
            var resolved = battle.Resolve(Finish(battle, Side.West, UnitStatus.Active, UnitStatus.Escaped, UnitStatus.Dead)).West;
            decimal ratio = battle.StartEffectivePower[Side.East] / battle.StartEffectivePower[Side.West];
            decimal multiplier = Math.Min(1.5m, Math.Max(1m, 1m + .5m * (ratio - 1m)));
            Assert.That(resolved.EarnedEnemyWeight, Is.EqualTo(2.04m));
            Assert.That(resolved.Pool, Is.EqualTo(resolved.EarnedEnemyWeight * multiplier));
            Assert.That(resolved.PersonalReserve, Is.EqualTo(resolved.Pool / 3m));
            Assert.That(commander.PersonalXp, Is.EqualTo(resolved.PersonalReserve));
            Assert.That(escaped.PersonalXp, Is.EqualTo(resolved.PersonalReserve * .25m));
            Assert.That(dead.PersonalXp, Is.EqualTo(0m));
            Assert.That(commander.CommandXp, Is.EqualTo(resolved.CommandReserve));
        }

        [Test]
        public void DefeatPoolAndCommanderOutcomeSharesFollowCanonicalFractions()
        {
            var escapedCommander = Character("escaped-commander", true); var troop = Character("troop");
            var west = new PersistentFormation("west", Side.West, new[] { escapedCommander, troop });
            var east = new PersistentFormation("east", Side.East, new[] { Character("enemy") }); var battle = Start(west, east);
            var resolved = battle.Resolve(Finish(battle, Side.East, UnitStatus.Escaped, UnitStatus.Dead)).West;
            Assert.That(resolved.Pool, Is.EqualTo(resolved.EarnedEnemyWeight * .5m));
            Assert.That(escapedCommander.PersonalXp, Is.EqualTo(resolved.PersonalReserve * .25m));
            Assert.That(escapedCommander.CommandXp, Is.EqualTo(resolved.CommandReserve * .25m));
            Assert.That(troop.PersonalXp, Is.EqualTo(0m));
        }

        [Test]
        public void LevelsKeepOverflowAndCommandLevelsResolveOneAtATime()
        {
            var commander = Character("commander", true, personalXp: 29.9m, commandXp: 39.9m, commandLevel: 1);
            var profile = commander.Profile;
            commander.AddPersonalXp(.2m);
            commander.AddCommandXp(.2m);
            Assert.That(commander.PersonalLevel, Is.EqualTo(4));
            Assert.That(commander.Profile, Is.SameAs(profile));
            Assert.That(commander.CommandXp, Is.EqualTo(40.1m));
            Assert.That(commander.CommandLevel, Is.EqualTo(2));
            commander.AddCommandXp(0m); commander.AddCommandXp(0m);
            Assert.That(commander.CommandLevel, Is.EqualTo(4));
            Assert.That(commander.CommandRank, Is.EqualTo(CommandRank.II));
            Assert.That(PersistentBattle.BasePower(commander), Is.EqualTo(10.8m));
        }
    }
}

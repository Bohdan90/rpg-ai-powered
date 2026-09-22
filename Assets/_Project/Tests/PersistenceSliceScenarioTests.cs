using System.Linq;
using NUnit.Framework;
using RPG.Core;

namespace RPG.Tests
{
    public class PersistenceSliceScenarioTests
    {
        private static BattleState End(PersistentBattle battle, Side winner, int westHp, int westArmor)
        {
            var state = battle.State.Copy();
            foreach (var unit in state.Units)
            {
                if (unit.Side == Side.West) { unit.Hp = unit.Id.Value == 1 ? westHp : 0; unit.Armor = unit.Id.Value == 1 ? westArmor : 0; unit.Status = unit.Id.Value == 1 ? UnitStatus.Active : UnitStatus.Dead; }
                else { unit.Hp = 0; unit.Armor = 0; unit.Status = UnitStatus.Dead; }
            }
            state.Outcome = new BattleOutcome(winner, winner == Side.West ? Side.East : Side.West, BattleEndReason.Eliminated);
            return state;
        }

        [Test]
        public void ThreeBattleFlowHasZeroThenOneFieldRefreshAndKeepsAttritedRoster()
        {
            var scenario = new PersistenceSliceScenario(); var one = scenario.StartFirstBattle();
            scenario.Resolve(End(one, Side.West, 17, 5));
            var two = scenario.StartNextBattle();
            Assert.That(two.State.Battlefield.Columns, Is.EqualTo(23)); Assert.That(two.State.Battlefield.Rows, Is.EqualTo(17));
            StringAssert.Contains("Result: In progress", scenario.Summary());
            Assert.That(two.State.Units.Count(u => u.Side == Side.West), Is.EqualTo(1));
            Assert.That(two.State.FindUnit(new UnitId(1)).Hp, Is.EqualTo(17)); Assert.That(two.State.FindUnit(new UnitId(1)).Armor, Is.EqualTo(5));
            scenario.Resolve(End(two, Side.West, 17, 5));
            var three = scenario.StartNextBattle();
            Assert.That(three.State.FindUnit(new UnitId(1)).Hp, Is.EqualTo(23)); Assert.That(three.State.FindUnit(new UnitId(1)).Armor, Is.EqualTo(5));
            StringAssert.Contains("1 Field Strategic Refresh applied", scenario.Summary());
            Assert.That(scenario.Formation.Members.Count(c => c.Status == PersistentCharacterStatus.Dead), Is.EqualTo(8));
        }

        [Test]
        public void IdenticalPersistentResultProducesIdenticalNextBattleState()
        {
            var first = new PersistenceSliceScenario(); var firstBattle = first.StartFirstBattle();
            first.Resolve(End(firstBattle, Side.West, 17, 5)); var firstNext = first.StartNextBattle();
            var second = new PersistenceSliceScenario(); var secondBattle = second.StartFirstBattle();
            second.Resolve(End(secondBattle, Side.West, 17, 5)); var secondNext = second.StartNextBattle();
            Assert.That(BattleStateHash.Compute(firstNext.State), Is.EqualTo(BattleStateHash.Compute(secondNext.State)));
            Assert.That(first.Summary(), Is.EqualTo(second.Summary()));
        }
    }
}

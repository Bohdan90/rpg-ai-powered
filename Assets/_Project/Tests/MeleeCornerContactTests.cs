using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RPG.Core;

namespace RPG.Tests
{
    public class MeleeCornerContactTests
    {
        private static IEnumerable<TestCaseData> Corners()
        {
            foreach (int dx in new[] { -1, 1 })
            foreach (int dy in new[] { -1, 1 })
            for (int walls = 0; walls < 4; walls++)
                yield return new TestCaseData(dx, dy, walls).SetName($"MeleeCorner_dx{dx}_dy{dy}_walls{walls}");
        }

        [TestCaseSource(nameof(Corners))]
        public void ContactPreviewZocAndOaShareOpenCornerContract(int dx, int dy, int walls)
        {
            var from = new GridPosition(4, 4);
            var enemyCell = new GridPosition(4 + dx, 4 + dy);
            var solids = new List<GridPosition>();
            if ((walls & 1) != 0) solids.Add(new GridPosition(4 + dx, 4));
            if ((walls & 2) != 0) solids.Add(new GridPosition(4, 4 + dy));
            var actor = new UnitState(new UnitId(5), Side.West, UnitProfile.ElfWarriorTI, from, Facing.East);
            var enemy = new UnitState(new UnitId(7), Side.East, UnitProfile.HumanWarriorTI, enemyCell, Facing.West);
            var state = BattleResolver.StartBattle(new[] { actor, enemy }, 2, new Battlefield(solids)).State;
            uint rng = state.RngState;
            bool allowed = walls != 3;
            var attack = new BasicAttackCommand(actor.Id, enemy.Id);
            Assert.That(BattleResolver.PreviewAttack(state, attack).IsLegal, Is.EqualTo(allowed), "attack preview");
            Assert.That(BattleResolver.Validate(state, attack), Is.EqualTo(allowed ? CommandError.None : CommandError.BlockedCorner));
            Assert.That(LineOfSight.IsMeleeCornerClear(state, enemyCell, from), Is.EqualTo(allowed), "symmetric contact");
            Assert.That(ZoneOfControl.Exerts(state, state.FindUnit(enemy.Id), from), Is.EqualTo(allowed));
            Assert.That(ZoneOfControl.Sources(state, actor.Side, from).Contains(enemy.Id), Is.EqualTo(allowed));
            var exit = new GridPosition(4 - dx, 4);
            var move = new MoveCommand(actor.Id, new[] { exit });
            var preview = OpportunityAttackPreview.Query(state, move);
            Assert.That(preview.IsLegal, Is.True);
            Assert.That(preview.Exposures.Sum(e => e.Threats.Count(t => t.WouldReact)), Is.EqualTo(allowed ? 1 : 0));
            Assert.That(ZoneOfControl.Reactors(state, actor.Id, from, exit).Contains(enemy.Id), Is.EqualTo(allowed));
            Assert.That(state.RngState, Is.EqualTo(rng));
            Assert.That(state.FindUnit(actor.Id).Position, Is.EqualTo(from));
            Assert.That(state.FindUnit(enemy.Id).OpportunityAttackAvailable, Is.True);
            var attacked = BattleResolver.Apply(state, attack);
            Assert.That(attacked.IsApplied, Is.EqualTo(allowed));
            if (!allowed) { Assert.That(attacked.State, Is.SameAs(state)); Assert.That(attacked.State.RngState, Is.EqualTo(rng)); }
            var moved = BattleResolver.Apply(state, move);
            Assert.That(moved.IsApplied, Is.True);
            Assert.That(moved.State.FindUnit(actor.Id).Position, Is.EqualTo(exit));
            Assert.That(moved.Events.Count(e => e.Kind == BattleEventKind.OpportunityAttackTriggered), Is.EqualTo(allowed ? 1 : 0));
            Assert.That(moved.State.FindUnit(enemy.Id).OpportunityAttackAvailable, Is.EqualTo(!allowed));
            if (allowed)
            {
                var events = moved.Events.Select(e => e.Kind).ToList();
                Assert.That(events.IndexOf(BattleEventKind.OpportunityAttackResolved), Is.LessThan(events.IndexOf(BattleEventKind.StepMoved)));
            }
            else Assert.That(moved.State.RngState, Is.EqualTo(rng));
        }

        [Test]
        public void OpenMeleeCornerDoesNotRelaxMovementOrRangedSupercover()
        {
            var actor = new UnitState(new UnitId(5), Side.West, UnitProfile.ElfWarriorTI, new GridPosition(4,4), Facing.East);
            var state = BattleResolver.StartBattle(new[] { actor }, 2, new Battlefield(new[] { new GridPosition(5,4) })).State;
            var destination = new GridPosition(5,5);
            Assert.That(LineOfSight.IsMeleeCornerClear(state, actor.Position, destination), Is.True);
            Assert.That(LineOfSight.IsClear(state, actor.Position, destination), Is.False);
            Assert.That(MovementRules.ValidateStep(state, actor.Id, actor.Position, destination), Is.EqualTo(CommandError.BlockedCorner));
            var path = Pathfinder.FindPath(state, actor.Id, destination);
            Assert.That(path.Found, Is.True); Assert.That(path.Cost, Is.EqualTo(2));
        }
    }
}

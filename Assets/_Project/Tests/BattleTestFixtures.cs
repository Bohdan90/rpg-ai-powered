using System;
using System.Linq;
using RPG.Core;

namespace RPG.Tests
{
    internal static class BattleTestFixtures
    {
        internal static readonly UnitId Attacker = new UnitId(1);
        internal static readonly UnitId Target = new UnitId(2);
        internal static UnitState Unit(int id, UnitProfile profile, Side side = Side.West,
            int x = 0, int y = 0, Facing facing = Facing.East, int? hp = null, int? armor = null,
            UnitStatus status = UnitStatus.Active) =>
            new UnitState(new UnitId(id), side, profile, new GridPosition(x, y), facing, hp, armor, status);

        internal static BattleState Duel(UnitProfile attacker = null, UnitProfile target = null,
            uint seed = 1, int? targetHp = null, int? targetArmor = null,
            Facing targetFacing = Facing.East, int targetX = 1, int targetY = 0, Side targetSide = Side.East)
        {
            var start = BattleResolver.StartBattle(new[] {
                Unit(1, attacker ?? UnitProfile.HumanWarriorTI),
                Unit(2, target ?? UnitProfile.HumanArcherTI, targetSide, targetX, targetY, targetFacing, targetHp, targetArmor)
            }, seed);
            return ToActor(start.State, Attacker);
        }
        internal static BattleState ToActor(BattleState state, UnitId actor)
        {
            for (int i = 0; i <= state.Units.Count; i++)
            {
                if (state.CurrentUnitId == actor) return state;
                state = BattleResolver.Apply(state, new EndActivationCommand(state.CurrentUnitId.Value)).State;
            }
            throw new InvalidOperationException("Expected actor is not active.");
        }
        internal static BasicAttackCommand Attack() => new BasicAttackCommand(Attacker, Target);
        internal static string Snapshot(BattleState state) => string.Join("|", new[] {
            string.Join(";", state.Battlefield.SolidCells.Select(p => p.X + "," + p.Y)),
            state.InitialSeed.ToString(), state.RngState.ToString(), state.Round.ToString(),
            state.CurrentUnitId.ToString(), state.PriorityIndex.ToString(),
            string.Join(",", state.PriorityOrder), string.Join(",", state.ActivationOrder),
            string.Join(";", state.Units.Select(u => string.Join(",", new object[] {
                u.Id, u.Side, u.Profile.Id, u.Position.X, u.Position.Y, u.Facing, u.Hp, u.Armor,
                u.Status, u.ActionAvailable, u.MovementRemaining, u.MovementSpentThisActivation,
                u.IsDefending, u.PhysicalResistance, u.TieKey
            })))
        });
    }
}

using System.Linq;
using NUnit.Framework;
using RPG.Core;
namespace RPG.Tests
{
    public class HealApproachTests
    {
        static readonly UnitId A=new UnitId(1),T=new UnitId(2);
        static BattleState State(int x=10,Battlefield board=null)=>BattleTestFixtures.ToActor(BattleResolver.StartBattle(new[]{
            new UnitState(A,Side.West,UnitProfile.HumanHealerTI,new GridPosition(5,8),Facing.East,hp:10),
            new UnitState(T,Side.West,UnitProfile.HumanWarriorTI,new GridPosition(x,8),Facing.East,hp:10),
            new UnitState(new UnitId(3),Side.East,UnitProfile.HumanWarriorTI,new GridPosition(19,8),Facing.West)},2,board??new Battlefield(23,17)).State,A);
        [TestCase(8,2)][TestCase(10,4)][TestCase(11,-1)]
        public void PurePlanExactLimitAndRealMoveCast(int x,int steps)
        {
            var s=State(x);string hash=BattleStateHash.Compute(s);var p=HealApproachPreview.Query(s,A,T);
            Assert.That(BattleStateHash.Compute(s),Is.EqualTo(hash));
            if(steps<0){Assert.That(p,Is.Null);return;}
            Assert.That(p.Movement.Path.Count,Is.EqualTo(steps));Assert.That(p.Movement.Path,Is.EqualTo(HealApproachPreview.Query(s,A,T).Movement.Path));
            var m=BattleResolver.Apply(s,p.Movement);Assert.That(m.IsApplied,Is.True);Assert.That(m.State.FindUnit(T).Hp,Is.EqualTo(10));Assert.That(m.State.FindUnit(A).CloseHealUsed,Is.Zero);
            var cast=BattleResolver.Apply(m.State,p.Heal);Assert.That(cast.IsApplied,Is.True);Assert.That(cast.State.FindUnit(T).Hp,Is.EqualTo(24));Assert.That(cast.State.FindUnit(A).CloseHealUsed,Is.EqualTo(1));Assert.That(cast.State.FindUnit(A).MovementRemaining,Is.EqualTo(4-steps));
        }
        [TestCase("action")][TestCase("budget")][TestCase("silence")][TestCase("exhausted")][TestCase("frozen")][TestCase("movement")][TestCase("full")]
        public void BlockedPlanNeverMutates(string blocker)
        {
            var s=State();var a=s.FindUnit(A);
            switch(blocker){case "action":a.ActionAvailable=false;break;case "budget":a.CloseHealUsed=3;break;case "silence":a.IsSilenced=true;break;case "exhausted":a.ExhaustedActivations=1;break;case "frozen":a.FrozenActivations=1;break;case "movement":a.MovementRemaining=3;break;case "full":s.FindUnit(T).Hp=40;break;}
            string hash=BattleStateHash.Compute(s);Assert.That(HealApproachPreview.Query(s,A,T),Is.Null);Assert.That(BattleStateHash.Compute(s),Is.EqualTo(hash));
        }
        [Test] public void AdjacentAndSelfRemainDirectAndSealedRecipientIsUnreachable()
        {
            var s=State(6);Assert.That(HealApproachPreview.Query(s,A,T),Is.Null);
            foreach(var id in new[]{A,T})Assert.That(BattleResolver.Apply(s,new CastCommand(A,SpellId.CloseHeal,s.FindUnit(id).Position)).IsApplied,Is.True);
            var walls=from x in Enumerable.Range(9,3) from y in Enumerable.Range(7,3) where x!=10||y!=8 select new GridPosition(x,y);
            s=State(10,new Battlefield(23,17,walls));string hash=BattleStateHash.Compute(s);Assert.That(HealApproachPreview.Query(s,A,T),Is.Null);Assert.That(BattleStateHash.Compute(s),Is.EqualTo(hash));
        }
        [Test] public void DeathOrChangedTargetAfterMovementCannotCreateAFreeHeal()
        {
            var s=State(8);var p=HealApproachPreview.Query(s,A,T);var m=BattleResolver.Apply(s,p.Movement).State;
            m.FindUnit(T).Hp=0;m.FindUnit(T).Status=UnitStatus.Dead;string hash=BattleStateHash.Compute(m);
            Assert.That(BattleResolver.Apply(m,p.Heal).IsApplied,Is.False);Assert.That(BattleStateHash.Compute(m),Is.EqualTo(hash));Assert.That(m.FindUnit(A).MovementRemaining,Is.EqualTo(2));
        }
        [Test] public void ApproachOaMayKillCasterBeforeAnyHealAndReplayStaysExact()
        {
            BattleState s=null;HealApproachPreview p=null;BattleResult moved=null;
            for(uint seed=1;seed<100;seed++){
                s=BattleTestFixtures.ToActor(BattleResolver.StartBattle(new[]{
                    new UnitState(A,Side.West,UnitProfile.HumanHealerTI,new GridPosition(5,8),Facing.East,hp:1),
                    new UnitState(T,Side.West,UnitProfile.HumanWarriorTI,new GridPosition(10,8),Facing.East,hp:10),
                    new UnitState(new UnitId(3),Side.East,UnitProfile.ElfWarriorTI,new GridPosition(4,8),Facing.East)},seed,new Battlefield(23,17)).State,A);
                p=HealApproachPreview.Query(s,A,T);Assert.That(p,Is.Not.Null);Assert.That(p.Risk.Exposures.Any(e=>e.Threats.Any(t=>t.WouldReact)),Is.True);
                moved=BattleResolver.Apply(s,p.Movement);if(!moved.State.FindUnit(A).IsActive)break;
            }
            Assert.That(moved.State.FindUnit(A).IsActive,Is.False);Assert.That(moved.State.FindUnit(T).Hp,Is.EqualTo(10));Assert.That(moved.State.FindUnit(A).CloseHealUsed,Is.Zero);
            Assert.That(BattleResolver.Apply(moved.State,p.Heal).IsApplied,Is.False);
            Assert.That(BattleStateHash.Compute(BattleResolver.Apply(s,p.Movement).State),Is.EqualTo(BattleStateHash.Compute(moved.State)));
        }
    }
}

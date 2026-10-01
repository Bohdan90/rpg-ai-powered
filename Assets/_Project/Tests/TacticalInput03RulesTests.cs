using System.Linq;
using NUnit.Framework;
using RPG.Core;
namespace RPG.Tests
{
    public class TacticalInput03RulesTests
    {
        [TestCase(1,0)][TestCase(2,0)][TestCase(3,0)][TestCase(3,2)]
        public void PostCastMovementUsesRemainingBudgetAndRecordedRules(int version,int spent)
        {
            var id=new UnitId(1);
            var s=BattleTestFixtures.ToActor(BattleResolver.StartBattle(new[]{
                BattleTestFixtures.Unit(1,UnitProfile.FireMageTI,x:8,y:8),
                BattleTestFixtures.Unit(2,UnitProfile.HumanWarriorTI,Side.East,18,8)
            },2,new Battlefield(23,17),version).State,id);
            var j=new BattleJournal(s,"input03-rules","test");
            if(spent>0)Assert.That(j.Apply(new MoveCommand(id,Enumerable.Range(1,spent).Select(i=>new GridPosition(8,8+i)))).IsApplied,Is.True);
            int remaining=j.State.FindUnit(id).MovementRemaining;
            Assert.That(j.Apply(new CastCommand(id,SpellId.FireArmor,j.State.FindUnit(id).Position)).IsApplied,Is.True);
            var actor=j.State.FindUnit(id);Assert.That(actor.ActionAvailable,Is.False);Assert.That(actor.IsExhausted,Is.True);
            Assert.That(actor.MovementRemaining,Is.EqualTo(remaining));
            var move=new MoveCommand(id,new[]{new GridPosition(7,8+spent)});
            var result=j.Apply(move);Assert.That(result.IsApplied,Is.EqualTo(version>=3));
            if(version>=3){Assert.That(j.State.FindUnit(id).MovementRemaining,Is.EqualTo(remaining-1));Assert.That(j.State.FindUnit(id).ActionAvailable,Is.False);}
            Assert.That(j.Apply(new CastCommand(id,SpellId.FireStream,new GridPosition(9,8+spent))).IsApplied,Is.False);
            Assert.That(ReplayVerification.Verify(j.Header,j.Records,j.Footer()).Matches,Is.True);
            Assert.That(BattleStateHash.Compute(ReplaySnapshot.Capture(j.State).Restore()),Is.EqualTo(BattleStateHash.Compute(j.State)));
        }
        [Test] public void FreezeStillBlocksPostCastMovementAndInvalidCastDoesNotUnlockIt()
        {
            var id=new UnitId(1);var s=BattleTestFixtures.Duel(UnitProfile.FireMageTI,targetX:3);
            var hash=BattleStateHash.Compute(s);
            Assert.That(BattleResolver.Apply(s,new CastCommand(id,SpellId.Fireball,new GridPosition(3,0))).IsApplied,Is.False);
            Assert.That(s.FindUnit(id).PostSpellMovement,Is.False);Assert.That(BattleStateHash.Compute(s),Is.EqualTo(hash));
            s=BattleResolver.Apply(s,new CastCommand(id,SpellId.FireArmor,s.FindUnit(id).Position)).State;
            s.FindUnit(id).FrozenActivations=1;
            Assert.That(BattleResolver.Validate(s,new MoveCommand(id,new[]{new GridPosition(0,1)})),Is.EqualTo(CommandError.Frozen));
            Assert.That(s.FindUnit(id).CanMove,Is.False);
        }
    }
}

using System.Linq;
using NUnit.Framework;
using RPG.Core;
namespace RPG.Tests
{
    public class MeleeApproachTests
    {
        static readonly UnitId A=new UnitId(1),B=new UnitId(2);
        static BattleState State(int x=12,Battlefield board=null,UnitProfile profile=null)=>BattleTestFixtures.ToActor(BattleResolver.StartBattle(new[]{
            BattleTestFixtures.Unit(1,profile??UnitProfile.HumanWarriorTI,x:8,y:8),
            BattleTestFixtures.Unit(2,UnitProfile.HumanArcherTI,Side.East,x,8)},2,board??new Battlefield(23,17)).State,A);
        [TestCase(12,true)][TestCase(13,true)][TestCase(14,false)]
        public void ApproachRespectsRemainingMovementAndDoesNotMutate(int x,bool found)
        {
            var s=State(x);string hash=BattleStateHash.Compute(s);var p=MeleeApproachPreview.Query(s,A,B);
            Assert.That(p!=null,Is.EqualTo(found));Assert.That(BattleStateHash.Compute(s),Is.EqualTo(hash));
            if(p==null)return;
            Assert.That(p.Movement.Path,Is.EqualTo(MeleeApproachPreview.Query(s,A,B).Movement.Path));
            var moved=BattleResolver.Apply(s,p.Movement);Assert.That(moved.IsApplied,Is.True);Assert.That(moved.State.FindUnit(A).ActionAvailable,Is.True);
            Assert.That(BattleResolver.PreviewAttack(moved.State,p.Attack).ContactChance,Is.EqualTo(p.OnArrival.ContactChance));
            Assert.That(BattleResolver.Apply(moved.State,p.Attack).IsApplied,Is.True);
        }
        [Test] public void WallsCornerGeometryAndRetreatCellsAreNeverBypassed()
        {
            var wall=Enumerable.Range(1,15).Select(y=>new GridPosition(10,y));var s=State(12,new Battlefield(23,17,wall));Assert.That(MeleeApproachPreview.Query(s,A,B),Is.Null);
            var sealedTarget=from x in Enumerable.Range(11,3) from y in Enumerable.Range(7,3) where x!=12||y!=8 select new GridPosition(x,y);
            s=State(12,new Battlefield(23,17,sealedTarget));Assert.That(MeleeApproachPreview.Query(s,A,B),Is.Null);
            s=State(12,new Battlefield(23,17,new[]{new GridPosition(10,8)}));var p=MeleeApproachPreview.Query(s,A,B);Assert.That(p,Is.Not.Null);
            var at=s.FindUnit(A).Position;foreach(var cell in p.Movement.Path){Assert.That(MovementRules.ValidateStep(s,A,at,cell),Is.EqualTo(CommandError.None));Assert.That(s.Battlefield.IsRetreatZone(s.FindUnit(A),cell),Is.False);at=cell;}
        }
        [Test] public void NoAutomaticApproachForMagesArchersSpentActionOrAlreadyLegalContact()
        {
            foreach(var profile in new[]{UnitProfile.FireMageTI,UnitProfile.HumanArcherTI})Assert.That(MeleeApproachPreview.Query(State(profile:profile),A,B),Is.Null);
            var s=State();s.FindUnit(A).ActionAvailable=false;Assert.That(MeleeApproachPreview.Query(s,A,B),Is.Null);
            Assert.That(MeleeApproachPreview.Query(State(9),A,B),Is.Null);
        }
    }
}

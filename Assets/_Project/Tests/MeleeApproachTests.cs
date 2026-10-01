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
        [TestCase(1,0)][TestCase(-1,0)][TestCase(0,1)][TestCase(0,-1)]
        [TestCase(1,1)][TestCase(-1,1)][TestCase(1,-1)][TestCase(-1,-1)]
        public void EqualCostUnobstructedApproachKeepsTheDirectLine(int dx,int dy)
        {
            var s=BattleTestFixtures.ToActor(BattleResolver.StartBattle(new[]{BattleTestFixtures.Unit(1,UnitProfile.HumanWarriorTI,x:8,y:8),BattleTestFixtures.Unit(2,UnitProfile.HumanArcherTI,Side.East,8+4*dx,8+4*dy)},2,new Battlefield(23,17)).State,A);
            var p=MeleeApproachPreview.Query(s,A,B);Assert.That(p,Is.Not.Null);
            Assert.That(p.Movement.Path,Is.EqualTo(Enumerable.Range(1,3).Select(i=>new GridPosition(8+i*dx,8+i*dy))));
        }
        [Test] public void WallsCornerGeometryAndRetreatCellsAreNeverBypassed()
        {
            var wall=Enumerable.Range(1,15).Select(y=>new GridPosition(10,y));var s=State(12,new Battlefield(23,17,wall));Assert.That(MeleeApproachPreview.Query(s,A,B),Is.Null);
            var sealedTarget=from x in Enumerable.Range(11,3) from y in Enumerable.Range(7,3) where x!=12||y!=8 select new GridPosition(x,y);
            s=State(12,new Battlefield(23,17,sealedTarget));Assert.That(MeleeApproachPreview.Query(s,A,B),Is.Null);
            s=State(12,new Battlefield(23,17,new[]{new GridPosition(10,8)}));var p=MeleeApproachPreview.Query(s,A,B);Assert.That(p,Is.Not.Null);
            var at=s.FindUnit(A).Position;foreach(var cell in p.Movement.Path){Assert.That(MovementRules.ValidateStep(s,A,at,cell),Is.EqualTo(CommandError.None));Assert.That(s.Battlefield.IsRetreatZone(s.FindUnit(A),cell),Is.False);at=cell;}
        }
        static BattleState BowState(int x=15,Battlefield board=null)=>BattleTestFixtures.ToActor(BattleResolver.StartBattle(new[]{
            BattleTestFixtures.Unit(1,UnitProfile.HumanArcherTI,x:2,y:8),
            BattleTestFixtures.Unit(2,UnitProfile.HumanWarriorTI,Side.East,x,8)},2,board??new Battlefield(23,17)).State,A);
        [TestCase(15,3)][TestCase(16,4)][TestCase(17,-1)]
        public void BowApproachUsesMinimumMovementForRangeAndActualMovingAccuracy(int x,int steps)
        {
            var s=BowState(x);string hash=BattleStateHash.Compute(s);var p=MeleeApproachPreview.QueryBow(s,A,B);
            Assert.That(BattleStateHash.Compute(s),Is.EqualTo(hash));
            if(steps<0){Assert.That(p,Is.Null);return;}
            Assert.That(p.Movement.Path.Count,Is.EqualTo(steps));Assert.That(p.Movement.Path.Last(),Is.EqualTo(new GridPosition(x-10,8)));
            Assert.That(p.OnArrival.Distance,Is.EqualTo(10));Assert.That(p.OnArrival.SteadyAim,Is.False);Assert.That(p.OnArrival.AimModifier,Is.Zero);
            Assert.That(p.Movement.Path,Is.EqualTo(MeleeApproachPreview.QueryBow(s,A,B).Movement.Path));
            var moved=BattleResolver.Apply(s,p.Movement);Assert.That(moved.IsApplied,Is.True);
            var actual=BattleResolver.PreviewAttack(moved.State,p.Attack);Assert.That(actual.IsLegal,Is.True);Assert.That(actual.ContactChance,Is.EqualTo(p.OnArrival.ContactChance));
            Assert.That(BattleResolver.Apply(moved.State,p.Attack).IsApplied,Is.True);
        }
        [Test] public void BowApproachRequiresClearLineAndDoesNotWalkThroughWalls()
        {
            var s=BowState(15,new Battlefield(23,17,new[]{new GridPosition(8,8)}));
            var p=MeleeApproachPreview.QueryBow(s,A,B);Assert.That(p,Is.Not.Null);
            var moved=BattleResolver.Apply(s,p.Movement);Assert.That(moved.IsApplied,Is.True);Assert.That(BattleResolver.PreviewAttack(moved.State,p.Attack).IsLegal,Is.True);
            s=BowState(15,new Battlefield(23,17,Enumerable.Range(0,17).Select(y=>new GridPosition(8,y))));
            string hash=BattleStateHash.Compute(s);Assert.That(MeleeApproachPreview.QueryBow(s,A,B),Is.Null);Assert.That(BattleStateHash.Compute(s),Is.EqualTo(hash));
        }
        [Test] public void BowApproachDoesNotReplaceExistingShotOrBypassSpentActionMovementOrEngagement()
        {
            Assert.That(MeleeApproachPreview.QueryBow(BowState(12),A,B),Is.Null);
            var s=BowState();s.FindUnit(A).ActionAvailable=false;Assert.That(MeleeApproachPreview.QueryBow(s,A,B),Is.Null);
            s=BowState();s.FindUnit(A).MovementRemaining=0;Assert.That(MeleeApproachPreview.QueryBow(s,A,B),Is.Null);
            s=BattleTestFixtures.ToActor(BattleResolver.StartBattle(new[]{BattleTestFixtures.Unit(1,UnitProfile.HumanArcherTI,x:2,y:8),BattleTestFixtures.Unit(2,UnitProfile.HumanWarriorTI,Side.East,15,8),BattleTestFixtures.Unit(3,UnitProfile.HumanWarriorTI,Side.East,1,8)},2,new Battlefield(23,17)).State,A);
            Assert.That(BattleResolver.IsArcherEngaged(s,A),Is.True);Assert.That(MeleeApproachPreview.QueryBow(s,A,B),Is.Null);
            Assert.That(MeleeApproachPreview.QueryBow(State(),A,B),Is.Null);
        }
        [Test] public void NoAutomaticApproachForMagesArchersSpentActionOrAlreadyLegalContact()
        {
            foreach(var profile in new[]{UnitProfile.FireMageTI,UnitProfile.HumanArcherTI})Assert.That(MeleeApproachPreview.Query(State(profile:profile),A,B),Is.Null);
            var s=State();s.FindUnit(A).ActionAvailable=false;Assert.That(MeleeApproachPreview.Query(s,A,B),Is.Null);
            Assert.That(MeleeApproachPreview.Query(State(9),A,B),Is.Null);
        }
    }
}

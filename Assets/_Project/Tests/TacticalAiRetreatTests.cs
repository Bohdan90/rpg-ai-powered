using System.Linq;
using NUnit.Framework;
using RPG.Core;

namespace RPG.Tests
{
    public class TacticalAiRetreatTests
    {
        private static readonly UnitId One=new UnitId(1);
        private static BattleState State(int x,int hp=8,Battlefield board=null)=>BattleTestFixtures.ToActor(BattleResolver.StartBattle(new[]{
            BattleTestFixtures.Unit(1,UnitProfile.HumanWarriorTI,x:x,y:4,hp:hp),
            BattleTestFixtures.Unit(2,UnitProfile.HumanWarriorTI,Side.East,11,4)
        },2,board??new Battlefield(13,9)).State,One);
        [Test] public void AdjacentWoundedWarriorEntersLegalExit()
        {
            var s=State(1);var d=TacticalAi.Choose(s);var r=BattleResolver.Apply(s,d.Command);
            Assert.That(r.IsApplied,Is.True);Assert.That(r.State.FindUnit(One).Status,Is.EqualTo(UnitStatus.Escaped));
        }
        [Test] public void OneActivationShortMakesFullProgressAndExitsNextActivation()
        {
            var s=State(5);var journal=new BattleJournal(s,"WP03E-short","regression");
            var d=TacticalAi.Choose(s);TestContext.WriteLine(d.Explanation);
            Assert.That(journal.Apply(d.Command).IsApplied,Is.True);
            var u=journal.State.FindUnit(One);Assert.That(u.Position.X,Is.EqualTo(1));Assert.That(u.MovementRemaining,Is.Zero);
            Assert.That(journal.Apply(TacticalAi.Choose(journal.State).Command).IsApplied,Is.True);
            while(journal.State.CurrentUnitId!=One)journal.Apply(new EndActivationCommand(journal.State.CurrentUnitId.Value));
            Assert.That(journal.Apply(TacticalAi.Choose(journal.State).Command).IsApplied,Is.True);
            Assert.That(journal.State.FindUnit(One).Status,Is.EqualTo(UnitStatus.Escaped));
            Assert.That(ReplayVerification.Verify(journal.Header,journal.Records,journal.Footer()).Matches,Is.True);
        }
        [Test] public void WarriorDetoursAroundWallWithoutRepeatedWaitingOrOscillation()
        {
            var wall=Enumerable.Range(1,7).Select(y=>new GridPosition(6,y));
            var s=BattleTestFixtures.ToActor(BattleResolver.StartBattle(new[]{
                BattleTestFixtures.Unit(1,UnitProfile.HumanWarriorTI,x:5,y:4),
                BattleTestFixtures.Unit(2,UnitProfile.HumanWarriorTI,Side.East,7,4)
            },2,new Battlefield(13,9,wall)).State,One);
            var journal=new BattleJournal(s,"WP03E-wall","regression");bool attack=false;
            for(int i=0;i<24&&!journal.State.Outcome.IsEnded;i++)
            {
                var d=journal.State.CurrentUnitId==One?TacticalAi.Choose(journal.State):null;
                if(d!=null){TestContext.WriteLine("R"+journal.State.Round+" "+d.Explanation);if(d.Command is BasicAttackCommand){attack=true;break;}}
                Assert.That(journal.Apply(d?.Command??new EndActivationCommand(journal.State.CurrentUnitId.Value)).IsApplied,Is.True);
            }
            Assert.That(attack,Is.True,"A legal safe detour exists; waiting behind this wall is not useful cover.");
            Assert.That(ReplayVerification.Verify(journal.Header,journal.Records,journal.Footer()).Matches,Is.True);
        }
        [Test] public void ExhaustedAdjacentUnitEndsThenEscapesWithRefreshedMovement()
        {
            var s=State(1);s.FindUnit(One).MovementRemaining=0;s.FindUnit(One).MovementSpentThisActivation=4;
            var j=new BattleJournal(s,"WP03E-exhausted","regression");string before=BattleStateHash.Compute(s);
            var a=TacticalAi.Choose(s);var b=TacticalAi.Choose(s);
            Assert.That(a.Command,Is.TypeOf<EndActivationCommand>());Assert.That(a.Explanation,Is.EqualTo(b.Explanation));
            Assert.That(BattleStateHash.Compute(s),Is.EqualTo(before));j.Apply(a.Command);
            while(j.State.CurrentUnitId!=One)j.Apply(new EndActivationCommand(j.State.CurrentUnitId.Value));
            j.Apply(TacticalAi.Choose(j.State).Command);Assert.That(j.State.FindUnit(One).Status,Is.EqualTo(UnitStatus.Escaped));
            Assert.That(ReplayVerification.Verify(j.Header,j.Records,j.Footer()).Matches,Is.True);
        }
        [Test] public void AnotherUnitBlockingPreviouslySelectedExitCausesLegalAlternate()
        {
            var blocker=new UnitId(3);
            var s=BattleTestFixtures.ToActor(BattleResolver.StartBattle(new[]{
                BattleTestFixtures.Unit(1,UnitProfile.HumanWarriorTI,x:1,y:4,hp:8),
                BattleTestFixtures.Unit(2,UnitProfile.HumanWarriorTI,Side.East,11,4),
                BattleTestFixtures.Unit(3,UnitProfile.HumanArcherTI,Side.East,2,7)
            },2,new Battlefield(13,9)).State,One);
            var first=(MoveCommand)TacticalAi.Choose(s).Command;var target=first.Path.Last();
            s=BattleResolver.Apply(s,new EndActivationCommand(One)).State;s=BattleTestFixtures.ToActor(s,blocker);
            var path=Pathfinder.FindPath(s,blocker,target);Assert.That(path.Found,Is.True);
            s=BattleResolver.Apply(s,new MoveCommand(blocker,path.Steps)).State;
            s=BattleResolver.Apply(s,new EndActivationCommand(blocker)).State;s=BattleTestFixtures.ToActor(s,One);
            string before=BattleStateHash.Compute(s);var next=(MoveCommand)TacticalAi.Choose(s).Command;
            Assert.That(next.Path.Last(),Is.Not.EqualTo(target));Assert.That(BattleStateHash.Compute(s),Is.EqualTo(before));
            Assert.That(BattleResolver.Validate(s,next),Is.EqualTo(CommandError.None));
            Assert.That(BattleResolver.Apply(s,next).State.FindUnit(One).Status,Is.EqualTo(UnitStatus.Escaped));
        }
        [Test] public void SealedExitCannotTeleportAndQueriesRemainPure()
        {
            var s=State(4,8,new Battlefield(13,9,Enumerable.Range(0,9).Select(y=>new GridPosition(0,y))));
            string before=BattleStateHash.Compute(s);var d=TacticalAi.Choose(s);
            Assert.That(BattleStateHash.Compute(s),Is.EqualTo(before));Assert.That(BattleResolver.Validate(s,d.Command),Is.EqualTo(CommandError.None));
            var r=BattleResolver.Apply(s,d.Command);Assert.That(r.State.FindUnit(One).Status,Is.EqualTo(UnitStatus.Active));
            if(d.Command is MoveCommand m)Assert.That(m.Path.Count,Is.LessThanOrEqualTo(4));
        }
        [Test] public void UsefulAttackFromWallCoverIsNotReplacedByACharge()
        {
            var s=BattleTestFixtures.ToActor(BattleResolver.StartBattle(new[]{
                BattleTestFixtures.Unit(1,UnitProfile.HumanArcherTI,x:4,y:4),
                BattleTestFixtures.Unit(2,UnitProfile.HumanWarriorTI,Side.East,8,4),
                BattleTestFixtures.Unit(3,UnitProfile.HumanArcherTI,Side.East,4,8,hp:3,armor:0)
            },2,new Battlefield(13,9,Enumerable.Range(1,7).Select(y=>new GridPosition(5,y)))).State,One);
            var d=TacticalAi.Choose(s);Assert.That(d.Command,Is.TypeOf<BasicAttackCommand>());
            Assert.That(((BasicAttackCommand)d.Command).Target,Is.EqualTo(new UnitId(3)));
            Assert.That(BattleResolver.Validate(s,d.Command),Is.EqualTo(CommandError.None));
        }
        [Test] public void WoundedWarriorBehindWallTakesLegalDetourThenEvacuates()
        {
            var s=State(5,8,new Battlefield(13,9,Enumerable.Range(1,7).Select(y=>new GridPosition(3,y))));
            var j=new BattleJournal(s,"WP03E-retreat-wall","regression");int decisions=0;
            while(!j.State.Outcome.IsEnded&&decisions++<24)
            {
                var d=j.State.CurrentUnitId==One?TacticalAi.Choose(j.State):null;
                if(d!=null)
                {
                    string before=BattleStateHash.Compute(j.State);var repeated=TacticalAi.Choose(j.State);
                    Assert.That(repeated.Explanation,Is.EqualTo(d.Explanation));Assert.That(BattleStateHash.Compute(j.State),Is.EqualTo(before));
                    TestContext.WriteLine("R"+j.State.Round+" "+d.Explanation);
                }
                Assert.That(j.Apply(d?.Command??new EndActivationCommand(j.State.CurrentUnitId.Value)).IsApplied,Is.True);
            }
            Assert.That(j.State.FindUnit(One).Status,Is.EqualTo(UnitStatus.Escaped));
            Assert.That(ReplayVerification.Verify(j.Header,j.Records,j.Footer()).Matches,Is.True);
        }
    }
}

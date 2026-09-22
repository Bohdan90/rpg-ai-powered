using System.Linq;
using NUnit.Framework;
using RPG.Core;

namespace RPG.Tests
{
    public class TacticalAiTests
    {
        private static BattleState State(UnitState a,UnitState b)=>BattleTestFixtures.ToActor(BattleResolver.StartBattle(new[]{a,b},2,new Battlefield(23,17)).State,a.Id);
        [Test]
        public void EvaluationIsPureDeterministicAndChoosesLegalKill()
        {
            var s=State(BattleTestFixtures.Unit(1,UnitProfile.HumanArcherTI,x:4,y:4),BattleTestFixtures.Unit(2,UnitProfile.HumanArcherTI,Side.East,8,4,hp:3,armor:0));
            string before=BattleTestFixtures.Snapshot(s);var a=TacticalAi.Choose(s);var b=TacticalAi.Choose(s);
            Assert.That(a.Command,Is.TypeOf<BasicAttackCommand>());Assert.That(((BasicAttackCommand)a.Command).Target,Is.EqualTo(new UnitId(2)));
            Assert.That(a.Explanation,Is.EqualTo(b.Explanation));Assert.That(a.Score,Is.EqualTo(b.Score));
            Assert.That(BattleTestFixtures.Snapshot(s),Is.EqualTo(before));Assert.That(BattleResolver.Validate(s,a.Command),Is.EqualTo(CommandError.None));
        }
        [Test]
        public void EngagedArcherUsesMeleeStrikeAndDoesNotInventBowOrOa()
        {
            var s=State(BattleTestFixtures.Unit(1,UnitProfile.HumanArcherTI,x:4,y:4),BattleTestFixtures.Unit(2,UnitProfile.HumanWarriorTI,Side.East,5,4,hp:1,armor:0));
            var d=TacticalAi.Choose(s);Assert.That(d.Command,Is.TypeOf<BasicAttackCommand>());
            Assert.That(((BasicAttackCommand)d.Command).Kind,Is.EqualTo(BasicAttackKind.MeleeStrike));
            Assert.That(s.FindUnit(new UnitId(1)).OpportunityAttackAvailable,Is.False);
        }
        [Test]
        public void WoundedUnitPrefersReachablePhysicalRetreat()
        {
            var s=State(BattleTestFixtures.Unit(1,UnitProfile.ElfWarriorTI,x:2,y:5,hp:8),BattleTestFixtures.Unit(2,UnitProfile.HumanWarriorTI,Side.East,15,5));
            var d=TacticalAi.Choose(s);Assert.That(d.Command,Is.TypeOf<MoveCommand>());
            var result=BattleResolver.Apply(s,d.Command);Assert.That(result.State.FindUnit(new UnitId(1)).Status,Is.EqualTo(UnitStatus.Escaped));
        }
        [Test]
        public void LethalOaExitLosesToZeroExposureRetreat()
        {
            var s=State(BattleTestFixtures.Unit(1,UnitProfile.ElfWarriorTI,x:1,y:4,hp:1,armor:0),BattleTestFixtures.Unit(2,UnitProfile.HumanWarriorTI,Side.East,1,5));
            var d=TacticalAi.Choose(s);var move=(MoveCommand)d.Command;
            Assert.That(move.Path.Last(),Is.EqualTo(new GridPosition(0,4)));
            Assert.That(BattleResolver.Apply(s,move).State.FindUnit(new UnitId(1)).Status,Is.EqualTo(UnitStatus.Escaped));
        }
        [Test]
        public void AfterMovementPolicyNeverDefends()
        {
            var s=State(BattleTestFixtures.Unit(1,UnitProfile.HumanWarriorTI,x:4,y:4),BattleTestFixtures.Unit(2,UnitProfile.HumanWarriorTI,Side.East,15,4));
            s=BattleResolver.Apply(s,new MoveCommand(new UnitId(1),new[]{new GridPosition(5,4)})).State;
            var d=TacticalAi.Choose(s);Assert.That(d.Command,Is.Not.TypeOf<DefendCommand>());Assert.That(BattleResolver.Validate(s,d.Command),Is.EqualTo(CommandError.None));
        }
        [Test]
        public void SafePathSearchRetainsZeroLossDetourAroundAnEnemy()
        {
            var s=State(BattleTestFixtures.Unit(1,UnitProfile.ElfWarriorTI,x:2,y:4,armor:0),BattleTestFixtures.Unit(2,UnitProfile.HumanWarriorTI,Side.East,4,5));
            var u=s.FindUnit(new UnitId(1));var destination=new GridPosition(6,4);
            var shortest=TacticalAiPaths.Start(u);
            foreach(var p in Pathfinder.FindPath(s,u.Id,destination).Steps)shortest=TacticalAiPaths.Extend(s,u,shortest,p);
            var safe=TacticalAiPaths.SafeRoutes(s,u).Single(r=>r.Position==destination);
            Assert.That(shortest.HpLoss(u),Is.GreaterThan(0));Assert.That(safe.HpLoss(u),Is.EqualTo(0));
            Assert.That(BattleResolver.Validate(s,new MoveCommand(u.Id,safe.Steps)),Is.EqualTo(CommandError.None));
        }
        [Test]
        public void DefendCandidateCanWinButSpentActionEnds()
        {
            var s=State(BattleTestFixtures.Unit(1,UnitProfile.HumanWarriorTI,x:4,y:4,armor:0),BattleTestFixtures.Unit(2,UnitProfile.HumanArcherTI,Side.East,9,4));
            s.FindUnit(new UnitId(1)).MovementRemaining=0;
            var d=TacticalAi.Choose(s);Assert.That(d.Command,Is.TypeOf<DefendCommand>());
            s=BattleResolver.Apply(s,d.Command).State;Assert.That(TacticalAi.Choose(s).Command,Is.TypeOf<EndActivationCommand>());
        }
        [TestCase(SizeExperimentMap.Field_23x17_Full_9v9)]
        [TestCase(SizeExperimentMap.Siege_35x27_Full_9v9)]
        public void MultipleActivationsUseLegalCommandsAndTerminate(SizeExperimentMap map)
        {
            var s=BattleResolver.StartBattle(SizeExperimentFixture.Units(map),20260921,SizeExperimentFixture.Board(map)).State;int ended=0;
            for(int i=0;i<40&&!s.Outcome.IsEnded&&ended<6;i++)
            {
                var before=s.CurrentUnitId;var d=TacticalAi.Choose(s);var result=BattleResolver.Apply(s,d.Command);
                Assert.That(result.IsApplied,Is.True,d.Explanation);s=result.State;if(s.CurrentUnitId!=before)ended++;
            }
            Assert.That(ended,Is.GreaterThanOrEqualTo(6));
        }
    }
}

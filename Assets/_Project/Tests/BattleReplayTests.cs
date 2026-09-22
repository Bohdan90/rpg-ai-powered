using System.Linq;
using NUnit.Framework;
using RPG.Core;
namespace RPG.Tests
{
    public class BattleReplayTests
    {
        private static BattleJournal Journal(UnitState a,UnitState b,uint seed=2)=>new BattleJournal(
            BattleTestFixtures.ToActor(BattleResolver.StartBattle(new[]{a,b},seed,new Battlefield(23,17)).State,a.Id),"test","test");
        private static void Matches(BattleJournal journal)
        {
            var r=ReplayVerification.Verify(journal.Header,journal.Records,journal.Footer());Assert.That(r.Matches,Is.True,r.Message);
            Assert.That(BattleTestFixtures.Snapshot(r.State),Is.EqualTo(BattleTestFixtures.Snapshot(journal.State)));
            Assert.That(r.State.RngState,Is.EqualTo(journal.State.RngState));
            Assert.That(r.State.Outcome.Reason,Is.EqualTo(journal.State.Outcome.Reason));
            // Independently inspect every successful-command hash, not only the final one.
            var state=journal.Header.initial.Restore();
            foreach(var record in journal.Records){state=BattleResolver.Apply(state,record.command.Restore()).State;Assert.That(BattleStateHash.Compute(state),Is.EqualTo(record.stateHash));}
        }
        [Test]
        public void AttackEliminationPoolsEventsAndRngReplay()
        {
            var j=Journal(BattleTestFixtures.Unit(1,UnitProfile.HumanArcherTI,x:4,y:4),BattleTestFixtures.Unit(2,UnitProfile.HumanArcherTI,Side.East,6,4,hp:1,armor:0));
            for(int i=0;i<20&&!j.State.Outcome.IsEnded;i++)
            {
                var id=j.State.CurrentUnitId.Value;
                if(id.Value==1&&j.State.FindUnit(id).ActionAvailable)j.Apply(new BasicAttackCommand(id,new UnitId(2)));
                else j.Apply(new EndActivationCommand(id));
            }
            Assert.That(j.State.Outcome.Reason,Is.EqualTo(BattleEndReason.Eliminated));Matches(j);
            Assert.That(j.Session.deaths,Is.EqualTo(1));Assert.That(j.Session.firstHpLossObserved,Is.True);
        }
        [Test]
        public void EscapeWithdrawalAndPreservedPoolsReplay()
        {
            var j=Journal(BattleTestFixtures.Unit(1,UnitProfile.ElfWarriorTI,x:1,y:4,hp:4,armor:3),BattleTestFixtures.Unit(2,UnitProfile.HumanWarriorTI,Side.East,12,4));
            j.Apply(new MoveCommand(new UnitId(1),new[]{new GridPosition(0,4)}));Matches(j);
            Assert.That(j.State.Outcome.Reason,Is.EqualTo(BattleEndReason.Withdrawal));Assert.That(j.State.FindUnit(new UnitId(1)).Armor,Is.EqualTo(3));
            Assert.That(j.Session.escapes,Is.EqualTo(1));
        }
        [Test]
        public void OaInterruptionReplaysUnexecutedStepAndDeath()
        {
            var j=Journal(BattleTestFixtures.Unit(1,UnitProfile.ElfWarriorTI,x:4,y:4,hp:1,armor:0),BattleTestFixtures.Unit(2,UnitProfile.HumanWarriorTI,Side.East,5,4));
            j.Apply(new MoveCommand(new UnitId(1),new[]{new GridPosition(3,4),new GridPosition(2,4)}));Matches(j);
            Assert.That(j.Records[0].events.Any(e=>e.kind==nameof(BattleEventKind.MovementInterruptedByDeath)),Is.True);
            Assert.That(j.State.FindUnit(new UnitId(1)).Position,Is.EqualTo(new GridPosition(4,4)));Assert.That(j.Session.oaTriggers,Is.EqualTo(1));
        }
        [Test]
        public void InvalidAndPreviewDoNotAdvanceSuccessfulSequenceOrRng()
        {
            var j=Journal(BattleTestFixtures.Unit(1,UnitProfile.HumanArcherTI,x:4,y:4),BattleTestFixtures.Unit(2,UnitProfile.HumanWarriorTI,Side.East,6,4));
            string hash=BattleStateHash.Compute(j.State);uint rng=j.State.RngState;
            BattleResolver.PreviewAttack(j.State,new BasicAttackCommand(new UnitId(1),new UnitId(2)));
            Pathfinder.FindPath(j.State,new UnitId(1),new GridPosition(4,5));
            j.Apply(new MoveCommand(new UnitId(1),new[]{new GridPosition(-1,4)}));
            Assert.That(j.Records[0].successfulSequence,Is.Zero);Assert.That(j.State.RngState,Is.EqualTo(rng));Assert.That(BattleStateHash.Compute(j.State),Is.EqualTo(hash));
            j.Apply(new DefendCommand(new UnitId(1)));Assert.That(j.Records[1].successfulSequence,Is.EqualTo(1));Matches(j);
            Assert.That(j.Session.invalidAttempts,Is.EqualTo(1));Assert.That(j.Session.defends,Is.EqualTo(1));
            var successfulOnly=j.Header.initial.Restore();
            foreach(var record in j.Records.Where(r=>r.applied))successfulOnly=BattleResolver.Apply(successfulOnly,record.command.Restore()).State;
            Assert.That(BattleStateHash.Compute(successfulOnly),Is.EqualTo(BattleStateHash.Compute(j.State)));
        }
        [Test]
        public void ReportsFirstDivergentSequenceAndRejectsTruncation()
        {
            var j=Journal(BattleTestFixtures.Unit(1,UnitProfile.HumanArcherTI,x:4,y:4),BattleTestFixtures.Unit(2,UnitProfile.HumanWarriorTI,Side.East,6,4));
            j.Apply(new DefendCommand(new UnitId(1)));j.Apply(new EndActivationCommand(new UnitId(1)));
            Assert.That(ReplayVerification.Verify(j.Header,j.Records.Take(1),j.Footer()).Matches,Is.False);
            j.Records[1].stateHash="tampered";var r=ReplayVerification.Verify(j.Header,j.Records,j.Footer());
            Assert.That(r.Matches,Is.False);Assert.That(r.DivergentSequence,Is.EqualTo(2));
        }
        [Test]
        public void RecordedRollsNeverBecomeReplayInputs()
        {
            var j=Journal(BattleTestFixtures.Unit(1,UnitProfile.HumanArcherTI,x:4,y:4),BattleTestFixtures.Unit(2,UnitProfile.HumanWarriorTI,Side.East,6,4));
            j.Apply(new BasicAttackCommand(new UnitId(1),new UnitId(2)));
            foreach(var e in j.Records[0].events)e.roll=99;Matches(j);
        }
        [TestCase(SizeExperimentMap.Field_23x17_Full_9v9)]
        [TestCase(SizeExperimentMap.Siege_35x27_Full_9v9)]
        public void AiCommandSequenceReplaysEveryHash(SizeExperimentMap map)
        {
            var j=new BattleJournal(BattleResolver.StartBattle(SizeExperimentFixture.Units(map),20260921,SizeExperimentFixture.Board(map)).State,map.ToString(),"test");
            for(int i=0;i<12&&!j.State.Outcome.IsEnded;i++)Assert.That(j.Apply(TacticalAi.Choose(j.State).Command,"AI").IsApplied,Is.True);
            Matches(j);
        }
        [Test]
        public void SnapshotRestoresNondefaultActivationDefendAndOaAvailability()
        {
            var j=Journal(BattleTestFixtures.Unit(1,UnitProfile.HumanWarriorTI,x:4,y:4),BattleTestFixtures.Unit(2,UnitProfile.HumanWarriorTI,Side.East,6,4));
            j.Apply(new DefendCommand(new UnitId(1)));j.State.FindUnit(new UnitId(1)).OpportunityAttackAvailable=false;
            var next=new BattleJournal(j.State,"mid-state","test");next.Apply(new EndActivationCommand(new UnitId(1),Facing.South));Matches(next);
        }
    }
}

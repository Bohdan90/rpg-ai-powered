using System.IO;
using NUnit.Framework;
using RPG.Core;

namespace RPG.Tests
{
    public class ProvisionsTuningTests
    {
        [Test] public void ThirtyStartsAndConsumptionUsesOnlyLivingFiguresWithoutRetroactiveLoss()
        {
            var s=new StrategicScenario();Assert.That(s.Provisions,Is.EqualTo(30));Assert.That(StrategicScenario.MaxProvisions,Is.EqualTo(30));
            s.Move(3);s.Move(8);var final=StrategicScenarioTests.Finish(s);
            final.FindUnit(new UnitId(2)).Hp=0;final.FindUnit(new UnitId(2)).Status=UnitStatus.Dead;
            s.ResolveBattle(final);Assert.That(s.Provisions,Is.EqualTo(30));Assert.That(s.Consumption,Is.EqualTo(5));
            Assert.That(s.ResolveBattle(final),Is.False);Assert.That(s.Provisions,Is.EqualTo(30));
            s.EndActivation();Assert.That(s.Provisions,Is.EqualTo(25));
            var copy=s.CaptureSave().Restore();s.EndActivation();copy.EndActivation();
            Assert.That(s.Provisions,Is.EqualTo(20));Assert.That(copy.CaptureSave().checksum,Is.EqualTo(s.CaptureSave().checksum));
        }
        [Test] public void SixConsumedAtFieldZeroTriggersHungryWithoutHpPenalty()
        {
            var s=new StrategicScenario();s.Move(2);s.EndActivation();Assert.That(s.Provisions,Is.EqualTo(24));
            s.Provisions=6;s.EndActivation();Assert.That(s.Provisions,Is.Zero);Assert.That(s.Hungry,Is.True);
            Assert.That(s.Player.Commander.Hp,Is.EqualTo(40));Assert.That(s.Graph.Cost(2,1,true),Is.EqualTo(25));
        }
        [Test] public void PreviousTuningSaveIsRejectedWithoutChangingLiveState()
        {
            var s=new StrategicScenario();string before=s.CaptureSave().checksum;var d=s.CaptureSave();d.maxProvisions=36;d.provisions=36;d.checksum=d.ComputeHash();
            Assert.That(()=>d.Restore(),Throws.TypeOf<InvalidDataException>().With.Message.Contains("different Provisions tuning"));
            Assert.That(s.CaptureSave().checksum,Is.EqualTo(before));
        }
    }
}

using System.Linq;
using NUnit.Framework;
using RPG.Core;
namespace RPG.Tests
{
    public class CityCombatIntegrationTests
    {
        [TestCase(Side.West)][TestCase(Side.East)]
        public void ControlledMirroredMatchPaidEconomyRealBattlePersistentResultAndLegalEnding(Side first)
        {
            var w=new CrossroadsScenario(first,foundations:true,combined:true,westPreset:first==Side.West?CombatPreset.Fire:CombatPreset.Ice,eastPreset:first==Side.East?CombatPreset.Fire:CombatPreset.Ice);
            var other=CrossroadsScenario.Other(first);var f=w.Foundations;
            Assert.That(f.QueueProject(w,first,w.OwnKeep(first),CityProjectKind.DevelopmentII),Is.True);
            Assert.That(f.QueueProject(w,first,w.OwnKeep(first),CityProjectKind.MageTowerI),Is.True);
            Assert.That(f.QueueResearch(w,first,CityTech.ElementalDrills),Is.True);
            for(int i=0;i<6;i++)CityFoundationsTests.Cycle(w);
            Assert.That(f.City(first).Development,Is.EqualTo(2));Assert.That(f.City(first).MageTower,Is.EqualTo(1));Assert.That(f.Realm(first).Knows(CityTech.ElementalDrills),Is.True);
            Assert.That(w.Move(first,6),Is.True);CityFoundationsTests.End(w);
            Assert.That(w.Move(other,first==Side.West?9:4),Is.True);CityFoundationsTests.End(w);
            var loaded=w.CaptureSave().Restore();Assert.That(w.Attack(first),Is.True);Assert.That(loaded.Attack(first),Is.True);
            Assert.That(BattleStateHash.Compute(w.Encounter.Battle.State),Is.EqualTo(BattleStateHash.Compute(loaded.Encounter.Battle.State)));
            var journal=new BattleJournal(w.Encounter.Battle.State,"controlled-05B-mirror-"+first,"test");
            var identities=w.Encounter.Ids.Values.ToArray();int refresh=w.Refresh;
            for(int i=0;i<1000&&!journal.State.Outcome.IsEnded;i++){var ai=TacticalAi.Choose(journal.State);Assert.That(journal.Apply(ai.Command,"AI",ai.Explanation).IsApplied,Is.True);}
            Assert.That(journal.State.Outcome.IsEnded,Is.True);Assert.That(journal.Session.deaths,Is.GreaterThan(0));Assert.That(journal.Records.SelectMany(r=>r.events).Any(e=>e.kind=="SpellCast"),Is.True);
            Assert.That(ReplayVerification.Verify(journal.Header,journal.Records,journal.Footer()).Matches,Is.True);
            Assert.That(w.ResolveBattle(journal.State),Is.True);Assert.That(loaded.ResolveBattle(journal.State),Is.True);
            Assert.That(w.Refresh,Is.EqualTo(refresh));Assert.That(w.CaptureSave().checksum,Is.EqualTo(loaded.CaptureSave().checksum));
            Assert.That(w.AllForces.SelectMany(a=>a.Formation.Members).Select(c=>c.CharacterId).OrderBy(id=>id),Is.EqualTo(identities.OrderBy(id=>id)));
            for(int i=0;i<60&&!w.Winner.HasValue&&!w.IsDraw;i++)CityFoundationsTests.End(w);
            Assert.That(w.Winner.HasValue||w.IsDraw,Is.True);
            TestContext.WriteLine("Controlled (not manual) 05B first="+first+" R="+w.Refresh+" winner="+w.Winner+" tactical rounds="+journal.State.Round+" casualties="+journal.Session.deaths+" escapes="+journal.Session.escapes+" same IDs/Refresh/save continuation PASS");
        }
    }
}

using System;
using System.Linq;
using NUnit.Framework;
using RPG.Core;

namespace RPG.Tests
{
    public class StrategicRouteTests
    {
        private static void Move(StrategicScenario s,params int[] nodes)=>StrategicScenarioTests.Move(s,nodes);
        private static void Battle(StrategicScenario s)
        {
            Assert.That(s.Encounter,Is.Not.Null);var journal=new BattleJournal(s.Encounter.Battle.State,"WP02-route","validation");
            for(int i=0;i<2500&&!journal.State.Outcome.IsEnded;i++)
                Assert.That(journal.Apply(TacticalAi.Choose(journal.State).Command).IsApplied,Is.True);
            Assert.That(journal.State.Outcome.IsEnded,Is.True,"Tactical command budget exceeded");
            TestContext.WriteLine("R"+s.Refresh+" @"+s.PlayerNode+" vs "+string.Join("+",s.Encounter.Participants)+" "+journal.State.Outcome.VictorySide
                +" "+journal.State.Outcome.Reason+"; player survivors "+journal.State.Units.Count(u=>u.Side==Side.West&&u.Status!=UnitStatus.Dead));
            Assert.That(ReplayVerification.Verify(journal.Header,journal.Records,journal.Footer()).Matches,Is.True);
            Assert.That(s.ResolveBattle(journal.State),Is.True);Assert.That(s.Result,Is.Not.EqualTo(StrategicMissionResult.FormationLost));
        }
        [TestCase("Central")][TestCase("North")][TestCase("South")]
        public void CompleteRoutesWithActualCoreCombatAndReplay(string route)
        {
            var s=new StrategicScenario();
            if(route=="Central")
            {
                Move(s,3,8);Battle(s);s.EndActivation();Move(s,10);Assert.That(s.Attack(StrategicActorKind.AreaGuard),Is.True);Battle(s);
                Move(s,10);Assert.That(s.InteractPortal(),Is.True);Move(s,12);s.EndActivation();s.EndActivation();Battle(s);Move(s,1);
            }
            else if(route=="North")
            {
                Move(s,5);s.EndActivation();Move(s,6,10);s.Attack(StrategicActorKind.AreaGuard);Battle(s);s.EndActivation();Move(s,10);
                s.InteractPortal();Move(s,8,3);Battle(s);s.EndActivation();Battle(s);Move(s,1);
            }
            else
            {
                Move(s,14);s.EndActivation();Battle(s);Move(s,17,10);s.EndActivation();Battle(s);s.InteractPortal();Move(s,14);
                s.EndActivation();Move(s,13);s.Attack(StrategicActorKind.IncursionB);Battle(s);s.EndActivation();Move(s,1);
                Assert.That(s.Waystation,Is.EqualTo(StrategicSiteCondition.Intact));
            }
            TestContext.WriteLine(route+" finish R"+s.Refresh+" provisions "+s.Provisions+" roster "+string.Join(",",s.Player.Members.Select(c=>c.CharacterId+":"+c.Hp+"/"+c.Armor+":"+c.Status)));
            Assert.That(s.Result,Is.EqualTo(StrategicMissionResult.CouncilAssistanceRequested));
            Assert.That(s.Player.Members.Any(c=>c.Armor<c.Profile.MaxArmor),Is.True,"Attrition must remain visible");
        }
    }
}

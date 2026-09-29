using System.Collections;
using System.Linq;
using NUnit.Framework;
using RPG.Core;
using RPG.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object=UnityEngine.Object;

namespace RPG.Presentation.Tests
{
    public class StrategicPresentationTests
    {
        [UnityTest]
        public IEnumerator WorldBattleWorldUsesActualPersistentResultsAndKeepsSameSession()
        {
            yield return SceneManager.LoadSceneAsync("TacticalGraybox",LoadSceneMode.Single);yield return null;
            var p=Object.FindAnyObjectByType<BattlePresenter>();p.StartStrategicScenario();var world=p.World;yield return null;
            string before=world.DebugState();
            p.SelectWorldNode(3);
            Assert.That(world.DebugState(),Is.EqualTo(before));Assert.That(p.HudRoot.Q<Label>("world-preview").text,Does.Contain("40"));
            Assert.That(p.MoveOnWorld(3),Is.True);
            Assert.That(world.PlayerNode,Is.EqualTo(3));world.Move(8);p.WorldChanged();
            Assert.That(p.State,Is.SameAs(world.Encounter.Battle.State));Assert.That(p.State.Battlefield.Columns,Is.EqualTo(23));
            string persistent=world.DebugState();p.SelectCell(new GridPosition(4,4));Assert.That(world.DebugState(),Is.EqualTo(persistent));
            var invalid=p.Submit(new MoveCommand(p.State.CurrentUnitId.Value,new[]{new GridPosition(-1,-1)}));
            Assert.That(invalid.IsApplied,Is.False);Assert.That(world.DebugState(),Is.EqualTo(persistent));
            for(int i=0;i<2000&&!p.State.Outcome.IsEnded;i++)p.Submit(TacticalAi.Choose(p.State).Command);
            Assert.That(p.State.Outcome.IsEnded,Is.True);
            Assert.That(ReplayVerification.Verify(p.Journal.Header,p.Journal.Records,p.Journal.Footer()).Matches,Is.True);
            var hp=p.State.Units.Where(u=>u.Side==Side.West).Sum(u=>u.Hp);p.ReturnToWorld();
            Assert.That(p.World,Is.SameAs(world));Assert.That(world.Refresh,Is.EqualTo(1));Assert.That(world.Provisions,Is.EqualTo(36));
            Assert.That(world.Player.Members.Sum(c=>c.Hp),Is.EqualTo(hp));Assert.That(p.HudRoot.Q<Label>("world-roster").text,Does.Contain("baron-1"));
            Assert.That(p.HudRoot.Q<Label>("world-status").text,Does.Contain("No recovery on battle exit"));
            world.EndActivation();Assert.That(p.MoveOnWorld(10),Is.True);world.Attack(StrategicActorKind.AreaGuard);p.WorldChanged();ResolveDisplayedBattle(p);
            Assert.That(p.MoveOnWorld(10),Is.True);Assert.That(world.InteractPortal(),Is.True);p.WorldChanged();
            Assert.That(p.MoveOnWorld(12),Is.True);world.EndActivation();world.EndActivation();p.WorldChanged();ResolveDisplayedBattle(p);
            Assert.That(p.MoveOnWorld(1),Is.True);
            Assert.That(p.HudRoot.Q<Label>("world-status").text,Does.Contain("SUCCESS · Council assistance requested"));
            Assert.That(p.HudRoot.Q<Label>("world-roster").text,Does.Contain("Commanderless / roster locked"));
            yield return null;LogAssert.NoUnexpectedReceived();
        }
        private static void ResolveDisplayedBattle(BattlePresenter p)
        {
            Assert.That(p.World.Encounter,Is.Not.Null);
            for(int i=0;i<2000&&!p.State.Outcome.IsEnded;i++)p.Submit(TacticalAi.Choose(p.State).Command);
            Assert.That(p.State.Outcome.IsEnded,Is.True);p.ReturnToWorld();
        }
        [UnityTest]
        public IEnumerator VillageFailureIsCausalAndDisablesWorldCommands()
        {
            yield return SceneManager.LoadSceneAsync("TacticalGraybox",LoadSceneMode.Single);yield return null;
            var p=Object.FindAnyObjectByType<BattlePresenter>();p.StartStrategicScenario();
            for(int i=0;i<5;i++)p.World.EndActivation();p.WorldChanged();
            Assert.That(p.HudRoot.Q<Label>("world-status").text,Does.Contain("DEFEAT · Riverside Village Ravaged"));
            Assert.That(p.HudRoot.Q<Button>("world-end").enabledSelf,Is.False);
            Assert.That(p.World.Waystation,Is.EqualTo(StrategicSiteCondition.Ravaged));
            yield return null;LogAssert.NoUnexpectedReceived();
        }
    }
}

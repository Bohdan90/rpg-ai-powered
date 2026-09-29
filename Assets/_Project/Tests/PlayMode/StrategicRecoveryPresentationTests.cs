using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RPG.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object=UnityEngine.Object;

namespace RPG.Presentation.Tests
{
    public class StrategicRecoveryPresentationTests
    {
        [UnityTest]
        public IEnumerator ActualBattlesReturnSaveReloadKeepRecoveryAndRedeploy()
        {
            string path=Path.Combine(Application.temporaryCachePath,"keep-recovery-"+Guid.NewGuid().ToString("N")+".json");
            try
            {
                yield return SceneManager.LoadSceneAsync("TacticalGraybox",LoadSceneMode.Single);yield return null;
                var p=Object.FindAnyObjectByType<BattlePresenter>();p.StartStrategicScenario();p.MoveOnWorld(3);p.MoveOnWorld(8);FinishBattle(p);
                p.World.EndActivation();p.MoveOnWorld(10);p.World.Attack(StrategicActorKind.AreaGuard);p.WorldChanged();FinishBattle(p);
                Assert.That(p.World.Player.LivingMembers.Any(c=>c.Hp<c.Profile.MaxHp/2),Is.True,"Real combat must produce meaningful HP attrition");
                Assert.That(p.HudRoot.Q<Label>("world-recovery").text,Does.Contain("+15%"));
                p.MoveOnWorld(10);p.World.EndActivation();p.WorldChanged();Assert.That(p.MoveOnWorld(1),Is.True);
                var world=p.World;string before=world.CaptureSave().checksum;
                var expected=world.Player.Members.ToDictionary(c=>c.CharacterId,c=>c.Hp+c.PreviewHpRecovery(world.RecoveryPercent));
                var armor=world.Player.Members.ToDictionary(c=>c.CharacterId,c=>c.Armor);
                Assert.That(p.HudRoot.Q<Label>("world-recovery").text,Does.Contain("+40%").And.Contain("Armor is NOT repaired"));
                p.SelectWorldNode(12);Assert.That(world.CaptureSave().checksum,Is.EqualTo(before));Assert.That(p.SaveStrategic(path),Is.True);
                yield return SceneManager.LoadSceneAsync("TacticalGraybox",LoadSceneMode.Single);yield return null;
                p=Object.FindAnyObjectByType<BattlePresenter>();Assert.That(p.LoadStrategic(path),Is.True);
                p.World.EndActivation();p.WorldChanged();world.EndActivation();
                Assert.That(p.World.CaptureSave().checksum,Is.EqualTo(world.CaptureSave().checksum));
                foreach(var c in p.World.Player.Members){Assert.That(c.Hp,Is.EqualTo(expected[c.CharacterId]));Assert.That(c.Armor,Is.EqualTo(armor[c.CharacterId]));}
                Assert.That(p.World.Refresh,Is.EqualTo(4));Assert.That(p.World.Actor(StrategicActorKind.IncursionB).Active,Is.True);
                Assert.That(p.HudRoot.Q<Label>("world-events").text,Does.Contain("+40% Max HP"));
                Assert.That(p.MoveOnWorld(12),Is.True);Assert.That(p.HudRoot.Q<Label>("world-recovery").text,Does.Contain("+15%"));
                LogAssert.NoUnexpectedReceived();
            }
            finally {if(File.Exists(path))File.Delete(path);}
        }
        private static void FinishBattle(BattlePresenter p)
        {
            Assert.That(p.World.Encounter,Is.Not.Null);
            for(int i=0;i<2000&&!p.State.Outcome.IsEnded;i++)p.Submit(TacticalAi.Choose(p.State).Command);
            Assert.That(p.State.Outcome.IsEnded,Is.True);p.ReturnToWorld();
        }
    }
}

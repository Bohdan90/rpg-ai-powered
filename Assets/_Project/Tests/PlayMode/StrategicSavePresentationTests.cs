using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using RPG.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object=UnityEngine.Object;

namespace RPG.Presentation.Tests
{
    public class StrategicSavePresentationTests
    {
        private string path;
        [SetUp] public void Setup()=>path=Path.Combine(Application.temporaryCachePath,"mission-save-test-"+Guid.NewGuid().ToString("N")+".json");
        [TearDown] public void Cleanup(){if(File.Exists(path))File.Delete(path);}
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(5)]
        public void JsonRoundTripBeforeAnyBattlePreservesAbsentXpResolution(int refreshes)
        {
            var world=new StrategicScenario();for(int i=0;i<refreshes;i++)world.EndActivation();
            Assert.That(StrategicSaveFiles.TrySave(world,path,out var message),Is.True,message);
            Assert.That(StrategicSaveFiles.TryLoad(path,out var restored,out message),Is.True,message);
            Assert.That(restored.LastBattleResolution,Is.Null);
            Assert.That(restored.CaptureSave().checksum,Is.EqualTo(world.CaptureSave().checksum));
        }
        [UnityTest]
        public IEnumerator DiskSaveRecreatedSessionLoadAndContinueAfterActualBattle()
        {
            yield return SceneManager.LoadSceneAsync("TacticalGraybox",LoadSceneMode.Single);yield return null;
            var p=Object.FindAnyObjectByType<BattlePresenter>();p.StartStrategicScenario();p.MoveOnWorld(3);p.MoveOnWorld(8);
            Assert.That(p.SaveStrategic(path),Is.False);Assert.That(File.Exists(path),Is.False);
            for(int i=0;i<2000&&!p.State.Outcome.IsEnded;i++)p.Submit(TacticalAi.Choose(p.State).Command);
            Assert.That(p.State.Outcome.IsEnded,Is.True);p.ReturnToWorld();var original=p.World;
            Assert.That(p.SaveStrategic(path),Is.True);string hash=original.CaptureSave().checksum;
            Assert.That(p.SaveStrategic(path),Is.True,"Replacing existing manual slot must work");
            yield return SceneManager.LoadSceneAsync("TacticalGraybox",LoadSceneMode.Single);yield return null;
            p=Object.FindAnyObjectByType<BattlePresenter>();Assert.That(p.World,Is.Null);
            Assert.That(p.HudRoot.Q<Button>("world-load"),Is.Not.Null);Assert.That(p.LoadStrategic(path),Is.True);
            Assert.That(p.World,Is.Not.SameAs(original));Assert.That(p.World.CaptureSave().checksum,Is.EqualTo(hash));
            Assert.That(p.HudRoot.Q<Label>("world-save-status").text,Does.Contain("Loaded Mission 01"));
            p.World.EndActivation();original.EndActivation();Assert.That(p.World.CaptureSave().checksum,Is.EqualTo(original.CaptureSave().checksum));
            Assert.That(p.MoveOnWorld(10),Is.True);original.Move(10);p.World.Attack(StrategicActorKind.AreaGuard);original.Attack(StrategicActorKind.AreaGuard);p.WorldChanged();
            Assert.That(BattleStateHash.Compute(p.State),Is.EqualTo(BattleStateHash.Compute(original.Encounter.Battle.State)));
            var current=p.World;Assert.That(p.LoadStrategic(path),Is.False);Assert.That(p.World,Is.SameAs(current));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]
        public IEnumerator CorruptIncompatibleAndMissingFileNeverReplaceLiveScenario()
        {
            yield return SceneManager.LoadSceneAsync("TacticalGraybox",LoadSceneMode.Single);yield return null;
            var p=Object.FindAnyObjectByType<BattlePresenter>();p.StartStrategicScenario();p.MoveOnWorld(5);var live=p.World;string hash=live.CaptureSave().checksum;
            Assert.That(p.LoadStrategic(path),Is.False);
            foreach(string json in new[]{"not json","{}","{\"version\":999}","{\"version\":1"})
            {
                File.WriteAllText(path,json);Assert.That(p.LoadStrategic(path),Is.False);Assert.That(p.World,Is.SameAs(live));
                Assert.That(live.CaptureSave().checksum,Is.EqualTo(hash));
            }
            Assert.That(p.SaveStrategic(path),Is.True);var d=JsonUtility.FromJson<StrategicSaveData>(File.ReadAllText(path));d.tempo--;
            File.WriteAllText(path,JsonUtility.ToJson(d));Assert.That(p.LoadStrategic(path),Is.False);
            Assert.That(p.World,Is.SameAs(live));Assert.That(live.CaptureSave().checksum,Is.EqualTo(hash));
            Assert.That(p.HudRoot.Q<Label>("world-save-status").text,Does.Contain("current session unchanged"));LogAssert.NoUnexpectedReceived();
        }
    }
}

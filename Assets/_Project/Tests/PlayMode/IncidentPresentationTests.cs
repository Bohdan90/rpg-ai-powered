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
    public class IncidentPresentationTests
    {
        private string path;
        [SetUp] public void Setup()=>path=Path.Combine(Application.temporaryCachePath,"incident-test-"+Guid.NewGuid().ToString("N")+".json");
        [TearDown] public void Cleanup(){if(File.Exists(path))File.Delete(path);}
        [UnityTest] public IEnumerator SelectorWorldPhaseHudAndRecreatedSessionContinuation()
        {
            yield return SceneManager.LoadSceneAsync("TacticalGraybox");yield return null;var p=Object.FindAnyObjectByType<BattlePresenter>();
            Assert.That(p.HudRoot.Q<Button>("incident-start-west"),Is.Not.Null);p.StartIncident(Side.East);var s=p.Duel;s.Move(Side.East,8);s.EndActivation(Side.East);p.DuelChanged();Assert.That(p.SaveDuel(path),Is.True);
            string hash=s.CaptureSave().checksum;yield return SceneManager.LoadSceneAsync("TacticalGraybox");yield return null;p=Object.FindAnyObjectByType<BattlePresenter>();Assert.That(p.LoadDuel(path),Is.True,p.DuelSaveMessage);Assert.That(p.Duel.CaptureSave().checksum,Is.EqualTo(hash));
            foreach(var x in new[]{s,p.Duel}){x.ContinueHandoff(Side.West);x.EndActivation(Side.West);x.ContinueWorldPhase();}p.DuelChanged();
            Assert.That(p.Duel.CaptureSave().checksum,Is.EqualTo(s.CaptureSave().checksum));Assert.That(p.HudRoot.Q<Label>("incident-state").text,Does.Contain("Red"));Assert.That(p.HudRoot.Q("duel-node-16"),Is.Not.Null);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator NonActiveHumanNpcContactUsesCorrectControllerAndSaveBlocker()
        {
            yield return SceneManager.LoadSceneAsync("TacticalGraybox");yield return null;var p=Object.FindAnyObjectByType<BattlePresenter>();p.StartIncident();p.Duel.Move(Side.West,8);
            var s=p.Duel;s.EndActivation(Side.West);s.ContinueHandoff(Side.East);s.EndActivation(Side.East);s.ContinueWorldPhase();s.ContinueHandoff(Side.West);s.EndActivation(Side.West);s.ContinueHandoff(Side.East);s.EndActivation(Side.East);s.ContinueWorldPhase();p.DuelChanged();
            Assert.That(s.PendingContact,Is.Not.Null);Assert.That(p.HudRoot.Q<Button>("duel-save").enabledInHierarchy,Is.False);Assert.That(p.HudRoot.Q<Button>("incident-fight").style.display.value,Is.EqualTo(DisplayStyle.Flex));Assert.That(p.SaveDuel(path),Is.False);
            s.RespondToContact(Side.West,false);p.DuelChanged();Assert.That(p.PlayerVsAi,Is.True);Assert.That(p.AiSide,Is.EqualTo(Side.West));Assert.That(p.HudRoot.Q<Label>("battle-title").text,Does.Contain("Raider A AI"));Assert.That(p.TacticalSideLabel(Side.East),Is.EqualTo("West human"));Assert.That(s.Encounter.TacticalSides[s.West.Formation.FormationId],Is.EqualTo(Side.East));Assert.That(p.SaveDuel(path),Is.False);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator EastInitiatedHumanBattleRemainsHotseatWithExplicitOwnershipLabels()
        {
            yield return SceneManager.LoadSceneAsync("TacticalGraybox");yield return null;var p=Object.FindAnyObjectByType<BattlePresenter>();
            var d=new CrossroadsScenario(Side.East,incident:true).CaptureSave();d.east.node=8;d.west.node=7;d.checksum=d.ComputeHash();File.WriteAllText(path,JsonUtility.ToJson(d));
            Assert.That(p.LoadDuel(path),Is.True);Assert.That(p.Duel.AttackNode(Side.East,7),Is.True);Assert.That(p.Duel.RespondToContact(Side.West,false),Is.True);p.DuelChanged();
            Assert.That(p.PlayerVsAi,Is.False);Assert.That(p.TacticalSideLabel(Side.West),Is.EqualTo("East human"));Assert.That(p.TacticalSideLabel(Side.East),Is.EqualTo("West human"));
            Assert.That(p.HudRoot.Q<Label>("battle-title").text,Does.Contain("Blue: East human"));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator CorruptLoadLeavesLiveWorldAndOriginalScenarioSelectable()
        {
            yield return SceneManager.LoadSceneAsync("TacticalGraybox");yield return null;var p=Object.FindAnyObjectByType<BattlePresenter>();p.StartIncident();string h=p.Duel.CaptureSave().checksum;File.WriteAllText(path,"{bad");Assert.That(p.LoadDuel(path),Is.False);Assert.That(p.Duel.CaptureSave().checksum,Is.EqualTo(h));p.StartDuel();Assert.That(p.Duel.Incident,Is.Null);Assert.That(p.Duel.TargetPressure,Is.EqualTo(8));LogAssert.NoUnexpectedReceived();
        }
    }
}

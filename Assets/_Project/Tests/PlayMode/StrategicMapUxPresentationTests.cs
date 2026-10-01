using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using RPG.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
namespace RPG.Presentation.Tests
{
    public class StrategicMapUxPresentationTests
    {
        [UnityTest] public IEnumerator PortalPrimaryActionIsContextualAndRequiresSeparateConfirmation()
        {
            yield return SceneManager.LoadSceneAsync("TacticalGraybox",LoadSceneMode.Single);yield return null;
            var p=Object.FindAnyObjectByType<BattlePresenter>();p.StartSeamless();var view=p.SeamlessMap;var w=p.Duel;
            var b=view.Root.Q<Button>("worlds-traverse");Assert.That(b.style.display.value,Is.EqualTo(DisplayStyle.None));
            w.Seamless.SetDestination(Side.West,new WorldAddress(WorldId.Frontier,24));w.Seamless.ContinueTravel(Side.West);p.DuelChanged();yield return null;
            Assert.That(b.style.display.value,Is.EqualTo(DisplayStyle.Flex));Assert.That(b.enabledSelf,Is.True);Assert.That(b.text,Does.Contain("Traverse Portal"));
            string hash=w.CaptureSave().checksum;var click=typeof(SeamlessMapView).GetMethod("Traverse",BindingFlags.NonPublic|BindingFlags.Instance);click.Invoke(view,null);
            Assert.That(w.CaptureSave().checksum,Is.EqualTo(hash));Assert.That(b.text,Does.Contain("Confirm Traverse Portal"));Assert.That(view.Root.Query<Label>().ToList().Any(l=>l.text.Contains("Unknown destination")&&l.text.Contains("20 Tempo")),Is.True);
            click.Invoke(view,null);Assert.That(w.West.WorldId,Is.EqualTo(WorldId.StoneValley));Assert.That(w.West.Tempo,Is.EqualTo(20));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ExplicitContinueButtonForecastAndTurnBoundariesMatchCore()
        {
            yield return SceneManager.LoadSceneAsync("TacticalGraybox",LoadSceneMode.Single);yield return null;
            var p=Object.FindAnyObjectByType<BattlePresenter>();p.StartProductionRoads();yield return null;var w=p.Duel;var s=w.Seamless;
            int start=w.West.Node;Assert.That(s.SetDestination(Side.West,new WorldAddress(WorldId.Frontier,25)));p.DuelChanged();yield return null;
            var label=p.SeamlessMap.Root.Q<Label>("worlds-journey");var button=p.SeamlessMap.Root.Q<Button>("worlds-continue-travel");
            Assert.That(w.West.Node,Is.EqualTo(start));Assert.That(button.enabledSelf,Is.True);Assert.That(label.text,Does.Contain("expected stop"));Assert.That(label.text,Does.Contain("End Turn does not move"));
            var forecast=s.PreviewContinueTravel(Side.West);string hash=w.CaptureSave().checksum;p.SeamlessMap.Refresh();Assert.That(w.CaptureSave().checksum,Is.EqualTo(hash));
            typeof(SeamlessMapView).GetMethod("ContinueTravel",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(p.SeamlessMap,null);yield return null;
            Assert.That(w.West.Node,Is.EqualTo(forecast.Path.Last()));Assert.That(forecast.Path.Length,Is.GreaterThan(2));int at=w.West.Node;
            w.EndActivation(Side.West);w.ContinueHandoff(Side.East);w.EndActivation(Side.East);w.ContinueHandoff(Side.West);p.DuelChanged();yield return null;
            Assert.That(w.West.Node,Is.EqualTo(at));Assert.That(p.SeamlessMap.Root.Q<Button>("worlds-continue-travel").enabledSelf,Is.True);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ProductionViewHidesTechnicalAnchorsShowsRetainedJourneyAndRoundTrips()
        {
            yield return SceneManager.LoadSceneAsync("TacticalGraybox",LoadSceneMode.Single);yield return null;
            var p=Object.FindAnyObjectByType<BattlePresenter>();p.StartProductionRoads();yield return null;var w=p.Duel;var view=p.SeamlessMap;
            Assert.That(view.Root.Q("world-node-A26"),Is.Null);Assert.That(view.Root.Q("world-node-A06"),Is.Not.Null);Assert.That(view.Root.Q("world-node-B01"),Is.Null);
            Assert.That(w.Seamless.SetDestination(Side.West,new WorldAddress(WorldId.Frontier,19)),Is.True);p.DuelChanged();yield return null;
            Assert.That(view.Root.Q<Label>("worlds-journey").text,Does.Contain("Destination:"));Assert.That(view.Root.Q<Button>("worlds-cancel-destination").enabledSelf,Is.True);
            string hash=w.CaptureSave().checksum;view.Fit();view.Focus(w.West.Address);view.Refresh();Assert.That(w.CaptureSave().checksum,Is.EqualTo(hash));
            string path=Path.Combine(Application.temporaryCachePath,"roads08-ui.json");Assert.That(p.SaveDuel(path),Is.True,p.DuelSaveMessage);
            yield return SceneManager.LoadSceneAsync("TacticalGraybox",LoadSceneMode.Single);yield return null;p=Object.FindAnyObjectByType<BattlePresenter>();Assert.That(p.LoadDuel(path),Is.True,p.DuelSaveMessage);Assert.That(p.Duel.CaptureSave().checksum,Is.EqualTo(hash));Assert.That(p.Duel.Seamless.ProductionTopology,Is.True);
            p.Duel.EndActivation(Side.West);p.DuelChanged();Assert.That(p.SeamlessMap.Root.style.display.value,Is.EqualTo(DisplayStyle.None));p.Duel.ContinueHandoff(Side.East);p.DuelChanged();Assert.That(p.SeamlessMap.Root.Q<Label>("worlds-journey").text,Does.Not.Contain("retained"));File.Delete(path);LogAssert.NoUnexpectedReceived();
        }
    }
}

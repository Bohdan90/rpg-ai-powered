using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RPG.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
namespace RPG.Presentation.Tests
{
    public class SeamlessPresentationTests
    {
        [UnityTest] public IEnumerator AuthoredInspectionLoadsWithKnownPortalWithoutReapplyingPassage()
        {
            yield return SceneManager.LoadSceneAsync("TacticalGraybox",LoadSceneMode.Single);yield return null;
            var p=Object.FindAnyObjectByType<BattlePresenter>();
            Assert.That(p.LoadDuel(Path.Combine(Application.streamingAssetsPath,"SeamlessWorlds07","authored-portal-arrival.json")),Is.True,p.DuelSaveMessage);
            Assert.That(p.Duel.West.Address,Is.EqualTo(new WorldAddress(WorldId.StoneValley,1)));Assert.That(p.Duel.West.Tempo,Is.EqualTo(20));Assert.That(p.Duel.Refresh,Is.EqualTo(1));
            Assert.That(p.Duel.Seamless.Knowledge(Side.West).KnowsLink("West"),Is.True);Assert.That(p.Duel.Seamless.Knowledge(Side.East).Worlds,Is.EqualTo(new[]{WorldId.Frontier}));
            Assert.That(p.Duel.CaptureSave().Restore().West.Tempo,Is.EqualTo(20));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator RealViewUnknownWorldPrivacyPortalsLayoutAndRecreateLoad()
        {
            yield return SceneManager.LoadSceneAsync("TacticalGraybox",LoadSceneMode.Single);yield return null;
            var p=Object.FindAnyObjectByType<BattlePresenter>();p.StartSeamless();yield return null;var w=p.Duel;var view=p.SeamlessMap;
            Assert.That(view,Is.Not.Null);Assert.That(view.Root.Q("world-node-A24"),Is.Not.Null);Assert.That(view.Root.Q("world-node-B01"),Is.Null);Assert.That(view.Root.Query<Button>().ToList().Any(b=>b.text.Contains("Stone Valley")),Is.False);
            var sim=w.Seamless.SimulationHash();var k=w.Seamless.KnowledgeHash(Side.West);view.Fit();view.Focus(new WorldAddress(WorldId.StoneValley,1));view.ValleyOffset=new Vector2(5000,-600);view.ValleyRotation=73;view.Refresh();Assert.That(w.Seamless.SimulationHash(),Is.EqualTo(sim));Assert.That(w.Seamless.KnowledgeHash(Side.West),Is.EqualTo(k));
            Assert.That(w.Seamless.Move(Side.West,new WorldAddress(WorldId.Frontier,24)),Is.True);Assert.That(w.Seamless.Traverse(Side.West,w.West.Formation.FormationId,w.West.Address),Is.True);p.DuelChanged();yield return null;Assert.That(view.Root.Q("world-node-B01"),Is.Not.Null);Assert.That(view.Root.Q("world-node-B07"),Is.Null);
            var copy=w.CaptureSave().Restore();sim=w.Seamless.SimulationHash();k=w.Seamless.KnowledgeHash(Side.West);view.ValleyVisible=false;view.Refresh();view.Fit();view.ValleyVisible=true;view.ValleyOffset=new Vector2(1400,380);view.ValleyRotation=0;view.Refresh();view.ShowObservation(w.Seamless.Knowledge(Side.West).History.Last().sequence);Assert.That(w.Seamless.SimulationHash(),Is.EqualTo(sim));Assert.That(w.Seamless.KnowledgeHash(Side.West),Is.EqualTo(k));
            foreach(var x in new[]{w,copy}){Assert.That(x.Seamless.Move(Side.West,new WorldAddress(WorldId.StoneValley,2)),Is.True);x.EndActivation(Side.West);x.ContinueHandoff(Side.East);x.EndActivation(Side.East);x.ContinueHandoff(Side.West);}Assert.That(w.CaptureSave().checksum,Is.EqualTo(copy.CaptureSave().checksum));
            string path=Path.Combine(Application.temporaryCachePath,"seamless07-test.json");Assert.That(p.SaveDuel(path),Is.True,p.DuelSaveMessage);string hash=w.CaptureSave().checksum;
            yield return SceneManager.LoadSceneAsync("TacticalGraybox",LoadSceneMode.Single);yield return null;p=Object.FindAnyObjectByType<BattlePresenter>();Assert.That(p.LoadDuel(path),Is.True,p.DuelSaveMessage);Assert.That(p.Duel.CaptureSave().checksum,Is.EqualTo(hash));Assert.That(p.SeamlessMap.Root.Q("world-node-B01"),Is.Not.Null);
            p.Duel.EndActivation(Side.West);p.DuelChanged();Assert.That(p.HudRoot.Q("realm-handoff").resolvedStyle.display,Is.Not.EqualTo(DisplayStyle.None));Assert.That(p.SeamlessMap.Root.style.display.value,Is.EqualTo(DisplayStyle.None));
            p.Duel.ContinueHandoff(Side.East);p.DuelChanged();Assert.That(p.SeamlessMap.Root.Q("world-node-B01"),Is.Null);Assert.That(p.SeamlessMap.Root.Query<Button>().ToList().Any(b=>b.text.Contains("Stone Valley")),Is.False);
            hash=p.Duel.CaptureSave().checksum;File.WriteAllText(path,"{bad");Assert.That(p.LoadDuel(path),Is.False);Assert.That(p.Duel.CaptureSave().checksum,Is.EqualTo(hash));File.Delete(path);LogAssert.NoUnexpectedReceived();
        }
    }
}

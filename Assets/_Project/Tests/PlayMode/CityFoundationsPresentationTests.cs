using System.Collections;
using System.IO;
using NUnit.Framework;
using RPG.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
namespace RPG.Presentation.Tests
{
    public class CityFoundationsPresentationTests
    {
        [UnityTest] public IEnumerator CityUiPaidQueuesSaveRecreateLoadAndContinuation()
        {
            yield return SceneManager.LoadSceneAsync("TacticalGraybox",LoadSceneMode.Single);yield return null;
            var p=Object.FindAnyObjectByType<BattlePresenter>();p.StartCity(true);yield return null;
            Assert.That(p.HudRoot.Q("city-overview"),Is.Not.Null);
            var world=p.Duel;Assert.That(world.Foundations.QueueProject(world,Side.West,1,CityProjectKind.DevelopmentII),Is.True);
            Assert.That(world.Foundations.QueueResearch(world,Side.West,CityTech.ElementalDrills),Is.True);p.DuelChanged();
            string path=Path.Combine(Application.temporaryCachePath,"city-5051-"+System.Guid.NewGuid()+".json");
            Assert.That(p.SaveDuel(path),Is.True,p.DuelSaveMessage);string hash=world.CaptureSave().checksum;
            yield return SceneManager.LoadSceneAsync("TacticalGraybox",LoadSceneMode.Single);yield return null;
            p=Object.FindAnyObjectByType<BattlePresenter>();Assert.That(p.LoadDuel(path),Is.True,p.DuelSaveMessage);Assert.That(p.Duel.CaptureSave().checksum,Is.EqualTo(hash));
            foreach(var w in new[]{world,p.Duel})for(int i=0;i<6;i++){if(w.HandoffPending)w.ContinueHandoff(w.ActiveSide);Assert.That(w.EndActivation(w.ActiveSide),Is.True);}
            Assert.That(p.Duel.CaptureSave().checksum,Is.EqualTo(world.CaptureSave().checksum));p.DuelChanged();
            File.WriteAllText(path,"corrupt");Assert.That(p.LoadDuel(path),Is.False);Assert.That(p.Duel.CaptureSave().checksum,Is.EqualTo(world.CaptureSave().checksum));File.Delete(path);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator AuthoredReady05BStateAndVisualEvidence()
        {
            yield return SceneManager.LoadSceneAsync("TacticalGraybox",LoadSceneMode.Single);yield return null;
            var p=Object.FindAnyObjectByType<BattlePresenter>();p.StartCity(true);
            // Explicit controlled fixture, not an earned match: supports immediate economic/training inspection.
            var data=p.Duel.CaptureSave();var city=data.foundations.locations[0];city.development=2;city.forge=true;city.tower=2;
            data.west.formation.members[1].hp=0;data.west.formation.members[1].status=(int)PersistentCharacterStatus.Dead;
            data.west.formation.members[0].hp=21;data.west.formation.members[0].armor=3;
            var drills=data.foundations.west.research[(int)CityTech.ElementalDrills];drills.work="6";drills.paid=true;drills.cities=new[]{1};drills.contributions=new[]{"6"};
            data.events=new[]{"AUTHORED 05B FIXTURE: DevII, Forge, TowerII, Drills, one casualty and damaged Commander. Not a played match. Recruit still costs Gold/capacity; no trained replacement is fabricated."};
            data.checksum=data.ComputeHash();data.Restore();
            string directory=Path.Combine(Application.persistentDataPath,"CityCombat05B","Fixtures");Directory.CreateDirectory(directory);
            string path=Path.Combine(directory,"authored-ready.json");File.WriteAllText(path,JsonUtility.ToJson(data,true));Assert.That(p.LoadDuel(path),Is.True,p.DuelSaveMessage);yield return null;
            Assert.That(File.Exists(path),Is.True);
            Assert.That(p.LoadDuel(Path.Combine(Application.streamingAssetsPath,"CityCombat05B","authored-ready.json")),Is.True,p.DuelSaveMessage);
            Assert.That(p.Duel.RecruitBlocker(Side.West,UnitProfileId.FireMageTI),Is.Null);
            Assert.That(p.Duel.Foundations.QuoteRepair(p.Duel,Side.West).Cost,Is.EqualTo(16));
            for(int frame=0;frame<4;frame++)yield return null;
            ScreenCapture.CaptureScreenshot(Path.Combine(directory,"05B-overview.png"));
            for(int frame=0;frame<4;frame++)yield return null;
            LogAssert.NoUnexpectedReceived();
        }
    }
}

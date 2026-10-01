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
    public class RealmPresentationTests
    {
        [UnityTest] public IEnumerator ActualRealmHudSelectionSaveRecreateLoadAndContinue()
        {
            yield return SceneManager.LoadSceneAsync("TacticalGraybox",LoadSceneMode.Single);yield return null;
            var p=Object.FindAnyObjectByType<BattlePresenter>();p.StartRealm();yield return null;
            Assert.That(p.HudRoot.Q("realm-world"),Is.Not.Null);Assert.That(p.HudRoot.Q<Label>("realm-status").text,Does.Contain("selected ARMY"));
            var select=p.HudRoot.Q<DropdownField>("realm-selected");Assert.That(select.choices.Count,Is.EqualTo(2));select.value=select.choices[1];Assert.That(p.Duel.Realm.Selected(Side.West).Node,Is.EqualTo(1));Assert.That(p.Duel.Refresh,Is.EqualTo(1));
            Assert.That(p.Duel.Realm.Recruit(Side.West,UnitProfileId.HumanWarriorTI,"Reserve"),Is.True);p.DuelChanged();
            string path=Path.Combine(Application.temporaryCachePath,"realm06-"+System.Guid.NewGuid()+".json");Assert.That(p.SaveDuel(path),Is.True,p.DuelSaveMessage);string hash=p.Duel.CaptureSave().checksum;var original=p.Duel;
            yield return SceneManager.LoadSceneAsync("TacticalGraybox",LoadSceneMode.Single);yield return null;p=Object.FindAnyObjectByType<BattlePresenter>();Assert.That(p.LoadDuel(path),Is.True,p.DuelSaveMessage);Assert.That(p.Duel.CaptureSave().checksum,Is.EqualTo(hash));
            foreach(var w in new[]{original,p.Duel}){w.EndActivation(Side.West);w.ContinueHandoff(Side.East);w.EndActivation(Side.East);}
            Assert.That(p.Duel.CaptureSave().checksum,Is.EqualTo(original.CaptureSave().checksum));p.DuelChanged();Assert.That(p.Duel.Realm.West.Reserve.Count,Is.EqualTo(1));
            foreach(var toggle in p.HudRoot.Q("realm-unit-selection").Query<Toggle>().ToList())Assert.That(toggle.labelElement.resolvedStyle.color,Is.EqualTo(Color.white));
            hash=p.Duel.CaptureSave().checksum;File.WriteAllText(path,"{corrupt");Assert.That(p.LoadDuel(path),Is.False);Assert.That(p.Duel.CaptureSave().checksum,Is.EqualTo(hash));File.Delete(path);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator AuthoredInspectionSnapshotIsLabelledAndOwnNamespace()
        {
            yield return SceneManager.LoadSceneAsync("TacticalGraybox",LoadSceneMode.Single);yield return null;var p=Object.FindAnyObjectByType<BattlePresenter>();p.StartRealm();
            Assert.That(BattlePresenter.RealmSlot,Does.Contain("RealmOperations06"));Assert.That(BattlePresenter.RealmSlot,Is.Not.EqualTo(BattlePresenter.CitySlot(true)));
            var data=p.Duel.CaptureSave();data.events=new[]{"AUTHORED Realm 06 inspection state: two armies per side, six accepted historical Drills Work, Tower II. Not a played match."};data.checksum=data.ComputeHash();
            string dir=Path.Combine(Application.persistentDataPath,"RealmOperations06","Fixtures");Directory.CreateDirectory(dir);string path=Path.Combine(dir,"authored-initial.json");File.WriteAllText(path,JsonUtility.ToJson(data,true));
            Assert.That(p.LoadDuel(path),Is.True,p.DuelSaveMessage);yield return null;Assert.That(p.HudRoot.Q<Label>("realm-city").text,Does.Contain("Food 120/180"));Assert.That(p.LoadDuel(Path.Combine(Application.streamingAssetsPath,"RealmOperations06","controlled-joint-aftermath.json")),Is.True,p.DuelSaveMessage);Assert.That(p.Duel.Realm.LastBattle,Is.Not.Null);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator JointBattleAuditSerializesWithoutLosingOriginalParticipation()
        {
            yield return SceneManager.LoadSceneAsync("TacticalGraybox",LoadSceneMode.Single);yield return null;
            var p=Object.FindAnyObjectByType<BattlePresenter>();p.StartRealm();var data=p.Duel.CaptureSave();
            data.realm.armies[0].node=6;data.realm.armies[1].node=3;data.realm.armies[2].node=7;data.realm.armies[3].node=11;data.checksum=data.ComputeHash();
            string path=Path.Combine(Application.temporaryCachePath,"realm-joint-"+System.Guid.NewGuid()+".json");File.WriteAllText(path,JsonUtility.ToJson(data));Assert.That(p.LoadDuel(path),Is.True);
            var w=p.Duel;Assert.That(w.Realm.Attack(Side.West,w.East.Formation.FormationId),Is.True);Assert.That(w.RespondToContact(Side.East,false),Is.True);p.DuelChanged();yield return null;
            var j=new BattleJournal(w.Encounter.Battle.State,"06-audit","automated integration");
            for(int i=0;i<500&&!j.State.Outcome.IsEnded;i++) {
                var u=j.State.FindUnit(j.State.CurrentUnitId.Value);BattleCommand c=new EndActivationCommand(u.Id);
                if(u.Side==Side.East&&u.MovementRemaining>0){var b=j.State.Battlefield;var exits=Enumerable.Range(0,b.Columns*b.Rows).Select(k=>new GridPosition(k%b.Columns,k/b.Columns)).Where(g=>b.IsRetreatZone(u,g)).Select(g=>Pathfinder.FindPath(j.State,u.Id,g)).Where(r=>r.Found&&r.Cost>0).OrderBy(r=>r.Cost).ToArray();if(exits.Length>0)c=new MoveCommand(u.Id,exits[0].Steps);}
                Assert.That(j.Apply(c).IsApplied,Is.True);
            }
            Assert.That(j.State.Outcome.IsEnded,Is.True);Assert.That(w.ResolveBattle(j.State),Is.True);p.DuelChanged();Assert.That(w.Realm.LastBattle.unitIds.Length,Is.EqualTo(12));
            Assert.That(p.SaveDuel(path),Is.True,p.DuelSaveMessage);string hash=w.CaptureSave().checksum;
            yield return SceneManager.LoadSceneAsync("TacticalGraybox",LoadSceneMode.Single);yield return null;p=Object.FindAnyObjectByType<BattlePresenter>();Assert.That(p.LoadDuel(path),Is.True,p.DuelSaveMessage);Assert.That(p.Duel.CaptureSave().checksum,Is.EqualTo(hash));Assert.That(p.Duel.Realm.LastBattle.origins,Is.EqualTo(w.Realm.LastBattle.origins));File.Delete(path);LogAssert.NoUnexpectedReceived();
        }
    }
}

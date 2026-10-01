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
    public class CrossroadsPresentationTests
    {
        private string path;
        [SetUp] public void Setup()=>path=Path.Combine(Application.temporaryCachePath,"duel-"+Guid.NewGuid().ToString("N")+".json");
        [TearDown] public void Cleanup(){if(File.Exists(path))File.Delete(path);}
        [UnityTest] public IEnumerator MidRefreshSaveRecreateLoadHandoffAndDeterministicContinuation()
        {
            yield return SceneManager.LoadSceneAsync("TacticalGraybox",LoadSceneMode.Single);yield return null;
            var p=Object.FindAnyObjectByType<BattlePresenter>();p.StartDuel(Side.East);p.Duel.Move(Side.East,8);p.Duel.EndActivation(Side.East);p.DuelChanged();
            Assert.That(p.HudRoot.Q("duel-handoff").style.display.value,Is.EqualTo(DisplayStyle.Flex));
            Assert.That(p.HudRoot.Q<Button>("duel-end").enabledInHierarchy,Is.False);Assert.That(p.SaveDuel(path),Is.True);
            var original=p.Duel;string hash=original.CaptureSave().checksum;
            yield return SceneManager.LoadSceneAsync("TacticalGraybox",LoadSceneMode.Single);yield return null;p=Object.FindAnyObjectByType<BattlePresenter>();
            Assert.That(p.LoadDuel(path),Is.True,p.DuelSaveMessage);Assert.That(p.Duel.CaptureSave().checksum,Is.EqualTo(hash));
            foreach(var s in new[]{original,p.Duel}){s.ContinueHandoff(Side.West);s.Move(Side.West,6);s.EndActivation(Side.West);}
            p.DuelChanged();Assert.That(p.Duel.CaptureSave().checksum,Is.EqualTo(original.CaptureSave().checksum));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator TacticalHotseatPhysicalEscapeReturnsSameRosterAndSaves()
        {
            yield return SceneManager.LoadSceneAsync("TacticalGraybox",LoadSceneMode.Single);yield return null;
            var p=Object.FindAnyObjectByType<BattlePresenter>();p.StartDuel();var s=p.Duel;s.Move(Side.West,7);s.EndActivation(Side.West);s.ContinueHandoff(Side.East);s.Move(Side.East,11);s.Attack(Side.East);p.DuelChanged();
            Assert.That(p.PlayerVsAi,Is.False);p.SetPlayerVsAi(true);Assert.That(p.PlayerVsAi,Is.False);
            Assert.That(p.SaveDuel(path),Is.False);Assert.That(p.HudRoot.Q("duel-world").style.display.value,Is.EqualTo(DisplayStyle.None));
            var ids=s.East.Formation.Members.Select(c=>c.CharacterId).ToArray();
            for(int i=0;i<300&&!p.State.Outcome.IsEnded;i++)
            {
                var u=p.State.FindUnit(p.State.CurrentUnitId.Value);BattleCommand command=new EndActivationCommand(u.Id);
                if(u.Side==Side.West&&u.MovementRemaining>0)
                {var route=Enumerable.Range(0,17).Select(y=>Pathfinder.FindPath(p.State,u.Id,new GridPosition(0,y))).Where(r=>r.Found&&r.Cost>0).OrderBy(r=>r.Cost).FirstOrDefault();if(route!=null)command=new MoveCommand(u.Id,route.Steps);}
                Assert.That(p.Submit(command).IsApplied,Is.True);
            }
            Assert.That(p.State.Outcome.IsEnded,Is.True);Assert.That(ReplayVerification.Verify(p.Journal.Header,p.Journal.Records,p.Journal.Footer()).Matches,Is.True);
            p.ReturnToWorld();Assert.That(s.East.Node,Is.EqualTo(7));Assert.That(s.West.Formation.LivingMembers.All(c=>c.Status==PersistentCharacterStatus.EscapedSafe),Is.True);
            Assert.That(s.East.Formation.Members.Select(c=>c.CharacterId),Is.EqualTo(ids));Assert.That(p.SaveDuel(path),Is.True);
            Assert.That(p.HudRoot.Q("duel-world").style.display.value,Is.EqualTo(DisplayStyle.Flex));
            Assert.That(p.CombatFeed,Is.Not.Empty,"The completed escape generated readable outcomes.");
            p.StartDuel();s=p.Duel;s.Move(Side.West,7);s.EndActivation(Side.West);s.ContinueHandoff(Side.East);s.Move(Side.East,11);s.Attack(Side.East);p.DuelChanged();
            Assert.That(p.CombatFeed,Is.Empty,"New connected battle must not show the previous battle's outcomes.");
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator EconomicPendingSaveRecreateCompletesOnceAndRecruitEntersBattle()
        {
            yield return SceneManager.LoadSceneAsync("TacticalGraybox",LoadSceneMode.Single);yield return null;
            var p=Object.FindAnyObjectByType<BattlePresenter>();var data=new CrossroadsScenario(economy:true).CaptureSave();
            var dead=data.west.formation.members[1];dead.hp=0;dead.status=(int)PersistentCharacterStatus.Dead;
            data.west.provisions=13;data.owners[0]=(int)Side.West;data.checksum=data.ComputeHash();File.WriteAllText(path,JsonUtility.ToJson(data));
            Assert.That(p.LoadDuel(path),Is.True);Assert.That(p.HudRoot.Q<Button>("duel-recruit-hw").enabledInHierarchy,Is.True);
            Assert.That(p.Duel.Recruit(Side.West,UnitProfileId.HumanWarriorTI),Is.True);p.DuelChanged();var original=p.Duel;
            Assert.That(p.SaveDuel(path),Is.True);yield return SceneManager.LoadSceneAsync("TacticalGraybox",LoadSceneMode.Single);yield return null;
            p=Object.FindAnyObjectByType<BattlePresenter>();Assert.That(p.LoadDuel(path),Is.True,p.DuelSaveMessage);
            foreach(var s in new[]{original,p.Duel}){s.ContinueHandoff(Side.East);s.EndActivation(Side.East);s.ContinueHandoff(Side.West);}
            p.DuelChanged();Assert.That(p.Duel.CaptureSave().checksum,Is.EqualTo(original.CaptureSave().checksum));
            Assert.That(p.HudRoot.Q<Label>("duel-economy").text,Does.Contain("Gold 275").And.Contain("Keep Food 30"));
            Assert.That(p.Duel.West.Formation.Members.Last().CharacterId,Is.EqualTo("duel-West-recruit-1"));
            p.Duel.Move(Side.West,7);p.Duel.EndActivation(Side.West);p.Duel.ContinueHandoff(Side.East);p.Duel.Move(Side.East,11);p.Duel.Attack(Side.East);p.DuelChanged();
            Assert.That(p.Duel.Encounter.Ids.Values,Does.Contain("duel-West-recruit-1"));Assert.That(p.PlayerVsAi,Is.False);
            Assert.That(p.Duel.West.Gold,Is.EqualTo(275));Assert.That(p.Duel.West.KeepFood,Is.EqualTo(30));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator IncompatiblePartASaveAndTamperedEconomyLeaveLiveScenarioUnchanged()
        {
            yield return SceneManager.LoadSceneAsync("TacticalGraybox",LoadSceneMode.Single);yield return null;
            var p=Object.FindAnyObjectByType<BattlePresenter>();p.StartDuel();var original=p.Duel;string hash=original.CaptureSave().checksum;
            var d=original.CaptureSave();d.version=1;File.WriteAllText(path,JsonUtility.ToJson(d));Assert.That(p.LoadDuel(path),Is.False);
            d=original.CaptureSave();d.west.gold++;File.WriteAllText(path,JsonUtility.ToJson(d));Assert.That(p.LoadDuel(path),Is.False);
            Assert.That(p.Duel,Is.SameAs(original));Assert.That(p.Duel.CaptureSave().checksum,Is.EqualTo(hash));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator InvalidDiskSaveCannotReplaceLiveStateAndMissionSlotRemainsCompatible()
        {
            yield return SceneManager.LoadSceneAsync("TacticalGraybox",LoadSceneMode.Single);yield return null;
            var p=Object.FindAnyObjectByType<BattlePresenter>();p.StartDuel();var live=p.Duel;string hash=live.CaptureSave().checksum;
            foreach(string value in new[]{"bad json","{}","{\"version\":99}"}){File.WriteAllText(path,value);Assert.That(p.LoadDuel(path),Is.False);Assert.That(p.Duel,Is.SameAs(live));Assert.That(live.CaptureSave().checksum,Is.EqualTo(hash));}
            Assert.That(StrategicSaveFiles.TrySave(new StrategicScenario(),path,out var msg),Is.True,msg);Assert.That(p.LoadDuel(path),Is.False);
            Assert.That(StrategicSaveFiles.TryLoad(path,out var mission,out msg),Is.True,msg);Assert.That(mission.Refresh,Is.EqualTo(1));LogAssert.NoUnexpectedReceived();
        }
    }
}

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
            Assert.That(p.HudRoot.Q("duel-world").style.display.value,Is.EqualTo(DisplayStyle.Flex));LogAssert.NoUnexpectedReceived();
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

using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RPG.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace RPG.Presentation.Tests
{
    public class ReplayPresentationTests
    {
        [UnityTest]
        public IEnumerator JsonlRoundTripNormalOaEscapeAndAiUsesActualCoreState()
        {
            yield return SceneManager.LoadSceneAsync("TacticalGraybox",LoadSceneMode.Single);yield return null;
            var p=UnityEngine.Object.FindAnyObjectByType<BattlePresenter>();
            string directory=Path.Combine(Application.temporaryCachePath,"gate-c-replay-test-"+Guid.NewGuid().ToString("N"));
            try
            {
                foreach(string scenario in new[]{"field","oa","escape","ai"})
                {
                    p.SetPlayerVsAi(false);p.ConfigureFixture(SizeExperimentMap.Field_23x17_Full_9v9);
                    if(scenario=="field")
                    {
                        var id=p.State.CurrentUnitId.Value;var u=p.State.FindUnit(id);
                        p.SelectCell(new GridPosition(u.Position.X+1,u.Position.Y));p.CancelPreview();
                        Assert.That(p.Journal.Session.cancelledPreviews,Is.EqualTo(1));
                        p.Submit(new MoveCommand(id,new[]{new GridPosition(-1,0)}));
                        p.SelectCell(new GridPosition(u.Position.X+1,u.Position.Y));p.ConfirmPreview();
                    }
                    else
                    {
                        var mover=new UnitState(new UnitId(5),scenario=="ai"?Side.East:Side.West,UnitProfile.ElfWarriorTI,
                            new GridPosition(scenario=="escape"?1:4,4),Facing.East,hp:scenario=="oa"?1:32,armor:0);
                        var enemy=new UnitState(new UnitId(7),scenario=="ai"?Side.West:Side.East,UnitProfile.HumanWarriorTI,
                            new GridPosition(scenario=="escape"?12:5,4),Facing.West);
                        p.ConfigureBattle(new[]{mover,enemy},new Battlefield(23,17),2);
                        if(scenario=="ai")
                        {
                            p.SetPlayerVsAi(true);for(int i=0;i<9&&p.IsAiTurn;i++)Assert.That(p.StepAi().IsApplied,Is.True);
                            p.SetPlayerVsAi(false);Assert.That(p.Journal.Records.Any(r=>r.controller=="AI"),Is.True);
                        }
                        else p.Submit(new MoveCommand(mover.Id,new[]{new GridPosition(scenario=="escape"?0:3,4)}));
                    }
                    var state=p.State;string hash=BattleStateHash.Compute(state);
                    string path=p.ExportReplay(directory);Assert.That(File.Exists(path),Is.True);
                    Assert.That(File.Exists(Path.ChangeExtension(path,"session.json")),Is.True);
                    var result=p.VerifyReplay(path);Assert.That(result.Matches,Is.True,result.Message);
                    Assert.That(BattleStateHash.Compute(result.State),Is.EqualTo(hash));Assert.That(p.State,Is.SameAs(state));
                    Assert.That(result.State.Outcome.Reason,Is.EqualTo(state.Outcome.Reason));
                    LogAssert.NoUnexpectedReceived();
                }
            }
            finally { if(Directory.Exists(directory))Directory.Delete(directory,true); }
        }
        [Test]
        public void FileVerifierRejectsMissingFooterAndMalformedInput()
        {
            string dir=Path.Combine(Application.temporaryCachePath,"gate-c-replay-test-"+Guid.NewGuid().ToString("N"));
            try
            {
                var j=new BattleJournal(BattleResolver.StartBattle(SizeExperimentFixture.Units(SizeExperimentMap.Field_23x17_Full_9v9),2,SizeExperimentFixture.Board(SizeExperimentMap.Field_23x17_Full_9v9)).State,"field","test");
                string path=ReplayFiles.Export(j,dir);Assert.That(ReplayFiles.Verify(path).Matches,Is.True);
                File.WriteAllLines(path,File.ReadAllLines(path).Take(1));Assert.That(ReplayFiles.Verify(path).Matches,Is.False);
                File.WriteAllText(path,"not JSON");Assert.That(ReplayFiles.Verify(path).Matches,Is.False);
            }
            finally { if(Directory.Exists(dir))Directory.Delete(dir,true); }
        }
    }
}

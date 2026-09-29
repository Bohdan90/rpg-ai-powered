using System.Collections;
using System.Linq;
using NUnit.Framework;
using RPG.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RPG.Presentation.Tests
{
    public class TacticalAiRetreatPresentationTests
    {
        private static UnitState U(int id,Side side,int x,int y,int hp=40,UnitProfile profile=null,int? armor=null)=>
            new UnitState(new UnitId(id),side,profile??UnitProfile.HumanWarriorTI,new GridPosition(x,y),side==Side.West?Facing.East:Facing.West,hp,armor);
        [UnityTest] public IEnumerator EnemyRetreatsFromWallAndOneCellShortNextActivationThroughCore()
        {
            yield return SceneManager.LoadSceneAsync("TacticalGraybox",LoadSceneMode.Single);yield return null;
            var p=Object.FindAnyObjectByType<BattlePresenter>();
            foreach(bool wall in new[]{false,true})
            {
                p.ConfigureBattle(wall?new[]{U(1,Side.West,13,8,28,UnitProfile.HumanArcherTI),U(2,Side.East,17,8,11,armor:0)}
                    :new[]{U(1,Side.West,3,8),U(2,Side.East,17,8,8)},
                    new Battlefield(23,17,wall?Enumerable.Range(5,7).Select(y=>new GridPosition(19,y)):null),wall?1u:2u);
                p.SetPlayerVsAi(true,Side.East);bool sawShort=false;
                if(wall){p.Submit(new BasicAttackCommand(new UnitId(1),new UnitId(2)));Assert.That(p.State.FindUnit(new UnitId(2)).Hp,Is.EqualTo(1),"Real hit crosses the existing Retreat threshold");}
                for(int i=0;i<30&&!p.State.Outcome.IsEnded;i++)
                {
                    if(p.IsAiTurn){Assert.That(p.StepAi().IsApplied,Is.True);var enemy=p.State.FindUnit(new UnitId(2));
                        if(!wall&&enemy.IsActive&&enemy.Position.X==21&&enemy.MovementRemaining==0)sawShort=true;}
                    else p.EndActivation(null);
                }
                Assert.That(p.State.FindUnit(new UnitId(2)).Status,Is.EqualTo(UnitStatus.Escaped));
                if(!wall)Assert.That(sawShort,Is.True,"Legitimate one-cell-short exhaustion must be visible before the next activation");
                Assert.That(ReplayVerification.Verify(p.Journal.Header,p.Journal.Records,p.Journal.Footer()).Matches,Is.True);
            }
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator PlayerBlocksPredictedExitAndEnemyUsesAnotherLegalCell()
        {
            yield return SceneManager.LoadSceneAsync("TacticalGraybox",LoadSceneMode.Single);yield return null;
            var p=Object.FindAnyObjectByType<BattlePresenter>();
            p.ConfigureBattle(new[]{U(1,Side.West,20,12,28,UnitProfile.HumanArcherTI),U(2,Side.East,21,8,8)},new Battlefield(23,17),2);
            p.SetPlayerVsAi(true,Side.East);
            var projected=BattleResolver.Apply(p.State,new EndActivationCommand(new UnitId(1))).State;
            var chosen=(MoveCommand)TacticalAi.Choose(projected).Command;var target=chosen.Path.Last();
            var path=Pathfinder.FindPath(p.State,new UnitId(1),target);Assert.That(path.Found,Is.True);
            p.Submit(new MoveCommand(new UnitId(1),path.Steps));p.EndActivation(null);
            Assert.That(p.IsAiTurn,Is.True);Assert.That(p.StepAi().IsApplied,Is.True);
            var escaped=p.State.FindUnit(new UnitId(2));Assert.That(escaped.Status,Is.EqualTo(UnitStatus.Escaped));
            Assert.That(escaped.Position,Is.Not.EqualTo(target));
            Assert.That(ReplayVerification.Verify(p.Journal.Header,p.Journal.Records,p.Journal.Footer()).Matches,Is.True);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator HealthyWarriorMakesWallDetourAndEventuallyAttacks()
        {
            yield return SceneManager.LoadSceneAsync("TacticalGraybox",LoadSceneMode.Single);yield return null;
            var p=Object.FindAnyObjectByType<BattlePresenter>();
            p.ConfigureBattle(new[]{U(1,Side.West,12,8),U(2,Side.East,10,8)},
                new Battlefield(23,17,Enumerable.Range(3,11).Select(y=>new GridPosition(11,y))),2);p.SetPlayerVsAi(true,Side.East);
            bool attack=false;
            for(int i=0;i<50&&!p.State.Outcome.IsEnded;i++)
            {
                if(p.IsAiTurn){var d=TacticalAi.Choose(p.State);Assert.That(p.StepAi().IsApplied,Is.True);if(d.Command is BasicAttackCommand){attack=true;break;}}
                else p.EndActivation(null);
            }
            Assert.That(attack,Is.True);Assert.That(ReplayVerification.Verify(p.Journal.Header,p.Journal.Records,p.Journal.Footer()).Matches,Is.True);
            LogAssert.NoUnexpectedReceived();
        }
    }
}

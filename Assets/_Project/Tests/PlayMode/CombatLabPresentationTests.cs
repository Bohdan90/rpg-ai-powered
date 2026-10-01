using System.Collections;
using NUnit.Framework;
using RPG.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
namespace RPG.Presentation.Tests
{
    public class CombatLabPresentationTests
    {
        [UnityTest] public IEnumerator ThreeDirectEntriesLaunchAndSpellCommitsThroughCore()
        {
            yield return SceneManager.LoadSceneAsync("TacticalGraybox",LoadSceneMode.Single);yield return null;
            var p=Object.FindAnyObjectByType<BattlePresenter>();
            foreach(CombatLabMatch match in System.Enum.GetValues(typeof(CombatLabMatch))) {
                p.StartCombatLab(match);Assert.That(p.State.Battlefield.Columns,Is.EqualTo(23));Assert.That(p.State.Units.Count,Is.EqualTo(6));
                Assert.That(p.HudRoot.Q<Button>("lab-"+match),Is.Not.Null);
                foreach(var member in p.State.Units)
                    Assert.That(p.UnitName(member.Id),Does.StartWith(member.Side+" "+member.Profile.Id),"Lab names must use the actual roster, including ordinary warriors.");
            }
            p.ConfigureFixture(SizeExperimentMap.Field_13x9_Control);
            Assert.That(p.Lab,Is.Null);
            Assert.That(p.UnitName(new UnitId(3)),Is.EqualTo(PrototypeFixture.Name(new UnitId(3))));
            p.StartCombatLab(CombatLabMatch.FireVsIce);p.SetPlayerVsAi(false);
            for(int i=0;i<6&&p.State.FindUnit(p.State.CurrentUnitId.Value).Profile!=UnitProfile.FireMageTII;i++)p.EndActivation(null);
            var u=p.State.FindUnit(p.State.CurrentUnitId.Value);Assert.That(u.Profile,Is.EqualTo(UnitProfile.FireMageTII));
            p.SelectSpell(SpellId.Fireball);var hash=BattleStateHash.Compute(p.State);
            p.SelectCell(new GridPosition(12,6));Assert.That(BattleStateHash.Compute(p.State),Is.EqualTo(hash));
            p.ConfirmPreview();Assert.That(p.State.FindUnit(u.Id).FireballUsed,Is.EqualTo(1));
            var replay=ReplayVerification.Verify(p.Journal.Header,p.Journal.Records,p.Journal.Footer());Assert.That(replay.Matches,Is.True,replay.Message);
            p.SetPlayerVsAi(true,Side.West);p.RestartSameSeed();
            for(int i=0;i<10&&!p.State.Outcome.IsEnded;i++){if(p.IsAiTurn)Assert.That(p.StepAi().IsApplied,Is.True);else p.EndActivation(null);}
            LogAssert.NoUnexpectedReceived();
        }
    }
}

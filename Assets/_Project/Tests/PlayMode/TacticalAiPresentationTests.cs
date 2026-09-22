using System.Collections;
using NUnit.Framework;
using RPG.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
namespace RPG.Presentation.Tests
{
    public class TacticalAiPresentationTests
    {
        [UnityTest]
        public IEnumerator ControllerUsesCoreAndReturnsToPlayerWithoutInvalidLoop()
        {
            yield return SceneManager.LoadSceneAsync("TacticalGraybox",LoadSceneMode.Single);yield return null;
            var p=Object.FindAnyObjectByType<BattlePresenter>();
            p.ConfigureBattle(new[]{new UnitState(new UnitId(3),Side.West,UnitProfile.HumanArcherTI,new GridPosition(2,3),Facing.East),
                new UnitState(new UnitId(10),Side.East,UnitProfile.ElfWarriorTI,new GridPosition(5,3),Facing.West)},new Battlefield(23,17),2);
            p.HudRoot.Q<DropdownField>("controller-mode").index=1;
            Assert.That(p.IsAiTurn,Is.True);
            for(int i=0;i<9&&p.IsAiTurn;i++)Assert.That(p.StepAi().IsApplied,Is.True);
            Assert.That(p.IsAiTurn,Is.False);Assert.That(p.AiExplanation,Does.Contain("AI S="));
            p.SetPlayerVsAi(false);p.RestartSameSeed();Assert.That(p.PlayerVsAi,Is.False);
            LogAssert.NoUnexpectedReceived();
        }
    }
}

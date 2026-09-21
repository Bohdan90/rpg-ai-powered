using System.Collections;
using System.Linq;
using NUnit.Framework;
using RPG.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RPG.Presentation.Tests
{
    public class SizeExperimentPresentationTests
    {
        [UnityTest]
        public IEnumerator SelectorLoadsAllFourFixturesAndRestartPreservesSelection()
        {
            yield return SceneManager.LoadSceneAsync("TacticalGraybox",LoadSceneMode.Single); yield return null;
            var p=Object.FindAnyObjectByType<BattlePresenter>();
            var selector=p.HudRoot.Q<DropdownField>("fixture-selector");
            Assert.That(selector.choices.Count,Is.EqualTo(4));
            foreach(SizeExperimentMap map in System.Enum.GetValues(typeof(SizeExperimentMap)))
            {
                selector.value=map.ToString(); yield return null;
                var board=SizeExperimentFixture.Board(map);
                Assert.That(p.State.Battlefield.Columns,Is.EqualTo(board.Columns)); Assert.That(p.VisualUnitCount,Is.EqualTo(10));
                CollectionAssert.AreEqual(SizeExperimentFixture.Units(map).Select(u=>u.Position),p.State.Units.Select(u=>u.Position));
                var rng=p.State.RngState;
                var actor=p.State.FindUnit(p.State.CurrentUnitId.Value);
                p.SelectCell(new GridPosition(actor.Position.X+1,actor.Position.Y+1));
                Assert.That(p.HasMovePreview,Is.True); p.ConfirmPreview();
                Assert.That(p.State.FindUnit(actor.Id).Position,Is.Not.EqualTo(actor.Position));
                p.RestartSameSeed(); yield return null;
                Assert.That(p.Fixture,Is.EqualTo(map)); Assert.That(p.State.RngState,Is.EqualTo(rng));
                Assert.That(p.State.FindUnit(actor.Id).Position,Is.EqualTo(actor.Position));
                Assert.That(p.HudRoot.Q<Label>("retreat-info").text,Does.Contain(map>=SizeExperimentMap.Siege_23x17_Tight ? "full legal outer perimeter" : "East edge"));
                LogAssert.NoUnexpectedReceived();
            }
        }
    }
}

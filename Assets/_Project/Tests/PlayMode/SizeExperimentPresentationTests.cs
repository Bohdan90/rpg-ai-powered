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
        public IEnumerator CornerAttackAndOaPreviewReflectSharedCoreRule()
        {
            yield return SceneManager.LoadSceneAsync("TacticalGraybox", LoadSceneMode.Single); yield return null;
            var presenter = Object.FindAnyObjectByType<BattlePresenter>();
            foreach (bool sealedCorner in new[] { false, true })
            {
                var units = new[] {
                    new UnitState(new UnitId(5), Side.West, UnitProfile.ElfWarriorTI, new GridPosition(4,4), Facing.East),
                    new UnitState(new UnitId(7), Side.East, UnitProfile.HumanWarriorTI, new GridPosition(5,5), Facing.West)
                };
                var walls = sealedCorner ? new[] { new GridPosition(5,4), new GridPosition(4,5) } : new[] { new GridPosition(5,4) };
                var board = new Battlefield(walls);
                presenter.ConfigureBattle(units, board, 2);
                var before = presenter.State;
                presenter.SelectCell(units[1].Position);
                Assert.That(presenter.HudRoot.Q<Button>("confirm-command").enabledSelf, Is.EqualTo(!sealedCorner));
                Assert.That(presenter.State, Is.SameAs(before));
                if (!sealedCorner)
                {
                    presenter.ConfirmPreview();
                    Assert.That(presenter.State.FindUnit(units[0].Id).ActionAvailable, Is.False);
                }
                else Assert.That(presenter.PreviewText, Does.Contain("BlockedCorner"));
                presenter.ConfigureBattle(units, board, 2);
                Assert.That(presenter.ThreatCells.ContainsKey(units[0].Position), Is.EqualTo(!sealedCorner));
                presenter.SelectCell(new GridPosition(3,4));
                Assert.That(presenter.OpportunityRiskCount, Is.EqualTo(sealedCorner ? 0 : 1));
                presenter.ConfirmPreview();
                Assert.That(presenter.State.FindUnit(units[0].Id).Position, Is.EqualTo(new GridPosition(3,4)));
                Assert.That(presenter.RecentEvents.Any(e => e.Contains("OpportunityAttackTriggered")), Is.EqualTo(!sealedCorner));
                LogAssert.NoUnexpectedReceived(); yield return null;
            }
        }

        [UnityTest]
        public IEnumerator SelectorLoadsAllComparisonFixturesAndRestartPreservesSelection()
        {
            yield return SceneManager.LoadSceneAsync("TacticalGraybox",LoadSceneMode.Single); yield return null;
            var p=Object.FindAnyObjectByType<BattlePresenter>();
            var selector=p.HudRoot.Q<DropdownField>("fixture-selector");
            Assert.That(selector.choices.Count,Is.EqualTo(5));
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
                Assert.That(p.HudRoot.Q<Label>("retreat-info").text,Does.Contain(SizeExperimentFixture.IsSiege(map) ? "full legal outer perimeter" : "East edge"));
                LogAssert.NoUnexpectedReceived();
            }
        }
    }
}

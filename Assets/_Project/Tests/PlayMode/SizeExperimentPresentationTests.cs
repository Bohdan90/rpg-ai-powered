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
        public IEnumerator EngagedArcherHudShowsFallbackAndDisengagementRestoresBow()
        {
            yield return SceneManager.LoadSceneAsync("TacticalGraybox", LoadSceneMode.Single); yield return null;
            var presenter=Object.FindAnyObjectByType<BattlePresenter>();
            var archer=new UnitState(new UnitId(3),Side.West,UnitProfile.HumanArcherTI,new GridPosition(4,4),Facing.East);
            var elf=new UnitState(new UnitId(10),Side.East,UnitProfile.ElfWarriorTI,new GridPosition(5,4),Facing.East);
            presenter.ConfigureBattle(new[]{archer,elf},new Battlefield(19,13),2);
            presenter.EndActivation(null);
            presenter.SelectCell(elf.Position);
            Assert.That(presenter.PreviewText,Does.Contain("Bow Shot — unavailable: Engaged"));
            Assert.That(presenter.PreviewText,Does.Contain("Melee Strike"));
            Assert.That(presenter.PreviewText,Does.Contain("unguarded hit: 5"));
            presenter.ConfirmPreview();
            Assert.That(presenter.State.FindUnit(elf.Id).Armor,Is.EqualTo(1));
            Assert.That(presenter.State.FindUnit(archer.Id).OpportunityAttackAvailable,Is.False);
            presenter.RestartSameSeed();presenter.EndActivation(null);
            presenter.SelectCell(new GridPosition(3,4));
            Assert.That(presenter.OpportunityRiskCount,Is.EqualTo(1));
            presenter.ConfirmPreview();presenter.SelectCell(elf.Position);
            Assert.That(presenter.PreviewText,Does.Contain("Bow Shot"));
            Assert.That(presenter.PreviewText,Does.Not.Contain("unavailable: Engaged"));
            Assert.That(presenter.PreviewText,Does.Contain("Steady Aim +0 pp"));
            presenter.ConfirmPreview();
            Assert.That(presenter.State.FindUnit(archer.Id).ActionAvailable,Is.False);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ArcherHudUsesRangeTenAndAccuracyOnlyAim()
        {
            yield return SceneManager.LoadSceneAsync("TacticalGraybox", LoadSceneMode.Single); yield return null;
            var presenter = Object.FindAnyObjectByType<BattlePresenter>();
            foreach (int distance in new[] { 10, 11 })
            {
                var shooter = new UnitState(new UnitId(3), Side.West, UnitProfile.HumanArcherTI, new GridPosition(2,2), Facing.East);
                var target = new UnitState(new UnitId(7), Side.East, UnitProfile.HumanWarriorTI, new GridPosition(2+distance,2), Facing.East);
                presenter.ConfigureBattle(new[] { shooter, target }, new Battlefield(19,13), 1);
                presenter.SelectCell(target.Position);
                Assert.That(presenter.HudRoot.Q<Button>("confirm-command").enabledSelf, Is.EqualTo(distance == 10));
                if (distance == 10)
                {
                    Assert.That(presenter.PreviewText, Does.Contain("Effective range 10"));
                    Assert.That(presenter.PreviewText, Does.Contain("Steady Aim +15 pp"));
                    Assert.That(presenter.PreviewText, Does.Contain("Distance -30 pp"));
                    presenter.SelectCell(new GridPosition(2,3)); presenter.ConfirmPreview();
                    presenter.SelectCell(target.Position);
                    Assert.That(presenter.PreviewText, Does.Contain("Effective range 10"));
                    Assert.That(presenter.PreviewText, Does.Contain("Steady Aim +0 pp"));
                    presenter.ConfirmPreview();
                    Assert.That(presenter.State.FindUnit(shooter.Id).ActionAvailable, Is.False);
                }
                else Assert.That(presenter.PreviewText, Does.Contain("OutOfRange"));
                LogAssert.NoUnexpectedReceived();
            }
        }

        [UnityTest]
        public IEnumerator RangedPreviewAllowsExposedCornersButRejectsInteriorAndSealedVertices()
        {
            yield return SceneManager.LoadSceneAsync("TacticalGraybox", LoadSceneMode.Single); yield return null;
            var presenter = Object.FindAnyObjectByType<BattlePresenter>();
            for (int geometry = 0; geometry < 3; geometry++)
            {
                var walls = geometry == 0 ? new[] { new GridPosition(9,7) }
                    : geometry == 1 ? new[] { new GridPosition(10,7) }
                    : new[] { new GridPosition(9,7), new GridPosition(10,8) };
                var shooter = new UnitState(new UnitId(3), Side.West, UnitProfile.HumanArcherTI, new GridPosition(12,5), Facing.West);
                var target = new UnitState(new UnitId(10), Side.East, UnitProfile.ElfWarriorTI, new GridPosition(9,8), Facing.West);
                presenter.ConfigureBattle(new[] { shooter, target }, new Battlefield(19,13,walls), 2);
                presenter.EndActivation(null); // EW's ordinary first activation.
                Assert.That(presenter.State.CurrentUnitId, Is.EqualTo(shooter.Id));
                var before = presenter.State;
                presenter.SelectCell(target.Position);
                Assert.That(presenter.State, Is.SameAs(before));
                Assert.That(presenter.HudRoot.Q<Button>("confirm-command").enabledSelf, Is.EqualTo(geometry == 0));
                Assert.That(presenter.PreviewText, Does.Contain(geometry != 0 ? "BlockedLineOfSight" : "LoS / corner: clear"));
                if (geometry == 0)
                {
                    presenter.ConfirmPreview();
                    Assert.That(presenter.State.FindUnit(shooter.Id).ActionAvailable, Is.False);
                }
                LogAssert.NoUnexpectedReceived(); yield return null;
            }
        }

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

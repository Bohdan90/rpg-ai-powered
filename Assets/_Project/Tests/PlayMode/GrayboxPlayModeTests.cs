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
    public class GrayboxPlayModeTests
    {
        private BattlePresenter presenter;
        private static readonly UnitId One = new UnitId(1), Two = new UnitId(2);
        private static GridPosition P(int x, int y) => new GridPosition(x, y);
        private static UnitState Unit(int id, UnitProfile profile, int x, int y, Facing facing = Facing.East) =>
            new UnitState(new UnitId(id), id == 1 ? Side.West : Side.East, profile, P(x, y), facing);
        private static string Snapshot(BattleState state) => state.Round + "|" + state.CurrentUnitId + "|" + state.InitialSeed + "|" + state.RngState + "|"
            + string.Join(";", state.Units.Select(u => u.Id + ":" + u.Position.X + "," + u.Position.Y + "," + u.Hp + "," + u.Armor
                + "," + u.Facing + "," + u.Status + "," + u.ActionAvailable + "," + u.MovementRemaining + "," + u.MovementSpentThisActivation + "," + u.IsDefending + "," + u.TieKey));

        [UnitySetUp]
        public IEnumerator LoadGraybox()
        {
            yield return SceneManager.LoadSceneAsync("TacticalGraybox", LoadSceneMode.Single);
            yield return null;
            presenter = Object.FindAnyObjectByType<BattlePresenter>();
            Assert.That(presenter, Is.Not.Null); Assert.That(presenter.State, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator SceneBootsAndVisualCountMatchesCore()
        {
            Assert.That(presenter.VisualUnitCount, Is.EqualTo(10));
            Assert.That(presenter.VisualUnitCount, Is.EqualTo(presenter.State.Units.Count(u => u.IsActive)));
            Assert.That(Object.FindObjectsByType<Camera>().Length, Is.EqualTo(1));
            Assert.That(presenter.HudRoot.Q<Button>("confirm-command"), Is.Not.Null);
            Assert.That(presenter.State.Battlefield.SolidCells.Count, Is.EqualTo(3));
            LogAssert.NoUnexpectedReceived();
            Assert.That(presenter.GetComponent<UIDocument>().panelSettings, Is.Not.Null);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AttackOutcomeHudDistinguishesContactGuardArmorHpAndSpillFromCoreEvents()
        {
            // Same frontal HW duel; stable seeds after the two initiative draws.
            var seeds = new uint[] { 5, 31, 1, 1, 1 };
            var armor = new[] { 16, 16, 16, 0, 4 };
            var expected = new[] { "Failed contact", "Guard blocked", "Armor damage 12", "HP damage 12", "HP damage 8" };
            for (int i = 0; i < seeds.Length; i++)
            {
                presenter.ConfigureBattle(new[] {
                    Unit(1, UnitProfile.HumanWarriorTI, 2, 2),
                    new UnitState(Two, Side.East, UnitProfile.HumanWarriorTI, P(3,2), Facing.West, armor: armor[i])
                }, Battlefield.ControlMap, seeds[i]);
                while (presenter.State.CurrentUnitId != One) presenter.EndActivation(null);
                Assert.That(presenter.LastAttackOutcome, Is.Empty);
                var before = Snapshot(presenter.State);
                presenter.SelectCell(P(3,2));
                Assert.That(Snapshot(presenter.State), Is.EqualTo(before));
                Assert.That(presenter.LastAttackOutcome, Is.Empty);
                presenter.ConfirmPreview();
                string text = presenter.HudRoot.Q<Label>("attack-outcome").text;
                Assert.That(text, Does.Contain(expected[i]).And.Not.Contain("Dodge"));
                Assert.That(presenter.RecentEvents.Any(e => e.Contains(expected[i])), Is.True);
                if (i < 2)
                {
                    Assert.That(presenter.State.FindUnit(Two).Hp, Is.EqualTo(40));
                    Assert.That(presenter.State.FindUnit(Two).Armor, Is.EqualTo(16));
                    Assert.That(text, Does.Not.Contain("Armor damage").And.Not.Contain("HP damage"));
                }
                if (i == 2) { Assert.That(presenter.State.FindUnit(Two).Hp, Is.EqualTo(40)); Assert.That(text, Does.Not.Contain("HP damage")); }
                if (i == 4) Assert.That(text, Does.Contain("Armor damage 4 (4 → 0)").And.Contain("HP damage 8 (40 → 32)"));
                // Invalid commands and selection cannot fabricate or clear a resolved outcome.
                var resolved = Snapshot(presenter.State);
                presenter.Submit(new BasicAttackCommand(One, Two));
                Assert.That(Snapshot(presenter.State), Is.EqualTo(resolved));
                Assert.That(presenter.HudRoot.Q<Label>("attack-outcome").text, Is.EqualTo(text));
                presenter.RestartSameSeed();
                Assert.That(presenter.LastAttackOutcome, Is.Empty);
                yield return null;
            }
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator LegalSelectionAndConfirmationChangesCoreWithoutPreviewMutation()
        {
            var state = presenter.State; var actor = state.FindUnit(state.CurrentUnitId.Value);
            var destination = Enumerable.Range(0, Battlefield.Width).SelectMany(x => Enumerable.Range(0, Battlefield.Height).Select(y => P(x, y)))
                .First(p => Pathfinder.FindPath(state, actor.Id, p).Cost == 1);
            string before = Snapshot(state);
            presenter.SelectCell(destination);
            Assert.That(Snapshot(presenter.State), Is.EqualTo(before));
            Assert.That(presenter.HudRoot.Q<Button>("confirm-command").enabledSelf, Is.True);
            presenter.ConfirmPreview();
            Assert.That(presenter.State.FindUnit(actor.Id).Position, Is.EqualTo(destination));
            Assert.That(presenter.State.FindUnit(actor.Id).MovementRemaining, Is.EqualTo(actor.MovementRemaining - 1));
            Assert.That(presenter.State.RngState, Is.EqualTo(state.RngState)); yield return null;
        }

        [UnityTest]
        public IEnumerator BoardPointerAndConfirmButtonDispatchRealUiEvents()
        {
            yield return null;
            var state = presenter.State; var actor = state.FindUnit(state.CurrentUnitId.Value);
            var destination = Enumerable.Range(0, Battlefield.Width).SelectMany(x => Enumerable.Range(0, Battlefield.Height).Select(y => P(x, y)))
                .First(p => Pathfinder.FindPath(state, actor.Id, p).Cost == 1);
            var surface = presenter.HudRoot.Q<VisualElement>("board-input");
            Assert.That(surface.worldBound.width, Is.GreaterThan(0));
            var camera = Object.FindAnyObjectByType<Camera>();
            var viewport = camera.WorldToViewportPoint(BattleGridView.World(destination));
            var point = new Vector2(surface.worldBound.xMin + viewport.x * surface.worldBound.width,
                surface.worldBound.yMin + (1 - viewport.y) * surface.worldBound.height);
            using (var click = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, mousePosition = point, button = 0 }))
                surface.SendEvent(click);
            Assert.That(presenter.PreviewText, Does.Contain("Move to (" + destination.X + "," + destination.Y + ")"));
            Assert.That(presenter.State, Is.SameAs(state));
            var button = presenter.HudRoot.Q<Button>("confirm-command");
            button.Focus();
            using (var submit = NavigationSubmitEvent.GetPooled()) button.SendEvent(submit);
            yield return null;
            Assert.That(presenter.State.FindUnit(actor.Id).Position, Is.EqualTo(destination));
        }

        [UnityTest]
        public IEnumerator InvalidPresentationCommandAndMovedViewCannotChangeCoreTruth()
        {
            var state = presenter.State; var actor = state.FindUnit(state.CurrentUnitId.Value);
            var token = presenter.transform.Find("Battle views/" + PrototypeFixture.Name(actor.Id));
            token.position = Vector3.one * 999;
            Assert.That(presenter.State, Is.SameAs(state));
            var invalid = presenter.Submit(new MoveCommand(actor.Id, new[] { P(-1, -1) }));
            Assert.That(invalid.IsApplied, Is.False); Assert.That(presenter.State, Is.SameAs(state));
            Assert.That(token.localPosition, Is.EqualTo(BattleGridView.World(actor.Position)));
            Assert.That(presenter.Message, Does.Contain("Rejected by Core")); yield return null;
        }

        [UnityTest]
        public IEnumerator EndActivationUsesCoreNextActorAndRestartRecreatesFixture()
        {
            string initial = Snapshot(presenter.State);
            var expected = BattleResolver.Apply(presenter.State, new EndActivationCommand(presenter.State.CurrentUnitId.Value));
            presenter.EndActivation(null);
            Assert.That(Snapshot(presenter.State), Is.EqualTo(Snapshot(expected.State)));
            presenter.Defend(); presenter.EndActivation(Facing.North);
            presenter.RestartSameSeed();
            Assert.That(Snapshot(presenter.State), Is.EqualTo(initial));
            Assert.That(presenter.VisualUnitCount, Is.EqualTo(10)); yield return null;
        }

        [UnityTest]
        public IEnumerator OrthogonalDiagonalMoveAttackAndOrderRejectionsUseController()
        {
            presenter.ConfigureBattle(new[] { Unit(1, UnitProfile.ElfWarriorTI, 2, 2), Unit(2, UnitProfile.HumanArcherTI, 5, 3) }, Battlefield.ControlMap, 1);
            presenter.SelectCell(P(3, 2)); presenter.ConfirmPreview();
            Assert.That(presenter.State.FindUnit(One).Position, Is.EqualTo(P(3, 2)));
            presenter.SelectCell(P(4, 3)); presenter.ConfirmPreview();
            Assert.That(presenter.State.FindUnit(One).Position, Is.EqualTo(P(4, 3)));
            Assert.That(presenter.State.FindUnit(One).Facing, Is.EqualTo(Facing.NorthEast));
            var moved = presenter.State; presenter.Defend(); Assert.That(presenter.State, Is.SameAs(moved));
            Assert.That(presenter.Message, Does.Contain("MovementAlreadySpent"));
            presenter.SelectCell(P(5, 3)); presenter.ConfirmPreview();
            Assert.That(presenter.State.FindUnit(Two).Hp, Is.EqualTo(21));
            Assert.That(presenter.RecentEvents.Any(e => e.Contains("ContactRolled")), Is.True);
            Assert.That(presenter.Submit(new MoveCommand(One, new[] { P(4, 4) })).Error, Is.EqualTo(CommandError.NoAction));
            yield return null;
        }

        [UnityTest]
        public IEnumerator CentralWallRejectsMoveAndCoreRouteGoesAroundIt()
        {
            presenter.ConfigureBattle(new[] { Unit(1, UnitProfile.ElfWarriorTI, 5, 4), Unit(2, UnitProfile.HumanWarriorTI, 9, 4) }, Battlefield.BaseMap, 1);
            var before = presenter.State;
            Assert.That(presenter.Submit(new MoveCommand(One, new[] { P(6, 4) })).Error, Is.EqualTo(CommandError.SolidCell));
            Assert.That(presenter.State, Is.SameAs(before));
            presenter.SelectCell(P(7, 4)); Assert.That(presenter.PreviewText, Does.Contain("Cost 6"));
            presenter.ConfirmPreview();
            Assert.That(presenter.State.FindUnit(One).Position, Is.EqualTo(P(7, 4)));
            Assert.That(presenter.State.FindUnit(One).MovementRemaining, Is.Zero); yield return null;
        }

        [UnityTest]
        public IEnumerator RangedPreviewAndResolutionRespectSolidLosAndUnitCover()
        {
            var units = new[] { Unit(1, UnitProfile.HumanArcherTI, 2, 4), Unit(2, UnitProfile.HumanWarriorTI, 8, 4) };
            presenter.ConfigureBattle(units, Battlefield.BaseMap, 1);
            presenter.SelectCell(P(8, 4)); Assert.That(presenter.PreviewText, Does.Contain("BlockedLineOfSight"));
            Assert.That(presenter.HudRoot.Q<Button>("confirm-command").enabledSelf, Is.False);
            Assert.That(presenter.Submit(new BasicAttackCommand(One, Two)).Error, Is.EqualTo(CommandError.BlockedLineOfSight));
            presenter.ConfigureBattle(units.Concat(new[] { Unit(3, UnitProfile.HumanWarriorTI, 5, 4) }), Battlefield.ControlMap, 1);
            presenter.SelectCell(P(8, 4));
            Assert.That(presenter.PreviewText, Does.Contain("Light Cover: -15 pp Accuracy"));
            Assert.That(presenter.PreviewText, Does.Contain("Contact 70%"));
            Assert.That(presenter.HudRoot.Q<Button>("confirm-command").enabledSelf, Is.True);
            presenter.ConfirmPreview();
            Assert.That(presenter.State.FindUnit(One).ActionAvailable, Is.False);
            presenter.ConfigureBattle(units, Battlefield.ControlMap, 1);
            presenter.SelectCell(P(8, 4)); Assert.That(presenter.PreviewText, Does.Contain("Contact 85%"));
            presenter.ConfirmPreview();
            Assert.That(presenter.State.FindUnit(Two).Armor, Is.EqualTo(6)); yield return null;
        }

        [UnityTest]
        public IEnumerator ElfFrontalEvasionPreviewChangesAfterFlank()
        {
            presenter.ConfigureBattle(new[] { Unit(1, UnitProfile.HumanWarriorTI, 2, 2), Unit(2, UnitProfile.ElfWarriorTI, 3, 3, Facing.West) }, Battlefield.ControlMap, 1);
            presenter.EndActivation(null); // Elf has the higher initiative.
            presenter.SelectCell(P(3, 3)); Assert.That(presenter.PreviewText, Does.Contain("Frontal Evasion: applies"));
            Assert.That(presenter.PreviewText, Does.Contain("Contact 70%"));
            presenter.SelectCell(P(4, 3)); presenter.ConfirmPreview();
            presenter.SelectCell(P(3, 3)); Assert.That(presenter.PreviewText, Does.Contain("Frontal Evasion: does not apply"));
            Assert.That(presenter.PreviewText, Does.Contain("Contact 80%")); yield return null;
        }

        [UnityTest]
        public IEnumerator DeathRemovesTokenAndRestartRestoresIt()
        {
            presenter.ConfigureBattle(new[] {
                Unit(1, UnitProfile.ElfWarriorTI, 2, 2),
                new UnitState(Two, Side.East, UnitProfile.HumanArcherTI, P(3, 2), Facing.East, hp: 1, armor: 0)
            }, Battlefield.ControlMap, 1);
            Assert.That(presenter.VisualUnitCount, Is.EqualTo(2));
            presenter.SelectCell(P(3, 2)); presenter.ConfirmPreview();
            Assert.That(presenter.State.FindUnit(Two).Status, Is.EqualTo(UnitStatus.Dead));
            Assert.That(presenter.VisualUnitCount, Is.EqualTo(1));
            presenter.RestartSameSeed(); Assert.That(presenter.VisualUnitCount, Is.EqualTo(2));
            yield return null;
        }

        [UnityTest]
        public IEnumerator FullRoundActivatesBothSidesAndDefendReflectsCore()
        {
            var seen = new System.Collections.Generic.HashSet<UnitId>();
            var sides = new System.Collections.Generic.HashSet<Side>();
            for (int i = 0; i < 10; i++)
            {
                var actor = presenter.State.FindUnit(presenter.State.CurrentUnitId.Value);
                Assert.That(seen.Add(actor.Id), Is.True); sides.Add(actor.Side);
                if (i == 0) { presenter.Defend(); Assert.That(presenter.State.FindUnit(actor.Id).IsDefending, Is.True); }
                presenter.EndActivation(null);
            }
            Assert.That(sides.Count, Is.EqualTo(2)); Assert.That(presenter.State.Round, Is.EqualTo(2)); yield return null;
        }
    }
}

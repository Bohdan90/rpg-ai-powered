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
    public class EngagementPresentationTests
    {
        private BattlePresenter presenter;
        private static readonly UnitId Mover = new UnitId(5), Enemy = new UnitId(7);
        private static GridPosition P(int x, int y) => new GridPosition(x, y);
        private static UnitState Unit(int id, Side side, UnitProfile profile, int x, int y, int? hp = null, int? armor = null) =>
            new UnitState(new UnitId(id), side, profile, P(x,y), Facing.West, hp, armor);
        private void Engagement(uint seed = 2, int? hp = null, int? armor = null) => presenter.ConfigureBattle(new[] {
            Unit(5, Side.West, UnitProfile.ElfWarriorTI, 2,2,hp,armor),
            Unit(7, Side.East, UnitProfile.HumanWarriorTI, 3,2)
        }, Battlefield.ControlMap, seed);
        private static string Snapshot(BattleState s) => s.RngState+"|"+s.Round+"|"+s.CurrentUnitId+"|"+s.Outcome.Reason
            +string.Join(";",s.Units.Select(u=>u.Id+":"+u.Position.X+","+u.Position.Y+","+u.Hp+","+u.Armor+","+u.Status+","+u.OpportunityAttackAvailable+","+u.MovementRemaining+","+u.ActionAvailable));
        [UnitySetUp]
        public IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("TacticalGraybox"); yield return null;
            presenter = Object.FindAnyObjectByType<BattlePresenter>();
        }

        [UnityTest]
        public IEnumerator PreviewMatchesCoreExposuresAndRequiresExplicitConfirmation()
        {
            Engagement(); var state=presenter.State; string before=Snapshot(state);
            presenter.SelectCell(P(1,2));
            var expected=OpportunityAttackPreview.Query(state,new MoveCommand(Mover,Pathfinder.FindPath(state,Mover,P(1,2)).Steps));
            Assert.That(presenter.OpportunityRiskCount,Is.EqualTo(1));
            Assert.That(presenter.MovementRisk.Exposures.Select(e=>e.StepIndex),Is.EqualTo(expected.Exposures.Select(e=>e.StepIndex)));
            Assert.That(presenter.MovementRisk.Exposures[0].Threats.Select(t=>t.Responder),Is.EqualTo(expected.Exposures[0].Threats.Select(t=>t.Responder)));
            Assert.That(presenter.PreviewText,Does.Contain("Step 1 (2,2) → (1,2)"));
            Assert.That(presenter.HudRoot.Q<Label>("oa-warning").text,Does.Contain("may trigger 1"));
            Assert.That(presenter.HudRoot.Q<Button>("confirm-command").text,Does.Contain("accept 1 OA"));
            Assert.That(Snapshot(state),Is.EqualTo(before)); Assert.That(presenter.State,Is.SameAs(state));
            presenter.CancelPreview();
            Assert.That(presenter.OpportunityRiskCount,Is.Zero);
            Assert.That(presenter.HudRoot.Q<Button>("confirm-command").enabledSelf,Is.False);
            Assert.That(presenter.State,Is.SameAs(state));
            presenter.SelectCell(P(1,2)); presenter.ConfirmPreview();
            Assert.That(presenter.State.FindUnit(Mover).Position,Is.EqualTo(P(1,2)));
            Assert.That(presenter.State.FindUnit(Enemy).OpportunityAttackAvailable,Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SafePathsHaveNoRiskAndSpentAvailabilityComesFromCore()
        {
            Engagement(); presenter.SelectCell(P(2,3));
            Assert.That(presenter.OpportunityRiskCount,Is.Zero);
            Assert.That(presenter.HudRoot.Q<Button>("confirm-command").text,Is.EqualTo("Confirm Move"));
            presenter.SelectCell(P(1,2)); presenter.ConfirmPreview();
            presenter.SelectCell(P(2,2)); presenter.ConfirmPreview();
            presenter.SelectCell(P(1,2));
            Assert.That(presenter.OpportunityRiskCount,Is.Zero);
            Assert.That(presenter.PreviewText,Does.Contain("OA spent — cannot react"));
            Assert.That(presenter.Hover(P(2,2)),Does.Contain("OA spent"));
            Assert.That(presenter.HudRoot.Q<Label>("oa-warning").style.display.value,Is.EqualTo(DisplayStyle.None));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ZocOverlayUsesCoreSourcesAndArchersAreNotSources()
        {
            Engagement();
            foreach(var pair in presenter.ThreatCells)
                Assert.That(pair.Value,Is.EqualTo(ZoneOfControl.Sources(presenter.State,Side.West,pair.Key)));
            Assert.That(presenter.ThreatCells[P(2,2)],Contains.Item(Enemy));
            presenter.ConfigureBattle(new[] {Unit(5,Side.West,UnitProfile.ElfWarriorTI,2,2),Unit(8,Side.East,UnitProfile.HumanArcherTI,3,2)},Battlefield.ControlMap,2);
            Assert.That(presenter.ThreatCells,Is.Empty);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MultipleRespondersAppearInCoreOrderAndEventsStayChronological()
        {
            presenter.ConfigureBattle(new[] {Unit(5,Side.West,UnitProfile.ElfWarriorTI,2,2),
                Unit(7,Side.East,UnitProfile.HumanWarriorTI,3,1),Unit(10,Side.East,UnitProfile.ElfWarriorTI,3,3)},Battlefield.ControlMap,2);
            presenter.SelectCell(P(1,2));
            Assert.That(presenter.OpportunityRiskCount,Is.EqualTo(2));
            Assert.That(presenter.PreviewText.IndexOf("E EW-Flanker"),Is.LessThan(presenter.PreviewText.IndexOf("E HW-Infantry")));
            var expected=BattleResolver.Apply(presenter.State,new MoveCommand(Mover,new[]{P(1,2)}));
            presenter.ConfirmPreview();
            Assert.That(Snapshot(presenter.State),Is.EqualTo(Snapshot(expected.State)));
            var text=presenter.HudRoot.Q<Label>("battle-events").text;
            Assert.That(text.IndexOf("OpportunityAttackTriggered"),Is.LessThan(text.IndexOf("StepMoved")));
            Assert.That(text,Does.Contain("BEFORE the exit step"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator DeathInterruptionShowsUnexecutedStepAndEliminationPanel()
        {
            Engagement(hp:1,armor:0); presenter.SelectCell(P(1,3)); presenter.ConfirmPreview();
            Assert.That(presenter.State.FindUnit(Mover).Status,Is.EqualTo(UnitStatus.Dead));
            Assert.That(presenter.State.FindUnit(Mover).Position,Is.EqualTo(P(2,2)));
            Assert.That(presenter.VisualUnitCount,Is.EqualTo(1));
            Assert.That(presenter.HudRoot.Q<Label>("battle-events").text,Does.Contain("exit step NOT executed; remaining path cancelled"));
            Assert.That(presenter.HudRoot.Q<Label>("outcome-summary").text,Does.Contain("Winner: East").And.Contain("Result: Eliminated").And.Contain("Dead:"));
            Assert.That(presenter.HudRoot.Q<Button>("end-activation").enabledSelf,Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RetreatEdgesFollowCoreSideAndEnemyEdgeIsNotAnEscape()
        {
            presenter.ConfigureBattle(new[] {Unit(10,Side.East,UnitProfile.ElfWarriorTI,11,4),Unit(1,Side.West,UnitProfile.HumanWarriorTI,5,7)},Battlefield.ControlMap,2);
            Assert.That(presenter.HudRoot.Q<Label>("retreat-info").text,Does.Contain("East edge"));
            presenter.SelectCell(P(12,4)); Assert.That(presenter.PreviewEscapes,Is.True);
            Assert.That(presenter.State.Battlefield.IsRetreatZone(Side.East,P(12,4)),Is.True);
            presenter.ConfigureBattle(new[] {Unit(5,Side.West,UnitProfile.ElfWarriorTI,11,4),Unit(7,Side.East,UnitProfile.HumanWarriorTI,5,7)},Battlefield.ControlMap,2);
            presenter.SelectCell(P(12,4)); Assert.That(presenter.PreviewEscapes,Is.False);
            Assert.That(presenter.Hover(P(12,4)),Does.Contain("NOT your escape"));
            presenter.ConfirmPreview(); Assert.That(presenter.State.FindUnit(Mover).Status,Is.EqualTo(UnitStatus.Active));
            yield return null;
        }

        [UnityTest]
        public IEnumerator RiskyRetreatIsConditionalAndEscapedIsSafeNotDead()
        {
            Engagement(); presenter.SelectCell(P(0,2));
            Assert.That(presenter.OpportunityRiskCount,Is.EqualTo(1)); Assert.That(presenter.PreviewEscapes,Is.True);
            Assert.That(presenter.PreviewText,Does.Contain("if the unit survives").And.Contain("Pools AFTER any OA"));
            presenter.ConfirmPreview();
            Assert.That(presenter.State.FindUnit(Mover).Status,Is.EqualTo(UnitStatus.Escaped));
            Assert.That(presenter.VisualUnitCount,Is.EqualTo(1));
            Assert.That(presenter.HudRoot.Q<Label>("escaped-list").text,Does.Contain("W EW-Flanker").And.Contain("HP 26 / Armor 0"));
            var summary=presenter.HudRoot.Q<Label>("outcome-summary").text;
            Assert.That(summary,Does.Contain("Winner: East").And.Contain("Loser: West").And.Contain("Result: Withdrawal").And.Contain("Dead:\nNone"));
            Assert.That(presenter.HudRoot.Q<VisualElement>("outcome-panel").style.display.value,Is.EqualTo(DisplayStyle.Flex));
            yield return null;
        }

        [UnityTest]
        public IEnumerator CommanderPartialRetreatDoesNotShowDefeatAndRestartRestoresEverything()
        {
            presenter.ConfigureBattle(new[] {Unit(1,Side.West,UnitProfile.HumanWarriorTI,1,4),
                Unit(2,Side.West,UnitProfile.HumanWarriorTI,8,1),Unit(7,Side.East,UnitProfile.HumanWarriorTI,8,7)},Battlefield.ControlMap,2);
            while(presenter.State.CurrentUnitId!=new UnitId(1)) presenter.EndActivation(null);
            presenter.SelectCell(P(0,4)); presenter.ConfirmPreview();
            Assert.That(presenter.State.Outcome.IsEnded,Is.False);
            Assert.That(presenter.HudRoot.Q<VisualElement>("outcome-panel").style.display.value,Is.EqualTo(DisplayStyle.None));
            Assert.That(presenter.HudRoot.Q<Label>("escaped-list").text,Does.Contain("Commander"));
            Assert.That(presenter.HudRoot.Q<Label>("activation-queue").text,Does.Not.Contain("Commander"));
            presenter.RestartSameSeed();
            Assert.That(presenter.State.Units.All(u=>u.IsActive&&u.OpportunityAttackAvailable),Is.True);
            Assert.That(presenter.HudRoot.Q<Label>("escaped-list").text,Does.Contain("None"));
            Assert.That(presenter.VisualUnitCount,Is.EqualTo(3));
            yield return null;
        }

        [UnityTest]
        public IEnumerator OutcomeRestartButtonRestoresInitialStateAndNoCommandCanChangeFinishedState()
        {
            Engagement(); string initial=Snapshot(presenter.State);
            presenter.SelectCell(P(0,2)); presenter.ConfirmPreview(); var ended=presenter.State;
            presenter.SelectCell(P(4,4)); presenter.ConfirmPreview();
            Assert.That(presenter.State,Is.SameAs(ended));
            Assert.That(presenter.HudRoot.Q<Button>("defend").enabledSelf,Is.False);
            var button=presenter.HudRoot.Q<Button>("outcome-restart");button.Focus();
            using(var e=NavigationSubmitEvent.GetPooled())button.SendEvent(e);
            yield return null;
            Assert.That(Snapshot(presenter.State),Is.EqualTo(initial));
            Assert.That(presenter.HudRoot.Q<VisualElement>("outcome-panel").style.display.value,Is.EqualTo(DisplayStyle.None));
            Assert.That(presenter.OpportunityRiskCount,Is.Zero);
        }
    }
}

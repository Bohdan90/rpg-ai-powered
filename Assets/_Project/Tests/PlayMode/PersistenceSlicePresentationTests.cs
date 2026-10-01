using System.Collections;
using NUnit.Framework;
using RPG.Core;
using RPG.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace RPG.Presentation.Tests
{
    public class PersistenceSlicePresentationTests
    {
        [UnityTest]
        public IEnumerator DeveloperFlowStartsOnAcceptedFieldWithPersistentSummary()
        {
            yield return SceneManager.LoadSceneAsync("TacticalGraybox",LoadSceneMode.Single);yield return null;
            var presenter=Object.FindAnyObjectByType<BattlePresenter>(); presenter.StartPersistenceSlice();
            Assert.That(presenter.PersistenceActive,Is.True);Assert.That(presenter.State.Battlefield.Columns,Is.EqualTo(23));Assert.That(presenter.State.Battlefield.Rows,Is.EqualTo(17));
            Assert.That(presenter.HudRoot.Q<Label>("persistence-summary").text,Does.Contain("Battle 1"));
            Assert.That(presenter.HudRoot.Q<Button>("persistence-continue").enabledSelf,Is.False);
            Assert.That(presenter.RangedReach,Is.Not.Null);LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator PreviewAndRejectedCommandDoNotResolveOrMutatePersistence()
        {
            yield return SceneManager.LoadSceneAsync("TacticalGraybox",LoadSceneMode.Single);yield return null;
            var presenter=Object.FindAnyObjectByType<BattlePresenter>(); presenter.StartPersistenceSlice();
            string summary=presenter.PersistenceSummary; uint rng=presenter.State.RngState; var actor=presenter.State.CurrentUnitId.Value;
            presenter.SelectCell(new GridPosition(4,8));
            Assert.That(presenter.PersistenceSummary,Is.EqualTo(summary));Assert.That(presenter.State.RngState,Is.EqualTo(rng));
            var rejected=presenter.Submit(new MoveCommand(actor,new[]{new GridPosition(-1,-1)}));
            Assert.That(rejected.IsApplied,Is.False);Assert.That(presenter.PersistenceSummary,Is.EqualTo(summary));Assert.That(presenter.State.RngState,Is.EqualTo(rng));
            LogAssert.NoUnexpectedReceived();
        }
    }
}

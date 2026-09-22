using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using RPG.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace RPG.Presentation.Tests
{
    public class RangedReachPresentationTests
    {
        private static UnitState Archer(UnitProfile profile=null) => new UnitState(new UnitId(3),Side.West,profile??UnitProfile.HumanArcherTI,new GridPosition(2,2),Facing.East);
        private static UnitState Enemy(int x=12,int y=2) => new UnitState(new UnitId(7),Side.East,UnitProfile.HumanWarriorTI,new GridPosition(x,y),Facing.West);
        private static IEnumerator Load()
        { yield return SceneManager.LoadSceneAsync("TacticalGraybox",LoadSceneMode.Single);yield return null; }

        [UnityTest]
        public IEnumerator ReachIsGeometricPreservesMovementAndSelectionCannotChangeReplay()
        {
            yield return Load();var p=Object.FindAnyObjectByType<BattlePresenter>();
            p.ConfigureBattle(new[]{Archer(),Enemy()},new Battlefield(23,17,new[]{new GridPosition(6,2)}),2);
            var before=p.State;var hash=BattleStateHash.Compute(before);var rng=before.RngState;
            Assert.That(p.RangedReach,Does.Contain(new GridPosition(12,2)));
            Assert.That(p.RangedReach,Has.No.Member(new GridPosition(13,2)));
            Assert.That(p.RangedReach,Does.Contain(new GridPosition(6,2)),"Reach is geometric even on solid cells.");
            Assert.That(p.ReachableCells,Does.Contain(new GridPosition(3,2)));
            Assert.That(p.RangedVisualCount,Is.EqualTo(p.RangedReach.Count));
            p.SelectCell(new GridPosition(12,2));
            Assert.That(p.PreviewText,Does.Contain("BlockedLineOfSight"));
            Assert.That(p.HudRoot.Q<Button>("confirm-command").enabledSelf,Is.False);
            p.SelectCell(new GridPosition(3,2));p.Hover(new GridPosition(13,2));p.CancelPreview();
            Assert.That(p.State,Is.SameAs(before));Assert.That(p.State.RngState,Is.EqualTo(rng));
            Assert.That(BattleStateHash.Compute(p.State),Is.EqualTo(hash));
            Assert.That(ReplayVerification.Verify(p.Journal.Header,p.Journal.Records,p.Journal.Footer()).Matches,Is.True);
            p.SelectCell(new GridPosition(3,2));p.ConfirmPreview();
            Assert.That(p.RangedReach,Does.Contain(new GridPosition(13,2)),"Reach follows actual accepted movement.");
            Assert.That(ReplayVerification.Verify(p.Journal.Header,p.Journal.Records,p.Journal.Footer()).Matches,Is.True);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]
        public IEnumerator RangeComesFromProfileDataAndMeleeHasNoBowOverlay()
        {
            yield return Load();var p=Object.FindAnyObjectByType<BattlePresenter>();
            // Test-only synthetic profile: avoid adding production tuning/weapon APIs just for this assertion.
            var shorter=(UnitProfile)Activator.CreateInstance(typeof(UnitProfile),BindingFlags.Instance|BindingFlags.NonPublic,null,
                new object[]{UnitProfileId.HumanArcherTI,28,4,4,12,80,5,0,10,7},null);
            foreach(var profile in new[]{shorter,UnitProfile.HumanArcherTI})
            {
                p.ConfigureBattle(new[]{Archer(profile),Enemy()},new Battlefield(23,17),2);
                Assert.That(p.RangedReach,Does.Contain(new GridPosition(2+profile.Range,2)));
                Assert.That(p.RangedReach,Has.No.Member(new GridPosition(3+profile.Range,2)));
                Assert.That(p.RangedReachMessage,Does.Contain("reach "+profile.Range));
            }
            p.ConfigureBattle(new[]{new UnitState(new UnitId(1),Side.West,UnitProfile.HumanWarriorTI,new GridPosition(2,2),Facing.East),Enemy()},new Battlefield(23,17),2);
            Assert.That(p.RangedReach,Is.Empty);Assert.That(p.RangedVisualCount,Is.Zero);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]
        public IEnumerator EngagedAndSpentActionHideBowAndCoreStillControlsAttack()
        {
            yield return Load();var p=Object.FindAnyObjectByType<BattlePresenter>();
            p.ConfigureBattle(new[]{Archer(),Enemy(3)},new Battlefield(23,17),2);
            Assert.That(p.RangedReach,Is.Empty);Assert.That(p.RangedVisualCount,Is.Zero);
            Assert.That(p.RangedReachMessage,Does.Contain("unavailable: Engaged"));
            p.SelectCell(new GridPosition(3,2));Assert.That(p.PreviewText,Does.Contain("Melee Strike"));
            Assert.That(p.HudRoot.Q<Button>("confirm-command").enabledSelf,Is.True);
            p.ConfigureBattle(new[]{Archer(),Enemy()},new Battlefield(23,17),2);
            p.SelectCell(new GridPosition(12,2));Assert.That(p.HudRoot.Q<Button>("confirm-command").enabledSelf,Is.True);
            p.ConfirmPreview();Assert.That(p.State.FindUnit(new UnitId(3)).ActionAvailable,Is.False);
            Assert.That(p.RangedReach,Is.Empty);Assert.That(p.RangedReachMessage,Does.Contain("Action spent"));
            Assert.That(ReplayVerification.Verify(p.Journal.Header,p.Journal.Records,p.Journal.Footer()).Matches,Is.True);
            p.RestartSameSeed();Assert.That(p.RangedReach.Count,Is.GreaterThan(0));
            p.SetPlayerVsAi(true,Side.West);Assert.That(p.RangedReach,Is.Empty,"No enemy range overlay during AI activation.");
            p.SetPlayerVsAi(false);Assert.That(p.RangedReach.Count,Is.GreaterThan(0));
            LogAssert.NoUnexpectedReceived();
        }
    }
}

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
        public IEnumerator SelectedSiegeFixturesPreserveRangeTenAndPreviewDeterminism()
        {
            yield return Load();var p=Object.FindAnyObjectByType<BattlePresenter>();
            foreach(SizeExperimentMap map in Enum.GetValues(typeof(SizeExperimentMap)))
            {
                if(!SizeExperimentFixture.IsDirectionalSiege(map))continue;
                p.ConfigureFixture(map);
                for(int i=0;i<27 && p.State.FindUnit(p.State.CurrentUnitId.Value).Profile.Id!=UnitProfileId.HumanArcherTI;i++)
                    p.EndActivation(null);
                var actor=p.State.FindUnit(p.State.CurrentUnitId.Value);
                Assert.That(actor.Profile.Id,Is.EqualTo(UnitProfileId.HumanArcherTI));
                Assert.That(actor.Profile.Range,Is.EqualTo(10));
                var board=p.State.Battlefield;
                Assert.That(board.Columns,Is.EqualTo(41));Assert.That(board.Rows,Is.EqualTo(39));
                var expected=(from x in Enumerable.Range(0,board.Columns)
                              from y in Enumerable.Range(0,board.Rows)
                              let cell=new GridPosition(x,y)
                              where actor.Position.DistanceTo(cell)<=10 && cell!=actor.Position
                              select cell).ToArray();
                CollectionAssert.AreEquivalent(expected,p.RangedReach);
                Assert.That(p.RangedVisualCount,Is.EqualTo(expected.Length));
                Assert.That(p.ReachableCells.Count,Is.GreaterThan(0));
                var before=p.State;var hash=BattleStateHash.Compute(before);var rng=before.RngState;
                var destination=p.ReachableCells.First(c=>c!=actor.Position);
                p.SelectCell(destination);p.Hover(expected.Last());p.CancelPreview();
                Assert.That(p.State,Is.SameAs(before));Assert.That(p.State.RngState,Is.EqualTo(rng));
                Assert.That(BattleStateHash.Compute(p.State),Is.EqualTo(hash));
                Assert.That(ReplayVerification.Verify(p.Journal.Header,p.Journal.Records,p.Journal.Footer()).Matches,Is.True);
                p.SelectCell(destination);p.ConfirmPreview();
                Assert.That(p.State.FindUnit(actor.Id).Position,Is.EqualTo(destination));
                Assert.That(ReplayVerification.Verify(p.Journal.Header,p.Journal.Records,p.Journal.Footer()).Matches,Is.True);
                yield return null;
                LogAssert.NoUnexpectedReceived();
            }
        }

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
            Assert.That(p.HudRoot.Q<Button>("confirm-command").enabledSelf,Is.True);
            Assert.That(p.HasApproachPreview,Is.True);
            var actor=p.State.CurrentUnitId.Value;var target=p.State.OccupantAt(new GridPosition(12,2)).Id;
            Assert.That(BattleResolver.Validate(p.State,new BasicAttackCommand(actor,target)),Is.EqualTo(CommandError.BlockedLineOfSight));
            var approach=MeleeApproachPreview.QueryBow(p.State,actor,target);var moved=BattleResolver.Apply(p.State,approach.Movement);
            Assert.That(moved.IsApplied,Is.True);Assert.That(BattleResolver.Validate(moved.State,approach.Attack),Is.EqualTo(CommandError.None));
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

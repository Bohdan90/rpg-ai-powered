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
        public IEnumerator DensityNamesAndViewsResetWithoutGhosts()
        {
            yield return SceneManager.LoadSceneAsync("TacticalGraybox",LoadSceneMode.Single);yield return null;
            var p=Object.FindAnyObjectByType<BattlePresenter>();
            foreach(var map in new[]{SizeExperimentMap.Field_23x17_Full_9v9,SizeExperimentMap.Siege_35x27_Full_9v9})
            {
                p.ConfigureFixture(map);yield return null;
                Assert.That(p.VisualUnitCount,Is.EqualTo(18));
                Assert.That(p.State.Units.Count(u=>PrototypeFixture.Name(u.Id).Contains("Commander")),Is.EqualTo(2));
                Assert.That(p.State.Units.Select(u=>PrototypeFixture.Name(u.Id)).Distinct().Count(),Is.EqualTo(18));
                p.RestartSameSeed();Assert.That(p.VisualUnitCount,Is.EqualTo(18));
                p.ConfigureFixture(SizeExperimentMap.Field_13x9_Control);yield return null;
                Assert.That(p.VisualUnitCount,Is.EqualTo(10));
            }
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator LargerSiegeFixturesLoadFrameAndUseCoreCrossingPreview()
        {
            yield return SceneManager.LoadSceneAsync("TacticalGraybox",LoadSceneMode.Single);yield return null;
            var presenter=Object.FindAnyObjectByType<BattlePresenter>();
            foreach(var map in new[]{SizeExperimentMap.Siege_31x25_Medium,SizeExperimentMap.Siege_35x27_Large})
            {
                presenter.HudRoot.Q<DropdownField>("fixture-selector").value=map.ToString();yield return null;
                Assert.That(presenter.Fixture,Is.EqualTo(map));Assert.That(presenter.VisualUnitCount,Is.EqualTo(10));
                var b=presenter.State.Battlefield;int cx=b.Columns/2,cy=b.Rows/2;
                presenter.FitBoard();
                var camera=presenter.GetComponentInChildren<Camera>();
                foreach(var pos in new[]{new Vector3(0,0,0),new Vector3(b.Columns-1,0,b.Rows-1)})
                {
                    var v=camera.WorldToViewportPoint(pos);
                    Assert.That(v.x,Is.InRange(0f,1f));Assert.That(v.y,Is.InRange(0f,1f));
                }
                presenter.SelectCell(new GridPosition(8,cy));presenter.ConfirmPreview();
                Assert.That(presenter.State.FindUnit(new UnitId(5)).Position,Is.EqualTo(new GridPosition(8,cy)));
                presenter.RestartSameSeed();Assert.That(presenter.VisualUnitCount,Is.EqualTo(10));
                presenter.ConfigureBattle(new[]{
                    new UnitState(new UnitId(5),Side.West,UnitProfile.ElfWarriorTI,new GridPosition(cx-7,cy),Facing.East),
                    new UnitState(new UnitId(7),Side.East,UnitProfile.HumanWarriorTI,new GridPosition(cx+2,cy),Facing.West)
                },b,2);
                presenter.SelectCell(new GridPosition(cx-4,cy));
                Assert.That(presenter.HudRoot.Q<Button>("confirm-command").enabledSelf,Is.True);
                presenter.ConfirmPreview();Assert.That(presenter.State.FindUnit(new UnitId(5)).Position,Is.EqualTo(new GridPosition(cx-4,cy)));
                LogAssert.NoUnexpectedReceived();
            }
        }

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
                Assert.That(presenter.HudRoot.Q<Button>("confirm-command").enabledSelf, Is.True);
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
                else {
                    Assert.That(presenter.PreviewText, Does.Contain("OutOfRange"));Assert.That(presenter.HasApproachPreview,Is.True);
                    Assert.That(BattleResolver.Validate(presenter.State,new BasicAttackCommand(shooter.Id,target.Id)),Is.EqualTo(CommandError.OutOfRange));
                    presenter.ConfirmPreview();Assert.That(presenter.State.FindUnit(shooter.Id).Position.DistanceTo(target.Position),Is.EqualTo(10));Assert.That(presenter.State.FindUnit(shooter.Id).ActionAvailable,Is.False);
                }
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
                Assert.That(presenter.HudRoot.Q<Button>("confirm-command").enabledSelf, Is.EqualTo(geometry!=2));
                Assert.That(BattleResolver.Validate(presenter.State,new BasicAttackCommand(shooter.Id,target.Id)),Is.EqualTo(geometry==0?CommandError.None:CommandError.BlockedLineOfSight));
                if(geometry==2)Assert.That(presenter.HasApproachPreview,Is.False,"Sealed geometry has no reachable shooting position within remaining Movement.");
                if(geometry==1){Assert.That(presenter.HasApproachPreview,Is.True);var approach=MeleeApproachPreview.QueryBow(presenter.State,shooter.Id,target.Id);var moved=BattleResolver.Apply(presenter.State,approach.Movement);Assert.That(moved.IsApplied,Is.True);Assert.That(BattleResolver.Validate(moved.State,approach.Attack),Is.EqualTo(CommandError.None));}
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
                Assert.That(BattleResolver.PreviewAttack(before,new BasicAttackCommand(units[0].Id,units[1].Id)).Error,Is.EqualTo(sealedCorner?CommandError.BlockedCorner:CommandError.None));
                Assert.That(presenter.HasApproachPreview,Is.EqualTo(sealedCorner));
                Assert.That(presenter.HudRoot.Q<Button>("confirm-command").enabledSelf, Is.True);
                Assert.That(presenter.State, Is.SameAs(before));
                if (!sealedCorner)
                {
                    presenter.ConfirmPreview();
                    Assert.That(presenter.State.FindUnit(units[0].Id).ActionAvailable, Is.False);
                }
                else {
                    var approach=MeleeApproachPreview.Query(before,units[0].Id,units[1].Id);
                    Assert.That(approach,Is.Not.Null);var at=units[0].Position;
                    foreach(var step in approach.Movement.Path){Assert.That(MovementRules.ValidateStep(before,units[0].Id,at,step),Is.EqualTo(CommandError.None));at=step;}
                    presenter.ConfirmPreview();Assert.That(presenter.State.FindUnit(units[0].Id).Position,Is.EqualTo(at));Assert.That(presenter.State.FindUnit(units[0].Id).ActionAvailable,Is.False);
                    Assert.That(presenter.Journal.Records.Count,Is.EqualTo(2));
                }
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
            Assert.That(selector.choices.Count,Is.EqualTo(15));
            foreach(SizeExperimentMap map in System.Enum.GetValues(typeof(SizeExperimentMap)))
            {
                selector.value=map.ToString(); yield return null;
                var board=SizeExperimentFixture.Board(map);
                Assert.That(p.State.Battlefield.Columns,Is.EqualTo(board.Columns)); Assert.That(p.VisualUnitCount,Is.EqualTo(SizeExperimentFixture.Units(map).Length));
                CollectionAssert.AreEqual(SizeExperimentFixture.Units(map).Select(u=>u.Position),p.State.Units.Select(u=>u.Position));
                var rng=p.State.RngState;
                var actor=p.State.FindUnit(p.State.CurrentUnitId.Value);
                var destination=(from x in Enumerable.Range(0,board.Columns) from y in Enumerable.Range(0,board.Rows) let cell=new GridPosition(x,y) where cell!=actor.Position && !board.IsRetreatZone(actor,cell) && Pathfinder.FindPath(p.State,actor.Id,cell).Found select cell).First();
                p.SelectCell(destination);
                Assert.That(p.HasMovePreview,Is.True); p.ConfirmPreview();
                Assert.That(p.State.FindUnit(actor.Id).Position,Is.Not.EqualTo(actor.Position));
                p.RestartSameSeed(); yield return null;
                Assert.That(p.Fixture,Is.EqualTo(map)); Assert.That(p.State.RngState,Is.EqualTo(rng));
                Assert.That(p.State.FindUnit(actor.Id).Position,Is.EqualTo(actor.Position));
                Assert.That(p.HudRoot.Q<Label>("retreat-info").text,Does.Contain(SizeExperimentFixture.IsSiege(map) ? "full legal outer perimeter" : "East edge"));
                if(map==SizeExperimentMap.Siege_41x39_NorthSouth_18v9)
                {
                    for(int i=0;i<27 && p.State.FindUnit(p.State.CurrentUnitId.Value).Side!=Side.East;i++)p.EndActivation(null);
                    var text=p.HudRoot.Q<Label>("retreat-info").text;
                    Assert.That(text,Does.Contain("North / South"));Assert.That(text,Does.Not.Contain("West edge"));
                }
                LogAssert.NoUnexpectedReceived();
            }
        }
    }
}

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
    public class SpellUxPresentationTests
    {
        static IEnumerator Open(){yield return SceneManager.LoadSceneAsync("TacticalGraybox",LoadSceneMode.Single);yield return null;}
        static BattlePresenter P=>Object.FindAnyObjectByType<BattlePresenter>();
        static void Actor(UnitProfile profile){for(int i=0;i<30&&P.State.FindUnit(P.State.CurrentUnitId.Value).Profile!=profile;i++)P.EndActivation(null);Assert.That(P.State.FindUnit(P.State.CurrentUnitId.Value).Profile,Is.EqualTo(profile));}
        static UnitState U(int id,UnitProfile p,Side side,int x,int y)=>new UnitState(new UnitId(id),side,p,new GridPosition(x,y),side==Side.West?Facing.East:Facing.West);
        [UnityTest] public IEnumerator FireArmorSelectsAllyAndInvalidEnemyDoesNotSpend()
        {
            yield return Open();P.ConfigureBattle(new[]{U(1,UnitProfile.FireMageTI,Side.West,8,8),U(2,UnitProfile.HumanWarriorTI,Side.East,10,9),U(3,UnitProfile.HumanWarriorTI,Side.West,8,9)},new Battlefield(23,17),2);Actor(UnitProfile.FireMageTI);P.SelectSpell(SpellId.FireArmor);var hash=BattleStateHash.Compute(P.State);
            P.HoverCell(new GridPosition(10,9));P.ClickCell(new GridPosition(10,9));P.ConfirmPreview();Assert.That(BattleStateHash.Compute(P.State),Is.EqualTo(hash));
            P.CancelPreview();P.SelectSpell(SpellId.FireArmor);P.HoverCell(new GridPosition(8,9));Assert.That(P.PreviewText,Does.Not.Contain("Self only"));Assert.That(P.SpellEnvelope.Count,Is.EqualTo(49));Assert.That(P.SpellFootprint,Is.EqualTo(new[]{new GridPosition(8,9)}));
            P.ClickCell(new GridPosition(8,9));P.ConfirmPreview();Assert.That(P.State.FindUnit(new UnitId(3)).TemporaryBarrier,Is.EqualTo(6));Assert.That(P.State.FindUnit(new UnitId(1)).TemporaryBarrier,Is.Zero);P.CancelPreview();Assert.That(P.SelectedSpell,Is.Null);
        }
        [UnityTest] public IEnumerator LiveHoverIsCoreExactAndEmptyFireballCommitsOnlyOnce()
        {
            yield return Open();P.StartCombatLab(CombatLabMatch.FireVsIce);Actor(UnitProfile.FireMageTII);P.SelectSpell(SpellId.Fireball);var actor=P.State.CurrentUnitId.Value;var cell=new GridPosition(13,6);string hash=BattleStateHash.Compute(P.State);
            P.HoverCell(cell);var core=BattleResolver.PreviewSpell(P.State,new CastCommand(actor,SpellId.Fireball,cell));Assert.That(P.SpellFootprint,Is.EqualTo(core.Cells));Assert.That(P.SpellObstructions,Is.EqualTo(core.BlockedCells));Assert.That(P.State.OccupantAt(cell),Is.Null);Assert.That(BattleStateHash.Compute(P.State),Is.EqualTo(hash));
            P.InspectSpell(SpellId.FireArmor);Assert.That(P.SelectedSpell,Is.EqualTo(SpellId.Fireball));Assert.That(P.SpellEnvelope.Count,Is.EqualTo(49));P.InspectSpell(null);Assert.That(P.SpellEnvelope.Count,Is.GreaterThan(1));
            int before=P.Journal.Records.Count;P.ClickCell(cell);P.HoverCell(new GridPosition(14,6));P.LeaveBoard();Assert.That(P.SpellFootprint,Is.EqualTo(core.Cells));Assert.That(P.Journal.Records.Count,Is.EqualTo(before));P.ConfirmPreview();P.ConfirmPreview();Assert.That(P.Journal.Records.Count,Is.EqualTo(before+1));Assert.That(P.State.FindUnit(actor).FireballUsed,Is.EqualTo(1));
            Assert.That(ReplayVerification.Verify(P.Journal.Header,P.Journal.Records,P.Journal.Footer()).Matches,Is.True);
        }
        [UnityTest] public IEnumerator PrimarySpellClicksAcrossActivationsAndInvalidTargetNeverWalksOrUsesStaff()
        {
            yield return Open();P.ConfigureBattle(new[]{U(1,UnitProfile.FireMageTI,Side.West,8,8),U(2,UnitProfile.HumanWarriorTI,Side.East,12,8),U(3,UnitProfile.HumanWarriorTI,Side.East,10,9)},new Battlefield(23,17),2);Actor(UnitProfile.FireMageTI);
            string hash=BattleStateHash.Compute(P.State);P.ClickCell(new GridPosition(12,8));Assert.That(BattleStateHash.Compute(P.State),Is.EqualTo(hash));Assert.That(P.PreviewText,Does.Contain("Out of range"));
            int n=P.Journal.Records.Count;P.HoverCell(new GridPosition(10,9));P.ClickCell(new GridPosition(10,9));Assert.That(P.Journal.Records.Count,Is.EqualTo(n));P.ClickCell(new GridPosition(10,9));Assert.That(P.Journal.Records.Count,Is.EqualTo(n+1));Assert.That(P.Journal.Records.Last().command.kind,Is.EqualTo(nameof(CastCommand)));P.ConfirmPreview();Assert.That(P.Journal.Records.Count,Is.EqualTo(n+1));
            P.EndActivation(null);Actor(UnitProfile.FireMageTI);Assert.That(P.SelectedSpell,Is.Null);Assert.That(P.PrimarySpell,Is.EqualTo(SpellId.FireStream));P.ClickCell(new GridPosition(10,9));P.ClickCell(new GridPosition(10,9));Assert.That(P.Journal.Records.Last().command.kind,Is.EqualTo(nameof(CastCommand)));
        }
        [UnityTest] public IEnumerator GroundMovementFriendlyInspectionSpecialCancelAndStaffStaySeparate()
        {
            yield return Open();P.ConfigureBattle(new[]{U(1,UnitProfile.IceMageTII,Side.West,8,8),U(2,UnitProfile.HumanWarriorTI,Side.East,9,8),U(3,UnitProfile.HumanWarriorTI,Side.West,8,9)},new Battlefield(23,17),2);Actor(UnitProfile.IceMageTII);
            string hash=BattleStateHash.Compute(P.State);P.ClickCell(new GridPosition(8,9));P.ConfirmPreview();Assert.That(BattleStateHash.Compute(P.State),Is.EqualTo(hash));P.ClickCell(new GridPosition(7,8));Assert.That(P.HasMovePreview,Is.True);P.CancelPreview();
            P.SelectSpell(SpellId.Freeze);P.HoverCell(new GridPosition(9,8));P.CancelPreview();Assert.That(P.PrimarySpell,Is.EqualTo(SpellId.IceShard));Assert.That(P.SelectedSpell,Is.Null);P.SelectStaff();P.ClickCell(new GridPosition(9,8));Assert.That(P.Journal.Records.Count,Is.Zero);P.ConfirmPreview();Assert.That(P.Journal.Records.Last().command.kind,Is.EqualTo(nameof(BasicAttackCommand)));
            P.EndActivation(null);Assert.That(P.StaffSelected,Is.False);Assert.That(P.SelectedSpell,Is.Null);
        }
        [UnityTest] public IEnumerator HoverCameraCancelAndFriendlyFireNeverMutateAndTargetingClearsOnHandoff()
        {
            yield return Open();P.StartCombatLab(CombatLabMatch.FireVsIce);Actor(UnitProfile.FireMageTII);P.SelectSpell(SpellId.Fireball);var pos=P.State.FindUnit(P.State.CurrentUnitId.Value).Position;string hash=BattleStateHash.Compute(P.State);P.HoverCell(pos);Assert.That(P.PreviewText,Does.Contain("friendly fire"));P.ClickCell(pos);P.ConfirmPreview();Assert.That(BattleStateHash.Compute(P.State),Is.EqualTo(hash));P.Zoom(.9f);Assert.That(P.SpellFootprint,Is.Empty);P.HoverCell(pos);P.FitBoard();Assert.That(P.SpellFootprint,Is.Empty);P.CancelPreview();Assert.That(BattleStateHash.Compute(P.State),Is.EqualTo(hash));P.SelectSpell(SpellId.Fireball);P.EndActivation(null);Assert.That(P.SelectedSpell,Is.Null);Assert.That(P.SpellFootprint,Is.Empty);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator SpentSpellActionHidesAimDisablesAttacksAndReturnsNextActivation()
        {
            yield return Open();
            foreach(bool explicitAim in new[]{false,true}) {
                P.ConfigureBattle(new[]{U(1,UnitProfile.FireMageTII,Side.West,8,8),U(2,UnitProfile.HumanWarriorTI,Side.East,10,9)},new Battlefield(23,17),2);Actor(UnitProfile.FireMageTII);
                var cell=new GridPosition(10,9);if(explicitAim)P.SelectSpell(SpellId.FireStream);
                Assert.That(P.SpellEnvelope,Is.Not.Empty);P.ClickCell(cell);P.ClickCell(cell);
                Assert.That(P.State.FindUnit(new UnitId(1)).ActionAvailable,Is.False);
                string hash=BattleStateHash.Compute(P.State);int count=P.Journal.Records.Count;
                Assert.That(BattleResolver.Validate(P.State,new CastCommand(new UnitId(1),SpellId.FireStream,cell)),Is.EqualTo(CommandError.NoAction));
                P.HoverCell(cell);P.InspectSpell(SpellId.FireStream);
                Assert.That(P.SpellEnvelope,Is.Empty,"Spent Action must not advertise cast range");Assert.That(P.SpellFootprint,Is.Empty);
                Assert.That(P.SpellDetails,Does.Contain("Action spent"));
                var root=Object.FindAnyObjectByType<UIDocument>().rootVisualElement;
                Assert.That(root.Q<Button>("spell-FireStream").enabledSelf,Is.False);Assert.That(root.Q<Button>("staff-attack").enabledSelf,Is.False);Assert.That(root.Q<Button>("primary-attack").enabledSelf,Is.False);
                P.ClickCell(cell);P.ConfirmPreview();Assert.That(P.Journal.Records.Count,Is.EqualTo(count));Assert.That(BattleStateHash.Compute(P.State),Is.EqualTo(hash));
                P.CancelPreview();P.ClickCell(new GridPosition(7,8));Assert.That(P.HasMovePreview,Is.False,"Mage movement still obeys the existing Action rule");Assert.That(BattleStateHash.Compute(P.State),Is.EqualTo(hash));P.CancelPreview();
                P.EndActivation(null);Actor(UnitProfile.FireMageTII);Assert.That(P.SpellEnvelope,Is.Not.Empty);Assert.That(root.Q<Button>("spell-FireStream").enabledSelf,Is.True);
                Assert.That(ReplayVerification.Verify(P.Journal.Header,P.Journal.Records,P.Journal.Footer()).Matches,Is.True);
            }
        }
        [UnityTest] public IEnumerator HoverPathTracksCursorPinnedPathSurvivesLeaveAndSecondClickMovesOnce()
        {
            yield return Open();P.ConfigureBattle(new[]{U(1,UnitProfile.HumanWarriorTI,Side.West,8,8),U(2,UnitProfile.HumanWarriorTI,Side.East,15,8)},new Battlefield(23,17),2);
            var actor=P.State.CurrentUnitId.Value;var pos=P.State.FindUnit(actor).Position;var a=new GridPosition(pos.X,pos.Y+1);var b=new GridPosition(pos.X,pos.Y+2);
            string hash=BattleStateHash.Compute(P.State);P.HoverCell(a);Assert.That(P.HasMovePreview,Is.True);Assert.That(P.PinnedCell,Is.Null);string first=P.PreviewText;
            P.ConfirmPreview();Assert.That(BattleStateHash.Compute(P.State),Is.EqualTo(hash),"Hover must never arm Confirm");P.HoverCell(b);Assert.That(P.PreviewText,Is.Not.EqualTo(first));P.LeaveBoard();Assert.That(P.HasMovePreview,Is.False);
            P.ClickCell(a);Assert.That(P.PinnedCell,Is.EqualTo(a));first=P.PreviewText;P.HoverCell(b);P.LeaveBoard();Assert.That(P.PreviewText,Is.EqualTo(first));Assert.That(P.HasMovePreview,Is.True);Assert.That(BattleStateHash.Compute(P.State),Is.EqualTo(hash));
            P.ClickCell(b);Assert.That(P.PinnedCell,Is.EqualTo(b));Assert.That(P.Journal.Records.Count,Is.Zero);P.ClickCell(b);Assert.That(P.State.FindUnit(actor).Position,Is.EqualTo(b));Assert.That(P.Journal.Records.Count,Is.EqualTo(1));P.ConfirmPreview();Assert.That(P.Journal.Records.Count,Is.EqualTo(1));Assert.That(P.PinnedCell,Is.Null);
            Assert.That(ReplayVerification.Verify(P.Journal.Header,P.Journal.Records,P.Journal.Footer()).Matches,Is.True);
        }
        [UnityTest] public IEnumerator EveryAttackPinsBeforeCommitAndTargetChangeNeverAttacksOldTarget()
        {
            yield return Open();
            foreach(var profile in new[]{UnitProfile.FireMageTI,UnitProfile.IceMageTI,UnitProfile.HumanWarriorTI,UnitProfile.HumanArcherTI}) {
                var a=profile.IsArcher?new GridPosition(11,8):new GridPosition(9,8);var b=profile.IsArcher?new GridPosition(10,10):new GridPosition(8,9);
                P.ConfigureBattle(new[]{U(1,profile,Side.West,8,8),U(2,UnitProfile.ElfWarriorTI,Side.East,a.X,a.Y),U(3,UnitProfile.ElfWarriorTI,Side.East,b.X,b.Y)},new Battlefield(23,17),2);Actor(profile);int n=P.Journal.Records.Count;string hash=BattleStateHash.Compute(P.State);
                P.HoverCell(a);Assert.That(P.SpellFootprint,Is.Not.Empty);P.LeaveBoard();Assert.That(P.SpellFootprint,Is.Empty);
                P.ClickCell(a);var footprint=P.SpellFootprint.ToArray();P.HoverCell(b);P.LeaveBoard();P.InspectSpell(SpellId.FireArmor);Assert.That(P.SpellFootprint,Is.EqualTo(footprint));Assert.That(P.PinnedCell,Is.EqualTo(a));
                P.ClickCell(b);Assert.That(P.PinnedCell,Is.EqualTo(b));Assert.That(BattleStateHash.Compute(P.State),Is.EqualTo(hash));Assert.That(P.Journal.Records.Count,Is.EqualTo(n));
                P.ClickCell(b);Assert.That(P.Journal.Records.Count,Is.EqualTo(n+1));P.ConfirmPreview();Assert.That(P.Journal.Records.Count,Is.EqualTo(n+1));Assert.That(P.PinnedCell,Is.Null);
            }
        }
        [UnityTest] public IEnumerator GroundSpellPinCancelActionSwitchAndFriendlyFireConsentRemainExplicit()
        {
            yield return Open();P.ConfigureBattle(new[]{U(1,UnitProfile.FireMageTII,Side.West,8,8),U(2,UnitProfile.HumanWarriorTI,Side.East,12,8)},new Battlefield(23,17),2);Actor(UnitProfile.FireMageTII);
            var empty=new GridPosition(11,7);P.SelectSpell(SpellId.Fireball);string hash=BattleStateHash.Compute(P.State);int n=P.Journal.Records.Count;
            P.HoverCell(empty);P.ConfirmPreview();Assert.That(BattleStateHash.Compute(P.State),Is.EqualTo(hash));P.ClickCell(empty);P.LeaveBoard();Assert.That(P.SpellFootprint,Is.Not.Empty);P.CancelPreview();Assert.That(P.PinnedCell,Is.Null);Assert.That(P.SpellFootprint,Is.Empty);
            P.SelectSpell(SpellId.Fireball);P.ClickCell(empty);P.SelectStaff();Assert.That(P.PinnedCell,Is.Null);P.ClickCell(empty);Assert.That(P.Journal.Records.Count,Is.EqualTo(n));P.CancelPreview();
            P.SelectSpell(SpellId.Fireball);var self=new GridPosition(8,8);P.ClickCell(self);P.ClickCell(self);Assert.That(BattleStateHash.Compute(P.State),Is.EqualTo(hash));Assert.That(P.PreviewText,Does.Contain("Friendly Fire confirmation"));
            P.Repreview(true);P.LeaveBoard();P.ClickCell(self);Assert.That(P.Journal.Records.Count,Is.EqualTo(n+1));Assert.That(P.State.FindUnit(new UnitId(1)).FireballUsed,Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator WarriorPinsApproachThenMovesAndAttacksThroughExistingReplayCommands()
        {
            yield return Open();P.ConfigureBattle(new[]{U(1,UnitProfile.HumanWarriorTI,Side.West,8,8),U(2,UnitProfile.HumanArcherTI,Side.East,12,8)},new Battlefield(23,17),2);Actor(UnitProfile.HumanWarriorTI);
            var cell=new GridPosition(12,8);string hash=BattleStateHash.Compute(P.State);int n=P.Journal.Records.Count;
            P.HoverCell(cell);Assert.That(P.HasApproachPreview,Is.True);P.ConfirmPreview();Assert.That(BattleStateHash.Compute(P.State),Is.EqualTo(hash));
            P.ClickCell(cell);string preview=P.PreviewText;P.HoverCell(new GridPosition(13,9));P.LeaveBoard();Assert.That(P.PreviewText,Is.EqualTo(preview));Assert.That(P.PreviewText,Does.Contain("Approach + Attack"));Assert.That(P.Journal.Records.Count,Is.EqualTo(n));
            P.ClickCell(cell);Assert.That(P.Journal.Records.Count,Is.EqualTo(n+2));Assert.That(P.Journal.Records[n].command.kind,Is.EqualTo(nameof(MoveCommand)));Assert.That(P.Journal.Records[n+1].command.kind,Is.EqualTo(nameof(BasicAttackCommand)));Assert.That(P.State.FindUnit(new UnitId(1)).Position.DistanceTo(cell),Is.EqualTo(1));Assert.That(P.State.FindUnit(new UnitId(1)).ActionAvailable,Is.False);
            P.ConfirmPreview();Assert.That(P.Journal.Records.Count,Is.EqualTo(n+2));Assert.That(ReplayVerification.Verify(P.Journal.Header,P.Journal.Records,P.Journal.Footer()).Matches,Is.True);
        }
        [UnityTest] public IEnumerator LethalOpportunityAttackStopsApproachWithoutFollowupStrike()
        {
            yield return Open();bool found=false;
            for(uint seed=1;seed<=32&&!found;seed++) {
                P.ConfigureBattle(new[]{new UnitState(new UnitId(1),Side.West,UnitProfile.HumanWarriorTI,new GridPosition(8,8),Facing.East,1,0),U(2,UnitProfile.HumanArcherTI,Side.East,12,8),U(3,UnitProfile.ElfWarriorTI,Side.East,7,8)},new Battlefield(23,17),seed);Actor(UnitProfile.HumanWarriorTI);
                var cell=new GridPosition(12,8);P.ClickCell(cell);Assert.That(P.HasApproachPreview,Is.True);Assert.That(P.OpportunityRiskCount,Is.GreaterThan(0));int n=P.Journal.Records.Count;P.ClickCell(cell);
                if(P.State.FindUnit(new UnitId(1)).IsActive)continue;
                found=true;Assert.That(P.Journal.Records.Count,Is.EqualTo(n+1));Assert.That(P.Journal.Records.Last().command.kind,Is.EqualTo(nameof(MoveCommand)));Assert.That(P.State.FindUnit(new UnitId(2)).Hp,Is.EqualTo(UnitProfile.HumanArcherTI.MaxHp));Assert.That(P.HasApproachPreview,Is.False);Assert.That(ReplayVerification.Verify(P.Journal.Header,P.Journal.Records,P.Journal.Footer()).Matches,Is.True);
            }
            Assert.That(found,Is.True,"Fixture must exercise a real lethal OA, not an injected outcome");
        }
        [Test] public void RetainedV1ReplayFilesStillUseRecordedRules()
        {
            int count=0;
            foreach(var folder in new[]{"Docs/Prototype/Evidence/5051/manual-complete","Docs/Prototype/Evidence/SPELL-UX-01"})
            foreach(var path in System.IO.Directory.GetFiles(folder,"gate-c-*.jsonl",System.IO.SearchOption.AllDirectories)) {
                var result=ReplayFiles.Verify(path);Assert.That(result.Matches,Is.True,path+": "+result.Message);count++;
            }
            Assert.That(count,Is.GreaterThanOrEqualTo(17),"Retained evidence must not disappear.");
        }
    }
}

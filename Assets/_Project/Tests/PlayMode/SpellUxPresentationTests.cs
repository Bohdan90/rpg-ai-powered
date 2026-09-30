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
            int n=P.Journal.Records.Count;P.HoverCell(new GridPosition(10,9));P.ClickCell(new GridPosition(10,9));Assert.That(P.Journal.Records.Count,Is.EqualTo(n+1));Assert.That(P.Journal.Records.Last().command.kind,Is.EqualTo(nameof(CastCommand)));P.ConfirmPreview();Assert.That(P.Journal.Records.Count,Is.EqualTo(n+1));
            P.EndActivation(null);Actor(UnitProfile.FireMageTI);Assert.That(P.SelectedSpell,Is.Null);Assert.That(P.PrimarySpell,Is.EqualTo(SpellId.FireStream));P.ClickCell(new GridPosition(10,9));Assert.That(P.Journal.Records.Last().command.kind,Is.EqualTo(nameof(CastCommand)));
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

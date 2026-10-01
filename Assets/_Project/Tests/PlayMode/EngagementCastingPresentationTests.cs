using System.Collections;
using NUnit.Framework;
using RPG.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
namespace RPG.Presentation.Tests
{
    public class EngagementCastingPresentationTests
    {
        static BattlePresenter P=>Object.FindAnyObjectByType<BattlePresenter>();
        static void Setup(UnitProfile mage){P.ConfigureBattle(new[]{new UnitState(new UnitId(1),Side.West,mage,new GridPosition(8,8),Facing.East),new UnitState(new UnitId(2),Side.East,UnitProfile.HumanWarriorTI,new GridPosition(9,8),Facing.West)},new Battlefield(23,17),23);while(P.State.CurrentUnitId.Value.Value!=1)P.EndActivation(null);}
        [UnityTest] public IEnumerator FireButtonsExposeAllowedStreamArmorAndBlockedFireball()
        {
            yield return SceneManager.LoadSceneAsync("TacticalGraybox",LoadSceneMode.Single);yield return null;Setup(UnitProfile.FireMageTII);
            Assert.That(P.HudRoot.Q<Button>("spell-FireStream").enabledSelf,Is.True);Assert.That(P.HudRoot.Q<Button>("spell-FireArmor").enabledSelf,Is.True);Assert.That(P.HudRoot.Q<Button>("spell-Fireball").enabledSelf,Is.False);Assert.That(P.HudRoot.Q<Button>("spell-Fireball").text,Does.Contain("Blocked while Engaged"));Assert.That(P.HudRoot.Q<Button>("staff-attack").enabledSelf,Is.True);
            string hash=BattleStateHash.Compute(P.State);P.SelectSpell(SpellId.Fireball);P.HoverCell(new GridPosition(12,8));P.ClickCell(new GridPosition(12,8));P.ConfirmPreview();Assert.That(P.PreviewText,Does.Contain("Blocked while Engaged"));Assert.That(BattleStateHash.Compute(P.State),Is.EqualTo(hash));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator IceReasonAndEnvelopeRecoverAfterLawfulMovement()
        {
            yield return SceneManager.LoadSceneAsync("TacticalGraybox",LoadSceneMode.Single);yield return null;Setup(UnitProfile.IceMageTII);
            Assert.That(P.HudRoot.Q<Button>("spell-IceShard").enabledSelf,Is.False);Assert.That(P.HudRoot.Q<Button>("spell-Freeze").enabledSelf,Is.False);Assert.That(P.HudRoot.Q<Button>("spell-IceShield").enabledSelf,Is.True);Assert.That(P.HudRoot.Q<Button>("staff-attack").enabledSelf,Is.True);Assert.That(P.SpellDetails,Does.Contain("Blocked while Engaged"));Assert.That(P.SpellEnvelope,Is.Empty);
            P.ClickCell(new GridPosition(7,8));P.ConfirmPreview();Assert.That(P.State.FindUnit(new UnitId(1)).Position,Is.EqualTo(new GridPosition(7,8)));Assert.That(P.HudRoot.Q<Button>("spell-IceShard").enabledSelf,Is.True);Assert.That(P.HudRoot.Q<Button>("spell-Freeze").enabledSelf,Is.True);Assert.That(P.SpellEnvelope,Is.Not.Empty);LogAssert.NoUnexpectedReceived();
        }
    }
}

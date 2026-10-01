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
    public class TacticalCompanionTests
    {
        static BattlePresenter P=>Object.FindAnyObjectByType<BattlePresenter>();
        static UnitState U(int id,UnitProfile p,Side side,int x,int y,int? hp=null)=>new UnitState(new UnitId(id),side,p,new GridPosition(x,y),Facing.East,hp:hp);
        static void Actor(UnitId id){for(int i=0;i<30&&P.State.CurrentUnitId!=id;i++)P.EndActivation(null);Assert.That(P.State.CurrentUnitId,Is.EqualTo(id));}
        static IEnumerator Open(){yield return SceneManager.LoadSceneAsync("TacticalGraybox");yield return null;}
        [UnityTest] public IEnumerator HealTwoClicksChangeCancelUnreachableAndReplay()
        {
            yield return Open();var a=new UnitId(1);
            P.ConfigureBattle(new[]{U(1,UnitProfile.HumanHealerTI,Side.West,5,8,10),U(2,UnitProfile.HumanWarriorTI,Side.West,10,8,10),U(3,UnitProfile.HumanWarriorTI,Side.West,12,8,10),U(4,UnitProfile.HumanWarriorTI,Side.East,19,8)},new Battlefield(23,17),2);Actor(a);
            P.SelectSpell(SpellId.CloseHeal);string hash=BattleStateHash.Compute(P.State);int n=P.Journal.Records.Count;
            P.ClickCell(new GridPosition(10,8));Assert.That(P.HasHealApproachPreview,Is.True);Assert.That(P.PreviewText,Does.Contain("Cast cell (9,8)").And.Contain("Movement 4 / 4"));Assert.That(P.ConfirmActionText,Does.Contain("Heal"));Assert.That(BattleStateHash.Compute(P.State),Is.EqualTo(hash));
            P.ClickCell(new GridPosition(12,8));P.ClickCell(new GridPosition(12,8));Assert.That(P.HasHealApproachPreview,Is.False);Assert.That(P.PreviewText,Does.Contain("Cannot reach and heal this activation"));Assert.That(BattleStateHash.Compute(P.State),Is.EqualTo(hash));
            P.ClickCell(new GridPosition(10,8));P.CancelPreview();Assert.That(BattleStateHash.Compute(P.State),Is.EqualTo(hash));
            P.SelectSpell(SpellId.CloseHeal);P.ClickCell(new GridPosition(10,8));P.LeaveBoard();P.ClickCell(new GridPosition(10,8));P.ConfirmPreview();
            Assert.That(P.Journal.Records.Count,Is.EqualTo(n+2));
            var inspection=P.HudRoot.Q<Label>("unit-inspection");inspection.text="stale";P.CancelPreview();Assert.That(inspection.text,Does.Not.Contain("stale"));Assert.That(P.State.FindUnit(a).Position,Is.EqualTo(new GridPosition(9,8)));Assert.That(P.State.FindUnit(new UnitId(2)).Hp,Is.EqualTo(24));Assert.That(P.State.FindUnit(a).CloseHealUsed,Is.EqualTo(1));
            Assert.That(P.LastAttackOutcome,Does.Contain("Close Heal").And.Contain("HP +14"));Assert.That(ReplayVerification.Verify(P.Journal.Header,P.Journal.Records,P.Journal.Footer()).Matches,Is.True);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator TemporaryProtectionDisplayedAbsorbedExpiredAndResetWithoutFakeWard()
        {
            yield return Open();var mage=new UnitId(1);var ally=new UnitId(2);
            P.ConfigureBattle(new[]{U(1,UnitProfile.FireMageTI,Side.West,5,8),U(2,UnitProfile.HumanWarriorTI,Side.West,6,8),U(3,UnitProfile.FireMageTI,Side.East,8,8)},new Battlefield(23,17),2);Actor(mage);
            var view=P.HudRoot.Q<UnitConditionView>("unit-condition-2");Assert.That(view.Q<Label>("unit-protection").text,Is.Empty);
            P.SelectSpell(SpellId.FireArmor);P.ClickCell(new GridPosition(6,8));P.ClickCell(new GridPosition(6,8));
            Assert.That(view.Q<Label>("unit-protection").text,Is.EqualTo("Temp B 6"));Assert.That(BattlePresenter.ProtectionText(P.State.FindUnit(ally)),Does.Contain("Armor 16/16").And.Contain("Temporary Barrier 6").And.Not.Contain("Ward"));
            Actor(new UnitId(3));P.SelectSpell(SpellId.FireStream);P.ClickCell(new GridPosition(6,8));P.ClickCell(new GridPosition(6,8));
            Assert.That(P.State.FindUnit(ally).TemporaryBarrier,Is.Zero);Assert.That(P.State.FindUnit(ally).Armor,Is.EqualTo(16));Assert.That(view.Q<Label>("unit-protection").text,Is.Empty);
            Assert.That(P.LastAttackOutcome,Does.Contain("Fire Stream").And.Contain("Barrier absorbed 6").And.Contain("HP -4"));
            Assert.That(P.HudRoot.Q<Label>("combat-feed").text,Does.Contain("Fire Stream"));Assert.That(P.RecentEvents.Any(s=>s.Contains("DamageApplied")),Is.True);
            P.RestartSameSeed();Assert.That(view.Q<Label>("unit-protection").text,Is.Empty);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator RecipientExpiryUpdatesUiAndDoesNotRequireAnEventTarget()
        {
            yield return Open();var a=new UnitId(1);var t=new UnitId(2);
            P.ConfigureBattle(new[]{U(1,UnitProfile.FireMageTI,Side.West,5,8),U(2,UnitProfile.HumanWarriorTI,Side.West,6,8),U(3,UnitProfile.HumanWarriorTI,Side.East,19,8)},new Battlefield(23,17),2);Actor(a);
            P.SelectSpell(SpellId.FireArmor);P.ClickCell(new GridPosition(6,8));P.ClickCell(new GridPosition(6,8));
            Actor(t);Assert.That(P.State.FindUnit(t).TemporaryBarrier,Is.EqualTo(6));P.EndActivation(null);Actor(t);
            Assert.That(P.State.FindUnit(t).TemporaryBarrier,Is.Zero);Assert.That(P.HudRoot.Q<UnitConditionView>("unit-condition-2").Q<Label>("unit-protection").text,Is.Empty);
            Assert.That(P.CombatFeed.Any(s=>s.Contains("expired / removed")),Is.True);LogAssert.NoUnexpectedReceived();
        }
        [Test] public void ReadableEventsRetainActorTargetContactAndBothDamagePools()
        {
            var a=new UnitId(1);var b=new UnitId(2);
            var events=new[]{new BattleEvent(BattleEventKind.ArmorLost,1,a,b,3,3,0),new BattleEvent(BattleEventKind.HpLost,1,a,b,4,20,16),new BattleEvent(BattleEventKind.BurnApplied,1,a,b)};
            var text=string.Join("\n",CombatOutcomeText.Format(events,id=>"Unit "+id.Value,new BasicAttackCommand(a,b)));
            Assert.That(text,Does.Contain("Unit 1 → Unit 2").And.Contain("Basic attack").And.Contain("Armor -3 | HP -4 | Burn +1"));
            var retaliation=CombatOutcomeText.Format(new[]{new BattleEvent(BattleEventKind.DamageApplied,1,a,b,12),new BattleEvent(BattleEventKind.BurnApplied,1,b,a)},id=>"Unit "+id.Value,new BasicAttackCommand(a,b));
            Assert.That(string.Join("\n",retaliation),Does.Contain("Unit 2 → Unit 1").And.Contain("Fire Armor retaliation").And.Not.Contain("Basic attack"));
            foreach(var k in new[]{BattleEventKind.AttackMissed,BattleEventKind.GuardSucceeded}) {
                text=string.Join("\n",CombatOutcomeText.Format(new[]{new BattleEvent(k,1,a,b)},id=>"Unit "+id.Value));
                Assert.That(text,Does.Contain(k==BattleEventKind.AttackMissed?"failed contact":"Guard").And.Not.Contain("HP -").And.Not.Contain("Armor -"));
            }
        }
        [UnityTest] public IEnumerator SchoolsKeepDifferentFootprintsAndFriendlyFire()
        {
            yield return Open();
            foreach(var profile in new[]{UnitProfile.FireMageTI,UnitProfile.IceMageTI}) {
                P.ConfigureBattle(new[]{U(1,profile,Side.West,5,8),U(2,UnitProfile.HumanWarriorTI,Side.West,6,8),U(3,UnitProfile.HumanWarriorTI,Side.East,8,8)},new Battlefield(23,17),2);Actor(new UnitId(1));
                P.SelectSpell(profile.IsFireMage?SpellId.FireStream:SpellId.IceShard);string hash=BattleStateHash.Compute(P.State);P.HoverCell(new GridPosition(8,8));
                Assert.That(P.UnitName(new UnitId(3)),Does.Contain("East HumanWarriorTI"));
                Assert.That(P.SpellFootprint.Count,Is.EqualTo(profile.IsFireMage?3:1));Assert.That(P.SpellDetails,Does.Contain(profile.IsFireMage?"SHORT LINE":"LONG SINGLE TARGET"));
                Assert.That(P.SpellEnvelope.Max(c=>c.DistanceTo(new GridPosition(5,8))),Is.EqualTo(profile.IsFireMage?3:8));
                if(profile.IsFireMage)Assert.That(P.PreviewText,Does.Contain("friendly fire"));
                Assert.That(BattleStateHash.Compute(P.State),Is.EqualTo(hash));
            }
            LogAssert.NoUnexpectedReceived();
        }
    }
}

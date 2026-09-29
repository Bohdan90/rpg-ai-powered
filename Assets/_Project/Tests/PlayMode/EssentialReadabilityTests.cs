using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RPG.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RPG.Presentation.Tests
{
    public class EssentialReadabilityTests
    {
        [UnityTest] public IEnumerator BarsFollowResolvedContactGuardArmorHpSpillDeathAndBattleReset()
        {
            yield return SceneManager.LoadSceneAsync("TacticalGraybox",LoadSceneMode.Single);yield return null;
            var p=Object.FindAnyObjectByType<BattlePresenter>();var one=new UnitId(1);var two=new UnitId(2);
            uint[] seeds={5,31,1,1,1,1};int[] armor={16,16,16,0,4,0};
            for(int i=0;i<seeds.Length;i++)
            {
                p.ConfigureBattle(new[]{new UnitState(one,Side.West,UnitProfile.HumanWarriorTI,new GridPosition(2,2),Facing.East),
                    new UnitState(two,Side.East,UnitProfile.HumanWarriorTI,new GridPosition(3,2),Facing.West,hp:i==5?5:40,armor:armor[i])},Battlefield.ControlMap,seeds[i]);
                while(p.State.CurrentUnitId!=one)p.EndActivation(null);
                string before=BattleStateHash.Compute(p.State);p.SelectCell(new GridPosition(3,2));Assert.That(BattleStateHash.Compute(p.State),Is.EqualTo(before));p.ConfirmPreview();
                var unit=p.State.FindUnit(two);var v=p.HudRoot.Q<UnitConditionView>("unit-condition-2");
                string resolved=BattleStateHash.Compute(p.State);
                Assert.That(v.SizeForCell(36),Is.LessThanOrEqualTo(36));
                Assert.That(BattleStateHash.Compute(p.State),Is.EqualTo(resolved));
                Assert.That(v.Q<Label>("hp-value").text,Is.EqualTo("HP "+unit.Hp+"/40"));
                Assert.That(v.Q<Label>("armor-value").text,Is.EqualTo("A "+unit.Armor+"/16"));
                Assert.That(v.Q("hp-fill").style.width.value.value,Is.EqualTo(100f*unit.Hp/40));
                Assert.That(v.Q("armor-fill").style.width.value.value,Is.EqualTo(100f*unit.Armor/16));
                if(i<2){Assert.That(unit.Hp,Is.EqualTo(40));Assert.That(unit.Armor,Is.EqualTo(16));}
                if(i==2)Assert.That(unit.Armor,Is.EqualTo(4));if(i==3)Assert.That(unit.Hp,Is.EqualTo(28));
                if(i==4){Assert.That(unit.Armor,Is.Zero);Assert.That(unit.Hp,Is.EqualTo(32));}
                if(i==5)Assert.That(v.Q<Label>("unit-role").text,Does.Contain("DEAD"));
                p.RestartSameSeed();Assert.That(p.HudRoot.Q<UnitConditionView>("unit-condition-2").Q<Label>("hp-value").text,Is.EqualTo("HP "+(i==5?5:40)+"/40"));
                yield return null;
            }
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator KnownIntelUsesActualRosterHidesDormantAndRemovedAndSurvivesLoad()
        {
            yield return SceneManager.LoadSceneAsync("TacticalGraybox",LoadSceneMode.Single);yield return null;
            var p=Object.FindAnyObjectByType<BattlePresenter>();p.StartStrategicScenario();
            var info=p.HudRoot.Q<Label>("world-intel");
            Assert.That(info.text,Does.Contain("Old Bridge Guard · 3 units\n2 HW · 1 EW").And.Contain("North Patrol · 3 units\n1 HW · 2 EW"));
            Assert.That(info.text,Does.Contain("Portal Area Guard · 4 units\n2 HW · 1 EW · 1 HA").And.Contain("Incursion A · 3 units\n1 HW · 2 EW").And.Not.Contain("Incursion B"));
            Assert.That(p.HudRoot.Q<Button>("world-node-11").text,Does.Not.Contain("Incursion B"));
            p.MoveOnWorld(3);p.MoveOnWorld(8);
            for(int i=0;i<2500&&!p.State.Outcome.IsEnded;i++)p.Submit(TacticalAi.Choose(p.State).Command);
            p.ReturnToWorld();Assert.That(p.World.BridgeGuardDefeated,Is.True);Assert.That(info.text,Does.Contain("Old Bridge Guard · 1 unit\n1 HW").And.Contain("Observed withdrawing"));
            p.World.EndActivation();p.World.EndActivation();p.World.EndActivation();p.WorldChanged();
            Assert.That(info.text,Does.Not.Contain("Old Bridge Guard"));
            Assert.That(info.text,Does.Contain("Incursion B · 3 units\n1 HW · 1 EW · 1 HA"));
            string path=Path.Combine(Application.temporaryCachePath,"wp03d-intel.json"),expected=info.text;
            try {Assert.That(p.SaveStrategic(path),Is.True);p.StartStrategicScenario();Assert.That(p.LoadStrategic(path),Is.True);
                Assert.That(p.HudRoot.Q<Label>("world-intel").text,Is.EqualTo(expected));
                Assert.That(p.HudRoot.Q<Label>("world-status").text,Does.Contain("/ 30").And.Contain("Consumption").And.Contain("Baron Keep"));}
            finally {if(File.Exists(path))File.Delete(path);}
            LogAssert.NoUnexpectedReceived();
        }
    }
}

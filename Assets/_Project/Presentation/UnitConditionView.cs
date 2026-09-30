using RPG.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPG.Presentation
{
    // A projection of Core pools, never an independent health/damage model.
    public sealed class UnitConditionView : VisualElement
    {
        private readonly Label title;
        private readonly VisualElement hpFill, armorFill;
        private readonly Label hpText, armorText;
        public UnitConditionView()
        {
            pickingMode=PickingMode.Ignore;style.position=Position.Absolute;
            style.backgroundColor=new Color(.025f,.04f,.06f,.92f);
            title=new Label {name="unit-role"};title.pickingMode=PickingMode.Ignore;
            title.style.marginTop=title.style.marginBottom=title.style.paddingTop=title.style.paddingBottom=0;
            title.style.flexShrink=0;title.style.height=12;title.style.fontSize=10;title.style.unityTextAlign=TextAnchor.MiddleCenter;Add(title);
            hpFill=Pool("hp",new Color(.16f,.55f,.25f),out hpText);
            armorFill=Pool("armor",new Color(.16f,.40f,.75f),out armorText);
        }
        private VisualElement Pool(string id,Color color,out Label text)
        {
            var track=new VisualElement {name=id+"-track",pickingMode=PickingMode.Ignore};
            track.style.flexShrink=0;track.style.height=11;track.style.overflow=Overflow.Hidden;track.style.backgroundColor=new Color(.14f,.16f,.18f);Add(track);
            var fill=new VisualElement {name=id+"-fill",pickingMode=PickingMode.Ignore};
            fill.style.position=Position.Absolute;fill.style.height=Length.Percent(100);fill.style.backgroundColor=color;track.Add(fill);
            text=new Label {name=id+"-value",pickingMode=PickingMode.Ignore};text.style.fontSize=9;
            text.style.color=Color.white;text.style.unityFontStyleAndWeight=FontStyle.Bold;text.style.unityTextAlign=TextAnchor.MiddleCenter;
            text.style.position=Position.Absolute;text.style.left=text.style.right=text.style.top=text.style.bottom=0;
            text.style.marginTop=text.style.marginBottom=text.style.marginLeft=text.style.marginRight=0;
            text.style.paddingTop=text.style.paddingBottom=text.style.paddingLeft=text.style.paddingRight=0;track.Add(text);return fill;
        }
        public float SizeForCell(float pixels)
        {
            float width=Mathf.Clamp(pixels*.94f,24,86),row=Mathf.Clamp(pixels*.24f,8,11),heading=Mathf.Clamp(pixels*.28f,9,12);
            style.width=width;style.height=heading+2*row;title.style.height=heading;
            title.style.fontSize=width<42?8:10;
            hpFill.parent.style.height=armorFill.parent.style.height=row;
            hpText.style.fontSize=armorText.style.fontSize=width<42?7:9;
            return width;
        }
        public void Refresh(UnitState unit,bool commander,bool current)
        {
            title.text=Mission01Intel.ProfileLabel(unit.Profile)+(commander?"*":"")+(unit.Status==UnitStatus.Dead?" DEAD":unit.IsFrozen?" FRZ":unit.BurnStacks>0?" B"+unit.BurnStacks:unit.IsDefending?" DEF":"");
            title.style.color=unit.Status==UnitStatus.Dead?new Color(1,.4f,.4f):current?new Color(1,.86f,.3f):Color.white;
            hpText.text="HP "+unit.Hp+"/"+unit.Profile.MaxHp;
            armorText.text=unit.TemporaryBarrier>0?"B "+unit.TemporaryBarrier+" · A "+unit.Armor:"A "+unit.Armor+"/"+unit.Profile.MaxArmor;
            hpFill.style.width=Length.Percent(100f*unit.Hp/unit.Profile.MaxHp);
            armorFill.style.width=Length.Percent(unit.Profile.MaxArmor==0?0:100f*unit.Armor/unit.Profile.MaxArmor);
            tooltip=title.text+" · "+hpText.text+" · Armor "+unit.Armor+"/"+unit.Profile.MaxArmor;
            style.display=unit.Status==UnitStatus.Escaped?DisplayStyle.None:DisplayStyle.Flex;
        }
    }
}

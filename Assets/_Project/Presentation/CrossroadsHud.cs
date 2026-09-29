using System;
using System.Linq;
using RPG.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPG.Presentation
{
    public sealed class CrossroadsHud
    {
        public VisualElement Root { get; }
        private readonly BattlePresenter p;
        private readonly VisualElement map,handoff,content;
        private readonly Label status,roster,preview,history,pass,saveStatus;
        private readonly Button move,attack,withdraw,end,accept;
        private int selected;
        public CrossroadsHud(VisualElement parent,BattlePresenter presenter)
        {
            p=presenter;Root=new VisualElement {name="duel-world"};Root.style.position=Position.Absolute;
            Root.style.left=Root.style.right=Root.style.top=Root.style.bottom=0;Root.style.backgroundColor=new Color(.055f,.085f,.11f);parent.Add(Root);
            content=new VisualElement();content.style.flexDirection=FlexDirection.Row;content.style.flexGrow=1;Root.Add(content);
            var left=new ScrollView();left.style.width=Length.Percent(64);content.Add(left);
            Text(left,"CROSSROADS DUEL · strategic Hotseat",22);Text(left,"Open information prototype · claim objectives by ending activation on them · 8 Pressure to win",14);
            map=new VisualElement();map.style.height=400;map.style.flexShrink=0;left.Add(map);map.generateVisualContent+=Draw;
            foreach(var n in CrossroadsScenario.Map.Nodes)
            {
                int id=n.Id;var b=Button(map,n.Name,"duel-node-"+id,()=>{selected=id;Refresh();});b.style.position=Position.Absolute;b.style.width=90;b.style.height=65;
                b.style.whiteSpace=WhiteSpace.Normal;b.style.fontSize=11;b.style.left=Length.Percent(n.X*10.9f);b.style.top=310-n.Y*70;
            }
            preview=Text(left,"",14);preview.name="duel-preview";
            move=Button(left,"Move along preview","duel-move",()=>Act(()=>p.Duel.Move(p.Duel.ActiveSide,selected)));
            attack=Button(left,"Attack adjacent enemy · up to 50 Tempo","duel-attack",()=>Act(()=>p.Duel.Attack(p.Duel.ActiveSide)));
            withdraw=Button(left,"Strategic Withdrawal · 40 Tempo / debt","duel-withdraw",()=>Act(()=>p.Duel.Withdraw(p.Duel.ActiveSide)));
            end=Button(left,"End side activation / claim current objective","duel-end",()=>Act(()=>p.Duel.EndActivation(p.Duel.ActiveSide)));
            history=Text(left,"",12);history.name="duel-events";
            var right=new ScrollView();right.style.flexGrow=1;right.style.paddingLeft=12;content.Add(right);
            status=Text(right,"",16);status.name="duel-status";
            Button(right,"Save Crossroads","duel-save",()=>p.SaveDuel());Button(right,"Load Crossroads","duel-load-slot",()=>p.LoadDuel());
            saveStatus=Text(right,"",12);roster=Text(right,"",13);roster.name="duel-roster";
            handoff=new VisualElement {name="duel-handoff"};handoff.style.position=Position.Absolute;handoff.style.left=handoff.style.right=handoff.style.top=handoff.style.bottom=0;
            handoff.style.backgroundColor=new Color(.06f,.09f,.12f);handoff.style.justifyContent=Justify.Center;handoff.style.alignItems=Align.Center;Root.Add(handoff);
            pass=Text(handoff,"",26);accept=Button(handoff,"Continue","duel-continue",()=>Act(()=>p.Duel.ContinueHandoff(p.Duel.ActiveSide)));
            Button(handoff,"Save between activations","duel-handoff-save",()=>p.SaveDuel());Button(handoff,"Load saved Crossroads","duel-handoff-load",()=>p.LoadDuel());Refresh();
        }
        private void Act(Func<bool> action){action();p.DuelChanged();}
        private void Draw(MeshGenerationContext c)
        {var pen=c.painter2D;pen.lineWidth=2;pen.strokeColor=Color.gray;foreach(var e in CrossroadsScenario.Map.Edges){pen.BeginPath();pen.MoveTo(map.Q<Button>("duel-node-"+e.A).layout.center);pen.LineTo(map.Q<Button>("duel-node-"+e.B).layout.center);pen.Stroke();}}
        public void Refresh()
        {
            var s=p.Duel;Root.style.display=s.Encounter==null?DisplayStyle.Flex:DisplayStyle.None;
            handoff.style.display=s.HandoffPending?DisplayStyle.Flex:DisplayStyle.None;content.SetEnabled(!s.HandoffPending);pass.text="Pass to "+s.ActiveSide+" — Continue";
            var f=s.Force(s.ActiveSide);var enemy=s.Force(CrossroadsScenario.Other(s.ActiveSide));var m=s.PreviewMove(s.ActiveSide,selected);
            preview.text="Path "+string.Join(" → ",m.Path)+" · cost "+m.Cost+"\n"+(m.Reason??"Legal movement");move.SetEnabled(m.IsLegal);
            attack.SetEnabled(s.CanAttack(s.ActiveSide));withdraw.SetEnabled(s.CanAttack(s.ActiveSide));end.SetEnabled(s.CanAct(s.ActiveSide));
            status.text="R"+s.Refresh+" · Active "+s.ActiveSide+" · first "+s.StartingSide+"\nPressure W "+s.West.Pressure+" / E "+s.East.Pressure+" · target 8"
                +(s.Winner.HasValue?"\nWINNER: "+s.Winner:"")+"\nTempo "+f.Tempo+" · Provisions "+f.Provisions+" / 30 · consumption "+f.Consumption+(f.Hungry?" · HUNGRY ×1.25 cost":"")
                +"\nEnemy @ "+enemy.Node+": "+enemy.Consumption+" units · "+string.Join(" · ",enemy.Formation.LivingMembers.GroupBy(c=>c.Profile.Id).Select(g=>g.Count()+" "+g.Key))
                +"\nNext Refresh here: +"+s.RecoveryPercent(s.ActiveSide)+"% Max HP. Own Keep: supply up to 6; HP40%. Field HP15%. Armor NOT repaired."
                +"\nObjectives: "+string.Join("; ",new[]{6,7,8}.Select(n=>CrossroadsScenario.Map.Node(n).Name+"="+(s.Owner(n)?.ToString()??"Neutral")));
            roster.text=(f.Formation.Commanderless?"COMMANDERLESS · roster locked":"COMMANDER-LED")+"\n"+string.Join("\n",f.Formation.Members.Select(c=>c.CharacterId+(c.IsCommander?" *":"")+" · "+c.Profile.Id+" · "+c.Status+"\nHP "+c.Hp+"/"+c.Profile.MaxHp+" Armor "+c.Armor+"/"+c.Profile.MaxArmor+" · XP "+c.PersonalXp.ToString("0.##")+" L"+c.PersonalLevel+(c.IsCommander?" · Command "+c.CommandXp.ToString("0.##")+" L"+c.CommandLevel+" Rank "+c.CommandRank:"")));
            history.text=string.Join("\n",s.Events.Reverse().Take(8));saveStatus.text=p.DuelSaveMessage;
            foreach(var n in CrossroadsScenario.Map.Nodes){var b=map.Q<Button>("duel-node-"+n.Id);b.text=n.Id+" "+n.Name+(s.Owner(n.Id).HasValue?"\n"+s.Owner(n.Id):"")+(s.West.Continues&&s.West.Node==n.Id?"\n◆ WEST":"")+(s.East.Continues&&s.East.Node==n.Id?"\n◆ EAST":"");b.tooltip="Edges: "+string.Join(", ",CrossroadsScenario.Map.Neighbors(n.Id).Select(v=>v+" cost "+CrossroadsScenario.Map.Cost(n.Id,v)));}
            map.MarkDirtyRepaint();
        }
        private static Label Text(VisualElement parent,string text,int size){var l=new Label(text);l.style.whiteSpace=WhiteSpace.Normal;l.style.fontSize=size;l.style.color=Color.white;l.style.marginBottom=8;parent.Add(l);return l;}
        private static Button Button(VisualElement parent,string text,string name,Action action){var b=new Button(action){text=text,name=name};b.style.minHeight=30;parent.Add(b);return b;}
    }
}

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
        private readonly Button move,attack,withdraw,end,accept,recruitWarrior,recruitArcher;
        private readonly Label economy,incident;
        private readonly Button worldContinue,contactFight,contactWithdraw;
        private int selected;
        public CrossroadsHud(VisualElement parent,BattlePresenter presenter)
        {
            p=presenter;Root=new VisualElement {name="duel-world"};Root.style.position=Position.Absolute;
            Root.style.left=Root.style.right=Root.style.top=Root.style.bottom=0;Root.style.backgroundColor=new Color(.055f,.085f,.11f);parent.Add(Root);
            content=new VisualElement();content.style.flexDirection=FlexDirection.Row;content.style.flexGrow=1;Root.Add(content);
            var left=new ScrollView();left.style.width=Length.Percent(64);content.Add(left);
            Text(left,p.Duel.Incident==null?"CROSSROADS DUEL · strategic Hotseat":"CROSSROADS INCIDENT 04 · World Dynamics",22);Text(left,"Open information · claim objectives at activation end · "+p.Duel.TargetPressure+" Pressure to win",14);
            map=new VisualElement();map.style.height=p.Duel.Incident==null?400:650;map.style.flexShrink=0;left.Add(map);map.generateVisualContent+=Draw;
            foreach(var n in p.Duel.Graph.Nodes)
            {
                int id=n.Id;var b=Button(map,n.Name,"duel-node-"+id,()=>{selected=id;Refresh();});b.style.position=Position.Absolute;b.style.width=90;b.style.height=65;
                b.style.whiteSpace=WhiteSpace.Normal;b.style.fontSize=11;b.style.left=Length.Percent(n.X*10.9f);b.style.top=310-n.Y*(p.Duel.Incident==null?70:65);
            }
            preview=Text(left,"",14);preview.name="duel-preview";
            move=Button(left,"Move along preview","duel-move",()=>Act(()=>p.Duel.Move(p.Duel.ActiveSide,selected)));
            attack=Button(left,"Attack adjacent enemy · up to 50 Tempo","duel-attack",()=>Act(()=>p.Duel.Incident==null?p.Duel.Attack(p.Duel.ActiveSide):p.Duel.AttackNode(p.Duel.ActiveSide,selected)));
            withdraw=Button(left,"Strategic Withdrawal · 40 Tempo / debt","duel-withdraw",()=>Act(()=>p.Duel.Incident==null?p.Duel.Withdraw(p.Duel.ActiveSide):p.Duel.WithdrawFromContact(p.Duel.ActiveSide)));
            end=Button(left,"End side activation / claim current objective","duel-end",()=>Act(()=>p.Duel.EndActivation(p.Duel.ActiveSide)));
            worldContinue=Button(left,"Continue World Phase · next actor / finish Refresh","incident-continue",()=>Act(()=>p.Duel.ContinueWorldPhase()));
            contactFight=Button(left,"Contact: Fight · defending human controls own units","incident-fight",()=>Act(()=>p.Duel.RespondToContact(p.Duel.PendingContact.Target.Formation.Side,false)));
            contactWithdraw=Button(left,"Contact: Strategic Withdrawal · 40 Tempo","incident-withdraw",()=>Act(()=>p.Duel.RespondToContact(p.Duel.PendingContact.Target.Formation.Side,true)));
            incident=Text(left,"",14);incident.name="incident-state";
            history=Text(left,"",12);history.name="duel-events";
            var right=new ScrollView();right.style.flexGrow=1;right.style.paddingLeft=12;content.Add(right);
            status=Text(right,"",16);status.name="duel-status";
            Button(right,"Save Crossroads","duel-save",()=>p.SaveDuel());Button(right,"Load Crossroads","duel-load-slot",()=>p.LoadDuel());
            saveStatus=Text(right,"",12);
            economy=Text(right,"",14);economy.name="duel-economy";
            recruitWarrior=Button(right,"Recruit L1 HW · 100 Gold","duel-recruit-hw",()=>Act(()=>p.Duel.Recruit(p.Duel.ActiveSide,UnitProfileId.HumanWarriorTI)));
            recruitArcher=Button(right,"Recruit L1 HA · 100 Gold","duel-recruit-ha",()=>Act(()=>p.Duel.Recruit(p.Duel.ActiveSide,UnitProfileId.HumanArcherTI)));
            roster=Text(right,"",13);roster.name="duel-roster";
            handoff=new VisualElement {name="duel-handoff"};handoff.style.position=Position.Absolute;handoff.style.left=handoff.style.right=handoff.style.top=handoff.style.bottom=0;
            handoff.style.backgroundColor=new Color(.06f,.09f,.12f);handoff.style.justifyContent=Justify.Center;handoff.style.alignItems=Align.Center;Root.Add(handoff);
            pass=Text(handoff,"",26);accept=Button(handoff,"Continue","duel-continue",()=>Act(()=>p.Duel.ContinueHandoff(p.Duel.ActiveSide)));
            Button(handoff,"Save between activations","duel-handoff-save",()=>p.SaveDuel());Button(handoff,"Load saved Crossroads","duel-handoff-load",()=>p.LoadDuel());Refresh();
        }
        private void Act(Func<bool> action){action();p.DuelChanged();}
        private void Draw(MeshGenerationContext c)
        {var pen=c.painter2D;pen.lineWidth=2;pen.strokeColor=Color.gray;foreach(var e in p.Duel.Graph.Edges){pen.BeginPath();pen.MoveTo(map.Q<Button>("duel-node-"+e.A).layout.center);pen.LineTo(map.Q<Button>("duel-node-"+e.B).layout.center);pen.Stroke();}}
        public void Refresh()
        {
            var s=p.Duel;Root.style.display=s.Encounter==null?DisplayStyle.Flex:DisplayStyle.None;
            handoff.style.display=s.HandoffPending?DisplayStyle.Flex:DisplayStyle.None;content.SetEnabled(!s.HandoffPending);pass.text="Pass to "+s.ActiveSide+" — Continue";
            var displayedSide=s.PendingContact?.Target.Formation.Side??s.ActiveSide;var f=s.Force(displayedSide);var enemy=s.Force(CrossroadsScenario.Other(displayedSide));var m=s.PreviewMove(s.ActiveSide,selected);
            preview.text=s.PendingContact!=null?"HOSTILE CONTACT — pass control to "+s.PendingContact.Target.Formation.Side+". Choose Fight or Withdrawal. Save blocked until resolved.":"Path "+string.Join(" → ",m.Path)+" · cost "+m.Cost+"\n"+(m.Reason??"Legal movement");move.SetEnabled(m.IsLegal);
            attack.SetEnabled(s.Incident==null?s.CanAttack(s.ActiveSide):s.CanAttackNode(s.ActiveSide,selected));withdraw.SetEnabled(s.Incident==null?s.CanAttack(s.ActiveSide):s.CanWithdrawIncident(s.ActiveSide));end.SetEnabled(s.CanAct(s.ActiveSide));
            status.text="R"+s.Refresh+" · "+(s.Incident?.WorldPhase==true?"WORLD PHASE":"Active "+s.ActiveSide)+" · first "+s.StartingSide+"\nPressure W "+s.West.Pressure+" / E "+s.East.Pressure+" · target "+s.TargetPressure
                +(s.Winner.HasValue?"\nWINNER: "+s.Winner:"")+"\nTempo "+f.Tempo+" · Provisions "+f.Provisions+" / 30 · consumption "+f.Consumption+(f.Hungry?" · HUNGRY ×1.25 cost":"")
                +"\nEnemy @ "+enemy.Node+": "+enemy.Consumption+" units · "+string.Join(" · ",enemy.Formation.LivingMembers.GroupBy(c=>c.Profile.Id).Select(g=>g.Count()+" "+g.Key))
                +"\nNext Refresh here: +"+s.RecoveryPercent(s.ActiveSide)+"% Max HP. Own Keep: supply up to 6; HP40%. Field HP15%. Armor NOT repaired."
                +"\nObjectives: "+string.Join("; ",new[]{6,7,8}.Select(n=>p.Duel.Graph.Node(n).Name+"="+(s.Owner(n)?.ToString()??"Neutral")));
            economy.style.display=s.Economy?DisplayStyle.Flex:DisplayStyle.None;
            economy.text=displayedSide+" · Gold "+f.Gold+" · Own Keep Food "+f.KeepFood+" · free capacity "+f.FreeCapacity+" / "+f.Capacity+" (Native unit6)"
                +"\nNorth Mine +75 Gold / Refresh. Central Beacon: position + Pressure only."
                +"\nSouth Waystation Food "+s.WaystationFood+" /24 · controlling formation here: supply<=6. No regeneration."
                +"\nOwn Keep supply<=6 costs actual Food. HP recovery costs time, not Gold/Food. No Armor repair."
                +"\n"+(f.PendingRecruit.HasValue?"PAID PENDING: "+f.PendingRecruitId+" / "+f.PendingRecruit:"Recruit: "+(s.RecruitBlocker(s.ActiveSide,UnitProfileId.HumanWarriorTI)??"legal · ends activation; joins at global Refresh end"));
            recruitWarrior.style.display=recruitArcher.style.display=s.Economy?DisplayStyle.Flex:DisplayStyle.None;
            recruitWarrior.SetEnabled(s.RecruitBlocker(s.ActiveSide,UnitProfileId.HumanWarriorTI)==null);recruitArcher.SetEnabled(s.RecruitBlocker(s.ActiveSide,UnitProfileId.HumanArcherTI)==null);
            roster.text=(f.Formation.Commanderless?"COMMANDERLESS · roster locked":"COMMANDER-LED")+"\n"+string.Join("\n",f.Formation.Members.Select(c=>c.CharacterId+(c.IsCommander?" *":"")+" · "+c.Profile.Id+" · "+c.Status+"\nHP "+c.Hp+"/"+c.Profile.MaxHp+" Armor "+c.Armor+"/"+c.Profile.MaxArmor+" · XP "+c.PersonalXp.ToString("0.##")+" L"+c.PersonalLevel+(c.IsCommander?" · Command "+c.CommandXp.ToString("0.##")+" L"+c.CommandLevel+" Rank "+c.CommandRank:"")));
            worldContinue.style.display=s.Incident?.WorldPhase==true?DisplayStyle.Flex:DisplayStyle.None;worldContinue.SetEnabled(s.Encounter==null&&s.PendingContact==null&&!s.Winner.HasValue);
            contactFight.style.display=contactWithdraw.style.display=s.PendingContact==null?DisplayStyle.None:DisplayStyle.Flex;contactWithdraw.SetEnabled(s.PendingContact!=null&&s.PendingContact.Target.Tempo>=0);
            incident.style.display=s.Incident==null?DisplayStyle.None:DisplayStyle.Flex;
            if(s.Incident!=null){var i=s.Incident;incident.text="Portal: "+i.Phase+(i.Phase==PortalPhase.Whiteout?" · closure in "+i.WhiteoutRemaining+" full Refresh(es)":"")+" · "+i.SituationResolution+"\nFar-side expedition is outside this prototype. Target: South Waystation.\nSupply "+(i.Ravaged?"RAVAGED / disabled · inaccessible Food ":"Intact · Food ")+s.WaystationFood+" (ownership/Pressure separate)\nProtect/Hold: "+i.Contract+" · "+i.ClaimantId+" · "+i.HoldCycles+" /2 full cycles · reward150 Gold · paid="+i.RewardPaid+"\n"+i.HoldReason+"\n"+string.Join("\n",i.Raiders.Select(r=>r.Id+" · "+r.State+(r.OnMap?" @"+r.Force.Node:"")+" · "+r.Force.Consumption+" units · "+string.Join(" / ",r.Force.Formation.LivingMembers.GroupBy(c=>c.Profile.Id).Select(g=>g.Count()+" "+g.Key))+" · Tempo "+r.Force.Tempo+" Prov "+r.Force.Provisions+" · "+r.Blocker));}
            history.text=string.Join("\n",s.Events.Reverse().Take(8));
            Root.Q<Button>("duel-save").SetEnabled(s.CanSave);Root.Q<Button>("duel-load-slot").SetEnabled(s.CanSave);
            saveStatus.text=s.PendingContact!=null?"Save/Load disabled: defending human must resolve Fight / Withdrawal first.":p.DuelSaveMessage;
            foreach(var n in p.Duel.Graph.Nodes){var b=map.Q<Button>("duel-node-"+n.Id);b.text=n.Id+" "+n.Name+(s.Owner(n.Id).HasValue?"\n"+s.Owner(n.Id):"")+(s.West.Continues&&s.West.Node==n.Id?"\n◆ WEST":"")+(s.East.Continues&&s.East.Node==n.Id?"\n◆ EAST":"");if(s.Incident!=null)foreach(var r in s.Incident.Raiders.Where(r=>r.OnMap&&r.Force.Node==n.Id))b.text+="\nRAIDER "+(r.Id.EndsWith("A")?"A":"B");b.tooltip="Edges: "+string.Join(", ",p.Duel.Graph.Neighbors(n.Id).Select(v=>v+" cost "+p.Duel.Graph.Cost(n.Id,v)));}
            map.MarkDirtyRepaint();
        }
        private static Label Text(VisualElement parent,string text,int size){var l=new Label(text);l.style.whiteSpace=WhiteSpace.Normal;l.style.fontSize=size;l.style.color=Color.white;l.style.marginBottom=8;parent.Add(l);return l;}
        private static Button Button(VisualElement parent,string text,string name,Action action){var b=new Button(action){text=text,name=name};b.style.minHeight=30;parent.Add(b);return b;}
    }
}

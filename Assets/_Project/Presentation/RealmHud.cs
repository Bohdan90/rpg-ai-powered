using System;
using System.Collections.Generic;
using System.Linq;
using RPG.Core;
using UnityEngine;
using UnityEngine.UIElements;
namespace RPG.Presentation
{
    // Graybox interaction only. All previews, quotes, authority and transactions are Core-owned.
    public sealed class RealmHud
    {
        public VisualElement Root {get;}
        private readonly BattlePresenter p;
        private readonly VisualElement content,map,units,handoff;
        private readonly Label status,overview,preview,transferQuote,services,cityInfo,message,pass;
        private readonly DropdownField selectedArmy,source,recipient,officer,profile,project,tech;
        private readonly Button move,attack,transfer,end,form,commission,assign,disband,contactFight,contactWithdraw,continueSide;
        private readonly HashSet<string> chosen=new HashSet<string>();
        private int selectedNode=1;
        private WorldId selectedWorld=WorldId.Frontier;
        public SeamlessMapView SeamlessView {get;}
        private bool endConfirmed;
        public RealmHud(VisualElement parent,BattlePresenter presenter)
        {
            p=presenter;Root=new VisualElement{name="realm-world"};Root.style.position=Position.Absolute;Root.style.left=Root.style.right=Root.style.top=Root.style.bottom=0;Root.style.backgroundColor=new Color(.055f,.085f,.11f);parent.Add(Root);
            content=new VisualElement();content.style.flexDirection=FlexDirection.Row;content.style.flexGrow=1;Root.Add(content);
            var left=new ScrollView();left.style.width=Length.Percent(56);content.Add(left);var right=new ScrollView();right.style.flexGrow=1;right.style.paddingLeft=12;content.Add(right);
            Text(left,p.Duel.Seamless==null?"REALM OPERATIONS 06 · two human sides · open information":"SEAMLESS WORLDS 07 · side-filtered information",21);
            status=Text(left,"",15);status.name="realm-status";
            selectedArmy=Drop(left,"Selected army", "realm-selected",()=>{if(p.Duel.Realm.Select(p.Duel.ActiveSide,selectedArmy.value))endConfirmed=false;Refresh();});
            Button(left,"Select displayed continuing army","realm-select",()=>Act(()=>p.Duel.Realm.Select(p.Duel.ActiveSide,selectedArmy.value)));
            map=new VisualElement();
            if(p.Duel.Seamless!=null)SeamlessView=new SeamlessMapView(left,p,a=>{selectedNode=a.Node;selectedWorld=a.World;},Refresh);
            else {
            map.style.height=700;map.style.flexShrink=0;left.Add(map);
            foreach(var n in p.Duel.Graph.Nodes){int id=n.Id;var b=Button(map,n.Name,"realm-node-"+id,()=>{selectedNode=id;Refresh();});b.style.position=Position.Absolute;b.style.width=88;b.style.height=65;b.style.fontSize=11;b.style.whiteSpace=WhiteSpace.Normal;b.style.left=Length.Percent(n.X*10.6f);b.style.top=310-n.Y*70;}
            map.generateVisualContent+=c=>{var pen=c.painter2D;pen.lineWidth=2;pen.strokeColor=Color.gray;foreach(var e in p.Duel.Graph.Edges){pen.BeginPath();pen.MoveTo(map.Q<Button>("realm-node-"+e.A).layout.center);pen.LineTo(map.Q<Button>("realm-node-"+e.B).layout.center);pen.Stroke();}};
            }
            preview=Text(left,"",14);preview.name="realm-encounter-preview";
            move=Button(left,"Move selected army","realm-move",()=>Act(()=>p.Duel.Seamless!=null?p.Duel.Seamless.Move(p.Duel.ActiveSide,new WorldAddress(selectedWorld,selectedNode)):p.Duel.Realm.Move(p.Duel.ActiveSide,selectedNode)));
            attack=Button(left,"Attack selected Battle Target · commit all listed armies","realm-attack",()=>Act(()=>p.Duel.Realm.Attack(p.Duel.ActiveSide,Target()?.Formation.FormationId)));
            Button(left,"Selected army: strategic Withdrawal · 40 Tempo/debt","realm-withdraw",()=>Act(()=>p.Duel.Realm.Withdraw(p.Duel.ActiveSide)));
            end=Button(left,"End Side Turn","realm-end",()=>{if(!endConfirmed&&p.Duel.Realm.Armies.Any(f=>f.Continues&&f.Formation.Side==p.Duel.ActiveSide&&f.Tempo>0)){endConfirmed=true;message.text="Armies still have Tempo. Click End Side Turn again to leave it unused voluntarily.";return;}endConfirmed=false;Act(()=>p.Duel.EndActivation(p.Duel.ActiveSide));});
            message=Text(left,"",13);message.name="realm-message";
            Button(right,"Save stable World","realm-save",()=>p.SaveDuel());Button(right,"Load scenario","realm-load",()=>p.LoadDuel(p.Duel.Seamless!=null?BattlePresenter.SeamlessSlot:BattlePresenter.RealmSlot));
            overview=Text(right,"",14);overview.name="realm-armies";
            source=Drop(right,"Physical source", "realm-source",()=>{chosen.Clear();Refresh();});recipient=Drop(right,"Recipient / recruit destination", "realm-recipient",Refresh);
            units=new VisualElement{name="realm-unit-selection"};right.Add(units);
            transferQuote=Text(right,"",13);transferQuote.name="realm-transfer-preview";
            transfer=Button(right,"Confirm selected physical transfer","realm-transfer",()=>Act(()=>p.Duel.Realm.Transfer(p.Duel.ActiveSide,source.value,recipient.value,chosen)));
            officer=Drop(right,"Reserve officer / Commission candidate", "realm-officer",Refresh);
            commission=Button(right,"Queue Commission · 200 Gold when active · subsequent Refresh","realm-commission",()=>Act(()=>p.Duel.Realm.QueueCommission(p.Duel.ActiveSide,officer.value)));
            Button(right,"Cancel candidate Commission / queue (no refund)","realm-cancel-commission",()=>Act(()=>p.Duel.Realm.CancelCommission(p.Duel.ActiveSide,officer.value)));
            form=Button(right,"Form Army: officer + selected Reserve IDs · zero free Tempo/food","realm-form",()=>Act(()=>p.Duel.Realm.FormArmy(p.Duel.ActiveSide,officer.value,chosen.Concat(new[]{officer.value}).Distinct())));
            assign=Button(right,"Assign Reserve officer to selected Commanderless army","realm-assign",()=>Act(()=>p.Duel.Realm.AssignCommander(p.Duel.ActiveSide,selectedArmy.value,officer.value)));
            disband=Button(right,"City disband selected army into Reserve (pending meals remain)","realm-disband",()=>Act(()=>p.Duel.Realm.Disband(p.Duel.ActiveSide,selectedArmy.value)));
            profile=Drop(right,"Recruit L1 profile","realm-profile",Refresh);profile.choices=new List<string>{"HumanWarriorTI","HumanArcherTI","FireMageTI","IceMageTI"};profile.SetValueWithoutNotify(profile.choices[0]);
            Button(right,"Pay recruit · HW/HA100 or HOM150 Gold · shared queue","realm-recruit",()=>Act(()=>p.Duel.Realm.Recruit(p.Duel.ActiveSide,(UnitProfileId)Enum.Parse(typeof(UnitProfileId),profile.value),recipient.value)));
            Button(right,"Train selected HOM L3 · 75G · permanent Fireball/Freeze","realm-train",()=>Act(()=>chosen.Count==1&&p.Duel.Realm.Train(p.Duel.ActiveSide,chosen.First(),true)));
            Button(right,"Buy quoted Armor repair for selected IDs","realm-repair",()=>Act(()=>p.Duel.Realm.Repair(p.Duel.ActiveSide,chosen)));
            Button(right,"Cancel selected personal service (no refund)","realm-cancel-service",()=>Act(()=>chosen.Aggregate(false,(done,id)=>p.Duel.Realm.CancelService(p.Duel.ActiveSide,id)||done)));
            services=Text(right,"",13);services.name="realm-services";
            cityInfo=Text(right,"",13);cityInfo.name="realm-city";
            project=Drop(right,"Construction at selected City/Minor","realm-project",Refresh);project.choices=Enum.GetNames(typeof(CityProjectKind)).ToList();project.SetValueWithoutNotify("Forge");
            Button(right,"Queue construction · pay when requirements met","realm-build",()=>Act(()=>p.Duel.Foundations.QueueProject(p.Duel,p.Duel.ActiveSide,CityNode(),(CityProjectKind)Enum.Parse(typeof(CityProjectKind),project.value))));
            Button(right,"Remove first unpaid construction queue entry","realm-build-unqueue",()=>Act(()=>p.Duel.Foundations.CancelProject(p.Duel,p.Duel.ActiveSide,CityNode(),0)));
            Button(right,"Pause/resume construction","realm-build-pause",()=>Act(()=>p.Duel.Foundations.PauseProject(p.Duel,p.Duel.ActiveSide,CityNode())));
            Button(right,"Cancel active construction (no refund)","realm-build-cancel",()=>Act(()=>p.Duel.Foundations.CancelProject(p.Duel,p.Duel.ActiveSide,CityNode())));
            tech=Drop(right,"Realm research","realm-tech",Refresh);tech.choices=Enum.GetNames(typeof(CityTech)).ToList();tech.SetValueWithoutNotify("ForgeOrganization");
            Button(right,"Queue research · 40 Gold on activation","realm-research",()=>Act(()=>p.Duel.Foundations.QueueResearch(p.Duel,p.Duel.ActiveSide,(CityTech)Enum.Parse(typeof(CityTech),tech.value))));
            Button(right,"Remove first unpaid research queue entry","realm-research-unqueue",()=>Act(()=>p.Duel.Foundations.RemoveQueuedResearch(p.Duel,p.Duel.ActiveSide,0)));
            Button(right,"Pause/resume research","realm-research-pause",()=>Act(()=>p.Duel.Foundations.PauseResearch(p.Duel,p.Duel.ActiveSide)));
            Button(right,"Stop active research (paid Work retained)","realm-research-stop",()=>Act(()=>p.Duel.Foundations.StopResearch(p.Duel,p.Duel.ActiveSide)));
            handoff=new VisualElement{name="realm-handoff"};handoff.style.position=Position.Absolute;handoff.style.left=handoff.style.right=handoff.style.top=handoff.style.bottom=0;handoff.style.backgroundColor=new Color(.06f,.09f,.12f);handoff.style.justifyContent=Justify.Center;handoff.style.alignItems=Align.Center;Root.Add(handoff);
            pass=Text(handoff,"",24);continueSide=Button(handoff,"Continue side activation","realm-continue",()=>Act(()=>p.Duel.ContinueHandoff(p.Duel.ActiveSide)));contactFight=Button(handoff,"Defender: Fight (all listed participants)","realm-fight",()=>Act(()=>p.Duel.RespondToContact(p.Duel.PendingContact.Target.Formation.Side,false)));contactWithdraw=Button(handoff,"Defender: pre-battle Withdrawal · 40 Tempo/debt","realm-contact-withdraw",()=>Act(()=>p.Duel.RespondToContact(p.Duel.PendingContact.Target.Formation.Side,true)));Button(handoff,"Save between activations","realm-handoff-save",()=>p.SaveDuel());
            Refresh();
        }
        private int CityNode()=>selectedWorld==WorldId.Frontier&&p.Duel.Foundations.Location(selectedNode)?.IsMinor==true&&p.Duel.Foundations.Location(selectedNode).Controller==p.Duel.ActiveSide?selectedNode:p.Duel.OwnKeep(p.Duel.ActiveSide);
        private DuelForce Target()=>p.Duel.Realm.Armies.FirstOrDefault(f=>f.Continues&&f.Formation.Side!=p.Duel.ActiveSide&&f.WorldId==selectedWorld&&f.Node==selectedNode&&(p.Duel.Seamless==null||p.Duel.Seamless.IsVisibleEnemy(p.Duel.ActiveSide,f.Formation.FormationId)));
        private void Act(Func<bool> action){bool applied=action();p.DuelChanged();if(!applied)message.text="Command rejected; no state spent. Review the current legal preview.";}
        private static void Choices(DropdownField d,List<string> list,string preferred=null){d.choices=list;if(preferred!=null&&list.Contains(preferred))d.SetValueWithoutNotify(preferred);else if(!list.Contains(d.value))d.SetValueWithoutNotify(list.FirstOrDefault()??"");}
        public void Refresh()
        {
            var w=p.Duel;var r=w.Realm;var side=w.ActiveSide;var s=r.SideState(side);Root.style.display=w.Encounter==null?DisplayStyle.Flex:DisplayStyle.None;
            handoff.style.display=w.HandoffPending||w.PendingContact!=null?DisplayStyle.Flex:DisplayStyle.None;content.SetEnabled(!w.HandoffPending);pass.text=w.PendingContact==null?"Pass to "+side+". No Refresh at handoff.":"Hostile Contact: "+w.PendingContact.Target.Formation.FormationId+" chooses Fight / Withdrawal. No save during commitment."+(w.Seamless==null?"":"\nBattle world: "+SeamlessWorlds.Name(w.PendingContact.Target.WorldId)+" · "+string.Join("; ",w.PendingContact.CommittedForces.Select(f=>f.Formation.FormationId+" @"+f.Address+" · "+f.Formation.LivingMembers.Count()+" figures")));continueSide.style.display=w.PendingContact==null?DisplayStyle.Flex:DisplayStyle.None;contactFight.style.display=contactWithdraw.style.display=w.PendingContact!=null?DisplayStyle.Flex:DisplayStyle.None;contactWithdraw.SetEnabled(w.PendingContact?.Target.Tempo>=0);
            if(w.Seamless!=null&&(w.HandoffPending||w.PendingContact!=null)){content.style.display=DisplayStyle.None;SeamlessView.Refresh();return;}content.style.display=DisplayStyle.Flex;
            var own=r.Armies.Where(f=>f.Formation.Side==side&&f.Continues).ToArray();var names=own.Select(f=>f.Formation.FormationId).ToList();Choices(selectedArmy,names,s.Selected);Choices(source,new[]{"Reserve"}.Concat(names).ToList());Choices(recipient,new[]{"Reserve"}.Concat(names).ToList());Choices(officer,s.Reserve.Where(c=>c.Status!=PersistentCharacterStatus.Dead).Select(c=>c.CharacterId).ToList());
            status.text="R"+w.Refresh+" · active SIDE "+side+" · selected ARMY "+s.Selected+"\nPressure West "+w.West.Pressure+" / East "+w.East.Pressure+" · target24 · "+(w.Winner.HasValue?"WINNER "+w.Winner:w.IsDraw?"DRAW":"match ongoing");
            overview.text=string.Join("\n\n",r.Armies.Where(f=>f.Formation.Side==side).Select(f=>f.Formation.FormationId+" @"+(w.Seamless!=null?SeamlessWorlds.Name(f.WorldId)+" ":"")+w.GraphFor(f.WorldId).Node(f.Node).Name+(f.Continues?"":" [retired/dead history]")+"\n"+(f.Formation.Commanderless?"COMMANDERLESS · roster locked":f.Formation.Commander?.Profile.Id+" · Command L"+f.Formation.Commander?.CommandLevel+" Rank "+f.Formation.Commander?.CommandRank)+" · Capacity "+r.UsedCapacity(f)+"/"+r.Capacity(f)+"\nTempo/debt "+f.Tempo+" · Prov "+f.RealmProvisions.ToString("0.##")+"/"+RealmOperations.CarryingCapacity(f)+" · meal "+RealmOperations.Consumption(f)+" incl staff2 · due this cycle "+r.PendingConsumption(f)+"\n"+string.Join("; ",f.Formation.LivingMembers.Where(c=>c!=f.Formation.Commander).Select(c=>c.Profile.Id+" "+(f.Formation.Commander==null?"no Commander":r.Familiarity(f.Formation.Commander,c)+" load"+(int)r.Familiarity(f.Formation.Commander,c))))));
            overview.text+="\n\nCity Reserve: "+s.Reserve.Count(c=>c.Status!=PersistentCharacterStatus.Dead)+" living · carried staff meals due "+s.CityStaffDue;
            var roster=(source.value=="Reserve"?s.Reserve.AsEnumerable():r.Army(source.value)?.Formation.Members??Enumerable.Empty<PersistentCharacter>()).ToArray();chosen.IntersectWith(roster.Select(c=>c.CharacterId));units.Clear();
            foreach(var c in roster){string id=c.CharacterId;var t=new Toggle(id+" · "+c.Profile.Id+" L"+c.PersonalLevel+" XP"+c.PersonalXp.ToString("0.##")+(c.IsCommander?" commissioned":"")+" · "+c.Status+" HP"+c.Hp+"/"+c.Profile.MaxHp+" A"+c.Armor+"/"+c.Profile.MaxArmor+" ceiling"+r.Cycles[id].Ceiling+" deprivation "+r.Cycles[id].Deprivation);t.style.whiteSpace=WhiteSpace.Normal;t.labelElement.style.color=Color.white;t.labelElement.style.whiteSpace=WhiteSpace.Normal;t.SetValueWithoutNotify(chosen.Contains(id));t.RegisterValueChangedCallback(e=>{if(e.newValue)chosen.Add(id);else chosen.Remove(id);Refresh();});units.Add(t);}
            var tr=r.PreviewTransfer(side,source.value,recipient.value,chosen);transferQuote.text=tr.Legal?"Confirm: field cost "+tr.Cost+" · BOTH Tempo -> "+tr.ResultTempo+" · receiving Load "+tr.UsedCapacity+" · sender/recipient Provisions "+tr.SenderProvisions+" / "+tr.RecipientProvisions+" · returned City Food "+tr.ReturnedFood:tr.Reason;transfer.SetEnabled(tr.Legal);
            commission.SetEnabled(w.CanAct(side)&&officer.value!=""&&r.CommissionBlocker(side,officer.value)==null);form.SetEnabled(source.value=="Reserve"&&r.FormBlocker(side,officer.value,chosen.Concat(new[]{officer.value}).Distinct())==null);assign.SetEnabled(w.CanAct(side));disband.SetEnabled(w.CanAct(side));
            var fp=r.PreviewForm(side,officer.value,chosen.Concat(new[]{officer.value}).Distinct());var repair=r.QuoteRepair(side,chosen);services.text="Form preview: "+(fp.Reason??"Load "+fp.Load+"/"+fp.Capacity+" · Tempo "+fp.Tempo+" · initial carried 0/"+fp.MaxProvisions+" · consumption "+fp.Consumption+"/Refresh incl staff2")+"\n";services.text+="Commission: "+(s.Commission?.Unit??"none")+" · queued "+string.Join(",",s.CommissionQueue)+"\nCandidate: "+(r.CommissionBlocker(side,officer.value)??"eligible")+"\nRecruit: "+(s.Recruit==null?"none":s.Recruit.Unit+" -> "+s.Recruit.Destination)+"\nRecruit blocker: "+(r.RecruitBlocker(side,(UnitProfileId)Enum.Parse(typeof(UnitProfileId),profile.value),recipient.value)??"legal")+"\nServices: "+string.Join("; ",s.Services.Select(o=>o.Kind+" "+o.Unit+" R"+o.Started))+"\nForge quote: "+(repair==null?"No legal selected damaged local IDs":repair.Quotes.Values.Sum()*2+" Gold · "+string.Join(", ",repair.Quotes.Select(q=>q.Key+" +"+q.Value)))+"\nTraining: "+(chosen.Count==1?r.TrainingBlocker(side,chosen.First())??"eligible, permanent school spell":"select one HOM");
            var city=w.Foundations.City(side);var realm=w.Foundations.Realm(side);var site=w.Foundations.Location(CityNode());var kind=(CityProjectKind)Enum.Parse(typeof(CityProjectKind),project.value);var cost=CityFoundations.Cost(kind);
            cityInfo.text="Integrated rear City · Dev "+city.Development+" Tower "+city.MageTower+" · pre-scenario Drills Work6\nGold "+w.Force(side).Gold+" Wood "+realm.Wood+" Iron "+realm.Iron+" Food "+w.Force(side).KeepFood.ToString("0.##")+"/180\nReserve eats first. Own-City army supply <=2×meal proportionally; HP40% (field15%); no free Armor repair. Waystation Food "+(w.Seamless==null?w.WaystationFood.ToString():w.Seamless.Knowledge(side).Place(new WorldAddress(WorldId.Frontier,8))?.food.ToString()??"unknown")+"/24, <=6.\nRegional current "+w.Foundations.ProjectedRegional(w,side)+" / last "+city.Regional+" · Deep "+city.PhysicalLoad+"+"+city.ReservedLoad+"/"+city.DeepCapacity+"\nBuild "+kind+": "+cost.gold+"G "+cost.wood+"W "+cost.iron+"I, "+cost.steps+" steps · "+(w.Foundations.ProjectBlocker(w,site.Node,kind)??"legal")+"\nActive: "+site.Active?.Kind+" steps "+site.Active?.Steps+" · queue "+string.Join(",",site.Queue)+"\nResearch: "+realm.ActiveResearch+" · "+string.Join("; ",realm.Research.Select(t=>t.Tech+" "+t.Work+"/"+CityFoundations.TechCost(t.Tech)));
            var m=w.Seamless!=null?w.Seamless.PreviewMove(side,new WorldAddress(selectedWorld,selectedNode)):r.PreviewMove(side,selectedNode);var battle=r.PreviewEncounter(side,Target()?.Formation.FormationId);preview.text="Move: "+string.Join(" -> ",m.Path)+" cost "+m.Cost+" · "+(m.Reason??"legal")+"\nEncounter: "+(battle.Reason??string.Join("\n",battle.Attackers.Concat(battle.Defenders).Where(f=>w.Seamless==null||f.Formation.Side==side||w.Seamless.IsVisibleEnemy(side,f.Formation.FormationId)).Select(f=>f.Formation.FormationId+" @"+f.Node+" · "+f.Formation.LivingMembers.Count()+" figures · "+(battle.Attackers.Contains(f)?"attacker Tempo "+f.Tempo+" -> "+StrategicScenario.AfterAttackCost(f.Tempo):"defender no Attack Cost")+" · entry "+battle.Deployments.First(d=>f.Formation.Members.Any(c=>c.CharacterId==d.CharacterId)).OwnRetreatEdge)));
            move.SetEnabled(m.IsLegal);attack.SetEnabled(battle.Legal);end.SetEnabled(w.CanAct(side));message.text=(w.Seamless==null?r.LastMessage:w.Seamless.Message(side))+"\n"+p.DuelSaveMessage;
            if(w.Seamless!=null){SeamlessView.Refresh();return;}
            foreach(var n in w.Graph.Nodes){var b=map.Q<Button>("realm-node-"+n.Id);b.text=n.Id+" "+n.Name+(w.Owner(n.Id).HasValue?"\n"+w.Owner(n.Id):"")+"\n"+string.Join("\n",r.Armies.Where(f=>f.Continues&&f.Node==n.Id).Select(f=>f.Formation.FormationId.Replace("realm06-","")+" ("+f.Formation.LivingMembers.Count()+")"));b.style.backgroundColor=r.Selected(side)?.Node==n.Id?new Color(.48f,.38f,.15f):new Color(.68f,.68f,.68f);b.tooltip="Edges: "+string.Join(", ",w.Graph.Neighbors(n.Id).Select(v=>v+" cost "+w.Graph.Cost(n.Id,v)));}map.MarkDirtyRepaint();
        }
        private static Label Text(VisualElement parent,string text,int size){var l=new Label(text);l.style.whiteSpace=WhiteSpace.Normal;l.style.fontSize=size;l.style.color=Color.white;l.style.marginBottom=8;parent.Add(l);return l;}
        private static Button Button(VisualElement parent,string text,string name,Action action){var b=new Button(action){text=text,name=name};b.style.minHeight=30;b.style.whiteSpace=WhiteSpace.Normal;parent.Add(b);return b;}
        private static DropdownField Drop(VisualElement parent,string label,string name,Action changed){var d=new DropdownField(label){name=name};d.labelElement.style.color=Color.white;d.RegisterValueChangedCallback(_=>changed());parent.Add(d);return d;}
    }
}

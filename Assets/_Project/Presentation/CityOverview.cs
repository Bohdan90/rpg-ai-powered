using System;
using System.Linq;
using RPG.Core;
using UnityEngine;
using UnityEngine.UIElements;
namespace RPG.Presentation
{
    internal sealed class CityOverview
    {
        private readonly BattlePresenter presenter;
        private readonly VisualElement root;
        private readonly Label overview,details,queues,repair,training;
        private readonly DropdownField project,technology,mage,trainee;
        private readonly Button build,research,forge,train,mageRecruit;
        private readonly Toggle confirm;
        private int selected;
        public CityOverview(VisualElement parent,BattlePresenter p)
        {
            presenter=p;root=new VisualElement{name="city-overview"};parent.Add(root);
            overview=Text("CITY OVERVIEW");details=Text("");queues=Text("");
            project=new DropdownField("Construction",Enum.GetNames(typeof(CityProjectKind)).ToList(),0);root.Add(project);
            build=Button("Queue selected construction (unpaid until active)",()=>Do(()=>p.Duel.Foundations.QueueProject(p.Duel,p.Duel.ActiveSide,Location(),(CityProjectKind)project.index)));
            Button("Pause / resume active construction",()=>Do(()=>p.Duel.Foundations.PauseProject(p.Duel,p.Duel.ActiveSide,Location())));
            Button("Cancel paid construction · NO REFUND",()=>Do(()=>p.Duel.Foundations.CancelProject(p.Duel,p.Duel.ActiveSide,Location())));
            Button("Remove first unpaid construction",()=>Do(()=>p.Duel.Foundations.CancelProject(p.Duel,p.Duel.ActiveSide,Location(),0)));
            technology=new DropdownField("Research",Enum.GetNames(typeof(CityTech)).ToList(),0);root.Add(technology);
            research=Button("Queue research · 40 Gold when activated",()=>Do(()=>p.Duel.Foundations.QueueResearch(p.Duel,p.Duel.ActiveSide,(CityTech)technology.index)));
            Button("Pause / resume research",()=>Do(()=>p.Duel.Foundations.PauseResearch(p.Duel,p.Duel.ActiveSide)));
            Button("Stop current research · progress retained / NO REFUND",()=>Do(()=>p.Duel.Foundations.StopResearch(p.Duel,p.Duel.ActiveSide)));
            Button("Remove first unpaid research",()=>Do(()=>p.Duel.Foundations.RemoveQueuedResearch(p.Duel,p.Duel.ActiveSide,0)));
            repair=Text("");forge=Button("Confirm quoted Forge order · full following Refresh",()=>Do(()=>p.Duel.Foundations.OrderRepair(p.Duel,p.Duel.ActiveSide)));
            mage=new DropdownField("L1 Mage",new System.Collections.Generic.List<string>{"Fire HOM","Ice HOM","HH Support"},0);root.Add(mage);
            mageRecruit=Button("Recruit selected L1 Mage · 150 Gold",()=>Do(()=>p.Duel.Recruit(p.Duel.ActiveSide,mage.index==0?UnitProfileId.FireMageTI:mage.index==1?UnitProfileId.IceMageTI:UnitProfileId.HumanHealerTI)));
            mage.RegisterValueChangedCallback(e=>Refresh(selected));
            training=Text("");trainee=new DropdownField("Persistent trainee",new System.Collections.Generic.List<string>{"None"},0);root.Add(trainee);
            confirm=new Toggle("Permanent school spell: Fireball / Freeze. Pay 75 Gold; no healing.");root.Add(confirm);
            train=Button("Start confirmed HOM II training",()=>Do(()=>p.Duel.Foundations.Train(p.Duel,p.Duel.ActiveSide,trainee.value,confirm.value)));
            Button("Cancel training · NO REFUND",()=>Do(()=>p.Duel.Foundations.CancelTraining(p.Duel,p.Duel.ActiveSide)));
            foreach(var field in new[]{project,technology,mage,trainee})field.labelElement.style.color=Color.white;
            confirm.labelElement.style.color=Color.white;
        }
        private int Location()=>presenter.Duel.Foundations.Location(selected)!=null?selected:presenter.Duel.OwnKeep(presenter.Duel.ActiveSide);
        private Label Text(string text){var l=new Label(text);l.style.whiteSpace=WhiteSpace.Normal;l.style.color=Color.white;l.style.marginBottom=8;root.Add(l);return l;}
        private Button Button(string text,Action action){var b=new Button(action){text=text};b.style.whiteSpace=WhiteSpace.Normal;b.style.minHeight=32;root.Add(b);return b;}
        private void Do(Func<bool> action){action();presenter.DuelChanged();}
        public void Refresh(int selection)
        {
            selected=selection;var w=presenter.Duel;var f=w.Foundations;root.style.display=f==null?DisplayStyle.None:DisplayStyle.Flex;if(f==null)return;
            var side=w.ActiveSide;var r=f.Realm(side);var c=f.City(side);var location=f.Location(Location());bool authority=w.CanAct(side);
            var sources=f.PreviewSources(w);
            overview.text=(f.Combined?"CITY & COMBAT 05B":"CITY FOUNDATIONS 05A")+" · Human Integrated · protected rear Cities (no siege)\n"+side+" City Development "+c.Development+" · IV outside this prototype\nRegional projected "+f.ProjectedRegional(w,side).ToString("0.##")+" / last cycle "+c.Regional.ToString("0.##")+"\nDeep "+c.PhysicalLoad+" physical + "+c.ReservedLoad+" reserved / "+c.DeepCapacity+"\nGlobal: Gold "+w.Force(side).Gold.ToString("0.##")+" · Wood "+r.Wood.ToString("0.##")+" · Iron "+r.Iron.ToString("0.##")+"\nCity Food "+w.Force(side).KeepFood.ToString("0.##")+"/90 · field supply<=6; HP40%, Armor only via Forge\nCenter/Institute Work: "+(c.Institute?6:2)+" · Mage Tower "+c.MageTower+" · direction "+(r.Preset==CombatPreset.Support?"HH":"HOM");
            details.text="Selected "+location.Node+" · controller "+location.Controller+" · fixed parent "+location.Parent+(location.IsMinor?" · slots "+((location.Depot?1:0)+(location.Assay?1:0))+"/5 (two supported recipes)":"")+"\nLocal stock "+string.Join(" · ",location.Stock.Select((n,i)=>((ResourceKind)i)+" "+n.ToString("0.##")))+"\n"+string.Join("\n",Enum.GetValues(typeof(CityProjectKind)).Cast<CityProjectKind>().Where(k=>f.Combined||k<CityProjectKind.MageTowerI).Select(k=>{var cost=CityFoundations.Cost(k);return k+": "+cost.gold+"G "+cost.wood+"W "+cost.iron+"I / "+cost.steps+" steps · "+(f.ProjectBlocker(w,location.Node,k)??"requirements met (payment when active)");}));
            overview.text+="\nProjected source contributions: "+string.Join(" · ",sources.Where(s=>s.City==c.Node).Select(s=>s.Node+": +"+s.Regional.ToString("0.##")));
            details.text+="\nNext production / unboosted reference: "+string.Join("; ",sources.Where(s=>location.IsCity?s.City==location.Node:location.IsMinor?f.Location(s.Node).Parent==location.Node:s.Node==location.Node).Select(s=>s.Node+" "+s.Output.ToString("0.##")+" / "+s.Reference+" · "+s.Status));
            queues.text="Construction: "+(location.Active==null?"none":location.Active.Kind+" "+location.Active.Steps+"/"+CityFoundations.Cost(location.Active.Kind).steps+(location.Active.Paused?" PAUSED":""))+"\nUnpaid queue: "+string.Join(", ",location.Queue)+"\nResearch: "+(r.ActiveResearch?.ToString()??"none")+(r.ResearchPaused?" PAUSED":"")+" · queue "+string.Join(", ",r.ResearchQueue)+"\n"+string.Join("\n",r.Research.Where(t=>f.Combined||t.Tech!=CityTech.ElementalDrills).Select(t=>t.Tech+(CityFoundations.Universal(t.Tech)?" [Universal]":" [Human foundation]")+" "+t.Work+"/"+CityFoundations.TechCost(t.Tech)+(t.Paid?" paid":" unpaid")+" · City Work "+string.Join(",",t.Provenance.Select(v=>v.Key+":"+v.Value))));
            if(location.Active!=null){var blocker=f.ProjectBlocker(w,location.Node,location.Active.Kind,true);if(blocker!=null)queues.text+="\nAUTO PAUSED: "+blocker+" Paid progress retained.";}
            var quote=f.QuoteRepair(w,side);repair.text=r.Repair!=null?"PAID Forge: "+r.Repair.Cost+" Gold; full following Refresh, leaving cancels without refund.":quote==null?"Forge: own functioning Forge, physical presence and missing Armor required.":"Forge quote "+quote.Cost+" Gold: "+string.Join(", ",quote.Quotes.Select(q=>q.Key+" +"+q.Value+" Armor"));forge.SetEnabled(quote!=null&&w.Force(side).Gold>=quote.Cost);
            build.SetEnabled(authority&&location.Controller==side);research.SetEnabled(authority);
            var candidates=w.Force(side).Formation.LivingMembers.Where(m=>m.Profile.IsCaster&&m.Profile.Tier==1).Select(m=>m.CharacterId).ToList();if(candidates.Count==0)candidates.Add("None");trainee.choices=candidates;if(!candidates.Contains(trainee.value))trainee.SetValueWithoutNotify(candidates[0]);
            training.text=f.Combined?(r.Training==null?"Training: "+(f.TrainingBlocker(w,side,trainee.value)??"eligible; confirm permanent spell"):"PAID trainee: "+r.Training.CharacterId+" · away/Commanderless pauses. Death cancels."):"New combat roster reserved for separate 05B.";
            train.SetEnabled(f.Combined&&authority);var mageBlocker=w.RecruitBlocker(side,mage.index==0?UnitProfileId.FireMageTI:mage.index==1?UnitProfileId.IceMageTI:UnitProfileId.HumanHealerTI);mageRecruit.SetEnabled(mageBlocker==null);training.text+="\nMage recruit: "+(mageBlocker??"legal · 150 Gold + 6 Capacity");mage.SetEnabled(f.Combined&&authority);
        }
    }
}

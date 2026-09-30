using System;
using System.Collections.Generic;
using System.Linq;
namespace RPG.Core
{
    public enum ResourceKind { Gold, Food, Wood, Iron }
    public enum CityProjectKind { DevelopmentII, DevelopmentIII, Forge, ResearchInstitute, ExtractionDepot, AssayOffice, MageTowerI, MageTowerII }
    public enum CityTech { Extraction, Assay, ResearchMethod, ForgeOrganization, ElementalDrills }
    public enum CombatPreset { Fire, Ice, Support }
    public sealed class SourceOutputPreview
    {
        public int Node { get; }
        public int City { get; }
        public decimal Output { get; }
        public decimal Reference { get; }
        public decimal Regional { get; }
        public string Status { get; }
        internal SourceOutputPreview(int node,int city,decimal output,decimal reference,decimal regional,string status)
        {Node=node;City=city;Output=output;Reference=reference;Regional=regional;Status=status;}
    }
    public sealed class CityProject
    {
        public CityProjectKind Kind {get;internal set;}
        public int Started {get;internal set;}
        public int Steps {get;internal set;}
        public bool Paused {get;internal set;}
        public int Reservation=>Kind==CityProjectKind.ResearchInstitute?2:0;
    }
    public sealed class ResearchProgress
    {
        public CityTech Tech {get;internal set;}
        public decimal Work {get;internal set;}
        public bool Paid {get;internal set;}
        public bool Completed=>Work>=CityFoundations.TechCost(Tech);
        public readonly SortedDictionary<int,decimal> Provenance=new SortedDictionary<int,decimal>();
    }
    public sealed class CityLocation
    {
        public int Node {get;internal set;}
        public Side Controller {get;internal set;}
        public int Parent {get;internal set;}
        public bool Functioning {get;internal set;}=true;
        public decimal[] Stock {get;internal set;}=new decimal[4];
        public int Development {get;internal set;}=1;
        public bool Forge {get;internal set;}
        public bool Institute {get;internal set;}
        public bool Depot {get;internal set;}
        public bool Assay {get;internal set;}
        public int MageTower {get;internal set;}
        public decimal OutputCondition {get;internal set;}=1m;
        public decimal Regional {get;internal set;}
        public CityProject Active {get;internal set;}
        public List<CityProjectKind> Queue {get;internal set;}=new List<CityProjectKind>();
        public int DeepCapacity=>Development<3?0:Development==3?4:6;
        public int PhysicalLoad=>Institute?2:0;
        public int ReservedLoad=>Active?.Reservation??0;
        public bool IsCity=>Node==1||Node==13;
        public bool IsMinor=>Node==14||Node==15;
        public ResourceKind Resource=>(ResourceKind)((Node-16)%4);
    }
    public sealed class ForgeOrder
    {
        public int Started {get;internal set;}
        public SortedDictionary<string,int> Quotes {get;internal set;}=new SortedDictionary<string,int>(StringComparer.Ordinal);
        public int Cost=>Quotes.Values.Sum()*2;
    }
    public sealed class MageTraining
    {
        public string CharacterId {get;internal set;}
        public int Started {get;internal set;}
    }
    public sealed class CityRealm
    {
        public decimal Wood {get;internal set;}=30;
        public decimal Iron {get;internal set;}=20;
        public CombatPreset Preset {get;internal set;}
        public CityTech? ActiveResearch {get;internal set;}
        public int ResearchStarted {get;internal set;}
        public bool ResearchPaused {get;internal set;}
        public List<CityTech> ResearchQueue {get;internal set;}=new List<CityTech>();
        public List<ResearchProgress> Research {get;internal set;}=Enum.GetValues(typeof(CityTech)).Cast<CityTech>().Select(t=>new ResearchProgress{Tech=t}).ToList();
        public ForgeOrder Repair {get;internal set;}
        public MageTraining Training {get;internal set;}
        public bool Knows(CityTech t)=>Research.Single(r=>r.Tech==t).Completed;
    }
    // Documents 50/51: deliberately bounded scenario economy, not a universal campaign framework.
    public sealed partial class CityFoundations
    {
        public static readonly decimal[] Baseline={30m,6m,6m,4m};
        public static readonly StrategicGraph Map=CreateMap();
        public bool Combined {get;internal set;}
        public List<CityLocation> Locations {get;internal set;}
        public CityRealm West {get;internal set;}=new CityRealm();
        public CityRealm East {get;internal set;}=new CityRealm();
        public CityRealm Realm(Side side)=>side==Side.West?West:East;
        public CityLocation Location(int node)=>Locations.SingleOrDefault(l=>l.Node==node);
        public CityLocation City(Side side)=>Location(side==Side.West?1:13);
        public CityFoundations(bool combined,CombatPreset west=CombatPreset.Fire,CombatPreset east=CombatPreset.Ice)
        {
            Combined=combined;West.Preset=west;East.Preset=east;
            Locations=new List<CityLocation>{new CityLocation{Node=1,Controller=Side.West},new CityLocation{Node=13,Controller=Side.East},new CityLocation{Node=14,Parent=1,Controller=Side.West},new CityLocation{Node=15,Parent=13,Controller=Side.East}};
            for(int n=16;n<=23;n++)Locations.Add(new CityLocation{Node=n,Parent=n<20?14:15,Controller=n<20?Side.West:Side.East});
        }
        private static StrategicGraph CreateMap()
        {
            var nodes=CrossroadsScenario.Map.Nodes.ToList();var edges=CrossroadsScenario.Map.Edges.ToList();
            nodes.Add(new StrategicNode(14,"West Minor",1,-2));nodes.Add(new StrategicNode(15,"East Minor",7,-2));
            edges.Add(new StrategicEdge(3,14,20));edges.Add(new StrategicEdge(11,15,20));
            for(int n=16;n<=23;n++){bool west=n<20;int r=(n-16)%4;nodes.Add(new StrategicNode(n,(west?"W ":"E ")+(ResourceKind)r,west?r*.7f:6+r*.7f,-4));edges.Add(new StrategicEdge(west?14:15,n,20));}
            return new StrategicGraph(nodes.ToArray(),edges.ToArray());
        }
        public static bool Universal(CityTech tech)=>tech!=CityTech.ElementalDrills;
        public static decimal TechCost(CityTech t)=>t==CityTech.ResearchMethod||t==CityTech.ForgeOrganization?12m:6m;
        public static (int gold,int wood,int iron,int steps) Cost(CityProjectKind k)
        {
            switch(k){case CityProjectKind.DevelopmentII:return(100,10,0,2);case CityProjectKind.DevelopmentIII:return(200,20,10,3);
                case CityProjectKind.Forge:return(100,10,10,2);case CityProjectKind.ResearchInstitute:return(150,20,10,2);
                case CityProjectKind.ExtractionDepot:case CityProjectKind.AssayOffice:return(60,10,0,1);
                case CityProjectKind.MageTowerI:return(100,10,0,2);case CityProjectKind.MageTowerII:return(120,10,5,2);default:throw new ArgumentOutOfRangeException(nameof(k));}
        }
        public static decimal EstablishmentRequirement(int tier,decimal affinity=1m)
        {if(affinity!=.9m&&affinity!=1m&&affinity!=1.1m)throw new ArgumentOutOfRangeException(nameof(affinity));return(tier==3?50m:tier==4?70m:0m)*affinity;}
        private bool Connected(CityLocation site)=>site.Functioning&&(site.IsCity||Location(site.Parent).Functioning&&Location(site.Parent).Controller==site.Controller);
        private decimal Capacity(CrossroadsScenario w,CityLocation location,int resource)
        {if(location.IsCity)return resource==1?Math.Max(0,90-w.Force(location.Controller).KeepFood):1000000000000000000m;return Baseline[resource]*(location.IsMinor?4:3)-location.Stock[resource];}
        private void Receive(CrossroadsScenario w,CityLocation location,int resource,decimal amount)
        {
            if(!location.IsCity){location.Stock[resource]+=amount;return;}
            var f=w.Force(location.Controller);var realm=Realm(location.Controller);
            if(resource==0)f.Gold+=amount;else if(resource==1)f.KeepFood+=amount;else if(resource==2)realm.Wood+=amount;else realm.Iron+=amount;
        }
        private void Transfer(CrossroadsScenario w,CityLocation from)
        {
            if(!Connected(from)||from.IsCity)return;var to=Location(from.Parent);
            for(int r=0;r<4;r++){decimal n=Math.Min(from.Stock[r],Capacity(w,to,r));from.Stock[r]-=n;Receive(w,to,r,n);}
        }
        private decimal Produce(CrossroadsScenario world,bool commit,List<SourceOutputPreview> preview=null)
        {
            // Projection is performed against a snapshot by ProjectedRegional; this method mutates only its own economy/world.
            foreach(var city in Locations.Where(l=>l.IsCity))city.Regional=0;
            foreach(var minor in Locations.Where(l=>l.IsMinor).OrderBy(l=>l.Node))Transfer(world,minor);
            foreach(var source in Locations.Where(l=>!l.IsCity&&!l.IsMinor).OrderBy(l=>l.Node))Transfer(world,source);
            foreach(var minor in Locations.Where(l=>l.IsMinor).OrderBy(l=>l.Node))Transfer(world,minor);
            foreach(var source in Locations.Where(l=>!l.IsCity&&!l.IsMinor).OrderBy(l=>l.Node)) {
                int r=(int)source.Resource;var minor=Location(source.Parent);var city=Location(minor.Parent);
                if(!source.Functioning){preview?.Add(new SourceOutputPreview(source.Node,city.Node,0,Baseline[r],0,"Source unavailable"));continue;}
                decimal multiplier=minor.Functioning&&minor.Controller==source.Controller&&(r==0?minor.Depot:minor.Assay)?1.5m:1m;
                // Prior downstream draining leaves room only where real storage/recipients exist.
                decimal room=Capacity(world,source,r);
                if(Connected(source)){room+=Capacity(world,minor,r);if(Connected(minor))room+=Capacity(world,city,r);}
                decimal output=Math.Min(Baseline[r]*multiplier*source.OutputCondition,room);
                decimal local=Math.Min(output,Capacity(world,source,r));source.Stock[r]+=local;
                Transfer(world,source);Transfer(world,minor);
                decimal rest=output-local;if(rest>0){source.Stock[r]+=rest;Transfer(world,source);Transfer(world,minor);}
                bool connected=Connected(source)&&Connected(minor)&&city.Functioning;
                decimal regional=connected?10m*output/Baseline[r]:0;
                city.Regional+=regional;
                preview?.Add(new SourceOutputPreview(source.Node,city.Node,output,Baseline[r],regional,
                    !connected?"Parent chain interrupted; local storage only":output<Baseline[r]*multiplier*source.OutputCondition?"Storage limits production":"Export chain available"));
            }
            return Locations.Where(l=>l.IsCity).Sum(l=>l.Regional);
        }
        public decimal ProjectedRegional(CrossroadsScenario world,Side side)
            =>PreviewSources(world).Where(s=>s.City==world.OwnKeep(side)).Sum(s=>s.Regional);
        public IReadOnlyList<SourceOutputPreview> PreviewSources(CrossroadsScenario world)
        {
            var copy=new CrossroadsScenario(foundations:true);
            copy.Foundations=CityFoundationData.Capture(this).Restore(world.Refresh);
            copy.West.Gold=world.West.Gold;copy.East.Gold=world.East.Gold;
            copy.West.KeepFood=world.West.KeepFood;copy.East.KeepFood=world.East.KeepFood;
            var result=new List<SourceOutputPreview>();
            copy.Foundations.Produce(copy,true,result);return result.AsReadOnly();
        }
        public string ProjectBlocker(CrossroadsScenario world,int node,CityProjectKind kind,bool continuation=false)
        {
            var site=Location(node);if(site==null||!site.Functioning)return "Location unavailable.";var realm=Realm(site.Controller);
            decimal regional=continuation?site.Regional:ProjectedRegional(world,site.Controller);
            bool minor=kind==CityProjectKind.ExtractionDepot||kind==CityProjectKind.AssayOffice;
            if(minor!=site.IsMinor||!minor&&!site.IsCity)return "Wrong location type.";
            switch(kind){
                case CityProjectKind.DevelopmentII:if(site.Development!=1)return "Development II already reached.";if(regional<20)return "Regional 20 required.";break;
                case CityProjectKind.DevelopmentIII:if(site.Development!=2)return "Development II required / III already reached.";if(regional<40)return "Regional 40 required.";break;
                case CityProjectKind.Forge:if(site.Forge)return "Forge already built.";if(site.Development<2||!realm.Knows(CityTech.ForgeOrganization))return "Development II + Forge Organization required.";break;
                case CityProjectKind.ResearchInstitute:if(site.Institute)return "Institute already built.";if(site.Development<3||regional<50||!realm.Knows(CityTech.ResearchMethod))return "Development III, Regional 50, Research Method required.";if(site.PhysicalLoad+(continuation?site.ReservedLoad:2)>site.DeepCapacity)return "Insufficient Deep Capacity.";break;
                case CityProjectKind.ExtractionDepot:if(site.Depot)return "Depot already built.";if(!realm.Knows(CityTech.Extraction))return "Extraction research required.";break;
                case CityProjectKind.AssayOffice:if(site.Assay)return "Assay Office already built.";if(!realm.Knows(CityTech.Assay))return "Assay research required.";break;
                case CityProjectKind.MageTowerI:if(!Combined||site.MageTower!=0)return "05B only / Tower already built.";break;
                case CityProjectKind.MageTowerII:if(!Combined||realm.Preset==CombatPreset.Support||site.MageTower!=1||site.Development<2||!realm.Knows(CityTech.ElementalDrills))return "HOM direction, Tower I, Development II, Elemental Drills required.";break;
            }
            return null;
        }
        private bool Pay(CrossroadsScenario w,CityLocation site,int gold,int wood,int iron)
        {
            bool global=site.IsCity||Connected(site);var realm=Realm(site.Controller);var force=w.Force(site.Controller);
            if(global){if(force.Gold<gold||realm.Wood<wood||realm.Iron<iron)return false;force.Gold-=gold;realm.Wood-=wood;realm.Iron-=iron;}
            else {if(site.Stock[0]<gold||site.Stock[2]<wood||site.Stock[3]<iron)return false;site.Stock[0]-=gold;site.Stock[2]-=wood;site.Stock[3]-=iron;}return true;
        }
        public bool QueueProject(CrossroadsScenario w,Side side,int node,CityProjectKind kind)
        {
            var site=Location(node);if(!w.CanAct(side)||site==null||site.Controller!=side||!Enum.IsDefined(typeof(CityProjectKind),kind)||site.Queue.Contains(kind)||site.Active?.Kind==kind)return false;
            bool minorProject=kind==CityProjectKind.ExtractionDepot||kind==CityProjectKind.AssayOffice;
            if(minorProject?!site.IsMinor:!site.IsCity)return false;
            if(kind==CityProjectKind.MageTowerII&&Realm(side).Preset==CombatPreset.Support)return false;
            if((kind==CityProjectKind.MageTowerI||kind==CityProjectKind.MageTowerII)&&!Combined)return false;
            site.Queue.Add(kind);Activate(w,site);return true;
        }
        private void Activate(CrossroadsScenario w,CityLocation site)
        {
            if(site.Active!=null||site.Queue.Count==0)return;var kind=site.Queue[0];if(ProjectBlocker(w,site.Node,kind)!=null)return;var c=Cost(kind);
            if(!Pay(w,site,c.gold,c.wood,c.iron))return;
            site.Active=new CityProject{Kind=kind,Started=w.Refresh};site.Queue.RemoveAt(0);
        }
        public bool PauseProject(CrossroadsScenario w,Side side,int node)
        {var s=Location(node);if(!w.CanAct(side)||s?.Controller!=side||s.Active==null)return false;s.Active.Paused=!s.Active.Paused;return true;}
        public bool CancelProject(CrossroadsScenario w,Side side,int node,int queued=-1)
        {var s=Location(node);if(!w.CanAct(side)||s?.Controller!=side)return false;if(queued>=0){if(queued>=s.Queue.Count)return false;s.Queue.RemoveAt(queued);return true;}if(s.Active==null)return false;s.Active=null;Activate(w,s);return true;}
        public void Capture(int node,Side side)
        {var site=Location(node);if(site==null||site.IsCity||site.Controller==side)return;site.Controller=side;site.Queue.Clear();if(site.Active!=null)site.Active.Paused=true;}
        public bool QueueResearch(CrossroadsScenario w,Side side,CityTech tech)
        {
            var r=Realm(side);if(!w.CanAct(side)||!Enum.IsDefined(typeof(CityTech),tech)||r.Knows(tech)||r.ActiveResearch==tech||r.ResearchQueue.Contains(tech))return false;
            if(tech==CityTech.ElementalDrills&&(!Combined||r.Preset==CombatPreset.Support))return false;
            r.ResearchQueue.Add(tech);ActivateResearch(w,side);return true;
        }
        private void ActivateResearch(CrossroadsScenario w,Side side)
        {
            var r=Realm(side);if(r.ActiveResearch.HasValue||r.ResearchQueue.Count==0)return;var p=r.Research.Single(t=>t.Tech==r.ResearchQueue[0]);
            if(!p.Paid){if(w.Force(side).Gold<40)return;w.Force(side).Gold-=40;p.Paid=true;}
            r.ActiveResearch=p.Tech;r.ResearchQueue.RemoveAt(0);r.ResearchStarted=w.Refresh;r.ResearchPaused=false;
        }
        public bool PauseResearch(CrossroadsScenario w,Side side)
        {var r=Realm(side);if(!w.CanAct(side)||!r.ActiveResearch.HasValue)return false;r.ResearchPaused=!r.ResearchPaused;return true;}
        public bool RemoveQueuedResearch(CrossroadsScenario w,Side side,int index)
        {var r=Realm(side);if(!w.CanAct(side)||index<0||index>=r.ResearchQueue.Count)return false;r.ResearchQueue.RemoveAt(index);return true;}
        internal void ReconcileCasualties(CrossroadsScenario w)
        {foreach(var side in new[]{Side.West,Side.East}){var r=Realm(side);if(r.Training!=null&&w.Force(side).Formation.Members.Single(c=>c.CharacterId==r.Training.CharacterId).Status==PersistentCharacterStatus.Dead)r.Training=null;}}
        public bool StopResearch(CrossroadsScenario w,Side side)
        {var r=Realm(side);if(!w.CanAct(side)||!r.ActiveResearch.HasValue)return false;r.ActiveResearch=null;r.ResearchPaused=false;ActivateResearch(w,side);return true;}
        public ForgeOrder QuoteRepair(CrossroadsScenario w,Side side)
        {
            var f=w.Force(side);var city=City(side);if(!w.CanAct(side)||f.Node!=city.Node||!city.Functioning||!city.Forge||Realm(side).Repair!=null)return null;
            var q=new ForgeOrder{Started=w.Refresh};foreach(var c in f.Formation.LivingMembers){int n=Math.Min(c.Profile.MaxArmor-c.Armor,(c.Profile.MaxArmor+1)/2);if(n>0)q.Quotes.Add(c.CharacterId,n);}return q.Cost==0?null:q;
        }
        public bool OrderRepair(CrossroadsScenario w,Side side)
        {var q=QuoteRepair(w,side);if(q==null||w.Force(side).Gold<q.Cost)return false;w.Force(side).Gold-=q.Cost;Realm(side).Repair=q;return true;}
        internal void LeftCity(Side side,int refresh){Realm(side).Repair=null;if(Realm(side).Training!=null)Realm(side).Training.Started=refresh;}
        internal void RefreshEconomy(CrossroadsScenario w)
        {
            var rates=Locations.Where(l=>l.IsCity&&l.Functioning).ToDictionary(l=>l.Node,l=>l.Institute?6m:2m);
            Produce(w,true);
            var eligible=Locations.Where(l=>l.Active!=null&&ProjectBlocker(w,l.Node,l.Active.Kind,true)==null).Select(l=>l.Node).ToHashSet();
            foreach(var side in new[]{Side.West,Side.East}) {
                var realm=Realm(side);var active=realm.ActiveResearch;
                if(active.HasValue&&!realm.ResearchPaused&&realm.ResearchStarted<w.Refresh) {
                    var p=realm.Research.Single(t=>t.Tech==active);var contributors=Locations.Where(l=>l.IsCity&&l.Controller==side&&rates.ContainsKey(l.Node)).OrderBy(l=>l.Node).ToArray();decimal total=contributors.Sum(l=>rates[l.Node]);
                    var credits=ResearchAccounting.Accept(contributors.ToDictionary(l=>l.Node,l=>rates[l.Node]),TechCost(p.Tech)-p.Work);
                    foreach(var credit in credits)p.Provenance[credit.Key]=(p.Provenance.TryGetValue(credit.Key,out var prior)?prior:0)+credit.Value;
                    p.Work+=credits.Values.Sum();if(p.Completed)realm.ActiveResearch=null;
                }
            }
            foreach(var site in Locations.OrderBy(l=>l.Node)) {
                var p=site.Active;if(p==null||p.Started>=w.Refresh||p.Paused||!eligible.Contains(site.Node))continue;
                if(++p.Steps<Cost(p.Kind).steps)continue;
                switch(p.Kind){case CityProjectKind.DevelopmentII:site.Development=2;break;case CityProjectKind.DevelopmentIII:site.Development=3;break;case CityProjectKind.Forge:site.Forge=true;break;case CityProjectKind.ResearchInstitute:site.Institute=true;break;case CityProjectKind.ExtractionDepot:site.Depot=true;break;case CityProjectKind.AssayOffice:site.Assay=true;break;case CityProjectKind.MageTowerI:site.MageTower=1;break;case CityProjectKind.MageTowerII:site.MageTower=2;break;}site.Active=null;
            }
            foreach(var side in new[]{Side.West,Side.East}) {
                var realm=Realm(side);var f=w.Force(side);var repair=realm.Repair;
                if(repair!=null){if(f.Node!=City(side).Node||!City(side).Functioning||!City(side).Forge)realm.Repair=null;else if(repair.Started<w.Refresh){foreach(var c in f.Formation.LivingMembers)if(repair.Quotes.TryGetValue(c.CharacterId,out int n))c.RepairArmor(n);realm.Repair=null;}}
                AdvanceTraining(w,side);
                foreach(var c in f.Formation.Members)c.ResetSourceBudgets();
                ActivateResearch(w,side);
            }
            foreach(var site in Locations)Activate(w,site);
        }
        internal void InitializeRegional(CrossroadsScenario w)
        {foreach(var city in Locations.Where(l=>l.IsCity))city.Regional=0;}
        internal void InitializeCombined(CrossroadsScenario w)
        {
            if(!Combined)return;
            foreach(var side in new[]{Side.West,Side.East}) {
                var preset=Realm(side).Preset;
                var profiles=new[]{UnitProfile.HumanWarriorTI,UnitProfile.HumanWarriorTI,UnitProfile.HumanArcherTI,UnitProfile.ElfWarriorTII,preset==CombatPreset.Fire?UnitProfile.FireMageTII:preset==CombatPreset.Ice?UnitProfile.IceMageTII:UnitProfile.HumanHealerTI};
                w.Force(side).Formation=new PersistentFormation("duel-"+side,side,profiles.Select((p,i)=>new PersistentCharacter("duel-"+side+"-"+(i+1),p,i==0,personalXp:p.Tier==2?20m:0m)));
            }
        }
        public string TrainingBlocker(CrossroadsScenario w,Side side,string id)
        {
            var r=Realm(side);var f=w.Force(side);var c=f.Formation.Members.SingleOrDefault(m=>m.CharacterId==id);
            if(!w.CanAct(side)||!Combined||r.Preset==CombatPreset.Support||f.Formation.Commanderless||f.Node!=w.OwnKeep(side)||City(side).MageTower!=2||!r.Knows(CityTech.ElementalDrills))return "HOM formation at own Tower II, Drills, Commander required.";
            if(r.Training!=null)return "A trainee is already pending.";
            if(c==null||c.Status==PersistentCharacterStatus.Dead||c.Profile.Tier!=1||!c.Profile.IsFireMage&&!c.Profile.IsIceMage||c.PersonalLevel<3)return "Living HOM TI, Personal Level 3 required.";
            return f.Gold<75?"75 Gold required.":null;
        }
        public bool Train(CrossroadsScenario w,Side side,string id,bool permanentSpellConfirmed)
        {if(!permanentSpellConfirmed||TrainingBlocker(w,side,id)!=null)return false;w.Force(side).Gold-=75;Realm(side).Training=new MageTraining{CharacterId=id,Started=w.Refresh};return true;}
        public bool CancelTraining(CrossroadsScenario w,Side side)
        {if(!w.CanAct(side)||Realm(side).Training==null)return false;Realm(side).Training=null;return true;}
        private void AdvanceTraining(CrossroadsScenario w,Side side)
        {
            var r=Realm(side);if(r.Training==null)return;var c=w.Force(side).Formation.Members.Single(m=>m.CharacterId==r.Training.CharacterId);
            if(c.Status==PersistentCharacterStatus.Dead){r.Training=null;return;}
            if(w.Force(side).Formation.Commanderless||w.Force(side).Node!=w.OwnKeep(side)||!City(side).Functioning||City(side).MageTower<2){r.Training.Started=w.Refresh;return;}
            if(r.Training.Started<w.Refresh){c.TrainMageTierII();r.Training=null;}
        }
    }
}

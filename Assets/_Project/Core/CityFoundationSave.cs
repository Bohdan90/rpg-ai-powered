using System;
using System.Globalization;
using System.IO;
using System.Linq;
namespace RPG.Core
{
    [Serializable] public sealed class CityProjectData
    {
        public int kind,started,steps; public bool paused;
        public static CityProjectData Capture(CityProject p)=>p==null?null:new CityProjectData{kind=(int)p.Kind,started=p.Started,steps=p.Steps,paused=p.Paused};
        public CityProject Restore(int refresh){StrategicSaveData.Require(Enum.IsDefined(typeof(CityProjectKind),kind)&&started>=1&&started<=refresh&&steps>=0&&steps<CityFoundations.Cost((CityProjectKind)kind).steps,"Invalid paid project.");return new CityProject{Kind=(CityProjectKind)kind,Started=started,Steps=steps,Paused=paused};}
        public void Write(BinaryWriter w){w.Write(kind);w.Write(started);w.Write(steps);w.Write(paused);}
    }
    [Serializable] public sealed class CityLocationData
    {
        public int node,parent,controller,development,tower; public bool functioning,forge,institute,depot,assay,hasActive;
        public string[] stock;public string regional,condition;public int[] queue;public CityProjectData active;
        public static CityLocationData Capture(CityLocation l)=>new CityLocationData{node=l.Node,parent=l.Parent,controller=(int)l.Controller,development=l.Development,tower=l.MageTower,functioning=l.Functioning,forge=l.Forge,institute=l.Institute,depot=l.Depot,assay=l.Assay,stock=l.Stock.Select(CityFoundationData.Number).ToArray(),regional=CityFoundationData.Number(l.Regional),condition=CityFoundationData.Number(l.OutputCondition),queue=l.Queue.Select(k=>(int)k).ToArray(),hasActive=l.Active!=null,active=CityProjectData.Capture(l.Active)};
        public CityLocation Restore(CityLocation expected,int refresh)
        {
            StrategicSaveData.Require(node==expected.Node&&parent==expected.Parent&&CrossroadsScenario.ValidSide((Side)controller)&&(!expected.IsCity||controller==(int)expected.Controller),"Changed fixed parent/protected City.");
            StrategicSaveData.Require(development>=1&&development<=3&&tower>=0&&tower<=2&&stock?.Length==4&&queue!=null&&queue.Length<=8&&queue.Distinct().Count()==queue.Length&&queue.All(k=>Enum.IsDefined(typeof(CityProjectKind),k)),"Invalid location/queue.");
            var l=new CityLocation{Node=node,Parent=parent,Controller=(Side)controller,Development=development,MageTower=tower,Functioning=functioning,Forge=forge,Institute=institute,Depot=depot,Assay=assay,Stock=stock.Select(CityFoundationData.Decimal).ToArray(),Regional=CityFoundationData.Decimal(regional),OutputCondition=CityFoundationData.Decimal(condition),Queue=queue.Select(k=>(CityProjectKind)k).ToList(),Active=hasActive?(active??throw new InvalidDataException("Missing active project")).Restore(refresh):null};
            StrategicSaveData.Require(l.OutputCondition<=1,"Invalid source condition.");
            for(int i=0;i<4;i++)StrategicSaveData.Require(l.Stock[i]<=CityFoundations.Baseline[i]*(l.IsMinor?4:3),"Stock over capacity.");
            StrategicSaveData.Require(l.PhysicalLoad+l.ReservedLoad<=l.DeepCapacity,"Overbooked Deep Capacity.");return l;
        }
        public void Write(BinaryWriter w){foreach(int n in new[]{node,parent,controller,development,tower})w.Write(n);foreach(bool b in new[]{functioning,forge,institute,depot,assay})w.Write(b);foreach(var s in stock)w.Write(s);w.Write(regional);w.Write(condition);w.Write(queue.Length);foreach(int k in queue)w.Write(k);w.Write(hasActive);if(hasActive)active.Write(w);}
    }
    [Serializable] public sealed class ResearchData
    {
        public int tech;public string work;public bool paid;public int[] cities;public string[] contributions;
        public static ResearchData Capture(ResearchProgress p)=>new ResearchData{tech=(int)p.Tech,work=CityFoundationData.Number(p.Work),paid=p.Paid,cities=p.Provenance.Keys.ToArray(),contributions=p.Provenance.Values.Select(CityFoundationData.Number).ToArray()};
        public ResearchProgress Restore()
        {
            StrategicSaveData.Require(Enum.IsDefined(typeof(CityTech),tech)&&cities!=null&&contributions!=null&&cities.Length==contributions.Length&&cities.Length<=8&&cities.Distinct().Count()==cities.Length,"Invalid research provenance.");
            var p=new ResearchProgress{Tech=(CityTech)tech,Work=CityFoundationData.Decimal(work),Paid=paid};
            for(int i=0;i<cities.Length;i++){StrategicSaveData.Require(cities[i]==1||cities[i]==13,"Unknown provenance City.");p.Provenance.Add(cities[i],CityFoundationData.Decimal(contributions[i]));}
            StrategicSaveData.Require(p.Work<=CityFoundations.TechCost(p.Tech)&&p.Work==p.Provenance.Values.Sum()&&(p.Paid||p.Work==0),"Research work/payment mismatch.");return p;
        }
        public void Write(BinaryWriter w){w.Write(tech);w.Write(work);w.Write(paid);w.Write(cities.Length);for(int i=0;i<cities.Length;i++){w.Write(cities[i]);w.Write(contributions[i]);}}
    }
    [Serializable] public sealed class CityRealmData
    {
        public string wood,iron;public int preset,active,started;public bool paused;public int[] queue;public ResearchData[] research;
        public int repairStarted,trainingStarted;public string[] repairIds;public int[] repairs;public string trainee;
        public static CityRealmData Capture(CityRealm r)=>new CityRealmData{wood=CityFoundationData.Number(r.Wood),iron=CityFoundationData.Number(r.Iron),preset=(int)r.Preset,active=r.ActiveResearch.HasValue?(int)r.ActiveResearch.Value:-1,started=r.ResearchStarted,paused=r.ResearchPaused,queue=r.ResearchQueue.Select(t=>(int)t).ToArray(),research=r.Research.Select(ResearchData.Capture).ToArray(),repairStarted=r.Repair?.Started??0,repairIds=r.Repair?.Quotes.Keys.ToArray()??Array.Empty<string>(),repairs=r.Repair?.Quotes.Values.ToArray()??Array.Empty<int>(),trainingStarted=r.Training?.Started??0,trainee=r.Training?.CharacterId??""};
        public CityRealm Restore(int refresh)
        {
            StrategicSaveData.Require(Enum.IsDefined(typeof(CombatPreset),preset)&&(active==-1||Enum.IsDefined(typeof(CityTech),active))&&started>=0&&started<=refresh&&queue!=null&&queue.Length<=5&&queue.Distinct().Count()==queue.Length&&queue.All(k=>Enum.IsDefined(typeof(CityTech),k)),"Invalid research queue.");
            StrategicSaveData.Require(research?.Length==5&&research.All(p=>p!=null)&&research.Select(p=>p.tech).Distinct().Count()==5,"Invalid research records.");
            var r=new CityRealm{Wood=CityFoundationData.Decimal(wood),Iron=CityFoundationData.Decimal(iron),Preset=(CombatPreset)preset,ActiveResearch=active<0?(CityTech?)null:(CityTech)active,ResearchStarted=started,ResearchPaused=paused,ResearchQueue=queue.Select(k=>(CityTech)k).ToList(),Research=research.Select(p=>p.Restore()).ToList()};
            if(r.ActiveResearch.HasValue)StrategicSaveData.Require(started>0&&r.Research.Single(p=>p.Tech==r.ActiveResearch).Paid&&!r.Knows(r.ActiveResearch.Value),"Invalid active research payment.");
            StrategicSaveData.Require(repairStarted>=0&&repairStarted<=refresh&&repairIds!=null&&repairs!=null&&repairIds.Length==repairs.Length&&repairIds.Length<=100&&repairIds.Distinct().Count()==repairIds.Length&&repairs.All(n=>n>0&&n<=1000),"Invalid Forge order.");
            if(repairStarted>0){r.Repair=new ForgeOrder{Started=repairStarted};for(int i=0;i<repairIds.Length;i++)r.Repair.Quotes.Add(repairIds[i],repairs[i]);}
            StrategicSaveData.Require(trainingStarted>=0&&trainingStarted<=refresh&&trainee!=null,"Invalid training.");if(trainingStarted>0)r.Training=new MageTraining{Started=trainingStarted,CharacterId=trainee};return r;
        }
        public void Write(BinaryWriter w){w.Write(wood);w.Write(iron);w.Write(preset);w.Write(active);w.Write(started);w.Write(paused);w.Write(queue.Length);foreach(int k in queue)w.Write(k);foreach(var p in research)p.Write(w);w.Write(repairStarted);w.Write(repairIds.Length);for(int i=0;i<repairIds.Length;i++){w.Write(repairIds[i]);w.Write(repairs[i]);}w.Write(trainingStarted);w.Write(trainee);}
    }
    [Serializable] public sealed class CityFoundationData
    {
        public bool combined;public CityLocationData[] locations;public CityRealmData west,east;
        internal static string Number(decimal n)=>n.ToString(CultureInfo.InvariantCulture);
        internal static decimal Decimal(string s){StrategicSaveData.Require(decimal.TryParse(s,NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out var n)&&n>=0&&n<=1000000000m,"Invalid precise quantity.");return n;}
        public static CityFoundationData Capture(CityFoundations f)=>new CityFoundationData{combined=f.Combined,locations=f.Locations.Select(CityLocationData.Capture).ToArray(),west=CityRealmData.Capture(f.West),east=CityRealmData.Capture(f.East)};
        public CityFoundations Restore(int refresh)
        {
            var f=new CityFoundations(combined);StrategicSaveData.Require(locations?.Length==12&&locations.All(l=>l!=null)&&locations.Select(l=>l.node).Distinct().Count()==12&&west!=null&&east!=null,"Invalid foundation locations.");
            f.Locations=locations.Select(l=>l.Restore(f.Location(l.node)??throw new InvalidDataException("Unknown location"),refresh)).ToList();f.West=west.Restore(refresh);f.East=east.Restore(refresh);
            if(!combined)StrategicSaveData.Require(f.Locations.All(l=>l.MageTower==0)&&f.West.Training==null&&f.East.Training==null,"Mage data in 05A.");return f;
        }
        public void Write(BinaryWriter w){w.Write(combined);w.Write(locations.Length);foreach(var l in locations)l.Write(w);west.Write(w);east.Write(w);}
    }
}

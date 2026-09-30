using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace RPG.Core
{
    [Serializable] public sealed class DuelSaveForce
    {
        public int node,tempo,provisions,pressure,gold,keepFood,nextRecruit,pendingProfile,lastRecruitRefresh;
        public string pendingId;
        public StrategicSaveFormation formation;
        internal static DuelSaveForce Capture(DuelForce f)=>new DuelSaveForce {node=f.Node,tempo=f.Tempo,provisions=f.Provisions,pressure=f.Pressure,gold=f.Gold,keepFood=f.KeepFood,nextRecruit=f.NextRecruit,pendingProfile=f.PendingRecruit.HasValue?(int)f.PendingRecruit.Value:-1,pendingId=f.PendingRecruitId,lastRecruitRefresh=f.LastRecruitRefresh,formation=StrategicSaveFormation.Capture(f.Formation)};
        internal DuelForce Restore(Side side,bool economy,int refresh,StrategicGraph graph=null)
        {
            StrategicSaveData.Require((graph??CrossroadsScenario.Map).Node(node)!=null&&tempo>=-40&&tempo<=100&&provisions>=0&&provisions<=30&&pressure>=0&&pressure<=3000000&&formation!=null,"Invalid duel force.");
            StrategicSaveData.Require(gold>=0&&gold<=1000000000&&keepFood>=0&&keepFood<=36&&nextRecruit>=1&&nextRecruit<=10000
                &&(pendingProfile==-1||pendingProfile==(int)UnitProfileId.HumanWarriorTI||pendingProfile==(int)UnitProfileId.HumanArcherTI)
                &&lastRecruitRefresh>=0&&lastRecruitRefresh<=refresh&&pendingId!=null,"Invalid economy/queue.");
            StrategicSaveData.Require(economy||(gold==0&&keepFood==0&&nextRecruit==1&&pendingProfile==-1&&lastRecruitRefresh==0),"Economy data in Part A.");
            int recruits=nextRecruit-1-(pendingProfile>=0?1:0);
            StrategicSaveData.Require(formation.members!=null&&recruits>=0&&formation.members.Length==6+recruits
                &&formation.members.All(c=>c!=null),"Invalid recruited roster size.");
            StrategicSaveData.Require(pendingProfile<0?pendingId=="":lastRecruitRefresh>0&&pendingId=="duel-"+side+"-recruit-"+(nextRecruit-1),"Invalid pending identity.");
            var f=new DuelForce(side);var expected=f.Formation.Members.ToList();
            for(int i=0;i<recruits;i++)
            {
                var c=formation.members[6+i];StrategicSaveData.Require(c.profile==(int)UnitProfileId.HumanWarriorTI||c.profile==(int)UnitProfileId.HumanArcherTI,"Invalid recruit profile.");
                expected.Add(new PersistentCharacter("duel-"+side+"-recruit-"+(i+1),c.profile==(int)UnitProfileId.HumanWarriorTI?UnitProfile.HumanWarriorTI:UnitProfile.HumanArcherTI));
            }
            f.Formation=formation.Restore(new PersistentFormation(f.Formation.FormationId,side,expected));
            f.Gold=gold;f.KeepFood=keepFood;f.NextRecruit=nextRecruit;f.PendingRecruit=pendingProfile<0?(UnitProfileId?)null:(UnitProfileId)pendingProfile;f.PendingRecruitId=pendingId;f.LastRecruitRefresh=lastRecruitRefresh;f.Node=node;f.Tempo=tempo;f.Provisions=provisions;f.Pressure=pressure;return f;
        }
        internal void Write(BinaryWriter w){w.Write(node);w.Write(tempo);w.Write(provisions);w.Write(pressure);w.Write(gold);w.Write(keepFood);w.Write(nextRecruit);w.Write(pendingProfile);w.Write(pendingId);w.Write(lastRecruitRefresh);formation.Write(w);}
    }
    [Serializable] public sealed class CrossroadsSaveData
    {
        public const int Version=2;
        public const string Scenario="CrossroadsDuel-02";
        public int version,refresh,startingSide,activeSide,completed,winner,battleNumber;
        public uint seed;
        public bool handoff,economy;
        public int waystationFood;
        public string scenario,checksum;
        public int[] owners;
        public string[] events;
        public DuelSaveForce west,east;
        public IncidentSaveData incident;
        public CrossroadsScenario Restore()
        {
            StrategicSaveData.Require((version==Version&&scenario==Scenario)||(version==3&&scenario==IncidentState.Id&&incident!=null),"Unsupported Crossroads schema/scenario.");
            var candidate=new CrossroadsScenario(this);
            StrategicSaveData.Require(!string.IsNullOrEmpty(checksum)&&checksum==ComputeHash(),"Crossroads checksum mismatch.");return candidate;
        }
        public string ComputeHash()
        {
            using(var stream=new MemoryStream())using(var w=new BinaryWriter(stream))
            {
                w.Write(version);w.Write(scenario);w.Write(seed);w.Write(refresh);w.Write(startingSide);w.Write(activeSide);w.Write(completed);w.Write(winner);w.Write(battleNumber);w.Write(handoff);w.Write(economy);w.Write(waystationFood);
                w.Write(owners.Length);foreach(int n in owners)w.Write(n);west.Write(w);east.Write(w);w.Write(events.Length);foreach(var e in events)w.Write(e);
                if(version==3)incident.Write(w);
                w.Flush();using(var sha=SHA256.Create())return Convert.ToBase64String(sha.ComputeHash(stream.ToArray()));
            }
        }
    }
    public sealed partial class CrossroadsScenario
    {
        public CrossroadsSaveData CaptureSave()
        {
            if(!CanSave)throw new InvalidOperationException("Finish battle before saving Crossroads.");
            var d=new CrossroadsSaveData {version=Incident==null?CrossroadsSaveData.Version:3,scenario=Incident==null?CrossroadsSaveData.Scenario:IncidentState.Id,incident=Incident==null?null:IncidentSaveData.Capture(Incident),seed=Seed,refresh=Refresh,
                startingSide=(int)StartingSide,activeSide=(int)ActiveSide,completed=CompletedActivations,handoff=HandoffPending,winner=Winner.HasValue?(int)Winner.Value:-1,
                battleNumber=battleNumber,economy=Economy,waystationFood=WaystationFood,owners=owners.Select(o=>o.HasValue?(int)o.Value:-1).ToArray(),west=DuelSaveForce.Capture(West),east=DuelSaveForce.Capture(East),events=events.ToArray()};
            d.checksum=d.ComputeHash();return d;
        }
        internal CrossroadsScenario(CrossroadsSaveData d):this((Side)d.startingSide,d.seed,d.economy,d.version==3,d.version==3&&d.incident.enabled)
        {
            StrategicSaveData.Require(d.refresh>=1&&d.refresh<=1000000&&d.completed>=0&&d.completed<=(d.version==3?2:1)&&d.battleNumber>=0&&d.battleNumber<=1000000,"Invalid duel counters.");
            StrategicSaveData.Require(ValidSide((Side)d.activeSide)&&d.activeSide==(int)(d.completed==0?StartingSide:Other(StartingSide)),"Invalid active side.");
            StrategicSaveData.Require(d.winner==-1||ValidSide((Side)d.winner),"Invalid winner.");
            StrategicSaveData.Require(d.owners!=null&&d.owners.Length==3&&d.owners.All(o=>o==-1||ValidSide((Side)o)),"Invalid objective owners.");
            StrategicSaveData.Require(d.west!=null&&d.east!=null&&d.events!=null&&d.events.Length<=20000&&d.events.All(e=>e!=null&&e.Length<=4096),"Incomplete snapshot.");
            StrategicSaveData.Require(d.waystationFood>=0&&d.waystationFood<=24&&(d.economy||d.waystationFood==0),"Invalid Waystation Food.");
            WaystationFood=d.waystationFood;West=d.west.Restore(Side.West,Economy,d.refresh,Graph);East=d.east.Restore(Side.East,Economy,d.refresh,Graph);
            StrategicSaveData.Require(!West.Continues||!East.Continues||West.Node!=East.Node,"Overlapping formations.");
            bool pressureWin=Math.Max(West.Pressure,East.Pressure)>=TargetPressure&&West.Pressure!=East.Pressure;
            Side? expected=!West.Continues?(Side?)Side.East:!East.Continues?Side.West:pressureWin?(West.Pressure>East.Pressure?Side.West:Side.East):(Side?)null;
            StrategicSaveData.Require(d.winner==(expected.HasValue?(int)expected.Value:-1)&&(!expected.HasValue||!d.handoff),"Inconsistent match result.");
            if(d.version==3){StrategicSaveData.Require(d.economy,"Incident requires economy rules.");Incident=d.incident.Restore(d.refresh,d.completed,d.battleNumber,d.winner>=0);StrategicSaveData.Require(Occupants.Select(f=>f.Node).Distinct().Count()==Occupants.Count(),"Overlapping incident armies.");StrategicSaveData.Require(!Incident.WorldPhase||!d.handoff,"Handoff during world phase.");}
            Refresh=d.refresh;ActiveSide=(Side)d.activeSide;CompletedActivations=d.completed;HandoffPending=d.handoff;Winner=expected;battleNumber=d.battleNumber;
            for(int i=0;i<3;i++)owners[i]=d.owners[i]<0?(Side?)null:(Side)d.owners[i];events.AddRange(d.events);
        }
    }
}

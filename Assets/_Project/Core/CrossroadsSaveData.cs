using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace RPG.Core
{
    [Serializable] public sealed class DuelSaveForce
    {
        public int node,tempo,provisions,pressure,gold,keepFood,nextRecruit,pendingProfile,lastRecruitRefresh;
        public string pendingId,goldExact,foodExact;
        public StrategicSaveFormation formation;
        internal static DuelSaveForce Capture(DuelForce f)=>new DuelSaveForce {node=f.Node,tempo=f.Tempo,provisions=f.Provisions,pressure=f.Pressure,gold=(int)f.Gold,keepFood=(int)f.KeepFood,goldExact=CityFoundationData.Number(f.Gold),foodExact=CityFoundationData.Number(f.KeepFood),nextRecruit=f.NextRecruit,pendingProfile=f.PendingRecruit.HasValue?(int)f.PendingRecruit.Value:-1,pendingId=f.PendingRecruitId,lastRecruitRefresh=f.LastRecruitRefresh,formation=StrategicSaveFormation.Capture(f.Formation)};
        internal DuelForce Restore(Side side,bool economy,int refresh,StrategicGraph graph=null,CityFoundations foundations=null)
        {
            if(foundations!=null)return RestoreCity(side,refresh,foundations);
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
        private DuelForce RestoreCity(Side side,int refresh,CityFoundations foundation)
        {
            StrategicSaveData.Require(CityFoundations.Map.Node(node)!=null&&tempo>=-40&&tempo<=100&&provisions>=0&&provisions<=30&&pressure>=0&&pressure<1000000&&nextRecruit>=1&&nextRecruit<=10000&&lastRecruitRefresh>=0&&lastRecruitRefresh<=refresh,"Invalid 05 force counters.");
            StrategicSaveData.Require(formation?.members!=null&&formation.members.All(c=>c!=null)&&pendingId!=null,"Missing 05 roster.");
            int initial=foundation.Combined?5:6;int count=nextRecruit-1-(pendingProfile>=0?1:0);
            StrategicSaveData.Require(count>=0&&formation.members.Length==initial+count,"05 roster/recruit count mismatch.");
            var expected=new System.Collections.Generic.List<PersistentCharacter>();
            var preset=foundation.Realm(side).Preset;
            var startProfiles=foundation.Combined?new[]{UnitProfileId.HumanWarriorTI,UnitProfileId.HumanWarriorTI,UnitProfileId.HumanArcherTI,UnitProfileId.ElfWarriorTII,preset==CombatPreset.Fire?UnitProfileId.FireMageTII:preset==CombatPreset.Ice?UnitProfileId.IceMageTII:UnitProfileId.HumanHealerTI}:
                new[]{UnitProfileId.HumanWarriorTI,UnitProfileId.HumanWarriorTI,UnitProfileId.HumanWarriorTI,UnitProfileId.HumanWarriorTI,UnitProfileId.HumanArcherTI,UnitProfileId.HumanArcherTI};
            for(int i=0;i<formation.members.Length;i++) {
                var c=formation.members[i];var profile=UnitProfile.Get((UnitProfileId)c.profile);string id=i<initial?"duel-"+side+"-"+(i+1):"duel-"+side+"-recruit-"+(i-initial+1);
                StrategicSaveData.Require(i<initial?c.profile==(int)startProfiles[i]:profile.Id==UnitProfileId.HumanWarriorTI||profile.IsArcher||foundation.Combined&&profile.IsCaster&&((preset==CombatPreset.Support)==(profile.Id==UnitProfileId.HumanHealerTI)),"Illegal 05 class/direction.");
                expected.Add(new PersistentCharacter(id,profile,i==0));
            }
            if(pendingProfile>=0){var p=UnitProfile.Get((UnitProfileId)pendingProfile);StrategicSaveData.Require(p.Tier==1&&!p.IsElf&&(!p.IsCaster||foundation.Combined&&(preset==CombatPreset.Support)==(p.Id==UnitProfileId.HumanHealerTI))&&pendingId=="duel-"+side+"-recruit-"+(nextRecruit-1),"Invalid paid recruit.");}
            else StrategicSaveData.Require(pendingProfile==-1&&pendingId=="","Invalid empty recruit.");
            var f=new DuelForce(side){Formation=formation.Restore(new PersistentFormation("duel-"+side,side,expected)),Node=node,Tempo=tempo,Provisions=provisions,Pressure=pressure,Gold=CityFoundationData.Decimal(goldExact),KeepFood=CityFoundationData.Decimal(foodExact),NextRecruit=nextRecruit,PendingRecruit=pendingProfile<0?(UnitProfileId?)null:(UnitProfileId)pendingProfile,PendingRecruitId=pendingId,LastRecruitRefresh=lastRecruitRefresh};
            StrategicSaveData.Require(f.KeepFood<=90,"City Food over capacity.");
            var realm=foundation.Realm(side);if(realm.Training!=null)StrategicSaveData.Require(f.Formation.Members.Any(c=>c.CharacterId==realm.Training.CharacterId&&c.Profile.Tier==1&&(c.Profile.IsFireMage||c.Profile.IsIceMage)),"Unknown trainee.");
            if(realm.Repair!=null)StrategicSaveData.Require(realm.Repair.Quotes.Keys.All(id=>f.Formation.Members.Any(c=>c.CharacterId==id)),"Unknown Forge identity.");
            return f;
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
        public SeamlessSaveData seamless; public RealmSaveData realm; public IncidentSaveData incident; public CityFoundationData foundations;
        public CrossroadsScenario Restore()
        {
            StrategicSaveData.Require((version==Version&&scenario==Scenario)||(version==3&&scenario==IncidentState.Id&&incident!=null)||(version==4&&scenario=="CityFoundations-05"&&foundations!=null)||(version==5&&scenario=="RealmOperations-06"&&foundations!=null&&realm!=null)||((version==6&&scenario==SeamlessWorlds.ScenarioId||version==7&&scenario==ProductionRoads.ScenarioId)&&foundations!=null&&realm!=null&&seamless!=null&&seamless.production==(version==7)),"Unsupported Crossroads schema/scenario.");
            var candidate=new CrossroadsScenario(this);
            StrategicSaveData.Require(!string.IsNullOrEmpty(checksum)&&checksum==ComputeHash(),"Crossroads checksum mismatch.");return candidate;
        }
        public string ComputeHash(bool includeKnowledge=true)
        {
            using(var stream=new MemoryStream())using(var w=new BinaryWriter(stream))
            {
                w.Write(version);w.Write(scenario);w.Write(seed);w.Write(refresh);w.Write(startingSide);w.Write(activeSide);w.Write(completed);w.Write(winner);w.Write(battleNumber);w.Write(handoff);w.Write(economy);w.Write(waystationFood);
                w.Write(owners.Length);foreach(int n in owners)w.Write(n);west.Write(w);east.Write(w);w.Write(events.Length);foreach(var e in events)w.Write(e);
                if(version==3)incident.Write(w);if(version>=4){foundations.Write(w);w.Write(west.goldExact);w.Write(west.foodExact);w.Write(east.goldExact);w.Write(east.foodExact);if(version>=5){w.Write(foundations.realmMode);realm.Write(w,version>=6);if(version>=6)seamless.Write(w,includeKnowledge);}}
                w.Flush();using(var sha=SHA256.Create())return Convert.ToBase64String(sha.ComputeHash(stream.ToArray()));
            }
        }
    }
    public sealed partial class CrossroadsScenario
    {
        public CrossroadsSaveData CaptureSave()
        {
            if(!CanSave)throw new InvalidOperationException("Finish battle before saving Crossroads.");
            var d=new CrossroadsSaveData {version=Seamless!=null?(Seamless.ProductionTopology?7:6):Realm!=null?5:Foundations!=null?4:Incident==null?CrossroadsSaveData.Version:3,scenario=Seamless!=null?(Seamless.ProductionTopology?ProductionRoads.ScenarioId:SeamlessWorlds.ScenarioId):Realm!=null?"RealmOperations-06":Foundations!=null?"CityFoundations-05":Incident==null?CrossroadsSaveData.Scenario:IncidentState.Id,foundations=Foundations==null?null:CityFoundationData.Capture(Foundations),incident=Incident==null?null:IncidentSaveData.Capture(Incident),seed=Seed,refresh=Refresh,
                startingSide=(int)StartingSide,activeSide=(int)ActiveSide,completed=CompletedActivations,handoff=HandoffPending,winner=Winner.HasValue?(int)Winner.Value:-1,
                battleNumber=battleNumber,economy=Economy,waystationFood=WaystationFood,owners=owners.Select(o=>o.HasValue?(int)o.Value:-1).ToArray(),west=DuelSaveForce.Capture(West),east=DuelSaveForce.Capture(East),events=events.ToArray()};
            if(Seamless!=null)d.seamless=SeamlessSaveData.Capture(Seamless);if(Realm!=null)d.realm=RealmSaveData.Capture(Realm);d.checksum=d.ComputeHash();return d;
        }
        internal CrossroadsScenario(CrossroadsSaveData d):this((Side)d.startingSide,d.seed,d.economy,d.version==3,d.version==3&&d.incident.enabled)
        {
            StrategicSaveData.Require(d.refresh>=1&&d.refresh<=1000000&&d.completed>=0&&d.completed<=(d.version==3?2:1)&&d.battleNumber>=0&&d.battleNumber<=1000000,"Invalid duel counters.");
            StrategicSaveData.Require(ValidSide((Side)d.activeSide)&&d.activeSide==(int)(d.completed==0?StartingSide:Other(StartingSide)),"Invalid active side.");
            StrategicSaveData.Require(d.winner==-1||ValidSide((Side)d.winner),"Invalid winner.");
            StrategicSaveData.Require(d.owners!=null&&d.owners.Length==3&&d.owners.All(o=>o==-1||ValidSide((Side)o)),"Invalid objective owners.");
            StrategicSaveData.Require(d.west!=null&&d.east!=null&&d.events!=null&&d.events.Length<=20000&&d.events.All(e=>e!=null&&e.Length<=4096),"Incomplete snapshot.");
            StrategicSaveData.Require(d.waystationFood>=0&&d.waystationFood<=24&&(d.economy||d.waystationFood==0),"Invalid Waystation Food.");
            if(d.version>=4){StrategicSaveData.Require(d.economy,"05 requires economy.");StrategicSaveData.Require(d.version>=5||!d.foundations.realmMode,"06 adapter in legacy save.");Foundations=d.foundations.Restore(d.refresh);}
            if(d.version>=5) {
                StrategicSaveData.Require(d.foundations.realmMode,"Missing 06 economy adapter.");
                Refresh=d.refresh;ActiveSide=(Side)d.activeSide;CompletedActivations=d.completed;HandoffPending=d.handoff;Winner=d.winner<0?(Side?)null:(Side)d.winner;battleNumber=d.battleNumber;WaystationFood=d.waystationFood;
                if(d.version>=6)Seamless=new SeamlessWorlds(this,d.seamless.temporary,false,d.version==7);
                Realm=d.realm.Restore(this);West=Realm.Armies.Single(f=>f.Formation.FormationId=="realm06-West-army-1");East=Realm.Armies.Single(f=>f.Formation.FormationId=="realm06-East-army-1");
                West.Gold=CityFoundationData.Decimal(d.west.goldExact);East.Gold=CityFoundationData.Decimal(d.east.goldExact);West.KeepFood=CityFoundationData.Decimal(d.west.foodExact);East.KeepFood=CityFoundationData.Decimal(d.east.foodExact);West.Pressure=d.west.pressure;East.Pressure=d.east.pressure;
                StrategicSaveData.Require(West.KeepFood<=180&&East.KeepFood<=180&&West.Pressure>=0&&East.Pressure>=0,"Invalid 06 treasury.");
                for(int i=0;i<3;i++)owners[i]=d.owners[i]<0?(Side?)null:(Side)d.owners[i];events.AddRange(d.events);
                if(d.version>=6)d.seamless.RestoreInto(Seamless);
                var expectedWinner=Winner;Realm.CheckVictory();StrategicSaveData.Require(expectedWinner==Winner,"Inconsistent 06 victory.");return;
            }
            WaystationFood=d.waystationFood;West=d.west.Restore(Side.West,Economy,d.refresh,Graph,Foundations);East=d.east.Restore(Side.East,Economy,d.refresh,Graph,Foundations);
            StrategicSaveData.Require(!West.Continues||!East.Continues||West.Node!=East.Node,"Overlapping formations.");
            bool pressureWin=Math.Max(West.Pressure,East.Pressure)>=TargetPressure&&West.Pressure!=East.Pressure;
            Side? expected=!West.Continues&&!East.Continues?null:!West.Continues?(Side?)Side.East:!East.Continues?Side.West:pressureWin?(West.Pressure>East.Pressure?Side.West:Side.East):(Side?)null;
            StrategicSaveData.Require(d.winner==(expected.HasValue?(int)expected.Value:-1)&&(!expected.HasValue||!d.handoff),"Inconsistent match result.");
            if(d.version==3){StrategicSaveData.Require(d.economy,"Incident requires economy rules.");Incident=d.incident.Restore(d.refresh,d.completed,d.battleNumber,d.winner>=0);StrategicSaveData.Require(Occupants.Select(f=>f.Node).Distinct().Count()==Occupants.Count(),"Overlapping incident armies.");StrategicSaveData.Require(!Incident.WorldPhase||!d.handoff,"Handoff during world phase.");}
            Refresh=d.refresh;ActiveSide=(Side)d.activeSide;CompletedActivations=d.completed;HandoffPending=d.handoff;Winner=expected;battleNumber=d.battleNumber;
            for(int i=0;i<3;i++)owners[i]=d.owners[i]<0?(Side?)null:(Side)d.owners[i];events.AddRange(d.events);
        }
    }
}

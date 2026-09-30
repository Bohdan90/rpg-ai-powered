using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
namespace RPG.Core
{
    [Serializable] public sealed class RealmArmyData
    {
        public string id,commander,provisions;public int side,node,tempo,staff;public StrategicSaveCharacter[] units;
        internal static RealmArmyData Capture(RealmOperations r,DuelForce f)=>new RealmArmyData{id=f.Formation.FormationId,side=(int)f.Formation.Side,commander=f.Formation.AssignedCommanderId??"",node=f.Node,tempo=f.Tempo,staff=r.staffDue[f.Formation.FormationId],provisions=CityFoundationData.Number(f.RealmProvisions),units=f.Formation.Members.Select(StrategicSaveCharacter.Capture).ToArray()};
        internal DuelForce Restore()
        {
            StrategicSaveData.Require(CrossroadsScenario.ValidSide((Side)side)&&id!=null&&id.StartsWith("realm06-"+(Side)side+"-army-",StringComparison.Ordinal)&&commander!=null&&CityFoundations.Map.Node(node)!=null&&node!=((Side)side==Side.West?13:1)&&tempo>=-40&&tempo<=100&&staff>=0&&staff<=100000,"Invalid 06 army.");
            var members=RealmSaveData.Characters(units);
            StrategicSaveData.Require(commander==""||members.Any(c=>c.CharacterId==commander&&c.IsCommander),"Invalid assigned Commander.");
            return new DuelForce((Side)side){Formation=new PersistentFormation(id,(Side)side,members,commander==""?null:commander,true),Node=node,Tempo=tempo,Provisions=0,RealmProvisions=CityFoundationData.Decimal(provisions)};
        }
        internal void Write(BinaryWriter w){w.Write(id);w.Write(side);w.Write(commander);w.Write(node);w.Write(tempo);w.Write(staff);w.Write(provisions);w.Write(units.Length);foreach(var c in units)c.Write(w);}
    }
    [Serializable] public sealed class RealmServiceData
    {
        public string kind,unit,destination;public int started,profile;public string[] ids;public int[] amounts;
        internal static RealmServiceData Capture(RealmService s)=>s==null?null:new RealmServiceData{kind=s.Kind,unit=s.Unit??"",destination=s.Destination??"",started=s.Started,profile=(int)s.Profile,ids=s.Quotes.Keys.ToArray(),amounts=s.Quotes.Values.ToArray()};
        internal RealmService Restore(int refresh)
        {
            StrategicSaveData.Require(new[]{"Commission","Recruit","Training","Repair"}.Contains(kind)&&unit!=null&&destination!=null&&started>0&&started<=refresh&&ids!=null&&amounts!=null&&ids.Length==amounts.Length&&ids.Distinct().Count()==ids.Length&&amounts.All(n=>n>0&&n<1000),"Invalid 06 paid service.");
            var s=new RealmService{Kind=kind,Unit=unit,Destination=destination,Started=started,Profile=(UnitProfileId)profile};for(int i=0;i<ids.Length;i++)s.Quotes.Add(ids[i],amounts[i]);return s;
        }
        internal static void WriteOptional(BinaryWriter w,RealmServiceData s){w.Write(s!=null);if(s==null)return;w.Write(s.kind);w.Write(s.unit);w.Write(s.destination);w.Write(s.started);w.Write(s.profile);w.Write(s.ids.Length);for(int i=0;i<s.ids.Length;i++){w.Write(s.ids[i]);w.Write(s.amounts[i]);}}
    }
    [Serializable] public sealed class RealmSideData
    {
        public bool hasCommission,hasRecruit;public StrategicSaveCharacter[] reserve;public string[] commissionQueue;public RealmServiceData commission,recruit;public RealmServiceData[] services;
        public int nextArmy,nextRecruit,lastRecruit,cityStaff;public string selected;
        internal static RealmSideData Capture(RealmSideState s)=>new RealmSideData{hasCommission=s.Commission!=null,hasRecruit=s.Recruit!=null,reserve=s.Reserve.Select(StrategicSaveCharacter.Capture).ToArray(),commissionQueue=s.CommissionQueue.ToArray(),commission=RealmServiceData.Capture(s.Commission),recruit=RealmServiceData.Capture(s.Recruit),services=s.Services.Select(RealmServiceData.Capture).ToArray(),nextArmy=s.NextArmy,nextRecruit=s.NextRecruit,lastRecruit=s.LastRecruit,cityStaff=s.CityStaffDue,selected=s.Selected};
        internal RealmSideState Restore(int refresh)
        {
            StrategicSaveData.Require(nextArmy>=3&&nextArmy<=100000&&nextRecruit>=1&&nextRecruit<=100000&&lastRecruit>=0&&lastRecruit<=refresh&&cityStaff>=0&&cityStaff<=100000&&selected!=null&&services!=null&&services.Length<=10000&&services.All(s=>s!=null)&&commissionQueue!=null&&commissionQueue.Distinct().Count()==commissionQueue.Length,"Invalid 06 realm counters.");
            return new RealmSideState{Reserve=RealmSaveData.Characters(reserve).ToList(),CommissionQueue=commissionQueue.ToList(),Commission=hasCommission?(commission??throw new InvalidDataException("Missing Commission")).Restore(refresh):null,Recruit=hasRecruit?(recruit??throw new InvalidDataException("Missing Recruit")).Restore(refresh):null,Services=services.Select(s=>s.Restore(refresh)).ToList(),NextArmy=nextArmy,NextRecruit=nextRecruit,LastRecruit=lastRecruit,CityStaffDue=cityStaff,Selected=selected};
        }
        internal void Write(BinaryWriter w){w.Write(reserve.Length);foreach(var c in reserve)c.Write(w);w.Write(commissionQueue.Length);foreach(var id in commissionQueue)w.Write(id);RealmServiceData.WriteOptional(w,hasCommission?commission:null);RealmServiceData.WriteOptional(w,hasRecruit?recruit:null);w.Write(services.Length);foreach(var s in services)RealmServiceData.WriteOptional(w,s);w.Write(nextArmy);w.Write(nextRecruit);w.Write(lastRecruit);w.Write(cityStaff);w.Write(selected);}
    }
    [Serializable] public sealed class RealmCycleData { public string id; public int ceiling,deprivation,born; }
    [Serializable] public sealed class RealmFamiliarityData { public string key;public int load; }
    [Serializable] public sealed class RealmSaveData
    {
        public bool hasLastBattle; public RealmBattleHistory lastBattle; public RealmArmyData[] armies;public RealmSideData west,east;public RealmCycleData[] cycles;public RealmFamiliarityData[] familiarity;public int appliedBattle;public string message;
        internal static PersistentCharacter[] Characters(StrategicSaveCharacter[] data)
        {
            StrategicSaveData.Require(data!=null&&data.Length<=10000&&data.All(c=>c!=null)&&data.Select(c=>c.id).Distinct().Count()==data.Length,"Invalid 06 characters.");
            return data.Select(c=>c.Restore(new PersistentCharacter(c.id,UnitProfile.Get((UnitProfileId)c.profile),c.commander))).ToArray();
        }
        internal static RealmSaveData Capture(RealmOperations r)=>new RealmSaveData{hasLastBattle=r.LastBattle!=null,lastBattle=r.LastBattle?.Copy(),armies=r.Armies.Select(f=>RealmArmyData.Capture(r,f)).ToArray(),west=RealmSideData.Capture(r.West),east=RealmSideData.Capture(r.East),cycles=r.cycles.OrderBy(c=>c.Key,StringComparer.Ordinal).Select(c=>new RealmCycleData{id=c.Key,ceiling=c.Value.Ceiling,deprivation=c.Value.Deprivation,born=c.Value.BornRefresh}).ToArray(),familiarity=r.familiarity.OrderBy(c=>c.Key,StringComparer.Ordinal).Select(c=>new RealmFamiliarityData{key=c.Key,load=(int)c.Value}).ToArray(),appliedBattle=r.AppliedBattle,message=r.LastMessage};
        internal RealmOperations Restore(CrossroadsScenario w)
        {
            StrategicSaveData.Require(armies!=null&&armies.Length>=4&&armies.Length<=10000&&armies.All(f=>f!=null)&&armies.Select(f=>f.id).Distinct().Count()==armies.Length&&west!=null&&east!=null&&cycles!=null&&cycles.All(c=>c!=null)&&familiarity!=null&&message!=null&&appliedBattle>=0,"Invalid 06 collections.");
            var r=new RealmOperations(w, w.Foundations.West.Preset,w.Foundations.East.Preset,false){West=west.Restore(w.Refresh),East=east.Restore(w.Refresh),AppliedBattle=appliedBattle,LastMessage=message};
            foreach(var a in armies){var f=a.Restore();r.armies.Add(f);r.staffDue[a.id]=a.staff;}
            var chars=r.Characters(Side.West).Concat(r.Characters(Side.East)).ToArray();
            StrategicSaveData.Require(chars.Select(c=>c.CharacterId).Distinct().Count()==chars.Length,"One UnitId appears in multiple physical locations.");
            StrategicSaveData.Require(cycles.Length==chars.Length&&cycles.Select(c=>c.id).Distinct().Count()==cycles.Length&&cycles.All(c=>chars.Any(u=>u.CharacterId==c.id)&&c.ceiling>=-40&&c.ceiling<=100&&c.deprivation>=0&&c.deprivation<=w.Refresh&&c.born>=0&&c.born<=w.Refresh),"Invalid anti-relay / nutrition ledger.");
            foreach(var c in cycles)r.cycles.Add(c.id,new RealmUnitCycle{Ceiling=c.ceiling,Deprivation=c.deprivation,BornRefresh=c.born});
            foreach(var f in familiarity){StrategicSaveData.Require(f!=null&&f.key!=null&&f.load>=6&&f.load<=9&&!r.familiarity.ContainsKey(f.key),"Invalid familiarity.");r.familiarity.Add(f.key,(CommandFamiliarity)f.load);}
            StrategicSaveData.Require(r.Armies.Where(f=>f.Continues&&f.Node!=w.OwnKeep(f.Formation.Side)).GroupBy(f=>f.Node).All(g=>g.Count()==1),"Overlapping field formations.");
            foreach(var side in new[]{Side.West,Side.East}) {
                var s=r.SideState(side);StrategicSaveData.Require(r.Army(s.Selected)?.Formation.Side==side,"Invalid selected army.");
                StrategicSaveData.Require(r.Armies.Where(a=>a.Formation.Side==side).All(a=>int.TryParse(a.Formation.FormationId.Substring(("realm06-"+side+"-army-").Length),out int n)&&n>0&&n<s.NextArmy),"Reused army identity counter.");
                foreach(var f in r.Armies.Where(f=>f.Formation.Side==side)) {
                    StrategicSaveData.Require(f.Formation.Members.All(c=>c.CharacterId.StartsWith("realm06-"+side+"-",StringComparison.Ordinal)),"Foreign unit ownership.");
                    StrategicSaveData.Require(f.Formation.Commanderless||r.UsedCapacity(f)<=r.Capacity(f),"Overbooked receiving Commander.");
                    StrategicSaveData.Require(f.Formation.LivingMembers.All(c=>r.cycles[c.CharacterId].Ceiling<=f.Tempo),"Refreshed unit ceiling in spent army.");
                }
                var own=r.Characters(side).Select(c=>c.CharacterId).ToHashSet();
                StrategicSaveData.Require(s.CommissionQueue.All(id=>s.Reserve.Any(c=>c.CharacterId==id))&&(s.Commission==null||s.Commission.Kind=="Commission"&&s.Reserve.Any(c=>c.CharacterId==s.Commission.Unit)),"Invalid Commission recipient.");
                var personal=s.Services.SelectMany(o=>o.Kind=="Repair"?o.Quotes.Keys.AsEnumerable():new[]{o.Unit}).Concat(s.CommissionQueue).Concat(s.Commission==null?Array.Empty<string>():new[]{s.Commission.Unit}).ToArray();
                StrategicSaveData.Require(personal.All(own.Contains)&&personal.Distinct().Count()==personal.Length,"Duplicate/unknown personal services.");
                if(s.Recruit!=null)StrategicSaveData.Require(s.Recruit.Kind=="Recruit"&&!chars.Any(c=>c.CharacterId==s.Recruit.Unit)&&(s.Recruit.Destination=="Reserve"||r.Army(s.Recruit.Destination)?.Formation.Side==side)&&new[]{UnitProfileId.HumanWarriorTI,UnitProfileId.HumanArcherTI,UnitProfileId.FireMageTI,UnitProfileId.IceMageTI}.Contains(s.Recruit.Profile),"Invalid paid recruit destination/profile.");
            }
            if(hasLastBattle){StrategicSaveData.Require(lastBattle!=null,"Missing aftermath");lastBattle.Validate(r,appliedBattle);r.LastBattle=lastBattle.Copy();}
            return r;
        }
        internal void Write(BinaryWriter w)
        {w.Write(armies.Length);foreach(var a in armies)a.Write(w);west.Write(w);east.Write(w);w.Write(cycles.Length);foreach(var c in cycles){w.Write(c.id);w.Write(c.ceiling);w.Write(c.deprivation);w.Write(c.born);}w.Write(familiarity.Length);foreach(var f in familiarity){w.Write(f.key);w.Write(f.load);}w.Write(appliedBattle);w.Write(message);if(hasLastBattle)lastBattle.Write(w);}
    }
    public sealed partial class RealmOperations
    {
        internal RealmOperations(CrossroadsScenario world,CombatPreset west,CombatPreset east,bool initialize){World=world;}
    }
}

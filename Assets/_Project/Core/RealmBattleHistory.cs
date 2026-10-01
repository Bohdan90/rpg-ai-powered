using System;
using System.IO;
using System.Linq;
namespace RPG.Core
{
    // Stable-world audit of the last resolved encounter. This is not a second combat state
    // or campaign replay: subsequent transfers may change membership, so retain the snapshot.
    [Serializable] public sealed class RealmBattleHistory
    {
        public int world;public int number;public uint seed;public string lead,target,advanced,attackingPool,defendingPool,finalTacticalHash;
        public string[] armies,unitIds,unitArmies;
        public int[] origins,destinations,tempos,dead,escaped,active;
        public bool[] attackers,withdrew;
        internal RealmBattleHistory Copy()=>new RealmBattleHistory{world=world,number=number,seed=seed,lead=lead,target=target,advanced=advanced,attackingPool=attackingPool,defendingPool=defendingPool,finalTacticalHash=finalTacticalHash,armies=armies.ToArray(),unitIds=unitIds.ToArray(),unitArmies=unitArmies.ToArray(),origins=origins.ToArray(),destinations=destinations.ToArray(),tempos=tempos.ToArray(),dead=dead.ToArray(),escaped=escaped.ToArray(),active=active.ToArray(),attackers=attackers.ToArray(),withdrew=withdrew.ToArray()};
        internal void Validate(RealmOperations realm,int applied)
        {
            StrategicSaveData.Require(number==applied&&number>0&&armies!=null&&armies.Length>=2&&armies.Distinct().Count()==armies.Length&&armies.All(id=>realm.Army(id)!=null)&&armies.Contains(lead)&&armies.Contains(target)&&advanced!=null&&(advanced==""||advanced==lead)&&finalTacticalHash!=null,"Invalid last battle identity.");
            foreach(var array in new[]{origins,destinations,tempos,dead,escaped,active})StrategicSaveData.Require(array!=null&&array.Length==armies.Length,"Invalid aftermath lengths.");
            StrategicSaveData.Require(attackers?.Length==armies.Length&&withdrew?.Length==armies.Length&&attackers.Any(x=>x)&&attackers.Any(x=>!x)&&unitIds!=null&&unitArmies!=null&&unitIds.Length==unitArmies.Length&&unitIds.Distinct().Count()==unitIds.Length&&unitArmies.All(armies.Contains)&&unitIds.All(id=>realm.Character(id)!=null),"Invalid participation snapshot.");
            for(int i=0;i<armies.Length;i++)StrategicSaveData.Require(realm.World.GraphFor((WorldId)world).Node(origins[i])!=null&&realm.World.GraphFor((WorldId)world).Node(destinations[i])!=null&&tempos[i]>=-40&&tempos[i]<=100&&dead[i]>=0&&escaped[i]>=0&&active[i]>=0&&dead[i]+escaped[i]+active[i]==unitArmies.Count(id=>id==armies[i])&&withdrew[i]==(escaped[i]>0&&active[i]==0),"Invalid per-army aftermath.");
            CityFoundationData.Decimal(attackingPool);CityFoundationData.Decimal(defendingPool);
        }
        internal void Write(BinaryWriter w)
        {
            w.Write("last-battle-06");w.Write(number);w.Write(seed);w.Write(lead);w.Write(target);w.Write(advanced);w.Write(attackingPool);w.Write(defendingPool);w.Write(finalTacticalHash);w.Write(armies.Length);
            for(int i=0;i<armies.Length;i++){w.Write(armies[i]);w.Write(origins[i]);w.Write(destinations[i]);w.Write(tempos[i]);w.Write(dead[i]);w.Write(escaped[i]);w.Write(active[i]);w.Write(attackers[i]);w.Write(withdrew[i]);}
            w.Write(unitIds.Length);for(int i=0;i<unitIds.Length;i++){w.Write(unitIds[i]);w.Write(unitArmies[i]);}
        }
    }
    public sealed class RealmFormPreview
    {
        public string Reason {get;internal set;}
        public int Capacity {get;internal set;}
        public int Load {get;internal set;}
        public int Tempo {get;internal set;}
        public int Consumption {get;internal set;}
        public int MaxProvisions=>6*Consumption;
    }
    public sealed partial class RealmOperations
    {
        public RealmBattleHistory LastBattle {get;internal set;}
        public int PendingConsumption(DuelForce f)=>f.Formation.LivingMembers.Count()+(staffDue.TryGetValue(f.Formation.FormationId,out var due)?due:0);
        public RealmFormPreview PreviewForm(Side side,string commanderId,System.Collections.Generic.IEnumerable<string> selected)
        {
            var ids=selected?.ToArray();var p=new RealmFormPreview{Reason=FormBlocker(side,commanderId,ids)};if(p.Reason!=null)return p;
            var chars=SideState(side).Reserve.Where(c=>ids.Contains(c.CharacterId)).ToArray();var commander=chars.Single(c=>c.CharacterId==commanderId);
            p.Capacity=32+6*(int)commander.CommandRank;p.Load=Load(commander,chars);p.Tempo=Math.Min(0,chars.Min(c=>cycles[c.CharacterId].Ceiling));p.Consumption=chars.Length+2;return p;
        }
    }
}

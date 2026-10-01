using System;
using System.Collections.Generic;
using System.Linq;
namespace RPG.Core
{
    public sealed partial class RealmOperations
    {
        private bool Present(Side side,string id)=>SideState(side).Reserve.Any(c=>c.CharacterId==id)||armies.Any(f=>f.Formation.Side==side&&World.AtOwnCity(f)&&f.Formation.Members.Any(c=>c.CharacterId==id));
        private bool TrainingLocal(Side side,string id)=>Present(side,id)&&!armies.Any(f=>f.Formation.Members.Any(c=>c.CharacterId==id)&&f.Formation.Commanderless);
        public string RecruitBlocker(Side side,UnitProfileId profile,string destination,bool completing=false)
        {
            if(!completing&&!CanAct(side))return "No active side authority.";
            bool mage=profile==UnitProfileId.FireMageTI||profile==UnitProfileId.IceMageTI;
            if(profile!=UnitProfileId.HumanWarriorTI&&profile!=UnitProfileId.HumanArcherTI&&!mage)return "Only L1 HW/HA/HOM recruitment in 06.";
            if(!World.Foundations.City(side).Functioning||mage&&World.Foundations.City(side).MageTower<1)return "Functioning local recruitment wing required.";
            var s=SideState(side);if(!completing&&(s.Recruit!=null||s.LastRecruit==World.Refresh))return "Shared side recruit queue: at most one completion/order per Refresh.";
            if(destination!="Reserve") {
                var f=Army(destination);if(f==null||f.Formation.Side!=side||!World.AtOwnCity(f)||!f.Continues)return "Original receiving army must be present at own City.";
                if(f.Formation.Commanderless||f.Formation.Commander==null)return "Commanderless recipient is roster-locked.";
                var prospective=new PersistentCharacter("preview",UnitProfile.Get(profile));
                if(UsedCapacity(f)+(int)Familiarity(f.Formation.Commander,prospective)>Capacity(f))return "Receiving Command Capacity insufficient.";
            }
            return !completing&&World.Force(side).Gold<(mage?150:100)?"Insufficient own Gold.":null;
        }
        public bool Recruit(Side side,UnitProfileId profile,string destination)
        {
            var blocker=RecruitBlocker(side,profile,destination);if(blocker!=null){return false;}
            var s=SideState(side);World.Force(side).Gold-=UnitProfile.Get(profile).IsCaster?150:100;
            s.Recruit=new RealmService{Kind="Recruit",Destination=destination,Profile=profile,Started=World.Refresh,Unit="realm06-"+side+"-recruit-"+s.NextRecruit++};s.LastRecruit=World.Refresh;
            Say("Paid recruit queued for "+destination+"; existing side queue, same-cycle Refresh completion, no extra activation.");return true;
        }
        public string TrainingBlocker(Side side,string id)
        {
            var c=Character(id);var city=World.Foundations.City(side);var state=SideState(side);
            if(!CanAct(side)||!Characters(side).Contains(c)||c==null||c.Status==PersistentCharacterStatus.Dead||!TrainingLocal(side,id))return "Living local own character in Reserve or commanded army required.";
            if(c.Profile.Tier!=1||!c.Profile.IsFireMage&&!c.Profile.IsIceMage||c.PersonalLevel<3)return "HOM TI at Personal Level 3 required.";
            if(!city.Functioning||city.MageTower!=2||!World.Foundations.Realm(side).Knows(CityTech.ElementalDrills))return "Functioning Tower II and Elemental Drills required.";
            if(state.Services.Any(s=>s.Kind=="Training")||TransferServiceBlocker(side,new[]{id})!=null)return "Training/service already pending.";
            return World.Force(side).Gold<75?"75 Gold required.":null;
        }
        public bool Train(Side side,string id,bool confirmPermanentSpell)
        {if(!confirmPermanentSpell||TrainingBlocker(side,id)!=null)return false;World.Force(side).Gold-=75;SideState(side).Services.Add(new RealmService{Kind="Training",Unit=id,Started=World.Refresh});return true;}
        public RealmService QuoteRepair(Side side,IEnumerable<string> requested)
        {
            var ids=requested?.ToArray();var city=World.Foundations.City(side);
            if(!CanAct(side)||!city.Functioning||!city.Forge||ids==null||ids.Length==0||ids.Distinct().Count()!=ids.Length||TransferServiceBlocker(side,ids)!=null||ids.Any(id=>!Present(side,id)))return null;
            var q=new RealmService{Kind="Repair",Started=World.Refresh};
            foreach(var id in ids){var c=Character(id);if(c.Status==PersistentCharacterStatus.Dead)continue;int n=Math.Min(c.Profile.MaxArmor-c.Armor,(c.Profile.MaxArmor+1)/2);if(n>0)q.Quotes.Add(id,n);}
            return q.Quotes.Count==0?null:q;
        }
        public bool Repair(Side side,IEnumerable<string> ids)
        {var q=QuoteRepair(side,ids);if(q==null||World.Force(side).Gold<q.Quotes.Values.Sum()*2)return false;World.Force(side).Gold-=q.Quotes.Values.Sum()*2;SideState(side).Services.Add(q);return true;}
        public bool CancelService(Side side,string unitId)
        {if(!CanAct(side))return false;return SideState(side).Services.RemoveAll(s=>s.Unit==unitId||s.Quotes.ContainsKey(unitId))>0;}
        internal void CancelAbsentRepairs(Side side)
        {var s=SideState(side);s.Services.RemoveAll(o=>o.Kind=="Repair"&&(!World.Foundations.City(side).Functioning||!World.Foundations.City(side).Forge||o.Quotes.Keys.Any(id=>!Present(side,id))));foreach(var t in s.Services.Where(o=>o.Kind=="Training"))if(!TrainingLocal(side,t.Unit))t.Started=World.Refresh;}
        internal void FinishRefresh()
        {
            foreach(var side in new[]{Side.West,Side.East}) {
                var s=SideState(side);var city=World.Foundations.City(side);var treasury=World.Force(side);
                CancelAbsentRepairs(side);
                foreach(var order in s.Services.ToArray()) {
                    if(order.Kind=="Training") {
                        var c=Character(order.Unit);if(c==null||c.Status==PersistentCharacterStatus.Dead){s.Services.Remove(order);continue;}
                        if(!TrainingLocal(side,c.CharacterId)||!city.Functioning||city.MageTower<2){order.Started=World.Refresh;continue;}
                        if(order.Started<World.Refresh){c.TrainMageTierII();s.Services.Remove(order);}
                    }else if(order.Kind=="Repair"&&order.Started<World.Refresh){foreach(var q in order.Quotes)Character(q.Key)?.RepairArmor(q.Value);s.Services.Remove(order);}
                }
                if(s.Commission!=null) {
                    var c=Character(s.Commission.Unit);
                    if(c==null||c.Status==PersistentCharacterStatus.Dead)s.Commission=null;
                    else if(CommissionBlocker(side,c.CharacterId)!=null)s.Commission.Started=World.Refresh;
                    else if(s.Commission.Started<World.Refresh){c.Commission();Say("Commission completed for same character "+c.CharacterId,side);s.Commission=null;}
                }
                ActivateCommission(side);
                // Current living IDs owe one meal, wherever physically located. Retired staff obligations
                // follow their receiving container. New recruits are created after this transaction.
                int reserveNeed=s.Reserve.Count(c=>c.Status!=PersistentCharacterStatus.Dead)+s.CityStaffDue;
                decimal reservePaid=Math.Min(treasury.KeepFood,reserveNeed);treasury.KeepFood-=reservePaid;s.CityStaffDue=0;
                bool reserveSupplied=reserveNeed==0||reservePaid>0; // 52 adapter suppresses recovery only for a fully unsupplied Reserve cycle.
                foreach(var c in s.Reserve.Where(c=>c.Status!=PersistentCharacterStatus.Dead)) {
                    if(reserveSupplied&&city.Functioning)c.ApplyHpRefresh(40);
                    cycles[c.CharacterId].Deprivation=reserveSupplied?0:cycles[c.CharacterId].Deprivation+1;
                }
                var active=armies.Where(f=>f.Formation.Side==side&&f.Continues).OrderBy(f=>f.Formation.FormationId,StringComparer.Ordinal).ToArray();
                foreach(var f in active) {
                    int due=f.Formation.LivingMembers.Count()+(staffDue.TryGetValue(f.Formation.FormationId,out var n)?n:2);
                    bool supplied=f.RealmProvisions>=due;f.RealmProvisions=Math.Max(0,f.RealmProvisions-due);
                    foreach(var c in f.Formation.LivingMembers)cycles[c.CharacterId].Deprivation=supplied?0:cycles[c.CharacterId].Deprivation+1;
                    int percent=World.AtOwnCity(f)&&city.Functioning?40:15;
                    foreach(var c in f.Formation.LivingMembers)c.ApplyHpRefresh(percent);
                    Say(f.Formation.FormationId+" consumption "+due+"; HP recovery "+percent+"%; Armor unchanged",side);
                    f.Tempo=100+Math.Min(0,f.Tempo);staffDue[f.Formation.FormationId]=2;
                }
                var requests=active.Where(f=>World.AtOwnCity(f)&&city.Functioning).ToDictionary(f=>f.Formation.FormationId,f=>Math.Min(2*Consumption(f),CarryingCapacity(f)-f.RealmProvisions));
                decimal demand=requests.Values.Sum(),available=Math.Min(treasury.KeepFood,demand);
                // Exact decimal residual assigned to the last stable ID; total never exceeds stock.
                decimal left=available;int remaining=requests.Count;
                foreach(var f in active.Where(f=>requests.ContainsKey(f.Formation.FormationId))) {
                    decimal amount=--remaining==0?left:demand==0?0:available*requests[f.Formation.FormationId]/demand;
                    f.RealmProvisions+=amount;left-=amount;treasury.KeepFood-=amount;
                }
                foreach(var f in active.Where(f=>f.WorldId==WorldId.Frontier&&f.Node==8&&World.Owner(8)==side)) {
                    // Waystation stock is integral; carrying may include proportional City fractions.
                    int amount=(int)Math.Min(6,Math.Min(World.WaystationFood,CarryingCapacity(f)-f.RealmProvisions));
                    f.RealmProvisions+=amount;World.TakeRealmWaystation(amount);
                }
                foreach(var c in Characters(side)){var cycle=cycles[c.CharacterId];cycle.Ceiling=100+Math.Min(0,cycle.Ceiling);c.ResetSourceBudgets();}
                foreach(var f in active){f.Tempo=Math.Min(f.Tempo,f.Formation.LivingMembers.Min(c=>cycles[c.CharacterId].Ceiling));LowerCeilings(f);}
                if(s.Recruit!=null&&RecruitBlocker(side,s.Recruit.Profile,s.Recruit.Destination,true)==null) {
                    var order=s.Recruit;var c=new PersistentCharacter(order.Unit,UnitProfile.Get(order.Profile));cycles[c.CharacterId]=new RealmUnitCycle{BornRefresh=World.Refresh};
                    if(order.Destination=="Reserve")s.Reserve.Add(c);else {var f=Army(order.Destination);f.Formation.ReplaceMembers(f.Formation.Members.Concat(new[]{c}).ToArray(),f.Formation.AssignedCommanderId);cycles[c.CharacterId].Ceiling=Math.Min(100,f.Tempo);}
                    s.Recruit=null;Say("Recruit completed once: "+c.CharacterId+" -> "+order.Destination+"; new body, not a replacement of a dead ID.",side);
                }
            }
        }
        internal void CheckVictory()
        {
            bool west=HasMilitary(Side.West),east=HasMilitary(Side.East);Side? winner=null;
            if(west&&!east)winner=Side.West;else if(east&&!west)winner=Side.East;
            else if(Math.Max(World.West.Pressure,World.East.Pressure)>=24&&World.West.Pressure!=World.East.Pressure)winner=World.West.Pressure>World.East.Pressure?Side.West:Side.East;
            World.SetRealmWinner(winner);
        }
    }
}

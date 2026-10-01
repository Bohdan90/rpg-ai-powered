using System;
using System.Collections.Generic;
using System.Linq;

namespace RPG.Core
{
    public enum CommandFamiliarity { Native=6, Trained=7, Familiar=8, Unfamiliar=9 }
    public sealed class RealmUnitCycle
    {
        public int Ceiling {get;internal set;}=100;
        public int Deprivation {get;internal set;}
        public int BornRefresh {get;internal set;}
    }
    public sealed class RealmService
    {
        public string Kind {get;internal set;}
        public string Unit {get;internal set;}
        public string Destination {get;internal set;}
        public int Started {get;internal set;}
        public UnitProfileId Profile {get;internal set;}
        public SortedDictionary<string,int> Quotes {get;internal set;}=new SortedDictionary<string,int>(StringComparer.Ordinal);
    }
    public sealed class RealmSideState
    {
        public List<PersistentCharacter> Reserve {get;internal set;}=new List<PersistentCharacter>();
        public List<string> CommissionQueue {get;internal set;}=new List<string>();
        public RealmService Commission {get;internal set;}
        public RealmService Recruit {get;internal set;}
        public List<RealmService> Services {get;internal set;}=new List<RealmService>();
        public int NextArmy {get;internal set;}=3;
        public int NextRecruit {get;internal set;}=1;
        public int LastRecruit {get;internal set;}
        public int CityStaffDue {get;internal set;}
        public string Selected {get;internal set;}
    }
    public sealed class RealmTransferPreview
    {
        public string Reason {get;internal set;}
        public int Cost {get;internal set;}
        public int ResultTempo {get;internal set;}
        public int UsedCapacity {get;internal set;}
        public decimal SenderProvisions {get;internal set;}
        public decimal RecipientProvisions {get;internal set;}
        public decimal ReturnedFood {get;internal set;}
        public bool Legal=>Reason==null;
    }
    // Document 52 adapter. Treasury/economy/turn cursor remain on the same Crossroads world;
    // units exist in exactly one physical army or its owner's rear-City Reserve.
    public sealed partial class RealmOperations
    {
        internal readonly CrossroadsScenario World;
        internal readonly List<DuelForce> armies=new List<DuelForce>();
        public IReadOnlyList<DuelForce> Armies=>armies.AsReadOnly();
        internal readonly Dictionary<string,RealmUnitCycle> cycles=new Dictionary<string,RealmUnitCycle>(StringComparer.Ordinal);
        internal readonly Dictionary<string,int> staffDue=new Dictionary<string,int>(StringComparer.Ordinal);
        internal readonly Dictionary<string,CommandFamiliarity> familiarity=new Dictionary<string,CommandFamiliarity>(StringComparer.Ordinal);
        public IReadOnlyDictionary<string,RealmUnitCycle> Cycles=>cycles;
        public RealmSideState West {get;internal set;}=new RealmSideState();
        public RealmSideState East {get;internal set;}=new RealmSideState();
        public RealmSideState SideState(Side s)=>s==Side.West?West:East;
        public string LastMessage {get;internal set;}="06 authored start: existing veterans, commissioned officers and 6 accepted Elemental Drills Work at each City.";
        internal int AppliedBattle;
        public RealmOperations(CrossroadsScenario world,CombatPreset west,CombatPreset east)
        {
            World=world;if(west==CombatPreset.Support||east==CombatPreset.Support)throw new ArgumentException("06 supports Fire or Ice realm variants.");
            world.Foundations.RealmMode=true;
            foreach(var side in new[]{Side.West,Side.East}) {
                var treasury=world.Force(side);treasury.KeepFood=120;
                var city=world.Foundations.City(side);city.Development=2;city.MageTower=2;
                var research=world.Foundations.Realm(side).Research.Single(p=>p.Tech==CityTech.ElementalDrills);
                research.Paid=true;research.Work=6;research.Provenance[city.Node]=6;
                for(int slot=1;slot<=2;slot++) {
                    string id="realm06-"+side+"-army-"+slot;var preset=side==Side.West?west:east;
                    var profiles=slot==1?new[]{UnitProfile.HumanWarriorTI,UnitProfile.HumanWarriorTI,UnitProfile.HumanArcherTI}:new[]{preset==CombatPreset.Fire?UnitProfile.FireMageTII:UnitProfile.IceMageTII,UnitProfile.ElfWarriorTII,UnitProfile.HumanArcherTI};
                    var chars=profiles.Select((p,i)=>new PersistentCharacter(id+"-unit-"+(i+1),p,i==0,personalXp:p.Tier==2?20:0)).ToArray();
                    var f=slot==1?treasury:new DuelForce(side);
                    f.Formation=new PersistentFormation(id,side,chars,chars[0].CharacterId,true);f.Node=slot==1?(side==Side.West?2:12):world.OwnKeep(side);f.Provisions=0;f.RealmProvisions=30;
                    armies.Add(f);staffDue[id]=2;foreach(var c in chars)cycles[c.CharacterId]=new RealmUnitCycle();
                }
                SideState(side).Selected=armies.First(f=>f.Formation.Side==side).Formation.FormationId;
            }
        }
        public DuelForce Army(string id)=>armies.SingleOrDefault(f=>f.Formation.FormationId==id);
        public DuelForce Selected(Side side)=>Army(SideState(side).Selected);
        internal void EnsureSelection(Side side) {if(Selected(side)?.Continues!=true){var next=armies.FirstOrDefault(f=>f.Formation.Side==side&&f.Continues);if(next!=null)SideState(side).Selected=next.Formation.FormationId;}}
        public IEnumerable<PersistentCharacter> Characters(Side side)=>armies.Where(f=>f.Formation.Side==side).SelectMany(f=>f.Formation.Members).Concat(SideState(side).Reserve);
        public PersistentCharacter Character(string id)=>Characters(Side.West).Concat(Characters(Side.East)).SingleOrDefault(c=>c.CharacterId==id);
        public bool HasMilitary(Side side)=>armies.Any(f=>f.Formation.Side==side&&f.Continues)||SideState(side).Reserve.Any(c=>c.Status!=PersistentCharacterStatus.Dead&&(c.IsCommander||c.Profile.Tier>=2));
        public bool CanAct(Side side)=>CrossroadsScenario.ValidSide(side)&&side==World.ActiveSide&&!World.HandoffPending&&World.CanSave&&!World.Winner.HasValue&&!World.IsDraw&&HasMilitary(side);
        public bool Select(Side side,string id) {var f=Army(id);if(!CanAct(side)||f==null||f.Formation.Side!=side||!f.Continues)return false;SideState(side).Selected=id;return true;}
        public static int Consumption(DuelForce f)=>f.Formation.LivingMembers.Count()+2;
        public static int CarryingCapacity(DuelForce f)=>6*Consumption(f);
        public int Capacity(DuelForce f)=>f.Formation.Commanderless||f.Formation.Commander==null?0:32+6*(int)f.Formation.Commander.CommandRank;
        public CommandFamiliarity Familiarity(PersistentCharacter commander,PersistentCharacter unit)
        {
            if(commander.Profile.IsElf==unit.Profile.IsElf)return CommandFamiliarity.Native;
            return familiarity.TryGetValue(commander.CharacterId+(unit.Profile.IsElf?":Elf":":Human"),out var level)?level:CommandFamiliarity.Unfamiliar;
        }
        public int Load(PersistentCharacter commander,IEnumerable<PersistentCharacter> units)=>commander==null?0:units.Where(c=>c.Status!=PersistentCharacterStatus.Dead&&c!=commander).Sum(c=>(int)Familiarity(commander,c));
        public int UsedCapacity(DuelForce f)=>Load(f.Formation.Commander,f.Formation.Members);
        internal void LowerCeilings(DuelForce f) {foreach(var c in f.Formation.LivingMembers)cycles[c.CharacterId].Ceiling=Math.Min(cycles[c.CharacterId].Ceiling,f.Tempo);}
        public StrategicMovePreview PreviewMove(Side side,int destination)
        {
            if(World.Seamless!=null)return World.Seamless.PreviewMove(side,new WorldAddress(Selected(side)?.WorldId??WorldId.Frontier,destination));
            var p=new StrategicMovePreview();var f=Selected(side);
            if(!CanAct(side)||f==null||!f.Continues){p.Reason="Select a continuing own army.";return p;}
            if(World.Graph.Node(destination)==null||destination==f.Node){p.Reason="Choose another graph node.";return p;}
            p.Path=World.Graph.Path(f.Node,destination,n=>n!=World.OwnKeep(CrossroadsScenario.Other(side))&&(n==World.OwnKeep(side)||!armies.Any(o=>o!=f&&o.Continues&&o.Node==n)),f.RealmProvisions==0);
            if(p.Path.Length<2){p.Reason="Occupied or unreachable destination.";return p;}p.Cost=World.Graph.PathCost(p.Path,f.RealmProvisions==0);
            if(p.Cost>f.Tempo)p.Reason="Insufficient Tempo: "+p.Cost+" required.";return p;
        }
        public bool Move(Side side,int destination)
        {if(World.Seamless!=null)return World.Seamless.Move(side,new WorldAddress(Selected(side)?.WorldId??WorldId.Frontier,destination));var p=PreviewMove(side,destination);if(!p.IsLegal)return false;var f=Selected(side);f.Tempo-=p.Cost;LowerCeilings(f);f.Node=destination;CancelAbsentRepairs(side);Say(f.Formation.FormationId+" moves to "+destination+"; Tempo "+f.Tempo);return true;}
        private void Say(string message,Side? owner=null){LastMessage=message;World.RealmLog(message);World.Seamless?.SetSideMessage(owner??World.ActiveSide,message);World.Seamless?.Observe();}
        public bool EndSide(Side side)
        {if(!CanAct(side))return false;World.EndRealmSide(side);return true;}
        public string TransferServiceBlocker(Side side,IEnumerable<string> ids)
        {
            var set=ids.ToHashSet();var state=SideState(side);
            return state.Commission!=null&&set.Contains(state.Commission.Unit)||state.CommissionQueue.Any(set.Contains)||state.Services.Any(o=>set.Contains(o.Unit??"")||o.Quotes.Keys.Any(set.Contains))?"Cancel the conflicting personal service/Commission explicitly before transfer (paid fees are not refunded).":null;
        }
        private IEnumerable<PersistentCharacter> Container(Side side,string id)=>id=="Reserve"?SideState(side).Reserve:Army(id)?.Formation.Members??Enumerable.Empty<PersistentCharacter>();
        private bool LocalContainer(Side side,string id)=>id=="Reserve"||Army(id)?.Formation.Side==side&&World.AtOwnCity(Army(id));
        public RealmTransferPreview PreviewTransfer(Side side,string from,string to,IEnumerable<string> requested)
        {
            var p=new RealmTransferPreview();var ids=requested?.ToArray();var sender=Army(from);var receiver=Army(to);
            if(!CanAct(side)||from==to||ids==null||ids.Length==0||ids.Distinct().Count()!=ids.Length){p.Reason="Choose distinct own source/recipient and real unit IDs.";return p;}
            bool city=LocalContainer(side,from)&&LocalContainer(side,to);
            if(from!="Reserve"&&(sender==null||sender.Formation.Side!=side)||to!="Reserve"&&(receiver==null||receiver.Formation.Side!=side)){p.Reason="Unknown own container.";return p;}
            if(!city&&(sender==null||receiver==null||World.ContactCost(sender,receiver)<0)){p.Reason="Physical contact or the same own City required.";return p;}
            var members=Container(side,from);var moved=members.Where(c=>ids.Contains(c.CharacterId)).ToArray();
            if(moved.Length!=ids.Length||moved.Any(c=>c.Status==PersistentCharacterStatus.Dead)){p.Reason="Only present living characters may transfer.";return p;}
            if(sender?.Formation.Commander!=null&&moved.Contains(sender.Formation.Commander)){p.Reason="Assigned Commander: use City disband/reassign first.";return p;}
            if(receiver!=null&&(receiver.Formation.Commanderless||receiver.Formation.Commander==null)){p.Reason="Commanderless recipient: assign an existing officer at City first.";return p;}
            p.Reason=TransferServiceBlocker(side,ids);if(p.Reason!=null)return p;
            if(receiver!=null){p.UsedCapacity=Load(receiver.Formation.Commander,receiver.Formation.Members.Concat(moved));if(p.UsedCapacity>Capacity(receiver)){p.Reason="Receiving Command Capacity exceeded; select a smaller subset.";return p;}}
            p.Cost=city?0:World.ContactCost(sender,receiver);
            if(!city&&(sender.Tempo<p.Cost||receiver.Tempo<p.Cost)){p.Reason="Both armies need the full nonnegative field handover edge cost.";return p;}
            p.ResultTempo=Math.Min(sender?.Tempo??100,receiver?.Tempo??100);foreach(var c in moved)p.ResultTempo=Math.Min(p.ResultTempo,cycles[c.CharacterId].Ceiling);p.ResultTempo-=p.Cost;
            decimal pool=(sender?.RealmProvisions??0)+(receiver?.RealmProvisions??0);
            int senderCount=sender==null?0:sender.Formation.LivingMembers.Count()-moved.Length;
            int receiverCount=receiver==null?0:receiver.Formation.LivingMembers.Count()+moved.Length;
            decimal recipientMax=receiverCount==0?0:6*(receiverCount+2),senderMax=senderCount==0?0:6*(senderCount+2);
            // Preserve existing recipient supply first, then transfer excess from the source.
            p.RecipientProvisions=Math.Min(recipientMax,receiver?.RealmProvisions??0);pool-=p.RecipientProvisions;
            p.SenderProvisions=Math.Min(senderMax,pool);pool-=p.SenderProvisions;
            decimal extra=Math.Min(recipientMax-p.RecipientProvisions,pool);p.RecipientProvisions+=extra;pool-=extra;
            p.ReturnedFood=pool;
            if(pool>0&&(!city||World.Force(side).KeepFood+pool>180))p.Reason="Capacity change cannot preserve carried Food; reduce transfer or make room in City stock.";
            return p;
        }
        public bool Transfer(Side side,string from,string to,IEnumerable<string> requested)
        {
            var ids=requested?.ToArray();var p=PreviewTransfer(side,from,to,ids);if(!p.Legal){return false;}
            var moved=Container(side,from).Where(c=>ids.Contains(c.CharacterId)).ToArray();var a=Army(from);var b=Army(to);
            if(a==null)SideState(side).Reserve.RemoveAll(c=>ids.Contains(c.CharacterId));else {a.Formation.ReplaceMembers(a.Formation.Members.Except(moved).ToArray(),a.Formation.AssignedCommanderId);a.Tempo=p.ResultTempo;a.RealmProvisions=p.SenderProvisions;LowerCeilings(a);}
            if(b==null)SideState(side).Reserve.AddRange(moved);else {b.Formation.ReplaceMembers(b.Formation.Members.Concat(moved).ToArray(),b.Formation.AssignedCommanderId);b.Tempo=p.ResultTempo;b.RealmProvisions=p.RecipientProvisions;LowerCeilings(b);}
            foreach(var c in moved)cycles[c.CharacterId].Ceiling=Math.Min(cycles[c.CharacterId].Ceiling,p.ResultTempo);
            World.Force(side).KeepFood+=p.ReturnedFood;
            RetireEmpty(a,side,b);Say("Physical transfer "+string.Join(", ",ids)+"; both signed Tempo ceilings "+p.ResultTempo+"; returned Food "+p.ReturnedFood);return true;
        }
        private void RetireEmpty(DuelForce f,Side side,DuelForce recipient)
        {
            if(f==null||f.Continues)return;string id=f.Formation.FormationId;int due=staffDue.TryGetValue(id,out var n)?n:0;staffDue[id]=0;
            if(recipient!=null)staffDue[recipient.Formation.FormationId]+=due;else SideState(side).CityStaffDue+=due;
            if(SideState(side).Selected==id)SideState(side).Selected=armies.FirstOrDefault(a=>a.Formation.Side==side&&a.Continues)?.Formation.FormationId??id;CheckVictory();
        }
        public string CommissionBlocker(Side side,string id)
        {
            var c=SideState(side).Reserve.SingleOrDefault(u=>u.CharacterId==id);var city=World.Foundations.City(side);
            if(c==null||c.Status==PersistentCharacterStatus.Dead||c.IsCommander||c.Profile.Tier<2)return "Existing living Tier II+ uncommissioned character in City Reserve required.";
            if(!city.Functioning||city.MageTower<2||!World.Foundations.Realm(side).Knows(CityTech.ElementalDrills))return "Functioning Tier-II training-capable Recruitment Complex required.";
            return SideState(side).Services.Any(s=>s.Unit==id||s.Quotes.ContainsKey(id))?"Conflicting personal service must be cancelled.":null;
        }
        public bool QueueCommission(Side side,string id)
        {var s=SideState(side);if(!CanAct(side)||CommissionBlocker(side,id)!=null||s.Commission?.Unit==id||s.CommissionQueue.Contains(id))return false;s.CommissionQueue.Add(id);ActivateCommission(side);return true;}
        private void ActivateCommission(Side side)
        {
            var s=SideState(side);if(s.Commission!=null||s.CommissionQueue.Count==0)return;var id=s.CommissionQueue[0];if(CommissionBlocker(side,id)!=null||World.Force(side).Gold<200)return;
            World.Force(side).Gold-=200;s.Commission=new RealmService{Kind="Commission",Unit=id,Started=World.Refresh};s.CommissionQueue.RemoveAt(0);Say("Commission paid 200 Gold for "+id+"; full subsequent Refresh required.",side);
        }
        public bool CancelCommission(Side side,string id)
        {if(!CanAct(side))return false;var s=SideState(side);if(s.Commission?.Unit==id){s.Commission=null;ActivateCommission(side);return true;}bool removed=s.CommissionQueue.Remove(id);if(removed)ActivateCommission(side);return removed;}
        public string FormBlocker(Side side,string commander,IEnumerable<string> ids)
        {
            if(!CanAct(side))return "No active side authority.";var reserve=SideState(side).Reserve;var c=reserve.SingleOrDefault(u=>u.CharacterId==commander);var chosen=ids?.ToArray();
            if(c==null||!c.IsCommander||c.Status==PersistentCharacterStatus.Dead)return "Physically present unassigned commissioned officer required.";
            if(chosen==null||chosen.Distinct().Count()!=chosen.Length||!chosen.Contains(commander)||chosen.Any(id=>!reserve.Any(u=>u.CharacterId==id&&u.Status!=PersistentCharacterStatus.Dead)))return "Choose existing living Reserve IDs, including the Commander.";
            if(Load(c,reserve.Where(u=>chosen.Contains(u.CharacterId)))>32+6*(int)c.CommandRank)return "Receiving Command Capacity exceeded.";
            return TransferServiceBlocker(side,chosen);
        }
        public bool FormArmy(Side side,string commander,IEnumerable<string> ids)
        {
            var selected=ids?.ToArray();var blocker=FormBlocker(side,commander,selected);if(blocker!=null){return false;}
            var s=SideState(side);var members=s.Reserve.Where(c=>selected.Contains(c.CharacterId)).ToArray();string armyId="realm06-"+side+"-army-"+s.NextArmy++;
            var army=new DuelForce(side){Formation=new PersistentFormation(armyId,side,members,commander,true),Node=World.OwnKeep(side),Tempo=Math.Min(0,members.Min(c=>cycles[c.CharacterId].Ceiling)),Provisions=0,RealmProvisions=0};
            s.Reserve.RemoveAll(c=>selected.Contains(c.CharacterId));armies.Add(army);staffDue[armyId]=2;LowerCeilings(army);s.Selected=armyId;Say("Formed "+armyId+" from existing IDs; no free Tempo or Provisions.");return true;
        }
        public bool Disband(Side side,string id)
        {
            var f=Army(id);if(!CanAct(side)||f==null||f.Formation.Side!=side||!World.AtOwnCity(f)||!f.Continues||TransferServiceBlocker(side,f.Formation.LivingMembers.Select(c=>c.CharacterId))!=null||World.Force(side).KeepFood+f.RealmProvisions>180)return false;
            LowerCeilings(f);SideState(side).Reserve.AddRange(f.Formation.LivingMembers);f.Formation.ReplaceMembers(f.Formation.Members.Where(c=>c.Status==PersistentCharacterStatus.Dead).ToArray(),null);
            World.Force(side).KeepFood+=f.RealmProvisions;f.RealmProvisions=0;RetireEmpty(f,side,null);Say("City disband: IDs/debt preserved; pending staff meal moved to City, no reset.");return true;
        }
        public bool AssignCommander(Side side,string armyId,string officerId)
        {
            var f=Army(armyId);var c=SideState(side).Reserve.SingleOrDefault(u=>u.CharacterId==officerId);
            if(!CanAct(side)||f==null||!f.Continues||f.Formation.Side!=side||!World.AtOwnCity(f)||!f.Formation.Commanderless||c==null||!c.IsCommander||c.Status==PersistentCharacterStatus.Dead||TransferServiceBlocker(side,new[]{officerId})!=null||Load(c,f.Formation.Members)>32+6*(int)c.CommandRank)return false;
            SideState(side).Reserve.Remove(c);f.Formation.ReplaceMembers(f.Formation.Members.Concat(new[]{c}).ToArray(),officerId);f.Tempo=Math.Min(f.Tempo,cycles[c.CharacterId].Ceiling);LowerCeilings(f);Say("Real officer "+officerId+" assigned; old dead Commander remains dead history.");return true;
        }
    }
    public sealed partial class CrossroadsScenario
    {
        internal void RealmLog(string message)=>Log(message);
        internal void EndRealmSide(Side side)
        {
            foreach(var f in Realm.Armies.Where(a=>a.Continues&&a.Formation.Side==side)) {int n=f.Node;if(f.WorldId!=WorldId.Frontier){Seamless?.Claim(f);continue;}if(n>=6&&n<=8)owners[n-6]=side;Foundations.Capture(n,side);}
            CompletedActivations++;
            if(CompletedActivations==2) {
                Foundations.RefreshEconomy(this);Realm.FinishRefresh();Seamless?.FinishRefresh();
                foreach(var owner in owners)if(owner.HasValue)Force(owner.Value).Pressure++;
                if(Owner(6).HasValue)Force(Owner(6).Value).Gold+=75;
                Refresh++;CompletedActivations=0;ActiveSide=StartingSide;Realm.CheckVictory();
            }else ActiveSide=Other(StartingSide);
            HandoffPending=!Winner.HasValue&&!IsDraw;Seamless?.Observe();
        }
        internal void SetRealmWinner(Side? side)=>Winner=side;
        internal void SetRealmEncounter(DuelForce lead,DuelForce target,DuelForce[] attackers,DuelForce[] defenders)
        {Encounter=new DuelEncounter(this,lead,target,attackers,defenders,++battleNumber,Seed+(uint)battleNumber);}
        internal void ClearRealmEncounter()=>Encounter=null;
        internal void SetRealmContact(IncidentContact contact)=>PendingContact=contact;
        internal void TakeRealmWaystation(int amount)=>WaystationFood-=amount;
    }
}

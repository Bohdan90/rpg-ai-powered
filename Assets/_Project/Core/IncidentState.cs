using System;
using System.Linq;
using System.Collections.Generic;

namespace RPG.Core
{
    public enum PortalPhase { Precursor, Red, Whiteout, Closure, Recovery, Stable }
    public enum RaiderState { Staged, PendingEntry, AdvanceToTarget, RaidArmed, Returning, StrandedReturn, GuardingClosedAnchor, Defeated, ExitedThroughPortal, UnreleasedForThisIncident }
    public enum HoldState { Disabled, Open, Completed, Failed, Expired }
    public sealed class IncidentRaider
    {
        public string Id => Force.Formation.FormationId;
        public DuelForce Force { get; internal set; }
        public RaiderState State { get; internal set; }
        public int ArmedRefresh { get; internal set; }
        public string Blocker { get; internal set; }="";
        public bool OnMap => State>=RaiderState.AdvanceToTarget&&State<=RaiderState.GuardingClosedAnchor&&Force.Continues;
        internal IncidentRaider(int index)
        {
            string id="incident-04-raider-"+(index==0?"A":"B");
            var profiles=new[]{UnitProfile.HumanWarriorTI,index==0?UnitProfile.HumanWarriorTI:UnitProfile.HumanArcherTI,UnitProfile.HumanArcherTI};
            Force=new DuelForce(Side.East){Node=16,Formation=new PersistentFormation(id,Side.East,profiles.Select((p,i)=>new PersistentCharacter(id+"-"+(i+1),p,i==0)))};
        }
    }
    // One authored incident, not a production world simulation or contract generator.
    public sealed class IncidentState
    {
        public const string Id="Crossroads-Incident-04";
        public static readonly StrategicGraph Map=new StrategicGraph(CrossroadsScenario.Map.Nodes.Concat(new[]{new StrategicNode(14,"Contract Lodge",4,-1.3f),new StrategicNode(15,"Portal Cordon",4,-2.6f),new StrategicNode(16,"Breach Anchor",4,-3.9f)}).ToArray(),
            CrossroadsScenario.Map.Edges.Concat(new[]{new StrategicEdge(8,14,20),new StrategicEdge(14,15,20),new StrategicEdge(15,16,20)}).ToArray());
        public bool Enabled { get; internal set; }
        public PortalPhase Phase { get; internal set; }
        public int WhiteoutRemaining { get; internal set; }
        public bool WorldPhase { get; internal set; }
        public int Cursor { get; internal set; }
        public string[] ActorOrder { get; internal set; }=Array.Empty<string>();
        public IReadOnlyList<IncidentRaider> Raiders { get; }
        public bool Ravaged { get; internal set; }
        public HoldState Contract { get; internal set; }
        public string ClaimantId { get; internal set; }="";
        public int HoldCycles { get; internal set; }
        public bool CycleEligible { get; internal set; }
        public bool ContinuityBroken { get; internal set; }
        public bool RewardPaid { get; internal set; }
        public string HoldReason { get; internal set; }="Arrive and own node 08 before a full cycle begins.";
        public int AppliedBattle { get; internal set; }
        public string SituationResolution => Raiders.Any(r=>r.OnMap)?"Active / surviving aftermath":Ravaged?"Supply disabled; no active raiders":Phase==PortalPhase.Stable?"Stable; no active threats":"Incident ongoing";
        internal IncidentState(bool enabled){Enabled=enabled;Phase=enabled?PortalPhase.Precursor:PortalPhase.Stable;Contract=enabled?HoldState.Open:HoldState.Disabled;Raiders=Array.AsReadOnly(new[]{new IncidentRaider(0),new IncidentRaider(1)});}
    }
    public sealed partial class CrossroadsScenario
    {
        internal IEnumerable<DuelForce> Occupants => new[]{West,East}.Where(f=>f.Continues).Concat(Incident==null?Enumerable.Empty<DuelForce>():Incident.Raiders.Where(r=>r.OnMap).Select(r=>r.Force));
        private void Trace(string text)=>Log("["+IncidentState.Id+" "+Incident.Phase+" cursor="+Incident.Cursor+"] "+text);
        private void BeginIncidentRefresh()
        {
            var i=Incident;if(!i.Enabled)return;
            var phase=Refresh==1?PortalPhase.Precursor:Refresh<=3?PortalPhase.Red:Refresh<=5?PortalPhase.Whiteout:Refresh==6?PortalPhase.Closure:Refresh==7?PortalPhase.Recovery:PortalPhase.Stable;
            if(phase!=i.Phase){i.Phase=phase;Trace("Portal -> "+phase);}
            i.WhiteoutRemaining=phase==PortalPhase.Whiteout?6-Refresh:0;
            if(Refresh==4)
            {
                foreach(var r in i.Raiders)
                {
                    if(r.State==RaiderState.Staged||r.State==RaiderState.PendingEntry){r.State=RaiderState.UnreleasedForThisIncident;Trace(r.Id+" unreleased (not defeated)");}
                    else if(r.OnMap){r.State=RaiderState.Returning;r.ArmedRefresh=0;Trace(r.Id+" return before closure");}
                }
            }
            if(Refresh==6)
            {
                foreach(var r in i.Raiders.Where(r=>r.OnMap)){r.State=RaiderState.StrandedReturn;r.ArmedRefresh=0;Trace(r.Id+" stranded; physical return to closed Anchor");}
                if(i.Contract==HoldState.Open){i.Contract=HoldState.Expired;i.HoldReason="R6 Closure: expired after final R5 evaluation.";Trace(i.HoldReason);}
            }
            if(phase==PortalPhase.Red)
            {
                for(int n=0;n<i.Raiders.Count;n++)if(Refresh>=n+2&&i.Raiders[n].State==RaiderState.Staged)i.Raiders[n].State=RaiderState.PendingEntry;
                var r=i.Raiders.FirstOrDefault(a=>a.State==RaiderState.PendingEntry);
                if(r!=null)
                {
                    if(Occupants.Any(f=>f.Node==16)){r.Blocker="Anchor occupied; retry next Red boundary.";Trace(r.Id+" entry pending: "+r.Blocker);}
                    else{r.State=RaiderState.AdvanceToTarget;r.Force.Node=16;r.Blocker="";Trace(r.Id+" physically entered node16");}
                }
            }
            if(Refresh>1)foreach(var r in i.Raiders.Where(r=>r.OnMap))r.Force.Tempo=100+Math.Min(0,r.Force.Tempo);
            if(i.Contract!=HoldState.Open)return;
            var holder=new[]{West,East}.FirstOrDefault(f=>f.Continues&&f.Node==8&&Owner(8)==f.Formation.Side);
            if(holder==null||i.Ravaged){BreakHold(null,"no continuing owner at cycle start");i.CycleEligible=false;return;}
            if(i.ClaimantId!=holder.Formation.FormationId){i.HoldCycles=0;i.ClaimantId=holder.Formation.FormationId;}
            i.CycleEligible=Refresh>=2;i.ContinuityBroken=false;i.HoldReason=i.CycleEligible?"Eligible full cycle in progress":"First eligible cycle is R2.";
        }
        private void BreakHold(DuelForce moving,string reason)
        {
            var i=Incident;if(i==null||i.Contract!=HoldState.Open)return;
            if(moving!=null&&moving.Formation.FormationId!=i.ClaimantId)return;
            if(i.HoldCycles>0||i.CycleEligible)Trace("Hold reset: "+reason);
            i.HoldCycles=0;i.CycleEligible=false;i.ContinuityBroken=true;i.HoldReason=reason;
        }
        private void EvaluateHold()
        {
            var i=Incident;if(i.Contract!=HoldState.Open)return;
            var f=new[]{West,East}.FirstOrDefault(f=>f.Formation.FormationId==i.ClaimantId);
            if(!i.CycleEligible||i.ContinuityBroken||f==null||!f.Continues||f.Node!=8||Owner(8)!=f.Formation.Side||i.Ravaged)
            {i.HoldCycles=0;if(!i.ContinuityBroken)i.HoldReason="Arrived mid-cycle / not present as owner for the complete Refresh.";return;}
            i.HoldCycles++;i.HoldReason="Completed full hold cycle "+i.HoldCycles+" /2";Trace(i.HoldReason);
            if(i.HoldCycles==2){i.Contract=HoldState.Completed;i.RewardPaid=true;f.Gold+=150;Trace("Protect complete: "+i.ClaimantId+" +150 Gold once");}
        }
        public bool ContinueWorldPhase()
        {
            var i=Incident;if(i==null||!i.WorldPhase||Encounter!=null||PendingContact!=null||Winner.HasValue)return false;
            if(i.Cursor<i.ActorOrder.Length)
            {
                var r=i.Raiders.Single(a=>a.Id==i.ActorOrder[i.Cursor]);i.Cursor++; // persisted next slot BEFORE any battle
                if(r.OnMap)ActivateRaider(r);return true;
            }
            EvaluateHold();CompleteRefresh();HandoffPending=!Winner.HasValue;return true;
        }
        private void ActivateRaider(IncidentRaider r)
        {
            var i=Incident;var f=r.Force;r.Blocker="";
            if(r.State==RaiderState.RaidArmed&&f.Node==8&&r.ArmedRefresh<Refresh&&!i.Ravaged)
            {
                i.Ravaged=true;r.State=RaiderState.Returning;r.ArmedRefresh=0;
                Trace(r.Id+" raid completed: supply disabled; inaccessible Food="+WaystationFood);
                if(i.Contract==HoldState.Open){i.Contract=HoldState.Failed;i.HoldReason="Target ravaged";Trace("Protect failed: target ravaged");}return;
            }
            if(i.Ravaged&&r.State==RaiderState.AdvanceToTarget)r.State=RaiderState.Returning;
            bool returning=r.State==RaiderState.Returning||r.State==RaiderState.StrandedReturn;
            int target=returning?16:8;
            if(r.State==RaiderState.GuardingClosedAnchor)
            {
                // Local guard intercepts inside its authored area; after intrusion ends it walks home.
                var enemy=new[]{West,East}.Where(h=>h.Continues&&h.Node>=14).OrderBy(h=>h.Formation.FormationId,StringComparer.Ordinal).FirstOrDefault();
                target=enemy?.Node??16;
            }
            var path=Graph.Path(f.Node,target,null,f.Hungry);
            foreach(int next in path.Skip(1))
            {
                var occupant=Occupants.FirstOrDefault(o=>o!=f&&o.Node==next);
                if(occupant!=null)
                {
                    if(!i.Raiders.Any(a=>a.Force==occupant)&&f.Tempo>=0&&(r.State!=RaiderState.StrandedReturn||next>=14))StartIncidentBattle(f,occupant);
                    else r.Blocker=f.Tempo<0?"Negative Tempo: offense unavailable":"Allied formation occupies route";
                    return;
                }
                int cost=Graph.Cost(f.Node,next,f.Hungry);if(cost>f.Tempo){r.Blocker="Insufficient Tempo to continue";return;}
                int before=f.Node;f.Tempo-=cost;f.Node=next;Trace(r.Id+" node "+before+" -> "+next+" Tempo="+f.Tempo);
            }
            RaiderArrival(r);
        }
        private void RaiderArrival(IncidentRaider r)
        {
            if(!r.OnMap)return;
            if(r.Force.Node==8&&r.State==RaiderState.AdvanceToTarget&&!Incident.Ravaged)
            {r.State=RaiderState.RaidArmed;r.ArmedRefresh=Refresh;Trace(r.Id+" raid armed R"+Refresh+"; both humans may respond");}
            if(r.Force.Node==16&&(r.State==RaiderState.Returning||r.State==RaiderState.StrandedReturn))
            {
                r.State=Incident.Phase==PortalPhase.Red||Incident.Phase==PortalPhase.Whiteout?RaiderState.ExitedThroughPortal:RaiderState.GuardingClosedAnchor;
                Trace(r.Id+" -> "+r.State+"; roster retained");
            }
        }
        private void FinishRaiderRefresh()
        {
            foreach(var r in Incident.Raiders.Where(r=>r.OnMap)){r.Force.Provisions=Math.Max(0,r.Force.Provisions-r.Force.Consumption);r.Force.Formation.ApplyOneFieldStrategicRefresh();}
        }
    }
}

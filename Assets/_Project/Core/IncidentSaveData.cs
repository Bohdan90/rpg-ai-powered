using System;
using System.IO;
using System.Linq;

namespace RPG.Core
{
    [Serializable] public sealed class RaiderSaveData
    {
        public int state,node,tempo,provisions,armedRefresh;
        public string blocker;
        public StrategicSaveFormation formation;
        internal static RaiderSaveData Capture(IncidentRaider r)=>new RaiderSaveData{state=(int)r.State,node=r.Force.Node,tempo=r.Force.Tempo,provisions=r.Force.Provisions,armedRefresh=r.ArmedRefresh,blocker=r.Blocker,formation=StrategicSaveFormation.Capture(r.Force.Formation)};
        internal void Restore(IncidentRaider r,int refresh)
        {
            StrategicSaveData.Require(Enum.IsDefined(typeof(RaiderState),state)&&node>=1&&node<=16&&tempo>=-40&&tempo<=100&&provisions>=0&&provisions<=30&&armedRefresh>=0&&armedRefresh<=refresh&&blocker!=null&&formation!=null,"Invalid incident raider.");
            r.Force.Formation=formation.Restore(r.Force.Formation);r.Force.Node=node;r.Force.Tempo=tempo;r.Force.Provisions=provisions;r.State=(RaiderState)state;r.ArmedRefresh=armedRefresh;r.Blocker=blocker;
            StrategicSaveData.Require((r.State==RaiderState.Defeated)==!r.Force.Continues,"Inconsistent raider death.");
            StrategicSaveData.Require(r.State==RaiderState.RaidArmed?node==8&&armedRefresh>0:armedRefresh==0,"Invalid raid commitment.");
        }
        internal void Write(BinaryWriter w){w.Write(state);w.Write(node);w.Write(tempo);w.Write(provisions);w.Write(armedRefresh);w.Write(blocker);formation.Write(w);}
    }
    [Serializable] public sealed class IncidentSaveData
    {
        public bool enabled,worldPhase,ravaged,cycleEligible,continuityBroken,rewardPaid;
        public int phase,whiteout,cursor,contract,holdCycles,appliedBattle;
        public string id,claimant,holdReason;
        public string[] actorOrder;
        public RaiderSaveData[] raiders;
        internal static IncidentSaveData Capture(IncidentState i)=>new IncidentSaveData{enabled=i.Enabled,worldPhase=i.WorldPhase,ravaged=i.Ravaged,cycleEligible=i.CycleEligible,continuityBroken=i.ContinuityBroken,rewardPaid=i.RewardPaid,phase=(int)i.Phase,whiteout=i.WhiteoutRemaining,cursor=i.Cursor,contract=(int)i.Contract,holdCycles=i.HoldCycles,appliedBattle=i.AppliedBattle,id=IncidentState.Id,claimant=i.ClaimantId,holdReason=i.HoldReason,actorOrder=i.ActorOrder.ToArray(),raiders=i.Raiders.Select(RaiderSaveData.Capture).ToArray()};
        internal IncidentState Restore(int refresh,int completed,int battleNumber,bool terminal)
        {
            StrategicSaveData.Require(id==IncidentState.Id&&Enum.IsDefined(typeof(PortalPhase),phase)&&Enum.IsDefined(typeof(HoldState),contract)&&raiders!=null&&raiders.Length==2&&raiders.All(r=>r!=null)&&actorOrder!=null&&actorOrder.Length<=2&&actorOrder.Distinct().Count()==actorOrder.Length&&cursor>=0&&cursor<=actorOrder.Length&&claimant!=null&&holdReason!=null&&holdCycles>=0&&holdCycles<=2&&appliedBattle==battleNumber,"Invalid incident snapshot.");
            var i=new IncidentState(enabled){Phase=(PortalPhase)phase,WhiteoutRemaining=whiteout,WorldPhase=worldPhase,Cursor=cursor,ActorOrder=actorOrder.ToArray(),Ravaged=ravaged,Contract=(HoldState)contract,ClaimantId=claimant,HoldReason=holdReason,HoldCycles=holdCycles,CycleEligible=cycleEligible,ContinuityBroken=continuityBroken,RewardPaid=rewardPaid,AppliedBattle=appliedBattle};
            for(int n=0;n<2;n++)raiders[n].Restore(i.Raiders[n],refresh);
            StrategicSaveData.Require(actorOrder.All(id=>i.Raiders.Any(r=>r.Id==id))&&actorOrder.SequenceEqual(actorOrder.OrderBy(id=>id,StringComparer.Ordinal)),"Invalid actor iteration.");
            StrategicSaveData.Require(worldPhase?completed==2:completed<2&&cursor==0&&actorOrder.Length==0,"Invalid resolver phase.");
            StrategicSaveData.Require(rewardPaid==(i.Contract==HoldState.Completed)&&(!rewardPaid||holdCycles==2)&&(!ravaged||i.Contract==HoldState.Failed||i.Contract==HoldState.Completed||i.Contract==HoldState.Expired),"Invalid contract payment/state.");
            StrategicSaveData.Require(claimant==""||claimant=="duel-West"||claimant=="duel-East","Invalid claimant.");
            int phaseRefresh=refresh;
            var expected=!enabled?PortalPhase.Stable:phaseRefresh==1?PortalPhase.Precursor:phaseRefresh<=3?PortalPhase.Red:phaseRefresh<=5?PortalPhase.Whiteout:phaseRefresh==6?PortalPhase.Closure:phaseRefresh==7?PortalPhase.Recovery:PortalPhase.Stable;
            StrategicSaveData.Require(i.Phase==expected,"Phase/Refresh mismatch.");
            StrategicSaveData.Require(whiteout==(i.Phase==PortalPhase.Whiteout?6-phaseRefresh:0),"Invalid Whiteout boundary.");
            if(!enabled)StrategicSaveData.Require(i.Contract==HoldState.Disabled&&!ravaged&&i.Raiders.All(r=>r.State==RaiderState.Staged),"Disabled incident has activity.");
            return i;
        }
        internal void Write(BinaryWriter w)
        {
            w.Write(id);w.Write(enabled);w.Write(worldPhase);w.Write(ravaged);w.Write(cycleEligible);w.Write(continuityBroken);w.Write(rewardPaid);w.Write(phase);w.Write(whiteout);w.Write(cursor);w.Write(contract);w.Write(holdCycles);w.Write(appliedBattle);w.Write(claimant);w.Write(holdReason);
            w.Write(actorOrder.Length);foreach(var a in actorOrder)w.Write(a);w.Write(raiders.Length);foreach(var r in raiders)r.Write(w);
        }
    }
}

using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace RPG.Core
{
    [Serializable] public sealed class DuelSaveForce
    {
        public int node,tempo,provisions,pressure;
        public StrategicSaveFormation formation;
        internal static DuelSaveForce Capture(DuelForce f)=>new DuelSaveForce {node=f.Node,tempo=f.Tempo,provisions=f.Provisions,pressure=f.Pressure,formation=StrategicSaveFormation.Capture(f.Formation)};
        internal DuelForce Restore(Side side)
        {
            StrategicSaveData.Require(CrossroadsScenario.Map.Node(node)!=null&&tempo>=-40&&tempo<=100&&provisions>=0&&provisions<=30&&pressure>=0&&pressure<=3000000&&formation!=null,"Invalid duel force.");
            var f=new DuelForce(side);f.Formation=formation.Restore(f.Formation);f.Node=node;f.Tempo=tempo;f.Provisions=provisions;f.Pressure=pressure;return f;
        }
        internal void Write(BinaryWriter w){w.Write(node);w.Write(tempo);w.Write(provisions);w.Write(pressure);formation.Write(w);}
    }
    [Serializable] public sealed class CrossroadsSaveData
    {
        public const int Version=1;
        public const string Scenario="CrossroadsDuel-02";
        public int version,refresh,startingSide,activeSide,completed,winner,battleNumber;
        public uint seed;
        public bool handoff;
        public string scenario,checksum;
        public int[] owners;
        public string[] events;
        public DuelSaveForce west,east;
        public CrossroadsScenario Restore()
        {
            StrategicSaveData.Require(version==Version&&scenario==Scenario,"Unsupported Crossroads schema/scenario.");
            var candidate=new CrossroadsScenario(this);
            StrategicSaveData.Require(!string.IsNullOrEmpty(checksum)&&checksum==ComputeHash(),"Crossroads checksum mismatch.");return candidate;
        }
        public string ComputeHash()
        {
            using(var stream=new MemoryStream())using(var w=new BinaryWriter(stream))
            {
                w.Write(version);w.Write(scenario);w.Write(seed);w.Write(refresh);w.Write(startingSide);w.Write(activeSide);w.Write(completed);w.Write(winner);w.Write(battleNumber);w.Write(handoff);
                w.Write(owners.Length);foreach(int n in owners)w.Write(n);west.Write(w);east.Write(w);w.Write(events.Length);foreach(var e in events)w.Write(e);
                w.Flush();using(var sha=SHA256.Create())return Convert.ToBase64String(sha.ComputeHash(stream.ToArray()));
            }
        }
    }
    public sealed partial class CrossroadsScenario
    {
        public CrossroadsSaveData CaptureSave()
        {
            if(!CanSave)throw new InvalidOperationException("Finish battle before saving Crossroads.");
            var d=new CrossroadsSaveData {version=CrossroadsSaveData.Version,scenario=CrossroadsSaveData.Scenario,seed=Seed,refresh=Refresh,
                startingSide=(int)StartingSide,activeSide=(int)ActiveSide,completed=CompletedActivations,handoff=HandoffPending,winner=Winner.HasValue?(int)Winner.Value:-1,
                battleNumber=battleNumber,owners=owners.Select(o=>o.HasValue?(int)o.Value:-1).ToArray(),west=DuelSaveForce.Capture(West),east=DuelSaveForce.Capture(East),events=events.ToArray()};
            d.checksum=d.ComputeHash();return d;
        }
        internal CrossroadsScenario(CrossroadsSaveData d):this((Side)d.startingSide,d.seed)
        {
            StrategicSaveData.Require(d.refresh>=1&&d.refresh<=1000000&&d.completed>=0&&d.completed<=1&&d.battleNumber>=0&&d.battleNumber<=1000000,"Invalid duel counters.");
            StrategicSaveData.Require(ValidSide((Side)d.activeSide)&&d.activeSide==(int)(d.completed==0?StartingSide:Other(StartingSide)),"Invalid active side.");
            StrategicSaveData.Require(d.winner==-1||ValidSide((Side)d.winner),"Invalid winner.");
            StrategicSaveData.Require(d.owners!=null&&d.owners.Length==3&&d.owners.All(o=>o==-1||ValidSide((Side)o)),"Invalid objective owners.");
            StrategicSaveData.Require(d.west!=null&&d.east!=null&&d.events!=null&&d.events.Length<=20000&&d.events.All(e=>e!=null&&e.Length<=4096),"Incomplete snapshot.");
            West=d.west.Restore(Side.West);East=d.east.Restore(Side.East);
            StrategicSaveData.Require(!West.Continues||!East.Continues||West.Node!=East.Node,"Overlapping formations.");
            bool pressureWin=Math.Max(West.Pressure,East.Pressure)>=PressureTarget&&West.Pressure!=East.Pressure;
            Side? expected=!West.Continues?(Side?)Side.East:!East.Continues?Side.West:pressureWin?(West.Pressure>East.Pressure?Side.West:Side.East):(Side?)null;
            StrategicSaveData.Require(d.winner==(expected.HasValue?(int)expected.Value:-1)&&(!expected.HasValue||!d.handoff),"Inconsistent match result.");
            Refresh=d.refresh;ActiveSide=(Side)d.activeSide;CompletedActivations=d.completed;HandoffPending=d.handoff;Winner=expected;battleNumber=d.battleNumber;
            for(int i=0;i<3;i++)owners[i]=d.owners[i]<0?(Side?)null:(Side)d.owners[i];events.AddRange(d.events);
        }
    }
}

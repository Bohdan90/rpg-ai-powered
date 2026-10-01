using System;
using System.Collections.Generic;
using System.Linq;
namespace RPG.Core
{
    // One authored scale experiment. Zones are geography, never economic Parents.
    public static class ProductionRoads
    {
        public const string ScenarioId="ProductionRoads-08";
        public static readonly string[] Zones={"West March","Pine Uplands","Iron Ridge","Crosswater","Reed Lowlands","East Downs","Ash Pass","Stone Valley"};
        public static readonly int[][] ZoneNodes={new[]{1,2,3,16,17,26,27,28},new[]{4,5,14,18,24,29,30,31},new[]{6,19,32,33,34,35,36,37},new[]{7,9,20,38,39,40,41,42,43},new[]{8,15,21,44,45,46,47,48},new[]{10,11,12,13,22,23,49,50,51,52},new[]{25,53,54,55,56,57,58,59,60}};
        public static readonly int[] PointsOfInterest={1,6,7,8,13,14,15,16,17,18,19,20,21,22,23,24,25};
        public static readonly int[] Junctions={3,5,9,12,27,28,31,35,37,40,43,46,48,50,53,56};
        public static bool Marker(WorldAddress a)=>a.World==WorldId.StoneValley||PointsOfInterest.Contains(a.Node)||Junctions.Contains(a.Node);
        public static readonly StrategicGraph Map=Build();
        private static StrategicGraph Build()
        {
            // Explicit positions are decorative; edge Tempo below is gameplay truth.
            var xy=new Dictionary<int,(float,float)>{
                {1,(0,6)},{2,(1,7)},{3,(3,7)},{16,(4,6)},{17,(3,4)},{26,(2,3)},{27,(1,4)},{28,(0,5)},
                {4,(3,10)},{5,(5,11)},{14,(7,11)},{18,(8,10)},{24,(7,9)},{29,(5,8)},{30,(3,8)},{31,(4,9)},
                {6,(10,12)},{19,(12,12)},{32,(9,11)},{33,(9,10)},{34,(11,10)},{35,(13,10)},{36,(14,11)},{37,(13,12)},
                {7,(9,6)},{9,(12,7)},{20,(11,4)},{38,(8,4)},{39,(7,6)},{40,(6,5)},{41,(8,7)},{42,(10,8)},{43,(12,5)},
                {8,(5,1)},{15,(3,1)},{21,(1,1)},{44,(0,2)},{45,(3,2)},{46,(5,3)},{47,(7,3)},{48,(8,1)},
                {10,(17,5)},{11,(18,6)},{12,(17,8)},{13,(20,8)},{22,(21,6)},{23,(20,4)},{49,(18,3)},{50,(15,4)},{51,(14,2)},{52,(16,2)},
                {25,(24,12)},{53,(15,10)},{54,(17,10)},{55,(18,11)},{56,(15,8)},{57,(16,9)},{58,(20,12)},{59,(22,11)},{60,(19,11)}};
            var names=CityFoundations.Map.Nodes.ToDictionary(n=>n.Id,n=>n.Name);names[24]="Pine Portal";names[25]="Ash Portal";
            var nodes=xy.OrderBy(x=>x.Key).Select(x=>new StrategicNode(x.Key,names.TryGetValue(x.Key,out var name)?name:"Road",x.Value.Item1,x.Value.Item2)).ToArray();
            var edges=new List<StrategicEdge>();
            void Chain(int cost,params int[] ids){for(int i=1;i<ids.Length;i++)edges.Add(new StrategicEdge(ids[i-1],ids[i],cost));}
            Chain(15,1,2,3,16,17,26,27,28,1);
            Chain(15,4,5,14,18,24,29,30,31,4);
            Chain(20,6,19,37,36,35,34,33,32,6);
            Chain(15,7,41,42,9,43,20,38,40,39,7);
            Chain(15,8,15,21,44,45,46,47,48,8);
            Chain(15,13,22,23,49,52,51,50,10,11,12,13);
            Chain(20,60,55,54,57,56,53,59,58,60);
            Chain(20,59,25); // intentional portal-pass spur, not an accidental bottleneck
            foreach(var e in new[]{(3,30),(28,44),(27,40),(5,32),(24,39),(31,41),(37,42),(43,50),(48,51),(46,40),(12,54),(35,53),(9,56),(36,54),(9,7)})edges.Add(new StrategicEdge(e.Item1,e.Item2,25));
            return new StrategicGraph(nodes,edges.ToArray());
        }
        public static CrossroadsScenario Create(Side first=Side.West,uint seed=20261001)
        {
            var w=new CrossroadsScenario(first,seed,realm:true,westPreset:CombatPreset.Fire,eastPreset:CombatPreset.Ice);
            w.Seamless=new SeamlessWorlds(w,false,production:true);w.Seamless.Observe();
            w.Seamless.SetSideMessage(first,"PRODUCTION ROADS 08 · authored asymmetric graybox. Select a known POI, set destination; teal route is reachable now, gold continues next own activation. Macro zones are geography only.");return w;
        }
    }
    public sealed class GraphDiagnostics
    {
        public int Nodes,Edges,Components,CycleRank; public double AverageDegree;
        public int[] Articulations; public string[] Bridges;
        public static GraphDiagnostics Inspect(StrategicGraph g)
        {
            int Components(int? skip=null,StrategicEdge? removed=null){var left=g.Nodes.Select(n=>n.Id).Where(n=>n!=skip).ToHashSet();int count=0;while(left.Count>0){count++;var q=new Queue<int>();int start=left.Min();left.Remove(start);q.Enqueue(start);while(q.Count>0){int at=q.Dequeue();foreach(int n in g.Neighbors(at))if(n!=skip&&(!removed.HasValue||!removed.Value.Connects(at,n))&&left.Remove(n))q.Enqueue(n);}}return count;}
            int c=Components();return new GraphDiagnostics{Nodes=g.Nodes.Count,Edges=g.Edges.Count,Components=c,CycleRank=g.Edges.Count-g.Nodes.Count+c,AverageDegree=2d*g.Edges.Count/g.Nodes.Count,Articulations=g.Nodes.Where(n=>Components(n.Id)>c).Select(n=>n.Id).OrderBy(n=>n).ToArray(),Bridges=g.Edges.Where(e=>Components(removed:e)>c).Select(e=>Math.Min(e.A,e.B)+"-"+Math.Max(e.A,e.B)).OrderBy(s=>s,StringComparer.Ordinal).ToArray()};
        }
        public override string ToString()=>Nodes+" anchors · "+Edges+" edges · components "+Components+" · degree "+AverageDegree.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)+" · cycle rank "+CycleRank+" · articulation "+string.Join(",",Articulations)+" · bridges "+string.Join(",",Bridges);
    }
}

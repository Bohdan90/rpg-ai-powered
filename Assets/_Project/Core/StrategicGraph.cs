using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace RPG.Core
{
    public sealed class StrategicNode
    {
        public int Id { get; }
        public string Name { get; }
        public float X { get; }
        public float Y { get; }
        public StrategicNode(int id, string name, float x, float y) { Id=id; Name=name; X=x; Y=y; }
    }
    public readonly struct StrategicEdge
    {
        public int A { get; } public int B { get; } public int Tempo { get; }
        public StrategicEdge(int a,int b,int tempo) { A=a; B=b; Tempo=tempo; }
        public bool Connects(int a,int b) => (A==a&&B==b)||(A==b&&B==a);
    }
    // Authored Mission 01 data, not a general campaign map generator.
    public sealed class StrategicGraph
    {
        public ReadOnlyCollection<StrategicNode> Nodes { get; }
        public ReadOnlyCollection<StrategicEdge> Edges { get; }
        public static readonly StrategicGraph Mission01 = new StrategicGraph();
        private StrategicGraph()
        {
            Nodes=Array.AsReadOnly(new[] {
                new StrategicNode(1,"Baron Keep",0,3),new StrategicNode(2,"West Road",1,3),
                new StrategicNode(3,"Crossroads",2,3),new StrategicNode(4,"North Road",3,5),
                new StrategicNode(5,"North Lookout",4,5.5f),new StrategicNode(6,"North Pass",5,5),
                new StrategicNode(7,"East Ridge",6,6),new StrategicNode(8,"Old Bridge",4,3),
                new StrategicNode(9,"Central Approach",5,3),new StrategicNode(10,"Portal Verge",6,3),
                new StrategicNode(11,"Awakened Portal",7,3),new StrategicNode(12,"Riverside Village",1,1),
                new StrategicNode(13,"South Fork",2,1),new StrategicNode(14,"Frontier Waystation",3,0),
                new StrategicNode(15,"Forest Track",4,0),new StrategicNode(16,"South Ford",5,0),
                new StrategicNode(17,"Eastern Ravine",6,1),new StrategicNode(18,"Portal Guard Camp",7,2) });
            Edges=Array.AsReadOnly(new[] {new StrategicEdge(1,2,20),new StrategicEdge(2,3,20),new StrategicEdge(2,12,20),
                new StrategicEdge(3,4,25),new StrategicEdge(4,5,25),new StrategicEdge(5,6,25),new StrategicEdge(6,7,20),
                new StrategicEdge(6,10,30),new StrategicEdge(3,8,20),new StrategicEdge(8,9,15),new StrategicEdge(9,10,20),
                new StrategicEdge(12,13,20),new StrategicEdge(13,14,20),new StrategicEdge(14,15,25),new StrategicEdge(15,16,25),
                new StrategicEdge(16,17,25),new StrategicEdge(17,10,25),new StrategicEdge(18,10,10),new StrategicEdge(18,17,10),
                new StrategicEdge(11,10,5)});
        }
        public StrategicNode Node(int id) => Nodes.FirstOrDefault(n=>n.Id==id);
        public int Cost(int a,int b,bool hungry=false)
        {
            var edge=Edges.FirstOrDefault(e=>e.Connects(a,b));
            return edge.Tempo==0 ? -1 : hungry ? (int)Math.Ceiling(edge.Tempo*1.25m) : edge.Tempo;
        }
        public IEnumerable<int> Neighbors(int node) => Edges.Where(e=>e.A==node||e.B==node).Select(e=>e.A==node?e.B:e.A).OrderBy(n=>n);
        public int[] Path(int from,int to,Func<int,bool> allowed=null,bool hungry=false,bool hops=false)
        {
            if(Node(from)==null||Node(to)==null)return Array.Empty<int>();
            var costs=Nodes.ToDictionary(n=>n.Id,n=>int.MaxValue); var previous=new Dictionary<int,int>();var open=new HashSet<int>(costs.Keys);
            costs[from]=0;
            while(open.Count>0)
            {
                int at=open.OrderBy(n=>costs[n]).ThenBy(n=>n).First();open.Remove(at);
                if(costs[at]==int.MaxValue)break;
                if(at==to) {var path=new List<int>{to};while(at!=from){at=previous[at];path.Add(at);}path.Reverse();return path.ToArray();}
                foreach(int next in Neighbors(at).Where(n=>open.Contains(n)&&(allowed==null||allowed(n))))
                {int c=costs[at]+(hops?1:Cost(at,next,hungry));if(c<costs[next]){costs[next]=c;previous[next]=at;}}
            }
            return Array.Empty<int>();
        }
        public int PathCost(IReadOnlyList<int> path,bool hungry=false)
        {int sum=0;for(int i=1;i<path.Count;i++){int c=Cost(path[i-1],path[i],hungry);if(c<0)return -1;sum+=c;}return sum;}
        public int Hops(int a,int b) {var p=Path(a,b,hops:true);return p.Length==0?int.MaxValue:p.Length-1;}
    }
}

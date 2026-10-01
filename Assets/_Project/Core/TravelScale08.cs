using System;
using System.Collections.Generic;
using System.Linq;
namespace RPG.Core
{
    // Authored 08B geographic resolution. Render transforms never enter this construction.
    // Ten Tempo per short travel segment; thirty Tempo per authored geographic unit.
    public static class TravelScale08
    {
        public const int SegmentTempo=10;
        public static readonly StrategicGraph ValleyMacro=new StrategicGraph(new[]{
            new StrategicNode(1,"West Portal",0,0),new StrategicNode(2,"Slate Fork",2,0),
            new StrategicNode(3,"North Supply",5,2),new StrategicNode(4,"Valley Beacon",8,0),
            new StrategicNode(5,"South Supply",6,-3),new StrategicNode(6,"East Fork",12,0),
            new StrategicNode(7,"East Portal",14,1),new StrategicNode(8,"High Road",3,2),
            new StrategicNode(9,"Ridge Junction",8,3),new StrategicNode(10,"Upper Pass",11,2),
            new StrategicNode(11,"Low Road",3,-2),new StrategicNode(12,"Dry Ford",9,-3),
            new StrategicNode(13,"Lower Pass",12,-2),new StrategicNode(14,"Central Fork",5,0),
            new StrategicNode(15,"Beacon Approach",10,-1)},
            new[]{new StrategicEdge(1,2,1),new StrategicEdge(2,8,1),new StrategicEdge(8,3,1),new StrategicEdge(3,9,1),new StrategicEdge(9,10,1),new StrategicEdge(10,6,1),new StrategicEdge(6,7,1),
            new StrategicEdge(2,11,1),new StrategicEdge(11,5,1),new StrategicEdge(5,12,1),new StrategicEdge(12,13,1),new StrategicEdge(13,6,1),
            new StrategicEdge(2,14,1),new StrategicEdge(14,4,1),new StrategicEdge(4,15,1),new StrategicEdge(15,6,1),new StrategicEdge(3,14,1),new StrategicEdge(5,14,1),new StrategicEdge(9,4,1),new StrategicEdge(12,15,1)});
        public static readonly StrategicGraph Mainland=Expand(ProductionRoads.Map);
        public static readonly StrategicGraph Valley=Expand(ValleyMacro);
        public static bool IsTravel(int id)=>id>=1000;
        public static StrategicGraph Expand(StrategicGraph macro)
        {
            var nodes=macro.Nodes.ToList();var edges=new List<StrategicEdge>();
            // Stable IDs depend on original endpoint IDs, not enumeration order or visual layout.
            foreach(var edge in macro.Edges.OrderBy(e=>Math.Min(e.A,e.B)).ThenBy(e=>Math.Max(e.A,e.B))) {
                int a=Math.Min(edge.A,edge.B),b=Math.Max(edge.A,edge.B);var from=macro.Node(a);var to=macro.Node(b);
                double dx=to.X-from.X,dy=to.Y-from.Y;
                int steps=Math.Max(2,(int)Math.Ceiling(Math.Sqrt(dx*dx+dy*dy)*3));
                int previous=a;
                for(int step=1;step<=steps;step++) {
                    int next=step==steps?b:1000+(a*100+b)*100+step;
                    if(step<steps)nodes.Add(new StrategicNode(next,"Road "+a+"–"+b+" · "+step+"/"+steps,from.X+(to.X-from.X)*step/steps,from.Y+(to.Y-from.Y)*step/steps));
                    edges.Add(new StrategicEdge(previous,next,SegmentTempo));previous=next;
                }
            }
            return new StrategicGraph(nodes.OrderBy(n=>n.Id).ToArray(),edges.ToArray());
        }
        public static CrossroadsScenario Create(Side first=Side.West,uint seed=20261001)
        {
            var w=new CrossroadsScenario(first,seed,realm:true,westPreset:CombatPreset.Fire,eastPreset:CombatPreset.Ice);
            w.Seamless=new SeamlessWorlds(w,false,production:true,dense:true);w.Seamless.Observe();
            w.Seamless.SetSideMessage(first,"PRODUCTION ROADS 08B · real road positions · queue destination, then explicitly Continue Travel; turns do not auto-move. Portals require explicit traversal.");return w;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
namespace RPG.Core
{
    public enum WorldId { Frontier, StoneValley }
    public enum KnowledgeLevel { Unexplored, ExploredNotObserved, CurrentlyObserved }
    public readonly struct WorldAddress : IEquatable<WorldAddress>, IComparable<WorldAddress>
    {
        public WorldId World { get; } public int Node { get; }
        public WorldAddress(WorldId world,int node){World=world;Node=node;}
        public bool Equals(WorldAddress o)=>World==o.World&&Node==o.Node;
        public override bool Equals(object o)=>o is WorldAddress a&&Equals(a);
        public override int GetHashCode()=>((int)World*397)^Node;
        public int CompareTo(WorldAddress o)=>World!=o.World?World.CompareTo(o.World):Node.CompareTo(o.Node);
        public static bool operator ==(WorldAddress a,WorldAddress b)=>a.Equals(b);
        public static bool operator !=(WorldAddress a,WorldAddress b)=>!a.Equals(b);
        public override string ToString()=>(World==WorldId.Frontier?"A":"B")+Node.ToString("00");
    }
    public sealed class PortalLink
    {
        public string Id {get;} public WorldAddress A {get;} public WorldAddress B {get;}
        public PortalLink(string id,WorldAddress a,WorldAddress b){Id=id;A=a;B=b;}
        public bool Contains(WorldAddress p)=>p==A||p==B;
        public WorldAddress Other(WorldAddress p)=>p==A?B:A;
    }
    public sealed class PortalPreview
    {
        public string Reason {get;internal set;} public string Destination {get;internal set;}
        public string RouteState {get;internal set;} public bool Legal=>Reason==null;
        public int Cost=>20;
    }
    [Serializable] public sealed class KnownArmy
    {
        public string id,composition; public int world,node,refresh,count,side;
        public WorldAddress Address=>new WorldAddress((WorldId)world,node);
        internal KnownArmy Copy()=>(KnownArmy)MemberwiseClone();
    }
    [Serializable] public sealed class WorldObservation
    {
        public int sequence,refresh,world; public string actor,description,kind;
        // Only observed nodes are stored; each separate event is a separate visible fragment.
        public int[] nodes;
        internal WorldObservation Copy()=>new WorldObservation{sequence=sequence,refresh=refresh,world=world,actor=actor,description=description,kind=kind,nodes=nodes.ToArray()};
    }
    [Serializable] public sealed class KnownPlace
    {
        public int world,node,owner=-1,food=-1;public string portalState="";
        public WorldAddress Address=>new WorldAddress((WorldId)world,node);
        internal KnownPlace Copy()=>(KnownPlace)MemberwiseClone();
    }
    public sealed class WorldKnowledge
    {
        internal readonly HashSet<WorldAddress> explored=new HashSet<WorldAddress>();
        internal readonly HashSet<WorldAddress> observed=new HashSet<WorldAddress>();
        internal readonly Dictionary<string,KnownArmy> armies=new Dictionary<string,KnownArmy>(StringComparer.Ordinal);
        internal readonly Dictionary<WorldAddress,KnownPlace> places=new Dictionary<WorldAddress,KnownPlace>();
        internal readonly HashSet<string> links=new HashSet<string>(StringComparer.Ordinal);
        internal readonly List<WorldObservation> events=new List<WorldObservation>();
        internal int nextSequence=1;
        public KnowledgeLevel At(WorldAddress a)=>observed.Contains(a)?KnowledgeLevel.CurrentlyObserved:explored.Contains(a)?KnowledgeLevel.ExploredNotObserved:KnowledgeLevel.Unexplored;
        public WorldId[] Worlds=>explored.Select(a=>a.World).Distinct().OrderBy(x=>x).ToArray();
        public WorldAddress[] KnownNodes=>explored.OrderBy(x=>x).ToArray();
        public KnownArmy[] LastKnown=>armies.Values.OrderBy(x=>x.id,StringComparer.Ordinal).Select(x=>x.Copy()).ToArray();
        public WorldObservation[] History=>events.Select(e=>e.Copy()).ToArray();
        public KnownPlace Place(WorldAddress a)=>places.TryGetValue(a,out var p)?p.Copy():null;
        public bool KnowsLink(string id)=>links.Contains(id);
    }
    // Document54 prototype. Layout/camera/read cursors are deliberately absent from simulation.
    public sealed partial class SeamlessWorlds
    {
        public const string ScenarioId="SeamlessWorlds-07";
        public static readonly StrategicGraph Frontier=new StrategicGraph(CityFoundations.Map.Nodes.Concat(new[]{new StrategicNode(24,"North-West Portal",3,6),new StrategicNode(25,"North-East Portal",5,6)}).ToArray(),CityFoundations.Map.Edges.Concat(new[]{new StrategicEdge(4,24,20),new StrategicEdge(9,25,20)}).ToArray());
        public static readonly StrategicGraph Valley=new StrategicGraph(new[]{new StrategicNode(1,"West Portal",0,0),new StrategicNode(2,"West Fork",2,0),new StrategicNode(3,"North Supply",4,2),new StrategicNode(4,"Valley Beacon",4,0),new StrategicNode(5,"South Supply",4,-2),new StrategicNode(6,"East Fork",6,0),new StrategicNode(7,"East Portal",8,0)},new[]{new StrategicEdge(1,2,20),new StrategicEdge(2,3,20),new StrategicEdge(2,5,20),new StrategicEdge(2,4,35),new StrategicEdge(3,6,20),new StrategicEdge(5,6,20),new StrategicEdge(4,6,35),new StrategicEdge(6,7,20)});
        public static StrategicGraph MapFor(WorldId id)=>id==WorldId.Frontier?Frontier:id==WorldId.StoneValley?Valley:throw new ArgumentOutOfRangeException(nameof(id));
        public static string Name(WorldId id)=>id==WorldId.Frontier?"Frontier":"Stone Valley";
        public static readonly IReadOnlyList<PortalLink> Portals=Array.AsReadOnly(new[]{new PortalLink("West",new WorldAddress(WorldId.Frontier,24),new WorldAddress(WorldId.StoneValley,1)),new PortalLink("East",new WorldAddress(WorldId.Frontier,25),new WorldAddress(WorldId.StoneValley,7))});
        internal readonly CrossroadsScenario world;
        private readonly WorldKnowledge west=new WorldKnowledge(),east=new WorldKnowledge();
        internal string westMessage="07: select an own army and a known local destination.",eastMessage="07: select an own army and a known local destination.";
        public string Message(Side side)=>side==Side.West?westMessage:eastMessage;
        internal void SetSideMessage(Side side,string message){if(side==Side.West)westMessage=message;else eastMessage=message;}
        private void Publish(Side side){if(side==Side.West)westMessage=LastMessage;else eastMessage=LastMessage;}
        internal readonly HashSet<string> opaque=new HashSet<string>(StringComparer.Ordinal);
        public bool TemporaryRoute {get;internal set;}
        public int NorthFood {get;internal set;}=24;
        public int SouthFood {get;internal set;}=24;
        public Side? BeaconOwner {get;internal set;}
        public int Revision {get;internal set;}
        public string LastMessage {get;internal set;}="07: local graph movement; only explicit portals cross worlds.";
        public WorldKnowledge Knowledge(Side side)=>side==Side.West?west:east;
        internal SeamlessWorlds(CrossroadsScenario w,bool temporary,bool initialize=true)
        {
            world=w;TemporaryRoute=temporary;
            if(initialize)foreach(var k in new[]{west,east})foreach(var n in Frontier.Nodes)k.explored.Add(new WorldAddress(WorldId.Frontier,n.Id));
        }
        public static CrossroadsScenario Create(Side first=Side.West,bool temporary=false,CombatPreset west=CombatPreset.Fire,CombatPreset east=CombatPreset.Ice,uint seed=20260930)
        {
            var w=new CrossroadsScenario(first,seed,realm:true,westPreset:west,eastPreset:east);w.Seamless=new SeamlessWorlds(w,temporary);w.Seamless.Observe();return w;
        }
        // Explicit rare-case initial fixture, not earned play history or the normal match.
        public static CrossroadsScenario CreateOpaqueProbe()
        {
            var w=Create(Side.East);var s=w.Seamless;
            foreach(var edge in new[]{(3,6),(2,1),(2,4),(2,5)})s.opaque.Add(EdgeKey(WorldId.StoneValley,edge.Item1,edge.Item2));
            w.West.WorldId=w.East.WorldId=WorldId.StoneValley;w.West.Node=3;w.East.Node=1;
            s.east.explored.UnionWith(new[]{new WorldAddress(WorldId.StoneValley,2),new WorldAddress(WorldId.StoneValley,5)});s.Observe();
            s.westMessage=s.eastMessage="CONTROLLED opaque-edge initial fixture. East previously charted B02/B05. Move East B01 → B02 → B05, then hand off and inspect West's filtered observations.";
            return w;
        }
        internal static string EdgeKey(WorldId id,int a,int b)=>((int)id)+":"+Math.Min(a,b)+":"+Math.Max(a,b);
        public bool SightOpen(WorldId id,int a,int b)=>!opaque.Contains(EdgeKey(id,a,b));
        public string Phase(string id)=>!TemporaryRoute?"Stable":id=="East"?"Disabled":world.Refresh<=3?"Red":world.Refresh==4?"Whiteout · 2 full Refreshes":world.Refresh==5?"Whiteout · 1 full Refresh":"Closure / Recovery";
        private bool Open(string id)=>!TemporaryRoute||id=="West"&&world.Refresh<6;
        internal HashSet<WorldAddress> Visible(Side side)
        {
            var result=new HashSet<WorldAddress>();
            var sources=world.Realm.Armies.Where(f=>f.Formation.Side==side&&f.Continues).Select(f=>f.Address).ToList();
            if(world.Foundations.City(side).Functioning)sources.Add(new WorldAddress(WorldId.Frontier,world.OwnKeep(side)));
            foreach(var source in sources){var q=new Queue<(WorldAddress,int)>();var seen=new HashSet<WorldAddress>{source};q.Enqueue((source,0));while(q.Count>0){var (at,depth)=q.Dequeue();result.Add(at);if(depth==2)continue;foreach(int n in MapFor(at.World).Neighbors(at.Node)){var next=new WorldAddress(at.World,n);if(SightOpen(at.World,at.Node,n)&&seen.Add(next))q.Enqueue((next,depth+1));}}}
            return result;
        }
        private static string Composition(DuelForce f)=>string.Join(" · ",f.Formation.LivingMembers.GroupBy(c=>c.Profile.Id).OrderBy(g=>g.Key).Select(g=>g.Count()+" "+g.Key));
        private KnownArmy Snapshot(DuelForce f)=>new KnownArmy{id=f.Formation.FormationId,side=(int)f.Formation.Side,world=(int)f.WorldId,node=f.Node,refresh=world.Refresh,count=f.Formation.LivingMembers.Count(),composition=Composition(f)};
        private int westGroupFloor=int.MaxValue,eastGroupFloor=int.MaxValue;
        private void Event(Side side,WorldId id,string actor,string kind,string description,params int[] nodes)
        {
            var k=Knowledge(side);var last=k.events.LastOrDefault();
            if(kind=="Move"&&last!=null&&last.sequence>=(side==Side.West?westGroupFloor:eastGroupFloor)&&last.kind==kind&&last.actor==actor&&last.world==(int)id&&last.refresh==world.Refresh&&last.description==description&&last.nodes.Last()==nodes[0]&&last.nodes.Length<1024){last.nodes=last.nodes.Concat(nodes.Skip(1)).ToArray();return;}
            k.events.Add(new WorldObservation{sequence=k.nextSequence++,refresh=world.Refresh,world=(int)id,actor=actor,kind=kind,description=description,nodes=nodes});
        }
        public bool IsVisibleEnemy(Side side,string id)=>world.Realm.Army(id) is DuelForce f&&f.Continues&&f.Formation.Side!=side&&Knowledge(side).observed.Contains(f.Address);
        public void Observe(string alreadyRecordedActor=null)
        {
            foreach(var side in new[]{Side.West,Side.East}){
                var k=Knowledge(side);var now=Visible(side);var oldObserved=k.observed.ToHashSet();var knownBefore=k.armies.Keys.ToHashSet();k.observed.Clear();k.observed.UnionWith(now);k.explored.UnionWith(now);
                foreach(var a in now){int owner=a.World==WorldId.Frontier?(world.Owner(a.Node).HasValue?(int)world.Owner(a.Node).Value:-1):a.Node==4&&BeaconOwner.HasValue?(int)BeaconOwner.Value:-1;
                    int food=a.World==WorldId.StoneValley?(a.Node==3?NorthFood:a.Node==5?SouthFood:-1):a.Node==8?world.WaystationFood:-1;
                    var link=Portals.FirstOrDefault(p=>p.Contains(a));string phase=link==null?"":Phase(link.Id);
                    if(link!=null&&k.places.TryGetValue(a,out var previous)&&previous.portalState!=phase)Event(side,a.World,"","Portal",a+" · "+phase,a.Node);
                    k.places[a]=new KnownPlace{world=(int)a.World,node=a.Node,owner=owner,food=food,portalState=phase};
                }
                // Reobserving an empty old position invalidates its current-looking marker only.
                foreach(var old in k.armies.Values.ToArray())if(now.Contains(old.Address)&&!world.Realm.Armies.Any(f=>f.Continues&&f.Formation.FormationId==old.id&&f.Address==old.Address))k.armies.Remove(old.id);
                foreach(var f in world.Realm.Armies.Where(f=>f.Formation.Side!=side&&f.Continues&&now.Contains(f.Address))){bool newContact=!knownBefore.Contains(f.Formation.FormationId)||!oldObserved.Contains(f.Address);var snapshot=Snapshot(f);k.armies[snapshot.id]=snapshot;if(newContact&&snapshot.id!=alreadyRecordedActor)Event(side,f.WorldId,snapshot.id,"Contact","Observed "+snapshot.composition,f.Node);}
            }
        }
        private void Step(DuelForce f,WorldAddress next)
        {
            var from=f.Address;
            foreach(var side in new[]{Side.West,Side.East}.Where(s=>s!=f.Formation.Side)){
                var k=Knowledge(side);bool a=k.observed.Contains(from),b=k.observed.Contains(next);
                if(a&&b&&from.World==next.World)Event(side,from.World,f.Formation.FormationId,"Move","Observed movement · "+Composition(f),from.Node,next.Node);
                else {if(a)Event(side,from.World,f.Formation.FormationId,"Lost","Lost contact",from.Node);if(b)Event(side,next.World,f.Formation.FormationId,"Appeared","Appeared · "+Composition(f),next.Node);}
            }
            f.WorldId=next.World;f.Node=next.Node;Observe(f.Formation.FormationId);
        }
        public StrategicMovePreview PreviewMove(Side side,WorldAddress destination)
        {
            var p=new StrategicMovePreview();var f=world.Realm.Selected(side);var k=Knowledge(side);
            if(!world.Realm.CanAct(side)||f==null||!f.Continues){p.Reason="Select a continuing own army.";return p;}
            if(destination.World!=f.WorldId||k.At(destination)==KnowledgeLevel.Unexplored||destination==f.Address){p.Reason="Choose another known node in the army's world; Traverse is separate.";return p;}
            var graph=MapFor(f.WorldId);
            p.Path=graph.Path(f.Node,destination.Node,n=>k.explored.Contains(new WorldAddress(f.WorldId,n))&&!world.AtEnemyCity(f,n)&&(world.AtOwnCity(f)&&n==f.Node||f.WorldId==WorldId.Frontier&&n==world.OwnKeep(side)||!world.Realm.Armies.Any(o=>o!=f&&o.Continues&&o.WorldId==f.WorldId&&o.Node==n&&(o.Formation.Side==side||k.observed.Contains(o.Address)))),f.RealmProvisions==0);
            if(p.Path.Length<2){p.Reason="No known legal local route.";return p;}p.Cost=graph.PathCost(p.Path,f.RealmProvisions==0);if(p.Cost>f.Tempo)p.Reason="Insufficient Tempo: "+p.Cost+" required.";return p;
        }
        public bool Move(Side side,WorldAddress destination,int expectedRevision=-1)
        {
            if(expectedRevision>=0&&expectedRevision!=Revision)return false;var p=PreviewMove(side,destination);if(!p.IsLegal)return false;var f=world.Realm.Selected(side);var graph=MapFor(f.WorldId);
            westGroupFloor=west.nextSequence;eastGroupFloor=east.nextSequence;
            for(int i=1;i<p.Path.Length;i++){
                var next=new WorldAddress(f.WorldId,p.Path[i]);if(!(next.World==WorldId.Frontier&&next.Node==world.OwnKeep(side))&&world.Realm.Armies.Any(o=>o!=f&&o.Continues&&o.Address==next)){LastMessage="Movement stopped before an obstructed local passage.";break;}
                var visibleEnemies=world.Realm.Armies.Where(o=>IsVisibleEnemy(side,o.Formation.FormationId)).Select(o=>o.Formation.FormationId).ToHashSet();
                f.Tempo-=graph.Cost(f.Node,next.Node,f.RealmProvisions==0);world.Realm.LowerCeilings(f);Step(f,next);Revision++;world.Realm.CancelAbsentRepairs(side);LastMessage=f.Formation.FormationId+" moved to "+f.Address+" · Tempo "+f.Tempo;
                if(world.Realm.Armies.Any(o=>IsVisibleEnemy(side,o.Formation.FormationId)&&!visibleEnemies.Contains(o.Formation.FormationId))){LastMessage+=" · new hostile contact: remaining route cancelled.";break;}
            }
            westGroupFloor=eastGroupFloor=int.MaxValue;Publish(side);return true;
        }
        public PortalPreview PreviewTraverse(Side side,string armyId,WorldAddress origin)
        {
            var p=new PortalPreview{Destination="Unknown destination"};var f=world.Realm.Army(armyId);var link=Portals.FirstOrDefault(l=>l.Contains(origin));
            if(!world.Realm.CanAct(side)||f==null||f.Formation.Side!=side||!f.Continues||f.Address!=origin||link==null){p.Reason="Active own formation must occupy this portal anchor.";return p;}
            p.RouteState=Phase(link.Id);if(!Open(link.Id)){p.Reason="Route closed or disabled.";return p;}if(f.Tempo<20){p.Reason="At least 20 Tempo required; debt cannot pay passage.";return p;}
            var k=Knowledge(side);if(k.KnowsLink(link.Id)){var other=link.Other(origin);p.Destination=Name(other.World)+" "+other;if(k.observed.Contains(other)&&world.Realm.Armies.Any(o=>o.Continues&&o.Address==other))p.Destination+=" · observed occupied";}
            return p;
        }
        public bool Traverse(Side side,string armyId,WorldAddress origin,int expectedRevision=-1)
        {
            if(expectedRevision>=0&&Revision!=expectedRevision)return false;var p=PreviewTraverse(side,armyId,origin);if(!p.Legal)return false;var f=world.Realm.Army(armyId);var link=Portals.Single(l=>l.Contains(origin));var to=link.Other(origin);
            f.Tempo-=20;world.Realm.LowerCeilings(f);Revision++;
            if(world.Realm.Armies.Any(o=>o.Continues&&o.Address==to)){LastMessage="PassageBlocked · attempt cost 20 Tempo; formation remains at "+origin;Event(side,origin.World,armyId,"PassageBlocked",LastMessage,origin.Node);Publish(side);return true;}
            Knowledge(side).links.Add(link.Id);Step(f,to);world.Realm.CancelAbsentRepairs(side);LastMessage=armyId+" traversed to "+to+" · 20 Tempo; no Refresh.";Event(side,to.World,armyId,"Traverse",LastMessage,to.Node);Publish(side);return true;
        }
        internal void Claim(DuelForce f){if(f.WorldId==WorldId.StoneValley&&f.Node==4)BeaconOwner=f.Formation.Side;}
        internal void FinishRefresh()
        {
            foreach(var f in world.Realm.Armies.Where(f=>f.Continues&&f.WorldId==WorldId.StoneValley)){
                int stock=f.Node==3?NorthFood:f.Node==5?SouthFood:0;int amount=(int)Math.Min(6,Math.Min(stock,RealmOperations.CarryingCapacity(f)-f.RealmProvisions));if(amount<=0)continue;f.RealmProvisions+=amount;if(f.Node==3)NorthFood-=amount;else SouthFood-=amount;
            }
            if(BeaconOwner.HasValue)world.Force(BeaconOwner.Value).Pressure++;Revision++;
        }
    }
}

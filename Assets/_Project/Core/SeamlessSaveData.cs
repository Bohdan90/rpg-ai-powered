using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
namespace RPG.Core
{
    [Serializable] public sealed class WorldAddressData
    {
        public int world,node;
        public WorldAddress Address=>new WorldAddress((WorldId)world,node);
        internal static WorldAddressData Capture(WorldAddress a)=>new WorldAddressData{world=(int)a.World,node=a.Node};
        internal void Validate(SeamlessWorlds s)=>StrategicSaveData.Require((world==0||world==1)&&s.Map((WorldId)world).Node(node)!=null,"Invalid world/node address.");
        internal void Write(BinaryWriter w){w.Write(world);w.Write(node);}
    }
    [Serializable] public sealed class WorldKnowledgeData
    {
        public WorldAddressData[] explored,observed;public KnownArmy[] armies;public KnownPlace[] places;public string[] links;public WorldObservation[] events;public int nextSequence,readThrough;public int[] readByWorld;
        internal static WorldKnowledgeData Capture(WorldKnowledge k,int read,int[] worldReads=null)=>new WorldKnowledgeData{explored=k.explored.OrderBy(x=>x).Select(WorldAddressData.Capture).ToArray(),observed=k.observed.OrderBy(x=>x).Select(WorldAddressData.Capture).ToArray(),armies=k.LastKnown,places=k.places.OrderBy(x=>x.Key).Select(x=>x.Value.Copy()).ToArray(),links=k.links.OrderBy(x=>x,StringComparer.Ordinal).ToArray(),events=k.History,nextSequence=k.nextSequence,readThrough=read,readByWorld=worldReads??new[]{read,read}};
        internal void RestoreInto(WorldKnowledge k,SeamlessWorlds s,Side side)
        {
            StrategicSaveData.Require(explored!=null&&observed!=null&&armies!=null&&places!=null&&links!=null&&events!=null&&explored.Length<=s.Map(WorldId.Frontier).Nodes.Count+s.Map(WorldId.StoneValley).Nodes.Count&&observed.Length<=explored.Length&&places.Length<=explored.Length&&armies.Length<=10000&&events.Length<=100000,"Invalid knowledge arrays.");
            foreach(var a in explored.Concat(observed)){StrategicSaveData.Require(a!=null,"Missing address.");a.Validate(s);}
            StrategicSaveData.Require(explored.Select(a=>a.Address).Distinct().Count()==explored.Length&&observed.Select(a=>a.Address).Distinct().Count()==observed.Length,"Duplicate knowledge address.");
            k.explored.UnionWith(explored.Select(a=>a.Address));k.observed.UnionWith(observed.Select(a=>a.Address));
            StrategicSaveData.Require(k.observed.IsSubsetOf(k.explored)&&k.observed.SetEquals(s.Visible(side))&&s.Map(WorldId.Frontier).Nodes.All(n=>k.explored.Contains(new WorldAddress(WorldId.Frontier,n.Id))),"Knowledge/physical sight mismatch.");
            foreach(var a in armies){StrategicSaveData.Require(a!=null&&a.id!=null&&a.composition!=null&&a.composition.Length<2048&&a.side==(int)CrossroadsScenario.Other(side)&&a.count>0&&a.refresh>0&&a.refresh<=s.world.Refresh&&k.explored.Contains(a.Address)&&!k.armies.ContainsKey(a.id),"Invalid historical army snapshot.");k.armies.Add(a.id,a.Copy());}
            foreach(var p in places){StrategicSaveData.Require(p!=null&&p.portalState!=null&&k.explored.Contains(p.Address)&&!k.places.ContainsKey(p.Address)&&p.owner>=-1&&p.owner<=1&&p.food>=-1&&p.food<=24,"Invalid observed place.");k.places.Add(p.Address,p.Copy());}
            StrategicSaveData.Require(links.Distinct().Count()==links.Length&&links.All(id=>SeamlessWorlds.Portals.Any(p=>p.Id==id))&&(links.Length==0||k.Worlds.Contains(WorldId.StoneValley)),"Invalid known link.");k.links.UnionWith(links);
            int last=0;foreach(var e in events){StrategicSaveData.Require(e!=null&&e.sequence==last+1&&e.refresh>=1&&e.refresh<=s.world.Refresh&&(e.world==0||e.world==1)&&e.actor!=null&&e.description!=null&&e.description.Length<=4096&&e.kind!=null&&e.nodes!=null&&e.nodes.Length>0&&e.nodes.All(n=>k.explored.Contains(new WorldAddress((WorldId)e.world,n))),"Invalid filtered observation.");last=e.sequence;k.events.Add(e.Copy());}
            StrategicSaveData.Require(nextSequence==last+1&&readThrough>=0&&readThrough<=last&&readByWorld!=null&&readByWorld.Length==2&&readByWorld.All(r=>r>=0&&r<=last),"Invalid event cursor.");k.nextSequence=nextSequence;
        }
        internal void Write(BinaryWriter w)
        {
            w.Write(explored.Length);foreach(var a in explored)a.Write(w);w.Write(observed.Length);foreach(var a in observed)a.Write(w);
            w.Write(armies.Length);foreach(var a in armies){w.Write(a.id);w.Write(a.side);w.Write(a.world);w.Write(a.node);w.Write(a.refresh);w.Write(a.count);w.Write(a.composition);}
            w.Write(places.Length);foreach(var p in places){w.Write(p.world);w.Write(p.node);w.Write(p.owner);w.Write(p.food);w.Write(p.portalState);}
            w.Write(links.Length);foreach(var id in links)w.Write(id);w.Write(events.Length);foreach(var e in events){w.Write(e.sequence);w.Write(e.refresh);w.Write(e.world);w.Write(e.actor);w.Write(e.kind);w.Write(e.description);w.Write(e.nodes.Length);foreach(int n in e.nodes)w.Write(n);}w.Write(nextSequence);
            // readThrough is persisted but is presentation state, excluded from hashes.
        }
    }
    [Serializable] public sealed class SeamlessSaveData
    {
        public bool production,dense;public StrategicJourney[] journeys;
        public int rules=1,northFood,southFood,beaconOwner,revision;public bool temporary;public string message,westMessage,eastMessage;public string[] opaque;
        public WorldKnowledgeData west,east;
        internal static SeamlessSaveData Capture(SeamlessWorlds s)=>new SeamlessSaveData{rules=s.TravelRules,production=s.ProductionTopology,dense=s.DenseTravel,journeys=s.journeys.Values.OrderBy(j=>j.army,StringComparer.Ordinal).Select(j=>j.Copy()).ToArray(),northFood=s.NorthFood,southFood=s.SouthFood,beaconOwner=s.BeaconOwner.HasValue?(int)s.BeaconOwner.Value:-1,revision=s.Revision,temporary=s.TemporaryRoute,message=s.LastMessage,westMessage=s.westMessage,eastMessage=s.eastMessage,opaque=s.opaque.OrderBy(x=>x,StringComparer.Ordinal).ToArray(),west=WorldKnowledgeData.Capture(s.Knowledge(Side.West),s.ReadThrough(Side.West),new[]{s.ReadThrough(Side.West,WorldId.Frontier),s.ReadThrough(Side.West,WorldId.StoneValley)}),east=WorldKnowledgeData.Capture(s.Knowledge(Side.East),s.ReadThrough(Side.East),new[]{s.ReadThrough(Side.East,WorldId.Frontier),s.ReadThrough(Side.East,WorldId.StoneValley)})};
        internal void RestoreInto(SeamlessWorlds s)
        {
            StrategicSaveData.Require((rules==1||rules==2)&&(!production||rules==2)&&northFood>=0&&northFood<=24&&southFood>=0&&southFood<=24&&beaconOwner>=-1&&beaconOwner<=1&&revision>=0&&message!=null&&message.Length<4096&&westMessage!=null&&eastMessage!=null&&westMessage.Length<4096&&eastMessage.Length<4096&&opaque!=null&&opaque.Length<=100&&west!=null&&east!=null,"Invalid07 rules/state.");
            foreach(var key in opaque){StrategicSaveData.Require(new[]{WorldId.Frontier,WorldId.StoneValley}.Any(id=>s.Map(id).Edges.Any(e=>SeamlessWorlds.EdgeKey(id,e.A,e.B)==key))&&s.opaque.Add(key),"Invalid sight edge.");}
            s.TravelRules=rules;
            if(rules==2){StrategicSaveData.Require(journeys!=null&&journeys.Length<=s.world.Realm.Armies.Count(),"Invalid journeys.");foreach(var j in journeys){
                var f=j==null?null:s.world.Realm.Army(j.army);StrategicSaveData.Require(f!=null&&j.paused!=null&&j.paused.Length<4096&&j.formation!=null&&j.world>=0&&j.world<=1&&j.issuedRefresh>0&&j.issuedRefresh<=s.world.Refresh&&j.route!=null&&j.route.Length>=2&&j.route.Length<=s.Map((WorldId)j.world).Nodes.Count&&j.next>=1&&j.next<=j.route.Length&&j.route.Last()==j.destination&&!s.journeys.ContainsKey(j.army),"Invalid journey state.");
                var map=s.Map((WorldId)j.world);StrategicSaveData.Require(j.route.Distinct().Count()==j.route.Length&&j.route.All(n=>map.Node(n)!=null)&&map.PathCost(j.route)>=0,"Invalid saved route.");
                StrategicSaveData.Require(j.paused!=""||(f.Continues&&(int)f.WorldId==j.world&&j.next<j.route.Length&&j.route[j.next-1]==f.Node),"Invalid active journey cursor.");s.journeys.Add(j.army,j.Copy());}}
            s.NorthFood=northFood;s.SouthFood=southFood;s.BeaconOwner=beaconOwner<0?(Side?)null:(Side)beaconOwner;s.Revision=revision;s.LastMessage=message;s.westMessage=westMessage;s.eastMessage=eastMessage;
            west.RestoreInto(s.Knowledge(Side.West),s,Side.West);east.RestoreInto(s.Knowledge(Side.East),s,Side.East);foreach(var id in new[]{WorldId.Frontier,WorldId.StoneValley}){s.MarkRead(Side.West,west.readByWorld[(int)id],id);s.MarkRead(Side.East,east.readByWorld[(int)id],id);}
        }
        internal void Write(BinaryWriter w,bool knowledge)
        {w.Write(rules);w.Write(northFood);w.Write(southFood);w.Write(beaconOwner);w.Write(revision);w.Write(temporary);w.Write(message);w.Write(westMessage);w.Write(eastMessage);w.Write(opaque.Length);foreach(var e in opaque)w.Write(e);if(rules>=2){w.Write(production);if(dense)w.Write("TravelScale08B");w.Write(journeys.Length);foreach(var j in journeys){w.Write(j.army);w.Write(j.paused);w.Write(j.formation);w.Write(j.world);w.Write(j.destination);w.Write(j.next);w.Write(j.issuedRefresh);w.Write(j.route.Length);foreach(int n in j.route)w.Write(n);}}if(knowledge){west.Write(w);east.Write(w);}}
    }
    public sealed partial class SeamlessWorlds
    {
        private readonly int[,] readCursors=new int[2,2];
        public int ReadThrough(Side side,WorldId? world=null)=>world.HasValue?readCursors[(int)side,(int)world.Value]:Math.Max(readCursors[(int)side,0],readCursors[(int)side,1]);
        public void MarkRead(Side side,int through,WorldId? world=null){through=Math.Max(0,Math.Min(through,Knowledge(side).nextSequence-1));for(int i=0;i<2;i++)if(!world.HasValue||(int)world.Value==i)readCursors[(int)side,i]=Math.Max(readCursors[(int)side,i],through);}
        public string SimulationHash()=>world.CaptureSave().ComputeHash(false);
        public string KnowledgeHash(Side side)
        {using(var stream=new MemoryStream())using(var writer=new BinaryWriter(stream)){WorldKnowledgeData.Capture(Knowledge(side),0).Write(writer);writer.Flush();using(var sha=SHA256.Create())return Convert.ToBase64String(sha.ComputeHash(stream.ToArray()));}}
    }
}

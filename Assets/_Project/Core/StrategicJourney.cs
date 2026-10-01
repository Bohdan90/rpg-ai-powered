using System;
using System.Collections.Generic;
using System.Linq;
namespace RPG.Core
{
    [Serializable] public sealed class StrategicJourney
    {
        public string army,paused="",formation;public int world,destination,next,issuedRefresh;public int[] route;
        public WorldAddress Destination=>new WorldAddress((WorldId)world,destination);
        internal StrategicJourney Copy()=>new StrategicJourney{army=army,paused=paused,formation=formation,world=world,destination=destination,next=next,issuedRefresh=issuedRefresh,route=route.ToArray()};
    }
    public sealed partial class SeamlessWorlds
    {
        internal readonly Dictionary<string,StrategicJourney> journeys=new Dictionary<string,StrategicJourney>(StringComparer.Ordinal);
        internal int TravelRules=3;
        public StrategicJourney Journey(string army)=>army!=null&&journeys.TryGetValue(army,out var j)?j.Copy():null;
        private static string FormationStamp(DuelForce f)=>string.Join("|",f.Formation.LivingMembers.Select(c=>c.CharacterId))+"/"+f.Formation.AssignedCommanderId;
        public StrategicMovePreview PreviewJourney(Side side,WorldAddress destination)
        {
            var p=PreviewMove(side,destination);
            if(p.Reason!=null&&p.Path.Length>1&&p.Cost>world.Realm.Selected(side).Tempo&&p.Reason.StartsWith("Insufficient Tempo",StringComparison.Ordinal))p.Reason=null;
            return p;
        }
        public int ReachableLegs(DuelForce f,int[] route,int next=1)
        {
            int budget=f.Tempo,count=0;var graph=Map(f.WorldId);
            for(int i=next;i<route.Length;i++){int cost=graph.Cost(route[i-1],route[i],f.RealmProvisions==0);if(cost<0||cost>budget)break;budget-=cost;count++;if(Portals.Any(p=>p.Contains(new WorldAddress(f.WorldId,route[i]))))break;}
            return count;
        }
        public bool SetDestination(Side side,WorldAddress destination)
        {
            var p=PreviewJourney(side,destination);if(!p.IsLegal)return false;var f=world.Realm.Selected(side);
            if(TravelRules<2)TravelRules=2;journeys[f.Formation.FormationId]=new StrategicJourney{army=f.Formation.FormationId,world=(int)f.WorldId,destination=destination.Node,route=p.Path.ToArray(),next=1,issuedRefresh=world.Refresh,formation=FormationStamp(f)};
            Revision++;if(TravelRules<3)AdvanceJourney(f);else {LastMessage="Destination queued. Choose Continue Travel; turns never move automatically.";Publish(side);}return true;
        }
        // Pure forecast uses only lawful knowledge. Discovery/contact can interrupt before this budget stop.
        public StrategicMovePreview PreviewContinueTravel(Side side)
        {
            var p=new StrategicMovePreview();var f=world.Realm.Selected(side);
            if(!world.Realm.CanAct(side)||f==null){p.Reason="Not a lawful own activation.";return p;}
            if(!journeys.TryGetValue(f.Formation.FormationId,out var j)){p.Reason="Choose a destination first.";return p;}
            p.Path=new[]{f.Node};
            if(j.paused!=""){p.Reason=j.paused;return p;}
            if(!f.Continues||FormationStamp(f)!=j.formation||f.WorldId!=(WorldId)j.world||j.next<1||j.next>=j.route.Length||j.route[j.next-1]!=f.Node){p.Reason="Formation changed; choose destination again.";return p;}
            var graph=Map(f.WorldId);var path=new List<int>{f.Node};int budget=f.Tempo;
            for(int i=j.next;i<j.route.Length;i++){
                int n=j.route[i],cost=graph.Cost(path.Last(),n,f.RealmProvisions==0);var address=new WorldAddress(f.WorldId,n);
                if(cost<0||Knowledge(side).At(address)==KnowledgeLevel.Unexplored||world.AtEnemyCity(f,n)){if(path.Count==1)p.Reason="Saved route is no longer legal; choose destination again.";break;}
                // Do not inspect hidden occupancy in a preview.
                if(world.Realm.Armies.Any(o=>o!=f&&o.Continues&&o.Address==address&&(o.Formation.Side==side||IsVisibleEnemy(side,o.Formation.FormationId)))&&!(f.WorldId==WorldId.Frontier&&n==world.OwnKeep(side))){if(path.Count==1)p.Reason="Known occupied passage.";break;}
                if(cost>budget)break;budget-=cost;p.Cost+=cost;path.Add(n);
                if(Portals.Any(portal=>portal.Contains(address))||world.Realm.Armies.Any(o=>IsVisibleEnemy(side,o.Formation.FormationId)&&o.WorldId==f.WorldId&&graph.Cost(n,o.Node)>=0))break;
            }
            p.Path=path.ToArray();if(p.Path.Length==1&&p.Reason==null)p.Reason="Insufficient Tempo for the next travel segment.";return p;
        }
        public bool ContinueTravel(Side side)
        {
            if(!PreviewContinueTravel(side).IsLegal)return false;
            AdvanceJourney(world.Realm.Selected(side));return true;
        }
        public bool CancelDestination(Side side,string id)
        {
            if(!world.Realm.CanAct(side)||world.Realm.Army(id)?.Formation.Side!=side||!journeys.Remove(id))return false;
            Revision++;LastMessage="Destination cancelled.";Publish(side);return true;
        }
        internal void PauseJourney(string id,string reason)
        {if(journeys.TryGetValue(id,out var j)&&j.paused!=reason){j.paused=reason;Revision++;}}
        internal void ResumeJourneys(Side side)
        {
            if(TravelRules>=3||!world.Realm.CanAct(side))return;
            foreach(var f in world.Realm.Armies.Where(f=>f.Formation.Side==side).OrderBy(f=>f.Formation.FormationId,StringComparer.Ordinal))
                if(journeys.TryGetValue(f.Formation.FormationId,out var j)&&j.paused=="")AdvanceJourney(f);
        }
        private void AdvanceJourney(DuelForce f)
        {
            var side=f.Formation.Side;var j=journeys[f.Formation.FormationId];var graph=Map(f.WorldId);
            string stop=null;
            if(!f.Continues||FormationStamp(f)!=j.formation||f.WorldId!=(WorldId)j.world||j.next<1||j.next>=j.route.Length||j.route[j.next-1]!=f.Node)stop="Formation changed; choose destination again.";
            else if(world.PendingContact!=null||world.Encounter!=null)stop="Encounter requires a decision.";
            if(stop!=null){PauseJourney(j.army,stop);LastMessage=stop;Publish(side);return;}
            westGroupFloor=west.nextSequence;eastGroupFloor=east.nextSequence;
            while(j.next<j.route.Length){
                var next=new WorldAddress(f.WorldId,j.route[j.next]);int cost=graph.Cost(f.Node,next.Node,f.RealmProvisions==0);
                if(cost<0||Knowledge(side).At(next)==KnowledgeLevel.Unexplored||world.AtEnemyCity(f,next.Node)){stop="Saved route is no longer legal; choose a destination again.";break;}
                if(j.paused!=""){stop=j.paused;break;}
                if(cost>f.Tempo)break;
                if(!TryMovementLeg(f,next,out stop))break;
                j.next++;
                if(stop!=null)break;
                if(Portals.Any(p=>p.Contains(f.Address))){stop="Portal reached: choose Traverse Portal explicitly.";break;}
                if(world.Realm.Armies.Any(o=>IsVisibleEnemy(side,o.Formation.FormationId)&&o.WorldId==f.WorldId&&graph.Cost(f.Node,o.Node)>=0)){stop="Hostile contact: choose the next action.";break;}
            }
            westGroupFloor=eastGroupFloor=int.MaxValue;
            if(stop!=null){PauseJourney(j.army,stop);LastMessage=stop;}
            else if(j.next==j.route.Length){journeys.Remove(j.army);LastMessage="Destination reached: "+j.Destination;}
            else LastMessage="Destination "+j.Destination+" retained · Tempo "+f.Tempo+(TravelRules>=3?" · choose Continue Travel when ready.":" · resumes next own activation.");
            Publish(side);
        }
        // Shared authoritative movement leg: never routed through hidden occupancy.
        private bool TryMovementLeg(DuelForce f,WorldAddress next,out string stop)
        {
            stop=null;var side=f.Formation.Side;var graph=Map(f.WorldId);int cost=graph.Cost(f.Node,next.Node,f.RealmProvisions==0);
            if(next.World!=f.WorldId||cost<0||cost>f.Tempo){stop="Movement leg is no longer legal.";return false;}
            if(!(next.World==WorldId.Frontier&&next.Node==world.OwnKeep(side))&&world.Realm.Armies.Any(o=>o!=f&&o.Continues&&o.Address==next)){stop="Movement paused before an obstructed local passage.";return false;}
            var visible=world.Realm.Armies.Where(o=>IsVisibleEnemy(side,o.Formation.FormationId)).Select(o=>o.Formation.FormationId).ToHashSet();
            f.Tempo-=cost;world.Realm.LowerCeilings(f);Step(f,next);Revision++;world.Realm.CancelAbsentRepairs(side);
            if(world.Realm.Armies.Any(o=>IsVisibleEnemy(side,o.Formation.FormationId)&&!visible.Contains(o.Formation.FormationId)))stop="new hostile observed: destination paused; choose the next action.";
            return true;
        }
    }
}

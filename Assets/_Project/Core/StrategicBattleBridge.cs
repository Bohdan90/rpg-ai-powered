using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace RPG.Core
{
    public sealed class StrategicEncounter
    {
        public PersistentBattle Battle { get; }
        public StrategicActorKind LeadActor { get; }
        public ReadOnlyCollection<StrategicActorKind> Participants { get; }
        public bool PlayerAttacks { get; }
        public int PlayerOrigin { get; }
        public int TargetNode { get; }
        public IReadOnlyDictionary<StrategicActorKind,int> Origins { get; }
        public IReadOnlyDictionary<string,UnitId> UnitIds { get; }
        public IReadOnlyDictionary<StrategicActorKind,RetreatEdge> ApproachEdges { get; }
        internal StrategicEncounter(PersistentBattle battle,StrategicActor lead,StrategicActor[] participants,bool playerAttacks,
            int origin,int target,Dictionary<string,UnitId> unitIds,Dictionary<StrategicActorKind,RetreatEdge> approaches)
        {
            Battle=battle;LeadActor=lead.Kind;Participants=Array.AsReadOnly(participants.Select(a=>a.Kind).ToArray());
            Origins=participants.ToDictionary(a=>a.Kind,a=>a.Node);PlayerAttacks=playerAttacks;PlayerOrigin=origin;TargetNode=target;UnitIds=unitIds;ApproachEdges=approaches;
        }
    }
    public sealed partial class StrategicScenario
    {
        public StrategicEncounter Encounter { get; private set; }
        public string LastBattleSummary { get; private set; }="";
        public PersistenceBattleResolution? LastBattleResolution { get; private set; }
        private void BeginEncounter(StrategicActor lead,bool playerAttacks,int target)
        {
            // One-hop snapshot around the Battle Target, never recursively around joining allies.
            var participants=Actors.Where(a=>a.Active&&(a==lead||(a.OccupiesNode
                &&Graph.Cost(a.Node,target)>=0&&(playerAttacks||a.Tempo>=0)))).ToArray();
            if(!playerAttacks)foreach(var a in participants.Where(a=>a!=lead))a.Tempo=AfterAttackCost(a.Tempo);
            var coalition=new PersistentFormation("encounter-east",Side.East,participants.SelectMany(a=>a.Formation.Members));
            var deployments=new List<PersistentDeployment>();var ids=new Dictionary<string,UnitId>();
            for(int i=0;i<Player.Members.Count;i++)
            {
                var c=Player.Members[i];var id=new UnitId(i+1);ids.Add(c.CharacterId,id);
                deployments.Add(new PersistentDeployment(c.CharacterId,id,new GridPosition(c.Profile.IsArcher?1:3,3+i*2),Facing.East,
                    Tempo<0?RetreatEdge.Unavailable:RetreatEdge.West));
            }
            // Battle-local orientation keeps the primary encounter West/East as in Gate C.
            // Rotate the other strategic approach vectors into that SAME frame; never collapse them all to East.
            var approaches=participants.ToDictionary(a=>a.Kind,a=>a==lead?RetreatEdge.East:ApproachEdge(a.Node,target,lead,playerAttacks));
            var slots=new Dictionary<RetreatEdge,int>();
            foreach(var a in participants)for(int i=0;i<a.Formation.Members.Count;i++)
            {
                var c=a.Formation.Members[i];var id=new UnitId(100+(int)a.Kind*10+i);ids.Add(c.CharacterId,id);
                var edge=approaches[a.Kind];int slot=slots.TryGetValue(edge,out var used)?used:0;slots[edge]=slot+1;
                GridPosition position=edge==RetreatEdge.East?new GridPosition(19+slot/7,2+(slot%7)*2)
                    :edge==RetreatEdge.North?new GridPosition(7+(slot%5)*2,14+slot/5)
                    :edge==RetreatEdge.South?new GridPosition(7+(slot%5)*2,2-slot/5)
                    :new GridPosition(5+slot/7,2+(slot%7)*2);
                Facing facing=edge==RetreatEdge.North?Facing.South:edge==RetreatEdge.South?Facing.North:edge==RetreatEdge.West?Facing.East:Facing.West;
                deployments.Add(new PersistentDeployment(c.CharacterId,id,position,facing,a.Tempo<0?RetreatEdge.Unavailable:edge));
            }
            var battle=PersistentBattle.Start(Player,coalition,deployments,Seed+(uint)++battleNumber,
                SizeExperimentFixture.Board(SizeExperimentMap.Field_23x17_Full_9v9));
            Encounter=new StrategicEncounter(battle,lead,participants,playerAttacks,PlayerNode,target,ids,approaches);
            Log((playerAttacks?"Player attacks ":"Player defends against ")+string.Join(" + ",participants.Select(a=>a.Kind))+". Battle "+battleNumber+"; no Refresh on battle exit.");
        }
        public bool ResolveBattle(BattleState finalState)
        {
            if(Encounter==null||finalState==null||!finalState.Outcome.IsEnded)return false;
            var e=Encounter;
            // Reject a result from another encounter before touching any persistent record.
            if(finalState.InitialSeed!=e.Battle.State.InitialSeed||finalState.Units.Count!=e.Battle.State.Units.Count
                ||finalState.Units.Any(u=>!e.Battle.State.Units.Any(v=>v.Id==u.Id&&v.Profile.Id==u.Profile.Id&&v.Side==u.Side)))return false;
            LastBattleResolution=e.Battle.Resolve(finalState);
            bool playerWithdrew=Withdrew(Player,finalState,e);
            if(playerWithdrew)
            {
                Tempo-=40;PlayerNode=RetreatDestination(e.PlayerOrigin,e.Origins.Values.ToArray(),true,null);
                Log("Player Withdrawal: displaced to "+PlayerNode+", Tempo "+Tempo+" (40 cost/debt).");
            }
            foreach(var kind in e.Participants)
            {
                var a=Actor(kind);a.Formation.RefreshCommanderState();
                if(!a.Formation.LivingMembers.Any()){a.Objective=StrategicObjective.Removed;a.RaidArmed=false;}
                else if(Withdrew(a.Formation,finalState,e))
                {
                    a.Tempo-=40;a.Node=RetreatDestination(e.Origins[kind],new[]{e.PlayerOrigin},false,a);
                    a.Objective=StrategicObjective.Withdraw;a.RaidArmed=false;a.InterceptPending=false;
                    Log(kind+" Withdrawal to "+a.Node+"; objective now exit, Tempo "+a.Tempo+".");
                }
            }
            // Occupation is part of victory, not another movement purchase. No overlap if retreat was trapped.
            if(finalState.Outcome.VictorySide==Side.West&&e.PlayerAttacks&&e.TargetNode!=11&&Occupant(e.TargetNode)==null)
                PlayerNode=e.TargetNode;
            if(finalState.Outcome.VictorySide==Side.East&&!e.PlayerAttacks&&PlayerNode!=e.TargetNode
                &&Actor(e.LeadActor).Active&&Actor(e.LeadActor).Objective!=StrategicObjective.Withdraw)
            {
                var lead=Actor(e.LeadActor);lead.Node=e.TargetNode;
                if(lead.Objective==StrategicObjective.Raid&&lead.Node==(lead.Kind==StrategicActorKind.IncursionA?14:12))lead.RaidArmed=true;
            }
            LastBattleSummary="Battle "+battleNumber+": "+(finalState.Outcome.VictorySide==Side.West?"Victory":"Defeat")+" / "+finalState.Outcome.Reason
                +". HP/Armor, deaths, escapes and XP returned to the same persistent IDs. No recovery on battle exit.";
            Log(LastBattleSummary);Encounter=null;UpdateIntercept();CheckMission();
            if(worldPhase&&Result==StrategicMissionResult.Ongoing)AdvanceWorld();return true;
        }
        private static bool Withdrew(PersistentFormation f,BattleState state,StrategicEncounter e)
        {
            var units=f.Members.Select(c=>state.FindUnit(e.UnitIds[c.CharacterId])).Where(u=>u!=null).ToArray();
            return units.Any(u=>u.Status==UnitStatus.Escaped)&&units.All(u=>!u.IsActive);
        }
        private RetreatEdge ApproachEdge(int node,int target,StrategicActor lead,bool playerAttacks)
        {
            var center=Graph.Node(target);var primary=Graph.Node(playerAttacks?PlayerNode:lead.Node);var other=Graph.Node(node);
            float px=primary.X-center.X,py=primary.Y-center.Y,dx=other.X-center.X,dy=other.Y-center.Y;
            // For a protected-edge encounter whose origins coincide, use the traversed edge direction.
            if(px==0&&py==0){px=-1;py=0;}
            float east=(dx*px+dy*py)*(playerAttacks?-1:1),north=(dx*-py+dy*px)*(playerAttacks?-1:1);
            return Math.Abs(east)>=Math.Abs(north)?(east>=0?RetreatEdge.East:RetreatEdge.West):(north>=0?RetreatEdge.North:RetreatEdge.South);
        }
        public int RetreatDestination(int origin,int[] enemies,bool player,StrategicActor self)
        {
            // Up to two graph hops; intermediate occupation does not invalidate abstract aftermath displacement.
            var candidates=Graph.Nodes.Where(n=>n.Id!=11&&Graph.Hops(origin,n.Id)<=2&&n.Id!=origin
                &&(player||n.Id!=PlayerNode)&&!Actors.Any(a=>a!=self&&a.OccupiesNode&&a.Node==n.Id)
                &&!enemies.Contains(n.Id));
            return candidates.OrderByDescending(n=>enemies.Min(e=>Graph.Hops(n.Id,e)))
                .ThenBy(n=>Graph.PathCost(Graph.Path(origin,n.Id))).ThenBy(n=>n.Id).Select(n=>n.Id).DefaultIfEmpty(origin).First();
        }
        public string DebugState()
        {
            return Refresh+"|"+PlayerNode+"|"+Tempo+"|"+Provisions+"|"+Hungry+"|"+WaystationFood+"|"+Waystation+"|"+Village+"|"+PortalInvestigated+"|"+Result
                +"|"+worldPhase+"|"+actorCursor+"|"+battleNumber+"|"+Seed+"|"+(Encounter==null?"none":BattleStateHash.Compute(Encounter.Battle.State))
                +"|"+string.Join(";",Actors.Select(a=>a.Kind+":"+a.Node+":"+a.Tempo+":"+a.Objective+":"+a.RaidArmed+":"+a.InterceptPending+":"+a.PatrolIndex))
                +"|"+string.Join(";",Player.Members.Concat(Actors.SelectMany(a=>a.Formation.Members)).Select(c=>c.CharacterId+":"+c.Status+":"+c.Hp+":"+c.Armor+":"
                    +c.PersonalXp.ToString(System.Globalization.CultureInfo.InvariantCulture)+":"+c.CommandXp.ToString(System.Globalization.CultureInfo.InvariantCulture)+":"+c.CommandLevel+":"+c.FieldRecoveryRemainderHundredths));
        }
    }
}

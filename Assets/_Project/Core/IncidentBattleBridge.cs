using System;
using System.Linq;
using System.Collections.Generic;

namespace RPG.Core
{
    public sealed class IncidentContact
    {
        public DuelForce Lead {get;} public DuelForce Target {get;}
        internal DuelForce[] Attackers {get;} internal DuelForce[] Defenders {get;}
        internal IncidentContact(DuelForce lead,DuelForce target,DuelForce[] attackers,DuelForce[] defenders){Lead=lead;Target=target;Attackers=attackers;Defenders=defenders;}
    }
    public sealed partial class DuelEncounter
    {
        public IReadOnlyList<DuelForce> Participants { get; }
        public IReadOnlyDictionary<string,Side> TacticalSides { get; }
        public IReadOnlyDictionary<string,int> Origins { get; }
        public DuelForce Lead { get; }
        public DuelForce Target { get; }
        public int Number { get; }
        public bool HasRaiders { get; }
        public Side AiSide { get; }
        internal DuelEncounter(CrossroadsScenario world,DuelForce lead,DuelForce target,DuelForce[] attackers,DuelForce[] defenders,int number,uint seed)
        {
            Lead=lead;Target=target;Number=number;Attacker=Side.West;
            Participants=Array.AsReadOnly(attackers.Concat(defenders).ToArray());
            TacticalSides=Participants.ToDictionary(f=>f.Formation.FormationId,f=>attackers.Contains(f)?Side.West:Side.East);
            Origins=Participants.ToDictionary(f=>f.Formation.FormationId,f=>f.Node);
            HasRaiders=Participants.Any(f=>world.Incident.Raiders.Any(r=>r.Force==f));
            AiSide=world.Incident.Raiders.Any(r=>r.Force==lead)?Side.West:Side.East;
            WestOrigin=lead.Node;EastOrigin=target.Node;
            var board=SizeExperimentFixture.Board(SizeExperimentMap.Field_23x17_Full_9v9);
            var deployments=new List<PersistentDeployment>();var ids=new Dictionary<UnitId,string>();var occupied=new HashSet<GridPosition>();
            var sectorCount=new Dictionary<RetreatEdge,int>();int id=1;
            foreach(var f in Participants)
            {
                var node=world.Graph.Node(f.Node);var center=world.Graph.Node(target.Node);
                var edge=Direction(node.X-center.X,node.Y-center.Y);
                if(f==target){var a=world.Graph.Node(lead.Node);edge=Opposite(Direction(a.X-center.X,a.Y-center.Y));}
                int slot=sectorCount.TryGetValue(edge,out int count)?count:0;
                foreach(var c in f.Formation.LivingMembers)
                {
                    GridPosition position;
                    do{int row=slot%9,depth=slot/9+1;slot++;position=edge==RetreatEdge.West?new GridPosition(depth,4+row):edge==RetreatEdge.East?new GridPosition(22-depth,4+row):edge==RetreatEdge.North?new GridPosition(7+row,16-depth):new GridPosition(7+row,depth);}
                    while(!board.IsWalkable(position)||occupied.Contains(position));
                    occupied.Add(position);var uid=new UnitId(id++);ids.Add(uid,c.CharacterId);
                    var facing=edge==RetreatEdge.West?Facing.East:edge==RetreatEdge.East?Facing.West:edge==RetreatEdge.North?Facing.South:Facing.North;
                    deployments.Add(new PersistentDeployment(c.CharacterId,uid,position,facing,f.Tempo<0?RetreatEdge.Unavailable:edge));
                }
                sectorCount[edge]=slot;
            }
            Ids=ids;Battle=PersistentBattle.Start(attackers.Select(f=>f.Formation).ToArray(),defenders.Select(f=>f.Formation).ToArray(),deployments,seed,board);
        }
        private static RetreatEdge Direction(float dx,float dy)=>Math.Abs(dx)>=Math.Abs(dy)?(dx<0?RetreatEdge.West:RetreatEdge.East):(dy<0?RetreatEdge.South:RetreatEdge.North);
        private static RetreatEdge Opposite(RetreatEdge e)=>e==RetreatEdge.West?RetreatEdge.East:e==RetreatEdge.East?RetreatEdge.West:e==RetreatEdge.North?RetreatEdge.South:RetreatEdge.North;
    }
    public sealed partial class CrossroadsScenario
    {
        public IncidentContact PendingContact {get;private set;}
        private bool Allied(DuelForce a,DuelForce b)=>a==b||(Incident.Raiders.Any(r=>r.Force==a)&&Incident.Raiders.Any(r=>r.Force==b));
        public bool CanAttackNode(Side side,int node)=>Incident!=null&&CanAct(side)&&Force(side).Tempo>=0&&Occupants.Any(f=>f!=Force(side)&&f.Node==node&&Graph.Cost(Force(side).Node,node)>=0);
        public bool AttackNode(Side side,int node)
        {
            if(!CanAttackNode(side,node))return false;
            StartIncidentBattle(Force(side),Occupants.Single(f=>f.Node==node));return true;
        }
        private void StartIncidentBattle(DuelForce lead,DuelForce target)
        {
            var attackers=Occupants.Where(f=>Allied(f,lead)&&f.Tempo>=0&&Graph.Cost(f.Node,target.Node)>=0).OrderBy(f=>f.Formation.FormationId,StringComparer.Ordinal).ToArray();
            var defenders=Occupants.Where(f=>Allied(f,target)&&(f==target||Graph.Cost(f.Node,target.Node)>=0)).OrderBy(f=>f.Formation.FormationId,StringComparer.Ordinal).ToArray();
            foreach(var f in attackers)f.Tempo=StrategicScenario.AfterAttackCost(f.Tempo);
            if(target==West||target==East){PendingContact=new IncidentContact(lead,target,attackers,defenders);Trace("Hostile Contact: "+target.Formation.FormationId+" chooses Fight / Withdrawal before deployment");return;}
            CreateIncidentEncounter(lead,target,attackers,defenders);
        }
        private void CreateIncidentEncounter(DuelForce lead,DuelForce target,DuelForce[] attackers,DuelForce[] defenders)
        {
            Encounter=new DuelEncounter(this,lead,target,attackers,defenders,++battleNumber,Seed+(uint)battleNumber);
            Trace("battle#"+battleNumber+" lead="+lead.Formation.FormationId+" target="+target.Formation.FormationId+" participants="+string.Join(",",Encounter.Participants.Select(f=>f.Formation.FormationId)));
        }
        public bool RespondToContact(Side human,bool withdraw)
        {
            var p=PendingContact;if(p==null||p.Target!=Force(human)||Encounter!=null)return false;
            if(withdraw)
            {
                if(p.Target.Tempo<0)return false;
                int origin=p.Target.Node;int destination=IncidentRetreatDestination(p.Target,origin,p.Attackers.Select(f=>f.Node).ToArray());
                p.Target.Tempo-=40;p.Target.Node=destination;if(destination!=origin)BreakHold(p.Target,"pre-battle Withdrawal displacement");
                Trace(p.Target.Formation.FormationId+" pre-battle Withdrawal "+origin+" -> "+destination+" Tempo="+p.Target.Tempo);
            }
            else CreateIncidentEncounter(p.Lead,p.Target,p.Attackers,p.Defenders);
            PendingContact=null;return true;
        }
        private int IncidentRetreatDestination(DuelForce f,int origin,int[] enemies)
        {
            return Graph.Nodes.Where(n=>n.Id!=origin&&Graph.Hops(origin,n.Id)<=2&&!Occupants.Any(o=>o!=f&&o.Node==n.Id))
                .OrderByDescending(n=>enemies.Min(e=>Graph.Hops(n.Id,e))).ThenBy(n=>Graph.PathCost(Graph.Path(origin,n.Id))).ThenBy(n=>n.Id).Select(n=>n.Id).DefaultIfEmpty(origin).First();
        }
        private bool WithdrawIncident(Side side)
        {
            var f=Force(side);var enemies=Occupants.Where(o=>o!=f&&Graph.Cost(f.Node,o.Node)>=0).Select(o=>o.Node).ToArray();
            if(enemies.Length==0)return false;BreakHold(f,"Withdrawal displacement");f.Node=IncidentRetreatDestination(f,f.Node,enemies);f.Tempo-=40;Trace(f.Formation.FormationId+" Withdrawal node="+f.Node+" Tempo="+f.Tempo);return true;
        }
        public bool CanWithdrawIncident(Side side)=>Incident!=null&&CanAct(side)&&Force(side).Tempo>=0&&Occupants.Any(o=>o!=Force(side)&&Graph.Cost(Force(side).Node,o.Node)>=0);
        public bool WithdrawFromContact(Side side)=>CanWithdrawIncident(side)&&WithdrawIncident(side);
        private bool ResolveIncidentBattle(BattleState result)
        {
            var e=Encounter;if(e==null||result==null||!result.Outcome.IsEnded||e.Number<=Incident.AppliedBattle)return false;
            var start=e.Battle.State;if(result.InitialSeed!=start.InitialSeed||result.Units.Count!=start.Units.Count||result.Units.Any(u=>!start.Units.Any(v=>v.Id==u.Id&&v.Profile.Id==u.Profile.Id&&v.Side==u.Side)))return false;
            e.Battle.Resolve(result);var withdrawn=new HashSet<DuelForce>();
            foreach(var f in e.Participants.OrderBy(f=>f.Formation.FormationId,StringComparer.Ordinal))
            {
                var units=result.Units.Where(u=>f.Formation.Members.Any(c=>c.CharacterId==e.Ids[u.Id])).ToArray();
                var actor=Incident.Raiders.FirstOrDefault(r=>r.Force==f);
                if(!f.Continues){if(actor!=null){actor.State=RaiderState.Defeated;actor.ArmedRefresh=0;}BreakHold(f,"formation destroyed");continue;}
                if(units.Any(u=>u.Status==UnitStatus.Escaped)&&units.All(u=>!u.IsActive))
                {
                    var enemies=e.Participants.Where(o=>e.TacticalSides[o.Formation.FormationId]!=e.TacticalSides[f.Formation.FormationId]).Select(o=>e.Origins[o.Formation.FormationId]).ToArray();
                    int origin=e.Origins[f.Formation.FormationId];f.Node=IncidentRetreatDestination(f,origin,enemies);f.Tempo-=40;withdrawn.Add(f);
                    if(f.Node!=origin)BreakHold(f,"Withdrawal displaced holder");
                    if(actor!=null){actor.State=Refresh>=6?RaiderState.StrandedReturn:RaiderState.Returning;actor.ArmedRefresh=0;}
                    Trace(f.Formation.FormationId+" Withdrawal "+origin+" -> "+f.Node+" Tempo="+f.Tempo);
                }
            }
            int target=e.Origins[e.Target.Formation.FormationId];
            if(result.Outcome.VictorySide==Side.West&&e.Lead.Continues&&!withdrawn.Contains(e.Lead)&&!Occupants.Any(o=>o.Node==target))
            {BreakHold(e.Lead,"lead advance left site");e.Lead.Node=target;}
            foreach(var r in Incident.Raiders)RaiderArrival(r);
            Incident.AppliedBattle=e.Number;Encounter=null;Trace("battle#"+e.Number+" applied once; no recovery; winner tactical="+result.Outcome.VictorySide);
            // Finish the end transaction even if an NPC battle eliminated a human mid-world-phase.
            if(!West.Continues||!East.Continues)
            {
                Winner=!West.Continues?Side.East:Side.West;
                Incident.Cursor=Incident.ActorOrder.Length;EvaluateHold();CompleteRefresh();
                Winner=!West.Continues?Side.East:Side.West;HandoffPending=false;
            }
            return true;
        }
    }
}

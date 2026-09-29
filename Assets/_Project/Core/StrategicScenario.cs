using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace RPG.Core
{
    public enum StrategicActorKind { HardGuard, AreaGuard, Patrol, IncursionA, IncursionB }
    public enum StrategicObjective { Guard, Patrol, Raid, Withdraw, Removed, Dormant, Exited }
    public enum StrategicSiteCondition { Intact, Ravaged }
    public enum StrategicMissionResult { Ongoing, CouncilAssistanceRequested, VillageRavaged, FormationLost }
    public sealed class StrategicActor
    {
        public StrategicActorKind Kind { get; }
        public PersistentFormation Formation { get; private set; }
        internal void RestoreFormation(PersistentFormation formation) { Formation=formation; }
        public int Node { get; internal set; }
        public int Tempo { get; internal set; }=100;
        public StrategicObjective Objective { get; internal set; }
        public bool RaidArmed { get; internal set; }
        public bool InterceptPending { get; internal set; }
        internal int PatrolIndex;
        public bool Active => Objective!=StrategicObjective.Removed&&Objective!=StrategicObjective.Exited&&Objective!=StrategicObjective.Dormant;
        public bool OccupiesNode => Active&&!(Kind==StrategicActorKind.HardGuard&&Objective==StrategicObjective.Guard);
        internal StrategicActor(StrategicActorKind kind,int node,StrategicObjective objective,UnitProfile[] profiles)
        {Kind=kind;Node=node;Objective=objective;Formation=new PersistentFormation(kind.ToString(),Side.East,profiles.Select((p,i)=>new PersistentCharacter(kind+"-"+(i+1),p)));}
    }
    public sealed class StrategicMovePreview
    {
        public int[] Path { get; internal set; }=Array.Empty<int>();
        public int Cost { get; internal set; }
        public string Reason { get; internal set; }
        public bool IsLegal => Reason==null;
        public StrategicActorKind? Encounter { get; internal set; }
    }
    // Mutable session with validated commands, matching the existing persistence-slice ownership.
    // UI cannot set rule state. Queries and rejected commands do not write anything.
    public sealed partial class StrategicScenario
    {
        public StrategicGraph Graph => StrategicGraph.Mission01;
        public PersistentFormation Player { get; }
        public ReadOnlyCollection<StrategicActor> Actors { get; }
        public int PlayerNode { get; internal set; }=1;
        public int Tempo { get; internal set; }=100;
        public int Refresh { get; private set; }=1;
        public int Provisions { get; internal set; }=36;
        public const int MaxProvisions=36;
        public bool Hungry { get; private set; }
        public int Consumption => Player.LivingMembers.Count();
        public int WaystationFood { get; private set; }=12;
        public StrategicSiteCondition Waystation { get; private set; }
        public StrategicSiteCondition Village { get; private set; }
        public bool PortalInvestigated { get; internal set; }
        public bool BridgeGuardDefeated => Actor(StrategicActorKind.HardGuard).Objective!=StrategicObjective.Guard;
        public bool PortalAccessOpen => Actor(StrategicActorKind.AreaGuard).Objective!=StrategicObjective.Guard;
        public bool VillageThreatResolved => Actor(StrategicActorKind.IncursionB).Objective==StrategicObjective.Removed
            ||Actor(StrategicActorKind.IncursionB).Objective==StrategicObjective.Withdraw||Actor(StrategicActorKind.IncursionB).Objective==StrategicObjective.Exited;
        public StrategicMissionResult Result { get; private set; }
        public bool IsPlayerActivation => !worldPhase && Encounter==null && Result==StrategicMissionResult.Ongoing;
        private bool worldPhase;
        private int actorCursor;
        private readonly List<string> events=new List<string>();
        public IReadOnlyList<string> Events => events.AsReadOnly();
        public uint Seed { get; }
        private int battleNumber;
        public StrategicScenario(uint seed=20260929)
        {
            Seed=seed;var hw=UnitProfile.HumanWarriorTI;var ha=UnitProfile.HumanArcherTI;var ew=UnitProfile.ElfWarriorTI;
            Player=new PersistentFormation("baron",Side.West,new[]{hw,hw,hw,hw,ha,ha}.Select((p,i)=>new PersistentCharacter("baron-"+(i+1),p,i==0)));
            Actors=Array.AsReadOnly(new[]{new StrategicActor(StrategicActorKind.HardGuard,8,StrategicObjective.Guard,new[]{hw,hw,ew}),
                new StrategicActor(StrategicActorKind.AreaGuard,18,StrategicObjective.Guard,new[]{hw,hw,ew,ha}),
                new StrategicActor(StrategicActorKind.Patrol,6,StrategicObjective.Patrol,new[]{hw,ew,ew}),
                new StrategicActor(StrategicActorKind.IncursionA,17,StrategicObjective.Raid,new[]{hw,ew,ew}),
                // Mission-local tuning after the South mouse run: two prior fights stripped the front line's Armor.
                new StrategicActor(StrategicActorKind.IncursionB,11,StrategicObjective.Dormant,new[]{hw,ew,ha})});
            Log("Scout reports: Incursion A moving toward Frontier Waystation. Patrol at North Pass heading East Ridge. Recurring Portal activity threatens the domain.");
        }
        public StrategicActor Actor(StrategicActorKind kind)=>Actors.Single(a=>a.Kind==kind);
        private void Log(string message){events.Add("Refresh "+Refresh+": "+message);}
        public static int AfterAttackCost(int tempo)
        {if(tempo<0)throw new InvalidOperationException("Negative Tempo forbids attack.");return Math.Max(0,tempo-50);}
        private bool InGuardArea(int node)=>node==9||node==10||node==17;
        private StrategicActor Occupant(int node)=>Actors.FirstOrDefault(a=>a.OccupiesNode&&a.Node==node);
        public StrategicMovePreview PreviewMove(int destination)
        {
            var p=new StrategicMovePreview();
            if(!IsPlayerActivation){p.Reason="No player strategic activation.";return p;}
            if(destination==11||Graph.Node(destination)==null){p.Reason="Portal is interacted with from node 10; destination unavailable.";return p;}
            // Exclude occupied intermediate nodes, but show an occupied destination as a contact blocker.
            p.Path=Graph.Path(PlayerNode,destination,n=>n!=11&&(n==destination||Occupant(n)==null),Hungry);
            if(p.Path.Length<2){p.Reason="No movement path.";return p;}
            for(int i=1;i<p.Path.Length;i++)
            {
                int at=p.Path[i];p.Cost+=Graph.Cost(p.Path[i-1],at,Hungry);
                if(!BridgeGuardDefeated&&((p.Path[i-1]==3&&at==8)||(p.Path[i-1]==8&&at==3)))
                {p.Path=p.Path.Take(i+1).ToArray();p.Encounter=StrategicActorKind.HardGuard;break;}
                if(Occupant(at)!=null){p.Reason="Hostile formation occupies "+Graph.Node(at).Name+". Move adjacent and Attack.";return p;}
            }
            if(p.Cost>Tempo)p.Reason="Insufficient Tempo: needs "+p.Cost+", available "+Tempo+".";
            return p;
        }
        public bool Move(int destination)
        {
            var p=PreviewMove(destination);if(!p.IsLegal)return false;
            Tempo-=p.Cost;
            if(p.Encounter.HasValue)
            {
                PlayerNode=p.Path[p.Path.Length-2];
                Tempo=AfterAttackCost(Tempo);BeginEncounter(Actor(p.Encounter.Value),true,p.Path.Last());
            }
            else {PlayerNode=p.Path.Last();Log("Player moved to "+Graph.Node(PlayerNode).Name+" ("+p.Cost+" Tempo).");}
            UpdateIntercept();CheckMission();return true;
        }
        public bool CanAttack(StrategicActorKind kind)
        {var a=Actor(kind);return IsPlayerActivation&&Tempo>=0&&a.OccupiesNode&&Graph.Cost(PlayerNode,a.Node)>=0;}
        public bool Attack(StrategicActorKind kind)
        {if(!CanAttack(kind))return false;Tempo=AfterAttackCost(Tempo);BeginEncounter(Actor(kind),true,Actor(kind).Node);return true;}
        public bool CanInteractPortal => IsPlayerActivation&&PlayerNode==10&&PortalAccessOpen&&!PortalInvestigated&&Tempo>=5;
        public bool InteractPortal()
        {if(!CanInteractPortal)return false;Tempo-=5;PortalInvestigated=true;Log("Portal investigated: repeated hostile emergence continues. Expand the cordon; request Council portal specialists and military reinforcements at Keep.");EndActivation();return true;}
        private void UpdateIntercept()
        {var a=Actor(StrategicActorKind.AreaGuard);a.InterceptPending=a.Active&&a.Objective==StrategicObjective.Guard&&InGuardArea(PlayerNode);}
        public bool EndActivation()
        {if(!IsPlayerActivation)return false;worldPhase=true;actorCursor=0;AdvanceWorld();return true;}
        private void AdvanceWorld()
        {
            var order=new[]{StrategicActorKind.AreaGuard,StrategicActorKind.Patrol,StrategicActorKind.IncursionA,StrategicActorKind.IncursionB};
            while(worldPhase&&Encounter==null&&Result==StrategicMissionResult.Ongoing&&actorCursor<order.Length)
            {var a=Actor(order[actorCursor++]);if(a.Active)Act(a);}
            if(Encounter!=null||Result!=StrategicMissionResult.Ongoing)return;
            // A routed edge guard is no longer a stationary guard; surviving evacuees leave physically.
            var routedGuard=Actor(StrategicActorKind.HardGuard);
            if(routedGuard.Objective==StrategicObjective.Withdraw)Act(routedGuard);
            int used=Consumption;Provisions=Math.Max(0,Provisions-used);int supplied=0;
            if(PlayerNode==1)supplied=Math.Min(6,MaxProvisions-Provisions);
            if(PlayerNode==14&&Waystation==StrategicSiteCondition.Intact)
            {supplied=Math.Min(Math.Min(6,WaystationFood),MaxProvisions-Provisions);WaystationFood-=supplied;}
            Provisions+=supplied;Log("Supply: consumed "+used+", replenished "+supplied+"; "+Provisions+"/36.");
            var hpBefore=Player.LivingMembers.ToDictionary(c=>c.CharacterId,c=>c.Hp);
            Player.ApplyOneFieldStrategicRefresh();foreach(var a in Actors.Where(a=>a.Active))a.Formation.ApplyOneFieldStrategicRefresh();
            Log("One field Strategic Refresh: +15% Max HP, Armor unchanged. "+string.Join(", ",Player.LivingMembers.Where(c=>c.Hp!=hpBefore[c.CharacterId]).Select(c=>c.CharacterId+" "+hpBefore[c.CharacterId]+"→"+c.Hp+" HP")));
            Refresh++;Tempo=100+Math.Min(0,Tempo);foreach(var a in Actors)a.Tempo=100+Math.Min(0,a.Tempo);
            Hungry=Provisions==0;worldPhase=false;
            if(Refresh==4){var b=Actor(StrategicActorKind.IncursionB);b.Objective=StrategicObjective.Raid;Log("Local scouts: Incursion B emerged at Portal, moving toward Riverside Village. It must hold Village for a further activation to Ravage it.");}
            CheckMission();
        }
        private void Act(StrategicActor a)
        {
            if(a.Objective==StrategicObjective.Withdraw){Travel(a,17,false);if(a.Node==17){a.Objective=StrategicObjective.Exited;Log(a.Kind+" reached exit 17 and left.");}return;}
            if(a.Kind==StrategicActorKind.AreaGuard)
            {UpdateIntercept();if(a.InterceptPending)Travel(a,PlayerNode,true);else Travel(a,18,false);return;}
            if(a.Kind==StrategicActorKind.Patrol)
            {int[] cycle={6,7,6,5};int next=(a.PatrolIndex+1)%cycle.Length;Travel(a,cycle[next],true);if(a.Node==cycle[next])a.PatrolIndex=next;return;}
            int target=a.Kind==StrategicActorKind.IncursionA?14:12;
            if(a.RaidArmed&&a.Node==target)
            {
                a.RaidArmed=false;
                if(target==14){Waystation=StrategicSiteCondition.Ravaged;Log("Waystation Ravaged: supply disabled, remaining Food unavailable.");a.Objective=StrategicObjective.Withdraw;Travel(a,17,false);if(a.Node==17)a.Objective=StrategicObjective.Exited;}
                else {Village=StrategicSiteCondition.Ravaged;Result=StrategicMissionResult.VillageRavaged;Log("Mission defeat: Riverside Village Ravaged.");}
                return;
            }
            Travel(a,target,true);
            if(Encounter==null&&a.Node==target){a.RaidArmed=true;Log(a.Kind+" controls "+Graph.Node(target).Name+"; RAID ARMED for next actor activation.");}
        }
        private void Travel(StrategicActor a,int target,bool contact)
        {
            // Authored intent determines the path; no omniscient reroute around hidden player positions.
            var path=Graph.Path(a.Node,target,n=>n==target||!Actors.Any(other=>other!=a&&other.OccupiesNode&&other.Node==n));
            for(int i=1;i<path.Length;i++)
            {
                int next=path[i];
                if(next==PlayerNode)
                {if(contact&&a.Tempo>=0){a.Tempo=AfterAttackCost(a.Tempo);BeginEncounter(a,false,PlayerNode);}return;}
                if(Actors.Any(other=>other!=a&&other.OccupiesNode&&other.Node==next))return;
                int cost=Graph.Cost(a.Node,next);if(cost>a.Tempo)return;
                a.Tempo-=cost;a.Node=next;Log(a.Kind+" moved to "+Graph.Node(next).Name+".");
            }
        }
        private void CheckMission()
        {
            if(!Player.LivingMembers.Any()){Result=StrategicMissionResult.FormationLost;return;}
            if(Result==StrategicMissionResult.Ongoing&&PortalInvestigated&&VillageThreatResolved&&PlayerNode==1&&Encounter==null)
            {Result=StrategicMissionResult.CouncilAssistanceRequested;Log("Council assistance requested. Portal remains active; local containment succeeded.");}
        }
    }
}

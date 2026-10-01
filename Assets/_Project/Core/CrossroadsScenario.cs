using System;
using System.Collections.Generic;
using System.Linq;

namespace RPG.Core
{
    // Document 47 prototype adapter. One persistent human-controlled formation per side.
    public sealed class DuelForce
    {
        public PersistentFormation Formation { get; internal set; }
        public int Node { get; internal set; }
        public WorldId WorldId { get; internal set; }=WorldId.Frontier;
        public WorldAddress Address => new WorldAddress(WorldId,Node);
        public int Tempo { get; internal set; }=100;
        public int Provisions { get; internal set; }=30;
        public decimal RealmProvisions { get; internal set; }
        public int Pressure { get; internal set; }
        public decimal Gold { get; internal set; }
        public decimal KeepFood { get; internal set; }
        public int NextRecruit { get; internal set; }=1;
        public UnitProfileId? PendingRecruit { get; internal set; }
        public string PendingRecruitId { get; internal set; }="";
        public int LastRecruitRefresh { get; internal set; }
        public int Capacity => Formation.Commanderless||Formation.Commander==null?0:32+6*(int)Formation.Commander.CommandRank;
        public int UsedCapacity => Formation.LivingMembers.Where(c=>!c.IsCommander).Sum(c=>c.Profile.IsElf?9:6);
        public int FreeCapacity => Math.Max(0,Capacity-UsedCapacity);
        public bool Hungry => Provisions==0;
        public int Consumption => Formation.LivingMembers.Count();
        public bool Continues => Consumption>0;
        internal DuelForce(Side side)
        {
            Node=side==Side.West?1:13;
            var profiles=new[]{UnitProfile.HumanWarriorTI,UnitProfile.HumanWarriorTI,UnitProfile.HumanWarriorTI,UnitProfile.HumanWarriorTI,UnitProfile.HumanArcherTI,UnitProfile.HumanArcherTI};
            Formation=new PersistentFormation("duel-"+side,side,profiles.Select((p,i)=>new PersistentCharacter("duel-"+side+"-"+(i+1),p,i==0)));
        }
    }
    public sealed partial class DuelEncounter
    {
        public Side Attacker { get; }
        public int WestOrigin { get; }
        public int EastOrigin { get; }
        public PersistentBattle Battle { get; }
        public IReadOnlyDictionary<UnitId,string> Ids { get; }
        internal DuelEncounter(Side attacker,DuelForce west,DuelForce east,uint seed)
        {
            Attacker=attacker;WestOrigin=west.Node;EastOrigin=east.Node;
            var deployments=new List<PersistentDeployment>();var ids=new Dictionary<UnitId,string>();
            foreach(var f in new[]{west,east})
            {
                bool w=f.Formation.Side==Side.West;int index=0;
                foreach(var c in f.Formation.LivingMembers)
                {
                    var id=new UnitId((w?1:101)+index);ids.Add(id,c.CharacterId);
                    deployments.Add(new PersistentDeployment(c.CharacterId,id,new GridPosition(w?(c.Profile.IsArcher?1:3):(c.Profile.IsArcher?21:19),index*2),w?Facing.East:Facing.West,
                        f.Tempo<0?RetreatEdge.Unavailable:w?RetreatEdge.West:RetreatEdge.East));index++;
                }
            }
            Ids=ids;Battle=PersistentBattle.Start(west.Formation,east.Formation,deployments,seed,SizeExperimentFixture.Board(SizeExperimentMap.Field_23x17_Full_9v9));
        }
    }
    public sealed partial class CrossroadsScenario
    {
        public static readonly StrategicGraph Map=new StrategicGraph(new[]{
            new StrategicNode(1,"West Keep",0,2),new StrategicNode(2,"West Road",1,2),new StrategicNode(3,"West Fork",2,2),
            new StrategicNode(4,"NW Ridge",3,4),new StrategicNode(5,"SW Ford",3,0),new StrategicNode(6,"North Mine",4,4),
            new StrategicNode(7,"Central Beacon",4,2),new StrategicNode(8,"South Waystation",4,0),new StrategicNode(9,"NE Ridge",5,4),
            new StrategicNode(10,"SE Ford",5,0),new StrategicNode(11,"East Fork",6,2),new StrategicNode(12,"East Road",7,2),new StrategicNode(13,"East Keep",8,2)},
            new[]{new StrategicEdge(1,2,20),new StrategicEdge(2,3,20),new StrategicEdge(3,4,20),new StrategicEdge(3,5,20),new StrategicEdge(3,7,35),
                new StrategicEdge(4,6,20),new StrategicEdge(5,8,20),new StrategicEdge(6,7,25),new StrategicEdge(8,7,25),new StrategicEdge(6,9,20),
                new StrategicEdge(8,10,20),new StrategicEdge(7,11,35),new StrategicEdge(9,11,20),new StrategicEdge(10,11,20),new StrategicEdge(11,12,20),new StrategicEdge(12,13,20)});
        public const int MaxProvisions=30, PressureTarget=8;
        public RealmOperations Realm { get; internal set; }
        public SeamlessWorlds Seamless { get; internal set; }
        public StrategicGraph GraphFor(WorldId id)=>Seamless==null?Graph:SeamlessWorlds.MapFor(id);
        public bool AtOwnCity(DuelForce f)=>f.WorldId==WorldId.Frontier&&f.Node==OwnKeep(f.Formation.Side);
        public bool AtEnemyCity(DuelForce f,int node)=>f.WorldId==WorldId.Frontier&&node==OwnKeep(Other(f.Formation.Side));
        public int ContactCost(DuelForce a,DuelForce b)=>a.WorldId==b.WorldId?GraphFor(a.WorldId).Cost(a.Node,b.Node):-1;
        public CityFoundations Foundations { get; internal set; }
        public StrategicGraph Graph => Seamless!=null?SeamlessWorlds.Frontier:Foundations!=null?CityFoundations.Map:Incident==null?Map:IncidentState.Map;
        public int TargetPressure => Foundations!=null?24:Incident==null?PressureTarget:16;
        public IncidentState Incident { get; private set; }
        public IEnumerable<DuelForce> AllForces => Realm!=null?Realm.Armies:new[]{West,East}.Concat(Incident==null?Enumerable.Empty<DuelForce>():Incident.Raiders.Select(r=>r.Force));
        public DuelForce West { get; private set; }=new DuelForce(Side.West);
        public DuelForce East { get; private set; }=new DuelForce(Side.East);
        public Side StartingSide { get; }
        public Side ActiveSide { get; private set; }
        public int Refresh { get; private set; }=1;
        public int CompletedActivations { get; private set; }
        public bool HandoffPending { get; private set; }
        public Side? Winner { get; private set; }
        public bool IsDraw=>Realm!=null?!Realm.HasMilitary(Side.West)&&!Realm.HasMilitary(Side.East):!West.Continues&&!East.Continues;
        public DuelEncounter Encounter { get; private set; }
        public uint Seed { get; }
        public bool Economy { get; }
        public int WaystationFood { get; private set; }
        private int battleNumber;
        private readonly Side?[] owners=new Side?[3];
        private readonly List<string> events=new List<string>();
        public IReadOnlyList<string> Events=>events.AsReadOnly();
        public bool CanSave=>Encounter==null&&PendingContact==null;
        public CrossroadsScenario(Side startingSide=Side.West,uint seed=20260929,bool economy=false,bool incident=false,bool incidentsEnabled=true,bool foundations=false,bool combined=false,CombatPreset westPreset=CombatPreset.Fire,CombatPreset eastPreset=CombatPreset.Ice,bool realm=false)
        {if(!ValidSide(startingSide))throw new ArgumentOutOfRangeException(nameof(startingSide));StartingSide=ActiveSide=startingSide;Seed=seed;Economy=economy||incident||foundations||combined||realm;if(Economy){West.Gold=East.Gold=300;West.KeepFood=East.KeepFood=36;WaystationFood=24;}if(foundations||combined||realm){if(incident)throw new ArgumentException("05 and 04 are separate scenarios.");Foundations=new CityFoundations(combined||realm,westPreset,eastPreset);Foundations.InitializeCombined(this);Foundations.InitializeRegional(this);}if(incident){Incident=new IncidentState(incidentsEnabled);BeginIncidentRefresh();}if(realm)Realm=new RealmOperations(this,westPreset,eastPreset);}
        internal static bool ValidSide(Side side)=>side==Side.West||side==Side.East;
        public static Side Other(Side side)=>side==Side.West?Side.East:Side.West;
        public DuelForce Force(Side side)=>side==Side.West?West:side==Side.East?East:throw new ArgumentOutOfRangeException(nameof(side));
        public Side? Owner(int node)=>node>=6&&node<=8?owners[node-6]:null;
        public int OwnKeep(Side side)=>side==Side.West?1:13;
        public bool KeepAvailable(Side side)=>!Occupants.Any(f=>f!=Force(side)&&f.Node==OwnKeep(side));
        public int RecoveryPercent(Side side)=>Force(side).Node==OwnKeep(side)&&KeepAvailable(side)?40:15;
        public bool CanAct(Side side)=>Realm!=null?Realm.CanAct(side):ValidSide(side)&&side==ActiveSide&&!HandoffPending&&Encounter==null&&PendingContact==null&&!Winner.HasValue&&(Incident==null||!Incident.WorldPhase)&&Force(side).Continues;
        private void Log(string text)=>events.Add("R"+Refresh+" "+text);
        public bool ContinueHandoff(Side side)
        {if(!HandoffPending||side!=ActiveSide||Winner.HasValue||Encounter!=null)return false;HandoffPending=false;Realm?.EnsureSelection(side);return true;}
        public StrategicMovePreview PreviewMove(Side side,int destination)
        {
            if(Realm!=null)return Realm.PreviewMove(side,destination);
            var p=new StrategicMovePreview();if(!CanAct(side)){p.Reason="No active side authority.";return p;}
            var f=Force(side);var enemy=Force(Other(side));
            if(Graph.Node(destination)==null||destination==f.Node){p.Reason="Choose another graph node.";return p;}
            p.Path=Graph.Path(f.Node,destination,n=>(Foundations==null||n!=OwnKeep(Other(side)))&&!Occupants.Any(o=>o!=f&&o.Node==n),f.Hungry);
            if(p.Path.Length<2){p.Reason="Occupied or unreachable destination.";return p;}
            p.Cost=Graph.PathCost(p.Path,f.Hungry);if(p.Cost>f.Tempo)p.Reason="Insufficient Tempo: "+p.Cost+" required.";return p;
        }
        public bool Move(Side side,int destination)
        {if(Realm!=null)return Realm.Move(side,destination);var p=PreviewMove(side,destination);if(!p.IsLegal)return false;var f=Force(side);f.Tempo-=p.Cost;if(Foundations!=null&&f.Node==OwnKeep(side)){Foundations.LeftCity(side,Refresh);}BreakHold(f,"left site");f.Node=destination;Log(side+" moves to "+destination+" for "+p.Cost);return true;}
        public bool HostileContact=>(Foundations==null||West.Node!=1&&East.Node!=13)&&West.Continues&&East.Continues&&Graph.Cost(West.Node,East.Node)>=0;
        public bool CanAttack(Side side)=>Realm==null&&CanAct(side)&&HostileContact&&Force(side).Tempo>=0;
        public bool Attack(Side side)
        {if(Realm!=null)return false;if(Incident!=null)return AttackNode(side,Force(Other(side)).Node);if(!CanAttack(side))return false;Force(side).Tempo=StrategicScenario.AfterAttackCost(Force(side).Tempo);Encounter=new DuelEncounter(side,West,East,Seed+(uint)++battleNumber);Log(side+" attacks; tactical Hotseat");return true;}
        public int RetreatDestination(Side side,int origin,int enemyNode)
        {return Graph.Nodes.Where(n=>n.Id!=origin&&n.Id!=enemyNode&&(Foundations==null||n.Id!=OwnKeep(Other(side)))&&Graph.Hops(origin,n.Id)<=2)
            .OrderByDescending(n=>Graph.Hops(n.Id,enemyNode)).ThenBy(n=>Graph.PathCost(Graph.Path(origin,n.Id))).ThenBy(n=>n.Id).Select(n=>n.Id).DefaultIfEmpty(origin).First();}
        public bool Withdraw(Side side)
        {
            if(Realm!=null)return Realm.Withdraw(side);if(!CanAttack(side))return false;if(Incident!=null)return WithdrawIncident(side);var f=Force(side);Foundations?.LeftCity(side,Refresh);f.Node=RetreatDestination(side,f.Node,Force(Other(side)).Node);f.Tempo-=40;
            Log(side+" strategic Withdrawal to "+f.Node+"; Tempo "+f.Tempo);return true;
        }
        public bool ResolveBattle(BattleState result)
        {
            if(Realm!=null)return Realm.ResolveBattle(result);if(Incident!=null)return ResolveIncidentBattle(result);var e=Encounter;if(e==null||result==null||!result.Outcome.IsEnded)return false;
            var start=e.Battle.State;
            if(result.InitialSeed!=start.InitialSeed||result.Units.Count!=start.Units.Count||result.Units.Any(u=>!start.Units.Any(v=>v.Id==u.Id&&v.Profile.Id==u.Profile.Id&&v.Side==u.Side)))return false;
            e.Battle.Resolve(result);Foundations?.ReconcileCasualties(this);
            foreach(var side in new[]{Side.West,Side.East})
            {
                var units=result.Units.Where(u=>u.Side==side).ToArray();var f=Force(side);
                if(units.Any(u=>u.Status==UnitStatus.Escaped)&&units.All(u=>!u.IsActive))
                {int origin=side==Side.West?e.WestOrigin:e.EastOrigin;f.Node=RetreatDestination(side,origin,Force(Other(side)).Node);f.Tempo-=40;Log(side+" tactical Withdrawal to "+f.Node+"; Tempo "+f.Tempo);}
            }
            var defender=Force(Other(e.Attacker));int target=e.Attacker==Side.West?e.EastOrigin:e.WestOrigin;
            if(result.Outcome.VictorySide==e.Attacker&&(!defender.Continues||defender.Node!=target))Force(e.Attacker).Node=target;
            Log("Battle "+battleNumber+": "+result.Outcome.VictorySide+" / "+result.Outcome.Reason+". Same IDs, HP/Armor and XP; no recovery.");
            Encounter=null;if(!West.Continues&&!East.Continues)Winner=null;else if(!West.Continues)Winner=Side.East;else if(!East.Continues)Winner=Side.West;return true;
        }
        public bool EndActivation(Side side)
        {
            if(Realm!=null)return Realm.EndSide(side);if(!CanAct(side))return false;int n=Force(side).Node;
            if(n>=6&&n<=8){if(n==8&&Owner(n)!=side)BreakHold(null,"lost ownership");owners[n-6]=side;Log(side+" claims "+Graph.Node(n).Name);}
            Foundations?.Capture(n,side);
            CompletedActivations++;
            if(CompletedActivations==2&&Incident!=null){Incident.WorldPhase=true;Incident.Cursor=0;Incident.ActorOrder=Incident.Raiders.Where(r=>r.OnMap).Select(r=>r.Id).OrderBy(id=>id,StringComparer.Ordinal).ToArray();HandoffPending=false;Realm?.EnsureSelection(side);return true;}
            if(CompletedActivations==2)CompleteRefresh();else ActiveSide=Other(StartingSide);
            HandoffPending=!Winner.HasValue;return true;
        }
        private void CompleteRefresh()
        {
            Foundations?.RefreshEconomy(this);
            foreach(var owner in owners)if(owner.HasValue)Force(owner.Value).Pressure++;
            if(Math.Max(West.Pressure,East.Pressure)>=TargetPressure&&West.Pressure!=East.Pressure)Winner=West.Pressure>East.Pressure?Side.West:Side.East;
            foreach(var side in new[]{Side.West,Side.East})
            {
                var f=Force(side);if(Incident!=null&&!f.Continues)continue;int used=f.Consumption;f.Provisions=Math.Max(0,f.Provisions-used);
                int supplied=0;
                if(f.Node==OwnKeep(side)&&KeepAvailable(side))
                {supplied=Math.Min(6,MaxProvisions-f.Provisions);if(Economy){supplied=(int)Math.Min(supplied,f.KeepFood);f.KeepFood-=supplied;}}
                else if(Economy&&f.Node==8&&Owner(8)==side&&(Incident==null||!Incident.Ravaged))
                {supplied=Math.Min(Math.Min(6,WaystationFood),MaxProvisions-f.Provisions);WaystationFood-=supplied;}
                f.Provisions+=supplied;
                if(RecoveryPercent(side)==40)f.Formation.ApplyOneHealingBuildingStrategicRefresh();else f.Formation.ApplyOneFieldStrategicRefresh();
                Log(side+" supply -"+used+" +"+supplied+"; recovery "+RecoveryPercent(side)+"%, Armor unchanged");f.Tempo=100+Math.Min(0,f.Tempo);
            }
            if(Economy)
            {
                if(Owner(6).HasValue){var side=Owner(6).Value;Force(side).Gold+=75;Log(side+" Mine income +75 Gold");}
                foreach(var side in new[]{Side.West,Side.East})CompleteRecruit(side);
            }
            if(Incident!=null)
            {
                FinishRaiderRefresh();
                if(Winner.HasValue){Incident.WorldPhase=false;Incident.Cursor=0;Incident.ActorOrder=Array.Empty<string>();CompletedActivations=0;ActiveSide=StartingSide;return;}
            }
            Refresh++;CompletedActivations=0;ActiveSide=StartingSide;
            if(Incident!=null){Incident.WorldPhase=false;Incident.Cursor=0;Incident.ActorOrder=Array.Empty<string>();if(!Winner.HasValue)BeginIncidentRefresh();}
        }
    }
}

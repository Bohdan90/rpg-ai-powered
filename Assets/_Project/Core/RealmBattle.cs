using System;
using System.Collections.Generic;
using System.Linq;
namespace RPG.Core
{
    public sealed class RealmEncounterPreview
    {
        public string Reason {get;internal set;}
        public DuelForce Lead {get;internal set;}
        public DuelForce Target {get;internal set;}
        public DuelForce[] Attackers {get;internal set;}=Array.Empty<DuelForce>();
        public DuelForce[] Defenders {get;internal set;}=Array.Empty<DuelForce>();
        public PersistentDeployment[] Deployments {get;internal set;}=Array.Empty<PersistentDeployment>();
        public bool Legal=>Reason==null;
    }
    public sealed partial class RealmOperations
    {
        public RealmEncounterPreview PreviewEncounter(Side side,string targetId)
        {
            var p=new RealmEncounterPreview{Lead=Selected(side),Target=Army(targetId)};
            if(World.Seamless!=null&&!World.Seamless.IsVisibleEnemy(side,targetId)){p.Reason="No currently observed hostile target.";return p;}
            if(!CanAct(side)||p.Lead==null||!p.Lead.Continues||p.Lead.Tempo<0||p.Target==null||!p.Target.Continues||p.Target.Formation.Side==side||World.AtOwnCity(p.Target)||World.ContactCost(p.Lead,p.Target)<0){p.Reason="Legal hostile contact, nonnegative lead Tempo and an unprotected Battle Target required.";return p;}
            p.Attackers=armies.Where(f=>f.Continues&&f.Formation.Side==side&&f.Tempo>=0&&World.ContactCost(f,p.Target)>=0).OrderBy(f=>f.Formation.FormationId,StringComparer.Ordinal).ToArray();
            p.Defenders=armies.Where(f=>f.Continues&&f.Formation.Side!=side&&(f==p.Target||World.ContactCost(f,p.Target)>=0)).OrderBy(f=>f.Formation.FormationId,StringComparer.Ordinal).ToArray();
            try{p.Deployments=DuelEncounter.DeploymentPlan(World.GraphFor(p.Target.WorldId),p.Lead,p.Target,p.Attackers.Concat(p.Defenders),SizeExperimentFixture.Board(SizeExperimentMap.Field_23x17_Full_9v9));}
            catch(InvalidOperationException e){p.Reason=e.Message;}return p;
        }
        public bool Attack(Side side,string targetId)
        {
            var p=PreviewEncounter(side,targetId);if(!p.Legal){return false;}
            foreach(var f in p.Attackers){f.Tempo=StrategicScenario.AfterAttackCost(f.Tempo);LowerCeilings(f);}
            foreach(var f in p.Attackers.Concat(p.Defenders))World.Seamless?.PauseJourney(f.Formation.FormationId,"Battle/contact requires a new order.");
            World.SetRealmContact(new IncidentContact(p.Lead,p.Target,p.Attackers,p.Defenders));
            Say("Battle "+"contact"+"; all participants at start; each attacker paid Attack Cost.");return true;
        }
        public bool RespondToContact(Side side,bool withdraw)
        {
            var p=World.PendingContact;if(p==null||p.Target.Formation.Side!=side||World.Encounter!=null)return false;
            if(withdraw) {
                if(p.Target.Tempo<0)return false;
                p.Target.Node=RetreatDestination(p.Target,p.Attackers.Select(f=>f.Node));p.Target.Tempo-=40;LowerCeilings(p.Target);CancelAbsentRepairs(side);
                Say(p.Target.Formation.FormationId+" pre-battle Withdrawal -> "+p.Target.Node+"; Tempo "+p.Target.Tempo);
            }else World.SetRealmEncounter(p.Lead,p.Target,p.Attackers,p.Defenders);
            World.SetRealmContact(null);World.Seamless?.Observe();return true;
        }
        public int RetreatDestination(DuelForce force,IEnumerable<int> enemyNodes)
        {
            var enemies=enemyNodes.ToArray();int origin=force.Node;var graph=World.GraphFor(force.WorldId);
            return graph.Nodes.Where(n=>n.Id!=origin&&!World.AtEnemyCity(force,n.Id)&&graph.Hops(origin,n.Id)<=2&&!armies.Any(o=>o!=force&&o.Continues&&o.WorldId==force.WorldId&&o.Node==n.Id))
                .OrderByDescending(n=>enemies.Min(e=>graph.Hops(n.Id,e))).ThenBy(n=>graph.PathCost(graph.Path(origin,n.Id))).ThenBy(n=>n.Id).Select(n=>n.Id).DefaultIfEmpty(origin).First();
        }
        public bool Withdraw(Side side)
        {
            var f=Selected(side);if(!CanAct(side)||f==null||!f.Continues||f.Tempo<0)return false;
            var enemies=armies.Where(o=>o.Continues&&o.Formation.Side!=side&&World.ContactCost(f,o)>=0&&(World.Seamless==null||World.Seamless.IsVisibleEnemy(side,o.Formation.FormationId))).Select(o=>o.Node).ToArray();if(enemies.Length==0)return false;
            World.Seamless?.PauseJourney(f.Formation.FormationId,"Withdrawal changed formation position.");f.Node=RetreatDestination(f,enemies);f.Tempo-=40;LowerCeilings(f);CancelAbsentRepairs(side);Say(f.Formation.FormationId+" Withdrawal -> "+f.Node+"; Tempo "+f.Tempo);World.Seamless?.Observe();return true;
        }
        public bool ResolveBattle(BattleState result)
        {
            var e=World.Encounter;if(e==null||result==null||!result.Outcome.IsEnded||e.Number<=AppliedBattle)return false;
            var start=e.Battle.State;
            if(result.InitialSeed!=start.InitialSeed||result.Units.Count!=start.Units.Count||result.Units.Any(u=>!start.Units.Any(v=>v.Id==u.Id&&v.Profile.Id==u.Profile.Id&&v.Side==u.Side)))return false;
            var xp=e.Battle.Resolve(result);var withdrawn=new HashSet<DuelForce>();var lines=new List<string>();
            foreach(var f in e.Participants.OrderBy(f=>f.Formation.FormationId,StringComparer.Ordinal)) {
                var units=result.Units.Where(u=>f.Formation.Members.Any(c=>c.CharacterId==e.Ids[u.Id])).ToArray();
                if(!f.Continues){lines.Add(f.Formation.FormationId+" destroyed (dead records retained)");continue;}
                if(units.Any(u=>u.Status==UnitStatus.Escaped)&&units.All(u=>!u.IsActive)) {
                    var enemyNodes=e.Participants.Where(o=>e.TacticalSides[o.Formation.FormationId]!=e.TacticalSides[f.Formation.FormationId]).Select(o=>e.Origins[o.Formation.FormationId]);
                    f.Node=RetreatDestination(f,enemyNodes);f.Tempo-=40;LowerCeilings(f);withdrawn.Add(f);lines.Add(f.Formation.FormationId+" fully withdrew -> "+f.Node+" debt/Tempo "+f.Tempo);
                }else lines.Add(f.Formation.FormationId+" holds origin "+f.Node+"; living "+f.Formation.LivingMembers.Count()+"; Commanderless="+f.Formation.Commanderless);
                CancelAbsentRepairs(f.Formation.Side);
            }
            int target=e.Origins[e.Target.Formation.FormationId];
            if(result.Outcome.VictorySide==Side.West&&e.Lead.Continues&&!withdrawn.Contains(e.Lead)&&!armies.Any(f=>f.Continues&&f.WorldId==e.Target.WorldId&&f.Node==target)) {
                e.Lead.Node=target;lines.Add("Original Lead advances: "+e.Lead.Formation.FormationId);
            }
            var people=e.Ids.OrderBy(k=>k.Key.Value).ToArray();
            LastBattle=new RealmBattleHistory{world=(int)e.WorldId,number=e.Number,seed=result.InitialSeed,lead=e.Lead.Formation.FormationId,target=e.Target.Formation.FormationId,advanced=e.Lead.Node==target&&e.Origins[e.Lead.Formation.FormationId]!=target?e.Lead.Formation.FormationId:"",attackingPool=CityFoundationData.Number(xp.West.Pool),defendingPool=CityFoundationData.Number(xp.East.Pool),finalTacticalHash=BattleStateHash.Compute(result),
                armies=e.Participants.Select(f=>f.Formation.FormationId).ToArray(),origins=e.Participants.Select(f=>e.Origins[f.Formation.FormationId]).ToArray(),destinations=e.Participants.Select(f=>f.Node).ToArray(),tempos=e.Participants.Select(f=>f.Tempo).ToArray(),attackers=e.Participants.Select(f=>e.TacticalSides[f.Formation.FormationId]==Side.West).ToArray(),withdrew=e.Participants.Select(f=>withdrawn.Contains(f)).ToArray(),
                dead=e.Participants.Select(f=>result.Units.Count(u=>u.Status==UnitStatus.Dead&&f.Formation.Members.Any(c=>c.CharacterId==e.Ids[u.Id]))).ToArray(),escaped=e.Participants.Select(f=>result.Units.Count(u=>u.Status==UnitStatus.Escaped&&f.Formation.Members.Any(c=>c.CharacterId==e.Ids[u.Id]))).ToArray(),active=e.Participants.Select(f=>result.Units.Count(u=>u.IsActive&&f.Formation.Members.Any(c=>c.CharacterId==e.Ids[u.Id]))).ToArray(),
                unitIds=people.Select(k=>k.Value).ToArray(),unitArmies=people.Select(k=>e.Participants.Single(f=>f.Formation.Members.Any(c=>c.CharacterId==k.Value)).Formation.FormationId).ToArray()};
            AppliedBattle=e.Number;World.ClearRealmEncounter();World.Seamless?.Observe();EnsureSelection(Side.West);EnsureSelection(Side.East);CheckVictory();Say(string.Join("\n",lines)+"\nOne coalition XP pool; same IDs/HP/Armor/budgets; no Refresh.");
            if(World.Seamless!=null)foreach(var side in new[]{Side.West,Side.East})World.Seamless.SetSideMessage(side,"Battle resolved in "+SeamlessWorlds.Name(e.WorldId)+"; no Refresh.\n"+string.Join("\n",e.Participants.Where(f=>f.Formation.Side==side).Select(f=>f.Formation.FormationId+" @"+f.Address+" · living "+f.Formation.LivingMembers.Count()+" · Tempo "+f.Tempo)));
            return true;
        }
    }
}

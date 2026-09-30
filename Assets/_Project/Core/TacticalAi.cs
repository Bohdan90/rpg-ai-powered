using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace RPG.Core
{
    public sealed class TacticalAiDecision
    {
        public BattleCommand Command { get; internal set; }
        public double Score { get; internal set; }
        public string Explanation { get; internal set; }
    }

    // Contract §10 one-ply prototype policy. No combat RNG access or simulated random rolls.
    public static class TacticalAi
    {
        public static TacticalAiDecision Choose(BattleState state)
        {
            if(state==null||state.Outcome.IsEnded||!state.CurrentUnitId.HasValue)return null;
            var actor=state.FindUnit(state.CurrentUnitId.Value);
            if(actor.IsFrozen)return Decision(new EndActivationCommand(actor.Id),0,"Frozen; End activation");
            if(!actor.ActionAvailable&&actor.Profile.HasGracefulExit&&actor.GracefulExitTarget.HasValue&&actor.MovementRemaining>0) {
                double baseline=Incoming(state,actor);BattleCommand exit=null;double bestExit=baseline;
                foreach(var end in TacticalAiPaths.Reachable(state,actor,actor.MovementRemaining).OrderBy(e=>e.cost).ThenBy(e=>e.position.X).ThenBy(e=>e.position.Y)) {
                    if(end.cost==0||state.Battlefield.IsRetreatZone(actor,end.position))continue;
                    var path=Pathfinder.FindPath(state,actor.Id,end.position);if(!path.Found)continue;
                    var query=state.Copy();var moving=query.FindUnit(actor.Id);moving.Position=end.position;moving.Facing=end.facing;
                    var route=TacticalAiPaths.Start(actor);foreach(var step in path.Steps)route=TacticalAiPaths.Extend(state,actor,route,step);
                    double incoming=Incoming(query,moving)+route.HpLoss(actor);
                    if(incoming<bestExit-1e-9){bestExit=incoming;exit=new MoveCommand(actor.Id,path.Steps);}
                }
                if(exit!=null)return Decision(exit,baseline-bestExit,"Graceful Exit: reduce exposure using remaining legal Movement");
            }
            if(!actor.ActionAvailable)return Decision(new EndActivationCommand(actor.Id),0,"Action spent; End");
            var enemies=state.Units.Where(u=>u.IsActive&&u.Side!=actor.Side).OrderBy(u=>u.Id).ToArray();
            if(enemies.Length==0)return Decision(new EndActivationCommand(actor.Id),0,"No enemy; End");
            var routes=new List<TacticalAiPaths.Route>();
            foreach(var endpoint in TacticalAiPaths.Reachable(state,actor,actor.MovementRemaining))
            {
                var path=Pathfinder.FindPath(state,actor.Id,endpoint.position);
                if(!path.Found)continue;
                var r=TacticalAiPaths.Start(actor);
                foreach(var step in path.Steps)r=TacticalAiPaths.Extend(state,actor,r,step);
                routes.Add(r);
            }
            // No alternate OA routing needed when no enemy has a reaction available.
            if(routes.Any(r=>r.Spent.Count>0))routes.AddRange(TacticalAiPaths.SafeRoutes(state,actor));
            var exits=routes.Where(r=>r.Steps.Count>0&&state.Battlefield.IsRetreatZone(actor,r.Position))
                .OrderBy(r=>r.HpLoss(actor)).ThenBy(r=>r.Steps.Count).ToArray();
            if(actor.Hp*4<=actor.Profile.MaxHp&&exits.Length>0&&exits[0].HpLoss(actor)<actor.Hp)
                return Decision(new MoveCommand(actor.Id,exits[0].Steps),-exits[0].HpLoss(actor),"HP <=25%; physical Retreat, minimum expected OA HP loss");
            if(actor.Hp*4<=actor.Profile.MaxHp&&actor.MovementRemaining>0)
            {
                var distance=TacticalAiPaths.GoalDistances(state,actor,Cells(state).Where(p=>state.Battlefield.IsRetreatZone(actor,p)));
                if(distance.TryGetValue(actor.Position,out int start))
                {
                    var progress=routes.Where(r=>r.Steps.Count>0&&distance.TryGetValue(r.Position,out int remaining)&&remaining<start&&r.HpLoss(actor)<actor.Hp)
                        .OrderBy(r=>distance[r.Position]).ThenBy(r=>r.HpLoss(actor)).ThenBy(r=>r.Steps.Count).ThenBy(r=>r.Position.X).ThenBy(r=>r.Position.Y).FirstOrDefault();
                    if(progress!=null)return Decision(new MoveCommand(actor.Id,progress.Steps),-progress.HpLoss(actor),
                        "HP <=25%; Retreat approach: "+start+" → "+distance[progress.Position]+" legal steps to exit; replan after each command");
                }
            }
            // Wall-occluded setup turns need a route-to-attack potential. Open approaches,
            // immediate attacks and defensive risk scoring retain the existing policy.
            Dictionary<GridPosition,int> setupDistance=null;
            if(actor.MovementRemaining>0&&enemies.All(e=>!LineOfSight.IsClear(state,actor.Position,e.Position)))
            {
                var query=state.Copy();var mover=query.FindUnit(actor.Id);
                bool UsefulAttack(GridPosition position,int steps)
                {
                    mover.Position=position;mover.MovementRemaining=0;mover.MovementSpentThisActivation=actor.MovementSpentThisActivation+steps;
                    foreach(var enemy in enemies)
                    {
                        if(position.DistanceTo(enemy.Position)>actor.Profile.Range)continue;
                        var attack=new BasicAttackCommand(actor.Id,enemy.Id,kind:BattleResolver.AvailableBasicAttack(query,actor.Id));
                        var preview=BattleResolver.PreviewAttack(query,attack);
                        if(preview.IsLegal&&preview.ContactChance>0&&preview.GuardChance<100&&(preview.HpLossOnUnguardedHit>0||preview.ArmorLossOnUnguardedHit>0))return true;
                    }
                    return false;
                }
                if(!routes.Any(r=>(r.Steps.Count==0||!state.Battlefield.IsRetreatZone(actor,r.Position))&&UsefulAttack(r.Position,r.Steps.Count)))
                {
                    var goals=Cells(state).Where(p=>state.Battlefield.IsWalkable(p)&&(p==actor.Position||!state.Battlefield.IsRetreatZone(actor,p))
                        &&(state.OccupantAt(p)==null||p==actor.Position)&&UsefulAttack(p,1)).ToArray();
                    setupDistance=TacticalAiPaths.GoalDistances(state,actor,goals);
                }
            }
            TacticalAiDecision best=null;int bestCost=int.MaxValue;
            var incomingCache=new Dictionary<string,double>();
            foreach(var r in routes.OrderBy(r=>r.Steps.Count).ThenBy(r=>r.Position.X).ThenBy(r=>r.Position.Y))
            {
                // Healthy units do not evacuate as an accidental positional candidate.
                if(r.Steps.Count>0&&state.Battlefield.IsRetreatZone(actor,r.Position))continue;
                var projected=state.Copy();var mover=projected.FindUnit(actor.Id);
                mover.Position=r.Position;mover.Facing=r.Facing;mover.MovementRemaining-=r.Steps.Count;mover.MovementSpentThisActivation+=r.Steps.Count;
                double approach=Math.Max(-2,Math.Min(2,enemies.Min(e=>actor.Position.DistanceTo(e.Position))-enemies.Min(e=>r.Position.DistanceTo(e.Position))));
                if(setupDistance!=null)
                    approach=setupDistance.TryGetValue(actor.Position,out int start)&&setupDistance.TryGetValue(r.Position,out int remaining)
                        ?Math.Max(-2,Math.Min(2,start-remaining)):0;
                var actions=new List<BattleCommand>();
                foreach(var enemy in enemies)
                {
                    var attack=new BasicAttackCommand(actor.Id,enemy.Id,kind:BattleResolver.AvailableBasicAttack(projected,actor.Id));
                    if(BattleResolver.Validate(projected,attack)==CommandError.None)actions.Add(attack);
                }
                if(r.Steps.Count==0&&BattleResolver.Validate(projected,new DefendCommand(actor.Id))==CommandError.None)actions.Add(new DefendCommand(actor.Id));
                actions.AddRange(SpellAi.Candidates(projected,mover));
                actions.Add(new EndActivationCommand(actor.Id));
                foreach(var action in actions)
                {
                    double hp=0,armor=0,kill=0;
                    mover.Facing=r.Facing;mover.IsDefending=actor.IsDefending;
                    if(action is BasicAttackCommand attack)
                    {
                        var p=BattleResolver.PreviewAttack(projected,attack);double hit=p.ContactChance/100.0*(1-p.GuardChance/100.0);
                        hp=hit*p.HpLossOnUnguardedHit;armor=hit*p.ArmorLossOnUnguardedHit;
                        kill=p.HpLossOnUnguardedHit>=projected.FindUnit(attack.Target).Hp?hit:0;
                        mover.Facing=FacingDirections.Toward(mover.Position,projected.FindUnit(attack.Target).Position);
                    }
                    if(action is CastCommand cast){hp=SpellAi.Value(projected,cast);if(hp<=0)continue;}
                    if(action is DefendCommand)mover.IsDefending=true;
                    string key=r.Position.X+","+r.Position.Y+":"+mover.Facing+":"+mover.IsDefending;
                    if(!incomingCache.TryGetValue(key,out double incoming))incomingCache[key]=incoming=Incoming(projected,mover);
                    double survival=1-r.Damage[r.Damage.Length-1];
                    double oa=r.HpLoss(actor),score=survival*(hp+.5*armor+12*kill-.5*incoming+approach)-oa;
                    if(best!=null&&(score<best.Score-1e-9||Math.Abs(score-best.Score)<=1e-9&&r.Steps.Count>=bestCost))continue;
                    var command=r.Steps.Count>0?(BattleCommand)new MoveCommand(actor.Id,r.Steps):action;
                    best=Decision(command,score,string.Format(CultureInfo.InvariantCulture,
                        "{0}; endpoint {1},{2}; next {3}; HP {4:F2}, Armor {5:F2}, kill {6:F2}, OA HP {7:F2}, incoming HP {8:F2}, approach {9:F0}",
                        command.GetType().Name,r.Position.X,r.Position.Y,action is BasicAttackCommand a?"attack "+a.Target:action.GetType().Name,hp,armor,kill,oa,incoming,approach));
                    bestCost=r.Steps.Count;
                }
            }
            if(best?.Command is EndActivationCommand&&actor.Hp*4<=actor.Profile.MaxHp&&actor.MovementRemaining==0)
                best.Explanation+="; Movement exhausted; re-evaluate Retreat next activation";
            return best??Decision(new EndActivationCommand(actor.Id),0,"End");
        }
        private static IEnumerable<GridPosition> Cells(BattleState state)
        {for(int x=0;x<state.Battlefield.Columns;x++)for(int y=0;y<state.Battlefield.Rows;y++)yield return new GridPosition(x,y);}
        private static TacticalAiDecision Decision(BattleCommand c,double score,string why)=>new TacticalAiDecision{Command=c,Score=score,Explanation=why};
        private static double Incoming(BattleState projected,UnitState target)
        {
            double total=0;
            foreach(var enemy in projected.Units.Where(u=>u.IsActive&&u.Side!=target.Side))
            {
                // Independent optimistic enemy activation: no combos, no future RNG, no allied movement.
                var query=projected.Copy();query.CurrentUnitId=enemy.Id;var attacker=query.FindUnit(enemy.Id);
                attacker.ActionAvailable=true;attacker.MovementRemaining=attacker.Profile.Movement;attacker.MovementSpentThisActivation=0;attacker.IsDefending=false;
                var endpoints=TacticalAiPaths.Reachable(query,attacker,attacker.MovementRemaining);double best=0;
                foreach(var end in endpoints)
                {
                    if(query.Battlefield.IsRetreatZone(enemy,end.position)&&end.cost>0)continue;
                    if(end.position.DistanceTo(target.Position)>attacker.Profile.Range)continue;
                    attacker.Position=end.position;attacker.MovementSpentThisActivation=end.cost;
                    var attack=new BasicAttackCommand(enemy.Id,target.Id,kind:BattleResolver.AvailableBasicAttack(query,enemy.Id));
                    var p=BattleResolver.PreviewAttack(query,attack);
                    if(p.IsLegal)best=Math.Max(best,p.ContactChance/100.0*(1-p.GuardChance/100.0)*p.HpLossOnUnguardedHit);
                }
                total+=best;
            }
            return total;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace RPG.Core
{
    // AI-only query labels. Geometry and combat probabilities come from existing Core queries.
    internal static class TacticalAiPaths
    {
        internal static readonly GridPosition[] Directions = {
            new GridPosition(0,1),new GridPosition(1,1),new GridPosition(1,0),new GridPosition(1,-1),
            new GridPosition(0,-1),new GridPosition(-1,-1),new GridPosition(-1,0),new GridPosition(-1,1) };
        internal sealed class Route
        {
            internal List<GridPosition> Steps = new List<GridPosition>();
            internal double[] Damage;
            internal HashSet<UnitId> Spent = new HashSet<UnitId>();
            internal GridPosition Position;
            internal Facing Facing;
            internal double HpLoss(UnitState unit) => Damage.Select((p,d)=>p*Math.Max(0,d-unit.Armor)).Sum();
        }
        internal static Route Start(UnitState u)
        {
            var r=new Route { Position=u.Position,Facing=u.Facing,Damage=new double[u.Hp+u.Armor+1] };r.Damage[0]=1;return r;
        }
        internal static Route Extend(BattleState state, UnitState actor, Route route, GridPosition next)
        {
            var r=new Route { Position=next,Facing=FacingDirections.Toward(route.Position,next),
                Steps=new List<GridPosition>(route.Steps),Spent=new HashSet<UnitId>(route.Spent),Damage=(double[])route.Damage.Clone() };
            var projected=state.Copy();var mover=projected.FindUnit(actor.Id);mover.Position=route.Position;mover.Facing=route.Facing;
            foreach(var id in ZoneOfControl.Reactors(projected,actor.Id,route.Position,next))
            {
                if(!r.Spent.Add(id))continue;
                var p=BattleResolver.PreviewOpportunityAttack(projected,id,actor.Id);
                double hit=p.ContactChance/100.0*(1-p.GuardChance/100.0);
                var distribution=new double[r.Damage.Length];
                for(int d=0;d<distribution.Length;d++)
                { distribution[d]+=r.Damage[d]*(1-hit);distribution[Math.Min(distribution.Length-1,d+p.PhysicalDamage)]+=r.Damage[d]*hit; }
                r.Damage=distribution;
            }
            r.Steps.Add(next);return r;
        }
        // Retain non-dominated damage distributions per cell/facing/spent-responder set/cost.
        // This keeps safe detours even when longer than the ordinary readable shortest path.
        internal static List<Route> SafeRoutes(BattleState state, UnitState actor)
        {
            var all=new List<Route>{Start(actor)};var layer=all.ToList();
            for(int cost=1;cost<=actor.MovementRemaining && actor.ActionAvailable;cost++)
            {
                var groups=new SortedDictionary<string,List<Route>>(StringComparer.Ordinal);
                foreach(var r in layer)
                {
                    if(r.Steps.Count>0 && state.Battlefield.IsRetreatZone(actor.Side,r.Position))continue;
                    foreach(var delta in Directions)
                    {
                        var next=new GridPosition(r.Position.X+delta.X,r.Position.Y+delta.Y);
                        if(MovementRules.ValidateStep(state,actor.Id,r.Position,next)!=CommandError.None)continue;
                        var candidate=Extend(state,actor,r,next);
                        string key=next.X+","+next.Y+":"+(int)candidate.Facing+":"+string.Join(",",candidate.Spent.OrderBy(x=>x));
                        if(!groups.TryGetValue(key,out var bucket))groups[key]=bucket=new List<Route>();
                        if(bucket.Any(old=>Dominates(old.Damage,candidate.Damage)))continue;
                        bucket.RemoveAll(old=>Dominates(candidate.Damage,old.Damage));bucket.Add(candidate);
                    }
                }
                layer=groups.Values.SelectMany(x=>x).ToList();all.AddRange(layer);
            }
            return all.GroupBy(r=>r.Position).Select(g=>g.OrderBy(r=>r.HpLoss(actor)).ThenBy(r=>r.Steps.Count).First()).ToList();
        }
        private static bool Dominates(double[] a,double[] b)
        {
            double ca=0,cb=0;
            for(int i=0;i<a.Length;i++){ca+=a[i];cb+=b[i];if(ca+1e-12<cb)return false;}return true;
        }
        internal static List<(GridPosition position,int cost,Facing facing)> Reachable(BattleState state,UnitState u,int budget)
        {
            var result=new List<(GridPosition,int,Facing)>{(u.Position,0,u.Facing)};var seen=new HashSet<GridPosition>{u.Position};
            for(int i=0;i<result.Count;i++)
            {
                var (from,cost,facing)=result[i];
                if(cost>=budget || cost>0&&state.Battlefield.IsRetreatZone(u.Side,from))continue;
                foreach(var d in Directions)
                {
                    var p=new GridPosition(from.X+d.X,from.Y+d.Y);
                    if(seen.Contains(p)||MovementRules.ValidateStep(state,u.Id,from,p)!=CommandError.None)continue;
                    seen.Add(p);result.Add((p,cost+1,FacingDirections.Toward(from,p)));
                }
            }
            return result;
        }
    }
}

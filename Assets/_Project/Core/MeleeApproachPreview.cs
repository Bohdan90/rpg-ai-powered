using System;
using System.Linq;

namespace RPG.Core
{
    // Read-only composition of existing movement/attack queries, not a new combat command.
    // Query preserves melee callers; QueryBow provides the same composition for a Bow shot.
    public sealed class MeleeApproachPreview
    {
        public MoveCommand Movement { get; private set; }
        public BasicAttackCommand Attack { get; private set; }
        public AttackPreview OnArrival { get; private set; }
        public OpportunityAttackPreview Risk { get; private set; }
        public static MeleeApproachPreview Query(BattleState state, UnitId actorId, UnitId targetId)
            => Query(state,actorId,targetId,false);
        public static MeleeApproachPreview QueryBow(BattleState state, UnitId actorId, UnitId targetId)
            => Query(state,actorId,targetId,true);
        private static MeleeApproachPreview Query(BattleState state, UnitId actorId, UnitId targetId, bool bow)
        {
            var actor=state.FindUnit(actorId);var target=state.FindUnit(targetId);
            if(actor==null||target==null||!actor.IsActive||!target.IsActive||actor.Side==target.Side
                ||actor.Profile.IsCaster||!actor.ActionAvailable)return null;
            if(bow ? !actor.Profile.IsArcher||BattleResolver.IsArcherEngaged(state,actorId)
                : !actor.Profile.HasMeleeBasic||actor.Profile.IsArcher)return null;
            var attack=new BasicAttackCommand(actorId,targetId);
            if(BattleResolver.Validate(state,attack)==CommandError.None)return null; // Already in contact: attack directly.
            MeleeApproachPreview best=null;int bestRisk=int.MaxValue;long bestDeviation=long.MaxValue;
            int radius=bow?actor.MovementRemaining:1;var center=bow?actor.Position:target.Position;
            for(int x=Math.Max(0,center.X-radius);x<=Math.Min(state.Battlefield.Columns-1,center.X+radius);x++)
            for(int y=Math.Max(0,center.Y-radius);y<=Math.Min(state.Battlefield.Rows-1,center.Y+radius);y++) {
                var destination=new GridPosition(x,y);
                if(state.Battlefield.IsRetreatZone(actor,destination))continue;
                if(bow&&destination.DistanceTo(target.Position)>actor.Profile.Range)continue;
                var path=Pathfinder.FindPath(state,actorId,destination);
                if(!path.Found||path.Cost==0)continue;
                var move=new MoveCommand(actorId,path.Steps);var risk=OpportunityAttackPreview.Query(state,move);
                if(!risk.IsLegal)continue;
                var projected=state.Copy();var arrival=projected.FindUnit(actorId);
                arrival.Position=destination;arrival.Facing=FacingDirections.Toward(path.Cost==1?actor.Position:path.Steps[path.Cost-2],destination);
                arrival.MovementRemaining-=path.Cost;arrival.MovementSpentThisActivation+=path.Cost;
                var hit=BattleResolver.PreviewAttack(projected,attack);if(!hit.IsLegal)continue;
                int risks=risk.Exposures.Sum(e=>e.Threats.Count(t=>t.WouldReact));
                long dx=target.Position.X-actor.Position.X,dy=target.Position.Y-actor.Position.Y;
                long deviation=path.Steps.Sum(step=>Math.Abs(dx*(step.Y-actor.Position.Y)-dy*(step.X-actor.Position.X)));
                // Shortest approach, fewer OA triggers, then closest to the direct enemy line.
                // Stable x/y is only the final tie-break, not a reason to step sideways on open ground.
                if(best!=null&&(path.Cost>best.Movement.Path.Count||path.Cost==best.Movement.Path.Count
                    &&(risks>bestRisk||risks==bestRisk&&deviation>=bestDeviation)))continue;
                best=new MeleeApproachPreview{Movement=move,Attack=attack,OnArrival=hit,Risk=risk};bestRisk=risks;bestDeviation=deviation;
            }
            return best;
        }
    }
}

using System;
using System.Linq;

namespace RPG.Core
{
    // Read-only composition. Execution remains an ordinary Move followed by a revalidated Cast.
    public sealed class HealApproachPreview
    {
        public MoveCommand Movement { get; private set; }
        public CastCommand Heal { get; private set; }
        public UnitId Target { get; private set; }
        public SpellPreview OnArrival { get; private set; }
        public OpportunityAttackPreview Risk { get; private set; }
        public static HealApproachPreview Query(BattleState state, UnitId actorId, UnitId targetId)
        {
            var actor=state.FindUnit(actorId);var target=state.FindUnit(targetId);
            if(actor==null||target==null||!actor.IsActive||!target.IsActive||actor.Side!=target.Side
                ||actor.Position.DistanceTo(target.Position)<=1)return null;
            var heal=new CastCommand(actorId,SpellId.CloseHeal,target.Position);
            var direct=BattleResolver.PreviewSpell(state,heal);
            // Only range/LoS may be solved by walking; never hide an Action/source/target blocker.
            if(direct.Blockers.Any(e=>e!=CommandError.OutOfRange&&e!=CommandError.BlockedLineOfSight))return null;
            HealApproachPreview best=null;int bestRisk=int.MaxValue;long bestDeviation=long.MaxValue;
            for(int x=Math.Max(0,target.Position.X-1);x<=Math.Min(state.Battlefield.Columns-1,target.Position.X+1);x++)
            for(int y=Math.Max(0,target.Position.Y-1);y<=Math.Min(state.Battlefield.Rows-1,target.Position.Y+1);y++) {
                var cell=new GridPosition(x,y);if(state.Battlefield.IsRetreatZone(actor,cell))continue;
                var path=Pathfinder.FindPath(state,actorId,cell);if(!path.Found||path.Cost==0)continue;
                var move=new MoveCommand(actorId,path.Steps);var risk=OpportunityAttackPreview.Query(state,move);if(!risk.IsLegal)continue;
                var projected=state.Copy();var arrival=projected.FindUnit(actorId);arrival.Position=cell;
                arrival.MovementRemaining-=path.Cost;arrival.MovementSpentThisActivation+=path.Cost;
                arrival.Facing=FacingDirections.Toward(path.Cost==1?actor.Position:path.Steps[path.Cost-2],cell);
                var preview=BattleResolver.PreviewSpell(projected,heal);if(!preview.IsLegal)continue;
                int risks=risk.Exposures.Sum(e=>e.Threats.Count(t=>t.WouldReact));
                long dx=target.Position.X-actor.Position.X,dy=target.Position.Y-actor.Position.Y;
                long deviation=path.Steps.Sum(p=>Math.Abs(dx*(p.Y-actor.Position.Y)-dy*(p.X-actor.Position.X)));
                if(best!=null&&(path.Cost>best.Movement.Path.Count||path.Cost==best.Movement.Path.Count
                    &&(risks>bestRisk||risks==bestRisk&&deviation>=bestDeviation)))continue;
                best=new HealApproachPreview{Movement=move,Heal=heal,Target=targetId,OnArrival=preview,Risk=risk};bestRisk=risks;bestDeviation=deviation;
            }
            return best;
        }
    }
}

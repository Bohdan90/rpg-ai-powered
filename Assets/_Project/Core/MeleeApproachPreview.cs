using System.Linq;

namespace RPG.Core
{
    // Read-only composition of existing movement/contact queries, not a new combat command.
    public sealed class MeleeApproachPreview
    {
        public MoveCommand Movement { get; private set; }
        public BasicAttackCommand Attack { get; private set; }
        public AttackPreview OnArrival { get; private set; }
        public OpportunityAttackPreview Risk { get; private set; }
        public static MeleeApproachPreview Query(BattleState state, UnitId actorId, UnitId targetId)
        {
            var actor=state.FindUnit(actorId);var target=state.FindUnit(targetId);
            if(actor==null||target==null||!actor.IsActive||!target.IsActive||actor.Side==target.Side
                ||!actor.Profile.HasMeleeBasic||actor.Profile.IsCaster||actor.Profile.IsArcher||!actor.ActionAvailable)return null;
            var attack=new BasicAttackCommand(actorId,targetId);
            if(BattleResolver.Validate(state,attack)==CommandError.None)return null; // Already in contact: attack directly.
            MeleeApproachPreview best=null;int bestRisk=int.MaxValue;
            for(int x=target.Position.X-1;x<=target.Position.X+1;x++)
            for(int y=target.Position.Y-1;y<=target.Position.Y+1;y++) {
                var destination=new GridPosition(x,y);
                if(state.Battlefield.IsRetreatZone(actor,destination))continue;
                var path=Pathfinder.FindPath(state,actorId,destination);
                if(!path.Found||path.Cost==0)continue;
                var move=new MoveCommand(actorId,path.Steps);var risk=OpportunityAttackPreview.Query(state,move);
                if(!risk.IsLegal)continue;
                var projected=state.Copy();var arrival=projected.FindUnit(actorId);
                arrival.Position=destination;arrival.Facing=FacingDirections.Toward(path.Cost==1?actor.Position:path.Steps[path.Cost-2],destination);
                arrival.MovementRemaining-=path.Cost;arrival.MovementSpentThisActivation+=path.Cost;
                var hit=BattleResolver.PreviewAttack(projected,attack);if(!hit.IsLegal)continue;
                int risks=risk.Exposures.Sum(e=>e.Threats.Count(t=>t.WouldReact));
                // Shortest legal approach, fewer OA triggers on ties, then stable x/y enumeration.
                if(best!=null&&(path.Cost>best.Movement.Path.Count||path.Cost==best.Movement.Path.Count&&risks>=bestRisk))continue;
                best=new MeleeApproachPreview{Movement=move,Attack=attack,OnArrival=hit,Risk=risk};bestRisk=risks;
            }
            return best;
        }
    }
}

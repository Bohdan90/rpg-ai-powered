using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RPG.Core
{
    public readonly struct OpportunityThreat
    {
        public UnitId Responder { get; }
        public bool AvailableNow { get; }
        // Conditional on the mover surviving earlier steps; an earlier trigger spends availability.
        public bool WouldReact { get; }
        internal OpportunityThreat(UnitId responder, bool availableNow, bool wouldReact)
        { Responder = responder; AvailableNow = availableNow; WouldReact = wouldReact; }
    }

    public sealed class OpportunityExposure
    {
        public int StepIndex { get; }
        public GridPosition From { get; }
        public GridPosition To { get; }
        public ReadOnlyCollection<OpportunityThreat> Threats { get; }
        internal OpportunityExposure(int stepIndex, GridPosition from, GridPosition to, List<OpportunityThreat> threats)
        { StepIndex = stepIndex; From = from; To = to; Threats = threats.AsReadOnly(); }
    }

    public sealed class OpportunityAttackPreview
    {
        public CommandError Error { get; }
        public bool IsLegal => Error == CommandError.None;
        public ReadOnlyCollection<OpportunityExposure> Exposures { get; }
        private OpportunityAttackPreview(CommandError error, List<OpportunityExposure> exposures)
        { Error = error; Exposures = exposures.AsReadOnly(); }

        public static OpportunityAttackPreview Query(BattleState state, MoveCommand command)
        {
            var exposures = new List<OpportunityExposure>();
            var error = BattleResolver.Validate(state, command);
            if (error != CommandError.None) return new OpportunityAttackPreview(error, exposures);
            var mover = state.FindUnit(command.Actor);
            var spent = new HashSet<UnitId>();
            var from = mover.Position;
            var graceful=mover.GracefulExitTarget;
            for (int i = 0; i < command.Path.Count; i++)
            {
                var to = command.Path[i];
                var threats = new List<OpportunityThreat>();
                foreach (var id in ZoneOfControl.Sources(state, mover.Side, from))
                {
                    var responder = state.FindUnit(id);
                    if (ZoneOfControl.Exerts(state, responder, to)) continue;
                    bool available = responder.OpportunityAttackAvailable && !responder.IsFrozen;
                    if(graceful==id){graceful=null;threats.Add(new OpportunityThreat(id,available,false));continue;}
                    bool wouldReact = available && spent.Add(id);
                    threats.Add(new OpportunityThreat(id, available, wouldReact));
                }
                if (threats.Count > 0) exposures.Add(new OpportunityExposure(i, from, to, threats));
                if (state.Battlefield.IsRetreatZone(mover, to)) break;
                from = to;
            }
            return new OpportunityAttackPreview(CommandError.None, exposures);
        }
    }
}

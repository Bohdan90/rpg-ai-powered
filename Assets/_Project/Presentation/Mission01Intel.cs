using System.Linq;
using RPG.Core;

namespace RPG.Presentation
{
    // Authored Mission-01 local scout/civilian reports only; not a global visibility rule.
    public static class Mission01Intel
    {
        public static bool Known(StrategicActor actor)=>actor.Active;
        public static string ProfileLabel(UnitProfile p)=>p.IsArcher?"HA":p.Id==UnitProfileId.ElfWarriorTI?"EW":"HW";
        public static string Name(StrategicActorKind kind)
        {
            switch(kind){case StrategicActorKind.HardGuard:return "Old Bridge Guard";
                case StrategicActorKind.AreaGuard:return "Portal Area Guard";
                case StrategicActorKind.Patrol:return "North Patrol";
                case StrategicActorKind.IncursionA:return "Incursion A";default:return "Incursion B";}
        }
        public static string Force(StrategicActor actor)
        {
            if(!Known(actor))return "";
            var members=actor.Formation.LivingMembers.ToArray();
            return Name(actor.Kind)+" · "+members.Length+(members.Length==1?" unit\n":" units\n")+string.Join(" · ",members.GroupBy(c=>ProfileLabel(c.Profile)).Select(g=>g.Count()+" "+g.Key));
        }
        public static string Marker(StrategicActor actor)
        {
            if(!Known(actor))return "";
            string role=actor.Kind==StrategicActorKind.AreaGuard?"Portal Guard":Name(actor.Kind);
            string force=Force(actor);return role+"\n"+force.Substring(Name(actor.Kind).Length+3);
        }
        public static string Intent(StrategicActor actor)
        {
            if(!Known(actor))return "";
            if(actor.Objective==StrategicObjective.Withdraw)return "Observed withdrawing toward exit";
            switch(actor.Kind){case StrategicActorKind.HardGuard:return "Guarding Old Bridge";
                case StrategicActorKind.AreaGuard:return "Guarding Portal approaches";
                case StrategicActorKind.Patrol:return "Patrolling North Pass / East Ridge";
                case StrategicActorKind.IncursionA:return "Observed toward Frontier Waystation";
                default:return "Observed toward Riverside Village";}
        }
    }
}

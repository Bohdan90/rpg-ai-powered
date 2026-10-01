using System;
using System.Collections.Generic;
using System.Linq;
using RPG.Core;

namespace RPG.Presentation
{
    // Readable projection of resolved Core events. The complete journal stays untouched.
    public static class CombatOutcomeText
    {
        public static IReadOnlyList<string> Format(IEnumerable<BattleEvent> events,Func<UnitId,string> name,BattleCommand command=null)
        {
            var rows=new List<string>();var parts=new List<string>();UnitId? actor=null,target=null;
            string action=command is BasicAttackCommand?"Basic attack":command is MoveCommand?"Movement":"Status";
            string rowAction=action;UnitId? damageSource=null;
            Action flush=()=>{if(parts.Count==0)return;rows.Add((actor.HasValue?name(actor.Value):"Battle")+(target.HasValue?" → "+name(target.Value):"")+"\n"+rowAction+" · "+string.Join(" | ",parts));parts.Clear();};
            foreach(var e in events) {
                if(e.Kind==BattleEventKind.SpellCast){flush();action=SpellName((SpellId)e.Amount);continue;}
                if(e.Kind==BattleEventKind.OpportunityAttackTriggered){flush();action="Opportunity attack";continue;}
                if(e.Kind==BattleEventKind.BurnTick){flush();action="Burn";continue;}
                if(e.Kind==BattleEventKind.DamageApplied)damageSource=e.Actor;
                // Current Core emits reversed-source Burn only for Fire Armor's melee payoff.
                // Do not label the recipient's payoff as another Basic attack by that unit.
                string effectAction=e.Kind==BattleEventKind.BurnApplied&&damageSource.HasValue&&e.Actor!=damageSource?"Fire Armor retaliation":action;
                string part=null;
                switch(e.Kind) {
                    case BattleEventKind.AttackMissed:part="Miss (failed contact)";break;
                    case BattleEventKind.GuardSucceeded:part="Guard — no damage";break;
                    case BattleEventKind.BarrierChanged:part=e.After<e.Before?(e.Amount>0?"Temporary Barrier absorbed "+(e.Before-e.After):"Temporary Barrier expired / removed "+e.Before+" → "+e.After):"Temporary Barrier "+e.Before+" → "+e.After;break;
                    case BattleEventKind.ArmorLost:part="Armor -"+e.Amount;break;
                    case BattleEventKind.HpLost:part="HP -"+e.Amount;break;
                    case BattleEventKind.HpHealed:part="HP +"+(e.After-e.Before);break;
                    case BattleEventKind.BurnApplied:part="Burn +1";break;
                    case BattleEventKind.FreezeApplied:part="Frozen";break;
                    case BattleEventKind.FreezeEnded:part="Freeze ended";break;
                    case BattleEventKind.ConditionCleansed:part="Condition cleansed";break;
                    case BattleEventKind.UnitDied:part="Dead";break;
                    case BattleEventKind.UnitEscaped:part="Escaped / Safe";break;
                }
                if(part==null)continue;
                if(actor!=e.Actor||target!=e.Target||rowAction!=effectAction){flush();actor=e.Actor;target=e.Target;rowAction=effectAction;}
                parts.Add(part);
            }
            flush();return rows;
        }
        public static string SpellName(SpellId s)=>s==SpellId.FireStream?"Fire Stream":s==SpellId.FireArmor?"Fire Armor":s==SpellId.IceShard?"Ice Shard":s==SpellId.IceShield?"Ice Shield":s==SpellId.CloseHeal?"Close Heal":s.ToString();
    }
}

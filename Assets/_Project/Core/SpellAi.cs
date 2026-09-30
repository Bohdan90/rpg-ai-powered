using System;
using System.Collections.Generic;
using System.Linq;
namespace RPG.Core
{
    internal static class SpellAi
    {
        internal static IEnumerable<CastCommand> Candidates(BattleState state,UnitState actor)
        {
            foreach(var spell in SpellRules.Kit(actor.Profile)) {
                IEnumerable<GridPosition> cells;
                if(spell==SpellId.FireArmor)cells=new[]{actor.Position};
                else if(spell==SpellId.FireStream)cells=TacticalAiPaths.Directions.Select(d=>new GridPosition(actor.Position.X+d.X,actor.Position.Y+d.Y));
                else if(spell==SpellId.Fireball)cells=state.Units.Where(u=>u.IsActive&&u.Side!=actor.Side).SelectMany(u=>TacticalAiPaths.Directions.Select(d=>new GridPosition(u.Position.X+d.X,u.Position.Y+d.Y)).Concat(new[]{u.Position})).Distinct().OrderBy(p=>p.X).ThenBy(p=>p.Y);
                else cells=state.Units.Where(u=>u.IsActive).Select(u=>u.Position);
                foreach(var cell in cells){var c=new CastCommand(actor.Id,spell,cell,true);if(BattleResolver.Validate(state,c)==CommandError.None)yield return c;}
            }
        }
        internal static double Value(BattleState state,CastCommand c)
        {
            var p=BattleResolver.PreviewSpell(state,c);var actor=state.FindUnit(c.Actor);double value=0;
            foreach(var id in p.Targets) {
                var u=state.FindUnit(id);bool friendly=u.Side==actor.Side;double hit=p.ContactChance/100.0;
                switch(c.Spell) {
                    case SpellId.CloseHeal:value+=Math.Min(p.Magnitude,u.Profile.MaxHp-u.Hp)+(u.BurnStacks>0?u.BurnStacks*3:u.PoisonStacks>0||u.BleedStacks>0?3:0);break;
                    case SpellId.IceShield:case SpellId.FireArmor:
                        int amount=c.Spell==SpellId.FireArmor?6:10;
                        if(state.Units.Any(e=>e.IsActive&&e.Side!=u.Side&&e.Position.DistanceTo(u.Position)<=8))value+=Math.Max(0,amount-u.TemporaryBarrier)*.6;
                        break;
                    case SpellId.Freeze:if(!u.IsFrozen)value+=hit*7;break;
                    default:
                        double loss=Math.Min(u.Hp,Math.Max(0,p.Magnitude-u.TemporaryBarrier))+.4*Math.Min(u.TemporaryBarrier,p.Magnitude);
                        if(p.Magnitude>=u.Hp+u.TemporaryBarrier)loss+=12;
                        value+=(friendly?-1.5:1)*hit*loss;break;
                }
            }
            return value;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace RPG.Core
{
    public enum SpellId { FireStream, FireArmor, Fireball, IceShard, IceShield, Freeze, CloseHeal }
    public enum DamageType { Physical, Fire, WaterIce }
    public sealed class CastCommand : BattleCommand
    {
        public SpellId Spell { get; }
        public GridPosition Cell { get; }
        public bool FriendlyFireConfirmed { get; }
        public CastCommand(UnitId actor,SpellId spell,GridPosition cell,bool friendlyFireConfirmed=false):base(actor)
        { Spell=spell;Cell=cell;FriendlyFireConfirmed=friendlyFireConfirmed; }
    }
    public sealed class SpellPreview
    {
        public CommandError Error { get; internal set; }
        public bool IsLegal => Error==CommandError.None;
        public IReadOnlyList<GridPosition> Cells { get; internal set; }
        public IReadOnlyList<UnitId> Targets { get; internal set; }
        public int Magnitude { get; internal set; }
        public int ContactChance { get; internal set; }=100;
    }
    public static class SpellRules
    {
        public static bool Exertion(SpellId s)=>s!=SpellId.FireStream && s!=SpellId.IceShard;
        public static int Range(SpellId s)=>s==SpellId.FireStream?3:s==SpellId.Fireball||s==SpellId.IceShard?8:s==SpellId.Freeze?6:s==SpellId.IceShield?4:s==SpellId.CloseHeal?1:0;
        public static int Limit(SpellId s)=>s==SpellId.Fireball||s==SpellId.Freeze?2:s==SpellId.CloseHeal?3:int.MaxValue;
        public static int Used(UnitState u,SpellId s)=>s==SpellId.Fireball?u.FireballUsed:s==SpellId.Freeze?u.FreezeUsed:s==SpellId.CloseHeal?u.CloseHealUsed:0;
        public static bool Has(UnitProfile p,SpellId s)=>p.IsFireMage && (s==SpellId.FireStream||s==SpellId.FireArmor||s==SpellId.Fireball&&p.Tier==2)
            ||p.IsIceMage && (s==SpellId.IceShard||s==SpellId.IceShield||s==SpellId.Freeze&&p.Tier==2)
            ||p.Id==UnitProfileId.HumanHealerTI&&s==SpellId.CloseHeal;
        public static IEnumerable<SpellId> Kit(UnitProfile p)=>Enum.GetValues(typeof(SpellId)).Cast<SpellId>().Where(s=>Has(p,s));
        // Same integer-pool policy as physical resistance: scale once, truncate only the final amount.
        public static int Magnitude(UnitProfile p,SpellId s)=>(int)(p.MagicPower*(s==SpellId.FireStream?10:s==SpellId.IceShard?12:s==SpellId.Fireball||s==SpellId.CloseHeal?14:0));
        public static bool Area(SpellId s)=>s==SpellId.FireStream||s==SpellId.Fireball;
        public static bool Helpful(SpellId s)=>s==SpellId.FireArmor||s==SpellId.IceShield||s==SpellId.CloseHeal;
    }
    public static partial class BattleResolver
    {
        private static List<GridPosition> SpellCells(BattleState state,UnitState actor,CastCommand command)
        {
            var result=new List<GridPosition>();var center=command.Cell;
            if(command.Spell==SpellId.FireStream) {
                int dx=center.X-actor.Position.X,dy=center.Y-actor.Position.Y;
                if(dx==0&&dy==0 || dx!=0&&dy!=0&&Math.Abs(dx)!=Math.Abs(dy))return result;
                dx=Math.Sign(dx);dy=Math.Sign(dy);
                for(int i=1;i<=3;i++) {
                    var p=new GridPosition(actor.Position.X+dx*i,actor.Position.Y+dy*i);
                    if(!state.Battlefield.IsWalkable(p)||!LineOfSight.IsClear(state,actor.Position,p))break;
                    result.Add(p);
                }
            } else if(command.Spell==SpellId.Fireball) {
                for(int x=center.X-1;x<=center.X+1;x++)for(int y=center.Y-1;y<=center.Y+1;y++) {
                    var p=new GridPosition(x,y);
                    if(state.Battlefield.IsWalkable(p)&&LineOfSight.IsClear(state,center,p))result.Add(p);
                }
            } else result.Add(center);
            return result;
        }
        private static CommandError ValidateCast(BattleState state,UnitState actor,CastCommand command)
        {
            if(!actor.ActionAvailable)return CommandError.NoAction;
            if(!SpellRules.Has(actor.Profile,command.Spell))return CommandError.AbilityUnavailable;
            if(SpellRules.Exertion(command.Spell)&&actor.IsExhausted)return CommandError.Exhausted;
            if(SpellRules.Used(actor,command.Spell)>=SpellRules.Limit(command.Spell))return CommandError.SourceBudgetSpent;
            if(!state.Battlefield.Contains(command.Cell))return CommandError.OutOfBounds;
            if(!state.Battlefield.IsWalkable(command.Cell))return CommandError.SolidCell;
            if(actor.Position.DistanceTo(command.Cell)>SpellRules.Range(command.Spell))return CommandError.OutOfRange;
            if(!LineOfSight.IsClear(state,actor.Position,command.Cell))return CommandError.BlockedLineOfSight;
            if(SpellRules.Area(command.Spell)) {
                var cells=SpellCells(state,actor,command);
                if(cells.Count==0)return CommandError.InvalidCommand;
                if(!command.FriendlyFireConfirmed&&state.Units.Any(u=>u.IsActive&&u.Side==actor.Side&&cells.Contains(u.Position)))return CommandError.FriendlyFireNotConfirmed;
                return CommandError.None;
            }
            var target=state.OccupantAt(command.Cell);
            if(target==null)return CommandError.TargetNotFound;
            if(SpellRules.Helpful(command.Spell)?target.Side!=actor.Side:target.Side==actor.Side)return CommandError.InvalidCommand;
            if(command.Spell==SpellId.FireArmor&&target.Id!=actor.Id)return CommandError.SelfTarget;
            if(command.Spell==SpellId.CloseHeal&&target.Hp==target.Profile.MaxHp&&target.BurnStacks==0&&target.PoisonStacks==0&&target.BleedStacks==0)return CommandError.NoUsefulEffect;
            return CommandError.None;
        }
        public static SpellPreview PreviewSpell(BattleState state,CastCommand command)
        {
            var error=Validate(state,command);var actor=state.FindUnit(command.Actor);
            var cells=actor==null?new List<GridPosition>():SpellCells(state,actor,command);
            var targets=state.Units.Where(u=>u.IsActive&&cells.Contains(u.Position)).OrderBy(u=>u.Id).ToArray();
            var preview=new SpellPreview{Error=error,Cells=cells.AsReadOnly(),Targets=Array.AsReadOnly(targets.Select(u=>u.Id).ToArray()),Magnitude=actor==null?0:SpellRules.Magnitude(actor.Profile,command.Spell)};
            if(actor!=null&&!SpellRules.Area(command.Spell)&&!SpellRules.Helpful(command.Spell)&&targets.Length==1) {
                var t=targets[0];int frontal=FacingDirections.IsFrontal(t.Facing,t.Position,actor.Position)?t.Profile.FrontalEvasion:0;
                preview.ContactChance=Math.Max(5,Math.Min(95,actor.Profile.Accuracy-t.Profile.Dodge-frontal));
            }
            return preview;
        }
        private static void Cast(BattleState state,UnitState actor,CastCommand command,List<BattleEvent> events)
        {
            var preview=PreviewSpell(state,command);
            ConsumeAction(state,actor,events);
            if(SpellRules.Exertion(command.Spell)) {actor.ExhaustedActivations=2;events.Add(new BattleEvent(BattleEventKind.ExhaustionChanged,state.Round,actor.Id,after:2));}
            if(command.Spell==SpellId.Fireball)actor.FireballUsed++;
            if(command.Spell==SpellId.Freeze)actor.FreezeUsed++;
            if(command.Spell==SpellId.CloseHeal)actor.CloseHealUsed++;
            events.Add(new BattleEvent(BattleEventKind.SpellCast,state.Round,actor.Id,amount:(int)command.Spell,to:command.Cell));
            if(command.Cell!=actor.Position)SetFacing(state,actor,FacingDirections.Toward(actor.Position,command.Cell),events);
            foreach(var id in preview.Targets) {
                var target=state.FindUnit(id);
                if(!SpellRules.Helpful(command.Spell)&&!SpellRules.Area(command.Spell)) {
                    int roll=state.Random.NextPercent();events.Add(new BattleEvent(BattleEventKind.ContactRolled,state.Round,actor.Id,target.Id,chancePercent:preview.ContactChance,roll:roll));
                    if(roll>=preview.ContactChance){events.Add(new BattleEvent(BattleEventKind.AttackMissed,state.Round,actor.Id,target.Id));continue;}
                }
                switch(command.Spell) {
                    case SpellId.FireArmor:case SpellId.IceShield:
                        int before=target.TemporaryBarrier;target.TemporaryBarrier=command.Spell==SpellId.FireArmor?6:10;
                        target.BarrierActivations=2;target.FireProtection=command.Spell==SpellId.FireArmor;
                        events.Add(new BattleEvent(BattleEventKind.BarrierChanged,state.Round,actor.Id,target.Id,before:before,after:target.TemporaryBarrier));break;
                    case SpellId.Freeze:
                        target.FrozenActivations=1;
                        events.Add(new BattleEvent(BattleEventKind.FreezeApplied,state.Round,actor.Id,target.Id));break;
                    case SpellId.CloseHeal:
                        int hp=target.Hp;target.Hp=Math.Min(target.Profile.MaxHp,target.Hp+preview.Magnitude);
                        events.Add(new BattleEvent(BattleEventKind.HpHealed,state.Round,actor.Id,target.Id,amount:target.Hp-hp,before:hp,after:target.Hp));
                        if(target.BurnStacks>0){target.BurnStacks=0;target.BurnTicks=0;}
                        else if(target.PoisonStacks>0)target.PoisonStacks=0;else if(target.BleedStacks>0)target.BleedStacks=0;else break;
                        events.Add(new BattleEvent(BattleEventKind.ConditionCleansed,state.Round,actor.Id,target.Id));break;
                    default:
                        int dealt=DealDamage(state,actor,target,preview.Magnitude,command.Spell==SpellId.IceShard?DamageType.WaterIce:DamageType.Fire,true,false,events);
                        if(dealt>0&&target.IsActive&&command.Spell!=SpellId.IceShard&&state.Random.NextPercent()<30)AddBurn(state,actor,target,events);
                        break;
                }
            }
        }
        private static int DealDamage(BattleState state,UnitState actor,UnitState target,int amount,DamageType type,bool direct,bool melee,List<BattleEvent> events)
        {
            int remaining=amount,before=target.TemporaryBarrier;
            int barrier=Math.Min(before,remaining);target.TemporaryBarrier-=barrier;remaining-=barrier;
            events.Add(new BattleEvent(BattleEventKind.DamageApplied,state.Round,actor.Id,target.Id,amount:amount));
            if(barrier>0)events.Add(new BattleEvent(BattleEventKind.BarrierChanged,state.Round,actor.Id,target.Id,amount:barrier,before:before,after:target.TemporaryBarrier));
            int armor=type==DamageType.Physical?Math.Min(target.Armor,remaining):0;remaining-=armor;
            if(armor>0){before=target.Armor;target.Armor-=armor;events.Add(new BattleEvent(BattleEventKind.ArmorLost,state.Round,actor.Id,target.Id,amount:armor,before:before,after:target.Armor));}
            int hp=Math.Min(target.Hp,remaining);
            if(hp>0){before=target.Hp;target.Hp-=hp;events.Add(new BattleEvent(BattleEventKind.HpLost,state.Round,actor.Id,target.Id,amount:hp,before:before,after:target.Hp));}
            int dealt=barrier+armor+hp;
            if(direct&&dealt>0&&target.IsFrozen){target.FrozenActivations=0;events.Add(new BattleEvent(BattleEventKind.FreezeEnded,state.Round,actor.Id,target.Id));}
            if(target.Hp==0){target.Status=UnitStatus.Dead;target.ActionAvailable=false;target.OpportunityAttackAvailable=false;events.Add(new BattleEvent(BattleEventKind.UnitDied,state.Round,actor.Id,target.Id));}
            else if(direct&&melee&&type==DamageType.Physical&&dealt>0&&target.FireProtection&&actor.IsActive)AddBurn(state,target,actor,events);
            return dealt;
        }
        private static void AddBurn(BattleState state,UnitState actor,UnitState target,List<BattleEvent> events)
        {target.BurnStacks=Math.Min(3,target.BurnStacks+1);target.BurnTicks=2;events.Add(new BattleEvent(BattleEventKind.BurnApplied,state.Round,actor.Id,target.Id,after:target.BurnStacks));}
        private static void EndStatuses(BattleState state,UnitState unit,List<BattleEvent> events)
        {
            unit.GracefulExitTarget=null;
            if(unit.FrozenActivations>0){unit.FrozenActivations--;events.Add(new BattleEvent(BattleEventKind.FreezeEnded,state.Round,unit.Id));}
            if(unit.ExhaustedActivations>0){unit.ExhaustedActivations--;events.Add(new BattleEvent(BattleEventKind.ExhaustionChanged,state.Round,unit.Id,after:unit.ExhaustedActivations));}
        }
        private static void StartStatuses(BattleState state,UnitState unit,List<BattleEvent> events)
        {
            if(unit.BarrierActivations>0&&--unit.BarrierActivations==0){int before=unit.TemporaryBarrier;unit.TemporaryBarrier=0;unit.FireProtection=false;events.Add(new BattleEvent(BattleEventKind.BarrierChanged,state.Round,unit.Id,before:before));}
            if(unit.BurnTicks>0){events.Add(new BattleEvent(BattleEventKind.BurnTick,state.Round,unit.Id,amount:unit.BurnStacks*2));DealDamage(state,unit,unit,unit.BurnStacks*2,DamageType.Fire,false,false,events);if(--unit.BurnTicks==0)unit.BurnStacks=0;}
        }
    }
}

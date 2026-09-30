using System.Linq;
using RPG.Core;
namespace RPG.Presentation
{
    public sealed partial class BattlePresenter
    {
        public CombatLabMatch? Lab { get; private set; }
        private System.Collections.Generic.IReadOnlyCollection<GridPosition> spellPreviewCells;
        public SpellId? SelectedSpell { get; private set; }
        public void StartCombatLab(CombatLabMatch match,bool nearContact=true)
        {
            if(World!=null||Duel!=null)return;
            Lab=match;SelectedSpell=null;
            ConfigureBattle(CombatLab.Units(match,nearContact),CombatLab.Board(match),5051);
            Message="Combat Lab · "+match+" · "+(nearContact?"AUTHORED near-contact quick-start":"ordinary deployment")+". New session = full source budgets. Select ability, target, confirm.";
            Refresh();
        }
        public void SelectSpell(SpellId? spell){SelectedSpell=spell;ClearPreview();Refresh();}
        private bool PreviewSelectedSpell(GridPosition cell,bool friendly)
        {
            if(!SelectedSpell.HasValue)return false;
            var command=new CastCommand(State.CurrentUnitId.Value,SelectedSpell.Value,cell,friendly);
            var p=BattleResolver.PreviewSpell(State,command);
            spellPreviewCells=p.Cells.ToArray();
            var actor=State.FindUnit(command.Actor);
            PreviewText=command.Spell+" · Action"+(SpellRules.Exertion(command.Spell)?" + Exertion":"")
                +" · range "+SpellRules.Range(command.Spell)+" · uses "+SpellRules.Used(actor,command.Spell)+"/"+(SpellRules.Limit(command.Spell)==int.MaxValue?"—":SpellRules.Limit(command.Spell).ToString())
                +"\nCells: "+string.Join(" ",p.Cells.Select(Cell))+"\nAffected: "+string.Join(", ",p.Targets.Select(id=>UnitName(id)+(State.FindUnit(id).Side==actor.Side?" [ALLY / SELF]":"")))
                +(p.Magnitude>0?"\nMagnitude "+p.Magnitude:"\nStatus / protection effect")+" · contact "+p.ContactChance+"% · "+(p.IsLegal?"LEGAL":p.Error.ToString());
            pending=p.IsLegal?command:null;Refresh();return true;
        }
        public static string CombatStatuses(UnitState u)=>"Barrier "+u.TemporaryBarrier+(u.FireProtection?" Fire Armor":"")
            +(u.BurnStacks>0?" · Burn "+u.BurnStacks+" ("+u.BurnTicks+" ticks)":"")+(u.IsFrozen?" · FROZEN":"")+(u.IsExhausted?" · EXHAUSTED":"");
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using RPG.Core;
namespace RPG.Presentation
{
    public sealed partial class BattlePresenter
    {
        public CombatLabMatch? Lab { get; private set; }
        private IReadOnlyCollection<GridPosition> spellPreviewCells, spellBlockedCells;
        private IReadOnlyCollection<GridPosition> spellEnvelope=Array.Empty<GridPosition>();
        private GridPosition? aimHover, spellCenter;
        private SpellId? inspectedSpell;
        private int aimActor, aimRound;
        public SpellId? SelectedSpell { get; private set; }
        public bool StaffSelected { get; private set; }
        public IReadOnlyCollection<GridPosition> SpellEnvelope => spellEnvelope;
        public IReadOnlyCollection<GridPosition> SpellFootprint => spellPreviewCells??Array.Empty<GridPosition>();
        public IReadOnlyCollection<GridPosition> SpellObstructions => spellBlockedCells??Array.Empty<GridPosition>();
        public SpellId? PrimarySpell => State?.CurrentUnitId==null?null:Primary(State.FindUnit(State.CurrentUnitId.Value).Profile);
        private static SpellId? Primary(UnitProfile p)=>p.IsFireMage?SpellId.FireStream:p.IsIceMage?SpellId.IceShard:(SpellId?)null;
        private SpellId? AimSpell => inspectedSpell??SelectedSpell??(StaffSelected?null:PrimarySpell);
        public string ConfirmActionText => HasApproachPreview?"Confirm Approach + Attack":pending is CastCommand c?"Confirm "+c.Spell:HasMovePreview?"Confirm Move":"Confirm Attack";
        public string SpellDetails {
            get {
                if(State?.CurrentUnitId==null)return "";
                var actor=State.FindUnit(State.CurrentUnitId.Value);var spell=AimSpell;
                if(!spell.HasValue)return StaffSelected?"Staff Strike · Physical 5 · melee range 1 · Action. Explicit alternative; no automatic fallback.":"";
                var s=spell.Value;int limit=SpellRules.Limit(s);
                return (!actor.ActionAvailable?"UNAVAILABLE: Action spent; remaining legal Movement is separate. Action resets on the next activation.\n":"")+(inspectedSpell.HasValue?"Inspecting (selected action unchanged): ":SelectedSpell.HasValue?"Selected: ":"Primary attack: ")+s+"\n"+TargetDescription(s)
                    +" · Action"+(SpellRules.Exertion(s)?" + Exertion":" · Spell (not Exertion)")
                    +(limit==int.MaxValue?"":"\nRemaining "+Math.Max(0,limit-SpellRules.Used(actor,s))+"/"+limit+" · resets only on global Strategic Refresh")
                    +"\n"+(!actor.ActionAvailable?"Spell targeting is hidden until Action is available.":SelectedSpell.HasValue?"Hover for exact effect; click to pin, click the same cell again to cast. Cancel clears the pin.":"Hover to preview; first click pins, second click on the same enemy casts. Empty ground remains Move; allies are inspected.")
                    +"\nAmber brackets: legal aim geometry (recipient/status checked separately). Magenta: exact effect. Red X: excluded by obstruction. Allies are named below.";
            }
        }
        public static string TargetDescription(SpellId s)=>s==SpellId.FireArmor?"Target: Self / one friendly living unit · range 3 · 6 temporary Barrier; expires at recipient’s second next activation; no Armor repair":s==SpellId.FireStream?"Target: Direction through cell center · thin line · range 3 (diagonals included)":s==SpellId.Fireball?"Target: Ground cell, empty or occupied · center range 8 · blast radius 1":s==SpellId.IceShield?"Target: Self / friendly living unit · range 4":s==SpellId.CloseHeal?"Target: Self / adjacent friendly living unit · range 1":s==SpellId.Freeze?"Target: Hostile living unit · range 6":"Target: Hostile living unit · range 8";
        public void StartCombatLab(CombatLabMatch match,bool nearContact=true)
        {
            if(World!=null||Duel!=null)return;
            Lab=match;ResetAim();
            ConfigureBattle(CombatLab.Units(match,nearContact),CombatLab.Board(match),5051);
            Message="Combat Lab · "+match+" · "+(nearContact?"AUTHORED near-contact quick-start":"ordinary deployment")+". Hover previews; first click pins; second click on the same cell acts. Specials: select, then aim with the same two clicks.";
            Refresh();
        }
        private void ResetAim(){SelectedSpell=null;StaffSelected=false;inspectedSpell=null;}
        public void SelectSpell(SpellId? spell){ResetAim();SelectedSpell=spell;ClearPreview();Refresh();}
        public void SelectStaff(){ResetAim();StaffSelected=true;ClearPreview();Refresh();}
        public void InspectSpell(SpellId? spell){if(selected.HasValue)return;inspectedSpell=spell;ShowViews();}
        private SpellId? SpellForCell(GridPosition cell)
        {
            if(SelectedSpell.HasValue)return SelectedSpell;
            if(StaffSelected)return null;
            var actor=State.FindUnit(State.CurrentUnitId.Value);var target=State.OccupantAt(cell);
            return target!=null&&target.Side!=actor.Side?Primary(actor.Profile):null;
        }
        // Hover is transient; the first click pins, the second click on the same cell commits.
        public void ClickCell(GridPosition cell)
        {
            if(State==null||State.Outcome.IsEnded||IsAiTurn)return;
            if(selected==cell&&pending!=null){ConfirmPreview();return;}
            inspectedSpell=null;
            SelectCell(cell);
        }
        public void HoverCell(GridPosition cell)
        {
            if(State==null||State.Outcome.IsEnded||IsAiTurn||selected.HasValue||aimHover==cell)return;
            PreviewCell(cell,false,false);
        }
        public void LeaveBoard()
        {
            if(selected.HasValue)return;
            ClearPreview();ShowViews();
        }
        private bool PreviewSelectedSpell(GridPosition cell,bool friendly)
        {
            var spell=SpellForCell(cell);if(!spell.HasValue)return false;
            PreviewSpellAt(cell,spell.Value,friendly,true);ShowViews();return true;
        }
        private void PreviewSpellAt(GridPosition cell,SpellId spell,bool friendly,bool select)
        {
            var command=new CastCommand(State.CurrentUnitId.Value,spell,cell,friendly);
            var p=BattleResolver.PreviewSpell(State,command);var actor=State.FindUnit(command.Actor);
            spellPreviewCells=p.Cells.ToArray();spellBlockedCells=p.BlockedCells.ToArray();spellCenter=cell;
            // A default attack must actually hit the hovered enemy, never silently aim past it.
            bool outside=spell==SpellId.FireStream&&!p.Cells.Contains(cell);
            PreviewText=spell+" · "+TargetDescription(spell)
                +"\nDistance "+actor.Position.DistanceTo(cell)+" / "+SpellRules.Range(spell,State.FireRulesVersion)
                +"\n"+(p.IsLegal&&!outside?"LEGAL": "Blocked: "+(p.Blockers.Count>0?Reason(p.Blockers[0],spell):"Outside stream footprint"))
                +"\nEffect cells: "+string.Join(" ",p.Cells.Select(Cell))
                +"\nAffected: "+(p.Targets.Count==0?"none (a legal empty area cast still spends Action/budget)":string.Join(", ",p.Targets.Select(id=>UnitName(id)+(State.FindUnit(id).Side==actor.Side?(SpellRules.Area(spell)?" [ALLY / SELF — friendly fire]":" [FRIENDLY / SELF]"):""))))
                +(p.BlockedCells.Count>0?"\nObstructed/excluded: "+string.Join(" ",p.BlockedCells.Select(Cell)):"")
                +(p.Blockers.Count>0?"\nAll blockers: "+string.Join("; ",p.Blockers.Select(e=>Reason(e,spell))):"")
                +(p.Magnitude>0?"\nMagnitude "+p.Magnitude:"\nStatus / protection effect")+" · contact "+p.ContactChance+"%";
            // Explicit direction may intentionally hit cells before the cursor obstruction; Core still validates the selected aim.
            if(select)pending=p.IsLegal&&(!outside||SelectedSpell.HasValue)?command:null;
        }
        private static string Reason(CommandError e,SpellId s)
        {
            switch(e){case CommandError.SelfOnly:return "Legacy Self-only recipient restriction";case CommandError.OutsideSpellLine:return "Choose a direction cell other than the caster cell";case CommandError.InvalidSpellTarget:return "Wrong target — "+TargetDescription(s);case CommandError.TargetNotFound:return "A living target is required";case CommandError.NoAction:return "Action spent";case CommandError.Silenced:return "Silence blocks Spell actions (staff remains an explicit alternative)";case CommandError.Exhausted:return "Exhausted blocks this Exertion action";case CommandError.SourceBudgetSpent:return "Source budget depleted until global Strategic Refresh";case CommandError.OutOfRange:return "Out of range";case CommandError.BlockedLineOfSight:return "Line of sight blocked / sealed crossing";case CommandError.SolidCell:return "Solid cell cannot be targeted";case CommandError.FriendlyFireNotConfirmed:return "Affected allies/self — explicit Friendly Fire confirmation required";case CommandError.NoUsefulEffect:return "No missing HP or supported harmful condition to cleanse";default:return e.ToString();}
        }
        private void RefreshSpellEnvelope()
        {
            var id=State.CurrentUnitId;int current=id?.Value??0;
            if(current!=aimActor||State.Round!=aimRound){ResetAim();spellPreviewCells=null;spellBlockedCells=null;spellCenter=null;aimHover=null;aimActor=current;aimRound=State.Round;}
            bool actionAvailable=id.HasValue&&State.FindUnit(id.Value).ActionAvailable;
            if(!actionAvailable){spellPreviewCells=null;spellBlockedCells=null;spellCenter=null;}
            spellEnvelope=!State.Outcome.IsEnded&&!IsAiTurn&&actionAvailable&&AimSpell.HasValue?BattleResolver.SpellAimCells(State,id.Value,AimSpell.Value):Array.Empty<GridPosition>();
        }
        public static string CombatStatuses(UnitState u)=>"Barrier "+u.TemporaryBarrier+(u.FireProtection?" Fire Armor":"")
            +(u.BurnStacks>0?" · Burn "+u.BurnStacks+" ("+u.BurnTicks+" ticks)":"")+(u.IsFrozen?" · FROZEN":"")+(u.IsExhausted?" · EXHAUSTED":"")+(u.IsSilenced?" · SILENCED":"");
    }
}

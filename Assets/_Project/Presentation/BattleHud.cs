using System;
using System.Collections.Generic;
using System.Linq;
using RPG.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPG.Presentation
{
    public sealed class BattleHud
    {
        private readonly BattlePresenter presenter;
        private readonly Camera camera;
        private readonly VisualElement surface;
        private readonly Label aiInfo, rangeInfo;
        private readonly Label active, queue, preview, message, events, hover, cell;
        private readonly Button confirm, cancel, defend, end;
        private readonly Label riskWarning, retreat, escaped, outcomeText;
        private readonly Label persistence, attackOutcome;
        private readonly Button persistenceContinue;
        private readonly VisualElement outcomePanel;
        private readonly ScrollView panel;
        private bool showedOutcome;
        private readonly Label westEdge, eastEdge;
        private readonly DropdownField finalFacing, map;
        private readonly Toggle friendly;
        private GridPosition? hoveredCell;
        private readonly Dictionary<UnitId, UnitConditionView> unitLabels = new Dictionary<UnitId, UnitConditionView>();
        private readonly List<(Label label, Vector3 position)> coordinates = new List<(Label, Vector3)>();
        public VisualElement Root { get; }

        public BattleHud(UIDocument document, BattlePresenter presenter, Camera camera)
        {
            this.presenter = presenter; this.camera = camera;
            Root = document.rootVisualElement; Root.name = "graybox-root";
            Root.focusable=true;Root.RegisterCallback<KeyDownEvent>(e=>{if(e.keyCode==KeyCode.Escape){presenter.CancelPreview();e.StopPropagation();}});
            Root.style.flexDirection = FlexDirection.Row; Root.style.flexGrow = 1;
            surface = new VisualElement { name = "board-input" };
            surface.style.width = Length.Percent(70); surface.style.height = Length.Percent(100);
            surface.style.overflow = Overflow.Hidden; Root.Add(surface);
            var title = Text(surface, "GATE C / HOTSEAT", 22); title.style.position = Position.Absolute;
            title.name="battle-title"; title.style.left = 20; title.style.top = 16; title.pickingMode = PickingMode.Ignore;
            var legend = Text(surface, "BLUE West · ORANGE East · GOLD active\nGreen: reachable · Gold: path · Red segment: OA risk\nSword + shield: HW/EW · Bow: HA · * Commander\nWhite arrow: facing · Red border: ZoC ready · Gray: spent\nHover: preview · Click: pin · Same cell again: execute\nWheel: zoom · Right-click / Escape: cancel", 13);
            legend.style.position = Position.Absolute; legend.style.left = 20; legend.style.bottom = 16; legend.pickingMode = PickingMode.Ignore;
            westEdge = Text(surface, "West Retreat", 13); eastEdge = Text(surface, "East Retreat", 13);
            westEdge.style.position = eastEdge.style.position = Position.Absolute;
            westEdge.style.left = 20; eastEdge.style.right = 20;
            westEdge.style.top = eastEdge.style.top = 49;
            westEdge.pickingMode = eastEdge.pickingMode = PickingMode.Ignore;
            surface.RegisterCallback<WheelEvent>(e => { presenter.Zoom(e.delta.y > 0 ? 1.12f : .89f); e.StopPropagation(); });
            surface.RegisterCallback<PointerMoveEvent>(e => {
                if (presenter.State != null && Pick(surface.WorldToLocal(e.position), out var p)) { hoveredCell = p; presenter.HoverCell(p); hover.text = presenter.SelectedSpell.HasValue?"Aim cell "+BattlePresenter.Cell(p):presenter.Hover(p); }
                else {hoveredCell=null;presenter.LeaveBoard();}
            });
            surface.RegisterCallback<PointerLeaveEvent>(e=>presenter.LeaveBoard());
            surface.RegisterCallback<PointerDownEvent>(e => {
                if (presenter.State == null || !Pick(surface.WorldToLocal(e.position), out var p)) return;
                if (e.button == 1) { presenter.CancelPreview(); e.StopPropagation(); return; }
                if (e.button != 0) return;
                Root.Focus();
                if(presenter.PinnedCell!=p)friendly.SetValueWithoutNotify(false); presenter.ClickCell(p); e.StopPropagation();
            });

            panel = new ScrollView { name = "battle-panel" };
            panel.style.width = Length.Percent(30); panel.style.height = Length.Percent(100);
            panel.style.backgroundColor = new Color(.075f, .105f, .14f);
            panel.style.paddingLeft = panel.style.paddingRight = 14;
            panel.style.paddingTop = panel.style.paddingBottom = 12; Root.Add(panel);
            Text(panel, "TACTICAL GRAYBOX", 20);
            Text(panel, "Hotseat · ZoC / OA / physical Retreat", 12);
            outcomePanel = new VisualElement { name = "outcome-panel" };
            outcomePanel.style.backgroundColor = new Color(.16f, .23f, .26f);
            outcomePanel.style.paddingLeft = outcomePanel.style.paddingRight = 8;
            outcomePanel.style.paddingTop = 8; panel.Add(outcomePanel);
            outcomeText = Text(outcomePanel, "", 15); outcomeText.name = "outcome-summary";
            AddButton(outcomePanel, "Restart Same Seed", "outcome-restart", presenter.RestartSameSeed);
            AddButton(outcomePanel, "Return persistent result to World", "world-return", presenter.ReturnToWorld);
            var control = new DropdownField("Controller",new List<string>{"Hotseat","Player West vs AI East","Player East vs AI West"},0) { name="controller-mode" };
            control.labelElement.style.color=new Color(.89f,.93f,.97f);
            control.RegisterValueChangedCallback(e=>presenter.SetPlayerVsAi(control.index!=0,control.index==2?Side.West:Side.East));panel.Add(control);
            aiInfo=Text(panel,"",12);aiInfo.name="ai-info";
            active = Text(panel, "", 16); active.name = "active-unit";
            var abilities=new DropdownField("Ability",new List<string>{"Basic / Move"},0){name="spell-selector"};
            abilities.RegisterValueChangedCallback(e=>presenter.SelectSpell((e.newValue=="Basic / Move"||e.newValue=="Primary / Move")?(SpellId?)null:(SpellId)Enum.Parse(typeof(SpellId),e.newValue)));abilities.labelElement.style.color=new Color(.89f,.93f,.97f);panel.Add(abilities);
            var primary=AddButton(panel,"Primary attack","primary-attack",()=>presenter.SelectSpell(null));
            primary.RegisterCallback<PointerEnterEvent>(e=>presenter.InspectSpell(presenter.PrimarySpell));primary.RegisterCallback<PointerLeaveEvent>(e=>presenter.InspectSpell(null));
            AddButton(panel,"Staff Strike · melee 1 · explicit alternative","staff-attack",presenter.SelectStaff);
            var spellButtons=new VisualElement{name="spell-buttons"};spellButtons.style.flexDirection=FlexDirection.Row;spellButtons.style.flexWrap=Wrap.Wrap;panel.Add(spellButtons);
            foreach(SpellId spell in Enum.GetValues(typeof(SpellId))){var chosen=spell;var b=AddButton(spellButtons,spell+(spell==SpellId.FireArmor?" · Self / Ally":""),"spell-"+spell,()=>presenter.SelectSpell(chosen));b.style.width=Length.Percent(48);b.tooltip=BattlePresenter.TargetDescription(spell);b.RegisterCallback<PointerEnterEvent>(e=>presenter.InspectSpell(chosen));b.RegisterCallback<PointerLeaveEvent>(e=>presenter.InspectSpell(null));}
            var aimPanel=new VisualElement{name="spell-aim-panel"};panel.Add(aimPanel);
            var details=Text(aimPanel,"",12);details.name="spell-details";
            attackOutcome = Text(panel, "", 14); attackOutcome.name = "attack-outcome";
            attackOutcome.style.color = new Color(1, .8f, .35f);
            rangeInfo = Text(panel, "", 12); rangeInfo.name="ranged-reach-info";
            rangeInfo.style.color=new Color(.3f,.85f,1f);
            queue = Text(panel, "", 12); queue.name = "activation-queue";
            retreat = Text(panel, "", 13); retreat.name = "retreat-info";
            escaped = Text(panel, "", 12); escaped.name = "escaped-list";
            hover = Text(panel, "Hover the battlefield.", 12);
            cell = Text(panel, "", 12);
            preview = Text(panel, "", 14); preview.name = "command-preview";
            friendly = new Toggle("Explicitly confirm allied target");
            friendly.RegisterValueChangedCallback(e => presenter.Repreview(e.newValue)); panel.Add(friendly);
            riskWarning = Text(panel, "", 14); riskWarning.name = "oa-warning";
            riskWarning.style.color = new Color(1, .68f, .4f);
            confirm = AddButton(panel, "Confirm selected action", "confirm-command", presenter.ConfirmPreview);
            cancel = AddButton(panel, "Cancel preview", "cancel-preview", presenter.CancelPreview);
            defend = AddButton(panel, "Defend", "defend", presenter.Defend);
            finalFacing = new DropdownField("End facing", new List<string> { "Keep current", "North", "NorthEast", "East", "SouthEast", "South", "SouthWest", "West", "NorthWest" }, 0);
            panel.Add(finalFacing);
            friendly.labelElement.style.color = finalFacing.labelElement.style.color = new Color(.89f, .93f, .97f);
            end = AddButton(panel, "End Activation", "end-activation", () => presenter.EndActivation(finalFacing.index == 0 ? (Facing?)null : (Facing)(finalFacing.index - 1)));
            message = Text(panel, "", 14); message.name = "battle-message"; message.style.color = new Color(1, .8f, .35f);
            foreach(var element in new VisualElement[]{hover,cell,preview,friendly,riskWarning,confirm,cancel})aimPanel.Add(element);
            map = new DropdownField("Fixture (resets battle)", new List<string>(Enum.GetNames(typeof(SizeExperimentMap))), 0) { name = "fixture-selector" };
            map.labelElement.style.color=new Color(.89f,.93f,.97f);
            map.RegisterValueChangedCallback(e => presenter.ConfigureFixture((SizeExperimentMap)Enum.Parse(typeof(SizeExperimentMap), e.newValue))); panel.Add(map);
            foreach(CombatLabMatch lab in Enum.GetValues(typeof(CombatLabMatch))) {
                var chosen=lab;AddButton(panel,"Combat Lab · "+lab+" · near contact","lab-"+lab,()=>presenter.StartCombatLab(chosen));
            }
            var westPreset=new DropdownField("05B West preset",Enum.GetNames(typeof(CombatPreset)).ToList(),0);westPreset.labelElement.style.color=new Color(.89f,.93f,.97f);panel.Add(westPreset);
            var eastPreset=new DropdownField("05B East preset",Enum.GetNames(typeof(CombatPreset)).ToList(),1);eastPreset.labelElement.style.color=new Color(.89f,.93f,.97f);panel.Add(eastPreset);
            var firstSide=new DropdownField("05 Starting Side",new List<string>{"West","East"},0);firstSide.labelElement.style.color=new Color(.89f,.93f,.97f);panel.Add(firstSide);
            AddButton(panel,"City Foundations 05A","city-start-a",()=>presenter.StartCity(first:(Side)firstSide.index));
            AddButton(panel,"City & Combat 05B · permanent presets","city-start-b",()=>presenter.StartCity(true,(CombatPreset)westPreset.index,(CombatPreset)eastPreset.index,(Side)firstSide.index));
            AddButton(panel,"Load AUTHORED 05B inspection fixture (not a played match)","city-authored",()=>presenter.LoadDuel(System.IO.Path.Combine(Application.streamingAssetsPath,"CityCombat05B","authored-ready.json")));
            AddButton(panel,"Realm Operations 06 · West first","realm-start-west",()=>presenter.StartRealm());
            AddButton(panel,"Realm Operations 06 · East first","realm-start-east",()=>presenter.StartRealm(Side.East));
            AddButton(panel,"Realm 06 Ice vs Fire · West first","realm-ice-west",()=>presenter.StartRealm(Side.West,CombatPreset.Ice,CombatPreset.Fire));
            AddButton(panel,"Realm 06 Ice vs Fire · East first","realm-ice-east",()=>presenter.StartRealm(Side.East,CombatPreset.Ice,CombatPreset.Fire));
            AddButton(panel,"Load Realm Operations 06","realm-load-slot",()=>presenter.LoadDuel(BattlePresenter.RealmSlot));
            AddButton(panel,"Load City Foundations 05A","city-load-a",()=>presenter.LoadDuel(BattlePresenter.CitySlot(false)));
            AddButton(panel,"Load City & Combat 05B","city-load-b",()=>presenter.LoadDuel(BattlePresenter.CitySlot(true)));
            AddButton(panel,"Start Persistence Slice v0.1","persistence-start",presenter.StartPersistenceSlice);
            AddButton(panel,"Start Connected Mission 01","world-start",presenter.StartStrategicScenario);
            AddButton(panel,"Load saved Mission 01","world-load",()=>presenter.LoadStrategic());
            AddButton(panel,"Crossroads economy Hotseat · West first","duel-start-west",()=>presenter.StartDuel(Side.West));
            AddButton(panel,"Crossroads economy Hotseat · East first","duel-start-east",()=>presenter.StartDuel(Side.East));
            AddButton(panel,"Crossroads Incident 04 · West first","incident-start-west",()=>presenter.StartIncident(Side.West));
            AddButton(panel,"Crossroads Incident 04 · East first","incident-start-east",()=>presenter.StartIncident(Side.East));
            AddButton(panel,"Incident 04 · control (no incidents)","incident-control",()=>presenter.StartIncident(Side.West,false));
            AddButton(panel,"Load Incident 04","incident-load",()=>presenter.LoadIncident());
            AddButton(panel,"Load Crossroads Hotseat","duel-load",()=>presenter.LoadDuel());
            persistence=Text(panel,"",12);persistence.name="persistence-summary";
            persistenceContinue=AddButton(panel,"Continue Persistence Battle","persistence-continue",presenter.ContinuePersistenceSlice);
            Text(panel, "Size/density experiment · no combat retuning. 9v9 = synthetic tactical roster, not strategic Capacity validation. Siege: static fortress; moat proxy has fixed crossings. 41×39 preserves each attacker approach; West = attacker coalition, East = defenders. No real siege mechanics.", 12);
            AddButton(panel, "Fit whole board", "fit-board", presenter.FitBoard);
            AddButton(panel, "Focus active unit (wheel to zoom)", "focus-unit", presenter.FocusActor);
            AddButton(panel, "Restart Same Seed", "restart", presenter.RestartSameSeed);
            Text(panel,"LOCAL REPLAY / TELEMETRY",14);
            var replayPath=new TextField("Replay file") { name="replay-path" };
            replayPath.labelElement.style.color=new Color(.89f,.93f,.97f);panel.Add(replayPath);
            AddButton(panel,"Export battle + session","export-replay",()=> { var path=presenter.ExportReplay();if(path!=null)replayPath.value=path; });
            AddButton(panel,"Load / verify replay file","verify-replay",()=>presenter.VerifyReplay(replayPath.value));
            Text(panel, "RECENT CORE EVENTS", 14);
            events = Text(panel, "", 11); events.name = "battle-events";
        }

        public void Resize(Battlefield board)
        {
            hoveredCell = null;
            foreach (var axis in coordinates) axis.label.RemoveFromHierarchy();
            coordinates.Clear();
            for (int x = 0; x < board.Columns; x++) Axis(x.ToString(), new Vector3(x, 0, board.Rows - .25f));
            for (int y = 0; y < board.Rows; y++) Axis(y.ToString(), new Vector3(-.8f, 0, y));
            map.SetValueWithoutNotify(presenter.Fixture.ToString());
        }
        public void ResetChoices() { friendly.SetValueWithoutNotify(false); finalFacing.SetValueWithoutNotify("Keep current"); }
        public void Refresh(BattleState state, bool canConfirm, GridPosition? selected)
        {
            var actor = state.FindUnit(state.CurrentUnitId.Value);
            if (hoveredCell.HasValue) hover.text = presenter.SelectedSpell.HasValue?"Aim cell "+BattlePresenter.Cell(hoveredCell.Value):presenter.Hover(hoveredCell.Value);
            bool ended = state.Outcome.IsEnded;
            bool playerTurn = !presenter.IsAiTurn;
            bool connected=presenter.World!=null||presenter.Duel!=null;
            bool incidentBattle=presenter.Duel?.Incident!=null||presenter.Duel?.Realm!=null;
            Root.Q<Label>("battle-title").text=incidentBattle?(presenter.Duel?.Realm!=null?"REALM 06 · Blue: ":"INCIDENT 04 · Blue: ")+presenter.TacticalSideLabel(Side.West)+" / Orange: "+presenter.TacticalSideLabel(Side.East):"GATE C / HOTSEAT";
            Root.Q("world-return").style.display=connected?DisplayStyle.Flex:DisplayStyle.None;
            foreach(string controlName in new[]{"fixture-selector","controller-mode","persistence-start","restart","outcome-restart","world-start","world-load","duel-start-west","duel-start-east","duel-load","incident-start-west","incident-start-east","incident-control","incident-load","city-start-a","city-start-b","city-load-a","city-load-b","city-authored","realm-start-west","realm-start-east","realm-load-slot","realm-ice-west","realm-ice-east"})Root.Q(controlName).SetEnabled(!connected);
            Root.Q<DropdownField>("controller-mode").SetValueWithoutNotify(presenter.PlayerVsAi?(presenter.AiSide==Side.East?"Player West vs AI East":"Player East vs AI West"):"Hotseat");
            var spellSelect=Root.Q<DropdownField>("spell-selector");
            string ordinary=presenter.PrimarySpell.HasValue?"Primary / Move":"Basic / Move";
            spellSelect.choices=new[]{ordinary}.Concat(SpellRules.Kit(actor.Profile).Select(s=>s.ToString())).ToList();
            spellSelect.SetValueWithoutNotify(presenter.SelectedSpell?.ToString()??ordinary);
            Root.Q<Button>("primary-attack").text=presenter.PrimarySpell.HasValue?"Primary: "+presenter.PrimarySpell+" · select, then click again":"Basic / Move";
            Root.Q("primary-attack").SetEnabled(playerTurn&&!ended&&(!presenter.PrimarySpell.HasValue||actor.ActionAvailable));Root.Q("staff-attack").style.display=actor.Profile.IsCaster?DisplayStyle.Flex:DisplayStyle.None;Root.Q("staff-attack").SetEnabled(playerTurn&&!ended&&actor.ActionAvailable);
            Root.Q<Label>("spell-details").text=presenter.SpellDetails;
            spellSelect.SetEnabled(playerTurn&&!ended&&actor.ActionAvailable);
            foreach(SpellId spell in Enum.GetValues(typeof(SpellId))){var b=Root.Q<Button>("spell-"+spell);b.style.display=SpellRules.Has(actor.Profile,spell)?DisplayStyle.Flex:DisplayStyle.None;b.SetEnabled(playerTurn&&!ended&&actor.ActionAvailable);}

            foreach(CombatLabMatch lab in Enum.GetValues(typeof(CombatLabMatch)))Root.Q("lab-"+lab).SetEnabled(!connected);
            aiInfo.text=presenter.PlayerVsAi?presenter.AiExplanation:"Hotseat";
            rangeInfo.text=presenter.RangedReachMessage;
            attackOutcome.text = presenter.LastAttackOutcome.Length == 0 ? "" : "LAST ATTACK RESULT\n" + presenter.LastAttackOutcome;
            attackOutcome.style.display = attackOutcome.text.Length == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            rangeInfo.style.display=rangeInfo.text.Length>0?DisplayStyle.Flex:DisplayStyle.None;
            if (ended != showedOutcome) panel.schedule.Execute(() => panel.scrollOffset = Vector2.zero);
            showedOutcome = ended;
            outcomePanel.style.display = ended ? DisplayStyle.Flex : DisplayStyle.None;
            outcomeText.text = ended ? "BATTLE ENDED\nWinner: " + presenter.TacticalSideLabel(state.Outcome.VictorySide) + "\nLoser: " + presenter.TacticalSideLabel(state.Outcome.DefeatedSide)
                + "\nResult: " + state.Outcome.Reason + "\n\nDead:\n" + Roster(state, UnitStatus.Dead)
                + "\n\nEscaped/Safe:\n" + Roster(state, UnitStatus.Escaped)
                + "\n\nSurviving active units:\n" + Roster(state, UnitStatus.Active) : "";
            persistence.text=presenter.PersistenceSummary;
            persistence.style.display=presenter.PersistenceActive?DisplayStyle.Flex:DisplayStyle.None;
            persistenceContinue.style.display=presenter.PersistenceActive?DisplayStyle.Flex:DisplayStyle.None;
            persistenceContinue.SetEnabled(presenter.CanContinuePersistence);
            active.text = ended ? "No active turn — battle completed." : "ROUND " + state.Round + " · " + presenter.UnitName(actor.Id) + "\n" + actor.Side + (actor.OwnRetreatEdge.HasValue?" · "+actor.OwnRetreatEdge+" approach":"")
                + " | HP " + actor.Hp + " / Armor " + actor.Armor + "\nMovement " + actor.MovementRemaining
                + " | Action " + (actor.ActionAvailable ? "available" : "spent")
                + "\nFacing " + actor.Facing + " | Defending " + (actor.IsDefending ? "yes" : "no") + " | " + BattlePresenter.OaStatus(actor);
            active.text+="\n"+presenter.ConnectedArmyName(actor.Id)+"\n"+BattlePresenter.CombatStatuses(actor);
            queue.text = ended ? "" : "Initiative order (► current):\n" + string.Join("\n", state.ActivationOrder.Select(id =>
                (id == actor.Id ? "► " : "   ") + presenter.UnitName(id) + (state.FindUnit(id).OwnRetreatEdge.HasValue?" ("+state.FindUnit(id).OwnRetreatEdge+")":"") + " [" + state.FindUnit(id).Profile.Initiative + "] " + BattlePresenter.OaStatus(state.FindUnit(id))));
            cell.text = selected.HasValue ? "Pinned — click again to act (" + selected.Value.X + "," + selected.Value.Y + ")" : "No destination / target selected.";
            preview.text = presenter.PreviewText; confirm.SetEnabled(canConfirm && playerTurn); message.text = presenter.Message;
            int risks = presenter.OpportunityRiskCount;
            riskWarning.text = risks > 0 ? "This path may trigger " + risks + " Opportunity Attack(s). Confirm to accept the risk, or Cancel." : "";
            riskWarning.style.display = risks > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            confirm.text = pendingSpellText();
            string pendingSpellText()=>presenter.HasMovePreview ? (risks > 0 ? "Confirm Move — accept " + risks + " OA risk(s)" : "Confirm Move") : presenter.ConfirmActionText;
            cancel.SetEnabled(playerTurn&&!ended);
            defend.SetEnabled(playerTurn && !ended && BattleResolver.Validate(state, new DefendCommand(actor.Id)) == CommandError.None);
            end.SetEnabled(!ended && playerTurn); finalFacing.SetEnabled(!ended); friendly.SetEnabled(!ended);
            surface.SetEnabled(!ended && playerTurn);
            escaped.text = "Escaped/Safe:\n" + Roster(state, UnitStatus.Escaped);
            string eastZone = state.Battlefield.EastRetreatUsesPerimeter ? "full legal outer perimeter" : "East edge";
            retreat.text = "West: West edge. East: " + eastZone + "." + (ended ? "" : "\nYOUR escape: " + (actor.Side == Side.West ? "West edge" : eastZone));
            string approachEdges = string.Join(" / ",state.Units.Where(u=>u.Side==Side.West && u.OwnRetreatEdge.HasValue).Select(u=>u.OwnRetreatEdge.Value).Distinct());
            if(approachEdges.Length>0) retreat.text="West rear edges (per army): "+approachEdges+". East: "+eastZone+"."
                +(ended?"":"\nYOUR Retreat: "+(actor.OwnRetreatEdge.HasValue?actor.OwnRetreatEdge+" edge":eastZone));
            westEdge.text = "← West Retreat" + (!ended && actor.Side == Side.West ? " — YOUR ESCAPE" : "");
            eastEdge.text = (state.Battlefield.EastRetreatUsesPerimeter ? "East: ALL outer edges" : "East Retreat →") + (!ended && actor.Side == Side.East ? " — YOUR ESCAPE" : "");
            if(approachEdges.Length>0)westEdge.text=actor.OwnRetreatEdge.HasValue?"Attacker coalition · YOUR retreat: "+actor.OwnRetreatEdge:"Attacker retreat: "+approachEdges;
            if(incidentBattle)
            {
                string blue=string.Join(" / ",state.Units.Where(u=>u.Side==Side.West).Select(u=>u.OwnRetreatEdge).Distinct());
                string orange=string.Join(" / ",state.Units.Where(u=>u.Side==Side.East).Select(u=>u.OwnRetreatEdge).Distinct());
                westEdge.text="Blue rear: "+blue;eastEdge.text="Orange rear: "+orange;
                retreat.text="Blue ("+presenter.TacticalSideLabel(Side.West)+"): "+blue+". Orange ("+presenter.TacticalSideLabel(Side.East)+"): "+orange+"."+(ended?"":"\nCurrent unit Retreat: "+actor.OwnRetreatEdge);
            }
            if(!ended&&actor.OwnRetreatEdge==RetreatEdge.Unavailable)
            {
                retreat.text="Retreat unavailable for this formation: negative Strategic Tempo until Refresh.";
                if(actor.Side==Side.West)westEdge.text="West Retreat unavailable";else eastEdge.text="East Retreat unavailable";
            }
            events.text = string.Join("\n", presenter.RecentEvents);
            foreach (var label in unitLabels.Values) label.style.display = DisplayStyle.None;
            foreach (var unit in state.Units)
            {
                if (!unitLabels.TryGetValue(unit.Id, out var label))
                {
                    label = new UnitConditionView {name="unit-condition-"+unit.Id.Value};
                    surface.Add(label);unitLabels.Add(unit.Id, label);
                }
                label.Refresh(unit,presenter.IsCommander(unit.Id),!ended&&state.CurrentUnitId==unit.Id);
            }
            PositionLabels(state);
        }
        private string Roster(BattleState state, UnitStatus status)
        {
            var units = state.Units.Where(u => u.Status == status).ToArray();
            return units.Length == 0 ? "None" : string.Join("\n", units.Select(u => presenter.UnitName(u.Id)
                + " (" + u.Side + ") HP " + u.Hp + " / Armor " + u.Armor));
        }
        public void PositionLabels(BattleState state)
        {
            foreach (var unit in state.Units)
            {
                if (!unitLabels.TryGetValue(unit.Id, out var label)) continue;
                float cellPixels = surface.contentRect.height / (2 * camera.orthographicSize);
                float width=label.SizeForCell(cellPixels);
                Place(label, BattleGridView.World(unit.Position), -width/2, cellPixels*.10f);

            }
            foreach (var axis in coordinates) Place(axis.label, axis.position, -10, -10);
        }
        private void Place(VisualElement element, Vector3 position, float dx, float dy)
        {
            var point = camera.WorldToViewportPoint(position);
            element.style.left = point.x * surface.contentRect.width + dx;
            element.style.top = (1 - point.y) * surface.contentRect.height + dy;
        }
        private bool Pick(Vector2 local, out GridPosition cellPosition)
        {
            cellPosition = default;
            if (surface.contentRect.width <= 0 || surface.contentRect.height <= 0) return false;
            var ray = camera.ViewportPointToRay(new Vector3(local.x / surface.contentRect.width, 1 - local.y / surface.contentRect.height, 0));
            if (!new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float distance)) return false;
            var point = ray.GetPoint(distance);
            cellPosition = new GridPosition(Mathf.FloorToInt(point.x + .5f), Mathf.FloorToInt(point.z + .5f));
            return presenter.State.Battlefield.Contains(cellPosition);
        }
        private void Axis(string text, Vector3 position)
        {
            var label = Text(surface, text, 12); label.style.position = Position.Absolute;
            label.style.width = 20; label.style.height = 20; label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.pickingMode = PickingMode.Ignore; coordinates.Add((label, position));
        }
        private static Label Text(VisualElement parent, string value, int size)
        {
            var label = new Label(value); label.style.whiteSpace = WhiteSpace.Normal; label.style.fontSize = size;
            label.style.color = new Color(.89f, .93f, .97f); label.style.marginBottom = 7;
            parent.Add(label); return label;
        }
        private static Button AddButton(VisualElement parent, string text, string name, Action action)
        {
            var button = new Button(action) { text = text, name = name };
            button.style.minHeight = 31; button.style.marginBottom = 5; parent.Add(button); return button;
        }
    }
}

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
        private readonly VisualElement outcomePanel;
        private readonly ScrollView panel;
        private bool showedOutcome;
        private readonly Label westEdge, eastEdge;
        private readonly DropdownField finalFacing, map;
        private readonly Toggle friendly;
        private GridPosition? hoveredCell;
        private readonly Dictionary<UnitId, Label> unitLabels = new Dictionary<UnitId, Label>();
        private readonly List<(Label label, Vector3 position)> coordinates = new List<(Label, Vector3)>();
        public VisualElement Root { get; }

        public BattleHud(UIDocument document, BattlePresenter presenter, Camera camera)
        {
            this.presenter = presenter; this.camera = camera;
            Root = document.rootVisualElement; Root.name = "graybox-root";
            Root.style.flexDirection = FlexDirection.Row; Root.style.flexGrow = 1;
            surface = new VisualElement { name = "board-input" };
            surface.style.width = Length.Percent(70); surface.style.height = Length.Percent(100);
            surface.style.overflow = Overflow.Hidden; Root.Add(surface);
            var title = Text(surface, "GATE C / HOTSEAT", 22); title.style.position = Position.Absolute;
            title.style.left = 20; title.style.top = 16; title.pickingMode = PickingMode.Ignore;
            var legend = Text(surface, "BLUE West · ORANGE East · GOLD active\nGreen: reachable · Gold: path · Red segment: OA risk\nHW square · HA circle · EW diamond · * Commander\nThin red borders: enemy ZoC ready · Gray: spent\nWheel: zoom · Right-click: center view", 13);
            legend.style.position = Position.Absolute; legend.style.left = 20; legend.style.bottom = 16; legend.pickingMode = PickingMode.Ignore;
            westEdge = Text(surface, "West Retreat", 13); eastEdge = Text(surface, "East Retreat", 13);
            westEdge.style.position = eastEdge.style.position = Position.Absolute;
            westEdge.style.left = 20; eastEdge.style.right = 20;
            westEdge.style.top = eastEdge.style.top = 49;
            westEdge.pickingMode = eastEdge.pickingMode = PickingMode.Ignore;
            surface.RegisterCallback<WheelEvent>(e => { presenter.Zoom(e.delta.y > 0 ? 1.12f : .89f); e.StopPropagation(); });
            surface.RegisterCallback<PointerMoveEvent>(e => {
                if (presenter.State != null && Pick(surface.WorldToLocal(e.position), out var p)) { hoveredCell = p; hover.text = presenter.Hover(p); }
            });
            surface.RegisterCallback<PointerDownEvent>(e => {
                if (presenter.State == null || !Pick(surface.WorldToLocal(e.position), out var p)) return;
                if (e.button == 1) { presenter.CenterView(p); e.StopPropagation(); return; }
                if (e.button != 0) return;
                friendly.SetValueWithoutNotify(false); presenter.SelectCell(p); e.StopPropagation();
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
            var control = new DropdownField("Controller",new List<string>{"Hotseat","Player West vs AI East","Player East vs AI West"},0) { name="controller-mode" };
            control.labelElement.style.color=new Color(.89f,.93f,.97f);
            control.RegisterValueChangedCallback(e=>presenter.SetPlayerVsAi(control.index!=0,control.index==2?Side.West:Side.East));panel.Add(control);
            aiInfo=Text(panel,"",12);aiInfo.name="ai-info";
            active = Text(panel, "", 16); active.name = "active-unit";
            rangeInfo = Text(panel, "", 12); rangeInfo.name="ranged-reach-info";
            rangeInfo.style.color=new Color(.3f,.85f,1f);
            queue = Text(panel, "", 12); queue.name = "activation-queue";
            retreat = Text(panel, "", 13); retreat.name = "retreat-info";
            escaped = Text(panel, "", 12); escaped.name = "escaped-list";
            map = new DropdownField("Fixture (resets battle)", new List<string>(Enum.GetNames(typeof(SizeExperimentMap))), 0) { name = "fixture-selector" };
            map.RegisterValueChangedCallback(e => presenter.ConfigureFixture((SizeExperimentMap)Enum.Parse(typeof(SizeExperimentMap), e.newValue))); panel.Add(map);
            Text(panel, "Size/density experiment · no combat retuning. 9v9 = synthetic tactical roster, not strategic Capacity validation. Siege: static fortress; moat proxy has fixed crossings. 39×37 preserves each attacker approach; West = attacker coalition, East = defenders. No real siege mechanics.", 12);
            AddButton(panel, "Fit whole board", "fit-board", presenter.FitBoard);
            AddButton(panel, "Focus active unit (wheel to zoom)", "focus-unit", presenter.FocusActor);
            AddButton(panel, "Restart Same Seed", "restart", presenter.RestartSameSeed);
            hover = Text(panel, "Hover the battlefield.", 12);
            cell = Text(panel, "", 12);
            preview = Text(panel, "", 14); preview.name = "command-preview";
            friendly = new Toggle("Explicitly confirm allied target");
            friendly.RegisterValueChangedCallback(e => presenter.Repreview(e.newValue)); panel.Add(friendly);
            riskWarning = Text(panel, "", 14); riskWarning.name = "oa-warning";
            riskWarning.style.color = new Color(1, .68f, .4f);
            confirm = AddButton(panel, "Confirm selected Move / Attack", "confirm-command", presenter.ConfirmPreview);
            cancel = AddButton(panel, "Cancel preview", "cancel-preview", presenter.CancelPreview);
            defend = AddButton(panel, "Defend", "defend", presenter.Defend);
            finalFacing = new DropdownField("End facing", new List<string> { "Keep current", "North", "NorthEast", "East", "SouthEast", "South", "SouthWest", "West", "NorthWest" }, 0);
            panel.Add(finalFacing);
            map.labelElement.style.color = friendly.labelElement.style.color = finalFacing.labelElement.style.color = new Color(.89f, .93f, .97f);
            end = AddButton(panel, "End Activation", "end-activation", () => presenter.EndActivation(finalFacing.index == 0 ? (Facing?)null : (Facing)(finalFacing.index - 1)));
            message = Text(panel, "", 14); message.name = "battle-message"; message.style.color = new Color(1, .8f, .35f);
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
            if (hoveredCell.HasValue) hover.text = presenter.Hover(hoveredCell.Value);
            bool ended = state.Outcome.IsEnded;
            bool playerTurn = !presenter.IsAiTurn;
            Root.Q<DropdownField>("controller-mode").SetValueWithoutNotify(presenter.PlayerVsAi?(presenter.AiSide==Side.East?"Player West vs AI East":"Player East vs AI West"):"Hotseat");
            aiInfo.text=presenter.PlayerVsAi?presenter.AiExplanation:"Hotseat";
            rangeInfo.text=presenter.RangedReachMessage;
            rangeInfo.style.display=rangeInfo.text.Length>0?DisplayStyle.Flex:DisplayStyle.None;
            if (ended != showedOutcome) panel.schedule.Execute(() => panel.scrollOffset = Vector2.zero);
            showedOutcome = ended;
            outcomePanel.style.display = ended ? DisplayStyle.Flex : DisplayStyle.None;
            outcomeText.text = ended ? "BATTLE ENDED\nWinner: " + state.Outcome.VictorySide + "\nLoser: " + state.Outcome.DefeatedSide
                + "\nResult: " + state.Outcome.Reason + "\n\nDead:\n" + Roster(state, UnitStatus.Dead)
                + "\n\nEscaped/Safe:\n" + Roster(state, UnitStatus.Escaped)
                + "\n\nSurviving active units:\n" + Roster(state, UnitStatus.Active) : "";
            active.text = ended ? "No active turn — battle completed." : "ROUND " + state.Round + " · " + PrototypeFixture.Name(actor.Id) + "\n" + actor.Side + (actor.OwnRetreatEdge.HasValue?" · "+actor.OwnRetreatEdge+" approach":"")
                + " | HP " + actor.Hp + " / Armor " + actor.Armor + "\nMovement " + actor.MovementRemaining
                + " | Action " + (actor.ActionAvailable ? "available" : "spent")
                + "\nFacing " + actor.Facing + " | Defending " + (actor.IsDefending ? "yes" : "no") + " | " + BattlePresenter.OaStatus(actor);
            queue.text = ended ? "" : "Initiative order (► current):\n" + string.Join("\n", state.ActivationOrder.Select(id =>
                (id == actor.Id ? "► " : "   ") + PrototypeFixture.Name(id) + (state.FindUnit(id).OwnRetreatEdge.HasValue?" ("+state.FindUnit(id).OwnRetreatEdge+")":"") + " [" + state.FindUnit(id).Profile.Initiative + "] " + BattlePresenter.OaStatus(state.FindUnit(id))));
            cell.text = selected.HasValue ? "Selected (" + selected.Value.X + "," + selected.Value.Y + ")" : "No destination / target selected.";
            preview.text = presenter.PreviewText; confirm.SetEnabled(canConfirm && playerTurn); message.text = presenter.Message;
            int risks = presenter.OpportunityRiskCount;
            riskWarning.text = risks > 0 ? "This path may trigger " + risks + " Opportunity Attack(s). Confirm to accept the risk, or Cancel." : "";
            riskWarning.style.display = risks > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            confirm.text = presenter.HasMovePreview ? (risks > 0 ? "Confirm Move — accept " + risks + " OA risk(s)" : "Confirm Move") : "Confirm Attack";
            cancel.SetEnabled(canConfirm);
            defend.SetEnabled(playerTurn && !ended && BattleResolver.Validate(state, new DefendCommand(actor.Id)) == CommandError.None);
            end.SetEnabled(!ended && playerTurn); finalFacing.SetEnabled(!ended); friendly.SetEnabled(!ended);
            surface.SetEnabled(!ended && playerTurn);
            escaped.text = "Escaped/Safe:\n" + Roster(state, UnitStatus.Escaped);
            string eastZone = state.Battlefield.EastRetreatUsesPerimeter ? "full legal outer perimeter" : "East edge";
            retreat.text = "West: West edge. East: " + eastZone + "." + (ended ? "" : "\nYOUR escape: " + (actor.Side == Side.West ? "West edge" : eastZone));
            string approachEdges = string.Join(" / ",state.Units.Where(u=>u.Side==Side.West && u.OwnRetreatEdge.HasValue).Select(u=>u.OwnRetreatEdge.Value).Distinct());
            if(approachEdges.Length>0) retreat.text="Attacker rear edges (per army): "+approachEdges+". Defender: full legal outer perimeter."
                +(ended?"":"\nYOUR Retreat: "+(actor.OwnRetreatEdge.HasValue?actor.OwnRetreatEdge+" edge":eastZone));
            westEdge.text = "← West Retreat" + (!ended && actor.Side == Side.West ? " — YOUR ESCAPE" : "");
            eastEdge.text = (state.Battlefield.EastRetreatUsesPerimeter ? "East: ALL outer edges" : "East Retreat →") + (!ended && actor.Side == Side.East ? " — YOUR ESCAPE" : "");
            if(approachEdges.Length>0)westEdge.text=actor.OwnRetreatEdge.HasValue?"Attacker coalition · YOUR retreat: "+actor.OwnRetreatEdge:"Attacker retreat: "+approachEdges;
            events.text = string.Join("\n", presenter.RecentEvents);
            foreach (var label in unitLabels.Values) label.style.display = DisplayStyle.None;
            foreach (var unit in state.Units)
            {
                if (!unitLabels.TryGetValue(unit.Id, out var label))
                {
                    label = Text(surface, "", 11); label.pickingMode = PickingMode.Ignore;
                    label.style.position = Position.Absolute; label.style.width = 84; label.style.height = 47;
                    label.style.unityTextAlign = TextAnchor.MiddleCenter;
                    label.style.backgroundColor = new Color(.025f, .04f, .06f, .88f);
                    unitLabels.Add(unit.Id, label);
                }
                label.style.display = unit.IsActive ? DisplayStyle.Flex : DisplayStyle.None;
                string profile = unit.Profile.IsArcher ? "HA" : unit.Profile.Id == UnitProfileId.ElfWarriorTI ? "EW" : "HW";
                bool commander = unit.Id.Value == 1 || unit.Id.Value == 6 || unit.Id.Value == 19;
                label.text = (unit.Side == Side.West ? "W " : "E ") + profile + (commander ? " *" : "")
                    + (unit.IsDefending ? " DEF" : "") + "\nHP " + unit.Hp + " / A " + unit.Armor + "\n" + BattlePresenter.OaStatus(unit);
                label.style.color = !ended && state.CurrentUnitId == unit.Id ? new Color(1, .86f, .3f) : Color.white;
            }
            PositionLabels(state);
        }
        private static string Roster(BattleState state, UnitStatus status)
        {
            var units = state.Units.Where(u => u.Status == status).ToArray();
            return units.Length == 0 ? "None" : string.Join("\n", units.Select(u => PrototypeFixture.Name(u.Id)
                + " (" + u.Side + ") HP " + u.Hp + " / Armor " + u.Armor));
        }
        public void PositionLabels(BattleState state)
        {
            foreach (var unit in state.Units)
            {
                if (!unitLabels.TryGetValue(unit.Id, out var label)) continue;
                float cellPixels = surface.contentRect.height / (2 * camera.orthographicSize);
                bool detailed = cellPixels >= 64;
                label.style.width = detailed ? 84 : Mathf.Max(30, cellPixels);
                label.style.height = detailed ? 47 : 16;
                label.style.fontSize = detailed ? 11 : 10;
                string profile = unit.Profile.IsArcher ? "HA" : unit.Profile.Id == UnitProfileId.ElfWarriorTI ? "EW" : "HW";
                bool commander = unit.Id.Value == 1 || unit.Id.Value == 6 || unit.Id.Value == 19;
                label.text = detailed
                    ? (unit.Side == Side.West ? "W " : "E ") + profile + (commander ? " *" : "")
                        + (unit.IsDefending ? " DEF" : "") + "\nHP " + unit.Hp + " / A " + unit.Armor + "\n" + BattlePresenter.OaStatus(unit)
                    : profile + (commander ? "*" : "");
                Place(label, BattleGridView.World(unit.Position), detailed ? -42 : -Mathf.Max(30, cellPixels) / 2, cellPixels * .30f);
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

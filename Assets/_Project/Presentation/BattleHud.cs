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
        private readonly Label active, queue, preview, message, events, hover, cell;
        private readonly Button confirm, cancel, defend, end;
        private readonly Label riskWarning, retreat, escaped, outcomeText;
        private readonly VisualElement outcomePanel;
        private readonly ScrollView panel;
        private bool showedOutcome;
        private readonly Label westEdge, eastEdge;
        private readonly DropdownField finalFacing;
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
            var legend = Text(surface, "BLUE West · ORANGE East · GOLD active\nGreen: reachable · Gold: path · Red segment: OA risk\nHW square · HA circle · EW diamond · * Commander\nThin red borders: enemy ZoC ready · Gray: spent", 13);
            legend.style.position = Position.Absolute; legend.style.left = 20; legend.style.bottom = 16; legend.pickingMode = PickingMode.Ignore;
            westEdge = Text(surface, "West Retreat", 13); eastEdge = Text(surface, "East Retreat", 13);
            westEdge.style.position = eastEdge.style.position = Position.Absolute;
            westEdge.style.left = 20; eastEdge.style.right = 20;
            westEdge.style.top = eastEdge.style.top = 49;
            westEdge.pickingMode = eastEdge.pickingMode = PickingMode.Ignore;
            for (int x = 0; x < Battlefield.Width; x++) Axis(x.ToString(), new Vector3(x, 0, 8.75f));
            for (int y = 0; y < Battlefield.Height; y++) Axis(y.ToString(), new Vector3(-.8f, 0, y));
            surface.RegisterCallback<PointerMoveEvent>(e => {
                if (presenter.State != null && Pick(surface.WorldToLocal(e.position), out var p)) { hoveredCell = p; hover.text = presenter.Hover(p); }
            });
            surface.RegisterCallback<PointerDownEvent>(e => {
                if (e.button != 0 || presenter.State == null || !Pick(surface.WorldToLocal(e.position), out var p)) return;
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
            active = Text(panel, "", 16); active.name = "active-unit";
            queue = Text(panel, "", 12); queue.name = "activation-queue";
            retreat = Text(panel, "", 13); retreat.name = "retreat-info";
            escaped = Text(panel, "", 12); escaped.name = "escaped-list";
            var map = new DropdownField("Map (resets battle)", new List<string> { "BaseMap", "ControlMap" }, 0);
            map.RegisterValueChangedCallback(e => presenter.ConfigureFixture(e.newValue == "ControlMap")); panel.Add(map);
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
            Text(panel, "RECENT CORE EVENTS", 14);
            events = Text(panel, "", 11); events.name = "battle-events";
        }

        public void ResetChoices() { friendly.SetValueWithoutNotify(false); finalFacing.SetValueWithoutNotify("Keep current"); }
        public void Refresh(BattleState state, bool canConfirm, GridPosition? selected)
        {
            var actor = state.FindUnit(state.CurrentUnitId.Value);
            if (hoveredCell.HasValue) hover.text = presenter.Hover(hoveredCell.Value);
            bool ended = state.Outcome.IsEnded;
            if (ended != showedOutcome) panel.schedule.Execute(() => panel.scrollOffset = Vector2.zero);
            showedOutcome = ended;
            outcomePanel.style.display = ended ? DisplayStyle.Flex : DisplayStyle.None;
            outcomeText.text = ended ? "BATTLE ENDED\nWinner: " + state.Outcome.VictorySide + "\nLoser: " + state.Outcome.DefeatedSide
                + "\nResult: " + state.Outcome.Reason + "\n\nDead:\n" + Roster(state, UnitStatus.Dead)
                + "\n\nEscaped/Safe:\n" + Roster(state, UnitStatus.Escaped)
                + "\n\nSurviving active units:\n" + Roster(state, UnitStatus.Active) : "";
            active.text = ended ? "No active turn — battle completed." : "ROUND " + state.Round + " · " + PrototypeFixture.Name(actor.Id) + "\n" + actor.Side
                + " | HP " + actor.Hp + " / Armor " + actor.Armor + "\nMovement " + actor.MovementRemaining
                + " | Action " + (actor.ActionAvailable ? "available" : "spent")
                + "\nFacing " + actor.Facing + " | Defending " + (actor.IsDefending ? "yes" : "no") + " | " + BattlePresenter.OaStatus(actor);
            queue.text = ended ? "" : "Initiative order (► current):\n" + string.Join("\n", state.ActivationOrder.Select(id =>
                (id == actor.Id ? "► " : "   ") + PrototypeFixture.Name(id) + " [" + state.FindUnit(id).Profile.Initiative + "] " + BattlePresenter.OaStatus(state.FindUnit(id))));
            cell.text = selected.HasValue ? "Selected (" + selected.Value.X + "," + selected.Value.Y + ")" : "No destination / target selected.";
            preview.text = presenter.PreviewText; confirm.SetEnabled(canConfirm); message.text = presenter.Message;
            int risks = presenter.OpportunityRiskCount;
            riskWarning.text = risks > 0 ? "This path may trigger " + risks + " Opportunity Attack(s). Confirm to accept the risk, or Cancel." : "";
            riskWarning.style.display = risks > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            confirm.text = presenter.HasMovePreview ? (risks > 0 ? "Confirm Move — accept " + risks + " OA risk(s)" : "Confirm Move") : "Confirm Attack";
            cancel.SetEnabled(canConfirm);
            defend.SetEnabled(!ended && BattleResolver.Validate(state, new DefendCommand(actor.Id)) == CommandError.None);
            end.SetEnabled(!ended); finalFacing.SetEnabled(!ended); friendly.SetEnabled(!ended);
            surface.SetEnabled(!ended);
            escaped.text = "Escaped/Safe:\n" + Roster(state, UnitStatus.Escaped);
            retreat.text = ended ? "Retreat edges: West / East" : "Your Retreat Zone: " + actor.Side + " edge. Opponent's edge does NOT escape you.";
            westEdge.text = "← West Retreat" + (!ended && actor.Side == Side.West ? " — YOUR ESCAPE" : "");
            eastEdge.text = "East Retreat →" + (!ended && actor.Side == Side.East ? " — YOUR ESCAPE" : "");
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
                bool commander = unit.Id.Value == 1 || unit.Id.Value == 6;
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
                Place(label, BattleGridView.World(unit.Position), -38, 10);
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

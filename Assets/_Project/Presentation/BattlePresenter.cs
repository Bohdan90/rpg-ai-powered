using System;
using System.Collections.Generic;
using System.Linq;
using RPG.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPG.Presentation
{
    public sealed class BattlePresenter : MonoBehaviour
    {
        [SerializeField] private PanelSettings panelSettings;
        [SerializeField] private Shader unlitShader;
        private BattleHud hud;
        private BattleGridView grid;
        private Camera battleCamera;
        private UnitState[] initialUnits;
        private Battlefield initialBoard;
        private uint initialSeed;
        private readonly List<GridPosition> reachable = new List<GridPosition>();
        private readonly List<string> log = new List<string>();
        private BattleCommand pending;
        private GridPosition? selected;
        public BattleState State { get; private set; }
        public int VisualUnitCount => grid.ActiveVisualCount;
        public string PreviewText { get; private set; } = "Click a cell or a unit to preview.";
        public string Message { get; private set; } = "Hotseat: both sides are controlled here.";
        public IReadOnlyList<string> RecentEvents => log;
        public VisualElement HudRoot => hud.Root;

        private void Awake()
        {
            var cameraObject = new GameObject("Graybox camera"); cameraObject.transform.SetParent(transform, false);
            battleCamera = cameraObject.AddComponent<Camera>(); battleCamera.orthographic = true;
            battleCamera.transform.position = new Vector3(6, 20, 4); battleCamera.transform.rotation = Quaternion.Euler(90, 0, 0);
            battleCamera.clearFlags = CameraClearFlags.SolidColor; battleCamera.backgroundColor = new Color(.06f, .09f, .12f);
            battleCamera.nearClipPlane = .1f; battleCamera.farClipPlane = 50;
            battleCamera.rect = new Rect(0, 0, .70f, 1);
            grid = new BattleGridView(transform, unlitShader != null ? unlitShader : Shader.Find("Universal Render Pipeline/Unlit"));
            var document = gameObject.AddComponent<UIDocument>(); document.panelSettings = panelSettings;
            hud = new BattleHud(document, this, battleCamera);
        }
        private void Start() { if (State == null) ConfigureFixture(false); }
        private void LateUpdate()
        {
            battleCamera.orthographicSize = Mathf.Max(5.4f, 7.6f / Mathf.Max(.1f, battleCamera.aspect));
            if (State != null) hud.PositionLabels(State);
        }
        private void OnDestroy() { grid?.Dispose(); }

        public void ConfigureFixture(bool controlMap)
        {
            ConfigureBattle(PrototypeFixture.Units(), controlMap ? Battlefield.ControlMap : Battlefield.BaseMap, PrototypeFixture.Seed);
        }
        // An explicit initial fixture seam, also used by PlayMode integration tests; no rule implementation.
        public void ConfigureBattle(IEnumerable<UnitState> units, Battlefield board, uint seed)
        {
            initialUnits = units.ToArray(); initialBoard = board; initialSeed = seed; RestartSameSeed();
        }
        public void RestartSameSeed()
        {
            var result = BattleResolver.StartBattle(initialUnits, initialSeed, initialBoard);
            State = result.State; log.Clear(); Append(result.Events); Message = "Restarted with seed " + initialSeed + ".";
            ClearPreview(); Refresh();
        }
        public BattleResult Submit(BattleCommand command)
        {
            // No UI guard can bypass this boundary: Core validates every submitted command.
            var result = BattleResolver.Apply(State, command);
            State = result.State;
            Message = result.IsApplied ? command.GetType().Name + " applied." : "Rejected by Core: " + result.Error;
            if (result.IsApplied) Append(result.Events); else AddLog(Message);
            ClearPreview(); Refresh(); return result;
        }
        public void Defend() => Submit(new DefendCommand(State.CurrentUnitId.Value));
        public void EndActivation(Facing? facing) => Submit(new EndActivationCommand(State.CurrentUnitId.Value, facing));
        public void ConfirmPreview() { if (pending != null) Submit(pending); }

        public void SelectCell(GridPosition cell, bool friendlyConfirmed = false)
        {
            selected = cell; pending = null;
            var actor = State.FindUnit(State.CurrentUnitId.Value); var target = State.OccupantAt(cell);
            if (target != null && target.Id != actor.Id)
            {
                var command = new BasicAttackCommand(actor.Id, target.Id, friendlyConfirmed);
                var preview = BattleResolver.PreviewAttack(State, command);
                bool sight = actor.Profile.IsArcher ? LineOfSight.IsClear(State, actor.Position, target.Position)
                    : LineOfSight.IsMeleeCornerClear(State, actor.Position, target.Position);
                PreviewText = PrototypeFixture.Name(actor.Id) + " → " + PrototypeFixture.Name(target.Id)
                    + "\nTarget HP " + target.Hp + " / Armor " + target.Armor
                    + "\nDistance " + actor.Position.DistanceTo(target.Position)
                    + " | LoS / corner: " + (sight ? "clear" : "blocked");
                if (preview.IsLegal)
                {
                    PreviewText += "\nEffective range " + preview.MaximumRange + " | Contact " + preview.ContactChance + "%"
                        + "\nGuard " + preview.GuardChance + "% (separate roll)"
                        + "\nPhysical damage on unguarded hit: " + preview.PhysicalDamage
                        + "\nArmor loss " + preview.ArmorLossOnUnguardedHit + " | HP loss " + preview.HpLossOnUnguardedHit
                        + "\nFrontal Evasion: " + (preview.TargetFacesAttacker && target.Profile.FrontalEvasion > 0 ? "applies" : "does not apply")
                        + "\nSteady Aim: " + (preview.SteadyAim ? "active" : "inactive");
                    pending = command;
                }
                else PreviewText += "\nProfile range " + actor.Profile.Range + " | Core: " + preview.Error
                    + "\nContact / Guard / damage preview unavailable for this illegal attack.";
            }
            else
            {
                var path = Pathfinder.FindPath(State, actor.Id, cell);
                if (path.Found && path.Cost > 0)
                {
                    pending = new MoveCommand(actor.Id, path.Steps);
                    var previous = path.Cost == 1 ? actor.Position : path.Steps[path.Cost - 2];
                    PreviewText = "Move to (" + cell.X + "," + cell.Y + ")\nCost " + path.Cost + " Movement"
                        + "\nFinal facing: " + FacingDirections.Toward(previous, path.Steps[path.Cost - 1])
                        + "\nPath: " + string.Join(" → ", path.Steps.Select(p => p.X + "," + p.Y));
                }
                else
                {
                    var reason = BattleResolver.Validate(State, new MoveCommand(actor.Id, new[] { cell }));
                    PreviewText = "No executable Core path to (" + cell.X + "," + cell.Y + ").";
                    if (reason != CommandError.InvalidStep && reason != CommandError.None) PreviewText += "\nCore: " + reason;
                    else PreviewText += "\nChoose another cell within remaining Movement.";
                }
            }
            ShowViews();
        }
        public void Repreview(bool friendlyConfirmed) { if (selected.HasValue) SelectCell(selected.Value, friendlyConfirmed); }
        public string Hover(GridPosition cell)
        {
            var path = Pathfinder.FindPath(State, State.CurrentUnitId.Value, cell);
            var unit = State.OccupantAt(cell);
            return "Hover (" + cell.X + "," + cell.Y + ") " + (unit != null ? PrototypeFixture.Name(unit.Id)
                : path.Found ? "— Core path cost " + path.Cost : "— no reachable path");
        }
        private void ClearPreview() { pending = null; selected = null; PreviewText = "Click a cell or unit, then confirm. Green cells: Core reachable."; hud.ResetChoices(); }
        private void Refresh()
        {
            reachable.Clear();
            for (int x = 0; x < Battlefield.Width; x++)
            for (int y = 0; y < Battlefield.Height; y++)
            {
                var cell = new GridPosition(x, y); var path = Pathfinder.FindPath(State, State.CurrentUnitId.Value, cell);
                if (path.Found && path.Cost > 0) reachable.Add(cell);
            }
            ShowViews();
        }
        private void ShowViews()
        {
            grid.Refresh(State, reachable, (pending as MoveCommand)?.Path);
            hud.Refresh(State, pending != null, selected);
        }
        private void Append(IEnumerable<BattleEvent> events)
        {
            foreach (var e in events)
            {
                string who = e.Actor.HasValue ? PrototypeFixture.Name(e.Actor.Value) : "Battle";
                string line = "R" + e.Round + " " + who + ": " + e.Kind;
                if (e.Target.HasValue) line += " → " + PrototypeFixture.Name(e.Target.Value);
                if (e.Roll >= 0) line += " [" + e.Roll + " < " + e.ChancePercent + ": " + (e.Roll < e.ChancePercent ? "success" : "fail") + "]";
                if (e.Kind == BattleEventKind.ArmorLost || e.Kind == BattleEventKind.HpLost) line += " " + e.Before + " → " + e.After;
                if (e.Kind == BattleEventKind.DamageApplied) line += " " + e.Amount;
                if (e.To.HasValue) line += " (" + e.To.Value.X + "," + e.To.Value.Y + ")";
                AddLog(line);
            }
        }
        private void AddLog(string line) { log.Add(line); if (log.Count > 24) log.RemoveAt(0); }
    }
}

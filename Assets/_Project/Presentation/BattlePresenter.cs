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
        private float zoom = 1;
        public Side AiSide { get; private set; } = Side.East;
        public bool PlayerVsAi { get; private set; }
        public bool IsAiTurn => PlayerVsAi && State != null && !State.Outcome.IsEnded && State.FindUnit(State.CurrentUnitId.Value).Side == AiSide;
        public string AiExplanation { get; private set; } = "Hotseat";
        public string LastAttackOutcome { get; private set; } = "";
        private float nextAiTime;
        public void SetPlayerVsAi(bool enabled, Side side=Side.East) { AiSide=side; PlayerVsAi=enabled; nextAiTime=Time.unscaledTime+.4f; ClearPreview(); Refresh(); }
        public BattleResult StepAi()
        {
            if(!IsAiTurn)return null;
            var decision=TacticalAi.Choose(State);
            AiExplanation="AI S="+decision.Score.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+" · "+decision.Explanation;
            var result=Submit(decision.Command);AddLog(AiExplanation);return result;
        }
        private void Update()
        {
            if(IsAiTurn && Time.unscaledTime>=nextAiTime) { StepAi();nextAiTime=Time.unscaledTime+.4f; }
        }
        public SizeExperimentMap Fixture { get; private set; }
        private PersistenceSliceScenario persistence;
        private bool persistenceResolved;
        public bool PersistenceActive => persistence != null;
        public bool CanContinuePersistence => persistence != null && persistenceResolved && persistence.BattleNumber < 3;
        public string PersistenceSummary => persistence == null ? "" : persistence.Summary();
        public void StartPersistenceSlice()
        {
            persistence = new PersistenceSliceScenario(); persistenceResolved = false;
            LoadPersistenceBattle(persistence.StartFirstBattle());
        }
        public void ContinuePersistenceSlice()
        {
            if (!CanContinuePersistence) return;
            persistenceResolved = false; LoadPersistenceBattle(persistence.StartNextBattle());
        }
        private void LoadPersistenceBattle(PersistentBattle battle)
        {
            LastAttackOutcome = "";
            Fixture = SizeExperimentMap.Field_23x17_Full_9v9;
            initialUnits = battle.State.Units.ToArray(); initialBoard = battle.State.Battlefield; initialSeed = battle.State.InitialSeed;
            State = battle.State; Journal = new BattleJournal(State,"Persistence_Battle_"+persistence.BattleNumber,Application.version+" / Unity "+Application.unityVersion,
                PlayerVsAi&&AiSide==Side.West?"AI":"Player",PlayerVsAi&&AiSide==Side.East?"AI":"Player");
            AiExplanation="No AI decision yet.";nextAiTime=Time.unscaledTime+.4f;log.Clear();Message="Persistence Battle "+persistence.BattleNumber+" started.";
            grid.Resize(State.Battlefield);hud.Resize(State.Battlefield);FitBoard();ClearPreview();Refresh();
        }
        public void Zoom(float factor) { zoom = Mathf.Clamp(zoom * factor, .4f, 1); }
        public void CenterView(GridPosition cell) { battleCamera.transform.position = new Vector3(cell.X, 20, cell.Y); }
        public void FitBoard() { zoom = 1; CenterCamera(); }
        public void FocusActor()
        {
            if (State == null) return;
            CenterView(State.FindUnit(State.CurrentUnitId.Value).Position);
            zoom = .5f;
        }
        private void CenterCamera()
        {
            if (State != null) battleCamera.transform.position = new Vector3(
                (State.Battlefield.Columns - 1) / 2f, 20, (State.Battlefield.Rows - 1) / 2f);
        }
        private UnitState[] initialUnits;
        private Battlefield initialBoard;
        private uint initialSeed;
        private readonly List<GridPosition> reachable = new List<GridPosition>();
        private readonly List<GridPosition> rangedReach = new List<GridPosition>();
        public IReadOnlyCollection<GridPosition> RangedReach => rangedReach.AsReadOnly();
        public IReadOnlyCollection<GridPosition> ReachableCells => reachable.AsReadOnly();
        public int RangedVisualCount => grid.RangedVisualCount;
        public string RangedReachMessage { get; private set; } = "";
        private readonly List<string> log = new List<string>();
        public BattleJournal Journal { get; private set; }
        public string LastReplayPath { get; private set; }
        public string ExportReplay(string directory=null)
        {
            try {
                LastReplayPath=ReplayFiles.Export(Journal,directory??System.IO.Path.Combine(Application.persistentDataPath,"GateC","Replays"));
                Message="Exported replay + session: "+LastReplayPath;Refresh();return LastReplayPath;
            } catch(Exception e) { Message="Export failed: "+e.Message;Refresh();return null; }
        }
        public ReplayVerification VerifyReplay(string path)
        {
            var result=ReplayFiles.Verify(path);Message=result.Message;AddLog(Message);Refresh();return result;
        }
        private BattleCommand pending;
        private GridPosition? selected;
        public BattleState State { get; private set; }
        public int VisualUnitCount => grid.ActiveVisualCount;
        public string PreviewText { get; private set; } = "Click a cell or a unit to preview.";
        public string Message { get; private set; } = "Hotseat: both sides are controlled here.";
        public IReadOnlyList<string> RecentEvents => log;
        public VisualElement HudRoot => hud.Root;
        public OpportunityAttackPreview MovementRisk { get; private set; }
        public int OpportunityRiskCount => MovementRisk == null ? 0 : MovementRisk.Exposures.Sum(e => e.Threats.Count(t => t.WouldReact));
        public bool PreviewEscapes { get; private set; }
        public bool HasMovePreview => pending is MoveCommand;
        private readonly Dictionary<GridPosition, IReadOnlyList<UnitId>> threats = new Dictionary<GridPosition, IReadOnlyList<UnitId>>();
        public IReadOnlyDictionary<GridPosition, IReadOnlyList<UnitId>> ThreatCells => threats;
        public static string Cell(GridPosition p) => "(" + p.X + "," + p.Y + ")";
        public static string OaStatus(UnitState unit) => !unit.Profile.HasMeleeBasic ? "no OA" : unit.OpportunityAttackAvailable ? "OA ready" : "OA spent";
        public void CancelPreview() { if(pending!=null && Journal!=null)Journal.Session.cancelledPreviews++;ClearPreview(); ShowViews(); }

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
        private void Start() { if (State == null) ConfigureFixture(SizeExperimentMap.Field_13x9_Control); }
        private void LateUpdate()
        {
            if (State != null) battleCamera.orthographicSize = zoom * Mathf.Max((State.Battlefield.Rows + 1f) / 1.55f, (State.Battlefield.Columns + 2.2f) / (2 * Mathf.Max(.1f, battleCamera.aspect)));
            if (State != null) hud.PositionLabels(State);
        }
        private void OnDestroy() { grid?.Dispose(); }

        public void ConfigureFixture(SizeExperimentMap map)
        {
            persistence=null;
            Fixture = map;
            ConfigureBattle(SizeExperimentFixture.Units(map), SizeExperimentFixture.Board(map), PrototypeFixture.Seed);
        }
        public void ConfigureFixture(bool controlMap)
        {
            persistence=null;
            Fixture = SizeExperimentMap.Field_13x9_Control;
            ConfigureBattle(PrototypeFixture.Units(), controlMap ? Battlefield.ControlMap : Battlefield.BaseMap, PrototypeFixture.Seed);
        }
        // An explicit initial fixture seam, also used by PlayMode integration tests; no rule implementation.
        public void ConfigureBattle(IEnumerable<UnitState> units, Battlefield board, uint seed)
        {
            persistence=null;
            initialUnits = units.ToArray(); initialBoard = board; initialSeed = seed; RestartSameSeed();
        }
        public void RestartSameSeed()
        {
            LastAttackOutcome = "";
            var result = BattleResolver.StartBattle(initialUnits, initialSeed, initialBoard);
            State = result.State; Journal=new BattleJournal(State,Fixture.ToString(),Application.version+" / Unity "+Application.unityVersion,PlayerVsAi&&AiSide==Side.West?"AI":"Player",PlayerVsAi&&AiSide==Side.East?"AI":"Player"); AiExplanation="No AI decision yet."; nextAiTime=Time.unscaledTime+.4f; log.Clear(); Append(result.Events); Message = "Restarted with seed " + initialSeed + ".";
            grid.Resize(State.Battlefield); hud.Resize(State.Battlefield); FitBoard();
            ClearPreview(); Refresh();
        }
        public BattleResult Submit(BattleCommand command)
        {
            // No UI guard can bypass this boundary: Core validates every submitted command.
            var result = Journal.Apply(command,IsAiTurn?"AI":"Player",IsAiTurn?AiExplanation:null);
            State = result.State;
            Message = result.IsApplied ? command.GetType().Name + " applied." : "Rejected by Core: " + result.Error;
            if (result.IsApplied) Append(result.Events); else AddLog(Message);
            if (result.IsApplied && State.Outcome.IsEnded && persistence != null && !persistenceResolved)
            {
                persistence.Resolve(State); persistenceResolved=true;
                Message="Persistence Battle "+persistence.BattleNumber+" resolved. "+(CanContinuePersistence?"Continue to next battle when ready.":"Three-battle slice complete.");
            }
            ClearPreview(); Refresh(); return result;
        }
        public void Defend() { if(!IsAiTurn) Submit(new DefendCommand(State.CurrentUnitId.Value)); }
        public void EndActivation(Facing? facing) { if(!IsAiTurn) Submit(new EndActivationCommand(State.CurrentUnitId.Value, facing)); }
        public void ConfirmPreview() { if (!IsAiTurn && pending != null) Submit(pending); }

        public void SelectCell(GridPosition cell, bool friendlyConfirmed = false)
        {
            if (State.Outcome.IsEnded || IsAiTurn) return;
            selected = cell; pending = null; MovementRisk = null; PreviewEscapes = false;
            var actor = State.FindUnit(State.CurrentUnitId.Value); var target = State.OccupantAt(cell);
            if (target != null && target.Id != actor.Id)
            {
                var kind = BattleResolver.AvailableBasicAttack(State, actor.Id);
                bool meleeStrike = kind == BasicAttackKind.MeleeStrike;
                var command = new BasicAttackCommand(actor.Id, target.Id, friendlyConfirmed, kind);
                var preview = BattleResolver.PreviewAttack(State, command);
                bool sight = actor.Profile.IsArcher && !meleeStrike ? LineOfSight.IsClear(State, actor.Position, target.Position)
                    : LineOfSight.IsMeleeCornerClear(State, actor.Position, target.Position);
                PreviewText = PrototypeFixture.Name(actor.Id) + " → " + PrototypeFixture.Name(target.Id)
                    + "\nTarget HP " + target.Hp + " / Armor " + target.Armor
                    + "\nDistance " + actor.Position.DistanceTo(target.Position)
                    + " | LoS / corner: " + (sight ? "clear" : "blocked");
                if (actor.Profile.IsArcher)
                    PreviewText += meleeStrike ? "\nBow Shot — unavailable: Engaged\nMelee Strike"
                        : "\nBow Shot";
                if (preview.IsLegal)
                {
                    PreviewText += "\nEffective range " + preview.MaximumRange + " | Contact " + preview.ContactChance + "%"
                        + "\nBase Accuracy " + preview.BaseAccuracy + "% | Steady Aim +" + preview.AimModifier + " pp"
                        + "\nDistance " + preview.DistanceModifier + " pp | Dodge -" + preview.TargetDodge + " pp"
                        + " | Frontal Evasion -" + preview.FrontalEvasion + " pp"
                        + "\n" + preview.Cover + " Cover: " + preview.CoverAccuracyModifier + " pp Accuracy"
                        + "\nGuard " + preview.GuardChance + "% (separate roll)"
                        + "\nPhysical damage on unguarded hit: " + preview.PhysicalDamage
                        + "\nArmor loss " + preview.ArmorLossOnUnguardedHit + " | HP loss " + preview.HpLossOnUnguardedHit
                        + "\nFrontal Evasion: " + (preview.TargetFacesAttacker && target.Profile.FrontalEvasion > 0 ? "applies" : "does not apply")
                        + "\nSteady Aim: " + (preview.SteadyAim ? "active (Accuracy only; no range bonus)" : "inactive");
                    pending = command;
                }
                else PreviewText += "\nCore: " + preview.Error
                    + "\nContact / Guard / damage preview unavailable for this illegal attack.";
            }
            else
            {
                var path = Pathfinder.FindPath(State, actor.Id, cell);
                if (path.Found && path.Cost > 0)
                {
                    var move = new MoveCommand(actor.Id, path.Steps);
                    MovementRisk = OpportunityAttackPreview.Query(State, move);
                    pending = MovementRisk.IsLegal ? move : null;
                    PreviewEscapes = path.Steps.Any(p => State.Battlefield.IsRetreatZone(actor, p));
                    var previous = path.Cost == 1 ? actor.Position : path.Steps[path.Cost - 2];
                    PreviewText = "Move to (" + cell.X + "," + cell.Y + ")\nCost " + path.Cost + " Movement"
                        + "\nFinal facing: " + FacingDirections.Toward(previous, path.Steps[path.Cost - 1])
                        + "\nPath: " + string.Join(" → ", path.Steps.Select(p => p.X + "," + p.Y));
                    foreach (var exposure in MovementRisk.Exposures)
                    {
                        PreviewText += "\nStep " + (exposure.StepIndex + 1) + " " + Cell(exposure.From) + " → " + Cell(exposure.To);
                        foreach (var threat in exposure.Threats)
                            PreviewText += "\n  " + PrototypeFixture.Name(threat.Responder) + ": "
                                + (threat.WouldReact ? "may make an OA" : threat.AvailableNow ? "OA used earlier on this path" : "OA spent — cannot react");
                    }
                    if (OpportunityRiskCount > 0)
                        PreviewText += "\n" + OpportunityRiskCount + " OA risk(s). Hits/damage are NOT guaranteed; later steps require survival.";
                    if (PreviewEscapes)
                        PreviewText += "\nRetreat: " + (OpportunityRiskCount > 0 ? "if the unit survives and reaches its edge, it will Escape/Safe." : "unit will leave battle as Escaped/Safe.")
                            + "\nCurrent HP " + actor.Hp + " / Armor " + actor.Armor
                            + (OpportunityRiskCount > 0 ? ". Pools AFTER any OA will be preserved." : " will be preserved.");
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
            if (State.Outcome.IsEnded) return "Battle ended. Restart Same Seed to play again.";
            var actor = State.FindUnit(State.CurrentUnitId.Value);
            var path = Pathfinder.FindPath(State, actor.Id, cell);
            var unit = State.OccupantAt(cell);
            string text = "Hover " + Cell(cell) + " " + (unit != null ? PrototypeFixture.Name(unit.Id) + " | " + OaStatus(unit)
                    + "\nHP " + unit.Hp + " / Armor " + unit.Armor + " | Facing " + unit.Facing + (unit.IsDefending ? " | Defending" : "")
                : path.Found ? "— Core path cost " + path.Cost : "— no reachable path");
            var sources = ZoneOfControl.Sources(State, actor.Side, cell);
            if (sources.Count > 0) text += "\nEnemy ZoC: " + string.Join(", ", sources.Select(id => PrototypeFixture.Name(id) + " [" + OaStatus(State.FindUnit(id)) + "]"));
            if (path.Found && path.Cost > 0)
            {
                var risk = OpportunityAttackPreview.Query(State, new MoveCommand(actor.Id, path.Steps));
                text += "\nOA risks on Core path: " + risk.Exposures.Sum(e => e.Threats.Count(t => t.WouldReact));
            }
            if (State.Battlefield.IsRetreatZone(actor, cell)) text += "\nYour Retreat Zone — Escape/Safe on entry if alive.";
            else if (State.Battlefield.IsRetreatZone(actor.Side == Side.West ? Side.East : Side.West, cell)) text += "\nOpponent's edge — NOT your escape.";
            return text;
        }
        private void ClearPreview() { pending = null; selected = null; MovementRisk = null; PreviewEscapes = false; PreviewText = "Click a cell or unit, then confirm. Green cells: Core reachable."; hud.ResetChoices(); }
        private void Refresh()
        {
            reachable.Clear(); rangedReach.Clear(); RangedReachMessage=""; threats.Clear();
            if (State.Outcome.IsEnded) { ShowViews(); return; }
            var actor = State.FindUnit(State.CurrentUnitId.Value);
            bool showRange = actor.Profile.IsArcher && !IsAiTurn;
            if (showRange)
            {
                bool engaged = BattleResolver.IsArcherEngaged(State, actor.Id);
                RangedReachMessage = engaged ? "Bow unavailable: Engaged — reach hidden."
                    : !actor.ActionAvailable ? "Bow unavailable: Action spent — reach hidden."
                    : "Cyan marks: Bow geometric reach " + actor.Profile.Range + " from current cell. LoS/target legality checked separately.";
                showRange = !engaged && actor.ActionAvailable;
            }
            for (int x = 0; x < State.Battlefield.Columns; x++)
            for (int y = 0; y < State.Battlefield.Rows; y++)
            {
                var cell = new GridPosition(x, y); var path = Pathfinder.FindPath(State, State.CurrentUnitId.Value, cell);
                if (path.Found && path.Cost > 0) reachable.Add(cell);
                if (showRange && cell != actor.Position && actor.Position.DistanceTo(cell) <= actor.Profile.Range) rangedReach.Add(cell);
                var sources = ZoneOfControl.Sources(State, actor.Side, cell);
                if (sources.Count > 0) threats.Add(cell, sources);
            }
            ShowViews();
        }
        private void ShowViews()
        {
            grid.Refresh(State, reachable, (pending as MoveCommand)?.Path, threats, MovementRisk, rangedReach);
            hud.Refresh(State, pending != null && !State.Outcome.IsEnded, selected);
        }
        private void Append(IEnumerable<BattleEvent> events)
        {
            var outcomes = new List<string>();
            foreach (var e in events)
            {
                string who = e.Actor.HasValue ? PrototypeFixture.Name(e.Actor.Value) : "Battle";
                string line = "R" + e.Round + " " + who + ": " + e.Kind;
                string outcome = e.Kind == BattleEventKind.AttackMissed ? "Failed contact — no damage"
                    : e.Kind == BattleEventKind.GuardSucceeded ? "Guard blocked — no damage"
                    : e.Kind == BattleEventKind.ArmorLost ? "Armor damage " + e.Amount + " (" + e.Before + " → " + e.After + ")"
                    : e.Kind == BattleEventKind.HpLost ? "HP damage " + e.Amount + " (" + e.Before + " → " + e.After + ")" : null;
                if (outcome != null)
                {
                    outcomes.Add(who + " → " + PrototypeFixture.Name(e.Target.Value) + ": " + outcome);
                    line += " — " + outcome;
                }
                if (e.Target.HasValue) line += " → " + PrototypeFixture.Name(e.Target.Value);
                if (e.Roll >= 0) line += " [" + e.Roll + " < " + e.ChancePercent + ": " + (e.Roll < e.ChancePercent ? "success" : "fail") + "]";
                if (e.Kind == BattleEventKind.ArmorLost || e.Kind == BattleEventKind.HpLost) line += " " + e.Before + " → " + e.After;
                if (e.Kind == BattleEventKind.DamageApplied) line += " " + e.Amount;
                if (e.From.HasValue) line += " " + Cell(e.From.Value) + " →";
                if (e.To.HasValue) line += " " + Cell(e.To.Value);
                if (e.Kind == BattleEventKind.MovementInterruptedByDeath) line += " — exit step NOT executed; remaining path cancelled.";
                if (e.Kind == BattleEventKind.OpportunityAttackTriggered) line += " — BEFORE the exit step.";
                if (e.Kind == BattleEventKind.OpportunityAttackSpent) line += " — unavailable until own activation.";
                if (e.Kind == BattleEventKind.UnitEscaped) line += " — SAFE, not Dead.";
                if (e.Outcome.HasValue) line += " — Winner " + e.Outcome.Value.VictorySide + ", Loser " + e.Outcome.Value.DefeatedSide + ", " + e.Outcome.Value.Reason;
                AddLog(line);
            }
            if (outcomes.Count > 0) LastAttackOutcome = string.Join("\n", outcomes);
        }
        private void AddLog(string line) { log.Add(line); if (log.Count > 200) log.RemoveAt(0); }
    }
}

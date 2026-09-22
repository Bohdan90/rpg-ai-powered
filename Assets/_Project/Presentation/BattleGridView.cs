using System.Collections.Generic;
using System.Linq;
using RPG.Core;
using UnityEngine;

namespace RPG.Presentation
{
    // GameObjects are disposable views. No view owns or writes a UnitState.
    public sealed class BattleGridView
    {
        private readonly GameObject root;
        private readonly Material material;
        private Renderer[,] tiles;
        private readonly Dictionary<UnitId, Token> units = new Dictionary<UnitId, Token>();
        private readonly LineRenderer pathLine;
        private LineRenderer[,] zocBorders;
        private LineRenderer[,] rangeMarks;
        public int RangedVisualCount { get; private set; }
        private Renderer[,] retreatStripes;
        private Renderer[,] eastStripes;
        private GameObject cellsRoot;
        private readonly List<LineRenderer> riskSegments = new List<LineRenderer>();
        private readonly Color floorA = new Color(.17f, .23f, .28f), floorB = new Color(.20f, .27f, .32f);
        public int ActiveVisualCount => units.Values.Count(u => u.Root.activeSelf);
        public static Vector3 World(GridPosition p) => new Vector3(p.X, 0, p.Y);

        public BattleGridView(Transform parent, Shader shader)
        {
            root = new GameObject("Battle views"); root.transform.SetParent(parent, false);
            material = new Material(shader) { name = "Graybox runtime unlit" };
            var path = new GameObject("Core path preview"); path.transform.SetParent(root.transform, false);
            pathLine = path.AddComponent<LineRenderer>(); pathLine.sharedMaterial = material;
            pathLine.widthMultiplier = .045f; pathLine.useWorldSpace = true;
            Tint(pathLine, new Color(1, .77f, .23f));
            pathLine.positionCount = 0;
        }

        public void Resize(Battlefield board)
        {
            if (cellsRoot != null) { cellsRoot.SetActive(false); Object.Destroy(cellsRoot); }
            cellsRoot = new GameObject("Fixture cells"); cellsRoot.transform.SetParent(root.transform, false);
            tiles = new Renderer[board.Columns, board.Rows];
            zocBorders = new LineRenderer[board.Columns, board.Rows];
            rangeMarks = new LineRenderer[board.Columns, board.Rows];
            retreatStripes = new Renderer[board.Columns, board.Rows];
            eastStripes = new Renderer[board.Columns, board.Rows];
            for (int x = 0; x < board.Columns; x++)
            for (int y = 0; y < board.Rows; y++)
            {
                tiles[x, y] = Primitive("Cell " + x + "," + y, PrimitiveType.Cube, cellsRoot.transform,
                    new Vector3(x, -.12f, y), new Vector3(.95f, .12f, .95f));
                zocBorders[x, y] = Line("Enemy ZoC " + x + "," + y, .018f);
                zocBorders[x, y].transform.SetParent(cellsRoot.transform, false);
                zocBorders[x, y].positionCount = 5;
                zocBorders[x, y].SetPositions(new[] { new Vector3(x-.43f,.015f,y-.43f), new Vector3(x+.43f,.015f,y-.43f),
                    new Vector3(x+.43f,.015f,y+.43f), new Vector3(x-.43f,.015f,y+.43f), new Vector3(x-.43f,.015f,y-.43f) });
                rangeMarks[x,y] = Line("Geometric ranged reach " + x + "," + y, .035f);
                rangeMarks[x,y].transform.SetParent(cellsRoot.transform, false);
                rangeMarks[x,y].positionCount = 3;
                // Small inset L markers preserve movement fill, ZoC borders and path readability.
                rangeMarks[x,y].SetPositions(new[] { new Vector3(x-.35f,.56f,y+.1f),
                    new Vector3(x-.35f,.56f,y+.35f), new Vector3(x-.1f,.56f,y+.35f) });
                Tint(rangeMarks[x,y],new Color(.3f,.85f,1f));
                retreatStripes[x, y] = Primitive("Retreat edge " + x + "," + y, PrimitiveType.Cube, cellsRoot.transform,
                    new Vector3(x, .02f, y+.35f), new Vector3(.8f,.025f,.1f));
                eastStripes[x, y] = Primitive("East retreat " + x + "," + y, PrimitiveType.Cube, cellsRoot.transform,
                    new Vector3(x, .02f, y-.35f), new Vector3(.8f,.025f,.1f));
            }
        }

        public void Refresh(BattleState state, IReadOnlyCollection<GridPosition> reachable, IReadOnlyList<GridPosition> path,
            IReadOnlyDictionary<GridPosition, IReadOnlyList<UnitId>> threats, OpportunityAttackPreview risk, IReadOnlyCollection<GridPosition> rangedReach)
        {
            var highlights = new HashSet<GridPosition>(reachable);
            var rangeCells = new HashSet<GridPosition>(rangedReach);
            RangedVisualCount = 0;
            var pathCells = path == null ? new HashSet<GridPosition>() : new HashSet<GridPosition>(path);
            for (int x = 0; x < state.Battlefield.Columns; x++)
            for (int y = 0; y < state.Battlefield.Rows; y++)
            {
                var p = new GridPosition(x, y);
                bool inRange = rangeCells.Contains(p); rangeMarks[x,y].gameObject.SetActive(inRange);
                if (inRange) RangedVisualCount++;
                var tile = tiles[x, y]; bool solid = state.Battlefield.IsSolid(p);
                var selectedActor=state.CurrentUnitId.HasValue ? state.FindUnit(state.CurrentUnitId.Value) : null;
                bool westRetreat = selectedActor!=null && selectedActor.Side==Side.West ? state.Battlefield.IsRetreatZone(selectedActor,p) : state.Units.Any(u=>u.Side==Side.West && state.Battlefield.IsRetreatZone(u,p));
                bool eastRetreat = state.Battlefield.IsRetreatZone(Side.East, p);
                retreatStripes[x, y].gameObject.SetActive(westRetreat);
                eastStripes[x, y].gameObject.SetActive(eastRetreat);
                Tint(eastStripes[x, y], new Color(1,.55f,.30f));
                Tint(retreatStripes[x, y], new Color(.30f,.70f,1));
                bool threatened = threats.TryGetValue(p, out var sources);
                zocBorders[x, y].gameObject.SetActive(threatened);
                if (threatened) Tint(zocBorders[x, y], sources.Any(id => state.FindUnit(id).OpportunityAttackAvailable)
                    ? new Color(.8f,.36f,.32f) : new Color(.4f,.43f,.47f));
                tile.transform.localScale = new Vector3(.95f, solid ? .65f : .12f, .95f);
                tile.transform.localPosition = new Vector3(x, solid ? .20f : -.12f, y);
                Tint(tile, solid ? new Color(.40f, .43f, .47f) : pathCells.Contains(p) ? new Color(.72f, .51f, .12f)
                    : highlights.Contains(p) ? new Color(.18f, .38f, .37f) : (x + y) % 2 == 0 ? floorA : floorB);
            }
            foreach (var token in units.Values) token.Root.SetActive(false);
            foreach (var unit in state.Units)
            {
                if (!units.TryGetValue(unit.Id, out var token) || token.Profile != unit.Profile.Id)
                {
                    if (token != null) Object.Destroy(token.Root);
                    token = MakeToken(unit); units[unit.Id] = token;
                }
                token.Root.SetActive(unit.IsActive);
                token.Root.transform.localPosition = World(unit.Position);
                token.Facing.localRotation = Quaternion.Euler(0, (int)unit.Facing * 45, 0);
                token.Active.SetActive(!state.Outcome.IsEnded && state.CurrentUnitId == unit.Id);
                Tint(token.Body, unit.Side == Side.West ? new Color(.25f, .65f, .96f) : new Color(.98f, .45f, .30f));
            }
            foreach (var segment in riskSegments) segment.gameObject.SetActive(false);
            int riskIndex = 0;
            if (risk != null)
            foreach (var exposure in risk.Exposures.Where(e => e.Threats.Any(t => t.WouldReact)))
            {
                if (riskIndex == riskSegments.Count) riskSegments.Add(Line("OA risk step", .075f));
                var segment = riskSegments[riskIndex++]; segment.gameObject.SetActive(true);
                segment.name = "OA risk step " + (exposure.StepIndex + 1);
                segment.positionCount = 2; segment.SetPositions(new[] { World(exposure.From)+Vector3.up*.55f, World(exposure.To)+Vector3.up*.55f });
                Tint(segment, new Color(1,.28f,.20f));
            }
            pathLine.positionCount = path == null || path.Count == 0 ? 0 : path.Count + 1;
            if (pathLine.positionCount > 0)
            {
                pathLine.SetPosition(0, World(state.FindUnit(state.CurrentUnitId.Value).Position) + Vector3.up * .45f);
                for (int i = 0; i < path.Count; i++) pathLine.SetPosition(i + 1, World(path[i]) + Vector3.up * .45f);
            }
        }

        private Token MakeToken(UnitState unit)
        {
            var go = new GameObject(PrototypeFixture.Name(unit.Id)); go.transform.SetParent(root.transform, false);
            var ring = Primitive("Active marker", PrimitiveType.Cylinder, go.transform, new Vector3(0, .02f, 0), new Vector3(.88f, .025f, .88f));
            Tint(ring, new Color(1, .85f, .24f));
            var type = unit.Profile.IsArcher ? PrimitiveType.Cylinder : PrimitiveType.Cube;
            var body = Primitive("Unit token", type, go.transform, new Vector3(0, .20f, 0), new Vector3(.57f, .20f, .57f));
            if (unit.Profile.Id == UnitProfileId.ElfWarriorTI)
            {
                body.transform.localRotation = Quaternion.Euler(0, 45, 0);
                body.transform.localScale = new Vector3(.51f,.20f,.51f);
            }
            var facing = new GameObject("Facing"); facing.transform.SetParent(go.transform, false);
            var line = facing.AddComponent<LineRenderer>(); line.sharedMaterial = material;
            line.useWorldSpace = false; line.widthMultiplier = .06f;
            line.positionCount = 5;
            line.SetPositions(new[] { new Vector3(0, .50f, .06f), new Vector3(0, .50f, .44f),
                new Vector3(-.13f, .50f, .29f), new Vector3(0, .50f, .44f), new Vector3(.13f, .50f, .29f) });
            return new Token { Profile = unit.Profile.Id, Root = go, Body = body, Active = ring.gameObject, Facing = facing.transform };
        }
        private LineRenderer Line(string name, float width)
        {
            var go = new GameObject(name); go.transform.SetParent(root.transform, false);
            var line = go.AddComponent<LineRenderer>(); line.sharedMaterial = material;
            line.useWorldSpace = true; line.widthMultiplier = width; return line;
        }
        private Renderer Primitive(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = scale;
            Object.Destroy(go.GetComponent<Collider>());
            var renderer = go.GetComponent<Renderer>(); renderer.sharedMaterial = material; return renderer;
        }
        private static void Tint(Renderer renderer, Color color)
        {
            var block = new MaterialPropertyBlock(); block.SetColor("_BaseColor", color); block.SetColor("_Color", color);
            renderer.SetPropertyBlock(block);
        }
        public void Dispose() { Object.Destroy(root); Object.Destroy(material); }
        private sealed class Token { public UnitProfileId Profile; public GameObject Root, Active; public Renderer Body; public Transform Facing; }
    }
}

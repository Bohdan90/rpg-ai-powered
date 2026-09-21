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
        private readonly Renderer[,] tiles = new Renderer[Battlefield.Width, Battlefield.Height];
        private readonly Dictionary<UnitId, Token> units = new Dictionary<UnitId, Token>();
        private readonly LineRenderer pathLine;
        private readonly Color floorA = new Color(.17f, .23f, .28f), floorB = new Color(.20f, .27f, .32f);
        public int ActiveVisualCount => units.Values.Count(u => u.Root.activeSelf);
        public static Vector3 World(GridPosition p) => new Vector3(p.X, 0, p.Y);

        public BattleGridView(Transform parent, Shader shader)
        {
            root = new GameObject("Battle views"); root.transform.SetParent(parent, false);
            material = new Material(shader) { name = "Graybox runtime unlit" };
            for (int x = 0; x < Battlefield.Width; x++)
            for (int y = 0; y < Battlefield.Height; y++)
                tiles[x, y] = Primitive("Cell " + x + "," + y, PrimitiveType.Cube, root.transform,
                    new Vector3(x, -.12f, y), new Vector3(.95f, .12f, .95f));
            var path = new GameObject("Core path preview"); path.transform.SetParent(root.transform, false);
            pathLine = path.AddComponent<LineRenderer>(); pathLine.sharedMaterial = material;
            pathLine.widthMultiplier = .045f; pathLine.useWorldSpace = true;
            Tint(pathLine, new Color(1, .77f, .23f));
            pathLine.positionCount = 0;
        }

        public void Refresh(BattleState state, IReadOnlyCollection<GridPosition> reachable, IReadOnlyList<GridPosition> path)
        {
            var highlights = new HashSet<GridPosition>(reachable);
            var pathCells = path == null ? new HashSet<GridPosition>() : new HashSet<GridPosition>(path);
            for (int x = 0; x < Battlefield.Width; x++)
            for (int y = 0; y < Battlefield.Height; y++)
            {
                var p = new GridPosition(x, y); var tile = tiles[x, y]; bool solid = state.Battlefield.IsSolid(p);
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
                token.Active.SetActive(state.CurrentUnitId == unit.Id);
                Tint(token.Body, unit.Side == Side.West ? new Color(.25f, .65f, .96f) : new Color(.98f, .45f, .30f));
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
            var type = unit.Profile.Id == UnitProfileId.HumanWarriorTI ? PrimitiveType.Cylinder
                : unit.Profile.IsArcher ? PrimitiveType.Cube : PrimitiveType.Sphere;
            var body = Primitive("Unit token", type, go.transform, new Vector3(0, .20f, 0), new Vector3(.57f, .20f, .57f));
            var facing = new GameObject("Facing"); facing.transform.SetParent(go.transform, false);
            var line = facing.AddComponent<LineRenderer>(); line.sharedMaterial = material;
            line.useWorldSpace = false; line.widthMultiplier = .06f;
            line.positionCount = 5;
            line.SetPositions(new[] { new Vector3(0, .50f, .06f), new Vector3(0, .50f, .44f),
                new Vector3(-.13f, .50f, .29f), new Vector3(0, .50f, .44f), new Vector3(.13f, .50f, .29f) });
            return new Token { Profile = unit.Profile.Id, Root = go, Body = body, Active = ring.gameObject, Facing = facing.transform };
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

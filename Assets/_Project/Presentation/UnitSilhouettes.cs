using System.Collections.Generic;
using UnityEngine;

namespace RPG.Presentation
{
    // Two flat, code-authored vector sprites. Class/race labels remain in the HUD.
    // Shared presentation geometry only; no combat state or collider.
    internal static class UnitSilhouettes
    {
        public static Mesh Create(bool archer)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            void Polygon(params Vector2[] points)
            {
                int first = vertices.Count;
                foreach (var p in points) vertices.Add(new Vector3(p.x, .48f, p.y));
                for (int i = 1; i < points.Length - 1; i++)
                { triangles.Add(first); triangles.Add(first + i + 1); triangles.Add(first + i); }
            }
            void Bar(float ax, float ay, float bx, float by, float width)
            {
                var a = new Vector2(ax, ay); var b = new Vector2(bx, by);
                var offset = new Vector2(-(b-a).y, (b-a).x).normalized * width / 2;
                Polygon(a-offset, b-offset, b+offset, a+offset);
            }
            // Head, tunic and separated boots read as a figure even when zoomed out.
            Polygon(new Vector2(-.12f,.18f),new Vector2(.08f,.18f),new Vector2(.1f,.3f),
                new Vector2(-.02f,.37f),new Vector2(-.14f,.3f));
            Polygon(new Vector2(-.15f,-.12f),new Vector2(.13f,-.12f),new Vector2(.09f,.14f),new Vector2(-.12f,.14f));
            Bar(-.08f,-.1f,-.15f,-.34f,.09f); Bar(.07f,-.1f,.14f,-.34f,.09f);
            if (archer)
            {
                // Open bow + taut string and horizontal arrow, clearly unlike a shield.
                Bar(.24f,-.29f,.35f,-.15f,.045f); Bar(.35f,-.15f,.39f,.03f,.045f);
                Bar(.39f,.03f,.34f,.2f,.045f); Bar(.34f,.2f,.24f,.31f,.045f);
                Bar(.24f,-.29f,.24f,.31f,.022f);
                Bar(-.24f,.05f,.34f,.05f,.03f);
                Polygon(new Vector2(.34f,.01f),new Vector2(.41f,.05f),new Vector2(.34f,.09f));
                Bar(-.12f,.12f,-.25f,.02f,.065f); Bar(.06f,.12f,.23f,.05f,.065f);
            }
            else
            {
                // Broad shield on the left; long blade and crossguard on the right.
                Polygon(new Vector2(-.35f,-.08f),new Vector2(-.24f,-.2f),new Vector2(-.13f,-.08f),
                    new Vector2(-.13f,.15f),new Vector2(-.35f,.15f));
                Bar(.09f,.08f,.25f,-.01f,.07f);
                Bar(.27f,-.14f,.27f,.3f,.05f); Bar(.18f,-.01f,.36f,-.01f,.045f);
                Polygon(new Vector2(.245f,.3f),new Vector2(.295f,.3f),new Vector2(.27f,.4f));
            }
            var mesh = new Mesh { name = archer ? "Archer silhouette" : "Warrior silhouette" };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds();
            return mesh;
        }
    }
}

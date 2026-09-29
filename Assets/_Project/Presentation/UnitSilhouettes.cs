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
            if (archer)
            {
                // Weapon icons only: open bow, taut string and arrow.
                Bar(-.08f,-.34f,.12f,-.19f,.06f); Bar(.12f,-.19f,.2f,0,.06f);
                Bar(.2f,0,.12f,.19f,.06f); Bar(.12f,.19f,-.08f,.34f,.06f);
                Bar(-.08f,-.34f,-.08f,.34f,.028f);
                Bar(-.29f,0,.32f,0,.045f);
                Polygon(new Vector2(.27f,-.08f),new Vector2(.39f,0),new Vector2(.27f,.08f));
            }
            else
            {
                // Broad shield on the left; long blade and crossguard on the right.
                Polygon(new Vector2(-.34f,-.1f),new Vector2(-.17f,-.29f),new Vector2(0,-.1f),
                    new Vector2(0,.17f),new Vector2(-.34f,.17f));
                Bar(.17f,-.32f,.17f,.25f,.075f); Bar(.03f,-.13f,.32f,-.13f,.065f);
                Polygon(new Vector2(.1325f,.25f),new Vector2(.2075f,.25f),new Vector2(.17f,.39f));
            }
            var mesh = new Mesh { name = archer ? "Archer silhouette" : "Warrior silhouette" };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds();
            return mesh;
        }
    }
}

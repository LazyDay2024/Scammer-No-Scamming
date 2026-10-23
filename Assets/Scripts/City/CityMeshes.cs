using System.Collections.Generic;
using UnityEngine;

namespace ScammerNoScamming
{
    // Box meshes whose UVs are measured in metres, so a facade texture keeps its
    // proportions on buildings of any size.
    public static class CityMeshes
    {
        static readonly Dictionary<string, Mesh> boxes = new Dictionary<string, Mesh>();
        static Mesh cylinder;

        public static Mesh UnitCube => Box(Vector3.one, Vector2.one);

        public static Mesh Cylinder
        {
            get
            {
                if (cylinder == null)
                {
                    var temp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    cylinder = temp.GetComponent<MeshFilter>().sharedMesh;
                    if (Application.isPlaying) Object.Destroy(temp);
                    else Object.DestroyImmediate(temp);
                }
                return cylinder;
            }
        }

        // tileMeters is the world size of one full texture repeat (u, v).
        public static Mesh Box(Vector3 size, Vector2 tileMeters)
        {
            string key = $"{size.x:F2}|{size.y:F2}|{size.z:F2}|{tileMeters.x:F1}|{tileMeters.y:F1}";
            if (boxes.TryGetValue(key, out var cached) && cached != null) return cached;

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            Vector3 h = size * 0.5f;

            AddFace(Vector3.forward * h.z, Vector3.right, Vector3.up, size.x, size.y);
            AddFace(Vector3.back * h.z, Vector3.left, Vector3.up, size.x, size.y);
            AddFace(Vector3.right * h.x, Vector3.back, Vector3.up, size.z, size.y);
            AddFace(Vector3.left * h.x, Vector3.forward, Vector3.up, size.z, size.y);
            AddFace(Vector3.up * h.y, Vector3.right, Vector3.back, size.x, size.z);
            AddFace(Vector3.down * h.y, Vector3.right, Vector3.forward, size.x, size.z);

            void AddFace(Vector3 center, Vector3 right, Vector3 up, float width, float height)
            {
                int i = vertices.Count;
                Vector3 r = right * (width * 0.5f);
                Vector3 u = up * (height * 0.5f);
                Vector3 normal = Vector3.Cross(right, up);
                vertices.Add(center - r - u);
                vertices.Add(center + r - u);
                vertices.Add(center + r + u);
                vertices.Add(center - r + u);
                for (int n = 0; n < 4; n++) normals.Add(normal);
                float us = width / tileMeters.x;
                float vs = height / tileMeters.y;
                uvs.Add(new Vector2(0f, 0f));
                uvs.Add(new Vector2(us, 0f));
                uvs.Add(new Vector2(us, vs));
                uvs.Add(new Vector2(0f, vs));
                triangles.AddRange(new[] { i, i + 2, i + 1, i, i + 3, i + 2 });
            }

            var mesh = new Mesh { name = "CityBox_" + key };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            boxes[key] = mesh;
            return mesh;
        }
    }
}

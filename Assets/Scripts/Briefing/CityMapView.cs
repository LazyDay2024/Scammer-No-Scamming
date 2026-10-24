using System.Collections.Generic;
using UnityEngine;

namespace ScammerNoScamming
{
    // Where the map covers, and the picture of it. The picture is a plan drawn from the footprints of the
    // buildings in the scene when the case starts, so it always matches the city. Placed by the scene builder so
    // the team can move the bounds.
    public class CityMapView : MonoBehaviour
    {
        public static CityMapView Instance { get; private set; }

        public Vector2 center;
        public float halfSize = 300f;
        public int resolution = 1024;
        [Tooltip("Objects narrower than this (metres) are left off the map.")]
        public float minFootprint = 2.5f;
        [Tooltip("Optional hand-made map (north up, same centre and halfSize). When set it replaces the generated plan.")]
        public Texture overridePicture;
        public Texture Picture { get; private set; }

        void Awake() => Instance = this;
        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (Picture != null && Picture != overridePicture) Destroy(Picture);
        }

        public void Prepare()
        {
            if (Picture == null) Picture = overridePicture != null ? overridePicture : Draw();
        }

        Texture2D Draw()
        {
            int n = resolution;
            var pixels = new Color32[n * n];
            var ground = new Color32(36, 31, 26, 255);
            for (int i = 0; i < pixels.Length; i++) pixels[i] = ground;

            var footprints = new List<Bounds>();
            foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                var b = r.bounds;
                // Skip the underground interior, ground planes, tiny props and sky-sized meshes.
                if (b.center.y < -50f || b.size.x > 250f || b.size.z > 250f || b.size.y < 1.5f) continue;
                if (Mathf.Max(b.size.x, b.size.z) < minFootprint) continue;
                footprints.Add(b);
            }
            footprints.Sort((a, b) => a.max.y.CompareTo(b.max.y)); // taller on top

            float scale = n / (halfSize * 2f);
            foreach (var b in footprints)
            {
                int x0 = Mathf.Max(0, Mathf.FloorToInt((b.min.x - center.x + halfSize) * scale));
                int x1 = Mathf.Min(n - 1, Mathf.CeilToInt((b.max.x - center.x + halfSize) * scale));
                int y0 = Mathf.Max(0, Mathf.FloorToInt((b.min.z - center.y + halfSize) * scale));
                int y1 = Mathf.Min(n - 1, Mathf.CeilToInt((b.max.z - center.y + halfSize) * scale));
                float height = Mathf.Clamp01(b.max.y / 60f);
                var fill = Color32.Lerp(new Color32(96, 82, 58, 255), new Color32(196, 164, 104, 255), height);
                var edge = Color32.Lerp(fill, new Color32(20, 17, 14, 255), .45f);
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                        pixels[y * n + x] = x == x0 || x == x1 || y == y0 || y == y1 ? edge : fill;
            }

            var picture = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "CityMap", filterMode = FilterMode.Bilinear };
            picture.SetPixels32(pixels);
            picture.Apply();
            return picture;
        }

        // World XZ -> 0..1 inside the picture, v up.
        public Vector2 ToUv(Vector3 world) =>
            new Vector2((world.x - center.x) / (halfSize * 2f) + .5f, (world.z - center.y) / (halfSize * 2f) + .5f);
    }
}

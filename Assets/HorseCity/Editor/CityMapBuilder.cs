using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
#if HORSE_URP
using UnityEngine.Rendering.Universal;
#endif

namespace HorseCity.EditorTools
{
    /// <summary>
    /// Builds the whole HORSE city into Scenes/City map.
    ///
    /// Layout follows the ring sketch (north = +Z, east = +X):
    ///   center plaza     : the biggest building (Central Tower)
    ///   inner ring       : vet (west), hidden weapon shop (north-east), surrounding towers (south)
    ///   middle ring      : houses + random buildings (NW), horse auction house (NE),
    ///                      police station + police stable (SW), buildings (SE)
    ///   outer ring       : random buildings, then the city edge
    /// Four avenues cross the city along the axes, like the cross lines in the sketch.
    /// </summary>
    public static class CityMapBuilder
    {
        const string ScenePath = "Assets/Scenes/City map.unity";
        const string MeshAssetPath = CityAssets.Root + "/CityMeshes.asset";
        const string RootName = "CityMap (generated)";
        const float PadY = 0.15f;
        const int Seed = 4242;

        // ring roads: inner edge, outer edge
        static readonly (float r0, float r1)[] RingRoads = { (70, 82), (170, 184), (320, 334), (450, 464) };
        const float CityEdge = 466f;
        const float AvenueHalf = 9f, StreetHalf = 5.5f;

        struct Sep { public float ang, hw; public Sep(float a, float h) { ang = a; hw = h; } }

        class Band
        {
            public string name;
            public int index;
            public float r0, r1;
            public List<Sep> seps = new List<Sep>();
        }

        struct Footprint
        {
            public Vector2 c, half; public float yaw;
            public Footprint(Vector2 c, Vector2 half, float yaw) { this.c = c; this.half = half; this.yaw = yaw; }
        }

        // ------------------------------------------------------------------ state for one build
        static CityAssets A;
        static BuildingFactory BF;
        static Furniture F;
        static PropBatcher Props;
        static System.Random Rng;
        static Mesh _meshContainer;
        static int _meshCount;
        static readonly List<Footprint> Placed = new List<Footprint>();
        static readonly List<Light> StreetLights = new List<Light>();
        static Transform _root, _lightsRoot, _questRoot;

        [MenuItem("HORSE/Build City Map", priority = 0)]
        public static void BuildMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build();
        }

        [MenuItem("HORSE/Bake Occlusion Culling (optional, faster runtime)", priority = 20)]
        public static void BakeOcclusion()
        {
            StaticOcclusionCulling.smallestOccluder = 6f;
            StaticOcclusionCulling.smallestHole = 0.5f;
            StaticOcclusionCulling.backfaceThreshold = 100f;
            StaticOcclusionCulling.Compute();
        }

        public static void Build()
        {
            try
            {
                Progress("Opening scene", 0f);
                Scene scene = OpenOrCreateScene();

                Progress("Generating textures and materials", 0.05f);
                A = CityAssets.Create();
                BF = new BuildingFactory(A);
                F = new Furniture(A);
                Rng = new System.Random(Seed);
                Placed.Clear();
                StreetLights.Clear();
                _meshCount = 0;
                Props = new PropBatcher(RingRoads[0].r1 - 0.01f, RingRoads[1].r1, RingRoads[2].r1, RingRoads[3].r1, 600f);

                foreach (GameObject go in scene.GetRootGameObjects())
                    if (go.name == RootName) Object.DestroyImmediate(go);
                AssetDatabase.DeleteAsset(MeshAssetPath);
                _meshContainer = new Mesh { name = "CityMeshes" };
                AssetDatabase.CreateAsset(_meshContainer, MeshAssetPath);

                _root = new GameObject(RootName).transform;
                _lightsRoot = Child(_root, "StreetLights");
                _questRoot = Child(_root, "QuestPoints");

                List<Band> bands = MakeBands();

                Progress("Roads and blocks", 0.15f);
                BuildGround(bands);
                BuildRoadMarkings(bands);

                Progress("Landmarks", 0.25f);
                BuildLandmarks(bands);

                Progress("Buildings", 0.4f);
                BuildDistricts(bands);

                Progress("Street furniture", 0.7f);
                BuildPlaza();
                BuildPowerLines(bands);
                BuildStreetLamps(bands);
                BuildTrees(bands);
                BuildSkyline();

                Progress("Combining props", 0.85f);
                FlushProps();

                Progress("Lighting", 0.92f);
                SetupLighting(scene);

                var culler = _lightsRoot.gameObject.AddComponent<CityLightCuller>();
                culler.lights = new List<Light>(StreetLights);

                AssetDatabase.SaveAssets();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Selection.activeTransform = _root;
                Debug.Log($"[HORSE] City map built (generator v5): {_meshCount} meshes, {Placed.Count} footprints, {StreetLights.Count} street lamps. Scene saved to {scene.path}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        static void Progress(string what, float t) => EditorUtility.DisplayProgressBar("HORSE city map", what, t);

        static Scene OpenOrCreateScene()
        {
            string path = ScenePath;
            if (!System.IO.File.Exists(path))
            {
                // the scene may live somewhere else in the project
                foreach (string guid in AssetDatabase.FindAssets("t:Scene City map"))
                {
                    string p = AssetDatabase.GUIDToAssetPath(guid);
                    if (System.IO.Path.GetFileNameWithoutExtension(p) == "City map") { path = p; break; }
                }
            }
            if (System.IO.File.Exists(path))
                return EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            CityAssets.EnsureFolder("Assets/Scenes");
            Scene s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(s, ScenePath);
            return s;
        }

        static List<Band> MakeBands()
        {
            var bands = new List<Band>
            {
                new Band { name = "Inner", index = 1, r0 = RingRoads[0].r1, r1 = RingRoads[1].r0 },
                new Band { name = "Middle", index = 2, r0 = RingRoads[1].r1, r1 = RingRoads[2].r0 },
                new Band { name = "Outer", index = 3, r0 = RingRoads[2].r1, r1 = RingRoads[3].r0 },
            };
            foreach (Band b in bands)
            {
                for (int k = 0; k < 4; k++) b.seps.Add(new Sep(k * 90f, AvenueHalf));
                for (int k = 0; k < 4; k++) b.seps.Add(new Sep(b.index == 1 ? 45f + k * 90f : 22.5f + k * 90f, StreetHalf));
                if (b.index != 1) for (int k = 0; k < 4; k++) b.seps.Add(new Sep(67.5f + k * 90f, StreetHalf));
                b.seps.Sort((x, y) => x.ang.CompareTo(y.ang));
            }
            return bands;
        }

        // ================================================================== spawning helpers

        static Transform Child(Transform parent, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        static Mesh SaveMesh(Mesh m)
        {
            AssetDatabase.AddObjectToAsset(m, _meshContainer);
            _meshCount++;
            return m;
        }

        /// <summary>Creates a static GameObject from a Part. Returns null if the part is empty.</summary>
        static GameObject Spawn(Part p, string name, Transform parent, Vector3 pos, float yaw, bool collider, bool shadows,
                                StaticEditorFlags flags = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic |
                                                          StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic,
                                float cullScreenSize = 0f)
        {
            if (p == null || p.IsEmpty) return null;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            Mesh mesh = SaveMesh(p.kit.ToMesh(name));
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = p.mats.ToArray();
            mr.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            mr.receiveGI = ReceiveGI.LightProbes;
            mr.lightProbeUsage = LightProbeUsage.Off;
            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            if (collider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
            GameObjectUtility.SetStaticEditorFlags(go, flags);
            if (cullScreenSize > 0f)
            {
                var lod = go.AddComponent<LODGroup>();
                lod.SetLODs(new[] { new LOD(cullScreenSize, new Renderer[] { mr }) });
                lod.RecalculateBounds();
            }
            return go;
        }

        static Light MakeLight(Transform parent, string name, Vector3 localPos, LightSpec s)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = s.color;
            l.range = s.range;
            l.intensity = s.intensity;
            l.shadows = LightShadows.None;
            l.renderMode = LightRenderMode.Auto;
            l.lightmapBakeType = LightmapBakeType.Realtime;
            return l;
        }

        static void AddQuestPoint(QuestPointKind kind, Vector3 worldPos, float yaw, string note)
        {
            var go = new GameObject("QP_" + kind);
            go.transform.SetParent(_questRoot, false);
            go.transform.position = worldPos;
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var q = go.AddComponent<QuestPoint>();
            q.kind = kind;
            q.note = note;
        }

        static Vector3 Polar(float angDeg, float r, float y = 0f)
        {
            float a = angDeg * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r);
        }

        /// <summary>Yaw that turns local -Z (a building's front) toward the city center (inward) or away from it.</summary>
        static float FacingYaw(float angDeg, bool faceInward) => faceInward ? 90f - angDeg : 270f - angDeg;

        // ================================================================== ground, roads, blocks

        static void BuildGround(List<Band> bands)
        {
            Transform t = Child(_root, "Ground");

            var asphalt = new Part();
            asphalt.kit.RingSector(asphalt.S(A.Asphalt), 0f, CityEdge, 0f, 360f, 0f, 0f, 8f);
            Spawn(asphalt, "Roads_Asphalt", t, Vector3.zero, 0, true, false, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);

            var outskirts = new Part();
            outskirts.kit.RingSector(outskirts.S(A.Grass), CityEdge, 1500f, 0f, 360f, 0.02f, 0f, 6f);
            Spawn(outskirts, "Outskirts_Grass", t, Vector3.zero, 0, true, false, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);

            var plaza = new Part();
            plaza.kit.RingSector(plaza.S(A.Plaza), 0f, RingRoads[0].r0, 0f, 360f, PadY, PadY, 6f, plaza.S(A.Curb));
            Spawn(plaza, "Plaza_Pad", t, Vector3.zero, 0, true, false, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);

            foreach (Band b in bands)
            {
                var pad = new Part();
                int top = pad.S(A.Pavement), side = pad.S(A.Curb);
                for (int k = 0; k < b.seps.Count; k++)
                {
                    Sep s0 = b.seps[k];
                    Sep s1 = b.seps[(k + 1) % b.seps.Count];
                    float a1 = s1.ang + (k == b.seps.Count - 1 ? 360f : 0f);
                    StraightPad(pad.kit, top, side, b.r0, b.r1, s0, new Sep(a1, s1.hw), PadY);
                }
                Spawn(pad, "Blocks_" + b.name, t, Vector3.zero, 0, true, false, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
            }
        }

        /// <summary>Sidewalk block between two radial streets. Side edges stay parallel to the streets.</summary>
        static void StraightPad(MeshKit k, int top, int side, float r0, float r1, Sep a, Sep b, float y)
        {
            int nr = Mathf.Max(1, Mathf.CeilToInt((r1 - r0) / 12f));
            float arc = (b.ang - a.ang) * Mathf.Deg2Rad * r1;
            int na = Mathf.Max(2, Mathf.CeilToInt(arc / 8f));
            var P = new Vector3[nr + 1, na + 1];
            for (int j = 0; j <= nr; j++)
            {
                float r = Mathf.Lerp(r0, r1, (float)j / nr);
                float t0 = a.ang + Mathf.Asin(Mathf.Clamp01(a.hw / r)) * Mathf.Rad2Deg;
                float t1 = b.ang - Mathf.Asin(Mathf.Clamp01(b.hw / r)) * Mathf.Rad2Deg;
                for (int i = 0; i <= na; i++) P[j, i] = Polar(Mathf.Lerp(t0, t1, (float)i / na), r, y);
            }
            Vector2 Uv(Vector3 p) => new Vector2(p.x / 4f, p.z / 4f);
            Vector3 dn = Vector3.down * y;
            Vector2 u00 = Vector2.zero, u10 = Vector2.right, u11 = Vector2.one, u01 = Vector2.up;
            for (int j = 0; j < nr; j++)
                for (int i = 0; i < na; i++)
                    k.Quad(top, P[j, i], P[j + 1, i], P[j + 1, i + 1], P[j, i + 1], Uv(P[j, i]), Uv(P[j + 1, i]), Uv(P[j + 1, i + 1]), Uv(P[j, i + 1]));
            for (int i = 0; i < na; i++)
            {
                Vector3 o0 = P[nr, i], o1 = P[nr, i + 1], i0 = P[0, i], i1 = P[0, i + 1];
                k.Quad(side, o0 + dn, o1 + dn, o1, o0, u00, u10, u11, u01);
                k.Quad(side, i1 + dn, i0 + dn, i0, i1, u00, u10, u11, u01);
            }
            for (int j = 0; j < nr; j++)
            {
                Vector3 i0 = P[j, 0], o0 = P[j + 1, 0], i1 = P[j, na], o1 = P[j + 1, na];
                k.Quad(side, i0 + dn, o0 + dn, o0, i0, u00, u10, u11, u01);
                k.Quad(side, o1 + dn, i1 + dn, i1, o1, u00, u10, u11, u01);
            }
        }

        static bool OnRingRoad(float r, float margin = 0f)
        {
            foreach (var rr in RingRoads) if (r > rr.r0 - margin && r < rr.r1 + margin) return true;
            return false;
        }

        static void BuildRoadMarkings(List<Band> bands)
        {
            const float y = 0.012f;
            // dashed center line on every ring road
            foreach (var rr in RingRoads)
            {
                float rm = (rr.r0 + rr.r1) * 0.5f;
                float step = 9f / rm * Mathf.Rad2Deg;
                for (float a = 0; a < 360f; a += step)
                {
                    if (NearAxis(a, rm, AvenueHalf + 1f)) continue;
                    Vector3 p = Polar(a, rm, y);
                    Props.Get(p, PropBatcher.Kind.Detail).Box(A.LaneYellow, p, new Vector3(0.18f, 0.02f, 3f), -a);
                }
            }
            // avenues: double yellow center line, dashed lanes, crosswalks at every ring road
            for (int k = 0; k < 4; k++)
            {
                float ang = k * 90f;
                float yaw = 90f - ang;
                for (float r = RingRoads[0].r1; r < CityEdge; r += 3f)
                {
                    if (OnRingRoad(r, 0.5f)) continue;
                    Vector3 c = Polar(ang, r, y);
                    Vector3 side = Polar(ang + 90f, 1f) ;
                    Part p = Props.Get(c, PropBatcher.Kind.Detail);
                    p.Box(A.LaneYellow, c + side * 0.2f, new Vector3(0.14f, 0.02f, 3f), yaw);
                    p.Box(A.LaneYellow, c - side * 0.2f, new Vector3(0.14f, 0.02f, 3f), yaw);
                    if (Mathf.RoundToInt(r / 3f) % 3 == 0)
                    {
                        p.Box(A.LaneWhite, c + side * 4.5f, new Vector3(0.14f, 0.02f, 3f), yaw);
                        p.Box(A.LaneWhite, c - side * 4.5f, new Vector3(0.14f, 0.02f, 3f), yaw);
                    }
                }
                foreach (var rr in RingRoads)
                    foreach (float r in new[] { rr.r0 - 2.5f, rr.r1 + 2.5f })
                    {
                        if (r < RingRoads[0].r1) continue;
                        for (float x = -AvenueHalf + 0.8f; x < AvenueHalf; x += 1.2f)
                        {
                            Vector3 c = Polar(ang, r, y) + Polar(ang + 90f, x);
                            Props.Get(c, PropBatcher.Kind.Detail).Box(A.Crosswalk, c, new Vector3(0.6f, 0.02f, 3.2f), yaw);
                        }
                    }
            }
        }

        static bool NearAxis(float angDeg, float r, float hw)
        {
            for (int k = 0; k < 4; k++)
            {
                float d = Mathf.DeltaAngle(angDeg, k * 90f);
                if (Mathf.Abs(d) < 90f && Mathf.Abs(Mathf.Sin(d * Mathf.Deg2Rad) * r) < hw) return true;
            }
            return false;
        }

        // ================================================================== landmarks

        static void BuildLandmarks(List<Band> bands)
        {
            var lm = new Landmarks(A);
            Transform t = Child(_root, "Landmarks");

            // biggest building, dead center
            Place(lm.CentralTower(), t, 0f, 0f, 0f, false);

            Band inner = bands[0], middle = bands[1];
            // inner ring: vet on the west side, hidden weapon shop in the north-east
            PlaceOnFrontage(lm.Vet(), t, 160f, inner.r0, true, true);
            Landmark shop = lm.WeaponShop();
            float shopAng = 28f, shopR = 140f;
            Place(shop, t, shopAng, shopR, FacingYaw(shopAng, false), false);
            // keep a narrow alley open from the shop to the outer ring road, everything else crowds in
            Vector3 alleyC = Polar(shopAng, (shopR + 4f + inner.r1) * 0.5f);
            Placed.Add(new Footprint(new Vector2(alleyC.x, alleyC.z), new Vector2(2.2f, (inner.r1 - shopR - 4f) * 0.5f), FacingYaw(shopAng, false)));

            // middle ring: horse auction house NE, police station + stable SW
            PlaceOnFrontage(lm.AuctionHouse(), t, 45f, middle.r0, true, true);
            PlaceOnFrontage(lm.PoliceStation(), t, 214f, middle.r0, true, true);
            PlaceOnFrontage(lm.PoliceStable(), t, 233f, middle.r0, true, true);
        }

        /// <summary>Places a landmark so its lot starts 3m behind the curb of the road at radius roadEdge.</summary>
        static void PlaceOnFrontage(Landmark L, Transform parent, float ang, float roadEdge, bool faceInward, bool interior)
        {
            float r = faceInward ? roadEdge + 3f - L.lot.yMin : roadEdge - 3f + L.lot.yMin;
            Place(L, parent, ang, r, FacingYaw(ang, faceInward), interior);
        }

        static void Place(Landmark L, Transform parent, float ang, float r, float yaw, bool interior)
        {
            Vector3 pos = Polar(ang, r, PadY);
            Transform root = Child(parent, L.name);
            root.localPosition = pos;
            root.localRotation = Quaternion.Euler(0, yaw, 0);

            Spawn(L.shell, L.name + "_Shell", root, Vector3.zero, 0, true, true);
            Spawn(L.interior, L.name + "_Interior", root, Vector3.zero, 0, true, false,
                  StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic, 0.01f);
            Spawn(L.yard, L.name + "_Yard", root, Vector3.zero, 0, true, false,
                  StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic, 0.005f);

            if (L.interiorLights.Count > 0)
            {
                Transform lt = Child(root, "InteriorLights");
                var zone = root.gameObject.AddComponent<InteriorZone>();
                zone.localBounds = L.interiorBounds;
                zone.interiorLights = L.interiorLights.Select((s, i) => MakeLight(lt, "Light_" + i, s.pos, s)).ToArray();
            }
            for (int i = 0; i < L.outdoorLights.Count; i++)
            {
                LightSpec s = L.outdoorLights[i];
                Light l = MakeLight(_lightsRoot, L.name + "_Lamp_" + i, Vector3.zero, s);
                l.transform.position = root.TransformPoint(s.pos);
                StreetLights.Add(l);
            }
            foreach (var q in L.points)
                AddQuestPoint(q.kind, root.TransformPoint(q.pos), yaw, q.note);

            // reserve the lot so generic buildings stay out
            Vector2 lc = L.lot.center;
            Vector3 wc = root.TransformPoint(new Vector3(lc.x, 0, lc.y));
            Placed.Add(new Footprint(new Vector2(wc.x, wc.z), L.lot.size * 0.5f + Vector2.one * 2f, yaw));
        }

        // ================================================================== generic districts

        enum Row { Inward, Outward, Middle }

        static void BuildDistricts(List<Band> bands)
        {
            Transform t = Child(_root, "Districts");
            foreach (Band b in bands)
            {
                var groups = new Dictionary<string, Transform>();
                Transform Group(float ang)
                {
                    string q = ang < 90 ? "NE" : ang < 180 ? "NW" : ang < 270 ? "SW" : "SE";
                    if (!groups.TryGetValue(q, out Transform g)) groups[q] = g = Child(t, b.name + "_" + q);
                    return g;
                }

                float depth = b.r1 - b.r0;
                var rows = new List<(Row row, float front)> { (Row.Inward, b.r0 + 3f), (Row.Outward, b.r1 - 3f) };
                if (depth > 80f) rows.Add((Row.Middle, (b.r0 + b.r1) * 0.5f));

                for (int k = 0; k < b.seps.Count; k++)
                {
                    Sep s0 = b.seps[k];
                    Sep s1 = b.seps[(k + 1) % b.seps.Count];
                    float a0 = s0.ang, a1 = s1.ang + (k == b.seps.Count - 1 ? 360f : 0f);
                    foreach (var row in rows)
                        FillRow(b, row.row, row.front, a0, a1, Group);
                }
            }

            // city edge: scattered warehouses and workshops past the last ring road
            Transform edge = Child(t, "Edge_Warehouses");
            for (float a = 3f; a < 360f; a += 360f / 70f)
            {
                if (Rng.NextDouble() < 0.35 || NearAxis(a, 490f, AvenueHalf + 12f)) continue;
                float r = CityEdge + 12f + (float)Rng.NextDouble() * 40f;
                float w = 14f + (float)Rng.NextDouble() * 14f, d = 12f + (float)Rng.NextDouble() * 10f;
                Vector3 p = Polar(a, r, 0f);
                float yaw = FacingYaw(a, true) + (float)(Rng.NextDouble() * 20 - 10);
                var fp = new Footprint(new Vector2(p.x, p.z), new Vector2(w, d) * 0.5f + Vector2.one, yaw);
                if (Overlaps(fp)) continue;
                Placed.Add(fp);
                GameObject go = Spawn(BF.Warehouse(w, d, Rng), "Warehouse_" + Placed.Count, edge, p, yaw, true, true, cullScreenSize: 0.01f);
                if (Rng.NextDouble() < 0.12 && go)
                    AddQuestPoint(QuestPointKind.SuspectHideout, go.transform.TransformPoint(new Vector3(0, 0, -d * 0.5f - 2f)), yaw, "Possible scammer hideout (edge warehouse)");
            }
        }

        static void FillRow(Band b, Row row, float front, float a0, float a1, System.Func<float, Transform> group)
        {
            float midAng = (a0 + a1) * 0.5f;
            float cur = a0;
            int guard = 0;
            while (guard++ < 400)
            {
                float ang = Mathf.Repeat(cur, 360f);
                (BuildingStyle style, float w, float d, float h, int floors) = Choose(b, row, ang);
                float extraFront = style == BuildingStyle.House ? 3.4f : (style == BuildingStyle.Apartment ? 1.5f : (style == BuildingStyle.Shophouse ? 2f : 0f));
                float fd = d + extraFront;

                bool inward = row == Row.Inward || (row == Row.Middle && Rng.NextDouble() < 0.5);
                float rc = row == Row.Inward ? front + fd * 0.5f : row == Row.Outward ? front - fd * 0.5f : front;
                float placeAng = cur + (w * 0.5f + 1f) / rc * Mathf.Rad2Deg;
                if (placeAng > a1) break;

                float yaw = FacingYaw(placeAng, row == Row.Middle ? inward : row == Row.Inward);
                Vector3 pc = Polar(placeAng, rc);
                var fp = new Footprint(new Vector2(pc.x, pc.z), new Vector2(w * 0.5f + 0.6f, fd * 0.5f + 0.3f), yaw);

                if (!InsideBand(fp, b) || Overlaps(fp))
                {
                    cur += 2f / rc * Mathf.Rad2Deg;
                    continue;
                }
                Placed.Add(fp);

                // model origin is the building footprint center; shift back so front extras sit on the sidewalk
                Vector3 local = new Vector3(0, 0, extraFront * 0.5f);
                Vector3 pos = pc + Quaternion.Euler(0, yaw, 0) * local + Vector3.up * PadY;
                Part part;
                switch (style)
                {
                    case BuildingStyle.GlassTower: part = BF.GlassTower(w, d, h, Rng); break;
                    case BuildingStyle.Office: part = BF.Office(w, d, h, Rng); break;
                    case BuildingStyle.Apartment: part = BF.Apartment(w, d, h, Rng); break;
                    case BuildingStyle.Shophouse: part = BF.Shophouse(w, d, floors, Rng); break;
                    case BuildingStyle.House: part = BF.House(w, d, Rng); break;
                    default: part = BF.Warehouse(w, d, Rng); break;
                }
                bool small = style == BuildingStyle.House || style == BuildingStyle.Shophouse || style == BuildingStyle.Warehouse;
                Spawn(part, $"{style}_{Placed.Count:000}", group(Mathf.Repeat(placeAng, 360f)), pos, yaw, true, true,
                      cullScreenSize: small ? 0.012f : 0.003f);

                // horses need somewhere to park: hitching rails and troughs outside some shops
                if (style == BuildingStyle.Shophouse && Rng.NextDouble() < 0.3)
                {
                    Vector3 hp = pos + Quaternion.Euler(0, yaw, 0) * new Vector3(0, 0, -d * 0.5f - 2.6f);
                    Part pp = Props.Get(hp, PropBatcher.Kind.Solid);
                    F.HitchingRail(pp, hp, yaw, 3f);
                    if (Rng.NextDouble() < 0.6) F.Trough(pp, hp + Quaternion.Euler(0, yaw, 0) * new Vector3(2.8f, 0, 0.4f), yaw + 90f, 1.6f);
                }
                if (style == BuildingStyle.Warehouse && b.index == 3 && Rng.NextDouble() < 0.25)
                    AddQuestPoint(QuestPointKind.SuspectHideout, pos + Quaternion.Euler(0, yaw, 0) * new Vector3(0, 0, -d * 0.5f - 2f), yaw, "Possible scammer hideout");

                float gap = style == BuildingStyle.Shophouse ? 0.4f : (1.2f + (float)Rng.NextDouble() * 3f);
                cur = placeAng + (w * 0.5f + gap) / rc * Mathf.Rad2Deg;
            }
        }

        static (BuildingStyle, float w, float d, float h, int floors) Choose(Band b, Row row, float ang)
        {
            System.Random r = Rng;
            float R(float a, float c) => a + (float)r.NextDouble() * (c - a);
            bool ne = ang < 90, nw = ang >= 90 && ang < 180, sw = ang >= 180 && ang < 270;
            double roll = r.NextDouble();
            BuildingStyle s;
            float tallnessCap = 1f;

            if (b.index == 1) // inner ring: downtown towers, older shophouse quarter in the NE hiding the weapon shop
            {
                if (ne) s = roll < 0.55 ? BuildingStyle.Shophouse : roll < 0.8 ? BuildingStyle.Office : BuildingStyle.Apartment;
                else if (nw) s = roll < 0.4 ? BuildingStyle.GlassTower : roll < 0.75 ? BuildingStyle.Office : BuildingStyle.Apartment;
                else s = roll < 0.6 ? BuildingStyle.GlassTower : roll < 0.9 ? BuildingStyle.Office : BuildingStyle.Apartment;
            }
            else if (b.index == 2)
            {
                if (nw) s = roll < 0.5 ? BuildingStyle.House : roll < 0.85 ? BuildingStyle.Shophouse : BuildingStyle.Apartment;
                else if (ne) s = roll < 0.5 ? BuildingStyle.Shophouse : roll < 0.85 ? BuildingStyle.Apartment : BuildingStyle.Office;
                else if (sw) s = roll < 0.5 ? BuildingStyle.Shophouse : roll < 0.8 ? BuildingStyle.Apartment : BuildingStyle.Warehouse;
                else s = roll < 0.45 ? BuildingStyle.Apartment : roll < 0.75 ? BuildingStyle.Office : BuildingStyle.GlassTower;
            }
            else
            {
                if (nw && roll < 0.3) s = BuildingStyle.House;
                else s = roll < 0.35 ? BuildingStyle.Apartment : roll < 0.65 ? BuildingStyle.Shophouse : roll < 0.8 ? BuildingStyle.Warehouse : roll < 0.9 ? BuildingStyle.Office : BuildingStyle.GlassTower;
            }
            if (row == Row.Middle && s == BuildingStyle.GlassTower) s = BuildingStyle.Office;
            // the back-of-block row is shallower so it fits between the two street fronts
            if (row == Row.Middle && b.index == 1) tallnessCap = 0.6f;

            float tallness = (b.index == 1 ? 1f : b.index == 2 ? 0.55f : 0.45f) * tallnessCap;
            switch (s)
            {
                case BuildingStyle.GlassTower:
                {
                    float w = BuildingFactory.Snap(R(24, 40), CityAssets.GlassCell);
                    float d = BuildingFactory.Snap(R(22, 34), CityAssets.GlassCell);
                    float h = Mathf.Round(R(70, 175) * tallness / 3.6f) * 3.6f + 7.2f;
                    return (s, w, d, Mathf.Max(43.2f, h), 0);
                }
                case BuildingStyle.Office:
                {
                    float w = BuildingFactory.Snap(R(20, 34), CityAssets.OfficeCell);
                    float d = BuildingFactory.Snap(R(18, 28), CityAssets.OfficeCell);
                    float h = Mathf.Round(R(35, 95) * tallness / 3.6f) * 3.6f;
                    return (s, w, d, Mathf.Max(21.6f, h), 0);
                }
                case BuildingStyle.Apartment:
                {
                    float w = BuildingFactory.Snap(R(16, 32), CityAssets.AptCell * 2f);
                    float d = BuildingFactory.Snap(R(12, 18), CityAssets.AptCell);
                    float h = R(b.index == 1 ? 30 : 18, b.index == 1 ? 70 : 60);
                    return (s, w, d, h, 0);
                }
                case BuildingStyle.Shophouse:
                {
                    float w = CityAssets.ShopCell * r.Next(2, 6);
                    float d = R(12, 16);
                    return (s, w, d, 0, r.Next(2, b.index == 1 ? 6 : 5));
                }
                case BuildingStyle.House:
                    return (s, R(8, 12), R(9, 11), 0, 0);
                default:
                    return (s, R(14, 26), R(12, 20), 0, 0);
            }
        }

        static bool InsideBand(Footprint f, Band b)
        {
            foreach (Vector2 p in Corners(f))
            {
                float r = p.magnitude;
                if (r < b.r0 + 0.8f || r > b.r1 - 0.8f) return false;
                foreach (Sep s in b.seps)
                    if (Lateral(p, s.ang) < s.hw + 1.5f && Along(p, s.ang) > 0) return false;
            }
            // a footprint must not straddle a street even if all corners clear it
            foreach (Sep s in b.seps)
            {
                int sign = 0;
                foreach (Vector2 p in Corners(f))
                {
                    if (Along(p, s.ang) <= 0) continue;
                    int sg = Cross(p, s.ang) > 0 ? 1 : -1;
                    if (sign != 0 && sg != sign) return false;
                    sign = sg;
                }
            }
            return true;
        }

        static float Along(Vector2 p, float ang) { float a = ang * Mathf.Deg2Rad; return p.x * Mathf.Cos(a) + p.y * Mathf.Sin(a); }
        static float Cross(Vector2 p, float ang) { float a = ang * Mathf.Deg2Rad; return p.x * Mathf.Sin(a) - p.y * Mathf.Cos(a); }
        static float Lateral(Vector2 p, float ang) => Mathf.Abs(Cross(p, ang));

        static Vector2[] Corners(Footprint f)
        {
            Vector2 ax = Axis(f.yaw, true) * f.half.x, az = Axis(f.yaw, false) * f.half.y;
            return new[] { f.c - ax - az, f.c + ax - az, f.c + ax + az, f.c - ax + az };
        }

        /// <summary>World xz direction of the local X (or Z) axis for a yaw in degrees.</summary>
        static Vector2 Axis(float yaw, bool x)
        {
            Vector3 v = Quaternion.Euler(0, yaw, 0) * (x ? Vector3.right : Vector3.forward);
            return new Vector2(v.x, v.z);
        }

        static bool Overlaps(Footprint f)
        {
            foreach (Footprint o in Placed)
            {
                if ((o.c - f.c).sqrMagnitude > Sq(o.half.magnitude + f.half.magnitude)) continue;
                if (SatOverlap(f, o)) return true;
            }
            return false;
        }

        static float Sq(float v) => v * v;

        static bool SatOverlap(Footprint a, Footprint b)
        {
            Vector2[] ca = Corners(a), cb = Corners(b);
            Vector2[] axes = { Axis(a.yaw, true), Axis(a.yaw, false), Axis(b.yaw, true), Axis(b.yaw, false) };
            foreach (Vector2 ax in axes)
            {
                float minA = float.MaxValue, maxA = float.MinValue, minB = float.MaxValue, maxB = float.MinValue;
                foreach (Vector2 p in ca) { float d = Vector2.Dot(p, ax); minA = Mathf.Min(minA, d); maxA = Mathf.Max(maxA, d); }
                foreach (Vector2 p in cb) { float d = Vector2.Dot(p, ax); minB = Mathf.Min(minB, d); maxB = Mathf.Max(maxB, d); }
                if (maxA < minB || maxB < minA) return false;
            }
            return true;
        }

        // ================================================================== plaza

        static void BuildPlaza()
        {
            var p = new Part();
            float ringR = RingRoads[0].r0;
            for (int k = 0; k < 4; k++)
            {
                float a = 45f + k * 90f;
                for (int j = -1; j <= 1; j++)
                    F.Planter(p, Polar(a + j * 9f, 52f, PadY), 3f, 100 + k * 3 + j);
            }
            for (float a = 0; a < 360f; a += 15f)
            {
                if (Mathf.Abs(Mathf.DeltaAngle(a, Mathf.Round(a / 90f) * 90f)) < 1f) continue;
                F.Bench(p, Polar(a, ringR - 4f, PadY), FacingYaw(a, true) + 180f, 2.4f);
            }

            // dry fountain in the north-east and an equestrian statue in the south-west
            Vector3 fc = Polar(45f, 42f, PadY);
            p.Cylinder(A.Concrete, fc, 5f, 0.7f, 16);
            p.Cylinder(A.Water, fc + Vector3.up * 0.55f, 4.6f, 0.2f, 16);
            p.Cylinder(A.Concrete, fc, 0.8f, 2.6f, 8);
            p.Cylinder(A.Concrete, fc + Vector3.up * 2.6f, 1.8f, 0.3f, 10);
            Statue(p, Polar(225f, 44f, PadY), FacingYaw(225f, false));

            Spawn(p, "Plaza_Furniture", Child(_root, "Plaza"), Vector3.zero, 0, true, true,
                  StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
        }

        static void Statue(Part p, Vector3 at, float yaw)
        {
            p.Local(at, yaw, () =>
            {
                p.Box(A.Concrete, new Vector3(0, 1.6f, 0), new Vector3(4f, 3.2f, 7f));
                p.Box(A.Trim, new Vector3(0, 3.3f, 0), new Vector3(4.4f, 0.2f, 7.4f));
                Matrix4x4 saved = p.Frame;
                p.Frame = saved * Matrix4x4.TRS(new Vector3(0, 3.4f, 0), Quaternion.identity, Vector3.one * 2.2f);
                Material m = A.Metal;
                p.Box(m, new Vector3(0, 1.5f, 0), new Vector3(0.6f, 0.65f, 1.6f));
                p.Beam(m, new Vector3(0, 1.7f, 0.6f), new Vector3(0, 2.35f, 1.05f), 0.32f);
                p.Beam(m, new Vector3(0, 2.45f, 1.05f), new Vector3(0, 2.15f, 1.55f), 0.26f);
                p.Beam(m, new Vector3(-0.2f, 1.25f, -0.6f), new Vector3(-0.2f, 0f, -0.65f), 0.14f);
                p.Beam(m, new Vector3(0.2f, 1.25f, -0.6f), new Vector3(0.2f, 0f, -0.6f), 0.14f);
                p.Beam(m, new Vector3(-0.2f, 1.25f, 0.6f), new Vector3(-0.2f, 0f, 0.7f), 0.14f);
                p.Beam(m, new Vector3(0.2f, 1.25f, 0.6f), new Vector3(0.25f, 0.75f, 1.0f), 0.14f); // raised hoof
                p.Beam(m, new Vector3(0, 1.65f, -0.8f), new Vector3(0, 0.9f, -1.1f), 0.12f);       // tail
                p.Beam(m, new Vector3(0, 1.8f, 0.05f), new Vector3(0, 2.7f, 0.1f), 0.34f);         // rider
                p.Box(m, new Vector3(0, 2.95f, 0.12f), new Vector3(0.25f, 0.28f, 0.25f));
                p.Box(m, new Vector3(0, 3.12f, 0.12f), new Vector3(0.45f, 0.05f, 0.45f));          // hat brim
                p.Beam(m, new Vector3(0.2f, 2.55f, 0.15f), new Vector3(0.35f, 3.3f, 0.45f), 0.09f); // raised arm
                p.Beam(m, new Vector3(-0.25f, 1.9f, 0.1f), new Vector3(-0.32f, 1.25f, 0.25f), 0.12f);
                p.Beam(m, new Vector3(0.25f, 1.9f, 0.1f), new Vector3(0.32f, 1.25f, 0.25f), 0.12f);
                p.Frame = saved;
            });
        }

        // ================================================================== power lines (Thai / Japanese street feel)

        static void BuildPowerLines(List<Band> bands)
        {
            var runs = new List<List<Vector3>>();
            // along ring roads 2, 3 and 4 on the city side of the curb
            foreach (int ri in new[] { 1, 2, 3 })
            {
                float r = ri == 3 ? RingRoads[ri].r0 - 1.2f : RingRoads[ri].r1 + 1.2f;
                Band band = bands.First(b => r > b.r0 && r < b.r1);
                var run = new List<Vector3>();
                float step = 30f / r * Mathf.Rad2Deg;
                for (float a = 2f; a < 362f; a += step)
                {
                    Vector3 p = Polar(a, r, PadY);
                    if (band.seps.Any(s => Lateral(new Vector2(p.x, p.z), s.ang) < s.hw + 1.5f && Along(new Vector2(p.x, p.z), s.ang) > 0))
                    {
                        if (run.Count > 1) runs.Add(run);
                        run = new List<Vector3>();
                        continue;
                    }
                    run.Add(p);
                }
                if (run.Count > 1) runs.Add(run);
            }
            // along the side streets of the middle and outer rings
            foreach (Band b in bands.Where(b => b.index > 1))
                foreach (Sep s in b.seps.Where(s => s.hw < AvenueHalf))
                {
                    var run = new List<Vector3>();
                    for (float r = b.r0 + 4f; r < b.r1 - 2f; r += 28f)
                        run.Add(Polar(s.ang, r, PadY) + Polar(s.ang + 90f, s.hw + 1.2f));
                    if (run.Count > 1) runs.Add(run);
                }

            foreach (List<Vector3> run in runs)
            {
                for (int i = 0; i < run.Count; i++)
                {
                    Vector3 p = run[i];
                    Vector3 next = i + 1 < run.Count ? run[i + 1] : p + (p - run[i - 1]);
                    Vector3 dir = (next - p).normalized;
                    float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                    Part solid = Props.Get(p, PropBatcher.Kind.Solid);
                    solid.Cylinder(A.Concrete, p, 0.17f, 10.5f, 6);
                    solid.Local(p, yaw, () =>
                    {
                        solid.Box(A.Metal, new Vector3(0, 9f, 0), new Vector3(2f, 0.12f, 0.12f));
                        solid.Box(A.Metal, new Vector3(0, 7.4f, 0), new Vector3(1.2f, 0.1f, 0.1f));
                        if ((i * 7 + run.Count) % 4 == 0)
                            solid.Cylinder(A.Metal, new Vector3(0.45f, 6.2f, 0), 0.32f, 0.9f, 6); // transformer
                    });
                    if (i + 1 >= run.Count) continue;

                    // wires to the next pole, sagging in three segments
                    Vector3 side = Quaternion.Euler(0, yaw, 0) * Vector3.right;
                    Part det = Props.Get(p, PropBatcher.Kind.Detail);
                    (float off, float y, float t)[] wires = { (-0.9f, 9.05f, 0.03f), (0.9f, 9.05f, 0.03f), (0f, 10.4f, 0.025f), (-0.5f, 7.45f, 0.07f), (0.5f, 7.45f, 0.05f) };
                    foreach (var w in wires)
                    {
                        Vector3 a = p + side * w.off + Vector3.up * w.y;
                        Vector3 b = run[i + 1] + side * w.off + Vector3.up * w.y;
                        float sag = 0.4f + Mathf.Abs(w.off) * 0.2f + (w.y < 8f ? 0.4f : 0f);
                        Vector3 prev = a;
                        for (int sgm = 1; sgm <= 3; sgm++)
                        {
                            float t = sgm / 3f;
                            Vector3 cur = Vector3.Lerp(a, b, t) + Vector3.down * (4f * sag * t * (1f - t));
                            det.Beam(A.Trim, prev, cur, w.t);
                            prev = cur;
                        }
                    }
                }
            }
        }

        // ================================================================== street lamps

        static void BuildStreetLamps(List<Band> bands)
        {
            var spots = new List<(Vector3 pos, float yaw, bool warm)>();
            // ring road 1 around the plaza (both sides) and ring road 2 (city side)
            void Ring(float r, bool faceOut, bool warm, float spacing)
            {
                float step = spacing / r * Mathf.Rad2Deg;
                for (float a = step * 0.5f; a < 360f; a += step)
                {
                    if (NearAxis(a, r, AvenueHalf + 2f)) continue;
                    spots.Add((Polar(a, r, PadY), FacingYaw(a, !faceOut), warm));
                }
            }
            Ring(RingRoads[0].r0 - 1f, true, false, 30f);
            Ring(RingRoads[0].r1 + 1f, false, false, 30f);
            Ring(RingRoads[1].r0 - 1f, true, false, 34f);
            Ring(RingRoads[2].r0 - 1f, true, true, 40f);

            // avenues, both sides, cool LED downtown and warm sodium further out
            for (int k = 0; k < 4; k++)
            {
                float ang = k * 90f;
                for (float r = RingRoads[0].r1 + 10f; r < CityEdge - 6f; r += 32f)
                {
                    if (OnRingRoad(r, 4f)) continue;
                    bool warm = r > RingRoads[1].r1;
                    foreach (int sgn in new[] { -1, 1 })
                    {
                        Vector3 p = Polar(ang, r, PadY) + Polar(ang + 90f * sgn, AvenueHalf + 1f);
                        // the lamp arm should reach over the avenue
                        float yaw = 90f - (ang + 90f * sgn);
                        spots.Add((p, yaw, warm));
                    }
                }
            }

            int n = 0;
            foreach (var s in spots)
            {
                Part p = Props.Get(s.pos, PropBatcher.Kind.Solid);
                Material bulb = s.warm ? A.LampWarm : A.LampBulb;
                p.Local(s.pos, s.yaw, () =>
                {
                    p.Cylinder(A.Metal, Vector3.zero, 0.11f, 7.6f, 6);
                    p.Box(A.Metal, new Vector3(0, 7.5f, -0.9f), new Vector3(0.1f, 0.1f, 1.9f));
                    p.Box(A.Metal, new Vector3(0, 7.45f, -1.8f), new Vector3(0.35f, 0.15f, 0.6f));
                    p.Box(bulb, new Vector3(0, 7.36f, -1.8f), new Vector3(0.26f, 0.04f, 0.5f));
                });
                Vector3 lp = s.pos + Quaternion.Euler(0, s.yaw, 0) * new Vector3(0, 7f, -1.8f);
                Color c = s.warm ? new Color(1f, 0.72f, 0.42f) : new Color(0.85f, 0.92f, 1f);
                Light l = MakeLight(_lightsRoot, "StreetLamp_" + n++, Vector3.zero, new LightSpec(Vector3.zero, c, 17f, 1.5f));
                l.transform.position = lp;
                StreetLights.Add(l);
            }
        }

        // ================================================================== trees

        static void BuildTrees(List<Band> bands)
        {
            // avenue trees between the lamps
            for (int k = 0; k < 4; k++)
            {
                float ang = k * 90f;
                for (float r = RingRoads[0].r1 + 26f; r < CityEdge - 6f; r += 32f)
                {
                    if (OnRingRoad(r, 5f)) continue;
                    foreach (int sgn in new[] { -1, 1 })
                    {
                        Vector3 p = Polar(ang, r, PadY) + Polar(ang + 90f * sgn, AvenueHalf + 2f);
                        Part part = Props.Get(p, PropBatcher.Kind.Solid);
                        F.Tree(part, p, 0.9f + (float)Rng.NextDouble() * 0.4f, Rng.Next(), Rng.NextDouble() < 0.5 ? A.Leaves : A.LeavesDark);
                    }
                }
            }
            // overgrowth past the city edge, nature taking back the outskirts
            for (int i = 0; i < 260; i++)
            {
                float a = (float)Rng.NextDouble() * 360f, r = CityEdge + 8f + (float)Rng.NextDouble() * 160f;
                Vector3 p = Polar(a, r, 0f);
                var fp = new Footprint(new Vector2(p.x, p.z), Vector2.one * 2.5f, 0);
                if (NearAxis(a, r, AvenueHalf + 3f) || Overlaps(fp)) continue;
                Part part = Props.Get(p, PropBatcher.Kind.Solid);
                F.Tree(part, p, 1f + (float)Rng.NextDouble() * 0.8f, Rng.Next(), Rng.NextDouble() < 0.5 ? A.Leaves : A.LeavesDark);
            }
            // the avenues continue as dirt tracks out of town
            var tracks = new Part();
            for (int k = 0; k < 4; k++)
            {
                float ang = k * 90f;
                Vector3 a = Polar(ang, CityEdge - 1f, 0.03f), b = Polar(ang, 1400f, 0.03f);
                tracks.Box(A.Dirt, (a + b) * 0.5f, new Vector3(AvenueHalf * 1.4f, 0.02f, (b - a).magnitude), new Vector2(5, 5), 90f - ang);
            }
            Spawn(tracks, "Outskirts_DirtTracks", Child(_root, "Ground"), Vector3.zero, 0, false, false, StaticEditorFlags.BatchingStatic);
        }

        // ================================================================== distant skyline (cheap, fog hides the rest)

        static void BuildSkyline()
        {
            var p = new Part();
            var rnd = new System.Random(Seed + 7);
            for (int i = 0; i < 160; i++)
            {
                float a = (float)rnd.NextDouble() * 360f;
                float r = 640f + (float)rnd.NextDouble() * 280f;
                float w = 25f + (float)rnd.NextDouble() * 45f, h = 25f + (float)rnd.NextDouble() * 150f;
                Material m = rnd.NextDouble() < 0.5 ? A.GlassDark : (rnd.NextDouble() < 0.5 ? A.OfficeDark : A.AptGrey);
                Vector2 tile = m == A.GlassDark ? CityAssets.GlassTile : m == A.OfficeDark ? CityAssets.OfficeTile : CityAssets.AptTile;
                p.Box(m, Polar(a, r, h * 0.5f), new Vector3(w, h, w * 0.8f), tile, FacingYaw(a, true));
            }
            Spawn(p, "DistantSkyline", Child(_root, "Skyline"), Vector3.zero, 0, false, false, StaticEditorFlags.BatchingStatic);
        }

        // ================================================================== prop meshes

        static void FlushProps()
        {
            Transform t = Child(_root, "Props");
            foreach (var kv in Props.All)
            {
                var (kind, band, sector) = kv.Key;
                bool solid = kind == PropBatcher.Kind.Solid;
                Spawn(kv.Value, $"Props_{kind}_b{band}_s{sector}", t, Vector3.zero, 0, solid, solid && band <= 3,
                      StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
            }
        }

        // ================================================================== lighting: blue-hour dusk

        static void SetupLighting(Scene scene)
        {
            Transform t = Child(_root, "Lighting");

            // turn off any other directional light so the dusk sun is the only one (they are kept, just disabled)
            foreach (GameObject go in scene.GetRootGameObjects())
            {
                if (go.transform == _root) continue;
                foreach (Light l in go.GetComponentsInChildren<Light>(true))
                    if (l.type == LightType.Directional && l.gameObject.activeSelf)
                    {
                        l.gameObject.SetActive(false);
                        Debug.Log($"[HORSE] Disabled existing directional light '{l.name}' in favour of the dusk sun.");
                    }
            }

            var sunGo = new GameObject("Sun_Dusk");
            sunGo.transform.SetParent(t, false);
            sunGo.transform.rotation = Quaternion.Euler(9f, 250f, 0f); // low sun in the west-southwest
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.6f, 0.38f);
            sun.intensity = 0.75f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.75f;
            sun.lightmapBakeType = LightmapBakeType.Realtime;

            // sky
            string skyPath = CityAssets.MatDir + "/Sky_Dusk.mat";
            Material sky = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
            if (sky == null) { sky = new Material(Shader.Find("Skybox/Procedural")); AssetDatabase.CreateAsset(sky, skyPath); }
            if (sky.HasProperty("_SunDisk")) sky.SetFloat("_SunDisk", 2f);
            if (sky.HasProperty("_SunSize")) sky.SetFloat("_SunSize", 0.035f);
            if (sky.HasProperty("_SunSizeConvergence")) sky.SetFloat("_SunSizeConvergence", 4f);
            if (sky.HasProperty("_AtmosphereThickness")) sky.SetFloat("_AtmosphereThickness", 1.35f);
            if (sky.HasProperty("_SkyTint")) sky.SetColor("_SkyTint", new Color(0.36f, 0.42f, 0.62f));
            if (sky.HasProperty("_GroundColor")) sky.SetColor("_GroundColor", new Color(0.22f, 0.22f, 0.26f));
            if (sky.HasProperty("_Exposure")) sky.SetFloat("_Exposure", 0.9f);
            EditorUtility.SetDirty(sky);

            RenderSettings.skybox = sky;
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.33f, 0.39f, 0.56f);
            RenderSettings.ambientEquatorColor = new Color(0.38f, 0.32f, 0.34f);
            RenderSettings.ambientGroundColor = new Color(0.12f, 0.12f, 0.14f);
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.reflectionIntensity = 0.85f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0021f;
            RenderSettings.fogColor = new Color(0.4f, 0.43f, 0.54f);

            // realtime only: nothing to bake, so iteration stays fast on low-end machines
            string lsPath = CityAssets.Root + "/CityMap_Lighting.lighting";
            LightingSettings ls = AssetDatabase.LoadAssetAtPath<LightingSettings>(lsPath);
            if (ls == null) { ls = new LightingSettings(); AssetDatabase.CreateAsset(ls, lsPath); }
            ls.bakedGI = false;
            ls.realtimeGI = false;
            Lightmapping.lightingSettings = ls;

            Camera cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                camGo.transform.SetParent(t, false);
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
                camGo.AddComponent<FlyCamera>();
                QuestPoint spawn = _questRoot.GetComponentsInChildren<QuestPoint>().FirstOrDefault(q => q.kind == QuestPointKind.PlayerSpawn);
                Vector3 sp = spawn ? spawn.transform.position : new Vector3(0, 0, -120f);
                Vector3 fwd = spawn ? spawn.transform.forward : Vector3.forward;
                camGo.transform.position = sp + Vector3.up * 2.4f - fwd * 6f;
                camGo.transform.rotation = Quaternion.LookRotation(fwd);
            }
            cam.farClipPlane = Mathf.Max(cam.farClipPlane, 1100f);
            cam.nearClipPlane = 0.2f;
            cam.useOcclusionCulling = true;

#if HORSE_URP
            var camData = cam.GetUniversalAdditionalCameraData();
            if (camData != null) camData.renderPostProcessing = true;

            string profPath = CityAssets.Root + "/CityMap_PostFX.asset";
            AssetDatabase.DeleteAsset(profPath);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, profPath);
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.95f);
            bloom.intensity.Override(0.7f);
            bloom.scatter.Override(0.65f);
            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);
            var color = profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(0.35f);
            color.contrast.Override(8f);
            color.saturation.Override(-4f);
            color.colorFilter.Override(new Color(0.96f, 0.97f, 1f));
            var vig = profile.Add<Vignette>(true);
            vig.intensity.Override(0.22f);
            foreach (VolumeComponent c in profile.components) AssetDatabase.AddObjectToAsset(c, profile);
            EditorUtility.SetDirty(profile);

            var volGo = new GameObject("PostFX_Dusk");
            volGo.transform.SetParent(t, false);
            var vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.sharedProfile = profile;
#endif
        }
    }
}

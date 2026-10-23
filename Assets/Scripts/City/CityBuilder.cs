using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace ScammerNoScamming
{
    public class CityResult
    {
        public GameObject Root;
        public GameObject Station;
        public GameObject Hideout;
        public List<GameObject> Towers = new List<GameObject>();
        public List<GameObject> MidRise = new List<GameObject>();
        public List<GameObject> Houses = new List<GameObject>();
        public List<GameObject> Cars = new List<GameObject>();
    }

    // Ring city after Codex's concept art: police station on a plaza in the middle, towers around
    // it, an elevated ring road, mid-rise shops, then houses, all inside a perimeter wall.
    // Cardinal roads (south one is the main avenue) run from the plaza; diagonals start past the towers.
    public static class CityBuilder
    {
        public const float PlazaRadius = 30f;
        public const float InnerRingRoad = 90f;
        public const float ViaductRadius = 146f;
        public const float WallRadius = 218f;
        public const float GroundSize = 460f;
        public const float StationDepth = 16f;
        public const float StationWidth = 26f;

        // Metres covered by one repeat of each facade texture.
        static readonly Vector2 TowerATile = new Vector2(80f, 80f);
        static readonly Vector2 TowerBTile = new Vector2(60f, 60f);
        static readonly Vector2 MidTile = new Vector2(20f, 20f);
        static readonly Vector2 RoofTile = new Vector2(6f, 6f);

        static readonly Color Rust = Hex("A84932");
        static readonly Color Orange = Hex("D97843");
        static readonly Color Gold = Hex("E5B96E");
        static readonly Color Umber = Hex("69524A");
        static readonly Color Charcoal = Hex("363B3C");
        static readonly Color Teal = Hex("1E6C70");
        static readonly Color PaleTeal = Hex("3E9991");
        static readonly Color Stone = Hex("C8B894");
        static readonly Color Bone = Hex("E7D9B8");
        static readonly Color Black = Hex("11191D");
        static readonly Color RoadColor = Hex("4A403A");

        struct Footprint
        {
            public Vector2 Center;
            public float Radius;
        }

        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var color);
            return color;
        }

        static float Range(Random rng, float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }

        static float RoadHalfWidth(int index)
        {
            if (index == 4) return 10f;          // south: main avenue
            return index % 2 == 0 ? 6f : 5f;     // other cardinals, diagonals
        }

        static Vector2 RoadDirection(int index)
        {
            float theta = index * 45f * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(theta), Mathf.Cos(theta));
        }

        public static CityResult Build(CityMaterials mats, int seed = 2035)
        {
            var rng = new Random(seed);
            var result = new CityResult();
            var root = new GameObject("City");
            result.Root = root;

            var occupied = new List<Footprint>();

            BuildGround(root.transform, mats);
            BuildRoads(Group(root, "Roads"), mats);
            BuildViaduct(Group(root, "Viaduct"), mats);
            BuildWall(Group(root, "PerimeterWall"), mats, rng);

            var landmarks = Group(root, "Landmarks");
            result.Station = BuildStation(landmarks, mats);
            occupied.Add(new Footprint { Center = Vector2.zero, Radius = PlazaRadius });
            BuildPlazaProps(landmarks, mats);

            result.Hideout = PlaceHideout(landmarks, mats, occupied);

            PlaceTowers(Group(root, "Towers"), mats, rng, occupied, result);
            PlaceMidRise(Group(root, "MidRise"), mats, rng, occupied, result);
            PlaceHouses(Group(root, "Houses"), mats, rng, occupied, result);

            var props = Group(root, "Props");
            PlaceAvenueProps(props, mats, rng);
            PlaceCars(props, mats, rng, result);
            PlaceCrates(props, mats, rng);
            PlacePalms(props, mats, rng, occupied);

            return result;
        }

        static Transform Group(GameObject root, string name)
        {
            var group = new GameObject(name);
            group.transform.SetParent(root.transform, false);
            return group.transform;
        }

        // ---------------------------------------------------------------- primitives

        static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 size, Material mat,
            bool collider = false, Quaternion? rotation = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rotation ?? Quaternion.identity;
            go.transform.localScale = size;
            go.AddComponent<MeshFilter>().sharedMesh = CityMeshes.UnitCube;
            var renderer = go.AddComponent<MeshRenderer>();
            if (mat != null) renderer.sharedMaterial = mat;
            if (collider) go.AddComponent<BoxCollider>();
            return go;
        }

        static GameObject TexBox(Transform parent, string name, Vector3 pos, Vector3 size, Material mat,
            Vector2 tile, bool collider)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.AddComponent<MeshFilter>().sharedMesh = CityMeshes.Box(size, tile);
            var renderer = go.AddComponent<MeshRenderer>();
            if (mat != null) renderer.sharedMaterial = mat;
            if (collider) go.AddComponent<BoxCollider>().size = size;
            return go;
        }

        // diameter/height are world sizes; the primitive cylinder is 1 wide and 2 tall.
        static GameObject Cylinder(Transform parent, string name, Vector3 pos, float diameter, float height,
            Material mat, bool collider = false, Quaternion? rotation = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rotation ?? Quaternion.identity;
            go.transform.localScale = new Vector3(diameter, height * 0.5f, diameter);
            go.AddComponent<MeshFilter>().sharedMesh = CityMeshes.Cylinder;
            var renderer = go.AddComponent<MeshRenderer>();
            if (mat != null) renderer.sharedMaterial = mat;
            // A capsule needs height >= diameter; flat discs (the plaza) use their mesh instead.
            if (collider)
            {
                if (height >= diameter) go.AddComponent<CapsuleCollider>();
                else go.AddComponent<MeshCollider>().sharedMesh = CityMeshes.Cylinder;
            }
            return go;
        }

        static GameObject Empty(Transform parent, string name, Vector3 pos, float yaw)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return go;
        }

        // ---------------------------------------------------------------- ground, roads, ring structures

        static void BuildGround(Transform parent, CityMaterials mats)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(parent, false);
            ground.transform.localScale = new Vector3(GroundSize / 10f, 1f, GroundSize / 10f);
            if (mats.Ground != null) ground.GetComponent<MeshRenderer>().sharedMaterial = mats.Ground;
        }

        static void BuildRoads(Transform parent, CityMaterials mats)
        {
            Material road = mats.Paint(RoadColor);

            for (int k = 0; k < 8; k++)
            {
                float start = k % 2 == 1 ? InnerRingRoad : PlazaRadius;
                float end = WallRadius - 6f;
                float length = end - start;
                Vector2 dir = RoadDirection(k);
                Vector2 mid = dir * (start + length * 0.5f);
                Box(parent, "Road_" + k, new Vector3(mid.x, 0.03f, mid.y),
                    new Vector3(RoadHalfWidth(k) * 2f, 0.06f, length), road, false,
                    Quaternion.Euler(0f, k * 45f, 0f));
            }

            BuildRingRoad(parent, InnerRingRoad, 8f, 64, road);
            BuildRingRoad(parent, ViaductRadius, 10f, 96, road);
        }

        static void BuildRingRoad(Transform parent, float radius, float width, int segments, Material mat)
        {
            float chord = 2f * Mathf.PI * radius / segments * 1.06f;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * 360f / segments;
                float rad = angle * Mathf.Deg2Rad;
                Box(parent, "Ring_" + i, new Vector3(radius * Mathf.Sin(rad), 0.03f, radius * Mathf.Cos(rad)),
                    new Vector3(width, 0.06f, chord), mat, false, Quaternion.Euler(0f, angle + 90f, 0f));
            }
        }

        static void BuildViaduct(Transform parent, CityMaterials mats)
        {
            Material deck = mats.Paint(Charcoal);
            Material pillar = mats.Paint(Stone);
            int decks = 48;
            float chord = 2f * Mathf.PI * ViaductRadius / decks * 1.04f;
            for (int i = 0; i < decks; i++)
            {
                float angle = i * 360f / decks;
                float rad = angle * Mathf.Deg2Rad;
                Box(parent, "Deck_" + i, new Vector3(ViaductRadius * Mathf.Sin(rad), 6.6f, ViaductRadius * Mathf.Cos(rad)),
                    new Vector3(9f, 1.2f, chord), deck, false, Quaternion.Euler(0f, angle + 90f, 0f));
            }

            // Pillars sit halfway between road angles so every road passes beneath the viaduct.
            for (int i = 0; i < 24; i++)
            {
                float angle = 7.5f + i * 15f;
                float rad = angle * Mathf.Deg2Rad;
                Box(parent, "Pillar_" + i, new Vector3(ViaductRadius * Mathf.Sin(rad), 3f, ViaductRadius * Mathf.Cos(rad)),
                    new Vector3(2.2f, 6f, 2.2f), pillar, true, Quaternion.Euler(0f, angle, 0f));
            }
        }

        static void BuildWall(Transform parent, CityMaterials mats, Random rng)
        {
            Material wall = mats.Paint(Umber);
            int segments = 64;
            float chord = 2f * Mathf.PI * WallRadius / segments * 1.05f;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * 360f / segments;
                float rad = angle * Mathf.Deg2Rad;
                float height = Range(rng, 3f, 4.5f);
                Box(parent, "Wall_" + i, new Vector3(WallRadius * Mathf.Sin(rad), height * 0.5f, WallRadius * Mathf.Cos(rad)),
                    new Vector3(1.4f, height, chord), wall, true, Quaternion.Euler(0f, angle + 90f, 0f));
            }
        }

        // ---------------------------------------------------------------- placement

        static bool Blocked(Vector2 p, float radius)
        {
            float dist = p.magnitude;
            if (dist < PlazaRadius + radius + 2f) return true;
            if (Mathf.Abs(dist - InnerRingRoad) < 4f + radius + 1f) return true;
            if (Mathf.Abs(dist - ViaductRadius) < 5f + radius + 1f) return true;
            if (dist + radius > WallRadius - 4f) return true;

            for (int k = 0; k < 8; k++)
            {
                Vector2 dir = RoadDirection(k);
                float start = k % 2 == 1 ? InnerRingRoad : PlazaRadius;
                if (Vector2.Dot(p, dir) < start - radius) continue;
                float perpendicular = Mathf.Abs(p.x * dir.y - p.y * dir.x);
                if (perpendicular < RoadHalfWidth(k) + radius + 1f) return true;
            }
            return false;
        }

        static bool Overlaps(List<Footprint> occupied, Vector2 p, float radius, float gap)
        {
            foreach (var other in occupied)
                if ((other.Center - p).magnitude < other.Radius + radius + gap) return true;
            return false;
        }

        // Tries random spots in an annulus until `target` buildings fit or attempts run out.
        static void Scatter(Random rng, List<Footprint> occupied, int target, int attempts,
            float minRadius, float maxRadius, Vector2 sizeRange, float gap, Action<Vector2, float, float, float> place)
        {
            int placed = 0;
            for (int i = 0; i < attempts && placed < target; i++)
            {
                float angle = Range(rng, 0f, 360f) * Mathf.Deg2Rad;
                float r = Range(rng, minRadius, maxRadius);
                float w = Range(rng, sizeRange.x, sizeRange.y);
                float d = Range(rng, sizeRange.x, sizeRange.y);
                float radius = Mathf.Sqrt(w * w + d * d) * 0.5f;
                var p = new Vector2(r * Mathf.Sin(angle), r * Mathf.Cos(angle));

                if (Blocked(p, radius) || Overlaps(occupied, p, radius, gap)) continue;
                occupied.Add(new Footprint { Center = p, Radius = radius });
                place(p, w, d, radius);
                placed++;
            }
        }

        // Faces the building's front (-Z) toward the city centre.
        static float YawFacingCenter(Vector2 p)
        {
            return Mathf.Atan2(p.x, p.y) * Mathf.Rad2Deg;
        }

        static void PlaceTowers(Transform parent, CityMaterials mats, Random rng, List<Footprint> occupied, CityResult result)
        {
            Scatter(rng, occupied, 18, 6000, 46f, 80f, new Vector2(12f, 18f), 3f, (p, w, d, radius) =>
            {
                int floors = rng.Next(14, 32);
                var tower = BuildTower(parent, mats, rng, new Vector3(p.x, 0f, p.y), YawFacingCenter(p), floors, w, d);
                result.Towers.Add(tower);
            });
        }

        static void PlaceMidRise(Transform parent, CityMaterials mats, Random rng, List<Footprint> occupied, CityResult result)
        {
            Scatter(rng, occupied, 70, 2500, 102f, 138f, new Vector2(10f, 17f), 3f, (p, w, d, radius) =>
            {
                int floors = rng.Next(3, 6);
                var building = BuildMidRise(parent, mats, rng, new Vector3(p.x, 0f, p.y), YawFacingCenter(p), floors, w, d);
                result.MidRise.Add(building);
            });
        }

        static void PlaceHouses(Transform parent, CityMaterials mats, Random rng, List<Footprint> occupied, CityResult result)
        {
            Color[] walls = { Stone, Bone, Gold, PaleTeal, Orange };
            Scatter(rng, occupied, 160, 4000, 158f, 205f, new Vector2(6f, 9f), 2.5f, (p, w, d, radius) =>
            {
                var house = BuildHouse(parent, mats, rng, new Vector3(p.x, 0f, p.y), YawFacingCenter(p), w, d,
                    walls[rng.Next(walls.Length)]);
                result.Houses.Add(house);
            });
        }

        static GameObject PlaceHideout(Transform parent, CityMaterials mats, List<Footprint> occupied)
        {
            const float w = 16f, h = 11f, d = 14f;
            float radius = Mathf.Sqrt(w * w + d * d) * 0.5f;
            for (float angle = 125f; angle < 485f; angle += 5f)
            {
                float rad = angle * Mathf.Deg2Rad;
                var p = new Vector2(116f * Mathf.Sin(rad), 116f * Mathf.Cos(rad));
                if (Blocked(p, radius) || Overlaps(occupied, p, radius, 3f)) continue;

                occupied.Add(new Footprint { Center = p, Radius = radius });
                var hideout = BuildHollow(parent, "ScamHideout", new Vector3(p.x, 0f, p.y), YawFacingCenter(p),
                    w, h, d, mats.Mid, MidTile, mats);
                Furnish(hideout.transform, mats, Black, PaleTeal, d);
                return hideout;
            }
            return null;
        }

        // ---------------------------------------------------------------- buildings

        static GameObject BuildTower(Transform parent, CityMaterials mats, Random rng, Vector3 pos, float yaw,
            int floors, float w, float d)
        {
            var root = Empty(parent, "Tower", pos, yaw);
            bool useA = rng.Next(2) == 0;
            Material facade = useA ? mats.TowerA : mats.TowerB;
            Vector2 tile = useA ? TowerATile : TowerBTile;

            float height = floors * 3.5f;
            float lowerHeight = height * 0.72f;
            float upperHeight = height - lowerHeight;
            TexBox(root.transform, "Body", new Vector3(0f, lowerHeight * 0.5f, 0f), new Vector3(w, lowerHeight, d), facade, tile, true);
            TexBox(root.transform, "Setback", new Vector3(0f, lowerHeight + upperHeight * 0.5f, 0f),
                new Vector3(w * 0.78f, upperHeight, d * 0.78f), facade, tile, false);

            Box(root.transform, "Crown", new Vector3(0f, height + 0.4f, 0f), new Vector3(w * 0.82f, 0.8f, d * 0.82f), mats.Paint(Umber));
            Cylinder(root.transform, "WaterTank", new Vector3(w * 0.2f, height + 2.2f, d * 0.15f), 3.2f, 3f, mats.Paint(Rust));
            Box(root.transform, "Antenna", new Vector3(-w * 0.2f, height + 0.8f + Range(rng, 4f, 7f), -d * 0.1f),
                new Vector3(0.3f, Range(rng, 8f, 14f), 0.3f), mats.Paint(Black));
            return root;
        }

        static GameObject BuildMidRise(Transform parent, CityMaterials mats, Random rng, Vector3 pos, float yaw,
            int floors, float w, float d)
        {
            var root = Empty(parent, "MidRise", pos, yaw);
            float height = floors * 3.4f;
            TexBox(root.transform, "Body", new Vector3(0f, height * 0.5f, 0f), new Vector3(w, height, d), mats.Mid, MidTile, true);
            Box(root.transform, "RoofSlab", new Vector3(0f, height + 0.3f, 0f), new Vector3(w + 0.6f, 0.6f, d + 0.6f), mats.Paint(Stone));

            Color[] awnings = { Rust, Orange, Teal, Gold };
            Box(root.transform, "Awning", new Vector3(0f, 3.4f, -d * 0.5f - 1f), new Vector3(w * 0.8f, 0.1f, 2.4f),
                mats.Paint(awnings[rng.Next(awnings.Length)]), false, Quaternion.Euler(12f, 0f, 0f));
            return root;
        }

        static GameObject BuildHouse(Transform parent, CityMaterials mats, Random rng, Vector3 pos, float yaw,
            float w, float d, Color wallColor)
        {
            var root = Empty(parent, "House", pos, yaw);
            const float wallHeight = 3.2f;
            Box(root.transform, "Walls", new Vector3(0f, wallHeight * 0.5f, 0f), new Vector3(w, wallHeight, d), mats.Paint(wallColor), true);
            TexBox(root.transform, "RoofLow", new Vector3(0f, wallHeight + 0.45f, 0f), new Vector3(w + 1.2f, 0.9f, d + 1.2f), mats.Roof, RoofTile, false);
            TexBox(root.transform, "RoofHigh", new Vector3(0f, wallHeight + 1.35f, 0f), new Vector3(w * 0.6f, 0.9f, d * 0.6f), mats.Roof, RoofTile, false);
            Box(root.transform, "Door", new Vector3(0f, 1.1f, -d * 0.5f - 0.03f), new Vector3(1.2f, 2.2f, 0.1f), mats.Paint(Umber));
            return root;
        }

        // A room you can walk into: one door in the front wall (-Z), roof slab on top.
        static GameObject BuildHollow(Transform parent, string name, Vector3 pos, float yaw, float w, float h, float d,
            Material wallMat, Vector2 wallTile, CityMaterials mats)
        {
            var root = Empty(parent, name, pos, yaw);
            const float t = 0.6f;
            const float doorWidth = 4.5f;
            const float doorHeight = 3.8f;
            float sideWidth = (w - doorWidth) * 0.5f;
            Transform r = root.transform;

            Box(r, "Floor", new Vector3(0f, 0.1f, 0f), new Vector3(w, 0.2f, d), mats.Paint(Stone), true);
            TexBox(r, "WallBack", new Vector3(0f, h * 0.5f, d * 0.5f - t * 0.5f), new Vector3(w, h, t), wallMat, wallTile, true);
            TexBox(r, "WallLeft", new Vector3(-(w * 0.5f - t * 0.5f), h * 0.5f, 0f), new Vector3(t, h, d - 2f * t), wallMat, wallTile, true);
            TexBox(r, "WallRight", new Vector3(w * 0.5f - t * 0.5f, h * 0.5f, 0f), new Vector3(t, h, d - 2f * t), wallMat, wallTile, true);
            TexBox(r, "WallFrontLeft", new Vector3(-(doorWidth * 0.5f + sideWidth * 0.5f), h * 0.5f, -(d * 0.5f - t * 0.5f)),
                new Vector3(sideWidth, h, t), wallMat, wallTile, true);
            TexBox(r, "WallFrontRight", new Vector3(doorWidth * 0.5f + sideWidth * 0.5f, h * 0.5f, -(d * 0.5f - t * 0.5f)),
                new Vector3(sideWidth, h, t), wallMat, wallTile, true);
            TexBox(r, "Lintel", new Vector3(0f, doorHeight + (h - doorHeight) * 0.5f, -(d * 0.5f - t * 0.5f)),
                new Vector3(doorWidth, h - doorHeight, t), wallMat, wallTile, true);
            Box(r, "Ceiling", new Vector3(0f, h + 0.2f, 0f), new Vector3(w + 0.4f, 0.4f, d + 0.4f), mats.Paint(Umber), true);
            return root;
        }

        static void Furnish(Transform room, CityMaterials mats, Color body, Color accent, float depth)
        {
            var interior = Empty(room, "Interior", Vector3.zero, 0f).transform;
            float z = depth * 0.5f - 2.5f;
            for (int row = 0; row < 3; row++)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    float x = side * 4.5f;
                    Box(interior, "Rack", new Vector3(x, 1.2f, z - row * 3f), new Vector3(1.2f, 2.2f, 1f), mats.Paint(body), true);
                    Box(interior, "RackLight", new Vector3(x, 1.8f, z - row * 3f - 0.52f), new Vector3(0.9f, 0.12f, 0.05f), mats.Paint(accent));
                }
            }

            var lightObject = new GameObject("RoomLight");
            lightObject.transform.SetParent(interior, false);
            lightObject.transform.localPosition = new Vector3(0f, 6f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 16f;
            light.intensity = 1.2f;
            light.color = accent;
        }

        static GameObject BuildStation(Transform parent, CityMaterials mats)
        {
            Cylinder(parent, "Plaza", new Vector3(0f, 0.1f, 0f), PlazaRadius * 2f, 0.2f, mats.Paint(Stone), true);

            const float h = 9f;
            var station = BuildHollow(parent, "PoliceStation", new Vector3(0f, 0.2f, 0f), 0f,
                StationWidth, h, StationDepth, null, Vector2.one, mats);
            // The generic hollow uses default walls; repaint them in bone stone.
            foreach (var renderer in station.GetComponentsInChildren<MeshRenderer>())
                if (renderer.name.StartsWith("Wall") || renderer.name == "Lintel")
                    renderer.sharedMaterial = mats.Paint(Bone);

            Transform r = station.transform;
            float top = h + 0.4f;
            TexBox(r, "RoofTier1", new Vector3(0f, top + 0.5f, 0f), new Vector3(StationWidth + 3f, 1f, StationDepth + 3f), mats.Roof, RoofTile, false);
            TexBox(r, "RoofTier2", new Vector3(0f, top + 2.1f, 0f), new Vector3(StationWidth * 0.7f, 2.2f, StationDepth * 0.7f), mats.Roof, RoofTile, false);
            TexBox(r, "RoofTier3", new Vector3(0f, top + 4.3f, 0f), new Vector3(StationWidth * 0.4f, 2.2f, StationDepth * 0.4f), mats.Roof, RoofTile, false);
            Box(r, "Spire", new Vector3(0f, top + 8.2f, 0f), new Vector3(0.5f, 6f, 0.5f), mats.Paint(Gold));

            // Porch columns either side of the door.
            for (int side = -1; side <= 1; side += 2)
                Cylinder(r, "Column", new Vector3(side * 5.2f, 3.5f, -StationDepth * 0.5f - 1.6f), 1.1f, 7f, mats.Paint(Bone), true);
            Box(r, "PorchRoof", new Vector3(0f, 7.2f, -StationDepth * 0.5f - 1.6f), new Vector3(13f, 0.5f, 4f), mats.Paint(Umber));

            // Interior: desk, notice board, one warm light.
            var inside = Empty(r, "Interior", Vector3.zero, 0f).transform;
            Box(inside, "Desk", new Vector3(0f, 0.65f, 2.5f), new Vector3(4f, 0.9f, 1.6f), mats.Paint(Umber), true);
            Box(inside, "NoticeBoard", new Vector3(0f, 3.2f, StationDepth * 0.5f - 0.75f), new Vector3(8f, 3f, 0.12f), mats.Paint(Gold));
            Box(inside, "Bench", new Vector3(-8f, 0.5f, 0f), new Vector3(0.8f, 0.6f, 6f), mats.Paint(Umber), true);
            Box(inside, "Bench", new Vector3(8f, 0.5f, 0f), new Vector3(0.8f, 0.6f, 6f), mats.Paint(Umber), true);
            var lightObject = new GameObject("RoomLight");
            lightObject.transform.SetParent(inside, false);
            lightObject.transform.localPosition = new Vector3(0f, 7f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 22f;
            light.intensity = 1.4f;
            light.color = new Color(1f, 0.8f, 0.55f);
            return station;
        }

        // ---------------------------------------------------------------- props

        static void BuildPlazaProps(Transform parent, CityMaterials mats)
        {
            for (int i = 0; i < 6; i++)
            {
                float angle = 30f + i * 60f;
                float rad = angle * Mathf.Deg2Rad;
                Vector3 p = new Vector3((PlazaRadius - 4f) * Mathf.Sin(rad), 0.2f, (PlazaRadius - 4f) * Mathf.Cos(rad));
                BuildHitchingPost(parent, mats, p, angle);
                BuildTrough(parent, mats, p + new Vector3(Mathf.Cos(rad), 0f, -Mathf.Sin(rad)) * 4f, angle);
            }
        }

        static void BuildHitchingPost(Transform parent, CityMaterials mats, Vector3 pos, float yaw)
        {
            var post = Empty(parent, "HitchingPost", pos, yaw);
            Box(post.transform, "Post", new Vector3(0f, 0.9f, 0f), new Vector3(0.3f, 1.8f, 0.3f), mats.Paint(Umber), true);
            Box(post.transform, "Beam", new Vector3(0.6f, 1.5f, 0f), new Vector3(1.2f, 0.18f, 0.18f), mats.Paint(Umber));
            Cylinder(post.transform, "Ring", new Vector3(1.0f, 1.25f, 0f), 0.3f, 0.06f, mats.Paint(Black), false, Quaternion.Euler(0f, 0f, 90f));
        }

        static void BuildTrough(Transform parent, CityMaterials mats, Vector3 pos, float yaw)
        {
            var trough = Empty(parent, "Trough", pos, yaw);
            Box(trough.transform, "Basin", new Vector3(0f, 0.4f, 0f), new Vector3(0.9f, 0.8f, 2.6f), mats.Paint(Stone), true);
            Box(trough.transform, "Water", new Vector3(0f, 0.78f, 0f), new Vector3(0.65f, 0.06f, 2.3f), mats.Paint(Teal));
        }

        static void PlaceAvenueProps(Transform parent, CityMaterials mats, Random rng)
        {
            // South avenue runs along -Z, so lateral offset is world X.
            for (float z = 38f; z < 205f; z += 16f)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    if (Mathf.Abs(Mathf.Abs(z) - ViaductRadius) < 8f || Mathf.Abs(Mathf.Abs(z) - InnerRingRoad) < 6f) continue;
                    BuildLamp(parent, mats, new Vector3(side * 11.5f, 0f, -z));
                }
            }

            Color[] awnings = { Rust, Orange, Teal, Gold };
            int stall = 0;
            for (float z = 38f; z < 82f; z += 11f)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    BuildStall(parent, mats, new Vector3(side * 14.5f, 0f, -z), side > 0 ? 90f : -90f, awnings[stall++ % awnings.Length]);
                    if (stall % 2 == 0)
                    {
                        BuildHitchingPost(parent, mats, new Vector3(side * 11f, 0f, -z - 4f), 90f);
                        BuildTrough(parent, mats, new Vector3(side * 11.4f, 0f, -z + 3f), 0f);
                    }
                }
            }
        }

        static void BuildLamp(Transform parent, CityMaterials mats, Vector3 pos)
        {
            var lamp = Empty(parent, "StreetLamp", pos, 0f);
            Cylinder(lamp.transform, "Pole", new Vector3(0f, 2.6f, 0f), 0.25f, 5.2f, mats.Paint(Black), true);
            Box(lamp.transform, "Head", new Vector3(0f, 5.4f, 0f), new Vector3(0.6f, 0.8f, 0.6f), mats.Paint(Black));
            Box(lamp.transform, "Glass", new Vector3(0f, 5.4f, 0f), new Vector3(0.45f, 0.6f, 0.65f), mats.Paint(Bone));
        }

        // Open-fronted stall: four posts, a sloped awning and a counter. yaw turns the front toward the road.
        static void BuildStall(Transform parent, CityMaterials mats, Vector3 pos, float yaw, Color awning)
        {
            var stall = Empty(parent, "Stall", pos, yaw);
            Transform s = stall.transform;
            foreach (var corner in new[] { new Vector2(-1.7f, -1.4f), new Vector2(1.7f, -1.4f), new Vector2(-1.7f, 1.4f), new Vector2(1.7f, 1.4f) })
                Box(s, "Post", new Vector3(corner.x, 1.4f, corner.y), new Vector3(0.15f, 2.8f, 0.15f), mats.Paint(Umber));
            Box(s, "Awning", new Vector3(0f, 2.95f, -0.2f), new Vector3(4.2f, 0.08f, 3.6f), mats.Paint(awning), false, Quaternion.Euler(-10f, 0f, 0f));
            Box(s, "Counter", new Vector3(0f, 0.55f, -1.2f), new Vector3(3.2f, 1.1f, 0.8f), mats.Paint(Umber), true);
            Box(s, "Goods", new Vector3(-0.8f, 1.25f, -1.2f), new Vector3(0.8f, 0.3f, 0.5f), mats.Paint(Gold));
        }

        static void PlaceCars(Transform parent, CityMaterials mats, Random rng, CityResult result)
        {
            Color[] bodies = { Rust, Umber, Teal, Charcoal };
            for (int i = 0; i < 36; i++)
            {
                int k = rng.Next(8);
                float start = k % 2 == 1 ? InnerRingRoad + 6f : PlazaRadius + 10f;
                float along = Range(rng, start, WallRadius - 14f);
                if (Mathf.Abs(along - ViaductRadius) < 12f || Mathf.Abs(along - InnerRingRoad) < 10f) continue;

                float lateral = (rng.Next(2) == 0 ? -1f : 1f) * (RoadHalfWidth(k) - 2.2f);
                Vector2 dir = RoadDirection(k);
                Vector2 side = new Vector2(dir.y, -dir.x);
                Vector2 p = dir * along + side * lateral;
                float yaw = k * 45f + (rng.Next(2) == 0 ? 0f : 180f) + Range(rng, -15f, 15f);
                result.Cars.Add(BuildCar(parent, mats, new Vector3(p.x, 0f, p.y), yaw, bodies[rng.Next(bodies.Length)]));
            }
        }

        static GameObject BuildCar(Transform parent, CityMaterials mats, Vector3 pos, float yaw, Color bodyColor)
        {
            var car = Empty(parent, "AbandonedCar", pos, yaw);
            Transform c = car.transform;
            Material paint = mats.Paint(bodyColor);
            Box(c, "Body", new Vector3(0f, 0.65f, 0f), new Vector3(1.8f, 0.8f, 4.2f), paint);
            Box(c, "Cabin", new Vector3(0f, 1.35f, -0.2f), new Vector3(1.6f, 0.7f, 2.2f), paint);
            Box(c, "Windows", new Vector3(0f, 1.4f, -0.2f), new Vector3(1.64f, 0.4f, 2.0f), mats.Paint(Black));
            Material wheel = mats.Paint(Rust);
            foreach (var w in new[] { new Vector2(-0.9f, 1.3f), new Vector2(0.9f, 1.3f), new Vector2(-0.9f, -1.3f), new Vector2(0.9f, -1.3f) })
                Cylinder(c, "Rim", new Vector3(w.x, 0.3f, w.y), 0.6f, 0.15f, wheel, false, Quaternion.Euler(0f, 0f, 90f));
            var collider = car.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.9f, 0f);
            collider.size = new Vector3(1.9f, 1.8f, 4.3f);
            return car;
        }

        static void PlaceCrates(Transform parent, CityMaterials mats, Random rng)
        {
            Material wood = mats.Paint(Umber);
            for (int i = 0; i < 24; i++)
            {
                int k = rng.Next(4) * 2;
                float along = Range(rng, PlazaRadius + 8f, WallRadius - 14f);
                if (Mathf.Abs(along - ViaductRadius) < 10f || Mathf.Abs(along - InnerRingRoad) < 8f) continue;

                Vector2 dir = RoadDirection(k);
                Vector2 side = new Vector2(dir.y, -dir.x);
                float lateral = (rng.Next(2) == 0 ? -1f : 1f) * (RoadHalfWidth(k) + Range(rng, 1.5f, 3.5f));
                Vector2 p = dir * along + side * lateral;
                var stack = Empty(parent, "Crates", new Vector3(p.x, 0f, p.y), Range(rng, 0f, 90f));
                Box(stack.transform, "Crate", new Vector3(0f, 0.5f, 0f), new Vector3(1f, 1f, 1f), wood, true);
                if (rng.Next(2) == 0)
                    Box(stack.transform, "Crate", new Vector3(1.1f, 0.4f, 0.2f), new Vector3(0.8f, 0.8f, 0.8f), wood, true);
                if (rng.Next(2) == 0)
                    Box(stack.transform, "Crate", new Vector3(0.1f, 1.4f, 0f), new Vector3(0.8f, 0.8f, 0.8f), wood, true);
            }
        }

        static void PlacePalms(Transform parent, CityMaterials mats, Random rng, List<Footprint> occupied)
        {
            Material trunk = mats.Paint(Umber);
            Material leaf = mats.Paint(Teal);
            int placed = 0;
            for (int i = 0; i < 600 && placed < 60; i++)
            {
                float angle = Range(rng, 0f, 360f) * Mathf.Deg2Rad;
                float r = Range(rng, 100f, 210f);
                var p = new Vector2(r * Mathf.Sin(angle), r * Mathf.Cos(angle));
                if (Blocked(p, 1.5f) || Overlaps(occupied, p, 1.5f, 1f)) continue;
                occupied.Add(new Footprint { Center = p, Radius = 1.5f });
                placed++;

                var palm = Empty(parent, "Palm", new Vector3(p.x, 0f, p.y), Range(rng, 0f, 360f));
                Cylinder(palm.transform, "Trunk", new Vector3(0f, 3f, 0f), 0.45f, 6f, trunk, true,
                    Quaternion.Euler(Range(rng, -5f, 5f), 0f, 0f));
                for (int f = 0; f < 5; f++)
                {
                    Quaternion rot = Quaternion.Euler(0f, f * 72f, 0f) * Quaternion.Euler(20f, 0f, 0f);
                    Box(palm.transform, "Frond", new Vector3(0f, 6.1f, 0f) + rot * new Vector3(0f, 0f, 1.7f),
                        new Vector3(0.9f, 0.08f, 3.6f), leaf, false, rot);
                }
            }
        }
    }
}

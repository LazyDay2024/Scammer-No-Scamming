using System.IO;
using UnityEditor;
using UnityEngine;
using ScammerNoScamming;

// Editor-built dressing for the police station hall (26 x 16 m shell from CityBuilder). It is saved in the scene,
// so the team can move any piece in the Unity Editor. StationInvestigation adds the interactive boards at runtime:
// back-wall boards around z 5.85, victim (-5, -1.5), map table (5, 0), case file table (-4, -4).
public static class StationDressing
{
    const string TextureFolder = "Assets/Art/Textures/Procedural";

    public static void Build(Transform station)
    {
        var old = station.Find("StationDressing");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var root = WorldSceneArt.Empty(station, "StationDressing", Vector3.zero);
        var floor = Planks("StationFloor", new Color(.42f, .27f, .16f), new Vector2(7f, 4.5f));
        var panel = Planks("StationWainscot", new Color(.3f, .18f, .1f), new Vector2(10f, 1f));
        var ceilingWood = Planks("StationCeiling", new Color(.25f, .16f, .1f), new Vector2(8f, 5f));
        var beamWood = WorldSceneArt.Paint("StationBeam", new Color(.2f, .12f, .07f));
        var plaster = WorldSceneArt.Paint("StationPlaster", new Color(.74f, .63f, .47f));

        // Room shell: wooden floor, warm plaster, dark wainscot, a lower beamed ceiling instead of a 9 m void.
        WorldSceneArt.Box(root, "WoodFloor", new Vector3(0f, .22f, 0f), new Vector3(24.8f, .04f, 14.8f), floor, false);
        foreach (var wall in new[] { ("Back", new Vector3(0f, 2.6f, 7.38f), new Vector3(24.8f, 4.8f, .04f)),
                                     ("Left", new Vector3(-12.38f, 2.6f, 0f), new Vector3(.04f, 4.8f, 14.8f)),
                                     ("Right", new Vector3(12.38f, 2.6f, 0f), new Vector3(.04f, 4.8f, 14.8f)) })
            WorldSceneArt.Box(root, "Plaster" + wall.Item1, wall.Item2, wall.Item3, plaster, false);
        WorldSceneArt.Box(root, "WainscotBack", new Vector3(0f, .85f, 7.34f), new Vector3(24.8f, 1.3f, .08f), panel, false);
        WorldSceneArt.Box(root, "WainscotLeft", new Vector3(-12.34f, .85f, 0f), new Vector3(.08f, 1.3f, 14.8f), panel, false);
        WorldSceneArt.Box(root, "WainscotRight", new Vector3(12.34f, .85f, 0f), new Vector3(.08f, 1.3f, 14.8f), panel, false);
        foreach (float x in new[] { -7.5f, 7.5f })
            WorldSceneArt.Box(root, "WainscotFront", new Vector3(x, .85f, -7.34f), new Vector3(9.8f, 1.3f, .08f), panel, false);
        WorldSceneArt.Box(root, "Ceiling", new Vector3(0f, 5.1f, 0f), new Vector3(24.8f, .2f, 14.8f), ceilingWood, false);
        for (float x = -10f; x <= 10.1f; x += 4f)
            WorldSceneArt.Box(root, "Beam", new Vector3(x, 4.85f, 0f), new Vector3(.35f, .35f, 14.8f), beamWood, false);
        WorldSceneArt.Box(root, "FrontFascia", new Vector3(0f, 4.4f, -7.3f), new Vector3(24.8f, 1.2f, .1f), plaster, false);

        // Light: three chandeliers over the work area, a floor lamp in the waiting corner.
        foreach (float x in new[] { -6f, 0f, 6f })
        {
            WorldSceneArt.Model(root, "Interior/Light_Chandelier", new Vector3(x, 4f, 1.5f), .9f);
            Lamp(root, new Vector3(x, 3.8f, 1.5f), 3.2f, 9f);
        }
        WorldSceneArt.Model(root, "Interior/Light_Floor2", new Vector3(10.9f, .24f, -6.2f), 1.7f);
        Lamp(root, new Vector3(10.9f, 1.8f, -6.2f), 1.6f, 5f);

        // Left wall: records. Right wall: filing cabinets and the waiting couch.
        WorldSceneArt.Model(root, "Interior/Shelf_Large", new Vector3(-11.75f, .24f, 2.2f), 2.5f, 90f);
        WorldSceneArt.Model(root, "Interior/Bookshelf", new Vector3(-11.8f, .24f, -1.6f), 2.3f, 90f);
        WorldSceneArt.Model(root, "Interior/Houseplant_6", new Vector3(-11.4f, .24f, -6.4f), 1.5f);
        WorldSceneArt.Model(root, "Interior/Drawer_4", new Vector3(11.8f, .24f, 4.6f), 1.35f, -90f);
        WorldSceneArt.Model(root, "Interior/Drawer_4", new Vector3(11.8f, .24f, 3.4f), 1.35f, -90f);
        WorldSceneArt.Model(root, "Interior/Drawer_2", new Vector3(11.8f, .24f, 2.2f), 1.1f, -90f);
        WorldSceneArt.Model(root, "Interior/Couch_Large1", new Vector3(11.2f, .24f, -2.6f), .95f, -90f);
        WorldSceneArt.Model(root, "Interior/Table_RoundSmall2", new Vector3(9.9f, .24f, -4.6f), .6f);
        WorldSceneArt.Model(root, "Interior/Houseplant_5", new Vector3(11.4f, .24f, 6.6f), 1.3f);

        // Victim's corner: chair, side table with a desk lamp; the victim stands at (-5, -1.5).
        WorldSceneArt.Model(root, "Interior/Chair_3", new Vector3(-6.4f, .24f, -1.1f), .95f, 150f);
        WorldSceneArt.Model(root, "Interior/Table_RoundSmall2", new Vector3(-7.2f, .24f, -2.4f), .65f);
        WorldSceneArt.Model(root, "Interior/Light_Desk", new Vector3(-7.2f, .9f, -2.4f), .45f, 30f);
        WorldSceneArt.Model(root, "Interior/Carpet_2", new Vector3(0f, .245f, -2.2f), 6f, 0f, true);

        // Front corners and small props.
        WorldSceneArt.Model(root, "Interior/Trashcan_Small1", new Vector3(-9.2f, .24f, 6.6f), .5f);
        WorldSceneArt.Model(root, "Interior/Stool", new Vector3(8.6f, .24f, 5.8f), .6f);

        // Windows on the side walls with a warm daylight panel behind the glass.
        var daylight = WorldSceneArt.Paint("WindowDaylight", new Color(1.6f, 1.15f, .65f), true);
        foreach (float z in new[] { -3.5f, 3.5f })
            for (int side = -1; side <= 1; side += 2)
            {
                WorldSceneArt.FrontArt(root, "WindowGlow", new Vector3(side * 12.33f, 2.6f, z), new Vector2(1.6f, 1.7f), daylight, side * 90f);
                WorldSceneArt.Model(root, "Interior/Window_Large2", new Vector3(side * 12.28f, 1.6f, z), 2f, side * 90f);
            }

        // Wanted posters between the windows.
        var poster = WorldSceneArt.Texture("tex_wanted_board", Color.white);
        WorldSceneArt.FrontArt(root, "WantedPoster", new Vector3(-12.32f, 2.6f, 0f), new Vector2(1.3f, 1.3f), poster, -90f);
        WorldSceneArt.FrontArt(root, "WantedPoster", new Vector3(12.32f, 2.6f, 0f), new Vector2(1.3f, 1.3f), poster, 90f);

        // A deputy on duty makes the station feel lived in.
        var deputy = WorldSceneArt.Empty(root, "Deputy", new Vector3(9.3f, .24f, 5.2f), 200f);
        var model = WorldSceneArt.Model(deputy, "Characters/Cowboy_Female", Vector3.zero, 1.78f);
        var animation = deputy.gameObject.AddComponent<NpcAnimator>();
        animation.animator = model.GetComponentInChildren<Animator>();
        animation.idle = WorldSceneArt.Clip("Characters/Cowboy_Female", "Idle");
    }

    static void Lamp(Transform parent, Vector3 position, float intensity, float range)
    {
        var light = WorldSceneArt.Empty(parent, "Lamp", position).gameObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, .76f, .48f);
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
    }

    // Wood planks painted in code (no AI art): plank rows with per-board tone, grain streaks and dark seams.
    static Material Planks(string name, Color tone, Vector2 tiling)
    {
        WorldSceneArt.EnsureFolder(TextureFolder);
        string path = $"{TextureFolder}/{name}.png";
        if (!File.Exists(path))
        {
            const int size = 512, boards = 8;
            var texture = new Texture2D(size, size, TextureFormat.RGB24, false);
            var random = new System.Random(name.GetHashCode());
            var pixels = new Color[size * size];
            int boardHeight = size / boards;
            for (int b = 0; b < boards; b++)
            {
                float shade = .82f + (float)random.NextDouble() * .3f;
                int joint = random.Next(size);
                float seed = (float)random.NextDouble() * 100f;
                for (int y = b * boardHeight; y < (b + 1) * boardHeight; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float grain = Mathf.PerlinNoise(x * .012f + seed, y * .35f) * .22f + Mathf.PerlinNoise(x * .08f, y * .9f + seed) * .08f;
                        float v = shade * (.86f + grain);
                        int local = y - b * boardHeight;
                        if (local < 2 || Mathf.Abs(x - joint) < 2) v *= .45f; // seams between boards
                        pixels[y * size + x] = new Color(tone.r * v * 1.6f, tone.g * v * 1.6f, tone.b * v * 1.6f);
                    }
            }
            texture.SetPixels(pixels);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
        }
        var material = WorldSceneArt.Paint(name, Color.white);
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(path));
        material.SetTextureScale("_BaseMap", tiling);
        material.SetFloat("_Smoothness", .28f);
        EditorUtility.SetDirty(material);
        return material;
    }
}

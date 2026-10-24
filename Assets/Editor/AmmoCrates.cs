using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ScammerNoScamming;

// Real supply crates (SupplyCrate with isFake = false): press E beside one to refill the revolver. Some stand inside
// the police station, the rest around the city: near the tower, the stable and vet, and along each road to a hideout.
// A crate that is used opens again after SupplyCrate.restockSeconds. Placed on free ground found by probing, so a
// rerun of the builder after the map changes still lands them on the street, never inside a building.
public static class AmmoCrates
{
    static readonly Vector3 HalfExtents = new Vector3(.6f, .4f, .6f);

    public static void Build(Transform station, IList<Vector3> hideouts)
    {
        var root = new GameObject("AmmoCrates");
        int made = 0;

        // Inside the station: under four of its ceiling lamps (the lobby is lit from them), turned to the station's frame.
        foreach (string lamp in new[] { "Light_1", "Light_7", "Light_5", "Light_4" })
        {
            var light = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l => l.name == lamp && l.transform.parent != null && l.transform.parent.name == "InteriorLights");
            if (light != null && Place(root.transform, new Vector2(light.transform.position.x, light.transform.position.z), station.eulerAngles.y, 3f)) made++;
        }

        // Around the city.
        var around = new List<Vector2>();
        foreach (var name in new[] { "PoliceStable_Yard", "QP_Vet", "QP_HorseSeller", "QP_CentralTower" })
        {
            var t = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).FirstOrDefault(x => x.name == name);
            if (t != null) around.Add(new Vector2(t.position.x + 6f, t.position.z + 4f));
        }
        around.Add(new Vector2(-45f, 30f));
        around.Add(new Vector2(45f, -30f));
        var from = new Vector2(station.position.x, station.position.z);
        foreach (var hideout in hideouts)
        {
            var to = new Vector2(hideout.x, hideout.z);
            around.Add(Vector2.Lerp(from, to, .33f));
            around.Add(Vector2.Lerp(from, to, .66f));
            around.Add(to + (Vector2.zero - to).normalized * 12f); // behind the guards' side, toward the city
        }
        foreach (var spot in around)
            if (Place(root.transform, spot, Random.Range(0f, 360f), 25f)) made++;

        Debug.Log("AmmoCrates: " + made + " placed of " + (around.Count + 4));
    }

    // Searches outward from `wanted` for ground a crate fits on.
    static bool Place(Transform parent, Vector2 wanted, float yaw, float maxRadius)
    {
        for (float r = 0f; r <= maxRadius; r += 2f)
            for (int a = 0; a < (r == 0f ? 1 : 10); a++)
            {
                var q = wanted + new Vector2(Mathf.Cos(a * Mathf.PI / 5f), Mathf.Sin(a * Mathf.PI / 5f)) * r;
                if (!Ground(q, out float y)) continue;
                var centre = new Vector3(q.x, y + HalfExtents.y + .08f, q.y);
                if (Physics.CheckBox(centre, HalfExtents, Quaternion.Euler(0f, yaw, 0f), ~0, QueryTriggerInteraction.Ignore)) continue;
                Make(parent, new Vector3(q.x, y, q.y), yaw);
                return true;
            }
        return false;
    }

    // Lowest walkable surface near the street level (rooftops are above 1.5 m and ignored).
    static bool Ground(Vector2 xz, out float y)
    {
        y = 0f;
        var hits = Physics.RaycastAll(new Vector3(xz.x, 60f, xz.y), Vector3.down, 120f, ~0, QueryTriggerInteraction.Ignore)
            .Where(h => h.normal.y > .9f && h.point.y < 1.5f).OrderBy(h => h.point.y).ToArray();
        if (hits.Length == 0) return false;
        y = hits[0].point.y;
        return true;
    }

    static void Make(Transform parent, Vector3 ground, float yaw, bool fake = false)
    {
        var crate = new GameObject("AmmoCrate");
        crate.transform.SetParent(parent, false);
        crate.transform.SetPositionAndRotation(ground, Quaternion.Euler(0f, yaw, 0f));
        var supply = crate.AddComponent<SupplyCrate>();
        supply.isFake = fake;

        // The ammo box model (a flat, long box) sits on the ground; its own collider stops the player walking through it.
        var model = (GameObject)PrefabUtility.InstantiatePrefab(PropPrefabs.Prefab(fake ? "AmmoBoxLarge" : "AmmoBox"), crate.transform);
        model.name = "Model";
        model.transform.localPosition = Vector3.zero;

        var light = new GameObject("Glow").AddComponent<Light>();
        light.transform.SetParent(crate.transform, false);
        light.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        light.type = LightType.Point;
        light.color = new Color(.65f, 1f, .5f);
        light.intensity = 3f;
        light.range = 5f;
        light.shadows = LightShadows.None;
    }
}

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ScammerNoScamming.Tests
{
    public class CityTests
    {
        CityResult city;

        [TearDown]
        public void TearDown()
        {
            if (city != null && city.Root != null) Object.DestroyImmediate(city.Root);
        }

        [Test]
        public void Layout_has_expected_building_counts()
        {
            city = CityBuilder.Build(new CityMaterials());

            Assert.GreaterOrEqual(city.Towers.Count, 10, "towers");
            Assert.GreaterOrEqual(city.MidRise.Count, 30, "mid-rise");
            Assert.GreaterOrEqual(city.Houses.Count, 80, "houses");
            Assert.IsNotNull(city.Station);
            Assert.IsNotNull(city.Hideout, "a hideout spot should be found");
        }

        [Test]
        public void Same_seed_gives_same_layout()
        {
            city = CityBuilder.Build(new CityMaterials(), 7);
            int towers = city.Towers.Count;
            int houses = city.Houses.Count;
            Vector3 firstTower = city.Towers[0].transform.position;
            Object.DestroyImmediate(city.Root);

            city = CityBuilder.Build(new CityMaterials(), 7);

            Assert.AreEqual(towers, city.Towers.Count);
            Assert.AreEqual(houses, city.Houses.Count);
            Assert.AreEqual(0f, Vector3.Distance(firstTower, city.Towers[0].transform.position), 0.001f);
        }

        [Test]
        public void Towers_are_taller_than_mid_rise_and_houses()
        {
            city = CityBuilder.Build(new CityMaterials());

            float shortestTower = float.MaxValue;
            foreach (var tower in city.Towers)
                shortestTower = Mathf.Min(shortestTower, Top(tower));
            float tallestMid = 0f;
            foreach (var mid in city.MidRise)
                tallestMid = Mathf.Max(tallestMid, Top(mid));
            float tallestHouse = 0f;
            foreach (var house in city.Houses)
                tallestHouse = Mathf.Max(tallestHouse, Top(house));

            Assert.Greater(shortestTower, tallestMid, "ring order: towers above mid-rise");
            Assert.Greater(tallestMid, tallestHouse, "ring order: mid-rise above houses");
        }

        [Test]
        public void Everything_stays_inside_the_ground_plane()
        {
            city = CityBuilder.Build(new CityMaterials());

            foreach (var renderer in city.Root.GetComponentsInChildren<Renderer>())
            {
                if (renderer.name == "Ground") continue;
                Vector3 far = renderer.bounds.center;
                far.y = 0f;
                Assert.Less(far.magnitude, CityBuilder.GroundSize * 0.5f, renderer.name);
            }
        }

        [UnityTest]
        public IEnumerator Station_door_is_open_and_walls_are_solid()
        {
            city = CityBuilder.Build(new CityMaterials());
            yield return null;

            // Through the door (x = 0) the first thing hit is inside the hall, past the front wall.
            Assert.IsTrue(Physics.Raycast(new Vector3(0f, 1.5f, -29f), Vector3.forward, out var through, 60f));
            Assert.Greater(through.point.z, -CityBuilder.StationDepth * 0.5f + 1f, "ray should pass the door");

            // Beside the door the front wall must stop the ray.
            Assert.IsTrue(Physics.Raycast(new Vector3(8f, 1.5f, -29f), Vector3.forward, out var wall, 60f));
            Assert.Less(wall.point.z, -CityBuilder.StationDepth * 0.5f + 1f, "front wall should block");
        }

        [UnityTest]
        public IEnumerator Main_avenue_is_clear_for_a_horse()
        {
            city = CityBuilder.Build(new CityMaterials());
            yield return null;

            // Horse-sized capsule sliding down the south avenue from the plaza edge to just short of the wall.
            Vector3 bottom = new Vector3(0f, 0.9f, -35f);
            Vector3 top = new Vector3(0f, 2.2f, -35f);
            bool hit = Physics.CapsuleCast(bottom, top, 0.7f, Vector3.back, out var info, 172f);

            Assert.IsFalse(hit, hit ? "blocked by " + info.collider.name + " at z=" + info.point.z : "");
        }

        static float Top(GameObject building)
        {
            float top = 0f;
            foreach (var renderer in building.GetComponentsInChildren<Renderer>())
                top = Mathf.Max(top, renderer.bounds.max.y);
            return top;
        }
    }
}

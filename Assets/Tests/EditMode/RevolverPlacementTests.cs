using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ScammerNoScamming.Tests
{
    public class RevolverPlacementTests
    {
        const string Folder = "Assets/Art/Models/WesternRevolver/";

        [TestCase(0f, 0f)]
        [TestCase(45f, 0f)]
        [TestCase(180f, 0f)]
        [TestCase(0f, 30f)]
        [TestCase(45f, 30f)]
        [TestCase(180f, 30f)]
        public void ActualRevolverKeepsCameraLocalSizeCentreAndNearClipClearance(float yaw, float pitch)
        {
            var player = new GameObject("Translated revolver test player");
            try
            {
                player.transform.SetPositionAndRotation(new Vector3(17f, 4f, -32f), Quaternion.Euler(0f, yaw, 0f));
                var pivot = new GameObject("Camera pivot").transform;
                pivot.SetParent(player.transform, false);
                pivot.localPosition = Vector3.up * 1.6f;
                pivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
                var camera = new GameObject("Camera").AddComponent<Camera>();
                camera.transform.SetParent(pivot, false);
                camera.nearClipPlane = .05f;
                var view = player.AddComponent<RevolverViewModel>();
                view.revolverPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "WesternRevolver.fbx");
                Assert.IsNotNull(view.revolverPrefab, "Use the actual shipped revolver model.");
                view.metalMaterial = AssetDatabase.LoadAssetAtPath<Material>(Folder + "gunMetal.mat");
                view.handleMaterial = AssetDatabase.LoadAssetAtPath<Material>(Folder + "gunHandle.mat");
                view.modelEuler = new Vector3(90f, 0f, 0f);
                view.absoluteEuler = true;
                view.Build();

                Assert.IsNotNull(view.Rig);
                var renderers = view.Rig.GetComponentsInChildren<Renderer>(true);
                Assert.That(renderers.Length, Is.EqualTo(9), "Build must use the model, not fallback geometry.");
                Bounds local = BoundsInSpace(renderers, view.Rig);
                Assert.That(local.center.magnitude, Is.LessThan(.0002f));
                Assert.That(Mathf.Max(local.size.x, Mathf.Max(local.size.y, local.size.z)), Is.EqualTo(.34f).Within(.0002f),
                    "Camera yaw and pitch must not alter the gun's normalized size.");
                Assert.That(view.Rig.localPosition, Is.EqualTo(view.restPosition));
                Bounds cameraBounds = BoundsInSpace(renderers, camera.transform);
                Assert.That(cameraBounds.min.z, Is.GreaterThan(camera.nearClipPlane + .01f),
                    "Every rendered part must stay in front of the camera's near plane.");
            }
            finally { Object.DestroyImmediate(player); }
        }

        static Bounds BoundsInSpace(Renderer[] renderers, Transform space)
        {
            Bounds result = default;
            bool first = true;
            foreach (var renderer in renderers)
            {
                Vector3 min = renderer.localBounds.min, max = renderer.localBounds.max;
                for (int x = 0; x < 2; x++)
                for (int y = 0; y < 2; y++)
                for (int z = 0; z < 2; z++)
                {
                    Vector3 corner = new Vector3(x == 0 ? min.x : max.x, y == 0 ? min.y : max.y, z == 0 ? min.z : max.z);
                    Vector3 point = space.InverseTransformPoint(renderer.transform.TransformPoint(corner));
                    if (first) { result = new Bounds(point, Vector3.zero); first = false; }
                    else result.Encapsulate(point);
                }
            }
            return result;
        }
    }
}

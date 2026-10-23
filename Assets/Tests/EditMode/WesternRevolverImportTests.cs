using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ScammerNoScamming.Tests
{
    // The revolver FBX points at textures on its author's disk, so Unity finds none by itself; these checks
    // fail if the external URP materials ever stop being wired to it, or the model stops being a believable size.
    public class WesternRevolverImportTests
    {
        const string Folder = "Assets/Art/Models/WesternRevolver";

        static GameObject LoadModel()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/WesternRevolver.fbx");
            Assert.IsNotNull(model, "the revolver model imports");
            return model;
        }

        [Test]
        public void Every_part_uses_a_textured_URP_material_that_lives_beside_the_model()
        {
            var renderers = LoadModel().GetComponentsInChildren<Renderer>(true);
            Assert.AreEqual(9, renderers.Length);

            foreach (var renderer in renderers)
            {
                Assert.AreEqual(1, renderer.sharedMaterials.Length, renderer.name);
                var material = renderer.sharedMaterial;
                Assert.IsNotNull(material, renderer.name + " has a material");
                Assert.IsTrue(AssetDatabase.GetAssetPath(material).StartsWith(Folder + "/"), renderer.name + " uses a material asset, not an embedded one");
                Assert.AreEqual("Universal Render Pipeline/Lit", material.shader.name, renderer.name);
                Assert.IsNotNull(material.GetTexture("_BaseMap"), renderer.name + " has its texture");
            }
        }

        [Test]
        public void The_grip_is_wood_and_the_rest_is_metal()
        {
            foreach (var renderer in LoadModel().GetComponentsInChildren<Renderer>(true))
            {
                string expected = renderer.name == "handle" ? "gunHandle" : "gunMetal";
                Assert.AreEqual(expected, renderer.sharedMaterial.name, renderer.name);
            }
        }

        [Test]
        public void The_revolver_is_about_a_quarter_of_a_metre_long_and_points_down_Z()
        {
            Bounds bounds = default;
            bool first = true;
            foreach (var renderer in LoadModel().GetComponentsInChildren<Renderer>(true))
            {
                if (first) { bounds = renderer.bounds; first = false; }
                else bounds.Encapsulate(renderer.bounds);
            }

            Assert.AreEqual(0.27f, bounds.size.z, 0.04f, "length, in metres");
            Assert.Greater(bounds.size.z, bounds.size.y);
            Assert.Greater(bounds.size.z, bounds.size.x);
        }
    }
}

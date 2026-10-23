using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ScammerNoScamming.Tests
{
    // The bandit and horse FBX files point at textures on their authors' disks and the five Mixamo clips need
    // retargeting, so none of it works by itself. These fail if the wiring (materials, humanoid avatar, controller) breaks.
    public class ScammerCharacterImportTests
    {
        const string ScammerFolder = "Assets/Art/Characters/Scammer";
        const string HorseFolder = "Assets/Art/Characters/Horse";

        static GameObject LoadPrefab()
        {
            var prefab = Resources.Load<GameObject>(ScammerAnimation.ModelPath);
            Assert.IsNotNull(prefab, "the scammer prefab is where the game looks for it");
            return prefab;
        }

        [Test]
        public void The_model_is_a_valid_humanoid_driven_by_the_scammer_controller_without_root_motion()
        {
            var animator = LoadPrefab().GetComponent<Animator>();
            Assert.IsNotNull(animator);
            Assert.IsNotNull(animator.avatar);
            Assert.IsTrue(animator.avatar.isValid);
            Assert.IsTrue(animator.avatar.isHuman, "humanoid, so the Mixamo clips retarget onto it");
            Assert.AreEqual("ScammerAnimator", animator.runtimeAnimatorController.name);
            Assert.IsFalse(animator.applyRootMotion, "the NavMesh agent moves the enemy, not the clip");
        }

        [Test]
        public void The_controller_has_the_six_states_with_the_right_clips_and_the_four_parameters()
        {
            var controller = (AnimatorController)LoadPrefab().GetComponent<Animator>().runtimeAnimatorController;

            var parameters = new System.Collections.Generic.List<string>();
            foreach (var p in controller.parameters) parameters.Add(p.name);
            CollectionAssert.AreEquivalent(new[]
            {
                ScammerAnimation.SpeedParameter, ScammerAnimation.ShootingParameter,
                ScammerAnimation.DeadParameter, ScammerAnimation.MountedParameter,
            }, parameters);

            var states = controller.layers[0].stateMachine.states;
            Assert.AreEqual(6, states.Length);
            foreach (var wanted in new[] { "Idle", "Walk", "Run", "Shoot", "Sit", "Death" })
            {
                AnimatorState found = null;
                foreach (var s in states) if (s.state.name == wanted) found = s.state;
                Assert.IsNotNull(found, wanted + " state");
                var clip = found.motion as AnimationClip;
                Assert.IsNotNull(clip, wanted + " has a clip");
                Assert.AreEqual(wanted != "Death", clip.isLooping, wanted + (wanted == "Death" ? " plays once and stays down" : " loops"));
                Assert.IsTrue(clip.isHumanMotion, wanted + " is a humanoid clip");
            }
            Assert.AreEqual("Idle", controller.layers[0].stateMachine.defaultState.name);

            // Dead is reachable from any state, and a dead scammer cannot be pulled back into the firing stance.
            bool toDeath = false;
            foreach (var t in controller.layers[0].stateMachine.anyStateTransitions)
            {
                if (t.destinationState.name == "Death") toDeath = true;
                if (t.destinationState.name == "Shoot")
                    Assert.IsTrue(System.Linq.Enumerable.Any(t.conditions,
                        c => c.parameter == ScammerAnimation.DeadParameter && c.mode == AnimatorConditionMode.IfNot));
            }
            Assert.IsTrue(toDeath);

            // Riding: Sit is the pose while Mounted, and the foot shooting stance is never used in the saddle.
            bool toSit = false;
            foreach (var t in controller.layers[0].stateMachine.anyStateTransitions)
            {
                if (t.destinationState.name == "Sit") toSit = true;
                if (t.destinationState.name == "Shoot")
                    Assert.IsTrue(System.Linq.Enumerable.Any(t.conditions,
                        c => c.parameter == ScammerAnimation.MountedParameter && c.mode == AnimatorConditionMode.IfNot));
            }
            Assert.IsTrue(toSit);

            // The upper-body layer: off until the rider turns it on, arms and spine only, relaxed or aiming.
            Assert.AreEqual(2, controller.layers.Length);
            var upper = controller.layers[1];
            Assert.AreEqual(0f, upper.defaultWeight);
            Assert.AreEqual(AnimatorLayerBlendingMode.Override, upper.blendingMode);
            Assert.IsNotNull(upper.avatarMask);
            Assert.IsTrue(upper.avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm));
            Assert.IsTrue(upper.avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.Body));
            Assert.IsFalse(upper.avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftLeg));
            Assert.IsFalse(upper.avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.RightLeg));
            Assert.IsFalse(upper.avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.Root));
            var upperNames = new System.Collections.Generic.List<string>();
            foreach (var st in upper.stateMachine.states) upperNames.Add(st.state.name);
            CollectionAssert.AreEquivalent(new[] { "Relax", "Aim" }, upperNames);
        }

        [Test]
        public void The_horse_model_prefab_idles_and_walks_with_its_body_centred_on_the_origin()
        {
            var prefab = Resources.Load<GameObject>(ScammerAnimation.HorseModelPath);
            Assert.IsNotNull(prefab, "the horse prefab is where the game looks for it");
            var animator = prefab.GetComponentInChildren<Animator>();
            Assert.AreEqual("HorseAnimator", animator.runtimeAnimatorController.name);
            Assert.IsFalse(animator.applyRootMotion);

            var controller = (AnimatorController)animator.runtimeAnimatorController;
            foreach (var wanted in new[] { "Idle", "Walk" })
            {
                AnimatorState found = null;
                foreach (var st in controller.layers[0].stateMachine.states) if (st.state.name == wanted) found = st.state;
                Assert.IsNotNull(found, wanted);
                Assert.IsTrue(((AnimationClip)found.motion).isLooping, wanted + " loops");
            }

            // The model has its origin at the tail; the wrapper shifts it so the middle of the back sits over the origin.
            Transform back = null;
            foreach (var t in prefab.GetComponentsInChildren<Transform>(true)) if (t.name == "spine_02") back = t;
            Assert.IsNotNull(back);
            Assert.AreEqual(0f, back.position.z, 0.15f);
            Assert.AreEqual(1.55f, back.position.y, 0.1f);
        }

        [Test]
        public void Every_part_has_a_textured_URP_material_stored_beside_the_model()
        {
            var parts = LoadPrefab().GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Assert.AreEqual(9, parts.Length);
            foreach (var part in parts)
            {
                var material = part.sharedMaterial;
                Assert.IsNotNull(material, part.name);
                Assert.IsTrue(AssetDatabase.GetAssetPath(material).StartsWith(ScammerFolder + "/Materials/"), part.name + " uses a material asset");
                Assert.AreEqual("Universal Render Pipeline/Lit", material.shader.name, part.name);
                Assert.IsNotNull(material.GetTexture("_BaseMap"), part.name + " has its texture");
            }
        }

        [Test]
        public void The_model_stands_about_1_8_m_tall_to_match_the_hit_zones()
        {
            var parts = LoadPrefab().GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Bounds bounds = parts[0].bounds;
            foreach (var part in parts) bounds.Encapsulate(part.bounds);
            Assert.AreEqual(1.8f, bounds.size.y, 0.12f);
        }

        [Test]
        public void The_right_hand_holds_a_textured_revolver_with_a_muzzle_at_the_front_of_the_barrel()
        {
            var prefab = LoadPrefab();
            Transform Find(string name)
            {
                foreach (var t in prefab.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
                return null;
            }

            var grip = Find("RevolverGrip");
            Assert.IsNotNull(grip, "the revolver is held");
            Assert.AreSame(Find("bip01_r_hand"), grip.parent, "in the right hand");
            Assert.AreEqual(1, System.Linq.Enumerable.Count(prefab.GetComponentsInChildren<Transform>(true), t => t.name == "Revolver"), "exactly one revolver");

            var revolverParts = Find("Revolver").GetComponentsInChildren<Renderer>(true);
            Assert.AreEqual(9, revolverParts.Length);
            foreach (var part in revolverParts)
                Assert.IsNotNull(part.sharedMaterial.GetTexture("_BaseMap"), part.name + " keeps its texture");

            // The muzzle is about a quarter of a metre ahead of the grip, along the barrel.
            var muzzle = Find(ScammerAnimation.MuzzleName);
            Assert.AreSame(grip, muzzle.parent);
            Assert.AreEqual(0.24f, muzzle.localPosition.z, 0.03f);
            Assert.Greater(muzzle.localPosition.z, Mathf.Abs(muzzle.localPosition.x) * 10f);
        }

        [Test]
        public void The_horse_keeps_its_textured_materials_and_its_two_clips()
        {
            var horse = AssetDatabase.LoadAssetAtPath<GameObject>(HorseFolder + "/HorseDemo.fbx");
            Assert.IsNotNull(horse);
            foreach (var part in horse.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                foreach (var material in part.sharedMaterials)
                {
                    Assert.IsNotNull(material);
                    Assert.IsTrue(AssetDatabase.GetAssetPath(material).StartsWith(HorseFolder + "/Materials/"), material.name);
                    Assert.IsNotNull(material.GetTexture("_BaseMap"), material.name);
                }

            var clips = new System.Collections.Generic.List<string>();
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(HorseFolder + "/HorseDemo.fbx"))
                if (o is AnimationClip clip && !clip.name.StartsWith("__preview__")) clips.Add(clip.name);
            CollectionAssert.IsSupersetOf(clips, new[] { "Horse_Idle", "Horse_Walk" });
        }
    }
}

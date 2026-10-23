using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ScammerNoScamming
{
    // Sandbox helper: lays the five road traps in a row (along +Z from this object) and rebuilds them on a key press,
    // because a sprung trap, a woken ambusher or a shot-down villager cannot be put back. The reset also heals the
    // player, refills the gun and clears Trouble and the trap log, so every trap can be tried from a clean start.
    // Built at runtime so the scene file only stores this component and its materials.
    public class TrapCourse : MonoBehaviour
    {
        public Key resetKey = Key.T;
        public float spacing = 8f;
        public int ridersFromSign = 2;
        // Null keeps the default material (fine for tests).
        public Material realCrate, fakeCrate, sign, villager, moneyBag, booth, scammer;

        public IReadOnlyList<Trap> Traps => traps;

        readonly List<Trap> traps = new List<Trap>();
        GameObject root;

        void Awake()
        {
            Build();
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard[resetKey].wasPressedThisFrame) ResetAll();
        }

        public void ResetAll()
        {
            // The old objects are only destroyed at the end of the frame, so switch them off at once.
            root.SetActive(false);
            Destroy(root);
            traps.Clear();
            Build();

            TrapLog.Reset();
            TroubleMeter.ResetAll();
            var player = FindFirstObjectByType<PlayerCharacter>();
            if (player == null || player.Health.IsDead) return;
            player.Health.SetMax(player.Health.max);
            var gun = player.GetComponent<PlayerWeaponController>();
            if (gun != null) gun.Refill();
        }

        void Build()
        {
            root = new GameObject("Traps");
            root.transform.SetParent(transform, false);

            // 1 and 2: a real crate beside a fake one that wakes a hidden scammer.
            var real = Prop("SupplyCrate_Real", PrimitiveType.Cube, Spot(0), Vector3.one, 0f, realCrate);
            real.AddComponent<SupplyCrate>().isFake = false;
            var fake = Prop("SupplyCrate_Fake", PrimitiveType.Cube, Spot(1), Vector3.one, 0f, fakeCrate);
            var fakeTrap = fake.AddComponent<SupplyCrate>();
            fakeTrap.isFake = true;
            fakeTrap.activateOnSpring = new[] { Hidden(fake.transform.position + Vector3.right * 4f, fake.transform.position) };

            // 3: a phishing ad that locks the gun and calls riders in.
            var ad = Prop("PhishingSign", PrimitiveType.Cube, Spot(2) + Vector3.up * 1.7f, new Vector3(3f, 1.5f, 0.1f), 90f, sign);
            var phishing = ad.AddComponent<PhishingSign>();
            var riders = new GameObject("Riders from the sign").AddComponent<AmbushGroup>();
            riders.transform.SetParent(root.transform, false);
            riders.kind = AmbushKind.Riders;
            riders.count = ridersFromSign;
            riders.minRadius = 20f;
            riders.maxRadius = 30f;
            riders.material = scammer;
            riders.trigger = phishing;

            // 4: a "villager" who lunges if you hold eye contact; shoot it first.
            var stranger = Prop("DisguisedScammer", PrimitiveType.Capsule, Spot(3) + Vector3.up * 0.5f, Vector3.one, 0f, villager);
            stranger.AddComponent<DisguisedScammer>();

            // 5: a money bag in an alley with scammers waiting either side.
            var bag = Prop("AmbushBait_MoneyBag", PrimitiveType.Cube, Spot(4) + Vector3.up * -0.2f, Vector3.one * 0.6f, 0f, moneyBag);
            bag.AddComponent<AmbushBait>().activateOnSpring = new[]
            {
                Hidden(bag.transform.position + new Vector3(5f, 0f, 0f), bag.transform.position),
                Hidden(bag.transform.position + new Vector3(-5f, 0f, 3f), bag.transform.position),
            };

            // 6: a phone that rings until someone answers it.
            var phone = Prop("CallCenterBooth", PrimitiveType.Cube, Spot(5) + Vector3.up * 0.8f, new Vector3(1.2f, 2.6f, 1.2f), 0f, booth);
            phone.AddComponent<CallCenterBooth>();

            traps.AddRange(root.GetComponentsInChildren<Trap>());
        }

        Vector3 Spot(int index) => transform.position + Vector3.forward * (index * spacing) + Vector3.up * 0.5f;

        GameObject Prop(string name, PrimitiveType shape, Vector3 position, Vector3 scale, float yaw, Material material)
        {
            var prop = GameObject.CreatePrimitive(shape);
            prop.name = name;
            prop.transform.SetParent(root.transform, false);
            prop.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            prop.transform.localScale = scale;
            if (material != null) prop.GetComponent<Renderer>().sharedMaterial = material;
            return prop;
        }

        // A real scammer waiting switched off until its trap springs, facing the trap.
        GameObject Hidden(Vector3 position, Vector3 facing)
        {
            position.y = 0f;
            var enemy = PlayerSandbox.CreateEnemy(position, scammer != null ? (_ => scammer) : (System.Func<Color, Material>)null);
            Vector3 toTrap = facing - position;
            toTrap.y = 0f;
            if (toTrap.sqrMagnitude > 0.01f) enemy.transform.rotation = Quaternion.LookRotation(toTrap);
            enemy.transform.SetParent(root.transform, true);
            enemy.gameObject.SetActive(false);
            return enemy.gameObject;
        }

        void OnGUI()
        {
            GUI.Label(new Rect(12f, Screen.height - 78f, 600f, 24f), "[" + resetKey + "] Reset traps (rearm, heal, refill ammo, clear Trouble)");
        }
    }
}

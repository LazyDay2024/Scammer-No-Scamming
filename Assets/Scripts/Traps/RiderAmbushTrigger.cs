using UnityEngine;

namespace ScammerNoScamming
{
    // Templates live beside this trigger. Only disposable clones participate in a chase.
    [RequireComponent(typeof(BoxCollider))]
    public class RiderAmbushTrigger : Trap
    {
        public Transform templateRoot;
        public GameObject LivePack => livePack;
        public override TrapKind Kind => TrapKind.Ambush;
        protected override float DefaultDamage => 0f;

        BoxCollider volume;
        GameObject livePack;

        void Awake()
        {
            volume = GetComponent<BoxCollider>();
            volume.isTrigger = true;
            HideTemplate();
        }

        void Update()
        {
            if (HasSprung || IsDefused) return;
            var player = FindPlayer();
            if (player == null || !player.gameObject.activeInHierarchy) return;
            var health = player.GetComponent<Health>();
            if (health != null && health.IsDead) return;
            // Mounted players disable their CharacterController: physics callbacks alone miss them.
            if (Contains(player.transform.position)) Spring(player.gameObject);
        }

        public bool Contains(Vector3 worldPoint)
        {
            if (volume == null) volume = GetComponent<BoxCollider>();
            if (volume == null || !volume.enabled) return false;
            Vector3 point = volume.transform.InverseTransformPoint(worldPoint) - volume.center;
            Vector3 half = volume.size * .5f;
            return Mathf.Abs(point.x) <= half.x && Mathf.Abs(point.y) <= half.y && Mathf.Abs(point.z) <= half.z;
        }

        protected override void OnSprung(GameObject target)
        {
            HideTemplate();
            if (templateRoot == null) return;
            livePack = Instantiate(templateRoot.gameObject, transform, true);
            livePack.name = "Active mounted ambush";
            livePack.SetActive(true);
            foreach (var health in livePack.GetComponentsInChildren<Health>(true)) health.RestoreFull(health.max);
            foreach (var rider in livePack.GetComponentsInChildren<MountedScammer>(true))
            {
                rider.gameObject.SetActive(true);
                rider.ResetForAction();
                if (target != null) rider.Alert(target.transform.position);
            }
            NpcGunfire.EnsureExists();
        }

        public override void ResetTrap()
        {
            RemoveLivePack();
            HideTemplate();
            base.ResetTrap();
        }

        public override void ResetForAction()
        {
            RemoveLivePack();
            HideTemplate();
            base.ResetForAction();
        }

        void HideTemplate()
        {
            if (templateRoot != null) templateRoot.gameObject.SetActive(false);
        }

        void RemoveLivePack(bool detach = true)
        {
            if (livePack == null) return;
            livePack.SetActive(false);
            // Exclude a deferred-destroy pack from Trap.ResetForAction's child scan this frame.
            if (detach) livePack.transform.SetParent(null, true);
            if (Application.isPlaying) Destroy(livePack);
            else DestroyImmediate(livePack);
            livePack = null;
        }

        // Parenting while Unity is destroying/deactivating an ancestor is unsafe; no reset scan runs here.
        void OnDisable() => RemoveLivePack(false);
    }
}

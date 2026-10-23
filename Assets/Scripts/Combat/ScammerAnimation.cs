using UnityEngine;

namespace ScammerNoScamming
{
    // Puts the bandit model (holding a revolver in its right hand) on a scammer and drives its Animator from what the
    // Enemy is doing: walking while it patrols, running while it chases, a firing stance while it stands and fights,
    // and lying down when it dies. A RiderScammer also gets the horse model, which idles or walks (faster for faster
    // rides), and the bandit sits in the saddle with only its upper body aiming.
    // The placeholder boxes keep their colliders (they are the hit zones) but stop being drawn.
    // Without the prefabs (Resources/Scammer) the boxes stay visible, so nothing breaks.
    public class ScammerAnimation : MonoBehaviour
    {
        public const string ModelPath = "Scammer/ScammerModel";
        public const string HorseModelPath = "Scammer/HorseModel";
        public const string SpeedParameter = "Speed";
        public const string ShootingParameter = "Shooting";
        public const string MountedParameter = "Mounted";
        public const string DeadParameter = "Dead";
        public const string DeathClipName = "Scammer_Death";
        public const string MuzzleName = "Muzzle";
        const int UpperBodyLayer = 1;

        // Below this ground speed (m/s) an engaged enemy on foot counts as standing and shooting.
        public float standingSpeed = 0.5f;
        // The horse has only idle and walk clips: faster rides play the walk faster, up to this many times.
        public float horseWalkSpeed = 1.6f;
        public float maxHorseClipSpeed = 3.5f;

        Enemy enemy;
        RiderScammer rider;
        Animator animator;
        Animator horseAnimator;
        Vector3 lastPosition;
        float speed;

        // Length of the death clip, or 0 when the controller has none (the body is then tipped over instead).
        public float DeathSeconds { get; private set; }
        public float Speed => speed;
        public Animator Animator => animator;
        public Animator HorseAnimator => horseAnimator;

        public static ScammerAnimation Attach(Enemy enemy)
        {
            return Create(enemy, enemy.transform);
        }

        // The bandit goes under seat (the Rider child) and the horse model under horse (the Horse child).
        public static ScammerAnimation AttachRider(RiderScammer rider, Transform seat, Transform horse)
        {
            var horsePrefab = Resources.Load<GameObject>(HorseModelPath);
            if (horsePrefab == null) return null;

            var driver = Create(rider, seat);
            if (driver == null) return null;

            var horseModel = Instantiate(horsePrefab, horse);
            horseModel.transform.localPosition = Vector3.zero;
            horseModel.transform.localRotation = Quaternion.identity;
            driver.rider = rider;
            driver.horseAnimator = horseModel.GetComponentInChildren<Animator>();
            return driver;
        }

        static ScammerAnimation Create(Enemy enemy, Transform parent)
        {
            var prefab = Resources.Load<GameObject>(ModelPath);
            if (prefab == null) return null;

            foreach (var box in enemy.GetComponentsInChildren<MeshRenderer>())
                box.enabled = false;

            var model = Instantiate(prefab, parent);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;

            // Shots leave from the revolver in the right hand, not from the head.
            var tracer = enemy.GetComponent<ShotTracer>();
            if (tracer != null)
                foreach (var part in model.GetComponentsInChildren<Transform>())
                    if (part.name == MuzzleName) tracer.muzzle = part;

            var driver = enemy.gameObject.AddComponent<ScammerAnimation>();
            driver.enemy = enemy;
            driver.animator = model.GetComponent<Animator>();
            foreach (var clip in driver.animator.runtimeAnimatorController.animationClips)
                if (clip.name == DeathClipName) driver.DeathSeconds = clip.length;
            driver.lastPosition = enemy.transform.position;
            return driver;
        }

        // The horse went down: its pose stays as it fell.
        public void StopHorse()
        {
            if (horseAnimator != null) horseAnimator.enabled = false;
        }

        void Update()
        {
            if (animator == null) return;

            if (enemy.Health.IsDead)
            {
                // Lie down with the death clip and stay there; without a clip, freeze where it was (Npc tips it over).
                if (DeathSeconds > 0f)
                {
                    animator.SetBool(ShootingParameter, false);
                    animator.SetBool(MountedParameter, false);
                    animator.SetLayerWeight(UpperBodyLayer, 0f);
                    animator.SetBool(DeadParameter, true);
                }
                else
                {
                    animator.enabled = false;
                }
                if (horseAnimator != null) horseAnimator.SetFloat(SpeedParameter, 0f);
                return;
            }

            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            // Measured from where it actually moved, so patrol steps, NavMesh runs and teleports all read the same.
            Vector3 moved = transform.position - lastPosition;
            moved.y = 0f;
            lastPosition = transform.position;
            speed = Mathf.Lerp(speed, moved.magnitude / dt, 1f - Mathf.Exp(-10f * dt));

            bool mounted = rider != null && rider.IsMounted;
            animator.SetFloat(SpeedParameter, speed);
            animator.SetBool(MountedParameter, mounted);
            animator.SetLayerWeight(UpperBodyLayer, mounted ? 1f : 0f);
            // A rider aims on the move; a man on foot stops to shoot.
            animator.SetBool(ShootingParameter, enemy.IsEngaged && (mounted || speed < standingSpeed));

            if (horseAnimator != null && horseAnimator.enabled)
            {
                horseAnimator.SetFloat(SpeedParameter, speed);
                horseAnimator.speed = Mathf.Clamp(speed / horseWalkSpeed, 1f, maxHorseClipSpeed);
            }
        }
    }
}

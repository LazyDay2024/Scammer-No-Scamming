using UnityEngine;

namespace ScammerNoScamming
{
    // E mounts the nearest free horse in range, and E again dismounts to the horse's right.
    [RequireComponent(typeof(PlayerController), typeof(PlayerControls))]
    public class PlayerMount : MonoBehaviour
    {
        public float mountRange = 2.5f;

        PlayerController player;
        PlayerControls controls;
        PlayerCharacter character;

        public HorseController CurrentHorse { get; private set; }
        public HorseController Candidate { get; private set; }
        public bool IsMounted => CurrentHorse != null;

        void Awake()
        {
            player = GetComponent<PlayerController>();
            controls = GetComponent<PlayerControls>();
            character = GetComponent<PlayerCharacter>();
        }

        void Update()
        {
            if (CurrentHorse == null)
            {
                Candidate = FindNearestFreeHorse();
                if (Candidate != null && !WorldInteractor.HasFocusedInteraction && controls.Interact.WasPressedThisFrame())
                    Mount(Candidate);
            }
            else if (controls.Interact.WasPressedThisFrame() || CurrentHorse.IsDown)
            {
                Dismount();
            }
        }

        void Mount(HorseController horse)
        {
            GetComponent<RevolverAmmo>()?.NotifyMountTransition();
            CurrentHorse = horse;
            Candidate = null;
            horse.SetRider(controls);
            player.EnterMount(horse.Seat);
            if (character != null) character.SetRiding(horse.GetComponent<Collider>());
        }

        public void Dismount()
        {
            if (CurrentHorse == null) return;
            var horse = CurrentHorse;
            if (horse == null) return;
            GetComponent<RevolverAmmo>()?.NotifyMountTransition();
            Vector3 exitPoint = horse.GetDismountPoint();
            float exitYaw = horse.transform.eulerAngles.y;
            horse.ClearRider();
            CurrentHorse = null;
            if (character != null) character.SetRiding(null);
            player.ExitMount(exitPoint, exitYaw);
        }

        HorseController FindNearestFreeHorse()
        {
            HorseController best = null;
            float bestDistance = mountRange * mountRange;
            foreach (var horse in HorseController.All)
            {
                if (horse.HasRider || horse.IsDown) continue;
                Vector3 offset = horse.transform.position - transform.position;
                offset.y = 0f;
                float distance = offset.sqrMagnitude;
                if (distance > bestDistance) continue;
                bestDistance = distance;
                best = horse;
            }
            return best;
        }
    }
}

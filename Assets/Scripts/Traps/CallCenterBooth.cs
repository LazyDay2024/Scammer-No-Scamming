using UnityEngine;

namespace ScammerNoScamming
{
    // TRAP_05. A roadside phone that keeps ringing with an unknown number. Ignoring it is safe.
    // Answering (E on foot, in range) springs it: a threatening voice rushes the player, everything
    // slows down for panicDuration seconds (the "tunnel vision" of a call-centre scam).
    public class CallCenterBooth : Trap
    {
        public float interactRange = 2.5f;
        public float ringRange = 25f;
        public float panicDuration = 8f;
        [Range(0.1f, 1f)] public float panicSpeedFactor = 0.5f;
        public AudioSource ringing;

        float panicEnd = -1f;
        PlayerController slowedPlayer;
        float baseWalk, baseSprint;
        HorseController slowedHorse;
        float baseTrot, baseGallop;

        void Awake()
        {
            if (ringing == null)
            {
                ringing = gameObject.AddComponent<AudioSource>();
                ringing.clip = MakeRing();
                ringing.loop = true;
                ringing.spatialBlend = 1f;
                ringing.maxDistance = ringRange;
                ringing.rolloffMode = AudioRolloffMode.Linear;
                ringing.playOnAwake = false;
            }
        }

        // Old-phone double ring: two short 440/480 Hz bursts then silence, generated so no audio file is needed.
        static AudioClip MakeRing()
        {
            const int rate = 22050;
            var data = new float[rate * 3];
            for (int i = 0; i < data.Length; i++)
            {
                float t = (float)i / rate;
                bool on = t < 0.4f || (t > 0.6f && t < 1.0f);
                data[i] = on ? 0.25f * (Mathf.Sin(2f * Mathf.PI * 440f * t) + Mathf.Sin(2f * Mathf.PI * 480f * t)) : 0f;
            }
            var clip = AudioClip.Create("Ring", data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public override void ResetTrap()
        {
            EndPanic();
            base.ResetTrap();
        }

        public override TrapKind Kind => TrapKind.CallCenter;
        protected override float DefaultDamage => 0f;
        public bool IsRinging { get; private set; }
        public bool IsPanicking => slowedPlayer != null;
        public float PanicRemaining => Mathf.Max(0f, panicEnd - Time.time);

        void LateUpdate()
        {
            var player = FindPlayer();
            if (player == null) return;

            Vector3 offset = player.transform.position - transform.position;
            offset.y = 0f;
            IsRinging = !HasSprung && offset.sqrMagnitude <= ringRange * ringRange;
            if (ringing != null)
            {
                if (IsRinging && !ringing.isPlaying) ringing.Play();
                else if (!IsRinging && ringing.isPlaying) ringing.Stop();
            }

            bool pressed = InteractPressedOnFoot(player);
            if (!HasSprung && pressed && offset.sqrMagnitude <= interactRange * interactRange)
                Spring(player.gameObject);

            if (IsPanicking && Time.time >= panicEnd) EndPanic();
        }

        protected override void OnSprung(GameObject target)
        {
            var player = target != null ? target.GetComponent<PlayerController>() : null;
            if (player == null) return;
            slowedPlayer = player;
            baseWalk = player.walkSpeed;
            baseSprint = player.sprintSpeed;
            player.walkSpeed *= panicSpeedFactor;
            player.sprintSpeed *= panicSpeedFactor;
            var mount = player.GetComponent<PlayerMount>();
            slowedHorse = mount != null && mount.CurrentHorse != null ? mount.CurrentHorse : NearestHorse(player.transform.position);
            if (slowedHorse != null)
            {
                baseTrot = slowedHorse.trotSpeed;
                baseGallop = slowedHorse.gallopSpeed;
                slowedHorse.trotSpeed *= panicSpeedFactor;
                slowedHorse.gallopSpeed *= panicSpeedFactor;
            }
            panicEnd = Time.time + panicDuration;
        }

        void EndPanic()
        {
            if (slowedPlayer != null)
            {
                slowedPlayer.walkSpeed = baseWalk;
                slowedPlayer.sprintSpeed = baseSprint;
            }
            slowedPlayer = null;
            if (slowedHorse != null)
            {
                slowedHorse.trotSpeed = baseTrot;
                slowedHorse.gallopSpeed = baseGallop;
            }
            slowedHorse = null;
        }

        static HorseController NearestHorse(Vector3 position)
        {
            HorseController best = null;
            float bestDistance = float.MaxValue;
            foreach (var horse in HorseController.All)
            {
                float d = (horse.transform.position - position).sqrMagnitude;
                if (d < bestDistance) { bestDistance = d; best = horse; }
            }
            return best;
        }

        void OnDisable() => EndPanic();

        void OnGUI()
        {
            if (!IsPanicking)
            {
                if (IsRinging && !GameScreen.AnyOpen && PlayerWithin(interactRange)) DrawPrompt("โทรศัพท์ดังไม่หยุด… กด E รับสาย");
                return;
            }
            // Tunnel vision: darken the edges and show the threat.
            float edge = Mathf.Min(Screen.width, Screen.height) * 0.18f;
            var dark = ThaiGui.Solid(new Color(0.25f, 0f, 0f, 0.55f));
            GUI.DrawTexture(new Rect(0, 0, Screen.width, edge), dark);
            GUI.DrawTexture(new Rect(0, Screen.height - edge, Screen.width, edge), dark);
            GUI.DrawTexture(new Rect(0, edge, edge, Screen.height - 2 * edge), dark);
            GUI.DrawTexture(new Rect(Screen.width - edge, edge, edge, Screen.height - 2 * edge), dark);
            var canvas = ThaiGui.BeginCanvas();
            GUI.Label(new Rect(0f, 220f, canvas.width, 120f), "“ห้ามวางสาย! คุณมีคดี โอนเงินมาตรวจสอบเดี๋ยวนี้!”",
                ThaiGui.Text(46, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter));
            ThaiGui.EndCanvas();
        }
    }
}

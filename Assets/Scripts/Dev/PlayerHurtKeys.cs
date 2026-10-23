using UnityEngine;
using UnityEngine.InputSystem;

namespace ScammerNoScamming
{
    // Sandbox helper: hurt or kill the player on demand, on foot or mounted, without waiting for an enemy.
    public class PlayerHurtKeys : MonoBehaviour
    {
        public Key hurtKey = Key.H;
        public Key killKey = Key.K;
        public float hurtAmount = 20f;

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            bool hurt = keyboard[hurtKey].wasPressedThisFrame;
            bool kill = keyboard[killKey].wasPressedThisFrame;
            if (!hurt && !kill) return;

            var player = FindFirstObjectByType<PlayerCharacter>();
            if (player == null || player.Health.IsDead) return;

            float amount = kill ? player.Health.Current : hurtAmount;
            player.Health.TakeDamage(new DamageInfo(amount, HitZoneType.Body, Faction.Scammer, player.transform.position));
        }

        void OnGUI()
        {
            GUI.Label(new Rect(12f, Screen.height - 54f, 500f, 24f), "[" + hurtKey + "] Hurt " + hurtAmount + "   [" + killKey + "] Kill player");
        }
    }
}

using UnityEngine;

namespace ScammerNoScamming
{
    // A real supply box: use it to refill the player's ammo. Place them outside and inside the station.
    public class SupplyBox : MonoBehaviour
    {
        public float range = 2.5f;

        void Update()
        {
            var user = Interaction.Player;
            if (user == null || user.Health.IsDead) return;

            if (!Interaction.Pressed(user, transform.position, range, "[E] Take supplies")) return;
            var gun = user.GetComponent<PlayerWeaponController>();
            if (gun != null) gun.Refill();
        }
    }
}

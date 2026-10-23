using UnityEngine;

namespace ScammerNoScamming
{
    // What the teammate's supply crates do to the player's ammo (owner's rules): a real crate refills it,
    // a fake one robs every round on top of its own damage and hidden scammers.
    public static class TrapConsequences
    {
        // Runs after Trap and SupplyCrate clear their static events for the play session.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Wire()
        {
            SupplyCrate.Collected += (crate, user) => Gun(user)?.Refill();
            Trap.Sprung += trap =>
            {
                if (trap is SupplyCrate crate && crate.isFake) Gun(Interaction.Player != null ? Interaction.Player.gameObject : null)?.Drain();
            };
        }

        static PlayerWeaponController Gun(GameObject user)
        {
            var gun = user != null ? user.GetComponentInChildren<PlayerWeaponController>() : null;
            return gun != null ? gun : null; // normalise a destroyed component to plain null for ?.
        }
    }
}

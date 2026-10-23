using UnityEngine;

namespace ScammerNoScamming
{
    // Something that waits for the player to do the wrong thing, springs once, and sets off its AmbushGroup.
    // Subclasses say what counts as the trigger. Every trap should show a warning sign the player can notice first.
    public abstract class Trap : MonoBehaviour
    {
        // Placeholder: how much Trouble this trap adds when sprung (TroubleMeter reads it via GameEvents.TrapSprung).
        public float troubleCost = 10f;

        public bool IsSprung { get; private set; }

        protected virtual void Update()
        {
            if (IsSprung) return;
            var player = Interaction.Player;
            if (player == null || player.Health.IsDead) return;
            if (!ShouldSpring(player)) return;

            IsSprung = true;
            Spring(player);
            GameEvents.RaiseTrapSprung(this);
        }

        protected abstract bool ShouldSpring(PlayerCharacter player);

        protected virtual void Spring(PlayerCharacter player)
        {
            var ambush = GetComponent<AmbushGroup>();
            if (ambush != null) ambush.Fire(player);
        }
    }
}

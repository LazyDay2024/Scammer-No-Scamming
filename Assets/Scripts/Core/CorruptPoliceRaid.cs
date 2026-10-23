using UnityEngine;

namespace ScammerNoScamming
{
    // When Trouble fills up, corrupt police ride in on the player. Not a loss, just a lot more danger.
    // Uses the AmbushGroup next to it for where and how many; the riders are switched to the police side.
    [RequireComponent(typeof(AmbushGroup))]
    public class CorruptPoliceRaid : MonoBehaviour
    {
        public bool HasRaided { get; private set; }

        void OnEnable()
        {
            GameEvents.TroubleFull += Raid;
        }

        void OnDisable()
        {
            GameEvents.TroubleFull -= Raid;
        }

        void Raid()
        {
            var player = Interaction.Player;
            if (player == null || player.Health.IsDead) return;

            HasRaided = true;
            foreach (var enemy in GetComponent<AmbushGroup>().Fire(player))
                enemy.SetFaction(Faction.CorruptPolice);
        }
    }
}

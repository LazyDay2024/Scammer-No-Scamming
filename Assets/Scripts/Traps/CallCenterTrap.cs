using UnityEngine;

namespace ScammerNoScamming
{
    // Ordinary-looking people stand in front of a call-center building. When the player comes close
    // they drop the act: the decoys vanish and armed scammers appear where they stood.
    // Put the decoys in both this list and the AmbushGroup points.
    public class CallCenterTrap : ProximityTrap
    {
        public Transform[] decoys;

        protected override void Spring(PlayerCharacter player)
        {
            base.Spring(player);
            if (decoys == null) return;
            foreach (var decoy in decoys) decoy.gameObject.SetActive(false);
        }
    }
}

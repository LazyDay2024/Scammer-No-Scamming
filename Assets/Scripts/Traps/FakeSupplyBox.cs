namespace ScammerNoScamming
{
    // A "free supplies" box that robs the player of all their ammo and sends scammers after them.
    // Warning signs: too-good offer, wrong place, no official marking. A real SupplyBox says nothing special.
    public class FakeSupplyBox : InteractTrap
    {
        protected override string Prompt => "[E] FREE supplies - last box, take it now!";

        protected override void Spring(PlayerCharacter player)
        {
            var gun = player.GetComponent<PlayerWeaponController>();
            if (gun != null) gun.Drain();
            base.Spring(player);
        }
    }
}

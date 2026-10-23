namespace ScammerNoScamming
{
    // A trap sprung by pressing the use key next to it, dressed up as something worth using.
    public abstract class InteractTrap : Trap
    {
        public float range = 2.5f;

        protected abstract string Prompt { get; }

        protected override bool ShouldSpring(PlayerCharacter player)
        {
            return Interaction.Pressed(player, transform.position, range, Prompt);
        }
    }
}

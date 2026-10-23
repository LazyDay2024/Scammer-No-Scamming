namespace ScammerNoScamming
{
    // A scam victim hiding in a corner of a building, already crouched in fear.
    public class Victim : Civilian
    {
        protected override Faction Side => Faction.Victim;

        protected override void Awake()
        {
            base.Awake();
            Cower();
        }
    }
}

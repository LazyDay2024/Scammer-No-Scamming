namespace ScammerNoScamming
{
    // Someone forced to work in a scam hideout. Stands at their post until shooting starts nearby, then cowers.
    public class ForcedWorker : Civilian
    {
        protected override Faction Side => Faction.ForcedWorker;
    }
}

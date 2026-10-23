namespace ScammerNoScamming
{
    // The link on a phishing message: opening it calls riders in from all around.
    // Warning signs: urgent prize wording, odd address.
    public class LinkTrap : InteractTrap
    {
        protected override string Prompt => "[E] Open link: YOU WON! claim now at s1am-prize.example";
    }
}

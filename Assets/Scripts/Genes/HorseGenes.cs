using UnityEngine;

namespace ScammerNoScamming
{
    // Scales HorseController's gait from the values it had at Awake, so applying a new loadout
    // (for example on a retry) never compounds the previous one.
    [RequireComponent(typeof(HorseController))]
    public class HorseGenes : MonoBehaviour
    {
        HorseController horse;
        float baseTrot;
        float baseGallop;
        float baseTurn;

        void Awake()
        {
            CaptureBase();
        }

        void CaptureBase()
        {
            if (horse != null) return;
            horse = GetComponent<HorseController>();
            baseTrot = horse.trotSpeed;
            baseGallop = horse.gallopSpeed;
            baseTurn = horse.turnRate;
        }

        public void Apply(RunModifiers modifiers)
        {
            CaptureBase();
            if (modifiers == null) modifiers = RunModifiers.None;
            horse.trotSpeed = baseTrot * modifiers.HorseTrotSpeed;
            horse.gallopSpeed = baseGallop * modifiers.HorseGallopSpeed;
            horse.turnRate = baseTurn * modifiers.HorseTurnRate;
        }
    }
}

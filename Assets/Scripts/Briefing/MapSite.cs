using System.Collections.Generic;
using UnityEngine;

namespace ScammerNoScamming
{
    // A place on the city map the player can pick as the next raid. The id matches a case's HideoutId.
    // Only the site a case points at is a real hideout; the others are decoys: picking one brings scammers
    // to the station instead.
    public class MapSite : MonoBehaviour
    {
        public static readonly List<MapSite> All = new List<MapSite>();

        public string siteId;
        public string displayName;
        public ScamType scamType;

        public string ScamLabel => Label(scamType);

        public static string Label(ScamType type)
        {
            switch (type)
            {
                case ScamType.OnlineShopping: return "แก๊งร้านค้าออนไลน์ปลอม";
                case ScamType.InvestmentOrJob:
                case ScamType.Investment: return "แก๊งหลอกลงทุน / งานออนไลน์";
                case ScamType.FakeOfficial: return "แก๊งแอบอ้างเป็นเจ้าหน้าที่";
                case ScamType.PhishingLink: return "แก๊งส่งลิงก์ปลอม";
                default: return "แก๊งโรแมนซ์สแกม";
            }
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetRegistry() => All.Clear();
    }
}

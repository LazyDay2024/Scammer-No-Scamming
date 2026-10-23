using System;
using UnityEngine;

namespace ScammerNoScamming
{
    // Materials the city generator uses. Any field can stay null (the PlayMode tests do that).
    public class CityMaterials
    {
        public Material TowerA;
        public Material TowerB;
        public Material Mid;
        public Material Roof;
        public Material Ground;

        // Returns a flat-colour material for a palette colour.
        public Func<Color, Material> Flat;

        public Material Paint(Color color)
        {
            return Flat != null ? Flat(color) : null;
        }
    }
}

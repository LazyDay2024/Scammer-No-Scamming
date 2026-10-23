using UnityEngine;

namespace ScammerNoScamming
{
    public enum CityZone
    {
        Tower,
        Shophouse,
        House
    }

    public enum CityDirection
    {
        North,
        East,
        South,
        West
    }

    // Item 10. Positions inside the ring city by zone and compass direction, matching how the case clues
    // describe hideouts ("โซนตึกแถว ทิศตะวันออก"). Radii follow CityBuilder's rings; north is +Z.
    // The point sits beside the radial road of that direction, so a rider following the road finds it.
    public static class CityLayoutPoints
    {
        public static float ZoneRadius(CityZone zone)
        {
            switch (zone)
            {
                case CityZone.Tower: return (CityBuilder.PlazaRadius + CityBuilder.InnerRingRoad) * 0.5f;
                case CityZone.Shophouse: return (CityBuilder.InnerRingRoad + CityBuilder.ViaductRadius) * 0.5f;
                default: return (CityBuilder.ViaductRadius + CityBuilder.WallRadius) * 0.5f;
            }
        }

        public static Vector3 Direction(CityDirection direction)
        {
            switch (direction)
            {
                case CityDirection.North: return Vector3.forward;
                case CityDirection.East: return Vector3.right;
                case CityDirection.South: return Vector3.back;
                default: return Vector3.left;
            }
        }

        public static Vector3 Point(CityZone zone, CityDirection direction, float roadsideOffset = 9f)
        {
            Vector3 dir = Direction(direction);
            Vector3 side = Vector3.Cross(Vector3.up, dir);
            return dir * ZoneRadius(zone) + side * roadsideOffset;
        }

        public static string Describe(CityZone zone, CityDirection direction)
        {
            string z = zone == CityZone.Tower ? "โซนตึกสูง" : zone == CityZone.Shophouse ? "โซนตึกแถว" : "โซนบ้าน";
            string d = direction == CityDirection.North ? "ทิศเหนือ" : direction == CityDirection.East ? "ทิศตะวันออก"
                : direction == CityDirection.South ? "ทิศใต้" : "ทิศตะวันตก";
            return z + " " + d;
        }
    }
}

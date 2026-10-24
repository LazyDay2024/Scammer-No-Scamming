using System.Collections.Generic;
using UnityEngine;

namespace HorseCity.EditorTools
{
    /// <summary>
    /// Collects small props (poles, wires, lamps, trees, road paint) into a few large meshes
    /// per map chunk, so thousands of little objects cost a handful of draw calls and can still be
    /// frustum culled chunk by chunk.
    /// </summary>
    public class PropBatcher
    {
        public enum Kind { Solid, Detail }

        public const int Sectors = 16;
        readonly float[] _bandEdges;
        readonly Dictionary<(Kind, int, int), Part> _parts = new Dictionary<(Kind, int, int), Part>();

        public PropBatcher(params float[] bandEdges) { _bandEdges = bandEdges; }

        public IEnumerable<KeyValuePair<(Kind kind, int band, int sector), Part>> All => _parts;

        public Part Get(Vector3 worldPos, Kind kind)
        {
            float r = new Vector2(worldPos.x, worldPos.z).magnitude;
            int band = 0;
            while (band < _bandEdges.Length && r > _bandEdges[band]) band++;
            float ang = Mathf.Repeat(Mathf.Atan2(worldPos.z, worldPos.x) * Mathf.Rad2Deg, 360f);
            int sector = band == 0 ? 0 : Mathf.Clamp((int)(ang / (360f / Sectors)), 0, Sectors - 1);

            // split a chunk once it gets big so meshes stay friendly to 16-bit indices and culling
            var key = (kind, band, sector);
            if (_parts.TryGetValue(key, out Part p) && p.kit.VertexCount > 60000)
            {
                int extra = 1;
                while (_parts.TryGetValue((kind, band, sector + 1000 * extra), out p) && p.kit.VertexCount > 60000) extra++;
                key = (kind, band, sector + 1000 * extra);
            }
            if (!_parts.TryGetValue(key, out p))
            {
                p = new Part();
                _parts[key] = p;
            }
            return p;
        }
    }
}

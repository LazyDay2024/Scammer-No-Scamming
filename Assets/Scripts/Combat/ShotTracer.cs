using UnityEngine;

namespace ScammerNoScamming
{
    // Flashes a line along every shot fired by the Weapon on the same object, so the player can see where enemy fire comes from.
    public class ShotTracer : MonoBehaviour
    {
        // Null falls back to a plain unlit material in this colour.
        public Material material;
        public Color color = new Color(1f, 0.35f, 0.25f);
        public float duration = 0.06f;
        // When set, the line starts here (a gun's muzzle) instead of at the shot's own origin.
        public Transform muzzle;

        Weapon weapon;
        LineRenderer line;
        float hideTime;

        void Awake()
        {
            weapon = GetComponent<Weapon>();
            if (weapon == null)
            {
                Debug.LogError("ShotTracer needs a Weapon on the same object", this);
                enabled = false;
                return;
            }
            weapon.Fired += Show;

            var lineObject = new GameObject("ShotTracer");
            lineObject.transform.SetParent(transform, false);
            line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = 0.05f;
            line.endWidth = 0.02f;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.enabled = false;

            var mat = material;
            if (mat == null)
            {
                var shader = Shader.Find("Sprites/Default");
                if (shader != null) mat = new Material(shader) { color = color };
            }
            line.sharedMaterial = mat;
        }

        void OnDestroy()
        {
            if (weapon != null) weapon.Fired -= Show;
        }

        void Update()
        {
            if (line.enabled && Time.time >= hideTime) line.enabled = false;
        }

        void Show(ShotResult shot)
        {
            line.SetPosition(0, muzzle != null ? muzzle.position : shot.Origin);
            line.SetPosition(1, shot.EndPoint);
            line.enabled = true;
            hideTime = Time.time + duration;
        }
    }
}

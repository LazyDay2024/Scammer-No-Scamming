using UnityEngine;

namespace HorseCity
{
    /// <summary>
    /// Turns a building's interior lights on only while the camera is inside or near it.
    /// Interiors stay dark (and free) when the player is riding around outside.
    /// </summary>
    [DisallowMultipleComponent]
    public class InteriorZone : MonoBehaviour
    {
        [Tooltip("Local-space box that covers the interior.")]
        public Bounds localBounds = new Bounds(Vector3.zero, new Vector3(20f, 6f, 20f));

        [Tooltip("Extra distance around the box at which the lights switch on.")]
        public float margin = 12f;

        public Light[] interiorLights = new Light[0];

        bool _on = true;
        float _timer;

        void Start() { SetLights(false); }

        void Update()
        {
            _timer -= Time.unscaledDeltaTime;
            if (_timer > 0f) return;
            _timer = 0.2f;

            Camera cam = Camera.main;
            if (cam == null) return;
            Vector3 local = transform.InverseTransformPoint(cam.transform.position);
            Bounds b = localBounds;
            b.Expand(margin * 2f);
            SetLights(b.Contains(local));
        }

        void SetLights(bool on)
        {
            if (on == _on) return;
            _on = on;
            foreach (Light l in interiorLights)
                if (l) l.enabled = on;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.35f);
            Gizmos.DrawWireCube(localBounds.center, localBounds.size);
        }
    }
}

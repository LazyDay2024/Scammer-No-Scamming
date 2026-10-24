using System.Collections.Generic;
using UnityEngine;

namespace HorseCity
{
    /// <summary>
    /// Keeps only the street lamps closest to the camera lit with real-time point lights.
    /// Every other lamp still glows through its emissive bulb, so the street reads as lit
    /// while the GPU only pays for a handful of lights. Built for low to mid end PCs.
    /// </summary>
    [DisallowMultipleComponent]
    public class CityLightCuller : MonoBehaviour
    {
        [Tooltip("Lamps further than this from the camera never get a real light.")]
        public float maxDistance = 70f;

        [Tooltip("Upper limit of real-time street lights on at the same time.")]
        public int maxActiveLights = 12;

        [Tooltip("Seconds between re-sorting the lamps.")]
        public float refreshInterval = 0.25f;

        [Tooltip("Filled by the city builder. Leave empty to collect child lights on start.")]
        public List<Light> lights = new List<Light>();

        readonly List<int> _order = new List<int>();
        float[] _dist;
        System.Comparison<int> _byDistance;
        float _timer;
        Camera _cam;

        void Start()
        {
            if (lights.Count == 0)
                GetComponentsInChildren(true, lights);
            _dist = new float[lights.Count];
            _byDistance = (a, b) => _dist[a].CompareTo(_dist[b]);
            for (int i = 0; i < lights.Count; i++)
            {
                _order.Add(i);
                if (lights[i]) lights[i].enabled = false;
            }
            Refresh();
        }

        void Update()
        {
            _timer -= Time.unscaledDeltaTime;
            if (_timer > 0f) return;
            _timer = refreshInterval;
            Refresh();
        }

        void Refresh()
        {
            if (_cam == null || !_cam.isActiveAndEnabled) _cam = Camera.main;
            if (_cam == null || lights.Count == 0) return;

            Vector3 camPos = _cam.transform.position;
            float maxSqr = maxDistance * maxDistance;
            for (int i = 0; i < lights.Count; i++)
                _dist[i] = lights[i] ? (lights[i].transform.position - camPos).sqrMagnitude : float.MaxValue;

            _order.Sort(_byDistance);
            for (int rank = 0; rank < _order.Count; rank++)
            {
                int i = _order[rank];
                if (!lights[i]) continue;
                bool on = rank < maxActiveLights && _dist[i] < maxSqr;
                if (lights[i].enabled != on) lights[i].enabled = on;
            }
        }
    }
}

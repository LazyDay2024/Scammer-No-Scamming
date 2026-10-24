using System.Collections.Generic;
using UnityEngine;

namespace HorseCity
{
    /// <summary>
    /// Runs the BTS skytrain around its loop. Sits on "Skytrain_Train_Pivot" (the cars are its
    /// children), turns the pivot around the city centre, and stops at each station for a while.
    /// Added automatically at play time if the scene has a Skytrain_Train_Pivot, so no setup needed.
    /// Cost: one transform rotation per frame.
    /// </summary>
    [DisallowMultipleComponent]
    public class SkytrainRunner : MonoBehaviour
    {
        [Tooltip("Cruising speed in metres per second (15 = 54 km/h).")]
        public float cruiseSpeed = 15f;

        [Tooltip("Acceleration and braking in m/s².")]
        public float acceleration = 1.2f;

        [Tooltip("Seconds the train waits at each station.")]
        public float dwellTime = 10f;

        [Tooltip("Run clockwise (seen from above). Untick to run the other way.")]
        public bool clockwise = true;

        [Tooltip("Station centres as angles around the pivot (degrees, 0 = +X axis). Filled from Skytrain_Station_* objects if they exist.")]
        public List<float> stationAngles = new List<float> { 0f, 180f };

        readonly List<Transform> _cars = new List<Transform>();
        float _radius = 178.5f;
        float _speed;
        float _dwell;
        bool _leaving;
        float _leftDeg;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoAttach()
        {
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                if (t.name.StartsWith("Skytrain_Train_Pivot") && !t.GetComponent<SkytrainRunner>())
                    t.gameObject.AddComponent<SkytrainRunner>();
        }

        void Start()
        {
            foreach (Transform c in transform)
                if (c.name.StartsWith("Skytrain_Car")) _cars.Add(c);
            if (_cars.Count == 0)
            {
                Debug.LogWarning("SkytrainRunner: no Skytrain_Car children found under " + name);
                enabled = false;
                return;
            }

            Vector3 centre = CarCentre();
            _radius = Mathf.Max(1f, new Vector2(centre.x - transform.position.x, centre.z - transform.position.z).magnitude);

            // Use real station objects when the scene has them (newer exports keep them separate).
            var found = new List<float>();
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (!t.name.StartsWith("Skytrain_Station_") || t.name.Contains("Stairs")) continue;
                Vector3 d = t.position - transform.position;
                found.Add(Mathf.Atan2(d.z, d.x) * Mathf.Rad2Deg);
            }
            if (found.Count > 0) stationAngles = found;

            _speed = cruiseSpeed;
        }

        Vector3 CarCentre()
        {
            Vector3 sum = Vector3.zero;
            foreach (var c in _cars) sum += c.position;
            return sum / _cars.Count;
        }

        float CentreAngle()
        {
            Vector3 d = CarCentre() - transform.position;
            return Mathf.Atan2(d.z, d.x) * Mathf.Rad2Deg;
        }

        // Degrees still to travel (in the running direction) before the train centre reaches the next station.
        float DegreesToNextStation()
        {
            float here = CentreAngle();
            float best = 360f;
            foreach (float s in stationAngles)
            {
                float ahead = clockwise ? here - s : s - here;
                ahead = Mathf.Repeat(ahead, 360f);
                if (_leaving && ahead < 1f) continue;     // the station we are pulling out of
                if (ahead < best) best = ahead;
            }
            return best;
        }

        void Update()
        {
            float dt = Time.deltaTime;

            if (_dwell > 0f)
            {
                _dwell -= dt;
                if (_dwell <= 0f) { _leaving = true; _leftDeg = 0f; }
                return;
            }

            float distM = DegreesToNextStation() * Mathf.Deg2Rad * _radius;

            float brakeDist = (_speed * _speed) / (2f * acceleration);
            if (distM <= brakeDist + 0.05f)
                _speed = Mathf.Max(0.8f, _speed - acceleration * dt);
            else
                _speed = Mathf.Min(cruiseSpeed, _speed + acceleration * dt);

            float step = _speed * dt;
            if (step >= distM)
            {
                step = distM;
                _speed = 0f;
                _dwell = dwellTime;
                _leftDeg = 0f;
            }

            float deg = step / _radius * Mathf.Rad2Deg;
            transform.Rotate(Vector3.up, clockwise ? deg : -deg, Space.World);

            if (_leaving) { _leftDeg += deg; if (_leftDeg > 2f) _leaving = false; }
        }
    }
}

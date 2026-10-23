using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ScammerNoScamming
{
    // Live 3D backdrop for the menu and story screens: a second camera that slowly drifts across the plaza in front of the
    // police station, so the real city at golden hour sits behind the UI instead of a painted picture.
    // Runs on unscaled time because the game is paused while a screen is open.
    public class ScreenBackdrop : MonoBehaviour
    {
        // Tuning (world units / seconds). The subject sits on the right so the left stays free for text.
        public float radius = 66f;
        public float height = 25f;
        public float swing = 0.17f;
        public float swingSpeed = 0.045f;
        public float startAngle = 2.9f;
        public float lookHeight = 8f;
        public float subjectShift = 17f;

        static ScreenBackdrop instance;
        static readonly HashSet<object> holders = new HashSet<object>();

        Camera cam;
        Transform focus;
        float startTime;

        public static bool IsLive => instance != null && instance.cam != null && instance.cam.enabled;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            instance = null;
            holders.Clear();
        }

        public static void Hold(object owner)
        {
            holders.Add(owner);
            if (instance == null) Create();
            if (instance != null) instance.Activate();
        }

        public static void Release(object owner)
        {
            if (!holders.Remove(owner) || holders.Count > 0 || instance == null) return;
            instance.Deactivate();
        }

        static void Create()
        {
            var source = FindSourceCamera();
            if (source == null) return;
            var go = new GameObject("ScreenBackdropCamera");
            var made = go.AddComponent<Camera>();
            made.CopyFrom(source);
            made.depth = source.depth + 10f;
            made.fieldOfView = 42f;
            made.nearClipPlane = 0.3f;
            var data = go.GetComponent<UniversalAdditionalCameraData>();
            var sourceData = source.GetComponent<UniversalAdditionalCameraData>();
            if (data == null) data = go.AddComponent<UniversalAdditionalCameraData>();
            if (sourceData != null)
            {
                data.renderPostProcessing = sourceData.renderPostProcessing;
                data.antialiasing = sourceData.antialiasing;
            }
            go.SetActive(false);
            instance = go.AddComponent<ScreenBackdrop>();
            instance.cam = made;
        }

        static Camera FindSourceCamera()
        {
            var player = PlayerLocator.Player;
            var cam = player != null ? player.GetComponentInChildren<Camera>(true) : null;
            return cam != null ? cam : Camera.main;
        }

        void Activate()
        {
            if (gameObject.activeSelf) return;
            var station = GameObject.Find("PoliceStation");
            var player = PlayerLocator.Player;
            if (station != null) focus = station.transform;
            else if (player != null) focus = player.transform;
            startTime = Time.unscaledTime;
            Place(0f);
            gameObject.SetActive(true);
        }

        void Deactivate() => gameObject.SetActive(false);

        void LateUpdate() => Place(Time.unscaledTime - startTime);

        void Place(float t)
        {
            if (focus == null) return;
            Vector3 center = focus.position;
            float angle = startAngle + Mathf.Sin(t * swingSpeed) * swing;
            float r = radius + Mathf.Sin(t * 0.11f) * 3f;
            float h = height + Mathf.Sin(t * 0.08f + 1f) * 1.2f;
            Vector3 offset = new Vector3(Mathf.Sin(angle) * r, h, Mathf.Cos(angle) * r);
            Vector3 target = center + Vector3.up * lookHeight;
            Vector3 desired = center + offset;
            Quaternion look = Quaternion.LookRotation(target - desired, Vector3.up);
            // Aim a little to the side of the subject so it lands on the right of the frame.
            Vector3 aim = target - look * Vector3.right * subjectShift;
            transform.SetPositionAndRotation(desired, Quaternion.LookRotation(aim - desired, Vector3.up));
        }
    }
}

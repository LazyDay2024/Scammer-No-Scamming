using UnityEngine;

namespace ScammerNoScamming
{
    [DisallowMultipleComponent]
    public sealed class PlayerFeelAudio : MonoBehaviour
    {
        CharacterController body;
        PlayerMount mount;
        AudioSource steps, wind;
        float distance;
        int foot;

        void Awake()
        {
            body = GetComponent<CharacterController>();
            mount = GetComponent<PlayerMount>();
            steps = ProceduralFeelAudio.Source(gameObject, true, 12f);
            wind = ProceduralFeelAudio.Source(gameObject, false);
            wind.clip = ProceduralFeelAudio.Get(FeelSound.Wind);
            wind.loop = true;
            wind.volume = 0.18f;
        }

        void OnEnable() { if (wind != null) wind.Play(); }
        void OnDisable()
        {
            if (wind != null) wind.Stop();
            if (steps != null) steps.Stop();
        }

        void Update()
        {
            if (GameScreen.AnyOpen || Time.deltaTime <= 0f || body == null) return;
            bool mounted = mount != null && mount.IsMounted;
            float speed = body.enabled ? Vector3.ProjectOnPlane(body.velocity, Vector3.up).magnitude : 0f;
            wind.volume = Mathf.Lerp(wind.volume, mounted ? 0.3f : 0.18f, 1f - Mathf.Exp(-2f * Time.deltaTime));
            if (mounted || !body.enabled || !body.isGrounded || speed < 0.2f) { distance = 0f; return; }
            distance += speed * Time.deltaTime;
            if (distance < 1.65f) return;
            distance %= 1.65f;
            steps.pitch = foot++ % 2 == 0 ? 0.95f : 1.05f;
            steps.PlayOneShot(ProceduralFeelAudio.Get(FeelSound.Footstep), 0.22f);
        }
    }
}

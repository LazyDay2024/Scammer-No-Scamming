using UnityEngine;

namespace ScammerNoScamming
{
    [DisallowMultipleComponent, RequireComponent(typeof(HorseController))]
    public sealed class HorseFeelAudio : MonoBehaviour
    {
        HorseController horse;
        CharacterController body;
        AudioSource hooves, breath;
        float beat, nextSnort;
        int hoof;

        void Awake()
        {
            horse = GetComponent<HorseController>();
            body = GetComponent<CharacterController>();
            hooves = ProceduralFeelAudio.Source(gameObject, true, 40f);
            breath = ProceduralFeelAudio.Source(gameObject, true, 18f);
            nextSnort = Time.time + Random.Range(8f, 15f);
        }

        void Update()
        {
            if (GameScreen.AnyOpen || Time.deltaTime <= 0f) return;
            float speed = Mathf.Abs(horse.Speed);
            if (body != null && body.isGrounded && speed > 0.3f)
            {
                float gait = Mathf.InverseLerp(0f, horse.gallopSpeed, speed);
                beat += Time.deltaTime * Mathf.Lerp(1.8f, 6.2f, gait);
                if (beat >= 1f)
                {
                    beat %= 1f;
                    hooves.pitch = (hoof++ % 2 == 0 ? 0.92f : 1.08f) + Random.Range(-0.03f, 0.03f);
                    hooves.PlayOneShot(ProceduralFeelAudio.Get(FeelSound.Hoof), Mathf.Lerp(0.18f, 0.4f, gait));
                }
            }
            else beat = 0f;
            if (Time.time < nextSnort) return;
            nextSnort = Time.time + Random.Range(12f, 24f);
            breath.pitch = Random.Range(0.9f, 1.05f);
            breath.PlayOneShot(ProceduralFeelAudio.Get(FeelSound.Snort), 0.18f);
        }

        void OnDisable()
        {
            if (hooves != null) hooves.Stop();
            if (breath != null) breath.Stop();
        }
    }
}

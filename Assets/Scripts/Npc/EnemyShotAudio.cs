using UnityEngine;

namespace ScammerNoScamming
{
    [DisallowMultipleComponent]
    public sealed class EnemyShotAudio : MonoBehaviour
    {
        Weapon weapon;
        AudioSource source;
        void Awake()
        {
            weapon = GetComponent<Weapon>();
            source = ProceduralFeelAudio.Source(gameObject, true, 90f);
        }
        void OnEnable() { if (weapon != null) weapon.Fired += OnShot; }
        void OnShot(ShotResult shot)
        {
            source.pitch = Random.Range(0.94f, 1.03f);
            source.PlayOneShot(ProceduralFeelAudio.Get(FeelSound.Shot), 0.42f);
        }
        void OnDisable()
        {
            if (weapon != null) weapon.Fired -= OnShot;
            if (source != null) source.Stop();
        }
    }
}

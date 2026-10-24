using UnityEngine;

namespace HorseCity
{
    /// <summary>
    /// Makes a standing horse look alive: starts the idle loop at a random point and speed so
    /// horses don't move in sync, and now and then plays a small fidget (tail swish, head nod,
    /// snort, head shake) on the additive "Fidget" layer of the HorseIdle controller.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public class HorseIdle : MonoBehaviour
    {
        public Vector2 speedRange = new Vector2(0.85f, 1.1f);
        public Vector2 fidgetEvery = new Vector2(4f, 10f);
        public string[] fidgets = { "TailWag", "TailFlick", "HeadNod", "Snort", "HeadShake" };

        Animator _anim;
        float _next;

        void Start()
        {
            _anim = GetComponent<Animator>();
            _anim.applyRootMotion = false;
            _anim.speed = Random.Range(speedRange.x, speedRange.y);
            _anim.Play("Idle", 0, Random.value);
            _next = Time.time + Random.Range(fidgetEvery.x, fidgetEvery.y);
        }

        void Update()
        {
            if (Time.time < _next || _anim.layerCount < 2) return;
            _next = Time.time + Random.Range(fidgetEvery.x, fidgetEvery.y);
            _anim.CrossFadeInFixedTime(fidgets[Random.Range(0, fidgets.Length)], 0.3f, 1);
        }
    }
}

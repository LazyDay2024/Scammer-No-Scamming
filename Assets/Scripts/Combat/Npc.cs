using System.Collections;
using UnityEngine;

namespace ScammerNoScamming
{
    // A non-player character: runs its own Tick while alive, then falls over, lingers and sinks away when it dies.
    public abstract class Npc : Character
    {
        [Header("Death")]
        public float fallDuration = 0.4f;
        public float lingerTime = 3f;
        public float sinkDuration = 1.5f;
        public float sinkDepth = 1.2f;

        protected virtual void Update()
        {
            if (!Health.IsDead) Tick();
        }

        protected abstract void Tick();

        protected override void OnDied(Health health)
        {
            // A body must not block or soak up shots.
            foreach (var body in GetComponentsInChildren<Collider>()) body.enabled = false;
            StartCoroutine(Collapse(transform));
        }

        protected float CollapseTime => fallDuration + lingerTime + sinkDuration;

        // Falls over, lies there, sinks into the floor, then removes the body.
        // shortcut: sinks instead of an alpha fade, because the placeholder materials are opaque; switch once real materials exist.
        protected IEnumerator Collapse(Transform body)
        {
            Quaternion from = body.rotation;
            Quaternion to = from * Quaternion.Euler(-90f, 0f, 0f);
            for (float t = 0f; t < fallDuration; t += Time.deltaTime)
            {
                body.rotation = Quaternion.Slerp(from, to, t / fallDuration);
                yield return null;
            }
            body.rotation = to;

            yield return new WaitForSeconds(lingerTime);

            Vector3 start = body.position;
            for (float t = 0f; t < sinkDuration; t += Time.deltaTime)
            {
                body.position = start + Vector3.down * (sinkDepth * t / sinkDuration);
                yield return null;
            }
            Destroy(body.gameObject);
        }
    }
}

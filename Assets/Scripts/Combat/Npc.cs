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

        // Seconds the body spends playing its own death animation. Above zero the body is left upright for the
        // animation to lay down; at zero Collapse tips it over instead.
        protected virtual float DeathAnimationTime => 0f;

        protected override void OnDied(Health health)
        {
            // A body must not block or soak up shots.
            foreach (var body in GetComponentsInChildren<Collider>()) body.enabled = false;
            StartCoroutine(Collapse(transform, DeathAnimationTime));
        }

        protected float CollapseTime => (DeathAnimationTime > 0f ? DeathAnimationTime : fallDuration) + lingerTime + sinkDuration;

        // Falls over (or waits out its death animation), lies there, sinks into the floor, then removes the body.
        // shortcut: sinks instead of an alpha fade, because the placeholder materials are opaque; switch once real materials exist.
        // A rollDegrees other than 0 rolls the body onto its side instead of onto its back, and lift raises it so the flank rests on the floor.
        protected IEnumerator Collapse(Transform body, float animationTime = 0f, float rollDegrees = 0f, float lift = 0f)
        {
            if (animationTime > 0f)
            {
                yield return new WaitForSeconds(animationTime);
            }
            else
            {
                Quaternion from = body.rotation;
                Quaternion to = from * (rollDegrees != 0f ? Quaternion.Euler(0f, 0f, rollDegrees) : Quaternion.Euler(-90f, 0f, 0f));
                Vector3 startPosition = body.position;
                for (float t = 0f; t < fallDuration; t += Time.deltaTime)
                {
                    float k = t / fallDuration;
                    body.rotation = Quaternion.Slerp(from, to, k);
                    body.position = startPosition + Vector3.up * (lift * k);
                    yield return null;
                }
                body.rotation = to;
                body.position = startPosition + Vector3.up * lift;
            }

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

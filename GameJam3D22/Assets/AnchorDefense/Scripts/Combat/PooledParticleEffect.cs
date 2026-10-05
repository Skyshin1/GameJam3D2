using System;
using UnityEngine;

namespace AnchorDefense
{
    public sealed class PooledParticleEffect : MonoBehaviour, IPoolable
    {
        [SerializeField] private ParticleSystem particles;
        private Action<PooledParticleEffect> releaseAction;
        private float remaining;
        private bool unscaledPlayback;

        public void UseUnscaledPlayback(bool value)
        {
            unscaledPlayback = value;
            foreach (ParticleSystem system in GetComponentsInChildren<ParticleSystem>(true))
            { var main = system.main; main.useUnscaledTime = value; }
        }

        public void Configure(ParticleSystem particleSystem)
        {
            particles = particleSystem;
        }

        public void PlayBurst(Vector3 position, Color color, int count, float duration, Action<PooledParticleEffect> onRelease)
        {
            PlayBurst(position, color, count, duration, 0.04f, 0.12f, 1.2f, 3.5f, onRelease);
        }

        public void PlayBurst(
            Vector3 position,
            Color color,
            int count,
            float duration,
            float minimumSize,
            float maximumSize,
            float minimumSpeed,
            float maximumSpeed,
            Action<PooledParticleEffect> onRelease)
        {
            transform.position = position;
            releaseAction = onRelease;
            remaining = duration;
            if (particles == null)
            {
                Debug.LogError($"{name} cannot play because its Particle System reference is missing.", this);
                remaining = 0f;
                releaseAction?.Invoke(this);
                return;
            }
            particles.Clear(true);

            float safeMinSize = Mathf.Max(0.01f, Mathf.Min(minimumSize, maximumSize));
            float safeMaxSize = Mathf.Max(safeMinSize, Mathf.Max(minimumSize, maximumSize));
            float safeMinSpeed = Mathf.Max(0f, Mathf.Min(minimumSpeed, maximumSpeed));
            float safeMaxSpeed = Mathf.Max(safeMinSpeed, Mathf.Max(minimumSpeed, maximumSpeed));

            for (int i = 0; i < count; i++)
            {
                var emitParams = new ParticleSystem.EmitParams
                {
                    startColor = color,
                    startSize = UnityEngine.Random.Range(safeMinSize, safeMaxSize),
                    velocity = UnityEngine.Random.onUnitSphere * UnityEngine.Random.Range(safeMinSpeed, safeMaxSpeed)
                };
                particles.Emit(emitParams, 1);
            }
        }

        public void OnTakenFromPool()
        {
            remaining = 0f;
        }

        public void OnReturnedToPool()
        {
            UseUnscaledPlayback(false);
            particles?.Clear(true);
            releaseAction = null;
            remaining = 0f;
        }

        private void Update()
        {
            if (remaining <= 0f)
            {
                return;
            }

            remaining -= unscaledPlayback ? Time.unscaledDeltaTime : Time.deltaTime;
            if (remaining <= 0f)
            {
                releaseAction?.Invoke(this);
            }
        }
    }
}

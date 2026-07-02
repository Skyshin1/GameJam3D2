using System;
using UnityEngine;

namespace AnchorDefense
{
    public sealed class PooledParticleEffect : MonoBehaviour, IPoolable
    {
        [SerializeField] private ParticleSystem particles;
        private Action<PooledParticleEffect> releaseAction;
        private float remaining;

        public void Configure(ParticleSystem particleSystem)
        {
            particles = particleSystem;
        }

        public void PlayBurst(Vector3 position, Color color, int count, float duration, Action<PooledParticleEffect> onRelease)
        {
            transform.position = position;
            releaseAction = onRelease;
            remaining = duration;
            particles.Clear(true);

            for (int i = 0; i < count; i++)
            {
                var emitParams = new ParticleSystem.EmitParams
                {
                    startColor = color,
                    startSize = UnityEngine.Random.Range(0.04f, 0.12f),
                    velocity = UnityEngine.Random.onUnitSphere * UnityEngine.Random.Range(1.2f, 3.5f)
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

            remaining -= Time.deltaTime;
            if (remaining <= 0f)
            {
                releaseAction?.Invoke(this);
            }
        }
    }
}

using UnityEngine;

namespace AnchorDefense
{
    public sealed class VfxService
    {
        private readonly ComponentPool<PooledParticleEffect> hitPool;
        private readonly ComponentPool<PooledParticleEffect> deathPool;

        public VfxService(EnemyConfig config, Transform poolRoot)
        {
            hitPool = new ComponentPool<PooledParticleEffect>(
                () => Object.Instantiate(config.HitEffectPrefab),
                poolRoot,
                30);
            deathPool = new ComponentPool<PooledParticleEffect>(
                () => Object.Instantiate(config.DeathEffectPrefab),
                poolRoot,
                20);
        }

        public void SpawnHit(Vector3 position, Color color)
        {
            PooledParticleEffect effect = hitPool.Get();
            effect.PlayBurst(position, color, 7, 0.55f, hitPool.Release);
        }

        public void SpawnDeath(Vector3 position, Color color)
        {
            PooledParticleEffect effect = deathPool.Get();
            effect.PlayBurst(position, color, 18, 0.8f, deathPool.Release);
        }
    }
}

using UnityEngine;

namespace AnchorDefense
{
    public sealed class TurretHitVfxService
    {
        private readonly ComponentPool<PooledParticleEffect> hitPool;
        private readonly Color hitColor;

        public TurretHitVfxService(TurretConfig config, Transform poolRoot)
        {
            hitColor = config.HitEffectColor;
            if (config.HitEffectPrefab != null)
            {
                hitPool = new ComponentPool<PooledParticleEffect>(
                    () => Object.Instantiate(config.HitEffectPrefab), poolRoot, 24);
            }
        }

        public void SpawnHit(Vector3 position)
        {
            if (hitPool == null)
            {
                return;
            }

            PooledParticleEffect effect = hitPool.Get();
            effect.PlayBurst(position, hitColor, 9, 0.55f, hitPool.Release);
        }
    }
}

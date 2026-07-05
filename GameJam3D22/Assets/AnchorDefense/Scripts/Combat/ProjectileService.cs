using System.Collections.Generic;
using UnityEngine;

namespace AnchorDefense
{
    public sealed class ProjectileService
    {
        private readonly TurretConfig config;
        private readonly Transform poolRoot;
        private readonly int defaultPrewarmCount;
        private readonly ProjectileDefinition defaultProjectile;
        private readonly ProjectileFusionConfig fusionConfig;
        private readonly Dictionary<ProjectileDefinition, ComponentPool<ProjectileController>> pools =
            new Dictionary<ProjectileDefinition, ComponentPool<ProjectileController>>();
        private readonly Dictionary<PooledParticleEffect, ComponentPool<PooledParticleEffect>> fusionEffectPools =
            new Dictionary<PooledParticleEffect, ComponentPool<PooledParticleEffect>>();
        private readonly List<ProjectileController> activeProjectiles = new List<ProjectileController>(128);
        private readonly ComponentPool<PooledParticleEffect> muzzlePool;

        // Runtime-only compatibility definitions keep old config assets and API calls working.
        private readonly ProjectileDefinition legacyA;
        private readonly ProjectileDefinition legacyB;
        private readonly ProjectileDefinition legacyFused;
        private readonly ProjectileFusionRecipe legacyRecipe;

        public int ActiveProjectileCount => activeProjectiles.Count;
        public int SuccessfulFusionCount { get; private set; }
        public float LastFusedDamage { get; private set; }
        public ProjectileDefinition DefaultProjectile => defaultProjectile;
        public ProjectileFusionConfig FusionConfig => fusionConfig;

        public ProjectileService(TurretConfig turretConfig, Transform projectilePoolRoot, int prewarmCount)
        {
            config = turretConfig;
            poolRoot = projectilePoolRoot;
            defaultPrewarmCount = Mathf.Max(1, prewarmCount / 4);

            legacyA = CreateRuntimeDefinition(
                "legacy_a", "Legacy A", config.ProjectilePrefab, config.ProjectileColor, 1f);
            legacyB = CreateRuntimeDefinition(
                "legacy_b", "Legacy B",
                config.ProjectileBPrefab != null ? config.ProjectileBPrefab : config.ProjectilePrefab,
                config.ProjectileBColor, 1f);
            legacyFused = CreateRuntimeDefinition(
                "legacy_fused", "Legacy Fused",
                config.FusedProjectilePrefab != null ? config.FusedProjectilePrefab : config.ProjectilePrefab,
                config.FusedProjectileColor, 1.35f);
            legacyRecipe = new ProjectileFusionRecipe();
            legacyRecipe.Configure(
                legacyA, legacyB, legacyFused,
                Mathf.Max(1.5f, config.FusionRadius),
                config.FusionDamageMultiplier,
                config.FusedSpeedMultiplier,
                config.FusedHitRadiusMultiplier,
                1f,
                config.FusionEffectPrefab,
                config.FusedProjectileColor);

            defaultProjectile = config.DefaultProjectile != null ? config.DefaultProjectile : legacyA;
            fusionConfig = config.ProjectileFusionConfig;

            EnsurePool(defaultProjectile, Mathf.Max(1, prewarmCount / 2));
            PrewarmRecipeProjectiles();

            if (config.MuzzleEffectPrefab != null)
            {
                muzzlePool = new ComponentPool<PooledParticleEffect>(
                    () => Object.Instantiate(config.MuzzleEffectPrefab), poolRoot, 24);
            }
        }

        public void Fire(Vector3 origin, EnemyController target, float damage)
        {
            Fire(origin, target, damage, defaultProjectile);
        }

        public void Fire(
            Vector3 origin,
            EnemyController target,
            float damage,
            ProjectileDefinition projectileDefinition)
        {
            Fire(origin, target, damage, projectileDefinition, 1f, 1f);
        }

        public void Fire(
            Vector3 origin,
            EnemyController target,
            float damage,
            ProjectileDefinition projectileDefinition,
            float speedMultiplier,
            float hitRadiusMultiplier)
        {
            if (target == null || !target.IsAlive)
            {
                return;
            }

            ProjectileDefinition definition = projectileDefinition != null
                ? projectileDefinition
                : defaultProjectile;
            Spawn(origin, target, damage, definition, speedMultiplier, hitRadiusMultiplier, 1f);
            SpawnMuzzle(origin, definition.VisualColor);
        }

        // Backward-compatible overload for old tests/tools. New turrets should assign a
        // ProjectileDefinition asset instead of using this enum.
        public void Fire(Vector3 origin, EnemyController target, float damage, TurretProjectileType legacyType)
        {
            Fire(origin, target, damage, ResolveLegacyDefinition(legacyType));
        }

        public void Fire(
            Vector3 origin,
            EnemyController target,
            float damage,
            TurretProjectileType legacyType,
            float speedMultiplier,
            float hitRadiusMultiplier)
        {
            Fire(origin, target, damage, ResolveLegacyDefinition(legacyType), speedMultiplier, hitRadiusMultiplier);
        }

        private void Spawn(
            Vector3 origin,
            EnemyController target,
            float baseDamage,
            ProjectileDefinition definition,
            float externalSpeedMultiplier,
            float externalHitRadiusMultiplier,
            float externalLifetimeMultiplier)
        {
            if (definition == null || definition.Prefab == null)
            {
                Debug.LogError("Cannot fire: the selected Projectile Definition has no prefab.", definition);
                return;
            }

            ComponentPool<ProjectileController> pool = EnsurePool(definition, 0);
            ProjectileController projectile = pool.Get();
            projectile.Launch(
                origin,
                target,
                baseDamage * definition.DamageMultiplier,
                config.ProjectileSpeed * definition.SpeedMultiplier * Mathf.Max(0.1f, externalSpeedMultiplier),
                config.ProjectileHitRadius * definition.HitRadiusMultiplier * Mathf.Max(0.1f, externalHitRadiusMultiplier),
                config.ProjectileLifetime * definition.LifetimeMultiplier * Mathf.Max(0.1f, externalLifetimeMultiplier),
                definition,
                ReleaseProjectile,
                HandleProjectileMoved);
            activeProjectiles.Add(projectile);
        }

        private ComponentPool<ProjectileController> EnsurePool(ProjectileDefinition definition, int prewarmCount)
        {
            if (definition == null || definition.Prefab == null)
            {
                return null;
            }

            if (!pools.TryGetValue(definition, out ComponentPool<ProjectileController> pool))
            {
                ProjectileController prefab = definition.Prefab;
                pool = new ComponentPool<ProjectileController>(
                    () => Object.Instantiate(prefab), poolRoot, Mathf.Max(0, prewarmCount));
                pools.Add(definition, pool);
            }

            return pool;
        }

        private void PrewarmRecipeProjectiles()
        {
            if (fusionConfig == null)
            {
                EnsurePool(legacyB, defaultPrewarmCount);
                EnsurePool(legacyFused, Mathf.Max(1, defaultPrewarmCount / 2));
                return;
            }

            IReadOnlyList<ProjectileFusionRecipe> recipes = fusionConfig.Recipes;
            for (int i = 0; i < recipes.Count; i++)
            {
                ProjectileFusionRecipe recipe = recipes[i];
                if (recipe == null || !recipe.IsValid) continue;
                EnsurePool(recipe.InputA, defaultPrewarmCount);
                EnsurePool(recipe.InputB, defaultPrewarmCount);
                EnsurePool(recipe.Result, Mathf.Max(1, defaultPrewarmCount / 2));
            }
        }

        private void HandleProjectileMoved(ProjectileController source)
        {
            if (source == null || !source.IsFlying || source.Definition == null)
            {
                return;
            }

            for (int i = activeProjectiles.Count - 1; i >= 0; i--)
            {
                ProjectileController other = activeProjectiles[i];
                if (other == null || other == source || !other.IsFlying || other.Definition == null)
                {
                    continue;
                }

                if (!TryGetRecipe(source.Definition, other.Definition, out ProjectileFusionRecipe recipe))
                {
                    continue;
                }

                float fusionRadiusSqr = recipe.FusionRadius * recipe.FusionRadius;
                if ((source.transform.position - other.transform.position).sqrMagnitude > fusionRadiusSqr)
                {
                    continue;
                }

                EnemyController target = SelectFusionTarget(source, other);
                if (target == null)
                {
                    continue;
                }

                Vector3 fusionPosition = (source.transform.position + other.transform.position) * 0.5f;
                float fusedDamage = (source.Damage + other.Damage) * recipe.DamageMultiplier;
                LastFusedDamage = fusedDamage * recipe.Result.DamageMultiplier;

                source.ReleaseForFusion();
                other.ReleaseForFusion();
                Spawn(
                    fusionPosition,
                    target,
                    fusedDamage,
                    recipe.Result,
                    recipe.ResultSpeedMultiplier,
                    recipe.ResultHitRadiusMultiplier,
                    recipe.ResultLifetimeMultiplier);
                SpawnFusionEffect(fusionPosition, recipe);
                SuccessfulFusionCount++;
                return;
            }
        }

        private bool TryGetRecipe(
            ProjectileDefinition first,
            ProjectileDefinition second,
            out ProjectileFusionRecipe recipe)
        {
            if (fusionConfig != null)
            {
                return fusionConfig.TryGetRecipe(first, second, out recipe);
            }

            if (legacyRecipe.Matches(first, second))
            {
                recipe = legacyRecipe;
                return true;
            }

            recipe = null;
            return false;
        }

        private static EnemyController SelectFusionTarget(ProjectileController first, ProjectileController second)
        {
            bool firstValid = first.Target != null && first.Target.IsAlive;
            bool secondValid = second.Target != null && second.Target.IsAlive;
            if (!firstValid) return secondValid ? second.Target : null;
            if (!secondValid) return first.Target;
            Vector3 position = (first.transform.position + second.transform.position) * 0.5f;
            float firstDistance = (first.Target.transform.position - position).sqrMagnitude;
            float secondDistance = (second.Target.transform.position - position).sqrMagnitude;
            return firstDistance <= secondDistance ? first.Target : second.Target;
        }

        private void ReleaseProjectile(ProjectileController projectile)
        {
            if (projectile == null)
            {
                return;
            }

            ProjectileDefinition definition = projectile.Definition;
            activeProjectiles.Remove(projectile);
            if (definition != null && pools.TryGetValue(definition, out ComponentPool<ProjectileController> pool))
            {
                pool.Release(projectile);
            }
            else
            {
                Object.Destroy(projectile.gameObject);
            }
        }

        private void SpawnMuzzle(Vector3 position, Color color)
        {
            if (muzzlePool == null)
            {
                return;
            }

            PooledParticleEffect muzzle = muzzlePool.Get();
            muzzle.PlayBurst(position, color, 5, 0.28f, muzzlePool.Release);
        }

        private void SpawnFusionEffect(Vector3 position, ProjectileFusionRecipe recipe)
        {
            PooledParticleEffect prefab = recipe.FusionEffectPrefab;
            if (prefab == null)
            {
                return;
            }

            if (!fusionEffectPools.TryGetValue(prefab, out ComponentPool<PooledParticleEffect> pool))
            {
                pool = new ComponentPool<PooledParticleEffect>(
                    () => Object.Instantiate(prefab), poolRoot, 8);
                fusionEffectPools.Add(prefab, pool);
            }

            PooledParticleEffect effect = pool.Get();
            effect.PlayBurst(
                position,
                recipe.FusionEffectColor,
                recipe.BurstCount,
                recipe.EffectDuration,
                recipe.MinimumParticleSize,
                recipe.MaximumParticleSize,
                recipe.MinimumParticleSpeed,
                recipe.MaximumParticleSpeed,
                pool.Release);
        }

        private ProjectileDefinition ResolveLegacyDefinition(TurretProjectileType legacyType)
        {
            if (fusionConfig != null && fusionConfig.Recipes.Count > 0)
            {
                ProjectileFusionRecipe firstRecipe = fusionConfig.Recipes[0];
                if (firstRecipe != null && firstRecipe.IsValid)
                {
                    switch (legacyType)
                    {
                        case TurretProjectileType.B: return firstRecipe.InputB;
                        case TurretProjectileType.Fused: return firstRecipe.Result;
                        default: return firstRecipe.InputA;
                    }
                }
            }

            switch (legacyType)
            {
                case TurretProjectileType.B: return legacyB;
                case TurretProjectileType.Fused: return legacyFused;
                default: return legacyA;
            }
        }

        private static ProjectileDefinition CreateRuntimeDefinition(
            string id,
            string label,
            ProjectileController prefab,
            Color color,
            float visualScale)
        {
            ProjectileDefinition definition = ScriptableObject.CreateInstance<ProjectileDefinition>();
            definition.name = label;
            definition.hideFlags = HideFlags.HideAndDontSave;
            definition.Configure(id, label, prefab, color, 1f, 1f, 1f, 1f, visualScale);
            return definition;
        }
    }
}

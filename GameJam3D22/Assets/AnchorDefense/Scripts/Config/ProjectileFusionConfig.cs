using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnchorDefense
{
    [Serializable]
    public sealed class ProjectileFusionRecipe
    {
        [SerializeField] private ProjectileDefinition inputA;
        [SerializeField] private ProjectileDefinition inputB;
        [SerializeField] private ProjectileDefinition result;

        [Header("Fusion Gameplay")]
        [SerializeField, Min(0.05f)] private float fusionRadius = 1.5f;
        [SerializeField, Min(0f)] private float damageMultiplier = 1.35f;
        [SerializeField, Min(0.1f)] private float resultSpeedMultiplier = 1.1f;
        [SerializeField, Min(0.1f)] private float resultHitRadiusMultiplier = 1.5f;
        [SerializeField, Min(0.1f)] private float resultLifetimeMultiplier = 1f;

        [Header("Fusion Feedback")]
        [SerializeField] private PooledParticleEffect fusionEffectPrefab;
        [SerializeField] private Color fusionEffectColor = new Color(1f, 0.78f, 0.18f, 1f);
        [SerializeField, Range(1, 128)] private int burstCount = 36;
        [SerializeField, Min(0.05f)] private float effectDuration = 0.9f;
        [SerializeField, Min(0.01f)] private float minimumParticleSize = 0.12f;
        [SerializeField, Min(0.01f)] private float maximumParticleSize = 0.38f;
        [SerializeField, Min(0f)] private float minimumParticleSpeed = 2f;
        [SerializeField, Min(0f)] private float maximumParticleSpeed = 6f;

        public ProjectileDefinition InputA => inputA;
        public ProjectileDefinition InputB => inputB;
        public ProjectileDefinition Result => result;
        public float FusionRadius => fusionRadius;
        public float DamageMultiplier => damageMultiplier;
        public float ResultSpeedMultiplier => resultSpeedMultiplier;
        public float ResultHitRadiusMultiplier => resultHitRadiusMultiplier;
        public float ResultLifetimeMultiplier => resultLifetimeMultiplier;
        public PooledParticleEffect FusionEffectPrefab => fusionEffectPrefab;
        public Color FusionEffectColor => fusionEffectColor;
        public int BurstCount => burstCount;
        public float EffectDuration => effectDuration;
        public float MinimumParticleSize => Mathf.Min(minimumParticleSize, maximumParticleSize);
        public float MaximumParticleSize => Mathf.Max(minimumParticleSize, maximumParticleSize);
        public float MinimumParticleSpeed => Mathf.Min(minimumParticleSpeed, maximumParticleSpeed);
        public float MaximumParticleSpeed => Mathf.Max(minimumParticleSpeed, maximumParticleSpeed);
        public bool IsValid => inputA != null && inputB != null && result != null;

        public bool Matches(ProjectileDefinition first, ProjectileDefinition second)
        {
            return IsValid &&
                   ((first == inputA && second == inputB) || (first == inputB && second == inputA));
        }

        public void Configure(
            ProjectileDefinition firstInput,
            ProjectileDefinition secondInput,
            ProjectileDefinition output,
            float radius,
            float fusedDamageMultiplier,
            float fusedSpeedMultiplier,
            float fusedHitRadiusMultiplier,
            float fusedLifetimeMultiplier,
            PooledParticleEffect effectPrefab,
            Color effectColor,
            int particles = 36,
            float duration = 0.9f,
            float minParticleSize = 0.12f,
            float maxParticleSize = 0.38f,
            float minParticleSpeed = 2f,
            float maxParticleSpeed = 6f)
        {
            inputA = firstInput;
            inputB = secondInput;
            result = output;
            fusionRadius = Mathf.Max(0.05f, radius);
            damageMultiplier = Mathf.Max(0f, fusedDamageMultiplier);
            resultSpeedMultiplier = Mathf.Max(0.1f, fusedSpeedMultiplier);
            resultHitRadiusMultiplier = Mathf.Max(0.1f, fusedHitRadiusMultiplier);
            resultLifetimeMultiplier = Mathf.Max(0.1f, fusedLifetimeMultiplier);
            fusionEffectPrefab = effectPrefab;
            fusionEffectColor = effectColor;
            burstCount = Mathf.Clamp(particles, 1, 128);
            effectDuration = Mathf.Max(0.05f, duration);
            minimumParticleSize = Mathf.Max(0.01f, minParticleSize);
            maximumParticleSize = Mathf.Max(minimumParticleSize, maxParticleSize);
            minimumParticleSpeed = Mathf.Max(0f, minParticleSpeed);
            maximumParticleSpeed = Mathf.Max(minimumParticleSpeed, maxParticleSpeed);
        }
    }

    [CreateAssetMenu(
        menuName = "Anchor Defense/Combat/Projectile Fusion Config",
        fileName = "ProjectileFusionConfig")]
    public sealed class ProjectileFusionConfig : ScriptableObject
    {
        [SerializeField] private List<ProjectileFusionRecipe> recipes = new List<ProjectileFusionRecipe>();

        public IReadOnlyList<ProjectileFusionRecipe> Recipes => recipes;

        public bool TryGetRecipe(
            ProjectileDefinition first,
            ProjectileDefinition second,
            out ProjectileFusionRecipe recipe)
        {
            for (int i = 0; i < recipes.Count; i++)
            {
                ProjectileFusionRecipe candidate = recipes[i];
                if (candidate != null && candidate.Matches(first, second))
                {
                    recipe = candidate;
                    return true;
                }
            }

            recipe = null;
            return false;
        }

        public void Configure(IEnumerable<ProjectileFusionRecipe> newRecipes)
        {
            recipes.Clear();
            if (newRecipes != null)
            {
                recipes.AddRange(newRecipes);
            }
        }
    }
}

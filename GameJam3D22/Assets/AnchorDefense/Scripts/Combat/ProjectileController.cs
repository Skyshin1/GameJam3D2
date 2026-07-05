using System;
using UnityEngine;

namespace AnchorDefense
{
    public sealed class ProjectileController : MonoBehaviour, IPoolable
    {
        private EnemyController target;
        private Action<ProjectileController> releaseAction;
        private Action<ProjectileController> movedAction;

        [SerializeField] private TrailRenderer trail;
        [SerializeField] private DirectionalSpriteRenderer directionalVisual;

        private TrailRenderer[] trails;
        private ParticleSystem[] particleSystems;
        private Renderer[] renderers;
        private Light[] lights;
        private Color[] originalLightColors;
        private float[] originalLightIntensities;
        private Vector3 originalScale;
        private MaterialPropertyBlock colorProperties;

        private Vector3 direction;
        private float damage;
        private float speed;
        private float hitRadius;
        private float lifetime;
        private bool isFlying;
        private int targetSpawnVersion;

        public bool IsFlying => isFlying;
        public ProjectileDefinition Definition { get; private set; }
        public float Damage => damage;
        public EnemyController Target => target;

        private void Awake()
        {
            trails = GetComponentsInChildren<TrailRenderer>(true);
            particleSystems = GetComponentsInChildren<ParticleSystem>(true);
            renderers = GetComponentsInChildren<Renderer>(true);
            lights = GetComponentsInChildren<Light>(true);
            colorProperties = new MaterialPropertyBlock();
            originalLightColors = new Color[lights.Length];
            originalLightIntensities = new float[lights.Length];
            originalScale = transform.localScale;

            for (int i = 0; i < lights.Length; i++)
            {
                originalLightColors[i] = lights[i].color;
                originalLightIntensities[i] = lights[i].intensity;
            }

            if (trail == null && trails != null && trails.Length > 0)
            {
                trail = trails[0];
            }
        }

        public void Configure(TrailRenderer projectileTrail)
        {
            trail = projectileTrail;
            trails = GetComponentsInChildren<TrailRenderer>(true);
        }

        public void ConfigureDirectionalVisual(DirectionalSpriteRenderer visual)
        {
            directionalVisual = visual;
        }

        public void Launch(
            Vector3 position,
            EnemyController newTarget,
            float projectileDamage,
            float projectileSpeed,
            float projectileHitRadius,
            float projectileLifetime,
            ProjectileDefinition definition,
            Action<ProjectileController> onRelease,
            Action<ProjectileController> onMoved = null)
        {
            isFlying = false;

            StopAndClearVfx();

            transform.position = position;

            target = newTarget;
            targetSpawnVersion = target != null ? target.SpawnVersion : 0;
            damage = projectileDamage;
            speed = projectileSpeed;
            hitRadius = projectileHitRadius;
            lifetime = projectileLifetime;
            releaseAction = onRelease;
            movedAction = onMoved;
            Definition = definition;
            ApplyDefinitionPresentation(definition);

            direction = target != null
                ? (target.transform.position - position).normalized
                : transform.forward;

            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }

            directionalVisual?.SetWorldDirection(direction);

            ResetAndPlayVfx();

            isFlying = true;
        }

        public void OnTakenFromPool()
        {
            isFlying = false;
            StopAndClearVfx();
        }

        public void OnReturnedToPool()
        {
            isFlying = false;
            target = null;
            targetSpawnVersion = 0;
            releaseAction = null;
            movedAction = null;
            Definition = null;
            transform.localScale = originalScale;
            StopAndClearVfx();
        }

        private void ApplyDefinitionPresentation(ProjectileDefinition definition)
        {
            transform.localScale = originalScale * (definition != null ? definition.VisualScaleMultiplier : 1f);
            if (definition == null)
            {
                return;
            }

            Color color = definition.VisualColor;
            if (definition.OverrideVisualColor)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer visualRenderer = renderers[i];
                    if (visualRenderer == null || visualRenderer is TrailRenderer)
                    {
                        continue;
                    }

                    if (visualRenderer is SpriteRenderer spriteRenderer)
                    {
                        spriteRenderer.color = color;
                        continue;
                    }

                    visualRenderer.GetPropertyBlock(colorProperties);
                    colorProperties.SetColor("_BaseColor", color);
                    colorProperties.SetColor("_Color", color);
                    visualRenderer.SetPropertyBlock(colorProperties);
                }

                for (int i = 0; i < trails.Length; i++)
                {
                    if (trails[i] == null) continue;
                    trails[i].startColor = color;
                    trails[i].endColor = new Color(color.r, color.g, color.b, 0f);
                }

                for (int i = 0; i < particleSystems.Length; i++)
                {
                    if (particleSystems[i] == null) continue;
                    ParticleSystem.MainModule main = particleSystems[i].main;
                    main.startColor = color;
                }
            }

            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] == null) continue;
                lights[i].color = definition.OverrideVisualColor ? color : originalLightColors[i];
                lights[i].intensity = originalLightIntensities[i] * definition.LightIntensityMultiplier;
            }
        }

        private void Update()
        {
            if (!isFlying)
            {
                return;
            }

            lifetime -= Time.deltaTime;
            if (lifetime <= 0f)
            {
                Release();
                return;
            }

            if (target != null && target.IsAlive && target.SpawnVersion == targetSpawnVersion)
            {
                Vector3 toTarget = target.transform.position - transform.position;
                float distance = toTarget.magnitude;
                float travelDistance = speed * Time.deltaTime;
                direction = distance > 0.001f ? toTarget / distance : direction;
                directionalVisual?.SetWorldDirection(direction);

                if (distance <= travelDistance + hitRadius)
                {
                    transform.position = target.transform.position;
                    target.TakeDamage(new DamageInfo(damage, transform.position, gameObject));
                    Release();
                    return;
                }
            }

            transform.position += direction * (speed * Time.deltaTime);

            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }

            movedAction?.Invoke(this);
        }

        public void ReleaseForFusion()
        {
            Release();
        }

        private void Release()
        {
            if (!isFlying)
            {
                return;
            }

            isFlying = false;
            StopAndClearVfx();
            releaseAction?.Invoke(this);
        }

        private void StopAndClearVfx()
        {
            if (trails != null)
            {
                foreach (TrailRenderer t in trails)
                {
                    if (t == null) continue;

                    t.emitting = false;
                    t.Clear();
                }
            }

            if (particleSystems != null)
            {
                foreach (ParticleSystem ps in particleSystems)
                {
                    if (ps == null) continue;

                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.Clear(true);
                }
            }
        }

        private void ResetAndPlayVfx()
        {
            if (trails != null)
            {
                foreach (TrailRenderer t in trails)
                {
                    if (t == null) continue;

                    t.Clear();
                    t.emitting = true;
                }
            }

            if (particleSystems != null)
            {
                foreach (ParticleSystem ps in particleSystems)
                {
                    if (ps == null) continue;

                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.Clear(true);
                    ps.Play(true);
                }
            }
        }
    }
}

using UnityEngine;

namespace AnchorDefense
{
    [CreateAssetMenu(
        menuName = "Anchor Defense/Combat/Projectile Definition",
        fileName = "ProjectileDefinition")]
    public sealed class ProjectileDefinition : ScriptableObject
    {
        [SerializeField] private string projectileId = "projectile";
        [SerializeField] private string displayName = "Projectile";
        [SerializeField] private ProjectileController prefab;

        [Header("Gameplay Multipliers")]
        [SerializeField, Min(0f)] private float damageMultiplier = 1f;
        [SerializeField, Min(0.1f)] private float speedMultiplier = 1f;
        [SerializeField, Min(0.1f)] private float hitRadiusMultiplier = 1f;
        [SerializeField, Min(0.1f)] private float lifetimeMultiplier = 1f;

        [Header("Presentation")]
        [SerializeField] private bool overrideVisualColor = true;
        [SerializeField] private Color visualColor = Color.white;
        [SerializeField, Min(0.1f)] private float visualScaleMultiplier = 1f;
        [SerializeField, Min(0f)] private float lightIntensityMultiplier = 1f;

        public string ProjectileId => projectileId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public ProjectileController Prefab => prefab;
        public float DamageMultiplier => damageMultiplier;
        public float SpeedMultiplier => speedMultiplier;
        public float HitRadiusMultiplier => hitRadiusMultiplier;
        public float LifetimeMultiplier => lifetimeMultiplier;
        public bool OverrideVisualColor => overrideVisualColor;
        public Color VisualColor => visualColor;
        public float VisualScaleMultiplier => visualScaleMultiplier;
        public float LightIntensityMultiplier => lightIntensityMultiplier;

        public void Configure(
            string id,
            string label,
            ProjectileController projectilePrefab,
            Color color,
            float projectileDamageMultiplier = 1f,
            float projectileSpeedMultiplier = 1f,
            float projectileHitRadiusMultiplier = 1f,
            float projectileLifetimeMultiplier = 1f,
            float projectileVisualScaleMultiplier = 1f,
            bool shouldOverrideVisualColor = true,
            float projectileLightIntensityMultiplier = 1f)
        {
            projectileId = string.IsNullOrWhiteSpace(id) ? "projectile" : id.Trim();
            displayName = string.IsNullOrWhiteSpace(label) ? projectileId : label.Trim();
            prefab = projectilePrefab;
            visualColor = color;
            damageMultiplier = Mathf.Max(0f, projectileDamageMultiplier);
            speedMultiplier = Mathf.Max(0.1f, projectileSpeedMultiplier);
            hitRadiusMultiplier = Mathf.Max(0.1f, projectileHitRadiusMultiplier);
            lifetimeMultiplier = Mathf.Max(0.1f, projectileLifetimeMultiplier);
            visualScaleMultiplier = Mathf.Max(0.1f, projectileVisualScaleMultiplier);
            overrideVisualColor = shouldOverrideVisualColor;
            lightIntensityMultiplier = Mathf.Max(0f, projectileLightIntensityMultiplier);
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(projectileId))
            {
                projectileId = name;
            }
        }
    }
}

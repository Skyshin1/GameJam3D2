using UnityEditor;
using UnityEngine;

namespace AnchorDefense.Editor
{
    public static class AnchorDefenseProjectileFusionBuilder
    {
        private const string Root = "Assets/AnchorDefense";
        private const string ProjectileAPath = Root + "/Prefabs/Gameplay/Projectile.prefab";
        private const string ProjectileBPath = Root + "/Prefabs/Gameplay/Projectile_B.prefab";
        private const string ProjectileFusedPath = Root + "/Prefabs/Gameplay/Projectile_Fused.prefab";
        private const string TurretAPath = Root + "/Prefabs/Gameplay/Turret.prefab";
        private const string TurretBPath = Root + "/Prefabs/Gameplay/Turret_B.prefab";
        private const string ConfigPath = Root + "/Configs/TurretConfig.asset";
        private const string FusionEffectPath = Root + "/Prefabs/VFX/HitEffect.prefab";
        private const string ProjectileDefinitionAPath = Root + "/Configs/Projectiles/Projectile_A.asset";
        private const string ProjectileDefinitionBPath = Root + "/Configs/Projectiles/Projectile_B.asset";
        private const string ProjectileDefinitionFusedPath = Root + "/Configs/Projectiles/Projectile_Fused.asset";
        private const string FusionConfigPath = Root + "/Configs/Projectiles/ProjectileFusionConfig.asset";

        [MenuItem("Tools/Anchor Defense/Install Projectile Fusion Assets")]
        public static void BuildAll()
        {
            ProjectileController projectileB = DuplicateProjectile(ProjectileBPath, "Projectile_B", 1f);
            ProjectileController fusedProjectile = DuplicateProjectile(ProjectileFusedPath, "Projectile_Fused", 1.5f);
            ProjectileDefinition projectileA = EnsureDefinition(
                ProjectileDefinitionAPath, "projectile_a", "Projectile A",
                AssetDatabase.LoadAssetAtPath<ProjectileController>(ProjectileAPath),
                new Color(0.35f, 1f, 1f), 1f);
            ProjectileDefinition projectileBDefinition = EnsureDefinition(
                ProjectileDefinitionBPath, "projectile_b", "Projectile B",
                projectileB, new Color(1f, 0.4f, 0.9f), 1f);
            ProjectileDefinition fusedDefinition = EnsureDefinition(
                ProjectileDefinitionFusedPath, "projectile_fused", "Fused Projectile",
                fusedProjectile, new Color(1f, 0.78f, 0.18f), 1.4f);
            ProjectileFusionConfig fusionConfig = EnsureFusionConfig(
                projectileA, projectileBDefinition, fusedDefinition);
            DuplicateTurretB(projectileBDefinition);
            ConfigureTurretConfig(projectileB, fusedProjectile, projectileA, fusionConfig);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Projectile A + B fusion assets installed successfully.");
        }

        private static ProjectileController DuplicateProjectile(string destination, string objectName, float visualScale)
        {
            ProjectileController existing = AssetDatabase.LoadAssetAtPath<ProjectileController>(destination);
            if (existing != null)
            {
                return existing;
            }
            if (!AssetDatabase.CopyAsset(ProjectileAPath, destination))
            {
                return null;
            }
            AssetDatabase.ImportAsset(destination);
            GameObject root = PrefabUtility.LoadPrefabContents(destination);
            root.name = objectName;
            Transform visual = root.transform.Find("VisualRoot");
            if (visual != null)
            {
                visual.localScale *= visualScale;
            }
            PrefabUtility.SaveAsPrefabAsset(root, destination);
            PrefabUtility.UnloadPrefabContents(root);
            return AssetDatabase.LoadAssetAtPath<ProjectileController>(destination);
        }

        private static void DuplicateTurretB(ProjectileDefinition projectileDefinition)
        {
            if (AssetDatabase.LoadAssetAtPath<TurretController>(TurretBPath) == null)
            {
                if (!AssetDatabase.CopyAsset(TurretAPath, TurretBPath))
                {
                    return;
                }
                AssetDatabase.ImportAsset(TurretBPath);
            }
            GameObject root = PrefabUtility.LoadPrefabContents(TurretBPath);
            root.name = "Turret_B";
            root.GetComponent<TurretController>()?.ConfigureProjectile(projectileDefinition);
            PrefabUtility.SaveAsPrefabAsset(root, TurretBPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void ConfigureTurretConfig(
            ProjectileController projectileB,
            ProjectileController fusedProjectile,
            ProjectileDefinition defaultProjectile,
            ProjectileFusionConfig fusionConfig)
        {
            TurretConfig config = AssetDatabase.LoadAssetAtPath<TurretConfig>(ConfigPath);
            if (config == null)
            {
                return;
            }
            SerializedObject serialized = new SerializedObject(config);
            serialized.FindProperty("<ProjectileBPrefab>k__BackingField").objectReferenceValue = projectileB;
            serialized.FindProperty("<FusedProjectilePrefab>k__BackingField").objectReferenceValue = fusedProjectile;
            PooledParticleEffect effect = AssetDatabase.LoadAssetAtPath<PooledParticleEffect>(FusionEffectPath);
            serialized.FindProperty("<FusionEffectPrefab>k__BackingField").objectReferenceValue = effect;
            serialized.FindProperty("<DefaultProjectile>k__BackingField").objectReferenceValue = defaultProjectile;
            serialized.FindProperty("<ProjectileFusionConfig>k__BackingField").objectReferenceValue = fusionConfig;
            serialized.FindProperty("<FusionRadius>k__BackingField").floatValue = 1.5f;
            serialized.FindProperty("<FusionDamageMultiplier>k__BackingField").floatValue = 1.35f;
            serialized.FindProperty("<FusedSpeedMultiplier>k__BackingField").floatValue = 1.1f;
            serialized.FindProperty("<FusedHitRadiusMultiplier>k__BackingField").floatValue = 1.5f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }

        private static ProjectileDefinition EnsureDefinition(
            string path,
            string id,
            string label,
            ProjectileController prefab,
            Color color,
            float visualScale)
        {
            ProjectileDefinition definition = AssetDatabase.LoadAssetAtPath<ProjectileDefinition>(path);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<ProjectileDefinition>();
                AssetDatabase.CreateAsset(definition, path);
            }
            definition.Configure(id, label, prefab, color, 1f, 1f, 1f, 1f, visualScale);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static ProjectileFusionConfig EnsureFusionConfig(
            ProjectileDefinition projectileA,
            ProjectileDefinition projectileB,
            ProjectileDefinition result)
        {
            ProjectileFusionConfig fusionConfig =
                AssetDatabase.LoadAssetAtPath<ProjectileFusionConfig>(FusionConfigPath);
            if (fusionConfig == null)
            {
                fusionConfig = ScriptableObject.CreateInstance<ProjectileFusionConfig>();
                AssetDatabase.CreateAsset(fusionConfig, FusionConfigPath);
            }
            PooledParticleEffect effect = AssetDatabase.LoadAssetAtPath<PooledParticleEffect>(FusionEffectPath);
            var recipe = new ProjectileFusionRecipe();
            recipe.Configure(
                projectileA, projectileB, result,
                1.5f, 1.35f, 1.1f, 1.5f, 1f,
                effect, new Color(1f, 0.78f, 0.18f),
                36, 0.9f, 0.12f, 0.38f, 2f, 6f);
            fusionConfig.Configure(new[] { recipe });
            EditorUtility.SetDirty(fusionConfig);
            return fusionConfig;
        }
    }
}

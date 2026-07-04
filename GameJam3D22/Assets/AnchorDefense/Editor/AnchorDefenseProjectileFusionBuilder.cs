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

        [MenuItem("Tools/Anchor Defense/Install Projectile Fusion Assets")]
        public static void BuildAll()
        {
            ProjectileController projectileB = DuplicateProjectile(ProjectileBPath, "Projectile_B", 1f);
            ProjectileController fusedProjectile = DuplicateProjectile(ProjectileFusedPath, "Projectile_Fused", 1.5f);
            DuplicateTurretB();
            ConfigureTurretConfig(projectileB, fusedProjectile);
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

        private static void DuplicateTurretB()
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
            root.GetComponent<TurretController>()?.ConfigureProjectileType(TurretProjectileType.B);
            PrefabUtility.SaveAsPrefabAsset(root, TurretBPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void ConfigureTurretConfig(
            ProjectileController projectileB, ProjectileController fusedProjectile)
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
            serialized.FindProperty("<FusionRadius>k__BackingField").floatValue = 0.75f;
            serialized.FindProperty("<FusionDamageMultiplier>k__BackingField").floatValue = 1.35f;
            serialized.FindProperty("<FusedSpeedMultiplier>k__BackingField").floatValue = 1.1f;
            serialized.FindProperty("<FusedHitRadiusMultiplier>k__BackingField").floatValue = 1.5f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }
    }
}

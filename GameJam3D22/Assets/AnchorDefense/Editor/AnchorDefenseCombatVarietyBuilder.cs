using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AnchorDefense.Editor
{
    public static class AnchorDefenseCombatVarietyBuilder
    {
        private const string Root = "Assets/AnchorDefense";
        private const string NormalEnemyConfigPath = Root + "/Configs/EnemyConfig.asset";
        private const string RangedEnemyConfigPath = Root + "/Configs/EnemyConfig_Ranged.asset";
        private const string EndlessConfigPath = Root + "/Configs/EndlessModeConfig.asset";
        private const string TurretConfigPath = Root + "/Configs/TurretConfig.asset";
        private const string NormalEnemyPrefabPath = Root + "/Prefabs/Gameplay/Enemy.prefab";
        private const string RangedEnemyPrefabPath = Root + "/Prefabs/Gameplay/Enemy_Ranged.prefab";
        private const string EnemyProjectilePrefabPath = Root + "/Prefabs/Gameplay/EnemyProjectile.prefab";
        private const string EnemyProjectileMaterialPath = Root + "/Art/Materials/M_EnemyProjectile.mat";
        private const string HitEffectPath = Root + "/Prefabs/VFX/HitEffect.prefab";
        private const string TurretPrefabPath = Root + "/Prefabs/Gameplay/Turret.prefab";

        [MenuItem("Tools/Anchor Defense/Install Turret Slots and Enemy Variety")]
        public static void BuildAll()
        {
            EnemyProjectileController projectile = CreateEnemyProjectile();
            EnemyController rangedEnemy = CreateRangedEnemyPrefab();
            EnemyConfig normalConfig = AssetDatabase.LoadAssetAtPath<EnemyConfig>(NormalEnemyConfigPath);
            EnemyConfig rangedConfig = CreateRangedEnemyConfig(normalConfig, rangedEnemy, projectile);
            ConfigureEnemyRoster(normalConfig, rangedConfig);
            ConfigureTurretHitFeedback();
            PatchRingSet(Root + "/Prefabs/Gameplay", string.Empty, TurretPrefabPath);

            string directionalTurret = Root + "/Prefabs/Directional/Turret_Directional.prefab";
            if (AssetDatabase.LoadAssetAtPath<TurretController>(directionalTurret) != null)
            {
                PatchRingSet(Root + "/Prefabs/Directional", "_Directional", directionalTurret);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Anchor Defense turret slots and weighted enemy roster installed successfully.");
        }

        private static EnemyProjectileController CreateEnemyProjectile()
        {
            EnemyProjectileController existing = AssetDatabase.LoadAssetAtPath<EnemyProjectileController>(EnemyProjectilePrefabPath);
            if (existing != null)
            {
                return existing;
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(EnemyProjectileMaterialPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
                material = new Material(shader) { name = "M_EnemyProjectile" };
                Color color = new Color(1f, 0.12f, 0.035f, 1f);
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                if (material.HasProperty("_Color")) material.SetColor("_Color", color);
                AssetDatabase.CreateAsset(material, EnemyProjectileMaterialPath);
            }

            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            root.name = "EnemyProjectile";
            UnityEngine.Object.DestroyImmediate(root.GetComponent<Collider>());
            root.transform.localScale = Vector3.one * 0.18f;
            root.GetComponent<Renderer>().sharedMaterial = material;
            TrailRenderer trail = root.AddComponent<TrailRenderer>();
            trail.time = 0.28f;
            trail.startWidth = 0.13f;
            trail.endWidth = 0f;
            trail.sharedMaterial = material;
            trail.startColor = new Color(1f, 0.25f, 0.05f, 1f);
            trail.endColor = new Color(1f, 0.04f, 0.01f, 0f);
            EnemyProjectileController controller = root.AddComponent<EnemyProjectileController>();
            controller.Configure(trail);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, EnemyProjectilePrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab.GetComponent<EnemyProjectileController>();
        }

        private static EnemyController CreateRangedEnemyPrefab()
        {
            EnemyController existing = AssetDatabase.LoadAssetAtPath<EnemyController>(RangedEnemyPrefabPath);
            if (existing != null)
            {
                return existing;
            }
            if (!AssetDatabase.CopyAsset(NormalEnemyPrefabPath, RangedEnemyPrefabPath))
            {
                throw new InvalidOperationException("Could not create the ranged enemy prefab.");
            }
            AssetDatabase.ImportAsset(RangedEnemyPrefabPath);
            GameObject root = PrefabUtility.LoadPrefabContents(RangedEnemyPrefabPath);
            root.name = "Enemy_Ranged";
            GameObject emitter = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            emitter.name = "Ranged Emitter";
            emitter.transform.SetParent(root.transform.Find("VisualRoot"), false);
            emitter.transform.localPosition = new Vector3(0f, 0.25f, 0.55f);
            emitter.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            emitter.transform.localScale = new Vector3(0.22f, 0.38f, 0.22f);
            UnityEngine.Object.DestroyImmediate(emitter.GetComponent<Collider>());
            PrefabUtility.SaveAsPrefabAsset(root, RangedEnemyPrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            return AssetDatabase.LoadAssetAtPath<EnemyController>(RangedEnemyPrefabPath);
        }

        private static EnemyConfig CreateRangedEnemyConfig(EnemyConfig normal, EnemyController prefab, EnemyProjectileController projectile)
        {
            EnemyConfig config = AssetDatabase.LoadAssetAtPath<EnemyConfig>(RangedEnemyConfigPath);
            bool created = config == null;
            if (created)
            {
                config = ScriptableObject.CreateInstance<EnemyConfig>();
                AssetDatabase.CreateAsset(config, RangedEnemyConfigPath);
            }

            SerializedObject serialized = new SerializedObject(config);
            SetObject(serialized, "<Prefab>k__BackingField", prefab);
            SetObject(serialized, "<ProjectilePrefab>k__BackingField", projectile);
            if (normal != null)
            {
                SetObject(serialized, "<HitEffectPrefab>k__BackingField", normal.HitEffectPrefab);
                SetObject(serialized, "<DeathEffectPrefab>k__BackingField", normal.DeathEffectPrefab);
            }
            if (created)
            {
                SetFloat(serialized, "<MaxHealth>k__BackingField", 36f);
                SetFloat(serialized, "<MoveSpeed>k__BackingField", 1.25f);
                SetFloat(serialized, "<CoreDamage>k__BackingField", 0f);
                SetFloat(serialized, "<Size>k__BackingField", 0.62f);
                SetFloat(serialized, "<RangedStopRadius>k__BackingField", 10.5f);
                SetFloat(serialized, "<FireInterval>k__BackingField", 2.2f);
                SetFloat(serialized, "<ProjectileDamage>k__BackingField", 18f);
                SetFloat(serialized, "<ProjectileSpeed>k__BackingField", 8f);
                SetFloat(serialized, "<ProjectileLifetime>k__BackingField", 4f);
                SetFloat(serialized, "<ProjectileHitRadius>k__BackingField", 0.18f);
                serialized.FindProperty("<AttackMode>k__BackingField").enumValueIndex = (int)EnemyAttackMode.RangedTurret;
                serialized.FindProperty("<BaseColor>k__BackingField").colorValue = new Color(0.8f, 0.12f, 1f, 1f);
                serialized.FindProperty("<ProjectileColor>k__BackingField").colorValue = new Color(1f, 0.15f, 0.04f, 1f);
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            return config;
        }

        private static void ConfigureEnemyRoster(EnemyConfig normal, EnemyConfig ranged)
        {
            EndlessModeConfig endless = AssetDatabase.LoadAssetAtPath<EndlessModeConfig>(EndlessConfigPath);
            if (endless == null || normal == null || ranged == null || endless.HasEnemyRoster)
            {
                return;
            }
            SerializedObject serialized = new SerializedObject(endless);
            SerializedProperty roster = serialized.FindProperty("<EnemyTypes>k__BackingField");
            roster.arraySize = 2;
            ConfigureEntry(roster.GetArrayElementAtIndex(0), normal, 75f, 52);
            ConfigureEntry(roster.GetArrayElementAtIndex(1), ranged, 25f, 18);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(endless);
        }

        private static void ConfigureEntry(SerializedProperty entry, EnemyConfig enemy, float weight, int prewarm)
        {
            entry.FindPropertyRelative("enemy").objectReferenceValue = enemy;
            entry.FindPropertyRelative("spawnWeight").floatValue = weight;
            entry.FindPropertyRelative("prewarmCount").intValue = prewarm;
        }

        private static void ConfigureTurretHitFeedback()
        {
            TurretConfig turretConfig = AssetDatabase.LoadAssetAtPath<TurretConfig>(TurretConfigPath);
            PooledParticleEffect hitEffect = AssetDatabase.LoadAssetAtPath<PooledParticleEffect>(HitEffectPath);
            if (turretConfig == null || hitEffect == null)
            {
                return;
            }
            SerializedObject serialized = new SerializedObject(turretConfig);
            SetObject(serialized, "<HitEffectPrefab>k__BackingField", hitEffect);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(turretConfig);
        }

        private static void PatchRingSet(string folder, string suffix, string turretPath)
        {
            TurretController turretPrefab = AssetDatabase.LoadAssetAtPath<TurretController>(turretPath);
            if (turretPrefab == null)
            {
                return;
            }
            PatchRing(folder + "/OrbitRing_Inner" + suffix + ".prefab", OrbitRingId.Inner, turretPrefab);
            PatchRing(folder + "/OrbitRing_Middle" + suffix + ".prefab", OrbitRingId.Middle, turretPrefab);
            PatchRing(folder + "/OrbitRing_Outer" + suffix + ".prefab", OrbitRingId.Outer, turretPrefab);
        }

        private static void PatchRing(string path, OrbitRingId ringId, TurretController defaultTurret)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                return;
            }
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            OrbitRingController ring = root.GetComponent<OrbitRingController>();
            Transform turretRoot = root.transform.Find("Turrets");
            if (ring == null || turretRoot == null)
            {
                PrefabUtility.UnloadPrefabContents(root);
                return;
            }

            var initialSlots = new List<TurretSlot>();
            var upgradeSlots = new List<TurretSlot>();
            TurretSlot[] slots = turretRoot.GetComponentsInChildren<TurretSlot>(true);
            if (slots.Length == 0)
            {
                TurretController[] turrets = turretRoot.GetComponentsInChildren<TurretController>(true);
                for (int i = 0; i < turrets.Length; i++)
                {
                    TurretController source = PrefabUtility.GetCorrespondingObjectFromSource(turrets[i]);
                    if (source == null) source = defaultTurret;
                    bool upgrade = turrets[i].name.StartsWith("Upgrade", StringComparison.Ordinal);
                    GameObject slotObject = new GameObject(upgrade
                        ? $"Upgrade Turret Slot {upgradeSlots.Count + 1:00}"
                        : $"Turret Slot {initialSlots.Count + 1:00}");
                    slotObject.transform.SetParent(turretRoot, false);
                    slotObject.transform.localPosition = turrets[i].transform.localPosition;
                    slotObject.transform.localRotation = turrets[i].transform.localRotation;
                    TurretSlot slot = slotObject.AddComponent<TurretSlot>();
                    slot.Configure(source, !upgrade);
                    if (upgrade) upgradeSlots.Add(slot); else initialSlots.Add(slot);
                    UnityEngine.Object.DestroyImmediate(turrets[i].gameObject);
                }
            }
            else
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    if (slots[i].StartsUnlocked) initialSlots.Add(slots[i]); else upgradeSlots.Add(slots[i]);
                }
            }

            float radius = initialSlots.Count > 0 ? initialSlots[0].transform.localPosition.magnitude : GetRadius(ringId);
            float[] upgradeAngles = { 30f, 210f };
            for (int i = upgradeSlots.Count; i < upgradeAngles.Length; i++)
            {
                float angle = upgradeAngles[i] * Mathf.Deg2Rad;
                Vector3 position = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                GameObject slotObject = new GameObject($"Upgrade Turret Slot {i + 1:00}");
                slotObject.transform.SetParent(turretRoot, false);
                slotObject.transform.localPosition = position;
                slotObject.transform.localRotation = Quaternion.LookRotation(position.normalized, Vector3.up);
                TurretSlot slot = slotObject.AddComponent<TurretSlot>();
                slot.Configure(defaultTurret, false);
                upgradeSlots.Add(slot);
            }

            ring.ConfigureTurretSlotAssets(ringId, initialSlots.ToArray(), upgradeSlots.ToArray());
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static float GetRadius(OrbitRingId id)
        {
            return id == OrbitRingId.Inner ? 4.7f : id == OrbitRingId.Middle ? 6.1f : 7.5f;
        }

        private static void SetObject(SerializedObject target, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = target.FindProperty(propertyName);
            if (property != null) property.objectReferenceValue = value;
        }

        private static void SetFloat(SerializedObject target, string propertyName, float value)
        {
            SerializedProperty property = target.FindProperty(propertyName);
            if (property != null) property.floatValue = value;
        }
    }
}

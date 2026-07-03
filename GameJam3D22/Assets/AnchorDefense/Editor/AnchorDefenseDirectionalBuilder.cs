using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AnchorDefense.Editor
{
    public static class AnchorDefenseDirectionalBuilder
    {
        private const string Root = "Assets/AnchorDefense";
        private const string SpriteFolder = Root + "/Art/Sprites/Directional";
        private const string SpriteSheetPath = SpriteFolder + "/DirectionalSpriteSheet.png";
        private const string DirectionalConfigFolder = Root + "/Configs/Directional";
        private const string DirectionalPrefabFolder = Root + "/Prefabs/Directional";
        private const string GameplayScenePath = Root + "/Scenes/Gameplay.unity";
        private const string DirectionalScenePath = Root + "/Scenes/Gameplay_DirectionalSprites.unity";
        private const string DirectionalMaterialPath = Root + "/Art/Materials/M_DirectionalSprite.mat";

        private static readonly string[] DirectionNames = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };

        [MenuItem("Tools/Anchor Defense/Rebuild Directional Sprite Version")]
        public static void BuildDirectionalVersion()
        {
            EnsureFolder(SpriteFolder);
            EnsureFolder(DirectionalConfigFolder);
            EnsureFolder(DirectionalPrefabFolder);
            ConfigureSpriteSheet();

            Sprite[] turretSprites = LoadSpriteRow("Turret");
            Sprite[] enemySprites = LoadSpriteRow("Enemy");
            Sprite[] projectileSprites = LoadSpriteRow("Projectile");
            DirectionalSpriteSet turretSet = CreateSpriteSet(DirectionalConfigFolder + "/TurretDirections.asset", turretSprites);
            DirectionalSpriteSet enemySet = CreateSpriteSet(DirectionalConfigFolder + "/EnemyDirections.asset", enemySprites);
            DirectionalSpriteSet projectileSet = CreateSpriteSet(DirectionalConfigFolder + "/ProjectileDirections.asset", projectileSprites);
            Material spriteMaterial = CreateDirectionalMaterial();

            GameObject turretPrefab = CreateDirectionalTurretPrefab(turretSet, turretSprites[0], spriteMaterial);
            EnemyController enemyPrefab = CreateDirectionalEnemyPrefab(enemySet, enemySprites[0], spriteMaterial);
            ProjectileController projectilePrefab = CreateDirectionalProjectilePrefab(projectileSet, projectileSprites[0], spriteMaterial);

            EnemyConfig enemyConfig = CopyConfig<EnemyConfig>(
                Root + "/Configs/EnemyConfig.asset",
                DirectionalConfigFolder + "/EnemyConfig_Directional.asset");
            TurretConfig turretConfig = CopyConfig<TurretConfig>(
                Root + "/Configs/TurretConfig.asset",
                DirectionalConfigFolder + "/TurretConfig_Directional.asset");
            SetObjectReference(enemyConfig, "<Prefab>k__BackingField", enemyPrefab);
            SetObjectReference(turretConfig, "<ProjectilePrefab>k__BackingField", projectilePrefab);

            GameObject innerRing = CreateDirectionalRingPrefab(
                "OrbitRing_Inner_Directional",
                4.7f,
                Root + "/Art/Meshes/OrbitRing_Inner.asset",
                Root + "/Art/Materials/M_Ring_Inner.mat",
                new Color(0.08f, 0.62f, 0.9f),
                turretPrefab);
            GameObject middleRing = CreateDirectionalRingPrefab(
                "OrbitRing_Middle_Directional",
                6.1f,
                Root + "/Art/Meshes/OrbitRing_Middle.asset",
                Root + "/Art/Materials/M_Ring_Middle.mat",
                new Color(0.55f, 0.16f, 0.86f),
                turretPrefab);
            GameObject outerRing = CreateDirectionalRingPrefab(
                "OrbitRing_Outer_Directional",
                7.5f,
                Root + "/Art/Meshes/OrbitRing_Outer.asset",
                Root + "/Art/Materials/M_Ring_Outer.mat",
                new Color(0.9f, 0.36f, 0.06f),
                turretPrefab);

            CreateDirectionalScene(enemyConfig, turretConfig, innerRing, middleRing, outerRing);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Anchor Defense directional Sprite scene rebuilt successfully.");
        }

        private static void ConfigureSpriteSheet()
        {
            AssetDatabase.ImportAsset(SpriteSheetPath, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(SpriteSheetPath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("Directional sprite sheet could not be imported.");
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = 128f;

            string[] rowNames = { "Turret", "Enemy", "Projectile" };
            var metadata = new SpriteMetaData[24];
            int index = 0;
            for (int row = 0; row < 3; row++)
            {
                for (int column = 0; column < 8; column++)
                {
                    metadata[index++] = new SpriteMetaData
                    {
                        name = rowNames[row] + "_" + DirectionNames[column],
                        rect = new Rect(column * 256f, (2 - row) * 256f, 256f, 256f),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f)
                    };
                }
            }

            importer.spritesheet = metadata;
            importer.SaveAndReimport();
        }

        private static Sprite[] LoadSpriteRow(string prefix)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(SpriteSheetPath);
            var spritesByName = new Dictionary<string, Sprite>();
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite)
                {
                    spritesByName[sprite.name] = sprite;
                }
            }

            var sprites = new Sprite[DirectionalSpriteSet.DirectionCount];
            for (int i = 0; i < sprites.Length; i++)
            {
                string spriteName = prefix + "_" + DirectionNames[i];
                if (!spritesByName.TryGetValue(spriteName, out sprites[i]))
                {
                    throw new InvalidOperationException($"Sprite '{spriteName}' was not found in the directional sprite sheet.");
                }
            }
            return sprites;
        }

        private static DirectionalSpriteSet CreateSpriteSet(string path, Sprite[] sprites)
        {
            DirectionalSpriteSet set = AssetDatabase.LoadAssetAtPath<DirectionalSpriteSet>(path);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<DirectionalSpriteSet>();
                AssetDatabase.CreateAsset(set, path);
            }
            set.Configure(sprites);
            EditorUtility.SetDirty(set);
            return set;
        }

        private static Material CreateDirectionalMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(DirectionalMaterialPath);
            Shader shader = Shader.Find("AnchorDefense/DirectionalSpriteDepth");
            if (shader == null)
            {
                throw new InvalidOperationException("Directional sprite depth shader was not found.");
            }

            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, DirectionalMaterialPath);
            }
            material.shader = shader;
            material.SetFloat("_Cutoff", 0.06f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject CreateDirectionalTurretPrefab(DirectionalSpriteSet set, Sprite firstSprite, Material spriteMaterial)
        {
            Material baseMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Art/Materials/M_Turret.mat");
            Material accentMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Art/Materials/M_TurretAccent.mat");
            GameObject root = new GameObject("Turret_Directional");
            TurretController controller = root.AddComponent<TurretController>();
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.22f, 0.12f);
            collider.size = new Vector3(1.2f, 1.5f, 1.15f);

            GameObject baseRoot = new GameObject("BaseRoot_3D");
            baseRoot.transform.SetParent(root.transform, false);
            CreatePrimitive("Mount", PrimitiveType.Cylinder, baseRoot.transform, Vector3.zero, new Vector3(0.58f, 0.14f, 0.58f), baseMaterial);
            CreatePrimitive("Energy Socket", PrimitiveType.Sphere, baseRoot.transform, new Vector3(0f, 0.18f, 0f), Vector3.one * 0.24f, accentMaterial);

            GameObject visual = new GameObject("VisualRoot", typeof(SpriteRenderer), typeof(DirectionalSpriteRenderer));
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.82f, 0f);
            SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
            renderer.sprite = firstSprite;
            renderer.sharedMaterial = spriteMaterial;
            renderer.sortingOrder = 10;
            DirectionalSpriteRenderer directional = visual.GetComponent<DirectionalSpriteRenderer>();
            directional.Configure(renderer, set);

            GameObject muzzle = new GameObject("MuzzlePoint");
            muzzle.transform.SetParent(root.transform, false);
            muzzle.transform.localPosition = new Vector3(0f, 0.55f, 1.05f);
            controller.ConfigureFirePoint(muzzle.transform);
            controller.ConfigureDirectionalVisual(directional);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, DirectionalPrefabFolder + "/Turret_Directional.prefab");
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static EnemyController CreateDirectionalEnemyPrefab(DirectionalSpriteSet set, Sprite firstSprite, Material spriteMaterial)
        {
            GameObject root = new GameObject("Enemy_Directional");
            EnemyController controller = root.AddComponent<EnemyController>();
            GameObject visual = new GameObject("VisualRoot", typeof(SpriteRenderer), typeof(DirectionalSpriteRenderer), typeof(Animator));
            visual.transform.SetParent(root.transform, false);
            SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
            renderer.sprite = firstSprite;
            renderer.sharedMaterial = spriteMaterial;
            renderer.sortingOrder = 8;
            DirectionalSpriteRenderer directional = visual.GetComponent<DirectionalSpriteRenderer>();
            directional.Configure(renderer, set);
            controller.ConfigureDirectionalVisual(directional);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, DirectionalPrefabFolder + "/Enemy_Directional.prefab");
            UnityEngine.Object.DestroyImmediate(root);
            return prefab.GetComponent<EnemyController>();
        }

        private static ProjectileController CreateDirectionalProjectilePrefab(DirectionalSpriteSet set, Sprite firstSprite, Material spriteMaterial)
        {
            Material trailMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Art/Materials/M_Projectile.mat");
            GameObject root = new GameObject("Projectile_Directional");
            GameObject visual = new GameObject("VisualRoot", typeof(SpriteRenderer), typeof(DirectionalSpriteRenderer));
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = Vector3.one * 0.18f;
            SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
            renderer.sprite = firstSprite;
            renderer.sharedMaterial = spriteMaterial;
            renderer.sortingOrder = 12;
            DirectionalSpriteRenderer directional = visual.GetComponent<DirectionalSpriteRenderer>();
            directional.Configure(renderer, set);

            TrailRenderer trail = root.AddComponent<TrailRenderer>();
            trail.time = 0.16f;
            trail.startWidth = 0.1f;
            trail.endWidth = 0f;
            trail.sharedMaterial = trailMaterial;
            trail.startColor = new Color(0.35f, 1f, 1f);
            trail.endColor = new Color(0.35f, 1f, 1f, 0f);
            ProjectileController controller = root.AddComponent<ProjectileController>();
            controller.Configure(trail);
            controller.ConfigureDirectionalVisual(directional);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, DirectionalPrefabFolder + "/Projectile_Directional.prefab");
            UnityEngine.Object.DestroyImmediate(root);
            return prefab.GetComponent<ProjectileController>();
        }

        private static GameObject CreateDirectionalRingPrefab(
            string objectName,
            float radius,
            string meshPath,
            string materialPath,
            Color color,
            GameObject turretPrefab)
        {
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            GameObject root = new GameObject(objectName);
            OrbitRingController controller = root.AddComponent<OrbitRingController>();
            GameObject visualRoot = new GameObject("VisualRoot");
            visualRoot.transform.SetParent(root.transform, false);
            GameObject ringMesh = new GameObject("Ring Mesh", typeof(MeshFilter), typeof(MeshRenderer));
            ringMesh.transform.SetParent(visualRoot.transform, false);
            ringMesh.GetComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer ringRenderer = ringMesh.GetComponent<MeshRenderer>();
            ringRenderer.sharedMaterial = material;

            var selectionRenderers = new List<Renderer> { ringRenderer };
            for (int i = 0; i < 16; i++)
            {
                float angle = i * Mathf.PI * 2f / 16f;
                Vector3 position = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                GameObject node = CreatePrimitive("Energy Node", PrimitiveType.Cube, visualRoot.transform, position, new Vector3(0.26f, 0.2f, 0.5f), material);
                node.transform.localRotation = Quaternion.LookRotation(position.normalized, Vector3.up);
                selectionRenderers.Add(node.GetComponent<Renderer>());
            }

            GameObject collidersRoot = new GameObject("SelectionColliders");
            collidersRoot.transform.SetParent(root.transform, false);
            float segmentLength = 2f * Mathf.PI * radius / 32f * 1.12f;
            for (int i = 0; i < 32; i++)
            {
                float angle = (i + 0.5f) * Mathf.PI * 2f / 32f;
                Vector3 position = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Vector3 tangent = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                GameObject segment = new GameObject("Ring Hit Area");
                segment.transform.SetParent(collidersRoot.transform, false);
                segment.transform.localPosition = position;
                segment.transform.localRotation = Quaternion.LookRotation(tangent, Vector3.up) * Quaternion.Euler(0f, 90f, 0f);
                BoxCollider collider = segment.AddComponent<BoxCollider>();
                collider.size = new Vector3(segmentLength, 0.42f, 0.42f);
            }

            GameObject turretsRoot = new GameObject("Turrets");
            turretsRoot.transform.SetParent(root.transform, false);
            for (int i = 0; i < 6; i++)
            {
                float angle = i * Mathf.PI * 2f / 6f;
                Vector3 position = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                GameObject turret = (GameObject)PrefabUtility.InstantiatePrefab(turretPrefab);
                turret.name = $"Directional Turret {i + 1:00}";
                turret.transform.SetParent(turretsRoot.transform, false);
                turret.transform.localPosition = position;
                turret.transform.localRotation = Quaternion.LookRotation(position.normalized, Vector3.up);
            }

            controller.Configure(selectionRenderers.ToArray(), color, Color.Lerp(color, Color.white, 0.65f));
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, DirectionalPrefabFolder + "/" + objectName + ".prefab");
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static void CreateDirectionalScene(
            EnemyConfig enemyConfig,
            TurretConfig turretConfig,
            GameObject innerRingPrefab,
            GameObject middleRingPrefab,
            GameObject outerRingPrefab)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(DirectionalScenePath) == null)
            {
                AssetDatabase.CopyAsset(GameplayScenePath, DirectionalScenePath);
            }

            Scene scene = EditorSceneManager.OpenScene(DirectionalScenePath, OpenSceneMode.Single);
            GameObject ringsRoot = FindRoot(scene, "World").transform.Find("Orbit Rings").gameObject;
            for (int i = ringsRoot.transform.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(ringsRoot.transform.GetChild(i).gameObject);
            }

            GameObject innerRing = (GameObject)PrefabUtility.InstantiatePrefab(innerRingPrefab, scene);
            innerRing.transform.SetParent(ringsRoot.transform, false);
            GameObject middleRing = (GameObject)PrefabUtility.InstantiatePrefab(middleRingPrefab, scene);
            middleRing.transform.SetParent(ringsRoot.transform, false);
            middleRing.transform.localRotation = Quaternion.Euler(58f, 0f, 18f);
            GameObject outerRing = (GameObject)PrefabUtility.InstantiatePrefab(outerRingPrefab, scene);
            outerRing.transform.SetParent(ringsRoot.transform, false);
            outerRing.transform.localRotation = Quaternion.Euler(-50f, 25f, -12f);

            GameObject systems = FindRoot(scene, "Systems");
            GameBootstrap bootstrap = systems.GetComponent<GameBootstrap>();
            GameFlowController flow = systems.GetComponent<GameFlowController>();
            EndlessEnemySpawner spawner = systems.GetComponent<EndlessEnemySpawner>();
            RingInputController ringInput = systems.GetComponent<RingInputController>();
            Transform poolRoot = systems.transform.Find("Object Pools");
            CoreHealth core = FindRoot(scene, "World").GetComponentInChildren<CoreHealth>(true);
            HudController hud = UnityEngine.Object.FindObjectOfType<HudController>(true);
            Camera camera = Camera.main;
            CoreConfig coreConfig = AssetDatabase.LoadAssetAtPath<CoreConfig>(Root + "/Configs/CoreConfig.asset");
            EndlessModeConfig endlessConfig = AssetDatabase.LoadAssetAtPath<EndlessModeConfig>(Root + "/Configs/EndlessModeConfig.asset");
            bootstrap.Configure(coreConfig, enemyConfig, turretConfig, endlessConfig, camera, core, flow, spawner, ringInput, hud, poolRoot);

            DirectionalSpriteRenderer[] visuals = UnityEngine.Object.FindObjectsOfType<DirectionalSpriteRenderer>(true);
            for (int i = 0; i < visuals.Length; i++)
            {
                visuals[i].transform.rotation = camera.transform.rotation;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, DirectionalScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(GameplayScenePath, true),
                new EditorBuildSettingsScene(DirectionalScenePath, true),
                new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity", true)
            };
        }

        private static GameObject FindRoot(Scene scene, string objectName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == objectName)
                {
                    return roots[i];
                }
            }
            throw new InvalidOperationException($"Root object '{objectName}' was not found in scene '{scene.path}'.");
        }

        private static T CopyConfig<T>(string sourcePath, string destinationPath) where T : ScriptableObject
        {
            T source = AssetDatabase.LoadAssetAtPath<T>(sourcePath);
            T destination = AssetDatabase.LoadAssetAtPath<T>(destinationPath);
            if (destination == null)
            {
                destination = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(destination, destinationPath);
            }
            EditorUtility.CopySerialized(source, destination);
            destination.name = System.IO.Path.GetFileNameWithoutExtension(destinationPath);
            EditorUtility.SetDirty(destination);
            return destination;
        }

        private static void SetObjectReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            var serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Serialized property '{propertyName}' was not found on {target.GetType().Name}.");
            }
            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static GameObject CreatePrimitive(
            string objectName,
            PrimitiveType primitiveType,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            GameObject primitive = GameObject.CreatePrimitive(primitiveType);
            primitive.name = objectName;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = localPosition;
            primitive.transform.localScale = localScale;
            Collider collider = primitive.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
            primitive.GetComponent<Renderer>().sharedMaterial = material;
            return primitive;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
    }
}

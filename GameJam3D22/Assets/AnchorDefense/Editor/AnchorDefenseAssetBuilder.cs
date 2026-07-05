using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AnchorDefense.Editor
{
    public static class AnchorDefenseAssetBuilder
    {
        private const string Root = "Assets/AnchorDefense";
        private const string Art = Root + "/Art";
        private const string Meshes = Art + "/Meshes";
        private const string Materials = Art + "/Materials";
        private const string Configs = Root + "/Configs";
        private const string Prefabs = Root + "/Prefabs";
        private const string EnvironmentPrefabs = Prefabs + "/Environment";
        private const string GameplayPrefabs = Prefabs + "/Gameplay";
        private const string VfxPrefabs = Prefabs + "/VFX";
        private const string UiPrefabs = Prefabs + "/UI";
        private const string Scenes = Root + "/Scenes";
        private const string GameplayScenePath = Scenes + "/Gameplay.unity";

        [MenuItem("Tools/Anchor Defense/Rebuild Editable Assets and Gameplay Scene")]
        public static void BuildAll()
        {
            EnsureFolders();

            Material coreMaterial = CreateMaterial(Materials + "/M_CorePlanet.mat", "Universal Render Pipeline/Lit", new Color(0.05f, 0.3f, 0.88f), new Color(0.03f, 0.38f, 1.5f));
            Material coreAccentMaterial = CreateMaterial(Materials + "/M_CoreAccent.mat", "Universal Render Pipeline/Lit", new Color(0.12f, 0.9f, 1f), new Color(0.1f, 1.8f, 2.4f));
            Material enemyMaterial = CreateMaterial(Materials + "/M_Enemy.mat", "Universal Render Pipeline/Lit", new Color(0.75f, 0.035f, 0.09f), new Color(1.4f, 0.02f, 0.08f));
            Material enemyAccentMaterial = CreateMaterial(Materials + "/M_EnemyAccent.mat", "Universal Render Pipeline/Lit", new Color(1f, 0.28f, 0.12f), new Color(2.2f, 0.18f, 0.04f));
            Material turretMaterial = CreateMaterial(Materials + "/M_Turret.mat", "Universal Render Pipeline/Lit", new Color(0.08f, 0.32f, 0.45f), new Color(0.02f, 0.35f, 0.5f));
            Material turretAccentMaterial = CreateMaterial(Materials + "/M_TurretAccent.mat", "Universal Render Pipeline/Lit", new Color(0.15f, 0.95f, 1f), new Color(0.1f, 2.2f, 2.5f));
            Material projectileMaterial = CreateMaterial(Materials + "/M_Projectile.mat", "Universal Render Pipeline/Unlit", new Color(0.25f, 1f, 1f), new Color(0.25f, 1f, 1f));
            Material particleMaterial = CreateMaterial(Materials + "/M_Particles.mat", "Universal Render Pipeline/Particles/Unlit", Color.white, Color.white);
            Material ringInnerMaterial = CreateMaterial(Materials + "/M_Ring_Inner.mat", "Universal Render Pipeline/Lit", new Color(0.08f, 0.55f, 0.78f), new Color(0.02f, 0.9f, 1.6f));
            Material ringMiddleMaterial = CreateMaterial(Materials + "/M_Ring_Middle.mat", "Universal Render Pipeline/Lit", new Color(0.42f, 0.12f, 0.68f), new Color(0.9f, 0.08f, 1.8f));
            Material ringOuterMaterial = CreateMaterial(Materials + "/M_Ring_Outer.mat", "Universal Render Pipeline/Lit", new Color(0.72f, 0.27f, 0.04f), new Color(1.8f, 0.35f, 0.02f));

            Mesh coreOrbitMesh = SaveMesh(Meshes + "/CoreOrbit.asset", CreateTorusMesh("CoreOrbit", 2.35f, 0.035f, 80, 6));
            Mesh innerRingMesh = SaveMesh(Meshes + "/OrbitRing_Inner.asset", CreateTorusMesh("OrbitRing_Inner", 4.7f, 0.09f, 112, 10));
            Mesh middleRingMesh = SaveMesh(Meshes + "/OrbitRing_Middle.asset", CreateTorusMesh("OrbitRing_Middle", 6.1f, 0.09f, 128, 10));
            Mesh outerRingMesh = SaveMesh(Meshes + "/OrbitRing_Outer.asset", CreateTorusMesh("OrbitRing_Outer", 7.5f, 0.1f, 144, 10));

            PooledParticleEffect hitEffectPrefab = CreateParticlePrefab("HitEffect", VfxPrefabs + "/HitEffect.prefab", particleMaterial, 0.42f, 0.1f);
            PooledParticleEffect deathEffectPrefab = CreateParticlePrefab("DeathEffect", VfxPrefabs + "/DeathEffect.prefab", particleMaterial, 0.65f, 0.16f);
            PooledParticleEffect muzzleEffectPrefab = CreateParticlePrefab("MuzzleEffect", VfxPrefabs + "/MuzzleEffect.prefab", particleMaterial, 0.22f, 0.08f);
            ProjectileController projectilePrefab = CreateProjectilePrefab(projectileMaterial);
            EnemyController enemyPrefab = CreateEnemyPrefab(enemyMaterial, enemyAccentMaterial);
            GameObject turretPrefab = CreateTurretPrefab(turretMaterial, turretAccentMaterial);
            GameObject corePrefab = CreateCorePrefab(coreMaterial, coreAccentMaterial, coreOrbitMesh);

            GameObject innerRingPrefab = CreateRingPrefab("OrbitRing_Inner", GameplayPrefabs + "/OrbitRing_Inner.prefab", OrbitRingId.Inner, 4.7f, innerRingMesh, ringInnerMaterial, turretPrefab, new Color(0.08f, 0.62f, 0.9f));
            GameObject middleRingPrefab = CreateRingPrefab("OrbitRing_Middle", GameplayPrefabs + "/OrbitRing_Middle.prefab", OrbitRingId.Middle, 6.1f, middleRingMesh, ringMiddleMaterial, turretPrefab, new Color(0.55f, 0.16f, 0.86f));
            GameObject outerRingPrefab = CreateRingPrefab("OrbitRing_Outer", GameplayPrefabs + "/OrbitRing_Outer.prefab", OrbitRingId.Outer, 7.5f, outerRingMesh, ringOuterMaterial, turretPrefab, new Color(0.9f, 0.36f, 0.06f));
            GameObject starFieldPrefab = CreateStarFieldPrefab(particleMaterial);
            GameObject hudPrefab = CreateHudPrefab();

            CoreConfig coreConfig = CreateConfigAsset<CoreConfig>(Configs + "/CoreConfig.asset");
            EnemyConfig enemyConfig = CreateConfigAsset<EnemyConfig>(Configs + "/EnemyConfig.asset");
            TurretConfig turretConfig = CreateConfigAsset<TurretConfig>(Configs + "/TurretConfig.asset");
            EndlessModeConfig endlessConfig = CreateConfigAsset<EndlessModeConfig>(Configs + "/EndlessModeConfig.asset");
            SetObjectReference(enemyConfig, "<Prefab>k__BackingField", enemyPrefab);
            SetObjectReference(enemyConfig, "<HitEffectPrefab>k__BackingField", hitEffectPrefab);
            SetObjectReference(enemyConfig, "<DeathEffectPrefab>k__BackingField", deathEffectPrefab);
            SetObjectReference(turretConfig, "<ProjectilePrefab>k__BackingField", projectilePrefab);
            SetObjectReference(turretConfig, "<MuzzleEffectPrefab>k__BackingField", muzzleEffectPrefab);

            CreateGameplayScene(
                coreConfig,
                enemyConfig,
                turretConfig,
                endlessConfig,
                corePrefab,
                innerRingPrefab,
                middleRingPrefab,
                outerRingPrefab,
                starFieldPrefab,
                hudPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            AnchorDefenseUpgradeBuilder.RepairAfterGameplayRebuild();
            AnchorDefenseCombatVarietyBuilder.BuildAll();
            AnchorDefenseSingleSpriteEnemyBuilder.BuildAll();
            AnchorDefenseProjectileFusionBuilder.BuildAll();
            Debug.Log("Anchor Defense editable assets and Gameplay scene rebuilt successfully.");
        }

        private static void EnsureFolders()
        {
            EnsureFolder(Art);
            EnsureFolder(Meshes);
            EnsureFolder(Materials);
            EnsureFolder(Configs);
            EnsureFolder(Prefabs);
            EnsureFolder(EnvironmentPrefabs);
            EnsureFolder(GameplayPrefabs);
            EnsureFolder(VfxPrefabs);
            EnsureFolder(UiPrefabs);
            EnsureFolder(Scenes);
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

        private static Material CreateMaterial(string path, string shaderName, Color baseColor, Color emissionColor)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }

            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            material.name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", baseColor);
            }
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", baseColor);
            }
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emissionColor);
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Mesh SaveMesh(string path, Mesh mesh)
        {
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, path);
                return mesh;
            }

            EditorUtility.CopySerialized(mesh, existing);
            UnityEngine.Object.DestroyImmediate(mesh);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static Mesh CreateTorusMesh(string meshName, float majorRadius, float tubeRadius, int majorSegments, int tubeSegments)
        {
            var vertices = new Vector3[majorSegments * tubeSegments];
            var normals = new Vector3[vertices.Length];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[majorSegments * tubeSegments * 6];

            for (int major = 0; major < majorSegments; major++)
            {
                float u = major * Mathf.PI * 2f / majorSegments;
                Vector3 radial = new Vector3(Mathf.Cos(u), 0f, Mathf.Sin(u));
                Vector3 center = radial * majorRadius;
                for (int tube = 0; tube < tubeSegments; tube++)
                {
                    float v = tube * Mathf.PI * 2f / tubeSegments;
                    Vector3 normal = radial * Mathf.Cos(v) + Vector3.up * Mathf.Sin(v);
                    int index = major * tubeSegments + tube;
                    vertices[index] = center + normal * tubeRadius;
                    normals[index] = normal;
                    uv[index] = new Vector2((float)major / majorSegments, (float)tube / tubeSegments);
                }
            }

            int triangleIndex = 0;
            for (int major = 0; major < majorSegments; major++)
            {
                int nextMajor = (major + 1) % majorSegments;
                for (int tube = 0; tube < tubeSegments; tube++)
                {
                    int nextTube = (tube + 1) % tubeSegments;
                    int a = major * tubeSegments + tube;
                    int b = nextMajor * tubeSegments + tube;
                    int c = nextMajor * tubeSegments + nextTube;
                    int d = major * tubeSegments + nextTube;
                    triangles[triangleIndex++] = a;
                    triangles[triangleIndex++] = b;
                    triangles[triangleIndex++] = c;
                    triangles[triangleIndex++] = a;
                    triangles[triangleIndex++] = c;
                    triangles[triangleIndex++] = d;
                }
            }

            var mesh = new Mesh { name = meshName, vertices = vertices, normals = normals, uv = uv, triangles = triangles };
            mesh.RecalculateBounds();
            return mesh;
        }

        private static PooledParticleEffect CreateParticlePrefab(string objectName, string path, Material material, float lifetime, float size)
        {
            GameObject root = new GameObject(objectName);
            ParticleSystem particles = root.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = lifetime;
            main.startSpeed = 0f;
            main.startSize = size;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 64;
            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = false;
            particles.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;

            PooledParticleEffect effect = root.AddComponent<PooledParticleEffect>();
            effect.Configure(particles);
            AssignUiFonts(root);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab.GetComponent<PooledParticleEffect>();
        }

        private static ProjectileController CreateProjectilePrefab(Material material)
        {
            GameObject root = new GameObject("Projectile");
            GameObject visual = CreatePrimitive("VisualRoot", PrimitiveType.Sphere, root.transform, Vector3.zero, Vector3.one * 0.16f, material);
            visual.isStatic = false;
            TrailRenderer trail = root.AddComponent<TrailRenderer>();
            trail.time = 0.18f;
            trail.startWidth = 0.11f;
            trail.endWidth = 0f;
            trail.sharedMaterial = material;
            trail.startColor = new Color(0.35f, 1f, 1f);
            trail.endColor = new Color(0.35f, 1f, 1f, 0f);
            Light light = root.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 2.4f;
            light.intensity = 2.5f;
            light.color = new Color(0.25f, 1f, 1f);
            light.shadows = LightShadows.None;
            ProjectileController controller = root.AddComponent<ProjectileController>();
            controller.Configure(trail);
            AssignUiFonts(root);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, GameplayPrefabs + "/Projectile.prefab");
            UnityEngine.Object.DestroyImmediate(root);
            return prefab.GetComponent<ProjectileController>();
        }

        private static EnemyController CreateEnemyPrefab(Material bodyMaterial, Material accentMaterial)
        {
            GameObject root = new GameObject("Enemy");
            EnemyController controller = root.AddComponent<EnemyController>();
            GameObject visualRoot = new GameObject("VisualRoot");
            visualRoot.transform.SetParent(root.transform, false);
            visualRoot.AddComponent<Animator>();
            CreatePrimitive("Body", PrimitiveType.Cube, visualRoot.transform, Vector3.zero, new Vector3(1f, 0.72f, 1.25f), bodyMaterial);
            CreatePrimitive("Front Core", PrimitiveType.Sphere, visualRoot.transform, new Vector3(0f, 0f, 0.62f), Vector3.one * 0.36f, accentMaterial);
            CreatePrimitive("Left Fin", PrimitiveType.Cube, visualRoot.transform, new Vector3(-0.66f, 0f, -0.08f), new Vector3(0.42f, 0.14f, 0.72f), accentMaterial, Quaternion.Euler(0f, 16f, 18f));
            CreatePrimitive("Right Fin", PrimitiveType.Cube, visualRoot.transform, new Vector3(0.66f, 0f, -0.08f), new Vector3(0.42f, 0.14f, 0.72f), accentMaterial, Quaternion.Euler(0f, -16f, -18f));
            AssignUiFonts(root);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, GameplayPrefabs + "/Enemy.prefab");
            UnityEngine.Object.DestroyImmediate(root);
            return prefab.GetComponent<EnemyController>();
        }

        private static GameObject CreateTurretPrefab(Material bodyMaterial, Material accentMaterial)
        {
            GameObject root = new GameObject("Turret");
            TurretController controller = root.AddComponent<TurretController>();
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.2f, 0.2f);
            collider.size = new Vector3(0.9f, 0.65f, 1.35f);

            GameObject visualRoot = new GameObject("VisualRoot");
            visualRoot.transform.SetParent(root.transform, false);
            CreatePrimitive("Base", PrimitiveType.Cylinder, visualRoot.transform, Vector3.zero, new Vector3(0.58f, 0.14f, 0.58f), bodyMaterial);
            CreatePrimitive("Housing", PrimitiveType.Cube, visualRoot.transform, new Vector3(0f, 0.22f, 0.12f), new Vector3(0.62f, 0.38f, 0.72f), bodyMaterial);
            CreatePrimitive("Energy Core", PrimitiveType.Sphere, visualRoot.transform, new Vector3(0f, 0.27f, 0.18f), Vector3.one * 0.28f, accentMaterial);
            CreatePrimitive("Left Barrel", PrimitiveType.Cylinder, visualRoot.transform, new Vector3(-0.17f, 0.32f, 0.63f), new Vector3(0.075f, 0.42f, 0.075f), accentMaterial, Quaternion.Euler(90f, 0f, 0f));
            CreatePrimitive("Right Barrel", PrimitiveType.Cylinder, visualRoot.transform, new Vector3(0.17f, 0.32f, 0.63f), new Vector3(0.075f, 0.42f, 0.075f), accentMaterial, Quaternion.Euler(90f, 0f, 0f));

            GameObject muzzle = new GameObject("MuzzlePoint");
            muzzle.transform.SetParent(root.transform, false);
            muzzle.transform.localPosition = new Vector3(0f, 0.32f, 1.08f);
            controller.ConfigureFirePoint(muzzle.transform);
            AssignUiFonts(root);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, GameplayPrefabs + "/Turret.prefab");
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateCorePrefab(Material coreMaterial, Material accentMaterial, Mesh orbitMesh)
        {
            GameObject root = new GameObject("CorePlanet");
            root.AddComponent<CoreHealth>();
            SphereCollider collider = root.AddComponent<SphereCollider>();
            collider.radius = 2f;
            GameObject visualRoot = new GameObject("VisualRoot");
            visualRoot.transform.SetParent(root.transform, false);
            CreatePrimitive("Planet Body", PrimitiveType.Sphere, visualRoot.transform, Vector3.zero, Vector3.one * 4f, coreMaterial);
            CreatePrimitive("North Anchor", PrimitiveType.Sphere, visualRoot.transform, new Vector3(0f, 1.55f, 0f), new Vector3(0.9f, 0.35f, 0.9f), accentMaterial);
            CreatePrimitive("South Anchor", PrimitiveType.Sphere, visualRoot.transform, new Vector3(0f, -1.55f, 0f), new Vector3(0.9f, 0.35f, 0.9f), accentMaterial);

            for (int i = 0; i < 2; i++)
            {
                GameObject orbit = new GameObject(i == 0 ? "Core Orbit A" : "Core Orbit B", typeof(MeshFilter), typeof(MeshRenderer));
                orbit.transform.SetParent(visualRoot.transform, false);
                orbit.transform.localRotation = Quaternion.Euler(i == 0 ? new Vector3(68f, 0f, 24f) : new Vector3(-54f, 35f, 0f));
                orbit.GetComponent<MeshFilter>().sharedMesh = orbitMesh;
                orbit.GetComponent<MeshRenderer>().sharedMaterial = accentMaterial;
            }

            GameObject lightObject = new GameObject("Core Glow Light");
            lightObject.transform.SetParent(root.transform, false);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 10f;
            light.intensity = 2.4f;
            light.color = new Color(0.08f, 0.55f, 1f);
            light.shadows = LightShadows.None;
            AssignUiFonts(root);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, GameplayPrefabs + "/CorePlanet.prefab");
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateRingPrefab(string objectName, string path, OrbitRingId ringId, float radius, Mesh mesh, Material material, GameObject turretPrefab, Color color)
        {
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
            var turretSlots = new List<TurretSlot>();
            for (int i = 0; i < 6; i++)
            {
                float angle = i * Mathf.PI * 2f / 6f;
                Vector3 position = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                GameObject slotObject = new GameObject($"Turret Slot {i + 1:00}");
                slotObject.transform.SetParent(turretsRoot.transform, false);
                slotObject.transform.localPosition = position;
                slotObject.transform.localRotation = Quaternion.LookRotation(position.normalized, Vector3.up);
                TurretSlot slot = slotObject.AddComponent<TurretSlot>();
                slot.Configure(turretPrefab.GetComponent<TurretController>(), true);
                turretSlots.Add(slot);
            }

            controller.Configure(selectionRenderers.ToArray(), color, Color.Lerp(color, Color.white, 0.65f));
            controller.ConfigureTurretSlotAssets(ringId, turretSlots.ToArray(), new TurretSlot[0]);
            AssignUiFonts(root);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateStarFieldPrefab(Material material)
        {
            GameObject root = new GameObject("StarField");
            ParticleSystem stars = root.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = stars.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = 45f;
            main.startSpeed = 0.02f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.11f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.45f, 0.6f, 1f), Color.white);
            main.maxParticles = 650;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            ParticleSystem.EmissionModule emission = stars.emission;
            emission.rateOverTime = 16f;
            ParticleSystem.ShapeModule shape = stars.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 36f;
            shape.radiusThickness = 1f;
            stars.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
            AssignUiFonts(root);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, EnvironmentPrefabs + "/StarField.prefab");
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateHudPrefab()
        {
            Font font = LoadEnglishUiFont();
            GameObject canvasObject = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Text title = CreateText("Title", canvasObject.transform, font, 30, TextAnchor.UpperCenter, new Color(0.55f, 0.9f, 1f));
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-260f, -66f), new Vector2(260f, -18f));
            title.text = "ANCHOR DEFENSE";

            Text healthText = CreateText("Core Health", canvasObject.transform, font, 25, TextAnchor.MiddleLeft, Color.white);
            SetRect(healthText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(34f, -90f), new Vector2(370f, -50f));
            healthText.text = "CORE  100 / 100";
            Image healthBackground = CreateImage("Health Bar Background", canvasObject.transform, new Color(0.03f, 0.05f, 0.1f, 0.9f));
            SetRect(healthBackground.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(34f, -120f), new Vector2(370f, -99f));
            Image healthFill = CreateImage("Health Bar Fill", healthBackground.transform, new Color(0.15f, 0.75f, 1f));
            SetRect(healthFill.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            healthFill.type = Image.Type.Filled;
            healthFill.fillMethod = Image.FillMethod.Horizontal;

            Text timeText = CreateText("Elapsed Time", canvasObject.transform, font, 24, TextAnchor.MiddleRight, Color.white);
            SetRect(timeText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-380f, -86f), new Vector2(-34f, -48f));
            timeText.text = "SURVIVAL  00:00";
            Text enemyText = CreateText("Enemy Count", canvasObject.transform, font, 22, TextAnchor.MiddleRight, new Color(1f, 0.45f, 0.48f));
            SetRect(enemyText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-380f, -122f), new Vector2(-34f, -88f));
            enemyText.text = "ENEMIES  0";

            Text hint = CreateText("Controls", canvasObject.transform, font, 20, TextAnchor.LowerCenter, new Color(0.72f, 0.82f, 0.95f, 0.9f));
            SetRect(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-520f, 22f), new Vector2(520f, 64f));
            hint.text = "CLICK A RING  /  HOLD LEFT MOUSE AND DRAG TO ROTATE";

            Image gameOverPanel = CreateImage("Game Over Panel", canvasObject.transform, new Color(0.015f, 0.02f, 0.06f, 0.94f));
            SetRect(gameOverPanel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-260f, -175f), new Vector2(260f, 175f));
            Text gameOverTitle = CreateText("Game Over", gameOverPanel.transform, font, 46, TextAnchor.MiddleCenter, new Color(1f, 0.3f, 0.38f));
            SetRect(gameOverTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -100f), new Vector2(-20f, -32f));
            gameOverTitle.text = "CORE LOST";
            Text finalTime = CreateText("Final Time", gameOverPanel.transform, font, 25, TextAnchor.MiddleCenter, Color.white);
            SetRect(finalTime.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(20f, -28f), new Vector2(-20f, 28f));
            finalTime.text = "SURVIVED 00:00";
            Image buttonImage = CreateImage("Restart Button", gameOverPanel.transform, new Color(0.12f, 0.55f, 0.85f, 1f));
            SetRect(buttonImage.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-115f, 32f), new Vector2(115f, 90f));
            Button restartButton = buttonImage.gameObject.AddComponent<Button>();
            restartButton.targetGraphic = buttonImage;
            Text restartText = CreateText("Restart Text", buttonImage.transform, font, 24, TextAnchor.MiddleCenter, Color.white);
            SetRect(restartText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            restartText.text = "RESTART";

            HudController hud = canvasObject.AddComponent<HudController>();
            hud.ConfigureView(healthText, timeText, enemyText, finalTime, healthFill, gameOverPanel.gameObject, restartButton);
            AssignUiFonts(canvasObject);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(canvasObject, UiPrefabs + "/HUD.prefab");
            UnityEngine.Object.DestroyImmediate(canvasObject);
            return prefab;
        }

        private static T CreateConfigAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            EditorUtility.SetDirty(asset);
            return asset;
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

        private static void CreateGameplayScene(
            CoreConfig coreConfig,
            EnemyConfig enemyConfig,
            TurretConfig turretConfig,
            EndlessModeConfig endlessConfig,
            GameObject corePrefab,
            GameObject innerRingPrefab,
            GameObject middleRingPrefab,
            GameObject outerRingPrefab,
            GameObject starFieldPrefab,
            GameObject hudPrefab)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Gameplay";
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.075f, 0.095f, 0.17f);

            GameObject environment = new GameObject("Environment");
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.transform.SetParent(environment.transform, false);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 10.5f, -22f);
            cameraObject.transform.rotation = Quaternion.LookRotation(-cameraObject.transform.position.normalized, Vector3.up);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 52f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 120f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.002f, 0.004f, 0.018f);
            cameraObject.AddComponent<AudioListener>();

            GameObject lightObject = new GameObject("Directional Light");
            lightObject.transform.SetParent(environment.transform, false);
            lightObject.transform.rotation = Quaternion.Euler(38f, -32f, 0f);
            Light directionalLight = lightObject.AddComponent<Light>();
            directionalLight.type = LightType.Directional;
            directionalLight.color = new Color(0.65f, 0.78f, 1f);
            directionalLight.intensity = 1.35f;
            directionalLight.shadows = LightShadows.Soft;
            GameObject starField = (GameObject)PrefabUtility.InstantiatePrefab(starFieldPrefab);
            starField.transform.SetParent(environment.transform, false);

            GameObject world = new GameObject("World");
            GameObject coreObject = (GameObject)PrefabUtility.InstantiatePrefab(corePrefab);
            coreObject.transform.SetParent(world.transform, false);
            CoreHealth core = coreObject.GetComponent<CoreHealth>();
            GameObject ringsRoot = new GameObject("Orbit Rings");
            ringsRoot.transform.SetParent(world.transform, false);
            GameObject innerRing = (GameObject)PrefabUtility.InstantiatePrefab(innerRingPrefab);
            innerRing.transform.SetParent(ringsRoot.transform, false);
            GameObject middleRing = (GameObject)PrefabUtility.InstantiatePrefab(middleRingPrefab);
            middleRing.transform.SetParent(ringsRoot.transform, false);
            middleRing.transform.localRotation = Quaternion.Euler(58f, 0f, 18f);
            GameObject outerRing = (GameObject)PrefabUtility.InstantiatePrefab(outerRingPrefab);
            outerRing.transform.SetParent(ringsRoot.transform, false);
            outerRing.transform.localRotation = Quaternion.Euler(-50f, 25f, -12f);

            GameObject systems = new GameObject("Systems");
            GameFlowController gameFlow = systems.AddComponent<GameFlowController>();
            EndlessEnemySpawner spawner = systems.AddComponent<EndlessEnemySpawner>();
            RingInputController ringInput = systems.AddComponent<RingInputController>();
            GameBootstrap bootstrap = systems.AddComponent<GameBootstrap>();
            GameObject poolRootObject = new GameObject("Object Pools");
            poolRootObject.transform.SetParent(systems.transform, false);

            GameObject hudObject = (GameObject)PrefabUtility.InstantiatePrefab(hudPrefab);
            HudController hud = hudObject.GetComponent<HudController>();
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            bootstrap.Configure(
                coreConfig,
                enemyConfig,
                turretConfig,
                endlessConfig,
                camera,
                core,
                gameFlow,
                spawner,
                ringInput,
                hud,
                poolRootObject.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, GameplayScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(GameplayScenePath, true),
                new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity", true)
            };
        }

        private static GameObject CreatePrimitive(string objectName, PrimitiveType primitiveType, Transform parent, Vector3 localPosition, Vector3 localScale, Material material, Quaternion? localRotation = null)
        {
            GameObject primitive = GameObject.CreatePrimitive(primitiveType);
            primitive.name = objectName;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = localPosition;
            primitive.transform.localScale = localScale;
            primitive.transform.localRotation = localRotation ?? Quaternion.identity;
            Collider collider = primitive.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
            primitive.GetComponent<Renderer>().sharedMaterial = material;
            return primitive;
        }

        private static Text CreateText(string objectName, Transform parent, Font font, int size, TextAnchor alignment, Color color)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            return text;
        }

        private static Image CreateImage(string objectName, Transform parent, Color color)
        {
            GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
        private const string EnglishUiFontPath = "Assets/AnchorDefense/Art/UI/DomeaScrawl-Regular.ttf";
        private const string ChineseUiFontPath = "Assets/AnchorDefense/Art/UI/PF频凡胡涂体 PFANHUTUTI.ttf";

        private static Font LoadEnglishUiFont()
        {
            Font font = AssetDatabase.LoadAssetAtPath<Font>(EnglishUiFontPath);
            return font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private static Font LoadChineseUiFont()
        {
            Font font = AssetDatabase.LoadAssetAtPath<Font>(ChineseUiFontPath);
            return font != null ? font : LoadEnglishUiFont();
        }

        private static void AssignUiFonts(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            Font english = LoadEnglishUiFont();
            Font chinese = LoadChineseUiFont();
            Text[] texts = root.GetComponentsInChildren<Text>(true);
            foreach (Text text in texts)
            {
                string value = string.IsNullOrEmpty(text.text) ? GetTransformPath(text.transform) : text.text;
                text.font = ContainsCjk(value) ? chinese : english;
                EditorUtility.SetDirty(text);
            }
        }

        private static bool ContainsCjk(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            foreach (char c in value)
            {
                if ((c >= 0x3400 && c <= 0x4DBF) ||
                    (c >= 0x4E00 && c <= 0x9FFF) ||
                    (c >= 0xF900 && c <= 0xFAFF))
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetTransformPath(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }

            return path;
        }
    }
}

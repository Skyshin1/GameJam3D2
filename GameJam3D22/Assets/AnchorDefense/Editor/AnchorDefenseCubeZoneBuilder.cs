using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AnchorDefense.Editor
{
    public static class AnchorDefenseCubeZoneBuilder
    {
        private const string Root = "Assets/AnchorDefense";
        private const string ZoneConfigFolder = Root + "/Configs/Zones";
        private const string BlueEffectPath = ZoneConfigFolder + "/BlueTurretAcceleration.asset";
        private const string RedEffectPath = ZoneConfigFolder + "/RedEnemySuppression.asset";
        private const string ConfigPath = ZoneConfigFolder + "/CubeZoneConfig.asset";
        private const string MaterialPath = Root + "/Art/Materials/M_CubeZoneVolume.mat";
        private const string GridPrefabPath = Root + "/Prefabs/Zones/CubeZoneGrid.prefab";
        private const string UpgradeUiPath = Root + "/Prefabs/UI/UpgradeTreeUI.prefab";
        private const string GameplayScenePath = Root + "/Scenes/Gameplay.unity";

        private static readonly Color DarkPanel = new Color(0.015f, 0.03f, 0.065f, 0.985f);
        private static readonly Color Cyan = new Color(0.12f, 0.82f, 1f, 1f);
        private static readonly Color TextColor = new Color(0.82f, 0.94f, 1f, 1f);

        [MenuItem("Tools/Anchor Defense/Build 2x2x2 Cube Zones")]
        public static void BuildAll()
        {
            EnsureFolder(Root + "/Configs", "Zones");
            EnsureFolder(Root + "/Prefabs", "Zones");
            CubeZoneConfig config = CreateConfigAssets();
            Material material = CreateZoneMaterial();
            GameObject gridPrefab = CreateGridPrefab(config, material);
            PatchUpgradeUi(config);
            PatchGameplayScene(gridPrefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Anchor Defense 2x2x2 cube zones and drag assignment UI built successfully.");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static CubeZoneConfig CreateConfigAssets()
        {
            CubeZoneEffectDefinition blue = CreateOrLoad<CubeZoneEffectDefinition>(BlueEffectPath);
            blue.Configure("zone.turret.acceleration", "蓝色：炮塔超频",
                "区域内炮塔射击间隔缩短 30%。", new Color(0.08f, 0.48f, 1f, 0.12f),
                CubeZoneEffectType.TurretFireRateBoost, 0.7f, 1f, 0f);
            CubeZoneEffectDefinition red = CreateOrLoad<CubeZoneEffectDefinition>(RedEffectPath);
            red.Configure("zone.enemy.suppression", "红色：敌军抑制",
                "区域内敌人移动速度降低 35%，并每秒受到 4 点伤害。", new Color(1f, 0.08f, 0.12f, 0.12f),
                CubeZoneEffectType.EnemySlowAndDamage, 1f, 0.65f, 4f);
            EditorUtility.SetDirty(blue);
            EditorUtility.SetDirty(red);

            CubeZoneEffectDefinition[] defaults = new CubeZoneEffectDefinition[CubeZoneConfig.ZoneCount];
            for (int i = 0; i < defaults.Length; i++)
            {
                defaults[i] = (i & 1) == 0 ? blue : red;
            }
            CubeZoneConfig config = CreateOrLoad<CubeZoneConfig>(ConfigPath);
            config.Configure(10.5f, new[] { blue, red }, defaults);
            EditorUtility.SetDirty(config);
            return config;
        }

        private static Material CreateZoneMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
                material = new Material(shader) { name = "M_CubeZoneVolume" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            Color color = new Color(1f, 1f, 1f, 0.08f);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject CreateGridPrefab(CubeZoneConfig config, Material material)
        {
            GameObject root = new GameObject("CubeZoneGrid", typeof(CubeZoneGridController));
            float cubeSize = config.GridHalfExtent;
            float centerOffset = cubeSize * 0.5f;
            var volumes = new List<CubeZoneVolume>(CubeZoneConfig.ZoneCount);
            for (int i = 0; i < CubeZoneConfig.ZoneCount; i++)
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = $"Zone {i + 1:00}";
                cube.transform.SetParent(root.transform, false);
                cube.transform.localPosition = new Vector3(
                    (i & 1) != 0 ? centerOffset : -centerOffset,
                    (i & 2) != 0 ? centerOffset : -centerOffset,
                    (i & 4) != 0 ? centerOffset : -centerOffset);
                cube.transform.localScale = Vector3.one * cubeSize;
                MeshRenderer renderer = cube.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                BoxCollider collider = cube.GetComponent<BoxCollider>();
                collider.isTrigger = true;
                CubeZoneVolume volume = cube.AddComponent<CubeZoneVolume>();
                volume.Configure(i, renderer, collider);
                volume.SetEffect(config.GetDefaultEffect(i));
                volumes.Add(volume);
            }
            root.GetComponent<CubeZoneGridController>().Configure(config, volumes.ToArray());
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, GridPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void PatchUpgradeUi(CubeZoneConfig config)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(UpgradeUiPath) == null)
            {
                return;
            }
            GameObject root = PrefabUtility.LoadPrefabContents(UpgradeUiPath);
            if (root.GetComponent<CubeZoneAssignmentController>() != null)
            {
                PrefabUtility.UnloadPrefabContents(root);
                return;
            }
            Transform frame = root.transform.Find("Upgrade Tree Panel/Anchor Protocol Frame");
            if (frame == null)
            {
                PrefabUtility.UnloadPrefabContents(root);
                return;
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Button openButton = CreateButton("Zone Assignment", frame, font, "区域配置",
                new Vector2(1f, 1f), new Vector2(-260f, -55f), new Vector2(170f, 46f), Cyan);
            Image panel = CreateImage("Zone Assignment Panel", frame, DarkPanel);
            SetRect(panel.rectTransform, Vector2.zero, Vector2.one, new Vector2(28f, 32f), new Vector2(-28f, -112f));
            panel.transform.SetAsLastSibling();

            Text title = CreateText("Zone Title", panel.transform, font, 30, TextAnchor.MiddleLeft, TextColor);
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(0.75f, 1f), new Vector2(32f, -72f), new Vector2(0f, -20f));
            title.text = "2×2×2 ANCHOR FIELD  /  八区域效果配置";
            Text hint = CreateText("Zone Hint", panel.transform, font, 18, TextAnchor.MiddleLeft, new Color(0.52f, 0.7f, 0.84f));
            SetRect(hint.rectTransform, new Vector2(0f, 1f), new Vector2(0.8f, 1f), new Vector2(34f, -112f), new Vector2(0f, -76f));
            hint.text = "从左侧拖动效果图标到任意区域；后层/前层各代表魔方的一层。";
            Button closeButton = CreateButton("Close Zone Assignment", panel.transform, font, "返回技能树",
                new Vector2(1f, 1f), new Vector2(-145f, -56f), new Vector2(190f, 46f), new Color(1f, 0.36f, 0.2f));

            Image palette = CreateImage("Effect Palette", panel.transform, new Color(0.025f, 0.06f, 0.11f, 0.98f));
            SetRect(palette.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(28f, 28f), new Vector2(330f, -132f));
            Text paletteTitle = CreateText("Palette Title", palette.transform, font, 22, TextAnchor.MiddleCenter, TextColor);
            SetRect(paletteTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -62f), new Vector2(-12f, -18f));
            paletteTitle.text = "可分配区域效果";

            var sources = new List<ZoneEffectDragSource>();
            CubeZoneEffectDefinition[] effects = config.AvailableEffects ?? new CubeZoneEffectDefinition[0];
            for (int i = 0; i < effects.Length; i++)
            {
                CubeZoneEffectDefinition effect = effects[i];
                Image card = CreateImage("Effect " + effect.DisplayName, palette.transform,
                    effect.ZoneColor * new Color(1f, 1f, 1f, 4f));
                RectTransform cardRect = card.rectTransform;
                cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 1f);
                cardRect.sizeDelta = new Vector2(250f, 105f);
                cardRect.anchoredPosition = new Vector2(0f, -130f - i * 130f);
                Image icon = CreateImage("Icon", card.transform, effect.Icon != null ? Color.white : effect.ZoneColor);
                SetRect(icon.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(16f, 18f), new Vector2(82f, -18f));
                icon.sprite = effect.Icon;
                Text label = CreateText("Label", card.transform, font, 19, TextAnchor.MiddleLeft, TextColor);
                SetRect(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(96f, 8f), new Vector2(-12f, -8f));
                ZoneEffectDragSource source = card.gameObject.AddComponent<ZoneEffectDragSource>();
                source.Configure(effect, icon, label);
                sources.Add(source);
            }

            var targets = new List<ZoneAssignmentDropTarget>();
            CreateLayer(panel.transform, font, "后层  Z−", new Vector2(365f, -150f), 0, targets);
            CreateLayer(panel.transform, font, "前层  Z+", new Vector2(975f, -150f), 4, targets);

            CubeZoneAssignmentController controller = root.AddComponent<CubeZoneAssignmentController>();
            controller.Configure(openButton, closeButton, panel.gameObject, sources.ToArray(), targets.ToArray());
            panel.gameObject.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root, UpgradeUiPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void CreateLayer(Transform parent, Font font, string labelText,
            Vector2 topLeft, int zOffset, List<ZoneAssignmentDropTarget> targets)
        {
            Image layer = CreateImage(labelText, parent, new Color(0.025f, 0.055f, 0.1f, 0.92f));
            RectTransform layerRect = layer.rectTransform;
            layerRect.anchorMin = layerRect.anchorMax = new Vector2(0f, 1f);
            layerRect.pivot = new Vector2(0f, 1f);
            layerRect.anchoredPosition = topLeft;
            layerRect.sizeDelta = new Vector2(560f, 570f);
            Text title = CreateText("Layer Label", layer.transform, font, 23, TextAnchor.MiddleCenter, TextColor);
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -58f), new Vector2(-12f, -15f));
            title.text = labelText;

            int[] order = { zOffset + 2, zOffset + 3, zOffset, zOffset + 1 };
            for (int i = 0; i < order.Length; i++)
            {
                int row = i / 2;
                int column = i % 2;
                int zoneIndex = order[i];
                Image slot = CreateImage($"Zone Slot {zoneIndex + 1}", layer.transform, new Color(0.08f, 0.1f, 0.15f, 1f));
                RectTransform slotRect = slot.rectTransform;
                slotRect.anchorMin = slotRect.anchorMax = new Vector2(0f, 1f);
                slotRect.pivot = new Vector2(0f, 1f);
                slotRect.anchoredPosition = new Vector2(34f + column * 258f, -90f - row * 220f);
                slotRect.sizeDelta = new Vector2(234f, 190f);
                Image icon = CreateImage("Effect Icon", slot.transform, Color.white);
                SetRect(icon.rectTransform, new Vector2(0.28f, 0.38f), new Vector2(0.72f, 0.9f), Vector2.zero, Vector2.zero);
                icon.preserveAspect = true;
                Text label = CreateText("Zone Label", slot.transform, font, 18, TextAnchor.LowerCenter, TextColor);
                SetRect(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(8f, 8f), new Vector2(-8f, -8f));
                ZoneAssignmentDropTarget target = slot.gameObject.AddComponent<ZoneAssignmentDropTarget>();
                target.Configure(zoneIndex, slot, icon, label);
                targets.Add(target);
            }
        }

        private static void PatchGameplayScene(GameObject gridPrefab)
        {
            Scene scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
            CubeZoneGridController existing = Object.FindObjectOfType<CubeZoneGridController>(true);
            if (existing == null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(gridPrefab, scene);
                instance.name = "CubeZoneGrid";
                Transform world = GameObject.Find("World")?.transform;
                if (world != null) instance.transform.SetParent(world, false);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, GameplayScenePath);
        }

        private static T CreateOrLoad<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            return asset;
        }

        private static Button CreateButton(string name, Transform parent, Font font, string text,
            Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            Image image = CreateImage(name, parent, color);
            RectTransform rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Text label = CreateText("Label", image.transform, font, 19, TextAnchor.MiddleCenter, Color.white);
            SetRect(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-4f, -4f));
            label.text = text;
            return button;
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text CreateText(string name, Transform parent, Font font, int size,
            TextAnchor alignment, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            Text text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}

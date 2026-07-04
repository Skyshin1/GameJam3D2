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
        private const string GreenEffectPath = ZoneConfigFolder + "/GreenTurretDamage.asset";
        private const string GreenUnlockNodePath = Root + "/Configs/Upgrades/Nodes/ZoneDamageFragment.asset";
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
                CubeZoneEffectType.TurretFireRateBoost, 0.7f, 1f, 1f, 0f);
            CubeZoneEffectDefinition red = CreateOrLoad<CubeZoneEffectDefinition>(RedEffectPath);
            red.Configure("zone.enemy.suppression", "红色：敌军抑制",
                "区域内敌人移动速度降低 35%，并每秒受到 4 点伤害。", new Color(1f, 0.08f, 0.12f, 0.12f),
                CubeZoneEffectType.EnemySlowAndDamage, 1f, 1f, 0.65f, 4f);
            CubeZoneEffectDefinition green = CreateOrLoad<CubeZoneEffectDefinition>(GreenEffectPath);
            UpgradeNodeDefinition greenUnlock = AssetDatabase.LoadAssetAtPath<UpgradeNodeDefinition>(GreenUnlockNodePath);
            green.Configure("zone.turret.damage", "绿色：聚焦增幅",
                "区域内炮塔造成的伤害提高 50%。需要先解锁翠绿火控碎片。", new Color(0.08f, 1f, 0.32f, 0.12f),
                CubeZoneEffectType.TurretDamageBoost, 1f, 1.5f, 1f, 0f, greenUnlock);
            EditorUtility.SetDirty(blue);
            EditorUtility.SetDirty(red);
            EditorUtility.SetDirty(green);

            CubeZoneEffectDefinition[] defaults = new CubeZoneEffectDefinition[CubeZoneConfig.ZoneCount];
            for (int i = 0; i < defaults.Length; i++)
            {
                defaults[i] = (i & 1) == 0 ? blue : red;
            }
            CubeZoneConfig config = CreateOrLoad<CubeZoneConfig>(ConfigPath);
            config.Configure(10.5f, new[] { blue, red, green }, defaults);
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
                GameObject cube = new GameObject($"Zone Cube C{i + 1:00}", typeof(BoxCollider), typeof(CubeZoneVolume));
                cube.layer = CubeZoneGridController.ZoneRaycastLayer;
                cube.transform.SetParent(root.transform, false);
                cube.transform.localPosition = new Vector3(
                    (i & 1) != 0 ? centerOffset : -centerOffset,
                    (i & 2) != 0 ? centerOffset : -centerOffset,
                    (i & 4) != 0 ? centerOffset : -centerOffset);
                BoxCollider collider = cube.GetComponent<BoxCollider>();
                collider.size = Vector3.one * cubeSize;
                collider.isTrigger = true;

                GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.name = "Transparent Volume Visual";
                visual.layer = CubeZoneGridController.ZoneRaycastLayer;
                visual.transform.SetParent(cube.transform, false);
                visual.transform.localScale = Vector3.one * cubeSize;
                Object.DestroyImmediate(visual.GetComponent<Collider>());
                MeshRenderer renderer = visual.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;

                Transform vfxAnchor = new GameObject("Local VFX Anchor").transform;
                vfxAnchor.SetParent(cube.transform, false);
                CubeZoneVolume volume = cube.GetComponent<CubeZoneVolume>();
                Vector3Int gridPosition = new Vector3Int((i & 1) != 0 ? 1 : 0,
                    (i & 2) != 0 ? 1 : 0, (i & 4) != 0 ? 1 : 0);
                volume.Configure(i, gridPosition, renderer, collider, vfxAnchor);
                volume.SetEffect(config.GetDefaultEffect(i));
                volumes.Add(volume);
            }

            var hints = new List<Transform>(48);
            for (int i = 0; i < 48; i++)
            {
                GameObject hint = GameObject.CreatePrimitive(PrimitiveType.Cube);
                hint.name = $"Adjacent Drop Silhouette {i + 1}";
                hint.layer = CubeZoneGridController.ZoneRaycastLayer;
                hint.transform.SetParent(root.transform, false);
                hint.transform.localScale = Vector3.one * (cubeSize * 1.025f);
                BoxCollider hintCollider = hint.GetComponent<BoxCollider>();
                hintCollider.isTrigger = true;
                MeshRenderer renderer = hint.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                hint.SetActive(false);
                hints.Add(hint.transform);
            }
            root.GetComponent<CubeZoneGridController>().Configure(config, volumes.ToArray(), hints.ToArray());
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
            Transform frame = root.transform.Find("Upgrade Tree Panel/Anchor Protocol Frame");
            if (frame == null)
            {
                PrefabUtility.UnloadPrefabContents(root);
                return;
            }

            CubeZoneAssignmentController oldController = root.GetComponent<CubeZoneAssignmentController>();
            if (oldController != null) Object.DestroyImmediate(oldController);
            Transform oldOpen = frame.Find("Zone Assignment");
            if (oldOpen != null) Object.DestroyImmediate(oldOpen.gameObject);
            Transform oldPanel = frame.Find("Zone Assignment Panel");
            if (oldPanel != null) Object.DestroyImmediate(oldPanel.gameObject);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Button openButton = CreateButton("Zone Assignment", frame, font, "区域配置",
                new Vector2(1f, 1f), new Vector2(-260f, -55f), new Vector2(170f, 46f), Cyan);
            Image panel = CreateImage("Zone Assignment Panel", frame, DarkPanel);
            SetRect(panel.rectTransform, Vector2.zero, Vector2.one, new Vector2(28f, 32f), new Vector2(-28f, -112f));
            panel.transform.SetAsLastSibling();

            Text title = CreateText("Zone Title", panel.transform, font, 30, TextAnchor.MiddleLeft, TextColor);
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(0.75f, 1f), new Vector2(32f, -72f), new Vector2(0f, -20f));
            title.text = "ANCHOR FIELD TOPOLOGY  /  场域立方体配置";
            Text hint = CreateText("Zone Hint", panel.transform, font, 18, TextAnchor.MiddleLeft, new Color(0.52f, 0.7f, 0.84f));
            SetRect(hint.rectTransform, new Vector2(0f, 1f), new Vector2(0.8f, 1f), new Vector2(34f, -112f), new Vector2(0f, -76f));
            hint.text = "点击拓扑中的立方体查看配置；场景中拖动立方体，可吸附到任意其他立方体周围的空虚影位置。";
            Button closeButton = CreateButton("Close Zone Assignment", panel.transform, font, "返回技能树",
                new Vector2(1f, 1f), new Vector2(-145f, -56f), new Vector2(190f, 46f), new Color(1f, 0.36f, 0.2f));

            Image palette = CreateImage("Effect Palette", panel.transform, new Color(0.025f, 0.06f, 0.11f, 0.98f));
            SetRect(palette.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(28f, 28f), new Vector2(350f, -132f));
            Text paletteTitle = CreateText("Palette Title", palette.transform, font, 22, TextAnchor.MiddleCenter, TextColor);
            SetRect(paletteTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -62f), new Vector2(-12f, -18f));
            paletteTitle.text = "CONFIG FRAGMENTS  /  配置碎片";

            var sources = new List<ZoneEffectDragSource>();
            CubeZoneEffectDefinition[] effects = config.AvailableEffects ?? new CubeZoneEffectDefinition[0];
            for (int i = 0; i < effects.Length; i++)
            {
                CubeZoneEffectDefinition effect = effects[i];
                Image card = CreateImage("Effect " + effect.DisplayName, palette.transform,
                    effect.ZoneColor * new Color(1f, 1f, 1f, 4f));
                RectTransform cardRect = card.rectTransform;
                cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 1f);
                cardRect.sizeDelta = new Vector2(274f, 112f);
                cardRect.anchoredPosition = new Vector2(0f, -135f - i * 132f);
                card.gameObject.AddComponent<CanvasGroup>();
                Image icon = CreateImage("Icon", card.transform, effect.Icon != null ? Color.white : effect.ZoneColor);
                SetRect(icon.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(16f, 18f), new Vector2(82f, -18f));
                icon.sprite = effect.Icon;
                Text label = CreateText("Label", card.transform, font, 19, TextAnchor.MiddleLeft, TextColor);
                SetRect(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(96f, 8f), new Vector2(-12f, -8f));
                ZoneEffectDragSource source = card.gameObject.AddComponent<ZoneEffectDragSource>();
                source.Configure(effect, icon, label);
                sources.Add(source);
            }

            Image topology = CreateImage("Live Cube Topology", panel.transform, new Color(0.018f, 0.045f, 0.085f, 0.96f));
            SetRect(topology.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(375f, 28f), new Vector2(1080f, -132f));
            Text topologyTitle = CreateText("Topology Title", topology.transform, font, 22, TextAnchor.MiddleCenter, TextColor);
            SetRect(topologyTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -62f), new Vector2(-12f, -18f));
            topologyTitle.text = "LIVE FIELD SHAPE  /  实时自由形态";
            Text axisHint = CreateText("Axis Hint", topology.transform, font, 16, TextAnchor.MiddleCenter, new Color(0.45f, 0.7f, 0.82f));
            SetRect(axisHint.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(12f, 18f), new Vector2(-12f, 52f));
            axisHint.text = "C 编号随立方体移动 · 发光边框表示当前选中";

            var markers = new List<ZoneLayoutMarker>(CubeZoneConfig.ZoneCount);
            for (int slot = 0; slot < CubeZoneConfig.ZoneCount; slot++)
            {
                bool right = (slot & 1) != 0;
                bool top = (slot & 2) != 0;
                bool front = (slot & 4) != 0;
                Image markerImage = CreateImage($"Topology Slot {slot + 1}", topology.transform,
                    new Color(0.08f, 0.15f, 0.22f, 0.92f));
                RectTransform rect = markerImage.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(126f, 126f);
                rect.anchoredPosition = new Vector2(
                    (right ? 100f : -100f) + (front ? 68f : -68f),
                    (top ? 105f : -105f) + (front ? -52f : 52f));
                Button button = markerImage.gameObject.AddComponent<Button>();
                button.targetGraphic = markerImage;
                Outline outline = markerImage.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0.1f, 0.4f, 0.55f, 0.7f);
                outline.effectDistance = new Vector2(2f, -2f);
                Text markerLabel = CreateText("Cube Identity", markerImage.transform, font, 23,
                    TextAnchor.MiddleCenter, Color.white);
                SetRect(markerLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                ZoneLayoutMarker marker = markerImage.gameObject.AddComponent<ZoneLayoutMarker>();
                marker.Configure(slot, markerImage, markerLabel, outline);
                markers.Add(marker);
            }

            Image detailsPanel = CreateImage("Selected Cube Configuration", panel.transform,
                new Color(0.025f, 0.06f, 0.11f, 0.98f));
            SetRect(detailsPanel.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(1105f, 28f), new Vector2(-28f, -132f));
            Text detailsTitle = CreateText("Selected Title", detailsPanel.transform, font, 22,
                TextAnchor.MiddleCenter, TextColor);
            SetRect(detailsTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -62f), new Vector2(-18f, -18f));
            detailsTitle.text = "SELECTED CUBE  /  当前立方体";
            Text selectedDetails = CreateText("Selected Details", detailsPanel.transform, font, 19,
                TextAnchor.UpperLeft, new Color(0.72f, 0.86f, 0.94f));
            SetRect(selectedDetails.rectTransform, new Vector2(0f, 0.58f), new Vector2(1f, 0.88f), new Vector2(28f, 0f), new Vector2(-28f, 0f));
            selectedDetails.horizontalOverflow = HorizontalWrapMode.Wrap;
            selectedDetails.verticalOverflow = VerticalWrapMode.Overflow;

            Image selectedSlot = CreateImage("Selected Effect Drop Slot", detailsPanel.transform,
                new Color(0.08f, 0.1f, 0.15f, 1f));
            SetRect(selectedSlot.rectTransform, new Vector2(0.5f, 0.24f), new Vector2(0.5f, 0.24f),
                new Vector2(-205f, -100f), new Vector2(205f, 100f));
            Image selectedIcon = CreateImage("Effect Icon", selectedSlot.transform, Color.white);
            SetRect(selectedIcon.rectTransform, new Vector2(0.36f, 0.38f), new Vector2(0.64f, 0.86f), Vector2.zero, Vector2.zero);
            selectedIcon.preserveAspect = true;
            Text selectedLabel = CreateText("Effect Label", selectedSlot.transform, font, 19,
                TextAnchor.LowerCenter, TextColor);
            SetRect(selectedLabel.rectTransform, Vector2.zero, Vector2.one, new Vector2(10f, 10f), new Vector2(-10f, -10f));
            ZoneAssignmentDropTarget selectedTarget = selectedSlot.gameObject.AddComponent<ZoneAssignmentDropTarget>();
            selectedTarget.Configure(0, selectedSlot, selectedIcon, selectedLabel);
            Text dragHint = CreateText("Drop Hint", detailsPanel.transform, font, 17,
                TextAnchor.MiddleCenter, new Color(0.4f, 0.75f, 0.88f));
            SetRect(dragHint.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(18f, 25f), new Vector2(-18f, 72f));
            dragHint.text = "将左侧碎片拖到此处，或直接点击碎片配置";

            CubeZoneAssignmentController controller = root.AddComponent<CubeZoneAssignmentController>();
            controller.Configure(openButton, closeButton, panel.gameObject, sources.ToArray(), markers.ToArray(),
                selectedTarget, selectedDetails);
            panel.gameObject.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root, UpgradeUiPath);
            PrefabUtility.UnloadPrefabContents(root);
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

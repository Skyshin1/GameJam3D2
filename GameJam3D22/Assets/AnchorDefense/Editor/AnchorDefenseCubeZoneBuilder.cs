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
        private const string PreviewRenderTexturePath = Root + "/Art/UI/RT_CubeZonePreview.renderTexture";
        private const string PreviewMaterialPath = Root + "/Art/Materials/M_CubeZonePreview.mat";
        private const string GameplayScenePath = Root + "/Scenes/Gameplay.unity";

        private static readonly Color DarkPanel = new Color(0.015f, 0.03f, 0.065f, 0.985f);
        private static readonly Color Cyan = new Color(0.12f, 0.82f, 1f, 1f);
        private static readonly Color TextColor = new Color(0.82f, 0.94f, 1f, 1f);

        [MenuItem("Tools/Anchor Defense/Build 2x2x2 Cube Zones")]
        public static void BuildAll()
        {
            EnsureFolder(Root + "/Configs", "Zones");
            EnsureFolder(Root + "/Prefabs", "Zones");
            EnsureFolder(Root + "/Art", "UI");
            CubeZoneConfig config = CreateConfigAssets();
            Material material = CreateZoneMaterial();
            Material previewMaterial = CreatePreviewMaterial();
            RenderTexture previewTexture = CreatePreviewRenderTexture();
            GameObject gridPrefab = CreateGridPrefab(config, material);
            PatchUpgradeUi(config, previewMaterial, previewTexture);
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
            float cubeSize = config.CubeSize >= 1f ? config.CubeSize : 10.5f;
            config.Configure(cubeSize, new[] { blue, red, green }, defaults);
            EditorUtility.SetDirty(config);
            return config;
        }

        private static Material CreateZoneMaterial()
        {
            Shader shader = Shader.Find("AnchorDefense/Cube Zone Wireframe") ??
                            Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "M_CubeZoneVolume" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            else if (shader != null)
            {
                material.shader = shader;
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
            if (material.HasProperty("_EdgeWidth")) material.SetFloat("_EdgeWidth", 0.035f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreatePreviewMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(PreviewMaterialPath);
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            if (material == null)
            {
                material = new Material(shader) { name = "M_CubeZonePreview" };
                AssetDatabase.CreateAsset(material, PreviewMaterialPath);
            }
            else if (shader != null) material.shader = shader;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static RenderTexture CreatePreviewRenderTexture()
        {
            RenderTexture texture = AssetDatabase.LoadAssetAtPath<RenderTexture>(PreviewRenderTexturePath);
            if (texture == null)
            {
                texture = new RenderTexture(768, 768, 24, RenderTextureFormat.ARGB32)
                {
                    name = "RT_CubeZonePreview",
                    antiAliasing = 4
                };
                AssetDatabase.CreateAsset(texture, PreviewRenderTexturePath);
            }
            return texture;
        }

        private static GameObject CreateGridPrefab(CubeZoneConfig config, Material material)
        {
            GameObject root = new GameObject("CubeZoneGrid", typeof(CubeZoneGridController));
            float cubeSize = config.CubeSize;
            var volumes = new List<CubeZoneVolume>(CubeZoneConfig.ZoneCount);
            for (int i = 0; i < CubeZoneConfig.ZoneCount; i++)
            {
                GameObject cube = new GameObject($"Zone Cube C{i + 1:00}", typeof(BoxCollider), typeof(CubeZoneVolume));
                cube.layer = CubeZoneGridController.ZoneRaycastLayer;
                cube.transform.SetParent(root.transform, false);
                Vector3Int gridPosition = new Vector3Int((i & 1) != 0 ? 0 : -1,
                    (i & 2) != 0 ? 0 : -1, (i & 4) != 0 ? 0 : -1);
                cube.transform.localPosition = (Vector3)gridPosition * cubeSize;
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
                volume.Configure(i, gridPosition, renderer, collider, vfxAnchor);
                volume.ApplyInteractionColors(config.SelectedCubeColor, config.SwapTargetColor);
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
                hint.transform.localScale = Vector3.one * cubeSize;
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
            AssignUiFonts(root);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, GridPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void PatchUpgradeUi(CubeZoneConfig config, Material previewMaterial,
            RenderTexture previewTexture)
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
            CubeZoneEditModeController oldEditController = root.GetComponent<CubeZoneEditModeController>();
            if (oldEditController != null) Object.DestroyImmediate(oldEditController);
            Transform oldOpen = frame.Find("Zone Assignment");
            if (oldOpen != null) Object.DestroyImmediate(oldOpen.gameObject);
            Transform oldPanel = frame.Find("Zone Assignment Panel");
            if (oldPanel != null) Object.DestroyImmediate(oldPanel.gameObject);
            Transform oldEditButton = root.transform.Find("Field Weaving Mode");
            if (oldEditButton != null) Object.DestroyImmediate(oldEditButton.gameObject);
            Transform oldEditBanner = root.transform.Find("Field Weaving Banner");
            if (oldEditBanner != null) Object.DestroyImmediate(oldEditBanner.gameObject);
            Transform oldSidebar = root.transform.Find("Field Weaving Sidebar");
            if (oldSidebar != null) Object.DestroyImmediate(oldSidebar.gameObject);

            Font font = LoadEnglishUiFont();
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
            axisHint.text = "拖动旋转观察 · 点击真实方块选择 · 分布与 Gameplay 完全同步";
            CubeZoneSpatialPreviewController spatialPreview = CreateSpatialPreview(
                topology.transform, previewMaterial, previewTexture);
            var markers = new List<ZoneLayoutMarker>(0);

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
                selectedTarget, selectedDetails, spatialPreview);

            Button editButton = CreateButton("Field Weaving Mode", root.transform, font, "锚域编织",
                new Vector2(0f, 1f), new Vector2(174f, -225f), new Vector2(284f, 52f),
                new Color(0.08f, 0.52f, 0.66f, 0.96f));
            Transform upgradePanel = root.transform.Find("Upgrade Tree Panel");
            if (upgradePanel != null) editButton.transform.SetSiblingIndex(upgradePanel.GetSiblingIndex());

            Image editBanner = CreateImage("Field Weaving Banner", root.transform,
                new Color(0.012f, 0.035f, 0.07f, 0.96f));
            SetRect(editBanner.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-460f, -145f), new Vector2(460f, -28f));
            Text editTitle = CreateText("Edit Mode Title", editBanner.transform, font, 25,
                TextAnchor.MiddleLeft, Cyan);
            SetRect(editTitle.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(28f, 48f), new Vector2(-220f, -12f));
            editTitle.text = "ANCHOR FIELD WEAVING  /  锚域编织模式";
            Text editHint = CreateText("Edit Mode Hint", editBanner.transform, font, 17,
                TextAnchor.MiddleLeft, new Color(0.65f, 0.82f, 0.9f));
            SetRect(editHint.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(30f, 10f), new Vector2(-220f, -58f));
            editHint.text = "游戏已暂停 · 拖动方块调整位置 · 从右侧拖动碎片到立方体以改写效果";
            Button finishEdit = CreateButton("Finish Field Weaving", editBanner.transform, font, "完成编织",
                new Vector2(1f, 0.5f), new Vector2(-118f, 0f), new Vector2(190f, 54f),
                new Color(0.18f, 0.8f, 0.58f, 1f));

            Image sidebar = CreateImage("Field Weaving Sidebar", root.transform,
                new Color(0.01f, 0.03f, 0.065f, 0.97f));
            SetRect(sidebar.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(-370f, 28f), new Vector2(-24f, -170f));
            Text sidebarTitle = CreateText("Fragment Sidebar Title", sidebar.transform, font, 22,
                TextAnchor.MiddleCenter, TextColor);
            SetRect(sidebarTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(16f, -64f), new Vector2(-16f, -16f));
            sidebarTitle.text = "FIELD FRAGMENTS  /  场域碎片";

            var worldSources = new List<ZoneEffectWorldDragSource>();
            for (int i = 0; i < effects.Length; i++)
            {
                CubeZoneEffectDefinition effect = effects[i];
                Image card = CreateImage("World Fragment " + effect.DisplayName, sidebar.transform,
                    effect.ZoneColor * new Color(1f, 1f, 1f, 5f));
                RectTransform cardRect = card.rectTransform;
                cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 1f);
                cardRect.sizeDelta = new Vector2(300f, 82f);
                cardRect.anchoredPosition = new Vector2(0f, -112f - i * 98f);
                CanvasGroup group = card.gameObject.AddComponent<CanvasGroup>();
                Image fragmentIcon = CreateImage("Fragment Icon", card.transform,
                    effect.Icon != null ? Color.white : effect.ZoneColor);
                SetRect(fragmentIcon.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f),
                    new Vector2(12f, 12f), new Vector2(78f, -12f));
                fragmentIcon.sprite = effect.Icon;
                Text fragmentLabel = CreateText("Fragment Name", card.transform, font, 17,
                    TextAnchor.MiddleLeft, Color.white);
                SetRect(fragmentLabel.rectTransform, Vector2.zero, Vector2.one,
                    new Vector2(92f, 8f), new Vector2(-10f, -8f));
                ZoneEffectWorldDragSource source = card.gameObject.AddComponent<ZoneEffectWorldDragSource>();
                source.Configure(effect, fragmentIcon, fragmentLabel, group);
                worldSources.Add(source);
            }

            Image tooltip = CreateImage("Fragment Tooltip", sidebar.transform,
                new Color(0.025f, 0.075f, 0.12f, 0.99f));
            SetRect(tooltip.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(16f, 18f), new Vector2(-16f, 220f));
            Text tooltipTitle = CreateText("Tooltip Title", tooltip.transform, font, 20,
                TextAnchor.UpperLeft, Cyan);
            SetRect(tooltipTitle.rectTransform, new Vector2(0f, 0.68f), new Vector2(1f, 1f),
                new Vector2(16f, 0f), new Vector2(-16f, -10f));
            Text tooltipBody = CreateText("Tooltip Description", tooltip.transform, font, 16,
                TextAnchor.UpperLeft, new Color(0.76f, 0.88f, 0.94f));
            SetRect(tooltipBody.rectTransform, new Vector2(0f, 0.24f), new Vector2(1f, 0.72f),
                new Vector2(16f, 0f), new Vector2(-16f, 0f));
            tooltipBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            tooltipBody.verticalOverflow = VerticalWrapMode.Truncate;
            Text tooltipStatus = CreateText("Tooltip Status", tooltip.transform, font, 15,
                TextAnchor.MiddleLeft, Color.white);
            SetRect(tooltipStatus.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.24f),
                new Vector2(16f, 0f), new Vector2(-16f, 0f));

            CubeZoneEditModeController editController = root.AddComponent<CubeZoneEditModeController>();
            editController.Configure(editButton, finishEdit, editBanner.gameObject,
                sidebar.gameObject, worldSources.ToArray(), tooltip.gameObject,
                tooltipTitle, tooltipBody, tooltipStatus);
            Object.DestroyImmediate(controller);
            Object.DestroyImmediate(openButton.gameObject);
            Object.DestroyImmediate(panel.gameObject);
            editBanner.gameObject.SetActive(false);
            sidebar.gameObject.SetActive(false);
            tooltip.gameObject.SetActive(false);
            AssignUiFonts(root);
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

        private static CubeZoneSpatialPreviewController CreateSpatialPreview(
            Transform parent, Material material, RenderTexture renderTexture)
        {
            GameObject displayObject = new GameObject("Rotatable Spatial Preview",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            displayObject.transform.SetParent(parent, false);
            RawImage display = displayObject.GetComponent<RawImage>();
            display.texture = renderTexture;
            display.color = Color.white;
            RectTransform displayRect = display.rectTransform;
            displayRect.anchorMin = displayRect.anchorMax = new Vector2(0.5f, 0.5f);
            displayRect.sizeDelta = new Vector2(570f, 570f);
            displayRect.anchoredPosition = new Vector2(0f, -5f);

            GameObject world = new GameObject("Spatial Preview World");
            world.transform.SetParent(parent, false);
            world.transform.localPosition = new Vector3(0f, 0f, 1000f);

            GameObject model = new GameObject("Actual Cube Layout");
            model.transform.SetParent(world.transform, false);

            GameObject cameraObject = new GameObject("Spatial Preview Camera", typeof(Camera));
            cameraObject.transform.SetParent(world.transform, false);
            cameraObject.transform.localPosition = new Vector3(4.6f, 3.8f, -6.2f);
            cameraObject.transform.LookAt(model.transform.position, Vector3.up);
            Camera previewCamera = cameraObject.GetComponent<Camera>();
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0.006f, 0.018f, 0.035f, 1f);
            previewCamera.orthographic = true;
            previewCamera.orthographicSize = 3.2f;
            previewCamera.nearClipPlane = 0.1f;
            previewCamera.farClipPlane = 50f;
            previewCamera.cullingMask = 1 << 31;
            previewCamera.targetTexture = renderTexture;

            var cubeTransforms = new Transform[CubeZoneConfig.ZoneCount];
            var cubeRenderers = new Renderer[CubeZoneConfig.ZoneCount];
            var labels = new TextMesh[CubeZoneConfig.ZoneCount];
            for (int i = 0; i < CubeZoneConfig.ZoneCount; i++)
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = $"Preview Cube C{i + 1:00}";
                cube.layer = 31;
                cube.transform.SetParent(model.transform, false);
                cube.transform.localScale = Vector3.one * 0.9f;
                Renderer renderer = cube.GetComponent<Renderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;

                GameObject labelObject = new GameObject("Identity Label", typeof(TextMesh));
                labelObject.layer = 31;
                labelObject.transform.SetParent(cube.transform, false);
                TextMesh label = labelObject.GetComponent<TextMesh>();
                label.text = $"C{i + 1:00}";
                label.anchor = TextAnchor.MiddleCenter;
                label.alignment = TextAlignment.Center;
                label.fontSize = 48;
                label.characterSize = 0.08f;
                label.color = Color.white;
                labelObject.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

                cubeTransforms[i] = cube.transform;
                cubeRenderers[i] = renderer;
                labels[i] = label;
            }

            CubeZoneSpatialPreviewController controller =
                displayObject.AddComponent<CubeZoneSpatialPreviewController>();
            controller.Configure(display, previewCamera, model.transform,
                cubeTransforms, cubeRenderers, labels);
            return controller;
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

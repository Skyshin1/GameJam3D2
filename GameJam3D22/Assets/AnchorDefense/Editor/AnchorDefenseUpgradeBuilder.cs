using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AnchorDefense.Editor
{
    public static class AnchorDefenseUpgradeBuilder
    {
        private const string Root = "Assets/AnchorDefense";
        private const string UpgradeConfigFolder = Root + "/Configs/Upgrades";
        private const string EffectFolder = UpgradeConfigFolder + "/Effects";
        private const string NodeFolder = UpgradeConfigFolder + "/Nodes";
        private const string UiPrefabPath = Root + "/Prefabs/UI/UpgradeTreeUI.prefab";
        private const string GameplayScenePath = Root + "/Scenes/Gameplay.unity";
        private const string DirectionalScenePath = Root + "/Scenes/Gameplay_DirectionalSprites.unity";
        private const string UpgradeTreePath = UpgradeConfigFolder + "/UpgradeTreeConfig.asset";

        private static readonly Color Cyan = new Color(0.12f, 0.82f, 1f);
        private static readonly Color Violet = new Color(0.66f, 0.3f, 1f);
        private static readonly Color Orange = new Color(1f, 0.48f, 0.12f);
        private static readonly Color Mint = new Color(0.2f, 1f, 0.66f);
        private static readonly Color Gold = new Color(1f, 0.78f, 0.3f);
        private static readonly Color DarkPanel = new Color(0.025f, 0.045f, 0.09f, 0.97f);
        private static readonly Color DarkNode = new Color(0.055f, 0.09f, 0.15f, 0.98f);

        [MenuItem("Tools/Anchor Defense/Build or Refresh Upgrade Tree")]
        public static void BuildAll()
        {
            BuildInternal(true);
        }

        public static void RepairAfterGameplayRebuild()
        {
            BuildInternal(false);
        }

        private static void BuildInternal(bool rebuildUi)
        {
            EnsureFolder(UpgradeConfigFolder);
            EnsureFolder(EffectFolder);
            EnsureFolder(NodeFolder);

            UpgradeTreeConfig config = CreateUpgradeAssets();
            EnsureTurretConfigDefaults();
            PatchTurretPrefab(Root + "/Prefabs/Gameplay/Turret.prefab");
            PatchTurretPrefab(Root + "/Prefabs/Directional/Turret_Directional.prefab");

            PatchRingSet(false);
            PatchRingSet(true);

            GameObject uiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiPrefabPath);
            if (uiPrefab == null || rebuildUi)
            {
                CreateUpgradeUiPrefab(config);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(UiPrefabPath, ImportAssetOptions.ForceSynchronousImport);
            AddUpgradeSystemToScene(GameplayScenePath, config);
            AddUpgradeSystemToScene(DirectionalScenePath, config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            AnchorDefenseFrontendBuilder.RepairAfterGameplayRebuild();
            Debug.Log("Anchor Defense upgrade tree assets and scenes refreshed successfully.");
        }

        private static UpgradeTreeConfig CreateUpgradeAssets()
        {
            RingTurretUpgradeEffect innerEffect = CreateOrLoad<RingTurretUpgradeEffect>(EffectFolder + "/AddInnerRingTurrets.asset");
            innerEffect.Configure(OrbitRingId.Inner, 2);
            RingTurretUpgradeEffect middleEffect = CreateOrLoad<RingTurretUpgradeEffect>(EffectFolder + "/AddMiddleRingTurrets.asset");
            middleEffect.Configure(OrbitRingId.Middle, 2);
            RingTurretUpgradeEffect outerEffect = CreateOrLoad<RingTurretUpgradeEffect>(EffectFolder + "/AddOuterRingTurrets.asset");
            outerEffect.Configure(OrbitRingId.Outer, 2);

            TurretStatUpgradeEffect damageEffect = CreateOrLoad<TurretStatUpgradeEffect>(EffectFolder + "/IncreaseTurretDamage.asset");
            damageEffect.Configure(TurretRuntimeStat.Damage, 1.25f);
            TurretStatUpgradeEffect intervalEffect = CreateOrLoad<TurretStatUpgradeEffect>(EffectFolder + "/ReduceFireInterval.asset");
            intervalEffect.Configure(TurretRuntimeStat.FireInterval, 0.85f);
            TurretStatUpgradeEffect healthEffect = CreateOrLoad<TurretStatUpgradeEffect>(EffectFolder + "/IncreaseTurretHealth.asset");
            healthEffect.Configure(TurretRuntimeStat.MaxHealth, 1.3f);

            UpgradeNodeDefinition innerNode = CreateOrLoad<UpgradeNodeDefinition>(NodeFolder + "/Ring01Capacity.asset");
            innerNode.Configure(
                "ring.inner.capacity",
                "第一轨道扩容",
                "唤醒第一轨道的两个预留锚点，增加 2 座炮台。",
                "I",
                8,
                false,
                null,
                new UpgradeEffect[] { innerEffect });

            UpgradeNodeDefinition middleNode = CreateOrLoad<UpgradeNodeDefinition>(NodeFolder + "/Ring02Capacity.asset");
            middleNode.Configure(
                "ring.middle.capacity",
                "第二轨道扩容",
                "接通第二轨道的扩展回路，增加 2 座炮台。需要先扩容第一轨道。",
                "II",
                12,
                false,
                new[] { innerNode },
                new UpgradeEffect[] { middleEffect });

            UpgradeNodeDefinition outerNode = CreateOrLoad<UpgradeNodeDefinition>(NodeFolder + "/Ring03Capacity.asset");
            outerNode.Configure(
                "ring.outer.capacity",
                "第三轨道扩容",
                "启用最外层轨道的两个火力锚点，增加 2 座炮台。需要先扩容第二轨道。",
                "III",
                16,
                false,
                new[] { middleNode },
                new UpgradeEffect[] { outerEffect });

            UpgradeNodeDefinition damageNode = CreateOrLoad<UpgradeNodeDefinition>(NodeFolder + "/TurretDamage.asset");
            damageNode.Configure(
                "turret.damage.01",
                "锚定增幅",
                "强化所有炮台的供能线路，使炮弹伤害提高 25%。",
                "DMG",
                20,
                false,
                null,
                new UpgradeEffect[] { damageEffect });

            UpgradeNodeDefinition intervalNode = CreateOrLoad<UpgradeNodeDefinition>(NodeFolder + "/TurretInterval.asset");
            intervalNode.Configure(
                "turret.interval.01",
                "同步装填",
                "同步所有炮台的装填节拍，使射击间隔缩短 15%。需要先激活锚定增幅。",
                "SPD",
                24,
                false,
                new[] { damageNode },
                new UpgradeEffect[] { intervalEffect });

            UpgradeNodeDefinition healthNode = CreateOrLoad<UpgradeNodeDefinition>(NodeFolder + "/TurretHealth.asset");
            healthNode.Configure(
                "turret.health.01",
                "锚甲协议",
                "为所有炮台注入结构强化协议，最大生命提高 30%。",
                "HP",
                16,
                false,
                null,
                new UpgradeEffect[] { healthEffect });

            UnityEngine.Object[] dirtyAssets =
            {
                innerEffect, middleEffect, outerEffect, damageEffect, intervalEffect, healthEffect,
                innerNode, middleNode, outerNode, damageNode, intervalNode, healthNode
            };
            for (int i = 0; i < dirtyAssets.Length; i++)
            {
                EditorUtility.SetDirty(dirtyAssets[i]);
            }

            UpgradeTreeConfig tree = CreateOrLoad<UpgradeTreeConfig>(UpgradeTreePath);
            tree.Configure(new[] { innerNode, middleNode, outerNode, damageNode, intervalNode, healthNode });
            EditorUtility.SetDirty(tree);
            return tree;
        }

        private static void EnsureTurretConfigDefaults()
        {
            string[] paths =
            {
                Root + "/Configs/TurretConfig.asset",
                Root + "/Configs/Directional/TurretConfig_Directional.asset"
            };
            for (int i = 0; i < paths.Length; i++)
            {
                TurretConfig config = AssetDatabase.LoadAssetAtPath<TurretConfig>(paths[i]);
                if (config == null)
                {
                    continue;
                }

                SerializedObject serializedConfig = new SerializedObject(config);
                SerializedProperty maxHealth = serializedConfig.FindProperty("<MaxHealth>k__BackingField");
                if (maxHealth != null)
                {
                    maxHealth.floatValue = maxHealth.floatValue > 0f ? maxHealth.floatValue : 100f;
                    serializedConfig.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(config);
                }
            }
        }

        private static void PatchTurretPrefab(string path)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
            {
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            TurretController controller = root.GetComponent<TurretController>();
            TurretHealth health = root.GetComponent<TurretHealth>();
            if (health == null)
            {
                health = root.AddComponent<TurretHealth>();
            }
            controller.ConfigureHealth(health);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void PatchRingSet(bool directional)
        {
            string folder = directional ? Root + "/Prefabs/Directional" : Root + "/Prefabs/Gameplay";
            string suffix = directional ? "_Directional" : string.Empty;
            string turretPath = directional
                ? Root + "/Prefabs/Directional/Turret_Directional.prefab"
                : Root + "/Prefabs/Gameplay/Turret.prefab";
            GameObject turretPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(turretPath);
            if (turretPrefab == null)
            {
                return;
            }

            PatchRingPrefab(folder + "/OrbitRing_Inner" + suffix + ".prefab", OrbitRingId.Inner, turretPrefab);
            PatchRingPrefab(folder + "/OrbitRing_Middle" + suffix + ".prefab", OrbitRingId.Middle, turretPrefab);
            PatchRingPrefab(folder + "/OrbitRing_Outer" + suffix + ".prefab", OrbitRingId.Outer, turretPrefab);
        }

        private static void PatchRingPrefab(string path, OrbitRingId ringId, GameObject turretPrefab)
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

            var initialTurrets = new List<TurretController>();
            var upgradeTurrets = new List<TurretController>();
            TurretController[] existing = turretRoot.GetComponentsInChildren<TurretController>(true);
            for (int i = 0; i < existing.Length; i++)
            {
                if (existing[i].name.StartsWith("Upgrade Turret", StringComparison.Ordinal))
                {
                    upgradeTurrets.Add(existing[i]);
                }
                else
                {
                    initialTurrets.Add(existing[i]);
                }
            }

            float radius = initialTurrets.Count > 0 ? initialTurrets[0].transform.localPosition.magnitude : GetRingRadius(ringId);
            float[] upgradeAngles = { 30f, 210f };
            for (int i = upgradeTurrets.Count; i < upgradeAngles.Length; i++)
            {
                GameObject turretObject = (GameObject)PrefabUtility.InstantiatePrefab(turretPrefab, turretRoot);
                turretObject.name = $"Upgrade Turret {i + 1:00}";
                upgradeTurrets.Add(turretObject.GetComponent<TurretController>());
            }

            for (int i = 0; i < upgradeTurrets.Count; i++)
            {
                float angle = upgradeAngles[Mathf.Min(i, upgradeAngles.Length - 1)] * Mathf.Deg2Rad;
                Vector3 position = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Transform turret = upgradeTurrets[i].transform;
                turret.localPosition = position;
                turret.localRotation = Quaternion.LookRotation(position.normalized, Vector3.up);
                turret.gameObject.SetActive(false);
            }

            ring.ConfigureTurretSlots(ringId, initialTurrets.ToArray(), upgradeTurrets.ToArray());
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static float GetRingRadius(OrbitRingId ringId)
        {
            switch (ringId)
            {
                case OrbitRingId.Inner: return 4.7f;
                case OrbitRingId.Middle: return 6.1f;
                default: return 7.5f;
            }
        }

        private static UpgradeTreeController CreateUpgradeUiPrefab(UpgradeTreeConfig config)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject canvasObject = new GameObject(
                "UpgradeTreeUI",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(UpgradeTreeController));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 25;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Image openImage = CreateImage("Open Upgrade Tree", canvasObject.transform, new Color(0.045f, 0.13f, 0.2f, 0.96f));
            SetRect(openImage.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(34f, -184f), new Vector2(318f, -132f));
            Button openButton = openImage.gameObject.AddComponent<Button>();
            openButton.targetGraphic = openImage;
            AddOutline(openImage.gameObject, Cyan, new Vector2(2f, -2f));
            Text compactKills = CreateText("Compact Kill Points", openImage.transform, font, 20, TextAnchor.MiddleCenter, new Color(0.72f, 0.95f, 1f));
            Stretch(compactKills.rectTransform, 10f);
            compactKills.text = "升级协议  0";

            Image overlay = CreateImage("Upgrade Tree Panel", canvasObject.transform, new Color(0.005f, 0.012f, 0.03f, 0.94f));
            Stretch(overlay.rectTransform, 0f);
            Image frame = CreateImage("Anchor Protocol Frame", overlay.transform, DarkPanel);
            SetRect(frame.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-870f, -480f), new Vector2(870f, 480f));
            AddOutline(frame.gameObject, new Color(0.15f, 0.65f, 0.85f, 0.85f), new Vector2(3f, -3f));

            Image headerLine = CreateImage("Header Energy Line", frame.transform, Cyan);
            SetRect(headerLine.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(34f, -92f), new Vector2(-34f, -88f));
            Text title = CreateText("Title", frame.transform, font, 34, TextAnchor.MiddleLeft, new Color(0.72f, 0.95f, 1f));
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(0.58f, 1f), new Vector2(42f, -80f), new Vector2(0f, -20f));
            title.text = "ANCHOR PROTOCOL  /  锚定升级树";
            Text totalKills = CreateText("Total Kills", frame.transform, font, 21, TextAnchor.MiddleRight, new Color(0.64f, 0.76f, 0.9f));
            SetRect(totalKills.rectTransform, new Vector2(0.55f, 1f), new Vector2(0.76f, 1f), new Vector2(0f, -77f), new Vector2(0f, -23f));
            Text availableKills = CreateText("Available Kills", frame.transform, font, 22, TextAnchor.MiddleRight, Gold);
            SetRect(availableKills.rectTransform, new Vector2(0.74f, 1f), new Vector2(0.94f, 1f), new Vector2(0f, -77f), new Vector2(0f, -23f));

            Image closeImage = CreateImage("Close", frame.transform, new Color(0.26f, 0.08f, 0.12f, 0.95f));
            SetRect(closeImage.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-74f, -75f), new Vector2(-28f, -29f));
            Button closeButton = closeImage.gameObject.AddComponent<Button>();
            closeButton.targetGraphic = closeImage;
            Text closeText = CreateText("Close Label", closeImage.transform, font, 24, TextAnchor.MiddleCenter, Color.white);
            Stretch(closeText.rectTransform, 0f);
            closeText.text = "×";

            Image treeArea = CreateImage("Protocol Map", frame.transform, new Color(0.015f, 0.035f, 0.07f, 0.72f));
            SetRect(treeArea.rectTransform, new Vector2(0f, 0f), new Vector2(0.7f, 1f), new Vector2(28f, 32f), new Vector2(-10f, -112f));
            AddOutline(treeArea.gameObject, new Color(0.08f, 0.25f, 0.38f, 0.8f), new Vector2(2f, -2f));

            UpgradeNodeDefinition inner = config.FindNode("ring.inner.capacity");
            UpgradeNodeDefinition middle = config.FindNode("ring.middle.capacity");
            UpgradeNodeDefinition outer = config.FindNode("ring.outer.capacity");
            UpgradeNodeDefinition damage = config.FindNode("turret.damage.01");
            UpgradeNodeDefinition interval = config.FindNode("turret.interval.01");
            UpgradeNodeDefinition health = config.FindNode("turret.health.01");

            Vector2 hub = new Vector2(0f, -25f);
            Vector2 innerPos = new Vector2(-230f, 115f);
            Vector2 middlePos = new Vector2(-420f, 215f);
            Vector2 outerPos = new Vector2(-575f, 325f);
            Vector2 damagePos = new Vector2(15f, 210f);
            Vector2 intervalPos = new Vector2(200f, 325f);
            Vector2 healthPos = new Vector2(245f, -190f);

            CreateLine(treeArea.transform, hub, innerPos, Cyan, 4f);
            CreateLine(treeArea.transform, innerPos, middlePos, Violet, 4f);
            CreateLine(treeArea.transform, middlePos, outerPos, Orange, 4f);
            CreateLine(treeArea.transform, hub, damagePos, Gold, 4f);
            CreateLine(treeArea.transform, damagePos, intervalPos, Mint, 4f);
            CreateLine(treeArea.transform, hub, healthPos, Mint, 4f);

            var views = new List<UpgradeNodeView>
            {
                CreateNode(treeArea.transform, font, innerPos, inner, Cyan),
                CreateNode(treeArea.transform, font, middlePos, middle, Violet),
                CreateNode(treeArea.transform, font, outerPos, outer, Orange),
                CreateNode(treeArea.transform, font, damagePos, damage, Gold),
                CreateNode(treeArea.transform, font, intervalPos, interval, Mint),
                CreateNode(treeArea.transform, font, healthPos, health, Mint)
            };

            Vector2[] placeholderPositions =
            {
                new Vector2(-620f, 125f), new Vector2(-410f, 20f), new Vector2(-170f, 300f),
                new Vector2(25f, 365f), new Vector2(390f, 245f), new Vector2(430f, -20f),
                new Vector2(100f, -335f), new Vector2(-220f, -260f)
            };
            for (int i = 0; i < placeholderPositions.Length; i++)
            {
                CreateLine(treeArea.transform, hub, placeholderPositions[i], new Color(0.12f, 0.2f, 0.3f, 0.7f), 2f);
                views.Add(CreateNode(treeArea.transform, font, placeholderPositions[i], null, new Color(0.2f, 0.3f, 0.42f)));
            }
            CreateAnchorHub(treeArea.transform, font, hub);

            Image details = CreateImage("Node Details", frame.transform, new Color(0.025f, 0.055f, 0.1f, 0.96f));
            SetRect(details.rectTransform, new Vector2(0.7f, 0f), new Vector2(1f, 1f), new Vector2(12f, 32f), new Vector2(-28f, -112f));
            AddOutline(details.gameObject, new Color(0.12f, 0.38f, 0.55f, 0.9f), new Vector2(2f, -2f));
            Image detailAccent = CreateImage("Branch Accent", details.transform, Cyan);
            SetRect(detailAccent.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -27f), new Vector2(-20f, -20f));
            Text detailKicker = CreateText("Detail Kicker", details.transform, font, 17, TextAnchor.MiddleLeft, new Color(0.5f, 0.68f, 0.82f));
            SetRect(detailKicker.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(30f, -72f), new Vector2(-30f, -38f));
            detailKicker.text = "SELECTED ANCHOR NODE";
            Text detailTitle = CreateText("Detail Title", details.transform, font, 30, TextAnchor.UpperLeft, Color.white);
            SetRect(detailTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(30f, -137f), new Vector2(-30f, -78f));
            Text detailDescription = CreateText("Detail Description", details.transform, font, 21, TextAnchor.UpperLeft, new Color(0.78f, 0.86f, 0.94f));
            SetRect(detailDescription.rectTransform, new Vector2(0f, 0.45f), new Vector2(1f, 0.82f), new Vector2(30f, 0f), new Vector2(-30f, 0f));
            detailDescription.horizontalOverflow = HorizontalWrapMode.Wrap;
            detailDescription.verticalOverflow = VerticalWrapMode.Truncate;
            Text detailCost = CreateText("Detail Cost", details.transform, font, 23, TextAnchor.MiddleLeft, Gold);
            SetRect(detailCost.rectTransform, new Vector2(0f, 0.28f), new Vector2(1f, 0.4f), new Vector2(30f, 0f), new Vector2(-30f, 0f));
            Text detailStatus = CreateText("Detail Status", details.transform, font, 19, TextAnchor.MiddleLeft, new Color(0.45f, 0.9f, 1f));
            SetRect(detailStatus.rectTransform, new Vector2(0f, 0.18f), new Vector2(1f, 0.29f), new Vector2(30f, 0f), new Vector2(-30f, 0f));

            Image purchaseImage = CreateImage("Purchase Upgrade", details.transform, new Color(0.08f, 0.48f, 0.66f, 1f));
            SetRect(purchaseImage.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-155f, 38f), new Vector2(155f, 105f));
            Button purchaseButton = purchaseImage.gameObject.AddComponent<Button>();
            purchaseButton.targetGraphic = purchaseImage;
            AddOutline(purchaseImage.gameObject, Cyan, new Vector2(2f, -2f));
            Text purchaseText = CreateText("Purchase Label", purchaseImage.transform, font, 23, TextAnchor.MiddleCenter, Color.white);
            Stretch(purchaseText.rectTransform, 0f);

            Text footer = CreateText("Footer Hint", frame.transform, font, 17, TextAnchor.MiddleLeft, new Color(0.42f, 0.6f, 0.76f));
            SetRect(footer.rectTransform, new Vector2(0f, 0f), new Vector2(0.7f, 0f), new Vector2(42f, 4f), new Vector2(-12f, 29f));
            footer.text = "U  打开/关闭升级树    ·    击杀敌人获得协议点    ·    漏入核心的敌人不计入击杀";

            UpgradeTreeController controller = canvasObject.GetComponent<UpgradeTreeController>();
            controller.ConfigureView(
                overlay.gameObject,
                openButton,
                closeButton,
                compactKills,
                totalKills,
                availableKills,
                detailTitle,
                detailDescription,
                detailCost,
                detailStatus,
                purchaseButton,
                purchaseText,
                detailAccent,
                views.ToArray());
            overlay.gameObject.SetActive(false);

            PrefabUtility.SaveAsPrefabAsset(canvasObject, UiPrefabPath);
            UnityEngine.Object.DestroyImmediate(canvasObject);
            return AssetDatabase.LoadAssetAtPath<UpgradeTreeController>(UiPrefabPath);
        }

        private static UpgradeNodeView CreateNode(
            Transform parent,
            Font font,
            Vector2 position,
            UpgradeNodeDefinition definition,
            Color branchColor)
        {
            Image background = CreateImage(definition != null ? definition.DisplayName : "Future Node", parent, DarkNode);
            RectTransform rect = background.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(92f, 92f);
            rect.anchoredPosition = position;
            AddOutline(background.gameObject, branchColor * new Color(0.75f, 0.75f, 0.75f, 1f), new Vector2(3f, -3f));
            Button button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;

            Image icon = CreateImage("Icon", background.transform, Color.white);
            SetRect(icon.rectTransform, new Vector2(0.2f, 0.25f), new Vector2(0.8f, 0.85f), Vector2.zero, Vector2.zero);
            icon.preserveAspect = true;
            icon.gameObject.SetActive(false);
            Text label = CreateText("Short Label", background.transform, font, definition != null && definition.ShortLabel.Length > 2 ? 18 : 25, TextAnchor.MiddleCenter, Color.white);
            SetRect(label.rectTransform, new Vector2(0f, 0.2f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            label.text = definition != null ? definition.ShortLabel : "?";
            Text cost = CreateText("Cost", background.transform, font, 15, TextAnchor.MiddleCenter, Gold);
            SetRect(cost.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.28f), Vector2.zero, Vector2.zero);
            cost.text = definition != null ? definition.KillCost.ToString() : string.Empty;

            UpgradeNodeView view = background.gameObject.AddComponent<UpgradeNodeView>();
            view.Configure(definition, button, background, icon, label, cost, branchColor);
            return view;
        }

        private static void CreateAnchorHub(Transform parent, Font font, Vector2 position)
        {
            Image outer = CreateImage("Anchor Core", parent, new Color(0.08f, 0.3f, 0.42f, 1f));
            RectTransform rect = outer.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(156f, 156f);
            rect.anchoredPosition = position;
            rect.localRotation = Quaternion.Euler(0f, 0f, 45f);
            AddOutline(outer.gameObject, Cyan, new Vector2(4f, -4f));
            Image inner = CreateImage("Core Plate", outer.transform, new Color(0.025f, 0.08f, 0.13f, 1f));
            SetRect(inner.rectTransform, new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.88f), Vector2.zero, Vector2.zero);
            Text label = CreateText("Core Label", inner.transform, font, 20, TextAnchor.MiddleCenter, new Color(0.78f, 0.96f, 1f));
            Stretch(label.rectTransform, 4f);
            label.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -45f);
            label.text = "ANCHOR\nCORE";
        }

        private static void CreateLine(Transform parent, Vector2 start, Vector2 end, Color color, float width)
        {
            Image line = CreateImage("Protocol Link", parent, color);
            RectTransform rect = line.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            Vector2 delta = end - start;
            rect.sizeDelta = new Vector2(delta.magnitude, width);
            rect.anchoredPosition = (start + end) * 0.5f;
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        private static void AddUpgradeSystemToScene(string scenePath, UpgradeTreeConfig config)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            {
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            GameObject uiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiPrefabPath);
            if (uiPrefab == null)
            {
                return;
            }
            UpgradeTreeController view = UnityEngine.Object.FindObjectOfType<UpgradeTreeController>(true);
            if (view == null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(uiPrefab, scene);
                instance.name = "UpgradeTreeUI";
                view = instance.GetComponent<UpgradeTreeController>();
            }

            GameBootstrap bootstrap = UnityEngine.Object.FindObjectOfType<GameBootstrap>(true);
            bootstrap.ConfigureUpgradeSystem(config, view);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, scenePath);
        }

        private static T CreateOrLoad<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
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

        private static void AddOutline(GameObject target, Color color, Vector2 distance)
        {
            Outline outline = target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void Stretch(RectTransform rect, float margin)
        {
            SetRect(rect, Vector2.zero, Vector2.one, Vector2.one * margin, Vector2.one * -margin);
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

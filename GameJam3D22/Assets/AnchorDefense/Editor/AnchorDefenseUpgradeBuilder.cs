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
            AnchorDefenseCubeZoneBuilder.BuildAll();
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

            UpgradeNodeDefinition zoneDamageFragment = CreateOrLoad<UpgradeNodeDefinition>(NodeFolder + "/ZoneDamageFragment.asset");
            zoneDamageFragment.Configure(
                "zone.fragment.damage",
                "翠绿火控碎片",
                "解锁绿色区域配置：处于该区域内的炮塔伤害提高 50%。解锁后可在区域配置界面中分配。",
                "GRN",
                18,
                false,
                null,
                null);

            UnityEngine.Object[] dirtyAssets =
            {
                innerEffect, middleEffect, outerEffect, damageEffect, intervalEffect, healthEffect,
                innerNode, middleNode, outerNode, damageNode, intervalNode, healthNode, zoneDamageFragment
            };
            for (int i = 0; i < dirtyAssets.Length; i++)
            {
                EditorUtility.SetDirty(dirtyAssets[i]);
            }

            UpgradeTreeConfig tree = CreateOrLoad<UpgradeTreeConfig>(UpgradeTreePath);
            tree.Configure(new[] { innerNode, middleNode, outerNode, damageNode, intervalNode, healthNode, zoneDamageFragment });
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
            AssignUiFonts(root);
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

            TurretSlot[] existingSlots = turretRoot.GetComponentsInChildren<TurretSlot>(true);
            if (existingSlots.Length > 0)
            {
                var initialSlots = new List<TurretSlot>();
                var upgradeSlots = new List<TurretSlot>();
                for (int i = 0; i < existingSlots.Length; i++)
                {
                    if (existingSlots[i].StartsUnlocked)
                    {
                        initialSlots.Add(existingSlots[i]);
                    }
                    else
                    {
                        upgradeSlots.Add(existingSlots[i]);
                    }
                }

                float slotRadius = initialSlots.Count > 0 ? initialSlots[0].transform.localPosition.magnitude : GetRingRadius(ringId);
                float[] slotAngles = { 30f, 210f };
                for (int i = upgradeSlots.Count; i < slotAngles.Length; i++)
                {
                    GameObject slotObject = new GameObject($"Upgrade Turret Slot {i + 1:00}");
                    slotObject.transform.SetParent(turretRoot, false);
                    float angle = slotAngles[i] * Mathf.Deg2Rad;
                    Vector3 position = new Vector3(Mathf.Cos(angle) * slotRadius, 0f, Mathf.Sin(angle) * slotRadius);
                    slotObject.transform.localPosition = position;
                    slotObject.transform.localRotation = Quaternion.LookRotation(position.normalized, Vector3.up);
                    TurretSlot slot = slotObject.AddComponent<TurretSlot>();
                    slot.Configure(turretPrefab.GetComponent<TurretController>(), false);
                    upgradeSlots.Add(slot);
                }
                ring.ConfigureTurretSlotAssets(ringId, initialSlots.ToArray(), upgradeSlots.ToArray());
                AssignUiFonts(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
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
            AssignUiFonts(root);
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
            Font font = LoadEnglishUiFont();
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
            treeArea.gameObject.AddComponent<RectMask2D>();
            RectTransform treeContent = CreateRect("Draggable Protocol Canvas", treeArea.transform);
            treeContent.anchorMin = treeContent.anchorMax = new Vector2(0.5f, 0.5f);
            treeContent.pivot = new Vector2(0.5f, 0.5f);
            treeContent.sizeDelta = new Vector2(1500f, 1450f);
            treeContent.anchoredPosition = Vector2.zero;
            ScrollRect protocolScroll = treeArea.gameObject.AddComponent<ScrollRect>();
            protocolScroll.viewport = treeArea.rectTransform;
            protocolScroll.content = treeContent;
            protocolScroll.horizontal = true;
            protocolScroll.vertical = true;
            protocolScroll.movementType = ScrollRect.MovementType.Clamped;
            protocolScroll.inertia = true;
            protocolScroll.decelerationRate = 0.12f;

            UpgradeNodeDefinition inner = config.FindNode("ring.inner.capacity");
            UpgradeNodeDefinition middle = config.FindNode("ring.middle.capacity");
            UpgradeNodeDefinition outer = config.FindNode("ring.outer.capacity");
            UpgradeNodeDefinition damage = config.FindNode("turret.damage.01");
            UpgradeNodeDefinition interval = config.FindNode("turret.interval.01");
            UpgradeNodeDefinition health = config.FindNode("turret.health.01");
            UpgradeNodeDefinition zoneDamageFragment = config.FindNode("zone.fragment.damage");

            Vector2 turretHub = new Vector2(0f, 330f);
            Vector2 innerPos = new Vector2(-230f, 420f);
            Vector2 middlePos = new Vector2(-420f, 500f);
            Vector2 outerPos = new Vector2(-590f, 585f);
            Vector2 damagePos = new Vector2(95f, 480f);
            Vector2 intervalPos = new Vector2(285f, 585f);
            Vector2 healthPos = new Vector2(330f, 285f);
            Vector2 zoneHub = new Vector2(0f, -315f);
            Vector2 zoneDamagePos = new Vector2(0f, -510f);

            CreateSectionLabel(treeContent, font, "TURRET ANCHOR PROTOCOLS  /  炮塔与轨道升级", new Vector2(0f, 665f), Cyan);
            CreateSectionLabel(treeContent, font, "CUBE FIELD FRAGMENTS  /  立方体区块功能解锁", new Vector2(0f, -85f), new Color(0.28f, 1f, 0.55f));
            Image divider = CreateImage("Protocol Branch Divider", treeContent, new Color(0.12f, 0.35f, 0.46f, 0.8f));
            RectTransform dividerRect = divider.rectTransform;
            dividerRect.anchorMin = dividerRect.anchorMax = new Vector2(0.5f, 0.5f);
            dividerRect.sizeDelta = new Vector2(1320f, 3f);
            dividerRect.anchoredPosition = new Vector2(0f, -5f);

            CreateLine(treeContent, turretHub, innerPos, Cyan, 4f);
            CreateLine(treeContent, innerPos, middlePos, Violet, 4f);
            CreateLine(treeContent, middlePos, outerPos, Orange, 4f);
            CreateLine(treeContent, turretHub, damagePos, Gold, 4f);
            CreateLine(treeContent, damagePos, intervalPos, Mint, 4f);
            CreateLine(treeContent, turretHub, healthPos, Mint, 4f);
            CreateLine(treeContent, zoneHub, zoneDamagePos, new Color(0.25f, 1f, 0.45f), 4f);

            var views = new List<UpgradeNodeView>
            {
                CreateNode(treeContent, font, innerPos, inner, Cyan),
                CreateNode(treeContent, font, middlePos, middle, Violet),
                CreateNode(treeContent, font, outerPos, outer, Orange),
                CreateNode(treeContent, font, damagePos, damage, Gold),
                CreateNode(treeContent, font, intervalPos, interval, Mint),
                CreateNode(treeContent, font, healthPos, health, Mint),
                CreateNode(treeContent, font, zoneDamagePos, zoneDamageFragment, new Color(0.25f, 1f, 0.45f))
            };

            Vector2[] turretPlaceholderPositions =
            {
                new Vector2(-640f, 350f), new Vector2(-340f, 270f), new Vector2(-120f, 590f),
                new Vector2(470f, 500f), new Vector2(520f, 240f)
            };
            for (int i = 0; i < turretPlaceholderPositions.Length; i++)
            {
                CreateLine(treeContent, turretHub, turretPlaceholderPositions[i], new Color(0.12f, 0.2f, 0.3f, 0.7f), 2f);
                views.Add(CreateNode(treeContent, font, turretPlaceholderPositions[i], null, new Color(0.2f, 0.3f, 0.42f)));
            }
            Vector2[] fieldPlaceholderPositions =
            {
                new Vector2(-330f, -430f), new Vector2(330f, -430f),
                new Vector2(-230f, -610f), new Vector2(230f, -610f)
            };
            for (int i = 0; i < fieldPlaceholderPositions.Length; i++)
            {
                CreateLine(treeContent, zoneHub, fieldPlaceholderPositions[i], new Color(0.1f, 0.3f, 0.24f, 0.75f), 2f);
                views.Add(CreateNode(treeContent, font, fieldPlaceholderPositions[i], null, new Color(0.16f, 0.34f, 0.3f)));
            }
            CreateAnchorHub(treeContent, font, turretHub, "TURRET\nCORE", Cyan);
            CreateAnchorHub(treeContent, font, zoneHub, "FIELD\nCORE", new Color(0.25f, 1f, 0.55f));

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
            footer.text = "拖动左侧画布浏览上下协议区    ·    U 打开/关闭    ·    击杀敌人获得协议点";

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

            AssignUiFonts(canvasObject);
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

        private static void CreateAnchorHub(Transform parent, Font font, Vector2 position,
            string labelText, Color accent)
        {
            Image outer = CreateImage("Anchor Core", parent, new Color(0.08f, 0.3f, 0.42f, 1f));
            RectTransform rect = outer.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(156f, 156f);
            rect.anchoredPosition = position;
            rect.localRotation = Quaternion.Euler(0f, 0f, 45f);
            AddOutline(outer.gameObject, accent, new Vector2(4f, -4f));
            Image inner = CreateImage("Core Plate", outer.transform, new Color(0.025f, 0.08f, 0.13f, 1f));
            SetRect(inner.rectTransform, new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.88f), Vector2.zero, Vector2.zero);
            Text label = CreateText("Core Label", inner.transform, font, 20, TextAnchor.MiddleCenter, new Color(0.78f, 0.96f, 1f));
            Stretch(label.rectTransform, 4f);
            label.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -45f);
            label.text = labelText;
        }

        private static void CreateSectionLabel(Transform parent, Font font, string text,
            Vector2 position, Color color)
        {
            Text label = CreateText(text, parent, font, 22, TextAnchor.MiddleCenter, color);
            RectTransform rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(760f, 48f);
            rect.anchoredPosition = position;
            label.text = text;
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

        private static RectTransform CreateRect(string objectName, Transform parent)
        {
            GameObject root = new GameObject(objectName, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            return root.GetComponent<RectTransform>();
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

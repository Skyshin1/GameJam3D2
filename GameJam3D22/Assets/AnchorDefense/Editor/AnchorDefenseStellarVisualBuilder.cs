using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.Rendering.Universal;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEngine;
using UnityEngine.Rendering;

namespace AnchorDefense.Editor
{
    // Updates presentation assets only; safe to reapply without rebuilding the gameplay scene.
    public static class AnchorDefenseStellarVisualBuilder
    {
        private const string Prefabs = "Assets/AnchorDefense/Prefabs";
        private const string Materials = "Assets/AnchorDefense/Art/Materials/StellarReadability";
        private const string Models = "Assets/AnchorDefense/Art/PolygonParticles/Models/";
        private const float TurretEmission = 0.35f;

        [MenuItem("Tools/Anchor Defense/Apply Stellar Rings and Soft Turret Glow")]
        public static void ApplyAll()
        {
            EnsureMaterialFolder();
            var changed = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { Prefabs + "/Gameplay", Prefabs + "/Directional" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                bool ring = asset.GetComponent<OrbitRingController>() != null;
                bool turret = asset.GetComponent<TurretController>() != null;
                if (!ring && !turret) continue;
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if (ring) ApplyRing(root);
                    if (turret) ApplyTurret(root);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    changed.Add(path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Stellar presentation applied to " + changed.Count + " prefabs:\n" + string.Join("\n", changed));
        }

        public static void ApplyRing(GameObject root)
        {
            OrbitRingController ring = root.GetComponent<OrbitRingController>();
            Transform visual = root.transform.Find("VisualRoot");
            if (ring == null || visual == null) throw new InvalidOperationException("Ring visual root is missing: " + root.name);
            Mesh star = LoadMesh("FX_Star.fbx");
            Mesh crystal = LoadMesh("FX_CrystalShard_01.fbx");
            TurretSlot slot = root.GetComponentInChildren<TurretSlot>(true);
            float radius = slot != null ? Vector3.ProjectOnPlane(visual.InverseTransformPoint(slot.transform.position), Vector3.up).magnitude :
                ring.RingId == OrbitRingId.Inner ? 4.7f : ring.RingId == OrbitRingId.Middle ? 6.1f : 7.5f;

            // Only remove the former decorative primitives, never the selection hit areas or turret slots.
            foreach (Transform child in visual.Cast<Transform>().ToArray())
                if (child.name == "Energy Node") UnityEngine.Object.DestroyImmediate(child.gameObject);
            Transform nodes = GetChild(visual, "Stellar Nodes");
            var renderers = new List<Renderer>();
            Material material = GetRingMaterial(ring);
            for (int i = 0; i < 12; i++)
            {
                bool isStar = i % 3 == 0;
                float angle = (15f + i * 30f) * Mathf.Deg2Rad;
                Transform node = GetChild(nodes, (isStar ? "Star " : "Crystal ") + (i + 1).ToString("00"));
                node.localPosition = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                node.localRotation = Quaternion.LookRotation(new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle)), Vector3.up);
                node.localScale = Vector3.one;
                Transform geometry = GetChild(node, "Geometry");
                Mesh mesh = isStar ? star : crystal;
                Vector3 size = mesh.bounds.size;
                int axis = isStar ? SmallestAxis(size) : LargestAxis(size);
                Quaternion rotation = Quaternion.FromToRotation(Axis(axis), isStar ? Vector3.up : Vector3.forward);
                float scale = (isStar ? 0.32f : 0.24f) / Mathf.Max(size.x, size.y, size.z, 0.0001f);
                geometry.localRotation = rotation;
                geometry.localScale = Vector3.one * scale;
                geometry.localPosition = -(rotation * (mesh.bounds.center * scale));
                MeshFilter filter = geometry.GetComponent<MeshFilter>();
                if (filter == null) filter = geometry.gameObject.AddComponent<MeshFilter>();
                filter.sharedMesh = mesh;
                MeshRenderer renderer = geometry.GetComponent<MeshRenderer>();
                if (renderer == null) renderer = geometry.gameObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderers.Add(renderer);
            }
            var serialized = new SerializedObject(ring);
            SerializedProperty selection = serialized.FindProperty("selectionRenderers");
            var body = Enumerable.Range(0, selection.arraySize)
                .Select(i => selection.GetArrayElementAtIndex(i).objectReferenceValue as Renderer)
                .Where(r => r != null && !r.transform.IsChildOf(nodes)).ToArray();
            selection.arraySize = body.Length;
            for (int i = 0; i < body.Length; i++) selection.GetArrayElementAtIndex(i).objectReferenceValue = body[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            ring.ConfigureDecorations(renderers.ToArray());
            EditorUtility.SetDirty(ring);
        }

        public static void ApplyTurret(GameObject root)
        {
            EnsureMaterialFolder();
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;
                Material[] slots = renderer.sharedMaterials;
                for (int i = 0; i < slots.Length; i++)
                {
                    Material source = slots[i];
                    if (source == null || source.shader == null || source.shader.name.IndexOf("Unlit", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        source.shader.name.Contains("DirectionalSprite") || source.shader.name.StartsWith("Sprites/", StringComparison.Ordinal)) continue;
                    slots[i] = GetTurretMaterial(source);
                }
                renderer.sharedMaterials = slots;
                if (PrefabUtility.IsPartOfPrefabInstance(renderer))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
        }

        private static Material GetTurretMaterial(Material source)
        {
            string sourcePath = AssetDatabase.GetAssetPath(source);
            // Keep authored copies and avoid repeated gamma/linear round-trips on reapplication.
            if (sourcePath.StartsWith(Materials + "/Turret_", StringComparison.Ordinal)) return source;
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source, out string guid, out long fileId))
                throw new InvalidOperationException("Cannot identify turret material: " + source.name);
            string path = Materials + "/Turret_" + guid + "_" + fileId + ".mat";
            Material result = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (result == null)
            {
                result = new Material(source);
                AssetDatabase.CreateAsset(result, path);
            }
            else
            {
                result.shader = source.shader;
                result.CopyPropertiesFromMaterial(source);
            }
            result.name = source.name + " Soft Glow";

            bool urp = source.shader.name == "Universal Render Pipeline/Lit";
            string baseMap = source.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
            Color tint = source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") : source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white;
            Texture texture = source.HasProperty(baseMap) ? source.GetTexture(baseMap) : null;
            Vector2 tiling = source.HasProperty(baseMap) ? source.GetTextureScale(baseMap) : Vector2.one;
            Vector2 offset = source.HasProperty(baseMap) ? source.GetTextureOffset(baseMap) : Vector2.zero;
            bool clip = source.IsKeywordEnabled("_ALPHATEST_ON") || source.HasProperty("_AlphaClip") && source.GetFloat("_AlphaClip") > 0.5f;
            bool transparent = source.HasProperty("_Surface") ? source.GetFloat("_Surface") > 0.5f : source.HasProperty("_Mode") && source.GetFloat("_Mode") >= 2f;
            if (source.shader.name == "Standard" || source.shader.name == "Standard (Specular setup)")
            {
                new StandardUpgrader(source.shader.name).Upgrade(result, MaterialUpgrader.UpgradeFlags.None);
            }
            else if (!urp)
            {
                result.shader = Shader.Find("Universal Render Pipeline/Lit");
                result.SetFloat("_Smoothness", source.HasProperty("_Glossiness") ? source.GetFloat("_Glossiness") : 0.3f);
                result.SetFloat("_AlphaClip", clip ? 1f : 0f);
                result.SetFloat("_Surface", transparent ? 1f : 0f);
                result.SetFloat("_SrcBlend", transparent ? (float)BlendMode.SrcAlpha : (float)BlendMode.One);
                result.SetFloat("_DstBlend", transparent ? (float)BlendMode.OneMinusSrcAlpha : (float)BlendMode.Zero);
                result.SetFloat("_ZWrite", transparent ? 0f : 1f);
                result.SetOverrideTag("RenderType", transparent ? "Transparent" : clip ? "TransparentCutout" : "Opaque");
                result.renderQueue = transparent ? (int)RenderQueue.Transparent : clip ? (int)RenderQueue.AlphaTest : -1;
                if (transparent) result.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                else result.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                if (clip) result.EnableKeyword("_ALPHATEST_ON");
                else result.DisableKeyword("_ALPHATEST_ON");
            }
            result.SetTexture("_BaseMap", texture);
            result.SetTextureScale("_BaseMap", tiling);
            result.SetTextureOffset("_BaseMap", offset);
            result.SetColor("_BaseColor", tint);
            result.SetTexture("_EmissionMap", texture);
            result.SetTextureScale("_EmissionMap", tiling);
            result.SetTextureOffset("_EmissionMap", offset);
            result.SetColor("_EmissionColor", new Color(tint.r * TurretEmission, tint.g * TurretEmission, tint.b * TurretEmission, 1f));
            result.EnableKeyword("_EMISSION");
            result.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            BaseShaderGUI.SetMaterialKeywords(result, LitGUI.SetMaterialKeywords);
            EditorUtility.SetDirty(result);
            return result;
        }

        private static Material GetRingMaterial(OrbitRingController ring)
        {
            EnsureMaterialFolder();
            string path = Materials + "/OrbitPearl_" + ring.RingId + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            Color color = new SerializedObject(ring).FindProperty("normalColor").colorValue;
            Color pearl = Color.Lerp(color, Color.white, 0.55f);
            pearl.a = 1f;
            material.SetColor("_BaseColor", pearl);
            material.SetColor("_EmissionColor", pearl * 0.25f);
            material.EnableKeyword("_EMISSION");
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0.3f);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Mesh LoadMesh(string file)
        {
            Mesh mesh = AssetDatabase.LoadAllAssetsAtPath(Models + file).OfType<Mesh>().FirstOrDefault();
            if (mesh == null) throw new InvalidOperationException("Stellar model is missing: " + file);
            return mesh;
        }

        private static Transform GetChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null) return child;
            child = new GameObject(name).transform;
            child.SetParent(parent, false);
            child.gameObject.layer = parent.gameObject.layer;
            return child;
        }

        private static int LargestAxis(Vector3 size) => size.x >= size.y && size.x >= size.z ? 0 : size.y >= size.z ? 1 : 2;
        private static int SmallestAxis(Vector3 size) => size.x <= size.y && size.x <= size.z ? 0 : size.y <= size.z ? 1 : 2;
        private static Vector3 Axis(int axis) => axis == 0 ? Vector3.right : axis == 1 ? Vector3.up : Vector3.forward;

        private static void EnsureMaterialFolder()
        {
            if (!AssetDatabase.IsValidFolder(Materials))
                AssetDatabase.CreateFolder("Assets/AnchorDefense/Art/Materials", "StellarReadability");
        }
    }
}

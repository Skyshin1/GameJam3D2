using System;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AnchorDefense.Editor
{
    public static class GlobalOutlineInstaller
    {
        private const string ShaderPath = "Assets/AnchorDefense/Shaders/GlobalOutline.shader";
        private const string VolumeProfilePath = "Assets/AnchorDefense/Scenes/Gameplay/Global Volume Profile.asset";

        private static readonly string[] RendererDataPaths =
        {
            "Assets/Settings/URP-Performant-Renderer.asset",
            "Assets/Settings/URP-Balanced-Renderer.asset",
            "Assets/Settings/URP-HighFidelity-Renderer.asset"
        };

        [MenuItem("Tools/Anchor Defense/Install or Refresh Global Outline")]
        public static void Install()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (shader == null)
            {
                throw new InvalidOperationException($"Global outline shader was not found at '{ShaderPath}'.");
            }

            for (int i = 0; i < RendererDataPaths.Length; i++)
            {
                InstallRendererFeature(RendererDataPaths[i], shader);
            }

            InstallVolumeComponent();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Global outline installed in all URP renderer tiers and the shared Gameplay Volume Profile.");
        }

        private static void InstallRendererFeature(string path, Shader shader)
        {
            ScriptableRendererData rendererData = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(path);
            if (rendererData == null)
            {
                throw new InvalidOperationException($"Renderer Data was not found at '{path}'.");
            }

            GlobalOutlineRendererFeature feature = null;
            for (int i = 0; i < rendererData.rendererFeatures.Count; i++)
            {
                if (rendererData.rendererFeatures[i] is GlobalOutlineRendererFeature existingFeature)
                {
                    feature = existingFeature;
                    break;
                }
            }

            if (feature == null)
            {
                feature = ScriptableObject.CreateInstance<GlobalOutlineRendererFeature>();
                feature.name = "Anchor Defense Global Outline";
                AssetDatabase.AddObjectToAsset(feature, rendererData);
                rendererData.rendererFeatures.Add(feature);
                UpdateRendererFeatureMap(rendererData, rendererData.rendererFeatures.Count - 1, feature);
            }

            feature.Configure(shader, RenderPassEvent.BeforeRenderingPostProcessing, true, false);
            feature.SetActive(true);
            feature.Create();
            rendererData.SetDirty();
            EditorUtility.SetDirty(feature);
            EditorUtility.SetDirty(rendererData);
        }

        private static void UpdateRendererFeatureMap(
            ScriptableRendererData rendererData,
            int featureIndex,
            ScriptableRendererFeature feature)
        {
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId))
            {
                return;
            }

            var serializedRenderer = new SerializedObject(rendererData);
            SerializedProperty featureMap = serializedRenderer.FindProperty("m_RendererFeatureMap");
            featureMap.arraySize = rendererData.rendererFeatures.Count;
            featureMap.GetArrayElementAtIndex(featureIndex).longValue = localId;
            serializedRenderer.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void InstallVolumeComponent()
        {
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile == null)
            {
                throw new InvalidOperationException($"Gameplay Volume Profile was not found at '{VolumeProfilePath}'.");
            }

            if (!profile.TryGet(out GlobalOutlineVolume settings))
            {
                settings = VolumeProfileFactory.CreateVolumeComponent<GlobalOutlineVolume>(profile, true, false);
            }

            SetOverride(settings.enabled, true);
            SetOverride(settings.outlineColor, new Color(0.015f, 0.035f, 0.08f, 1f));
            SetOverride(settings.opacity, 0.9f);
            SetOverride(settings.thickness, 1.35f);
            SetOverride(settings.blendMode, GlobalOutlineBlendMode.SolidColor);
            SetOverride(settings.depthWeight, 1f);
            SetOverride(settings.normalWeight, 0.75f);
            SetOverride(settings.colorWeight, 0.2f);
            SetOverride(settings.edgeCombination, GlobalOutlineEdgeCombination.Maximum);
            SetOverride(settings.depthThreshold, 0.012f);
            SetOverride(settings.normalThreshold, 0.16f);
            SetOverride(settings.colorThreshold, 0.12f);
            SetOverride(settings.thresholdSoftness, 0.06f);
            SetOverride(settings.distanceFade, false);
            SetOverride(settings.fadeStart, 20f);
            SetOverride(settings.fadeEnd, 65f);
            SetOverride(settings.includeSkyEdges, false);
            SetOverride(settings.debugMode, GlobalOutlineDebugMode.None);
            settings.active = true;
            profile.Reset();
            EditorUtility.SetDirty(settings);
            EditorUtility.SetDirty(profile);
        }

        private static void SetOverride<T>(VolumeParameter<T> parameter, T value)
        {
            parameter.overrideState = true;
            parameter.value = value;
        }
    }
}

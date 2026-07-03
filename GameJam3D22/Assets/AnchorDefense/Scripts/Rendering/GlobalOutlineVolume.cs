using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AnchorDefense
{
    public enum GlobalOutlineBlendMode
    {
        SolidColor,
        Multiply,
        Additive
    }

    public enum GlobalOutlineEdgeCombination
    {
        Maximum,
        Additive
    }

    public enum GlobalOutlineDebugMode
    {
        None,
        CombinedEdges,
        DepthEdges,
        NormalEdges,
        ColorEdges
    }

    [Serializable]
    public sealed class GlobalOutlineBlendModeParameter : VolumeParameter<GlobalOutlineBlendMode>
    {
        public GlobalOutlineBlendModeParameter(GlobalOutlineBlendMode value, bool overrideState = false)
            : base(value, overrideState) { }
    }

    [Serializable]
    public sealed class GlobalOutlineEdgeCombinationParameter : VolumeParameter<GlobalOutlineEdgeCombination>
    {
        public GlobalOutlineEdgeCombinationParameter(GlobalOutlineEdgeCombination value, bool overrideState = false)
            : base(value, overrideState) { }
    }

    [Serializable]
    public sealed class GlobalOutlineDebugModeParameter : VolumeParameter<GlobalOutlineDebugMode>
    {
        public GlobalOutlineDebugModeParameter(GlobalOutlineDebugMode value, bool overrideState = false)
            : base(value, overrideState) { }
    }

    [Serializable, VolumeComponentMenu("Anchor Defense/Global Outline")]
    public sealed class GlobalOutlineVolume : VolumeComponent, IPostProcessComponent
    {
        [Header("Master")]
        [Tooltip("Turns the full-screen outline pass on or off.")]
        public BoolParameter enabled = new BoolParameter(true);

        [Tooltip("Final outline color. Alpha is multiplied by Opacity.")]
        public ColorParameter outlineColor = new ColorParameter(new Color(0.015f, 0.035f, 0.08f, 1f), true, false, true);

        [Tooltip("Outline strength after edge detection.")]
        public ClampedFloatParameter opacity = new ClampedFloatParameter(0.9f, 0f, 1f);

        [Tooltip("Sampling radius in screen pixels.")]
        public ClampedFloatParameter thickness = new ClampedFloatParameter(1.35f, 0.25f, 8f);

        [Tooltip("How the outline color is combined with the scene.")]
        public GlobalOutlineBlendModeParameter blendMode = new GlobalOutlineBlendModeParameter(GlobalOutlineBlendMode.SolidColor);

        [Header("Edge Sources")]
        [Tooltip("Contribution of depth discontinuities and silhouettes.")]
        public ClampedFloatParameter depthWeight = new ClampedFloatParameter(1f, 0f, 4f);

        [Tooltip("Contribution of surface normal changes and hard creases.")]
        public ClampedFloatParameter normalWeight = new ClampedFloatParameter(0.75f, 0f, 4f);

        [Tooltip("Contribution of image color changes, including texture detail.")]
        public ClampedFloatParameter colorWeight = new ClampedFloatParameter(0.2f, 0f, 4f);

        [Tooltip("Use Maximum for cleaner lines or Additive for denser outlines.")]
        public GlobalOutlineEdgeCombinationParameter edgeCombination =
            new GlobalOutlineEdgeCombinationParameter(GlobalOutlineEdgeCombination.Maximum);

        [Header("Detection Thresholds")]
        [Tooltip("Minimum relative eye-depth difference that produces an edge.")]
        public ClampedFloatParameter depthThreshold = new ClampedFloatParameter(0.012f, 0.0001f, 1f);

        [Tooltip("Minimum normal direction difference that produces an edge.")]
        public ClampedFloatParameter normalThreshold = new ClampedFloatParameter(0.16f, 0.001f, 1f);

        [Tooltip("Minimum RGB difference that produces an edge.")]
        public ClampedFloatParameter colorThreshold = new ClampedFloatParameter(0.12f, 0.001f, 2f);

        [Tooltip("Soft transition range above each threshold.")]
        public ClampedFloatParameter thresholdSoftness = new ClampedFloatParameter(0.06f, 0.001f, 0.5f);

        [Header("Distance and Background")]
        [Tooltip("Fade outlines by eye-space distance.")]
        public BoolParameter distanceFade = new BoolParameter(false);

        public MinFloatParameter fadeStart = new MinFloatParameter(20f, 0f);
        public MinFloatParameter fadeEnd = new MinFloatParameter(65f, 0.01f);

        [Tooltip("Allow edges whose center sample is the sky/background.")]
        public BoolParameter includeSkyEdges = new BoolParameter(false);

        [Header("Diagnostics")]
        [Tooltip("Visualize individual edge sources while tuning.")]
        public GlobalOutlineDebugModeParameter debugMode = new GlobalOutlineDebugModeParameter(GlobalOutlineDebugMode.None);

        public bool IsActive()
        {
            return active && enabled.value && opacity.value > 0f && thickness.value > 0f;
        }

        public bool IsTileCompatible()
        {
            return false;
        }
    }
}

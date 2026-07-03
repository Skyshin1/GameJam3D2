using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AnchorDefense
{
    public sealed class GlobalOutlineRendererFeature : ScriptableRendererFeature
    {
        [SerializeField] private Shader outlineShader;
        [SerializeField] private RenderPassEvent injectionPoint = RenderPassEvent.BeforeRenderingPostProcessing;
        [SerializeField] private bool showInSceneView = true;
        [SerializeField] private bool applyToOverlayCameras;

        private Material outlineMaterial;
        private GlobalOutlinePass outlinePass;

        public void Configure(
            Shader shader,
            RenderPassEvent passEvent = RenderPassEvent.BeforeRenderingPostProcessing,
            bool enableSceneView = true,
            bool enableOverlayCameras = false)
        {
            outlineShader = shader;
            injectionPoint = passEvent;
            showInSceneView = enableSceneView;
            applyToOverlayCameras = enableOverlayCameras;
        }

        public override void Create()
        {
            CoreUtils.Destroy(outlineMaterial);
            outlineMaterial = outlineShader != null ? CoreUtils.CreateEngineMaterial(outlineShader) : null;
            outlinePass = outlineMaterial != null ? new GlobalOutlinePass(outlineMaterial, injectionPoint) : null;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (outlinePass == null || !ShouldRender(in renderingData) || !IsVolumeActive())
            {
                return;
            }

            renderer.EnqueuePass(outlinePass);
        }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            if (outlinePass == null || !ShouldRender(in renderingData) || !IsVolumeActive())
            {
                return;
            }

            outlinePass.SetSource(renderer.cameraColorTargetHandle);
        }

        protected override void Dispose(bool disposing)
        {
            outlinePass?.Dispose();
            outlinePass = null;
            CoreUtils.Destroy(outlineMaterial);
            outlineMaterial = null;
        }

        private bool ShouldRender(in RenderingData renderingData)
        {
            CameraData cameraData = renderingData.cameraData;
            if (cameraData.cameraType == CameraType.Preview || cameraData.cameraType == CameraType.Reflection)
            {
                return false;
            }
            if (cameraData.cameraType == CameraType.SceneView && !showInSceneView)
            {
                return false;
            }
            if (cameraData.renderType == CameraRenderType.Overlay && !applyToOverlayCameras)
            {
                return false;
            }
            return true;
        }

        private static bool IsVolumeActive()
        {
            GlobalOutlineVolume settings = VolumeManager.instance.stack.GetComponent<GlobalOutlineVolume>();
            return settings != null && settings.IsActive();
        }

        private sealed class GlobalOutlinePass : ScriptableRenderPass
        {
            private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
            private static readonly int OutlineParamsId = Shader.PropertyToID("_OutlineParams");
            private static readonly int EdgeWeightsId = Shader.PropertyToID("_EdgeWeights");
            private static readonly int EdgeThresholdsId = Shader.PropertyToID("_EdgeThresholds");
            private static readonly int FadeParamsId = Shader.PropertyToID("_FadeParams");
            private static readonly int ModeParamsId = Shader.PropertyToID("_ModeParams");

            private readonly Material material;
            private readonly ProfilingSampler outlineProfilingSampler = new ProfilingSampler("Anchor Defense Global Outline");
            private RTHandle source;
            private RTHandle colorCopy;

            public GlobalOutlinePass(Material outlineMaterial, RenderPassEvent passEvent)
            {
                material = outlineMaterial;
                renderPassEvent = passEvent;
                ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
            }

            public void SetSource(RTHandle cameraColorSource)
            {
                source = cameraColorSource;
            }

            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                RenderTextureDescriptor descriptor = cameraTextureDescriptor;
                descriptor.depthBufferBits = 0;
                descriptor.msaaSamples = 1;
                RenderingUtils.ReAllocateIfNeeded(
                    ref colorCopy,
                    descriptor,
                    FilterMode.Bilinear,
                    TextureWrapMode.Clamp,
                    name: "_AnchorDefenseOutlineColorCopy");
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (source == null || colorCopy == null)
                {
                    return;
                }

                GlobalOutlineVolume settings = VolumeManager.instance.stack.GetComponent<GlobalOutlineVolume>();
                if (settings == null || !settings.IsActive())
                {
                    return;
                }

                material.SetColor(OutlineColorId, settings.outlineColor.value);
                material.SetVector(OutlineParamsId, new Vector4(
                    settings.thickness.value,
                    settings.opacity.value,
                    settings.thresholdSoftness.value,
                    settings.distanceFade.value ? 1f : 0f));
                material.SetVector(EdgeWeightsId, new Vector4(
                    settings.depthWeight.value,
                    settings.normalWeight.value,
                    settings.colorWeight.value,
                    0f));
                material.SetVector(EdgeThresholdsId, new Vector4(
                    settings.depthThreshold.value,
                    settings.normalThreshold.value,
                    settings.colorThreshold.value,
                    0f));
                material.SetVector(FadeParamsId, new Vector4(
                    settings.fadeStart.value,
                    Mathf.Max(settings.fadeStart.value + 0.001f, settings.fadeEnd.value),
                    settings.includeSkyEdges.value ? 1f : 0f,
                    0f));
                material.SetVector(ModeParamsId, new Vector4(
                    (float)settings.blendMode.value,
                    (float)settings.edgeCombination.value,
                    (float)settings.debugMode.value,
                    0f));

                CommandBuffer commandBuffer = CommandBufferPool.Get();
                using (new ProfilingScope(commandBuffer, outlineProfilingSampler))
                {
                    Blitter.BlitCameraTexture(commandBuffer, source, colorCopy);
                    Blitter.BlitCameraTexture(commandBuffer, colorCopy, source, material, 0);
                }
                context.ExecuteCommandBuffer(commandBuffer);
                CommandBufferPool.Release(commandBuffer);
            }

            public void Dispose()
            {
                colorCopy?.Release();
                colorCopy = null;
            }
        }
    }
}

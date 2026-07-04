using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ScreenSpaceOutlines : ScriptableRendererFeature
{
    [System.Serializable]
    private class ScreenSpaceOutlineSettings
    {
        [Header("General Outline Settings")]
        public Color outlineColor = Color.black;

        [Range(0.0f, 20.0f)]
        public float outlineScale = 1.0f;

        [Header("Depth Settings")]
        [Range(0.0f, 100.0f)]
        public float depthThreshold = 1.5f;

        [Range(0.0f, 500.0f)]
        public float robertsCrossMultiplier = 100.0f;

        [Header("Normal Settings")]
        [Range(0.0f, 1.0f)]
        public float normalThreshold = 0.4f;

        [Header("Depth Normal Relation Settings")]
        [Range(0.0f, 2.0f)]
        public float steepAngleThreshold = 0.2f;

        [Range(0.0f, 500.0f)]
        public float steepAngleMultiplier = 25.0f;

        [Header("View Space Normal Texture Settings")]
        public RenderTextureFormat colorFormat = RenderTextureFormat.ARGB32;
        public int depthBufferBits = 0;
        public FilterMode filterMode = FilterMode.Bilinear;
        public Color backgroundColor = Color.clear;

        [Header("Object Draw Settings")]
        public PerObjectData perObjectData = PerObjectData.None;
        public bool enableDynamicBatching = true;
        public bool enableInstancing = true;
    }

    private class ScreenSpaceOutlinePass : ScriptableRenderPass
    {
        private readonly Material screenSpaceOutlineMaterial;
        private readonly Material normalsMaterial;
        private readonly ScreenSpaceOutlineSettings settings;

        private readonly FilteringSettings filteringSettings;
        private readonly List<ShaderTagId> shaderTagIdList;

        private RTHandle normals;
        private RTHandle temporaryBuffer;

        private static readonly int SceneViewSpaceNormalsID = Shader.PropertyToID("_SceneViewSpaceNormals");

        public ScreenSpaceOutlinePass(
            RenderPassEvent renderPassEvent,
            LayerMask layerMask,
            ScreenSpaceOutlineSettings settings,
            Shader outlineShader,
            Shader normalsShader)
        {
            this.settings = settings;
            this.renderPassEvent = renderPassEvent;

            screenSpaceOutlineMaterial = CoreUtils.CreateEngineMaterial(outlineShader);
            normalsMaterial = CoreUtils.CreateEngineMaterial(normalsShader);

            screenSpaceOutlineMaterial.SetColor("_OutlineColor", settings.outlineColor);
            screenSpaceOutlineMaterial.SetFloat("_OutlineScale", settings.outlineScale);

            screenSpaceOutlineMaterial.SetFloat("_DepthThreshold", settings.depthThreshold);
            screenSpaceOutlineMaterial.SetFloat("_RobertsCrossMultiplier", settings.robertsCrossMultiplier);

            screenSpaceOutlineMaterial.SetFloat("_NormalThreshold", settings.normalThreshold);

            screenSpaceOutlineMaterial.SetFloat("_SteepAngleThreshold", settings.steepAngleThreshold);
            screenSpaceOutlineMaterial.SetFloat("_SteepAngleMultiplier", settings.steepAngleMultiplier);

            filteringSettings = new FilteringSettings(RenderQueueRange.opaque, layerMask);

            shaderTagIdList = new List<ShaderTagId>
            {
                new ShaderTagId("UniversalForward"),
                new ShaderTagId("UniversalForwardOnly"),
                new ShaderTagId("LightweightForward"),
                new ShaderTagId("SRPDefaultUnlit")
            };
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor textureDescriptor = renderingData.cameraData.cameraTargetDescriptor;

            textureDescriptor.colorFormat = settings.colorFormat;
            textureDescriptor.depthBufferBits = settings.depthBufferBits;
            RenderingUtils.ReAllocateIfNeeded(
                ref normals,
                textureDescriptor,
                settings.filterMode,
                TextureWrapMode.Clamp,
                name: "_SceneViewSpaceNormals"
            );

            textureDescriptor.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(
                ref temporaryBuffer,
                textureDescriptor,
                FilterMode.Bilinear,
                TextureWrapMode.Clamp,
                name: "_ScreenSpaceOutlineTemp"
            );

            ConfigureTarget(normals, renderingData.cameraData.renderer.cameraDepthTargetHandle);
            ConfigureClear(ClearFlag.Color, settings.backgroundColor);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (screenSpaceOutlineMaterial == null || normalsMaterial == null)
                return;

            if (temporaryBuffer == null || temporaryBuffer.rt == null)
                return;

            CommandBuffer cmd = CommandBufferPool.Get("ScreenSpaceOutlines");

            using (new ProfilingScope(cmd, new ProfilingSampler("ScreenSpaceOutlines")))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                DrawingSettings drawSettings = CreateDrawingSettings(
                    shaderTagIdList,
                    ref renderingData,
                    renderingData.cameraData.defaultOpaqueSortFlags
                );

                drawSettings.perObjectData = settings.perObjectData;
                drawSettings.enableDynamicBatching = settings.enableDynamicBatching;
                drawSettings.enableInstancing = settings.enableInstancing;
                drawSettings.overrideMaterial = normalsMaterial;

                RendererListParams rendererListParams = new RendererListParams(
                    renderingData.cullResults,
                    drawSettings,
                    filteringSettings
                );

                RendererList rendererList = context.CreateRendererList(ref rendererListParams);
                cmd.DrawRendererList(rendererList);

                cmd.SetGlobalTexture(SceneViewSpaceNormalsID, normals);

                RTHandle cameraColorTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;

                Blitter.BlitCameraTexture(
                    cmd,
                    cameraColorTarget,
                    temporaryBuffer,
                    screenSpaceOutlineMaterial,
                    0
                );

                Blitter.BlitCameraTexture(
                    cmd,
                    temporaryBuffer,
                    cameraColorTarget
                );
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Release()
        {
            CoreUtils.Destroy(screenSpaceOutlineMaterial);
            CoreUtils.Destroy(normalsMaterial);

            normals?.Release();
            temporaryBuffer?.Release();
        }
    }

    [SerializeField] private RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingSkybox;
    [SerializeField] private LayerMask outlinesLayerMask = ~0;
    [SerializeField] private ScreenSpaceOutlineSettings outlineSettings = new ScreenSpaceOutlineSettings();

    private ScreenSpaceOutlinePass screenSpaceOutlinePass;

    public override void Create()
    {
        if (renderPassEvent < RenderPassEvent.BeforeRenderingPrePasses)
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingPrePasses;
        }

        Shader outlineShader = Shader.Find("Hidden/Outlines");
        Shader normalsShader = Shader.Find("Hidden/ViewSpaceNormals");

        if (outlineShader == null)
        {
            Debug.LogError("ScreenSpaceOutlines: 找不到 Shader \"Hidden/Outlines\"。请检查 Outlines Shader 第一行的名字。");
            screenSpaceOutlinePass = null;
            return;
        }

        if (normalsShader == null)
        {
            Debug.LogError("ScreenSpaceOutlines: 找不到 Shader \"Hidden/ViewSpaceNormals\"。请检查 ViewSpaceNormals Shader 第一行的名字。");
            screenSpaceOutlinePass = null;
            return;
        }

        screenSpaceOutlinePass = new ScreenSpaceOutlinePass(
            renderPassEvent,
            outlinesLayerMask,
            outlineSettings,
            outlineShader,
            normalsShader
        );
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (screenSpaceOutlinePass == null)
            return;

        renderer.EnqueuePass(screenSpaceOutlinePass);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            screenSpaceOutlinePass?.Release();
        }
    }
}
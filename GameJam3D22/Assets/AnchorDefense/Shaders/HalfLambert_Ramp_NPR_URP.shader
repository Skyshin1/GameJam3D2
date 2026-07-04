Shader "Custom/HalfLambert Ramp NPR URP"
{
    Properties
    {
        // 基础颜色，会和 Base Map 贴图相乘。
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)

        // 主贴图，可以放角色或者物体的 Albedo / BaseColor。
        _BaseMap("Base Map", 2D) = "white" {}

        // Ramp 贴图，用来控制暗面、过渡、亮面的颜色。
        // 左边通常是暗面颜色，右边通常是亮面颜色。
        _RampMap("Ramp Map", 2D) = "white" {}

        // 漫反射整体亮度强度。
        _DiffuseStrength("Diffuse Strength", Range(0, 2)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"

            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                // 模型空间顶点位置。
                float4 positionOS : POSITION;

                // 模型空间法线。
                float3 normalOS : NORMAL;

                // 模型 UV。
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                // 裁剪空间位置，用于屏幕渲染。
                float4 positionHCS : SV_POSITION;

                // 世界空间法线，用来和光照方向做点乘。
                float3 normalWS : TEXCOORD0;

                // 贴图 UV。
                float2 uv : TEXCOORD1;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            TEXTURE2D(_RampMap);
            SAMPLER(sampler_RampMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float _DiffuseStrength;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                // 把模型空间坐标转换到裁剪空间，让物体能显示到屏幕上。
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);

                // 把模型空间法线转换到世界空间。
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);

                // 让 Base Map 的 Tiling / Offset 生效。
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 采样基础颜色贴图。
                half4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);

                // 基础颜色 = 贴图颜色 × 材质颜色。
                half3 albedo = baseMap.rgb * _BaseColor.rgb;

                // 归一化世界空间法线。
                half3 normalWS = normalize(input.normalWS);

                // 获取 URP 主方向光。
                Light mainLight = GetMainLight();

                // 主光方向。
                half3 lightDirWS = normalize(mainLight.direction);

                // 计算 N dot L，表示表面朝向光源的程度。
                half ndotl = dot(normalWS, lightDirWS);

                // Half-Lambert：
                // 把 dot(N, L) 从 [-1, 1] 映射到 [0, 1]。
                // 这样背光面不会完全黑。
                half halfLambert = saturate(ndotl * 0.5h + 0.5h);

                // 用 halfLambert 作为 Ramp 图的横坐标 U。
                // V 固定为 0.5，表示采样 Ramp 图中间一行。
                half2 rampUV = half2(halfLambert, 0.5h);

                // 从 Ramp 贴图中读取 NPR 明暗颜色。
                half3 rampColor = SAMPLE_TEXTURE2D(_RampMap, sampler_RampMap, rampUV).rgb;

                // 最终颜色：
                // 物体本身颜色 × Ramp 明暗颜色 × 主光颜色 × 漫反射强度。
                half3 finalColor = albedo * rampColor * mainLight.color * _DiffuseStrength;

                return half4(finalColor, baseMap.a * _BaseColor.a);
            }

            ENDHLSL
        }
    }
}
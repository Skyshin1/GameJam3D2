Shader "Hidden/AnchorDefense/GlobalOutline"
{
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }

        Pass
        {
            Name "Global Outline"
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            float4 _OutlineColor;
            float4 _OutlineParams;
            float4 _EdgeWeights;
            float4 _EdgeThresholds;
            float4 _FadeParams;
            float4 _ModeParams;

            float EdgeResponse(float value, float threshold, float softness)
            {
                float upper = threshold * (1.0 + softness * 10.0) + 0.00001;
                return smoothstep(threshold, upper, value);
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                float2 texel = _OutlineParams.x / max(_BlitTextureSize, float2(1.0, 1.0));
                half4 sceneColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                float centerRawDepth = SampleSceneDepth(uv);
                float centerDepth = LinearEyeDepth(centerRawDepth, _ZBufferParams);
                float3 centerNormal = normalize(SampleSceneNormals(uv));

                static const float2 directions[8] =
                {
                    float2(-1.0, 0.0), float2(1.0, 0.0),
                    float2(0.0, -1.0), float2(0.0, 1.0),
                    float2(-0.7071, -0.7071), float2(0.7071, -0.7071),
                    float2(-0.7071, 0.7071), float2(0.7071, 0.7071)
                };

                float maximumDepthDifference = 0.0;
                float maximumNormalDifference = 0.0;
                float maximumColorDifference = 0.0;

                [unroll]
                for (int sampleIndex = 0; sampleIndex < 8; sampleIndex++)
                {
                    float2 sampleUv = saturate(uv + directions[sampleIndex] * texel);
                    float sampleRawDepth = SampleSceneDepth(sampleUv);
                    float sampleDepth = LinearEyeDepth(sampleRawDepth, _ZBufferParams);
                    float3 sampleNormal = normalize(SampleSceneNormals(sampleUv));
                    half3 sampleColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, sampleUv).rgb;

                    float relativeDepthDifference = abs(sampleDepth - centerDepth) / max(centerDepth, 0.001);
                    float normalDifference = 1.0 - saturate(dot(centerNormal, sampleNormal));
                    float colorDifference = length(sampleColor - sceneColor.rgb) * 0.57735;

                    maximumDepthDifference = max(maximumDepthDifference, relativeDepthDifference);
                    maximumNormalDifference = max(maximumNormalDifference, normalDifference);
                    maximumColorDifference = max(maximumColorDifference, colorDifference);
                }

                float softness = _OutlineParams.z;
                float depthEdge = EdgeResponse(maximumDepthDifference, _EdgeThresholds.x, softness);
                float normalEdge = EdgeResponse(maximumNormalDifference, _EdgeThresholds.y, softness);
                float colorEdge = EdgeResponse(maximumColorDifference, _EdgeThresholds.z, softness);

                float3 weightedEdges = float3(depthEdge, normalEdge, colorEdge) * _EdgeWeights.xyz;
                float combinedEdge = _ModeParams.y < 0.5
                    ? max(weightedEdges.x, max(weightedEdges.y, weightedEdges.z))
                    : saturate(weightedEdges.x + weightedEdges.y + weightedEdges.z);

                bool centerIsSky = centerDepth >= _ProjectionParams.z * 0.999;
                if (_FadeParams.z < 0.5 && centerIsSky)
                {
                    combinedEdge = 0.0;
                    depthEdge = 0.0;
                    normalEdge = 0.0;
                    colorEdge = 0.0;
                }

                if (_OutlineParams.w > 0.5)
                {
                    float distanceFade = 1.0 - saturate((centerDepth - _FadeParams.x) / max(_FadeParams.y - _FadeParams.x, 0.001));
                    combinedEdge *= distanceFade;
                    depthEdge *= distanceFade;
                    normalEdge *= distanceFade;
                    colorEdge *= distanceFade;
                }

                int debugMode = (int)round(_ModeParams.z);
                if (debugMode == 1)
                    return half4(combinedEdge.xxx, 1.0);
                if (debugMode == 2)
                    return half4(depthEdge.xxx, 1.0);
                if (debugMode == 3)
                    return half4(normalEdge.xxx, 1.0);
                if (debugMode == 4)
                    return half4(colorEdge.xxx, 1.0);

                float outlineAmount = saturate(combinedEdge * _OutlineParams.y * _OutlineColor.a);
                int blendMode = (int)round(_ModeParams.x);
                half3 finalColor;
                if (blendMode == 1)
                {
                    finalColor = sceneColor.rgb * lerp(half3(1.0, 1.0, 1.0), _OutlineColor.rgb, outlineAmount);
                }
                else if (blendMode == 2)
                {
                    finalColor = sceneColor.rgb + _OutlineColor.rgb * outlineAmount;
                }
                else
                {
                    finalColor = lerp(sceneColor.rgb, _OutlineColor.rgb, outlineAmount);
                }

                return half4(finalColor, sceneColor.a);
            }
            ENDHLSL
        }
    }
}

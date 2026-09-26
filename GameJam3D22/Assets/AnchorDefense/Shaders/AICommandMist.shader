Shader "AnchorDefense/AICommandMist"
{
    Properties
    {
        _TintColor ("Tint", Color) = (0.18, 0.8, 1, 1)
        _Opacity ("Opacity", Range(0, 1)) = 0.32
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent-10" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "CommandMist"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
                half4 _TintColor;
                half _Opacity;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half2 centered = input.uv * 2 - 1;
                half radius = dot(centered, centered);
                half soft = saturate(1 - radius);
                soft *= soft;
                return half4(_TintColor.rgb, soft * _Opacity * _TintColor.a);
            }
            ENDHLSL
        }
    }
}

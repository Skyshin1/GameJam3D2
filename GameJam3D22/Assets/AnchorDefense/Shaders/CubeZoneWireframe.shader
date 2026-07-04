Shader "AnchorDefense/Cube Zone Wireframe"
{
    Properties
    {
        [HDR] _BaseColor ("Edge Color", Color) = (0.2, 0.8, 1, 0.18)
        _Color ("Legacy Color", Color) = (0.2, 0.8, 1, 0.18)
        _EdgeWidth ("Edge Width", Range(0.005, 0.15)) = 0.035
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+20"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Cube Zone Edges"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _Color;
                float _EdgeWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionOS = input.positionOS.xyz;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // Cube primitives occupy -0.5..0.5 in object space. A surface pixel is
                // part of an edge when at least two axes are close to that boundary.
                float3 p = abs(input.positionOS);
                float secondLargest = max(min(p.x, p.y), max(min(p.x, p.z), min(p.y, p.z)));
                float threshold = 0.5 - _EdgeWidth;
                float antialiasing = max(fwidth(secondLargest), 0.0001);
                float edge = smoothstep(threshold - antialiasing, threshold + antialiasing, secondLargest);
                clip(edge - 0.001);
                half4 color = _BaseColor;
                color.a *= edge;
                return color;
            }
            ENDHLSL
        }
    }
}

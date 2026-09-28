Shader "AnchorDefense/AICommandSignal"
{
    Properties
    {
        _TintColor ("Tint", Color) = (0.25, 0.85, 1, 1)
        _Opacity ("Opacity", Range(0, 1)) = 0.3
        _Style ("Style: Burst / Hit / Heal / Slow / Boost / Star / Scan / Arc", Float) = 0
        _Progress ("Animation Progress", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent-5" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "CommandSignal"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            CBUFFER_START(UnityPerMaterial)
                half4 _TintColor;
                half _Opacity;
                half _Style;
                float _Progress;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }
            float Stroke(float distance, float width)
            {
                float aa = max(fwidth(distance), 0.003);
                return 1 - smoothstep(width - aa, width + aa, abs(distance));
            }
            float Star(float2 p)
            {
                float2 a = abs(p);
                // Frac-based burst cells have discontinuities; cap AA to avoid seam streaks.
                float aa = clamp(max(fwidth(p.x), fwidth(p.y)), 0.003, 0.045);
                float vertical = (1 - smoothstep(0.018, 0.038 + aa, a.x)) * pow(saturate(1 - a.y), 2.5);
                float horizontal = (1 - smoothstep(0.018, 0.038 + aa, a.y)) * pow(saturate(1 - a.x), 2.5);
                float core = exp(-length(p) * 25);
                return max(vertical, horizontal) * 0.8 + core + exp(-length(p) * 10) * 0.09;
            }
            float Hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float3 Prism(float phase)
            {
                float t = frac(phase) * 6;
                if (t < 1) return lerp(float3(0.28, 0.62, 1), float3(0.66, 0.36, 1), t);
                if (t < 2) return lerp(float3(0.66, 0.36, 1), float3(1, 0.36, 0.68), t - 1);
                if (t < 3) return lerp(float3(1, 0.36, 0.68), float3(1, 0.75, 0.3), t - 2);
                if (t < 4) return lerp(float3(1, 0.75, 0.3), float3(0.25, 1, 0.72), t - 3);
                if (t < 5) return lerp(float3(0.25, 1, 0.72), float3(0.25, 0.88, 1), t - 4);
                return lerp(float3(0.25, 0.88, 1), float3(0.28, 0.62, 1), t - 5);
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.uv * 2 - 1;
                float r = length(p);
                float signal = 0;
                half4 tint = _TintColor;
                if (_Style < 0.5)
                {
                    // A brief shower of tiny stars, without a visible plane or grid.
                    float2 uv = input.uv * 7;
                    float2 cell = floor(uv);
                    float seed = Hash(cell);
                    float2 center = 0.13 + float2(seed, Hash(cell + 17)) * 0.74;
                    float2 q = (frac(uv) - center) / (0.10 + seed * 0.13);
                    float arrival = seed * 0.4;
                    float blink = sin(saturate((_Progress - arrival) / 0.6) * 3.141593);
                    signal = Star(q) * blink * step(0.38, seed);
                    tint.rgb = Prism(seed + _Progress * 0.08);
                }
                else if (_Style < 1.5)
                {
                    signal = Star(p / lerp(0.7, 0.22, saturate(_Progress)));
                    for (int i = 0; i < 4; i++)
                    {
                        float angle = i * 1.570796 + 0.45;
                        float2 q = p - float2(cos(angle), sin(angle)) * lerp(0.12, 0.65, saturate(_Progress));
                        signal = max(signal, Star(q / 0.18) * 0.7);
                    }
                }
                else if (_Style < 2.5)
                {
                    // Three gentle healing sparkles orbit the body.
                    for (int i = 0; i < 3; i++)
                    {
                        float angle = _Progress * 1.5 + i * 2.094395;
                        float2 q = p - float2(cos(angle), sin(angle)) * 0.64;
                        signal = max(signal, Star(q / 0.19) * (0.6 + 0.4 * sin(_Progress * 4 + i * 2.1)));
                    }
                }
                else if (_Style < 3.5)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        float2 center = float2(i < 2 ? -0.65 : 0.65, (i % 2 == 0 ? -0.2 : 0.3) + sin(_Progress * 0.8 + i) * 0.05);
                        signal = max(signal, Star((p - center) / 0.14) * (0.55 + 0.45 * sin(_Progress * 2 + i * 1.6)));
                    }
                }
                else if (_Style < 4.5)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        float angle = i * 1.256637 - _Progress;
                        float2 q = p - float2(cos(angle), sin(angle)) * 0.64;
                        signal = max(signal, Star(q / 0.15) * (0.65 + 0.35 * sin(_Progress * 3 + i)));
                    }
                }
                else if (_Style < 5.5)
                {
                    signal = Star(p);
                    tint *= input.color;
                }
                else if (_Style < 6.5)
                {
                    // The original downward lattice sweep, now like a prismatic light sheet.
                    float2 grid = abs(frac(input.uv * 9) - 0.5);
                    float lattice = max(Stroke(grid.x - 0.5, 0.008), Stroke(grid.y - 0.5, 0.008));
                    float edgeFade = saturate((1 - max(abs(p.x), abs(p.y))) * 10);
                    float border = Stroke(max(abs(p.x), abs(p.y)) - 0.975, 0.01);
                    float orbit = Stroke(r - 0.67, 0.008) * (0.4 + 0.6 * pow(0.5 + 0.5 * sin(atan2(p.y, p.x) * 3 + _Progress * 5), 3));
                    signal = lattice * edgeFade * 0.17 + border * 0.9 + orbit * 0.28;
                    tint.rgb = Prism(input.uv.x * 0.32 + input.uv.y * 0.5 + _Progress * 0.12);
                }
                else
                {
                    signal = exp(-abs(p.y) * 3) * (0.28 + 0.72 * pow(0.5 + 0.5 * sin(input.uv.x * 25 - _Progress * 3), 6));
                    tint *= input.color;
                }
                half3 color = lerp(tint.rgb, half3(0.9, 0.96, 1), saturate(signal) * 0.22);
                return half4(color, saturate(signal) * _Opacity * tint.a);
            }
            ENDHLSL
        }
    }
}

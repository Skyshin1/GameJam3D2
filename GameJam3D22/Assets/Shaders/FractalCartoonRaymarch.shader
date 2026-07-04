Shader "Custom/URP/FractalCartoonRaymarch"
{
    Properties
    {
        [Header(Main Look)]
        _Brightness ("Brightness", Range(0, 3)) = 1.2
        _Gamma ("Gamma", Range(0.1, 3)) = 1.4
        _Saturation ("Saturation", Range(0, 2)) = 0.65

        [Header(Raymarch)]
        _Detail ("Detail", Range(0.0001, 0.01)) = 0.001
        _MaxDistance ("Max Distance", Range(5, 60)) = 25
        _Fov ("FOV", Range(0.2, 2)) = 0.9

        [Header(Animation)]
        _AnimationSpeed ("Animation Speed", Range(0, 3)) = 1
        _PathSpeed ("Path Speed", Range(0, 3)) = 1
        _Origin ("Origin", Vector) = (-1, 0.7, 0, 0)

        [Header(Effect)]
        [Toggle] _Waves ("Enable Waves", Float) = 1
        [Toggle] _ShowOnlyEdges ("Show Only Edges", Float) = 0
        _EdgeStrength ("Edge Strength", Range(0, 40)) = 15
        _EdgePower ("Edge Power", Range(0.1, 2)) = 0.55
        _EdgeDarkness ("Edge Darkness", Range(0, 2)) = 0.8

        [Header(Background)]
        _SunSize ("Sun Size", Range(1, 12)) = 7
        _BorderStrength ("Border Strength", Range(0, 1)) = 1

        [Header(Camera Rotate)]
        _MouseRotateX ("Rotate X", Range(-3.14, 3.14)) = 0
        _MouseRotateY ("Rotate Y", Range(-3.14, 3.14)) = -0.05
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Overlay"
        }

        Pass
        {
            Name "Fractal Cartoon Raymarch"

            Cull Off
            ZWrite Off
            ZTest Always
            Blend Off

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define RAY_STEPS 150
            #define PI 3.14159265359

            CBUFFER_START(UnityPerMaterial)
                float _Brightness;
                float _Gamma;
                float _Saturation;

                float _Detail;
                float _MaxDistance;
                float _Fov;

                float _AnimationSpeed;
                float _PathSpeed;
                float4 _Origin;

                float _Waves;
                float _ShowOnlyEdges;
                float _EdgeStrength;
                float _EdgePower;
                float _EdgeDarkness;

                float _SunSize;
                float _BorderStrength;

                float _MouseRotateX;
                float _MouseRotateY;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float Mod1(float x, float y)
            {
                return x - y * floor(x / y);
            }

            float2 Rotate2D(float2 p, float a)
            {
                float s = sin(a);
                float c = cos(a);
                return float2(c * p.x - s * p.y, s * p.x + c * p.y);
            }

            float4 Formula(float4 p)
            {
                p.xz = abs(p.xz + 1.0) - abs(p.xz - 1.0) - p.xz;

                p.y -= 0.25;
                p.xy = Rotate2D(p.xy, 35.0 * PI / 180.0);

                p = p * 2.0 / clamp(dot(p.xyz, p.xyz), 0.2, 1.0);

                return p;
            }

            float DE(float3 pos, float timeValue)
            {
                if (_Waves > 0.5)
                {
                    pos.y += sin(pos.z - timeValue * 6.0) * 0.15;
                }

                float3 tpos = pos;
                tpos.z = abs(3.0 - Mod1(tpos.z, 6.0));

                float4 p = float4(tpos, 1.0);

                for (int i = 0; i < 4; i++)
                {
                    p = Formula(p);
                }

                float fr = (length(max(float2(0.0, 0.0), p.yz - 1.5)) - 1.0) / p.w;

                float ro = max(abs(pos.x + 1.0) - 0.3, pos.y - 0.35);
                ro = max(ro, -max(abs(pos.x + 1.0) - 0.1, pos.y - 0.5));

                pos.z = abs(0.25 - Mod1(pos.z, 0.5));

                ro = max(ro, -max(abs(pos.z) - 0.2, pos.y - 0.3));
                ro = max(ro, -max(abs(pos.z) - 0.01, -pos.y + 0.32));

                float d = min(fr, ro);
                return d;
            }

            float3 Path(float ti)
            {
                ti *= 1.5 * _PathSpeed;

                float3 p = float3
                (
                    sin(ti),
                    (1.0 - sin(ti * 2.0)) * 0.5,
                    -ti * 5.0
                ) * 0.5;

                return p;
            }

            float3 GetNormal(float3 p, float det, float timeValue, out float edge)
            {
                float3 e = float3(0.0, det * 5.0, 0.0);

                float d1 = DE(p - e.yxx, timeValue);
                float d2 = DE(p + e.yxx, timeValue);

                float d3 = DE(p - e.xyx, timeValue);
                float d4 = DE(p + e.xyx, timeValue);

                float d5 = DE(p - e.xxy, timeValue);
                float d6 = DE(p + e.xxy, timeValue);

                float d = DE(p, timeValue);

                edge =
                    abs(d - 0.5 * (d2 + d1)) +
                    abs(d - 0.5 * (d4 + d3)) +
                    abs(d - 0.5 * (d6 + d5));

                edge = min(1.0, pow(edge, _EdgePower) * _EdgeStrength);

                return normalize(float3(d1 - d2, d3 - d4, d5 - d6));
            }

            float3 MoveCamera(inout float3 dir, float timeValue)
            {
                float3 go = Path(timeValue);
                float3 adv = Path(timeValue + 0.7);

                float3 advec = normalize(adv - go);

                float an = adv.x - go.x;
                an *= min(1.0, abs(adv.z - go.z)) * sign(adv.z - go.z) * 0.7;
                dir.xy = Rotate2D(dir.xy, an);

                an = advec.y * 1.7;
                dir.yz = Rotate2D(dir.yz, an);

                an = atan2(advec.x, advec.z);
                dir.xz = Rotate2D(dir.xz, an);

                return go;
            }

            float3 Raymarch(float3 rayOrigin, float3 rayDir, float timeValue)
            {
                float3 p = rayOrigin;

                float d = 100.0;
                float det = 0.0;
                float totalDist = 0.0;

                [loop]
                for (int i = 0; i < RAY_STEPS; i++)
                {
                    if (d > det && totalDist < _MaxDistance)
                    {
                        p = rayOrigin + totalDist * rayDir;
                        d = DE(p, timeValue);

                        det = _Detail * exp(0.13 * totalDist);
                        totalDist += d;
                    }
                }

                p -= (det - d) * rayDir;

                float edge = 0.0;
                float3 norm = GetNormal(p, det, timeValue, edge);

                float3 col;

                if (_ShowOnlyEdges > 0.5)
                {
                    col = 1.0 - float3(edge, edge, edge);
                }
                else
                {
                    col = (1.0 - abs(norm)) * max(0.0, 1.0 - edge * _EdgeDarkness);
                }

                totalDist = clamp(totalDist, 0.0, _MaxDistance + 1.0);

                float3 bgDir = rayDir;
                bgDir.y -= 0.02;

                float sunSize = _SunSize;

                float an = atan2(bgDir.x, bgDir.y) + timeValue * 1.5;

                float s = pow(saturate(1.0 - length(bgDir.xy) * sunSize - abs(0.2 - Mod1(an, 0.4))), 0.1);
                float sb = pow(saturate(1.0 - length(bgDir.xy) * (sunSize - 0.2) - abs(0.2 - Mod1(an, 0.4))), 0.1);
                float sg = pow(saturate(1.0 - length(bgDir.xy) * (sunSize - 4.5) - 0.5 * abs(0.2 - Mod1(an, 0.4))), 3.0);

                float y = lerp
                (
                    0.45,
                    1.2,
                    pow(smoothstep(0.0, 1.0, 0.75 - bgDir.y), 2.0)
                ) * (1.0 - sb * 0.5);

                float skyTerm = (1.0 - s) * (1.0 - sg) * y;
                float3 sunRayTerm = (1.0 - sb) * sg * float3(1.0, 0.8, 0.15) * 3.0;

                float3 backg = float3(0.5, 0.0, 1.0) * (float3(skyTerm, skyTerm, skyTerm) + sunRayTerm);
                backg += float3(1.0, 0.9, 0.1) * s;
                backg = max(backg, sg * float3(1.0, 0.9, 0.5));

                col = lerp(float3(1.0, 0.9, 0.3), col, exp(-0.004 * totalDist * totalDist));

                if (totalDist >= _MaxDistance)
                {
                    col = backg;
                }

                col = pow(max(col, float3(0.0, 0.0, 0.0)), float3(_Gamma, _Gamma, _Gamma)) * _Brightness;

                float gray = length(col);
                col = lerp(float3(gray, gray, gray), col, _Saturation);

                if (_ShowOnlyEdges <= 0.5)
                {
                    col *= float3(1.0, 0.9, 0.85);
                }

                return col;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv * 2.0 - 1.0;
                float2 oriUV = uv;

                uv.y *= _ScreenParams.y / _ScreenParams.x;

                float timeValue = _Time.y * 0.5 * _AnimationSpeed;

                float3 dir = normalize(float3(uv * _Fov, 1.0));

                dir.yz = Rotate2D(dir.yz, _MouseRotateY);
                dir.xz = Rotate2D(dir.xz, _MouseRotateX);

                float3 rayOrigin = _Origin.xyz + MoveCamera(dir, timeValue);

                float3 color = Raymarch(rayOrigin, dir, timeValue);

                float borderMask = pow
                (
                    max
                    (
                        0.0,
                        0.95 - length(oriUV * oriUV * oriUV * float2(1.05, 1.1))
                    ),
                    0.3
                );

                color = lerp(color, color * borderMask, _BorderStrength);

                return half4(color, 1.0);
            }

            ENDHLSL
        }
    }
}
Shader "DeadWalls/SpriteSheet"
{
    Properties
    {
        _MainTex ("Sprite Sheet", 2D) = "white" {}
        _WalkTex ("Worker Walk Sheet", 2D) = "white" {}
        _WorkTex ("Worker Work Sheet", 2D) = "white" {}
        _CelebrateTex ("Worker Delivery Sheet", 2D) = "white" {}
        _UVRect ("UV Rect", Vector) = (0, 0, 1, 1)
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _WorkerAnimation ("Worker Animation", Float) = 0
        _WorkerFeedback ("Worker Feedback", Vector) = (0, 0, 0, 0)
        _WorkerCargoColor ("Worker Cargo Color", Color) = (0.72, 0.43, 0.20, 1)
        _HordeReadability ("Horde Readability (Edge, Pixels, Ground, Reserved)", Vector) = (0, 0, 0, 0)
        _HordeEdgeColor ("Horde Edge Color", Color) = (0.18, 0.26, 0.36, 1)
        _HordeGroundColor ("Horde Ground Color", Color) = (0.03, 0.045, 0.065, 1)
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Cull Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 quadUV : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_WalkTex);
            SAMPLER(sampler_WalkTex);
            TEXTURE2D(_WorkTex);
            SAMPLER(sampler_WorkTex);
            TEXTURE2D(_CelebrateTex);
            SAMPLER(sampler_CelebrateTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _UVRect;
                float4 _Color;
                float _WorkerAnimation;
                float4 _WorkerFeedback;
                float4 _WorkerCargoColor;
                float4 _HordeReadability;
                float4 _HordeEdgeColor;
                float4 _HordeGroundColor;
                float _Cutoff;
            CBUFFER_END

            #ifdef DOTS_INSTANCING_ON
                UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
                    UNITY_DOTS_INSTANCED_PROP(float4, _UVRect)
                    UNITY_DOTS_INSTANCED_PROP(float4, _Color)
                    UNITY_DOTS_INSTANCED_PROP(float, _WorkerAnimation)
                    UNITY_DOTS_INSTANCED_PROP(float4, _WorkerFeedback)
                    UNITY_DOTS_INSTANCED_PROP(float4, _WorkerCargoColor)
                UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

                #define _UVRect UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _UVRect)
                #define _Color  UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _Color)
                #define _WorkerAnimation UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _WorkerAnimation)
                #define _WorkerFeedback UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _WorkerFeedback)
                #define _WorkerCargoColor UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _WorkerCargoColor)
            #endif

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv * _UVRect.zw + _UVRect.xy;
                OUT.quadUV = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                half4 col;
                if (_WorkerAnimation < 0.5)
                    col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                else if (_WorkerAnimation < 1.5)
                    col = SAMPLE_TEXTURE2D(_WalkTex, sampler_WalkTex, IN.uv);
                else if (_WorkerAnimation < 2.5)
                    col = SAMPLE_TEXTURE2D(_WorkTex, sampler_WorkTex, IN.uv);
                else
                    col = SAMPLE_TEXTURE2D(_CelebrateTex, sampler_CelebrateTex, IN.uv);

                col *= _Color;

                // Vampire material alone enables this uniform branch. It keeps the 10K
                // horde readable without a second pass, extra entity or material instance.
                float edgeMask = 0.0;
                float groundMask = 0.0;
                if (_HordeReadability.x > 0.001)
                {
                    float sourceVisible = step(_Cutoff, col.a);
                    float2 atlasTexel = (_UVRect.zw / 128.0)
                        * max(0.5, _HordeReadability.y);
                    float2 sampleMin = _UVRect.xy + atlasTexel * 0.5;
                    float2 sampleMax = _UVRect.xy + _UVRect.zw - atlasTexel * 0.5;

                    float neighborAlpha = 0.0;
                    neighborAlpha = max(neighborAlpha, SAMPLE_TEXTURE2D(
                        _MainTex, sampler_MainTex,
                        clamp(IN.uv + float2(atlasTexel.x, 0.0), sampleMin, sampleMax)).a);
                    neighborAlpha = max(neighborAlpha, SAMPLE_TEXTURE2D(
                        _MainTex, sampler_MainTex,
                        clamp(IN.uv - float2(atlasTexel.x, 0.0), sampleMin, sampleMax)).a);
                    neighborAlpha = max(neighborAlpha, SAMPLE_TEXTURE2D(
                        _MainTex, sampler_MainTex,
                        clamp(IN.uv + float2(0.0, atlasTexel.y), sampleMin, sampleMax)).a);
                    neighborAlpha = max(neighborAlpha, SAMPLE_TEXTURE2D(
                        _MainTex, sampler_MainTex,
                        clamp(IN.uv - float2(0.0, atlasTexel.y), sampleMin, sampleMax)).a);
                    edgeMask = step(_Cutoff, neighborAlpha * _Color.a)
                        * (1.0 - sourceVisible)
                        * saturate(_HordeReadability.x);

                    float2 groundUv = (IN.quadUV - float2(0.50, 0.085))
                        / float2(0.075, 0.025);
                    float contactPatch = 1.0 - step(1.0, dot(groundUv, groundUv));
                    groundMask = contactPatch
                        * (1.0 - sourceVisible)
                        * saturate(_HordeReadability.z);
                }

                float productionStrength = saturate(_WorkerFeedback.w);
                float cargoScale = lerp(0.86, 1.12, productionStrength);
                float2 cargoUv = (IN.quadUV - float2(0.66, 0.36))
                    / (float2(0.040, 0.034) * cargoScale);
                float cargoOuter = 1.0 - smoothstep(0.72, 0.90,
                    max(abs(cargoUv.x), abs(cargoUv.y)));
                float cargoInner = 1.0 - smoothstep(0.54, 0.72,
                    max(abs(cargoUv.x), abs(cargoUv.y)));
                float cargoKnot = 1.0 - smoothstep(0.13, 0.25,
                    length(cargoUv - float2(0.0, 0.88)));
                float cargoMask = saturate(_WorkerFeedback.x) * max(cargoOuter, cargoKnot);
                float cargoFillMask = saturate(_WorkerFeedback.x) * max(cargoInner, cargoKnot);

                float2 lanternUv = (IN.quadUV - float2(0.34, 0.42)) * float2(1.0, 1.35);
                float lanternDistance = length(lanternUv);
                float lanternGlow = 1.0 - smoothstep(0.012, 0.038, lanternDistance);
                float lanternCore = 1.0 - smoothstep(0.007, 0.016, lanternDistance);
                float lanternMask = saturate(_WorkerFeedback.y) * lanternGlow;

                float pulse = saturate(_WorkerFeedback.z);
                float pulseRadius = lerp(0.21, 0.10, pulse);
                float pulseDistance = length((IN.quadUV - float2(0.50, 0.18)) * float2(1.0, 1.55));
                float pulseRing = 1.0 - smoothstep(0.010, 0.030, abs(pulseDistance - pulseRadius));
                float pulseMask = pulseRing * pulse * productionStrength;

                col.rgb = lerp(col.rgb, _WorkerCargoColor.rgb * 0.42, cargoMask);
                col.rgb = lerp(col.rgb, _WorkerCargoColor.rgb, cargoFillMask);
                col.rgb = lerp(col.rgb, float3(1.0, 0.54, 0.12), lanternMask * 0.72);
                col.rgb = lerp(col.rgb, float3(1.0, 0.88, 0.38), lanternCore * _WorkerFeedback.y);
                col.rgb = lerp(col.rgb, _WorkerCargoColor.rgb * 1.35, pulseMask);
                col.a = max(col.a, max(cargoMask, max(lanternMask, pulseMask)));
                col.rgb = lerp(col.rgb, _HordeGroundColor.rgb,
                    step(edgeMask, groundMask) * step(0.001, groundMask));
                col.rgb = lerp(col.rgb, _HordeEdgeColor.rgb, step(0.001, edgeMask));
                col.a = max(col.a, max(edgeMask, groundMask));
                clip(col.a - _Cutoff);
                return col;
            }
            ENDHLSL
        }
    }
}

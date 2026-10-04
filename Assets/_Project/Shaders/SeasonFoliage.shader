Shader "AntColony/SeasonFoliage"
{
    // 계절 나무(2026-10-04): 잎(초록 계열 픽셀)만 계절색으로 바꾸고 줄기는 그대로 둔다. 겨울은 윗면에 눈.
    // 계절 값은 SeasonVisuals가 전역(_SeasonSpring/_SeasonAutumn/_SeasonWinter)으로 준다.
    Properties
    {
        _BaseMap("Base map", 2D) = "white" {}
        _BaseColor("Base color", Color) = (1, 1, 1, 1)
        _Evergreen("Evergreen (no autumn color)", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _Evergreen;
        CBUFFER_END
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            half _SeasonSpring, _SeasonAutumn, _SeasonWinter;

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionHCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; float2 uv : TEXCOORD2; float fog : TEXCOORD3; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(OUT.positionWS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.fog = ComputeFogFactor(OUT.positionHCS.z);
                return OUT;
            }

            float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }

            half4 frag(Varyings IN) : SV_Target
            {
                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).rgb * _BaseColor.rgb;
                // 잎 판정: 초록이 빨강·파랑보다 뚜렷하게 큰 픽셀.
                half green = albedo.g - max(albedo.r, albedo.b);
                half leaf = smoothstep(0.02, 0.1, green);
                half lum = dot(albedo, half3(0.3, 0.59, 0.11));
                // 나무마다 조금씩 다른 가을색(주황~빨강). 오브젝트 위치로 고른다.
                float pick = hash(floor(GetObjectToWorldMatrix()._m03_m23));
                half3 autumn = lum * lerp(half3(2.6, 1.25, 0.25), half3(2.5, 0.6, 0.25), pick);
                half3 spring = albedo * half3(1.05, 1.12, 0.85);
                half3 winter = lum * half3(1.0, 0.92, 0.8); // 마른 잎 갈색
                half3 leafColor = albedo;
                leafColor = lerp(leafColor, spring, _SeasonSpring);
                half autumnAmount = _SeasonAutumn * (1 - _Evergreen);
                leafColor = lerp(leafColor, autumn, autumnAmount);
                leafColor = lerp(leafColor, _Evergreen > 0.5 ? albedo * 0.85 : winter, _SeasonWinter);
                albedo = lerp(albedo, leafColor, leaf);
                // 겨울 눈: 위를 향한 면.
                half snow = smoothstep(0.35, 0.75, normalize(IN.normalWS).y) * _SeasonWinter;
                albedo = lerp(albedo, half3(0.93, 0.96, 1.0), snow);

                float3 normalWS = normalize(IN.normalWS);
                Light light = GetMainLight(TransformWorldToShadowCoord(IN.positionWS));
                half ndl = saturate(dot(normalWS, light.direction)) * 0.75 + 0.25; // 로우폴리라 반대편도 너무 어둡지 않게
                half3 color = albedo * (light.color * ndl * light.shadowAttenuation + SampleSH(normalWS));
                color = MixFog(color, IN.fog);
                return half4(color, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection;
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            float4 vert(Attributes IN) : SV_POSITION
            {
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return positionCS;
            }
            half4 frag() : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ColorMask R
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            float4 vert(float4 positionOS : POSITION) : SV_POSITION { return TransformObjectToHClip(positionOS.xyz); }
            half frag() : SV_Target { return 0; }
            ENDHLSL
        }
    }
}

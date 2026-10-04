Shader "AntColony/TerrainBlend"
{
    Properties
    {
        _textureScale("Texture scale", float) = 1
        _Tint("Biome tint", Color) = (1, 1, 1, 1)
        _Detail("Pattern contrast", Range(0, 1)) = 0.55
        _BlendWidth("Layer blend width", Range(0, 0.2)) = 0.06
        _Patches("Other ground patches", Range(0, 1)) = 0.9
        _SnowTex("Winter snow ground", 2D) = "white" {}
        _LeafTex("Autumn leaf ground (recolored)", 2D) = "gray" {}
        _Snow("Snow cover", Range(0, 1)) = 0
        _Leaves("Leaf cover", Range(0, 1)) = 0
        [HideInInspector] terrainTextures("Terrain textures", 2DArray) = "" {}
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define MAX_TEXTURES 32

            float _textureScale;
            half4 _Tint;
            half _Detail;
            float _BlendWidth;
            half _Patches;
            half _Snow, _Leaves;
            TEXTURE2D(_SnowTex); SAMPLER(sampler_SnowTex);
            TEXTURE2D(_LeafTex); SAMPLER(sampler_LeafTex);
            float minTerrainHeight;
            float maxTerrainHeight;
            float terrainHeights[MAX_TEXTURES];
            int numTextures;

            TEXTURE2D_ARRAY(terrainTextures);
            SAMPLER(sampler_terrainTextures);

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                return OUT;
            }

            // 2026-10-04 반복 무늬 완화: 값 노이즈로 두 번 샘플을 섞고, 무늬 대비를 낮추고, 높이 경계를 부드럽게 섞는다.
            float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3 - 2 * f);
                return lerp(lerp(hash(i), hash(i + float2(1, 0)), f.x), lerp(hash(i + float2(0, 1)), hash(i + 1), f.x), f.y);
            }

            // 세 번째 샘플(73도 회전·1.31배)을 다른 노이즈로 섞어 두 샘플만으로 남던 반복 무늬를 더 깬다.
            static float2 uv3; static float mix3;
            half3 Layer(float2 uv, float2 uv2, float mixAmount, int index)
            {
                half3 a = SAMPLE_TEXTURE2D_ARRAY(terrainTextures, sampler_terrainTextures, uv, index).rgb;
                half3 b = SAMPLE_TEXTURE2D_ARRAY(terrainTextures, sampler_terrainTextures, uv2, index).rgb;
                half3 c = SAMPLE_TEXTURE2D_ARRAY(terrainTextures, sampler_terrainTextures, uv3, index).rgb;
                half3 average = SAMPLE_TEXTURE2D_ARRAY_LOD(terrainTextures, sampler_terrainTextures, uv, index, 12).rgb;
                return lerp(average, lerp(lerp(a, b, mixAmount), c, mix3), _Detail);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                if (numTextures <= 0) return half4(0.5, 0.5, 0.5, 1);
                float2 world = IN.worldPos.xz;
                float2 uv = world / max(abs(_textureScale), 0.0001);
                // 두 번째 샘플: 37도 회전 + 0.77배 축척 + 이동. 노이즈 마스크로 섞어 같은 타일이 줄지어 보이지 않게 한다.
                float2 uv2 = mul(float2x2(0.799, -0.602, 0.602, 0.799), uv) * 0.77 + float2(0.37, 0.61);
                float mixAmount = smoothstep(0.3, 0.7, noise(world * 0.09));
                uv3 = mul(float2x2(0.292, -0.956, 0.956, 0.292), uv) * 1.31 + float2(0.71, 0.13);
                mix3 = smoothstep(0.35, 0.65, noise(world * 0.061 + 3.7)) * 0.7;
                float macro = noise(world * 0.018) * 0.55 + noise(world * 0.07) * 0.3 + noise(world * 0.23) * 0.15;

                int count = min(numTextures, MAX_TEXTURES);
                float heightValue = saturate((IN.worldPos.y - minTerrainHeight) / (maxTerrainHeight - minTerrainHeight));
                heightValue += (noise(world * 0.15) - 0.5) * 0.06; // 경계선을 울퉁불퉁하게

                int layerIndex = 0;
                for (int i = 1; i < count; i++)
                {
                    if (heightValue >= terrainHeights[i]) layerIndex = i;
                }
                half3 color = Layer(uv, uv2, mixAmount, layerIndex);
                int next = min(layerIndex + 1, count - 1);
                if (next != layerIndex)
                {
                    float edge = smoothstep(terrainHeights[next] - _BlendWidth, terrainHeights[next], heightValue);
                    if (edge > 0) color = lerp(color, Layer(uv, uv2, mixAmount, next), edge);
                }
                // 높이와 무관한 큰 얼룩: 같은 높이(평지)에도 위 1·2단 지면을 군데군데 섞어 한 텍스처만 깔리지 않게 한다.
                float patchA = smoothstep(0.52, 0.66, noise(world * 0.035 + 17.3));
                float patchB = smoothstep(0.6, 0.74, noise(world * 0.05 + 41.7));
                // 맨 아래(물가)·맨 위(봉우리: 눈·수정 등)는 성격이 달라 중간 지면층끼리만 섞는다.
                int lo = 1, hi = max(1, count - 2);
                if (layerIndex >= lo && layerIndex <= hi)
                {
                    int a = clamp(layerIndex + 1 <= hi ? layerIndex + 1 : layerIndex - 1, lo, hi);
                    int b = clamp(layerIndex - 1 >= lo ? layerIndex - 1 : layerIndex + 2, lo, hi);
                    if (patchA > 0 && a != layerIndex) color = lerp(color, Layer(uv, uv2, mixAmount, a), patchA * _Patches);
                    if (patchB > 0 && b != layerIndex && b != a) color = lerp(color, Layer(uv, uv2, mixAmount, b), patchB * _Patches);
                }
                color *= lerp(0.8, 1.14, macro);

                // 계절 바닥(2026-10-04): 가을 낙엽 얼룩(숲바닥 무늬를 주황·갈색으로), 겨울 눈. 0~1 덮임 비율은 SeasonVisuals가 준다.
                // 노이즈 문턱을 덮임에 따라 내려 계절이 깊어질수록 얼룩이 번지듯 넓어진다.
                // 물가·물속(맨 아래 층)은 덮지 않는다.
                float land = layerIndex > 0 ? 1 : 0;
                if (_Leaves > 0 && land > 0)
                {
                    half3 leafTex = lerp(SAMPLE_TEXTURE2D(_LeafTex, sampler_LeafTex, uv).rgb, SAMPLE_TEXTURE2D(_LeafTex, sampler_LeafTex, uv2).rgb, mixAmount);
                    half3 leafAvg = SAMPLE_TEXTURE2D_LOD(_LeafTex, sampler_LeafTex, uv, 12).rgb;
                    half3 leaf = dot(lerp(leafAvg, leafTex, _Detail), half3(0.3, 0.59, 0.11)) * half3(3.2, 1.55, 0.5);
                    float cover = smoothstep(1.05 - _Leaves * 0.6, 1.2 - _Leaves * 0.6, noise(world * 0.07 + 9.1) * 0.8 + 0.2);
                    color = lerp(color, leaf * lerp(0.86, 1.1, macro), cover);
                }
                // 바이옴·계절 색조는 풀(초록 픽셀)에만 온전히, 돌·흙엔 40%만 입혀 돌이 초록으로 물들지 않게 한다.
                half grass = saturate((color.g - max(color.r, color.b)) * 8);
                color *= lerp(lerp(1, _Tint.rgb, 0.4), _Tint.rgb, grass);
                if (_Snow > 0 && land > 0)
                {
                    half3 snowTex = lerp(SAMPLE_TEXTURE2D(_SnowTex, sampler_SnowTex, uv).rgb, SAMPLE_TEXTURE2D(_SnowTex, sampler_SnowTex, uv2).rgb, mixAmount);
                    // 눈 텍스처의 흙·마른 풀 얼룩은 밝기만 써서 흰~옅은 푸른 눈으로 바꾼다.
                    half snowLum = dot(snowTex, half3(0.3, 0.59, 0.11));
                    half3 snow = lerp(half3(0.74, 0.8, 0.9), half3(0.97, 0.98, 1.0), smoothstep(0.25, 0.8, snowLum));
                    // 한겨울(_Snow=1)엔 땅 전체, 늦가을·초봄엔 얼룩처럼 번진다.
                    float cover = smoothstep(1.05 - _Snow, 1.2 - _Snow, noise(world * 0.06 + 5.1) * 0.8 + 0.2);
                    color = lerp(color, snow * lerp(0.93, 1.03, macro), cover); // 겨울 회갈색 색조를 눈에는 입히지 않는다
                }
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}

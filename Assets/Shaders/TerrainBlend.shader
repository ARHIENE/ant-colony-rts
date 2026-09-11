Shader "AntColony/TerrainBlend"
{
    Properties
    {
        _textureScale("Texture scale", float) = 1
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

            half4 frag(Varyings IN) : SV_Target
            {
                if (numTextures <= 0) return half4(0.5, 0.5, 0.5, 1);
                float3 scaledWorldPos = IN.worldPos / max(abs(_textureScale), 0.0001);
                float worldPosY = IN.worldPos.y;

                float heightValue = saturate((worldPosY - minTerrainHeight) / (maxTerrainHeight - minTerrainHeight));

                int layerIndex = 0;
                for (int i = 1; i < min(numTextures, MAX_TEXTURES); i++)
                {
                    if (heightValue >= terrainHeights[i]) layerIndex = i;
                }

                return SAMPLE_TEXTURE2D_ARRAY(terrainTextures, sampler_terrainTextures, scaledWorldPos.xz, layerIndex);
            }
            ENDHLSL
        }
    }
}

Shader "AntColony/Organic Horde Motion"
{
    Properties
    {
        _Positions("Positions", 2D) = "black" {}
        _Normals("Normals", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma instancing_options forcemaxcount:250
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_Positions); SAMPLER(sampler_Positions);
            TEXTURE2D(_Normals); SAMPLER(sampler_Normals);
            float4 _Positions_TexelSize;
            UNITY_INSTANCING_BUFFER_START(Motion)
                UNITY_DEFINE_INSTANCED_PROP(float4, _MotionFrames)
            UNITY_INSTANCING_BUFFER_END(Motion)
            struct Attributes { float4 positionOS:POSITION; float4 color:COLOR; float2 motionUV:TEXCOORD1; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; half3 normalWS:TEXCOORD1; half4 color:COLOR; half fog:TEXCOORD2; };
            Varyings Vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float4 frames = UNITY_ACCESS_INSTANCED_PROP(Motion, _MotionFrames);
                float2 a = float2(input.motionUV.x, (frames.x + .5) * _Positions_TexelSize.y);
                float2 b = float2(input.motionUV.x, (frames.y + .5) * _Positions_TexelSize.y);
                float3 p = lerp(SAMPLE_TEXTURE2D_LOD(_Positions,sampler_Positions,a,0).xyz, SAMPLE_TEXTURE2D_LOD(_Positions,sampler_Positions,b,0).xyz, frames.z);
                float3 n = normalize(lerp(SAMPLE_TEXTURE2D_LOD(_Normals,sampler_Normals,a,0).xyz,SAMPLE_TEXTURE2D_LOD(_Normals,sampler_Normals,b,0).xyz,frames.z));
                Varyings o; o.positionWS=TransformObjectToWorld(p); o.positionCS=TransformWorldToHClip(o.positionWS);
                o.normalWS=TransformObjectToWorldNormal(n); o.color=input.color; o.fog=ComputeFogFactor(o.positionCS.z); return o;
            }
            half4 Frag(Varyings input):SV_Target
            {
                Light light=GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half3 n=normalize(input.normalWS);
                half3 lighting=SampleSH(n)+light.color*saturate(dot(n,light.direction))*light.shadowAttenuation;
                return half4(MixFog(input.color.rgb*lighting,input.fog),1);
            }
            ENDHLSL
        }
    }
}

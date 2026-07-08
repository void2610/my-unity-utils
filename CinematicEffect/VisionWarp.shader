Shader "Hidden/CinematicEffect/VisionWarp"
{
    // 視界の歪み。UV をサイン波と放射方向の揺らぎで変位させる陽炎/酩酊風の全画面歪み。
    // AddBlitPass 経由で _BlitTexture にカメラカラーがバインドされる前提。
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        Pass
        {
            Name "VisionWarp"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            // TEXTURE2D_X / _Time は URP の Core.hlsl が定義するため Blit.hlsl より先に include する
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Strength;    // UV 変位の最大量 (0=無効, 0.02 程度で顕著)
            float _Frequency;   // 波の空間周波数
            float _Speed;       // 時間変化速度

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float t = _Time.y * _Speed;

                // 直交する2方向のサイン波で全体を波打たせる
                float2 offset;
                offset.x = sin(uv.y * _Frequency + t) * _Strength;
                offset.y = cos(uv.x * _Frequency + t * 0.8) * _Strength;

                // 中心からの放射方向に揺らぎを加え「視界が歪む」酩酊感を強める
                float2 toCenter = uv - 0.5;
                float radius = length(toCenter);
                offset += toCenter * sin(radius * _Frequency - t) * _Strength * 0.5;

                float2 warpedUv = clamp(uv + offset, 0.0, 1.0);
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, warpedUv);
            }
            ENDHLSL
        }
    }
    Fallback Off
}

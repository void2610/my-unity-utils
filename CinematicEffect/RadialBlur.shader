Shader "Hidden/CinematicEffect/RadialBlur"
{
    // 放射状 (ズーム) ブラー。中心へ向かって複数タップをサンプルして平均する全画面ブラー。
    // AddBlitPass 経由で _BlitTexture にカメラカラーがバインドされる前提。
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        Pass
        {
            Name "RadialBlur"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            // TEXTURE2D_X は URP の Core.hlsl が定義するため Blit.hlsl より先に include する (内蔵 PP シェーダと同順)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Strength;   // 中心へ寄せる最大割合 (0=無効, 0.3 程度で強い)
            float2 _Center;    // 収束中心 (UV, 既定 0.5,0.5)

            static const int SAMPLE_COUNT = 16;

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float2 toCenter = _Center - uv;

                half4 acc = half4(0, 0, 0, 0);
                UNITY_UNROLL
                for (int i = 0; i < SAMPLE_COUNT; i++)
                {
                    float t = _Strength * (i / (float)(SAMPLE_COUNT - 1));
                    float2 sampleUv = uv + toCenter * t;
                    acc += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, sampleUv);
                }
                return acc / SAMPLE_COUNT;
            }
            ENDHLSL
        }
    }
    Fallback Off
}

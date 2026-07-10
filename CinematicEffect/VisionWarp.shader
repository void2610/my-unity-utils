Shader "Hidden/CinematicEffect/VisionWarp"
{
    // 視界の歪み。UV をアニメーションする fBm ノイズで変位させる陽炎/酩酊風の全画面歪み。
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
            float _Frequency;   // ノイズの空間周波数
            float _Speed;       // 時間変化速度

            // ハッシュベースの値ノイズ。滑らかに補間して連続的な揺らぎを得る
            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = Hash(i);
                float b = Hash(i + float2(1.0, 0.0));
                float c = Hash(i + float2(0.0, 1.0));
                float d = Hash(i + float2(1.0, 1.0));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // 複数オクターブを重ねて有機的な陽炎ディテールを作る
            float Fbm(float2 p)
            {
                float sum = 0.0;
                float amp = 0.5;
                for (int i = 0; i < 4; i++)
                {
                    sum += ValueNoise(p) * amp;
                    p *= 2.0;
                    amp *= 0.5;
                }
                return sum;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float t = _Time.y * _Speed;

                // x/y で異なるシード位置からノイズを引き、UV を有機的に変位させる
                float2 p = uv * _Frequency;
                float nx = Fbm(p + float2(0.0, t));
                float ny = Fbm(p + float2(t * 0.7, 11.5));
                float2 offset = (float2(nx, ny) - 0.5) * 2.0 * _Strength;

                float2 warpedUv = clamp(uv + offset, 0.0, 1.0);
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, warpedUv);
            }
            ENDHLSL
        }
    }
    Fallback Off
}

Shader "Hidden/CinematicEffect/RadialMonochrome"
{
    // 中心からの半径 (_Radius) の内側をカラー、外側を白黒にする全画面マスク。
    // _Radius を大きくすると色が全画面へ広がり、0 にすると白黒が縁から中心へ収縮する。
    // AddBlitPass 経由で _BlitTexture にカメラカラーがバインドされる前提。
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        Pass
        {
            Name "RadialMonochrome"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Radius;      // カラー域の半径 (0=全画面白黒)
            float _Softness;    // 境界のぼかし幅
            float2 _Center;     // マスク中心 (UV)
            float _Aspect;      // width/height (円形補正)

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                float2 d = uv - _Center;
                d.x *= _Aspect;
                float dist = length(d);

                // 半径の内側 (dist<_Radius) は 0=カラー、外側は 1=白黒へ滑らかに遷移
                float mono = smoothstep(_Radius - _Softness, _Radius, dist);
                float gray = dot(color.rgb, float3(0.299, 0.587, 0.114));
                color.rgb = lerp(color.rgb, gray.xxx, mono);
                return color;
            }
            ENDHLSL
        }
    }
    Fallback Off
}

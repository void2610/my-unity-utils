// 暗幕の指定矩形を _MaskTex の階調で抜き、境界をぼかす (SpotlightMask が穴の位置を毎フレーム渡す)
// Unmask (ステンシル) では二値にしかならず、ふんわりした境界を出せないため専用シェーダにしている
Shader "Void2610/UI/SpotlightMask"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _MaskTex ("Hole Mask", 2D) = "black" {}
        // _MaskTex の 9 スライスのボーダー幅 (テクスチャ UV 上)
        _MaskSliceBorder ("Mask Slice Border", Vector) = (0, 0, 0, 0)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float4 worldPosition : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MaskTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MaskSliceBorder;
            // ワールド座標を穴のローカル座標へ移す行列 (Canvas がメッシュをまとめても崩れないようワールドを経由する)
            float4x4 _WorldToHole;
            // 穴の矩形 (穴のローカル座標)。xy = 左下, zw = サイズ。サイズ 0 は穴なし
            float4 _HoleRect;
            // 穴の一辺に対するぼかし幅の比率
            float2 _HoleEdge;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(o.worldPosition);
                o.color = v.color * _Color;
                return o;
            }

            // Sliced Image のメッシュ 9 分割と同じ見た目になるよう、一辺の座標をテクスチャ UV へリマップする
            float SliceAxis(float t, float destBorder, float srcBorder)
            {
                if (destBorder <= 0) return t;
                // 穴がぼかし 2 つ分より狭いと中央領域の除数が 0 になり、NaN が暗幕全体へ広がるので手前で詰める
                destBorder = min(destBorder, 0.4999);
                if (t < destBorder) return t / destBorder * srcBorder;
                if (t > 1 - destBorder) return 1 - (1 - t) / destBorder * srcBorder;
                return srcBorder + (t - destBorder) / (1 - 2 * destBorder) * (1 - 2 * srcBorder);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                half hole = 0;
                if (_HoleRect.z > 0 && _HoleRect.w > 0)
                {
                    float2 local = mul(_WorldToHole, mul(unity_ObjectToWorld, i.worldPosition)).xy;
                    float2 t = (local - _HoleRect.xy) / _HoleRect.zw;
                    // 矩形の外は 0。clamp サンプリングだと縁の階調が外側へ伸びる
                    if (all(t > 0) && all(t < 1))
                    {
                        float2 uv = float2(SliceAxis(t.x, _HoleEdge.x, _MaskSliceBorder.x), SliceAxis(t.y, _HoleEdge.y, _MaskSliceBorder.y));
                        hole = tex2D(_MaskTex, uv).a;
                    }
                }

                fixed4 color = i.color;
                color.a *= 1 - hole;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}

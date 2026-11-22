Shader "Custom/2DOutline"{
    
    Properties{
        _MainTex("Texture",2D) = "white" {}
        _OutlineWidth("OutlineWidth",Range(0,20)) = 0
        _OutlineColor("OutlineColor",Color) = (0,0,0,1)   
        _AlphaThreshold("AlphaThreshold",Range(0,1)) = 0  
        _Light("Light",Range(1,5)) =1
    }

    SubShader{
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent"}



        Pass{
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            
            struct appdata{
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            struct v2f{
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            
            sampler2D _MainTex;
            float4 _MainTex_ST; //获取_MainTex纹理的Tiling和Offset，带入xyzw
            float4 _MainTex_TexelSize;//获取_MainTex纹理的宽高，4个分量如下：Vector4(1 / width, 1 / height, width, height)
            
            float _OutlineWidth;
            float4 _OutlineColor;
            float _AlphaThreshold;
            float _Light;
            
            //顶点着色器不做额外操作
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            
            half MaxNeighborAlpha(float2 uv, float2 radius)
            {
                float2 x = float2(radius.x, 0);
                float2 y = float2(0, radius.y);
                float2 diagonal = radius * 0.70710678;
                float2 otherDiagonal = float2(diagonal.x, -diagonal.y);
                half horizontal = max(tex2D(_MainTex, uv + x).a, tex2D(_MainTex, uv - x).a);
                half vertical = max(tex2D(_MainTex, uv + y).a, tex2D(_MainTex, uv - y).a);
                half slash = max(tex2D(_MainTex, uv + diagonal).a, tex2D(_MainTex, uv - diagonal).a);
                half backslash = max(tex2D(_MainTex, uv + otherDiagonal).a, tex2D(_MainTex, uv - otherDiagonal).a);
                return max(max(horizontal, vertical), max(slash, backslash));
            }

            half4 frag(v2f v) : SV_Target
            {
                half4 col = tex2D(_MainTex, v.uv);
                // 不描边时只采样原图；完全不透明的像素也无需查找轮廓。
                UNITY_BRANCH
                if (_OutlineWidth <= 0 || _OutlineColor.a <= 0 || col.a >= 1)
                    return col;

                // 保留已有的行动状态参数，用更细的轮廓代替旧版的亮边。
                float2 radius = _MainTex_TexelSize.xy * (_OutlineWidth * 0.55);
                half neighbor = MaxNeighborAlpha(v.uv, radius);
                // 不累加透明噪点；使用过渡带保留轮廓边缘的抗锯齿。
                half cutoff = max(0.16, _AlphaThreshold);
                half coverage = smoothstep(0.0, 0.35, neighbor - cutoff);
                half inkAlpha = coverage * _OutlineColor.a * 0.82 * (1 - col.a);
                if (inkAlpha <= 0)
                    return col;

                // 把描边合成到原图后方，不用描边色覆盖半透明的角色边缘。
                half3 ink = saturate(_OutlineColor.rgb * _Light);
                half alpha = col.a + inkAlpha;
                return half4((col.rgb * col.a + ink * inkAlpha) / alpha, alpha);
            }
            ENDCG
        }
    }
    FallBack "Sprites/Default"
}


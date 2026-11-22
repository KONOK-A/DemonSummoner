Shader "LD55/SceneCurtain"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Coverage ("Coverage", Range(0, 1)) = 0
        _CurtainColor ("Curtain", Color) = (0.080, 0.074, 0.106, 1)
        _IronColor ("Iron", Color) = (0.183, 0.166, 0.237, 1)
        _EdgeColor ("Amethyst Edge", Color) = (0.631, 0.529, 0.738, 1)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            struct Attributes
            {
                float4 position : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            float _Coverage;
            half4 _CurtainColor;
            half4 _IronColor;
            half4 _EdgeColor;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.position = UnityObjectToClipPos(input.position);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 p = (input.uv - 0.5) * float2(aspect, 1);
                float2 a = abs(p) * float2(0.87, 1);
                // The sloped facets follow the pointed setting gem.
                float cut = a.x + a.y + 0.19 * min(a.x, a.y);
                float2 corner = 0.5 * float2(0.87 * aspect, 1);
                float cornerCut = corner.x + corner.y + 0.19 * min(corner.x, corner.y);
                float pixel = max(fwidth(cut), 0.000001);
                float margin = 0.03;
                float opening = lerp(cornerCut + margin, -margin, saturate(_Coverage));
                float edgeDistance = (cut - opening) / pixel;

                half coverage = smoothstep(-0.6, 0.6, edgeDistance);
                half bevel = smoothstep(1.0, 2.3, edgeDistance) * (1 - smoothstep(7.4, 9.0, edgeDistance));
                half highlight = smoothstep(2.3, 3.1, edgeDistance) * (1 - smoothstep(3.9, 4.7, edgeDistance));
                half ink = 1 - smoothstep(0.8, 1.8, edgeDistance);
                half sideLight = lerp(0.72, 1, smoothstep(-0.4, 0.4, p.y - p.x * 0.35));
                half3 color = lerp(_CurtainColor.rgb, _IronColor.rgb * sideLight, bevel);
                color = lerp(color, _EdgeColor.rgb * sideLight, highlight * 0.76);
                color *= lerp(1, 0.48, ink);

                // Both endpoints are exact even at small resolutions.
                if (_Coverage <= 0)
                    return 0;
                if (_Coverage >= 1)
                    return half4(_CurtainColor.rgb * input.color.rgb, input.color.a);
                return half4(color * input.color.rgb, coverage * input.color.a);
            }
            ENDCG
        }
    }
}

Shader "LD55/Startup Emblem"
{
    Properties
    {
        [PerRendererData] _MainTex ("Emblem", 2D) = "white" {}
        _Reveal ("Sweep", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct Varyings
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            sampler2D _MainTex;
            float _Reveal;

            Varyings vert(appdata_full input)
            {
                Varyings output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.texcoord.xy;
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 ink = tex2D(_MainTex, input.uv) * input.color;
                float edge = lerp(-.07, 1.07, _Reveal) - dot(input.uv, float2(.76, .24));
                half opening = smoothstep(-.025, .025, edge);
                half shine = (1.0 - smoothstep(.008, .06, abs(edge - .014))) * .30;
                ink.rgb = lerp(ink.rgb, half3(.86, .92, 1.0), shine * opening);
                ink.a *= opening;
                return ink;
            }
            ENDCG
        }
    }
}


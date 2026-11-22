Shader "LD55/Battle Floor Ink"
{
    Properties { [PerRendererData] _Mark ("Mark", Float) = 0 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off Lighting Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            struct Vertex { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct Fragment { float4 position : SV_POSITION; float2 uv : TEXCOORD0; };
            float _Mark;
            Fragment vert (Vertex v)
            {
                Fragment o;
                o.position = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            float segmentDistance(float2 p, float2 a, float2 b)
            {
                float2 d = b-a;
                return length(p-a-d*saturate(dot(p-a,d)/dot(d,d)));
            }
            float diamond(float2 p, float2 size)
            {
                return (dot(abs(p), 1.0/size)-1.0) * (size.x*size.y/length(size));
            }
            float4 ink(float3 color, float alpha) { return float4(color*alpha,alpha); }
            float4 over(float4 back, float4 front) { return front + back*(1.0-front.a); }
            float coverage(float distance, float aa) { return 1.0-smoothstep(-aa,aa,distance); }
            float4 frag(Fragment f) : SV_Target
            {
                // Tile-space units keep the engraving attached to the stone as the camera moves.
                float2 p = (f.uv-0.5)*100.0;
                float aa = max(0.32, 0.55*max(fwidth(p.x),fwidth(p.y)));
                float4 c = 0;
                float rim, jewel;
                if (_Mark < 1.5)
                {
                    bool core = _Mark > 0.5;
                    float2 q = core ? float2(abs(p.x),p.y) : p;
                    float band = saturate(1.0-abs(p.y+24.0)/23.0) * (core ? 0.36 : 0.20);
                    c = ink(float3(0.56,0.055,0.105),band);
                    if (core)
                    {
                        rim = segmentDistance(q,float2(6,-34),float2(19,-35.5));
                        rim = min(rim,segmentDistance(q,float2(19,-35.5),float2(49,-35.5)));
                        rim = min(rim,segmentDistance(q,float2(25,-35.5),float2(32,-31.5)));
                        rim = min(rim,segmentDistance(q,float2(32,-31.5),float2(31,-35.5)));
                        jewel = diamond(p-float2(0,-35),float2(5.3,3.7));
                    }
                    else
                    {
                        rim = segmentDistance(p,float2(-42,-26),float2(-34,-35.5));
                        rim = min(rim,segmentDistance(p,float2(-34,-35.5),float2(49,-35.5)));
                        rim = min(rim,segmentDistance(p,float2(-34,-35.5),float2(-26,-26)));
                        rim = min(rim,segmentDistance(p,float2(-26,-26),float2(-26.5,-33)));
                        jewel = diamond(p-float2(-17,-33),float2(2.6,1.4));
                    }
                    c = over(c,ink(float3(.19,.04,.075),coverage(min(rim-1.7,jewel-0.8),aa)));
                    c = over(c,ink(core ? float3(1,.40,.22) : float3(.92,.32,.28),
                                 coverage(min(rim-.70,jewel),aa)));
                }
                else if (_Mark < 2.5)
                {
                    float2 q=float2(p.x,abs(p.y+16));
                    rim=segmentDistance(p,float2(-12,-16),float2(9,-16));
                    rim=min(rim,segmentDistance(q,float2(1,5.8),float2(9,0)));
                    rim=min(rim,segmentDistance(q,float2(-15,2.6),float2(-10,0)));
                    rim=min(rim,segmentDistance(p,float2(-38,-29),float2(-31,-34)));
                    rim=min(rim,segmentDistance(p,float2(-31,-34),float2(-17,-34)));
                    rim=min(rim,segmentDistance(p,float2(18,-34),float2(31,-34)));
                    rim=min(rim,segmentDistance(p,float2(31,-34),float2(38,-29)));
                    c = ink(float3(.15,.095,.23),coverage(rim-2.15,aa));
                    c = over(c,ink(float3(.86,.73,1),coverage(rim-.95,aa)));
                }
                else if (_Mark < 4.5)
                {
                    // The small silver inlay sits at the far edge of the tile, clear of spell warnings.
                    float2 t=p-float2(0,37);
                    float2 q=float2(t.x,abs(t.y));
                    rim=segmentDistance(q,float2(-12,0),float2(-4,0));
                    rim=min(rim,segmentDistance(q,float2(-4,0),float2(1,3.6)));
                    rim=min(rim,segmentDistance(q,float2(1,3.6),float2(9,0)));
                    if (_Mark < 3.5) rim=min(rim,segmentDistance(q,float2(-4,0),float2(7,0)));
                    c = ink(float3(.11,.13,.18),coverage(rim-1.45,aa)*.95);
                    c = over(c,ink(float3(.76,.80,.89),coverage(rim-.64,aa)*.96));
                }
                else
                {
                    // The player's reticle sits above the enemy's flame band, even on the same tile.
                    float2 t = p - float2(0,-19);
                    if (_Mark > 5.5)
                    {
                        float2 q = abs(t);
                        rim = abs(diamond(t, float2(17,8.8)));
                        rim = min(rim,segmentDistance(q,float2(18,0),float2(25,0)));
                        rim = min(rim,segmentDistance(q,float2(0,9.5),float2(0,14)));
                        jewel = diamond(t,float2(4.8,2.8));
                    }
                    else
                    {
                        float2 q = float2(t.x,abs(t.y));
                        rim = segmentDistance(q,float2(-7,6),float2(3,0));
                        rim = min(rim,segmentDistance(q,float2(2,6),float2(12,0)));
                        jewel = diamond(t-float2(-14,0),float2(3,1.8));
                    }
                    c = ink(float3(.12,.08,.21),coverage(min(rim-2.1,jewel-1.3),aa));
                    c = over(c,ink(float3(.83,.79,.96),coverage(rim-.85,aa)));
                    c = over(c,ink(float3(.77,.53,1),coverage(jewel,aa)));
                }
                return c;
            }
            ENDCG
        }
    }
}

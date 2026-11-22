Shader "LD55/Skirmish Ink"
{
    Properties
    {
        [PerRendererData] _Motif ("Motif", Float) = 0
        [PerRendererData] _Phase ("Phase", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off Lighting Off Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            struct InputVertex { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 position : SV_POSITION; float2 uv : TEXCOORD0; };
            float _Motif, _Phase;
            Varyings vert(InputVertex v)
            {
                Varyings o; o.position=UnityObjectToClipPos(v.vertex); o.uv=v.uv; return o;
            }
            float4 layer(float3 rgb, float a) { return float4(rgb*a,a); }
            float4 addLayer(float4 back, float4 top) { return top+back*(1-top.a); }
            float fill(float d, float aa) { return 1-smoothstep(-aa,aa,d); }
            float seg(float2 p,float2 a,float2 b)
            {
                float2 delta=b-a;
                return length(p-a-delta*saturate(dot(p-a,delta)/max(dot(delta,delta),.00001)));
            }
            float gem(float2 p,float2 size)
            {
                return (dot(abs(p),1/size)-1)*min(size.x,size.y);
            }
            float4 flash(float2 p, float size, float alpha, float aa)
            {
                // Sizes below 1 shrink the entire contact mark about its origin.
                float2 q=p/max(.001,size);
                float d=min(gem(q,float2(.14,.75)),gem(q,float2(.66,.12)));
                d=min(d,gem(float2(q.x+q.y,q.y-q.x)*.7071,float2(.38,.09)));
                return layer(float3(1,.97,.84),alpha*fill(d*size,aa));
            }
            float4 cut(float2 p,float t,bool backhand,float aa)
            {
                float u=(.91-p.x)/1.84;
                float bow=sin(saturate(u)*3.14159);
                float arc=backhand ? -.60+1.29*u-.33*bow : .65-1.22*u+.37*bow;
                float s=backhand ? arc-p.y : p.y-arc;
                float head=min(1.06,.24+3.8*t);
                float tail=max(0,(t-.17)/.83);
                float window=max(max((tail-u)*1.84,(u-head)*1.84),max(-u,u-1.0)*1.84);
                float w=(backhand ? .15 : .105)*pow(max(.001,bow),.72)*
                        saturate((u-tail)/.19)*saturate((head-u)/.12);
                float d=max(max(s-w,-s-.22*w),window);
                float fade=1-smoothstep(.77,1,t);
                float4 c=layer(float3(.14,.042,.102),fill(d-.014,aa)*fade);
                c=addLayer(c,layer(float3(.58,.12,.23),fill(d,aa)*fade));
                c=addLayer(c,layer(float3(.98,.54,.61),fill(max(d,s-w*.66),aa)*fade));
                c=addLayer(c,layer(float3(.99,.95,.95),fill(max(d,abs(s+.1*w)-max(.008,.26*w)),aa)*fade));
                float thread=max(abs(s-(w+.056*bow))-.009*bow,
                                 max((tail+.09-u)*1.84,(u-head+.15)*1.84));
                c=addLayer(c,layer(float3(.84,.44,.53),fill(thread,aa)*fade*.85));
                float2 contact=float2(-.06,backhand ? -.24 : .34);
                float h=(1-smoothstep(.04,.30,t))*(backhand?.47:.32);
                c=addLayer(c,flash(p-contact,h,saturate(h*4),aa));
                return c;
            }
            float4 blockHit(float2 p,float t,float aa)
            {
                float u=saturate((t-.12)/.82);
                float flight=1-(1-u)*(1-u);
                float2 origin=float2(-.24,.06);
                float4 c=0;
                [unroll] for(int i=0;i<6;i++)
                {
                    float a=1.04+i*.48;
                    float2 dir=float2(cos(a),sin(a));
                    float len=.63+.11*(i%3);
                    float2 lead=origin+dir*(.06+flight*len)+float2(0,-.27*u*u);
                    float streak=(.22+.07*(i%2))*(1-.68*u);
                    float2 delta=p-lead;
                    float along=dot(delta,dir);
                    float across=abs(delta.x*dir.y-delta.y*dir.x);
                    float width=lerp(.009,.040,saturate(1+along/streak))*(1-.52*u);
                    float d=max(max(-streak-along,along),across-width);
                    float alpha=smoothstep(.11,.17,t)*(1-smoothstep(.63,.97,t));
                    c=addLayer(c,layer(float3(.23,.20,.22),fill(d-.010,aa)*alpha));
                    c=addLayer(c,layer(i%2==0?float3(1,.78,.43):float3(.85,.94,1),fill(d,aa)*alpha));
                }
                return addLayer(c,flash(p-origin,.48*(1-smoothstep(.19,.42,t)),
                                         smoothstep(.06,.14,t)*(1-smoothstep(.19,.42,t)),aa));
            }
            float wardEdge(float2 p)
            {
                p.x=abs(p.x);
                return max(max(.50*p.x+p.y-.79,p.x-.80),max(p.x-.36*p.y-.65,.88*p.x-p.y-.85));
            }
            float4 ward(float2 p,float t,float aa)
            {
                float d=wardEdge(p);
                float inside=fill(d,aa)*smoothstep(-1.2,-.88,-p.y+t*2.0);
                float4 c=layer(float3(.69,.51,.22),inside*.14);
                c=addLayer(c,layer(float3(.26,.19,.19),fill(abs(d)-.080,aa)*inside));
                c=addLayer(c,layer(float3(.94,.78,.45),fill(abs(d)-.049,aa)*inside));
                float inner=abs(wardEdge(p/float2(.76,.77)))-.028;
                float ribs=min(seg(p,float2(0,-.67),float2(0,.56)),
                               seg(float2(abs(p.x),p.y),float2(.04,-.12),float2(.66,.32)))-.025;
                c=addLayer(c,layer(float3(1,.92,.68),fill(min(inner,ribs),aa)*inside*.94));
                float medallion=gem(p-float2(0,.04),float2(.25,.30));
                c=addLayer(c,layer(float3(.96,.73,.34),fill(medallion,aa)*inside));
                c=addLayer(c,layer(float3(1,.985,.85),fill(max(medallion,p.x),aa)*inside));
                return c;
            }
            float4 shatter(float2 p,float t,float aa)
            {
                float4 c=0;
                float f=1-(1-t)*(1-t);
                [unroll] for(int i=0;i<6;i++)
                {
                    float a=i*1.0472+.23;
                    float2 dir=float2(cos(a),sin(a));
                    float2 center=dir*float2(.13,.31)+dir*(.25+.19*(i%3))*f+float2(0,-.30*t*t);
                    float rotation=a*.43 + (i%2==0?1:-1)*1.34*f;
                    float2 v=p-center;
                    v=float2(v.x*cos(rotation)-v.y*sin(rotation),v.x*sin(rotation)+v.y*cos(rotation));
                    float scale=max(.01,1-smoothstep(.43,1,t));
                    float d=gem(v/scale,float2(.065+.014*(i%3),.12+.04*(i%2)))*scale;
                    c=addLayer(c,layer(float3(.37,.23,.14),fill(d-.012,aa)*scale));
                    c=addLayer(c,layer(v.x>.002?float3(.98,.76,.39):float3(1,.97,.79),fill(d,aa)*scale));
                }
                return addLayer(c,flash(p,.44*(1-t),1-smoothstep(.05,.26,t),aa));
            }
            float4 frag(Varyings f):SV_Target
            {
                float2 p=(f.uv-.5)*2;
                float aa=max(.003,.55*max(fwidth(p.x),fwidth(p.y)));
                if(_Motif<1.5) return cut(p,_Phase,_Motif>.5,aa);
                if(_Motif<2.5) return blockHit(p,_Phase,aa);
                if(_Motif<3.5) return ward(p,_Phase,aa);
                return shatter(p,_Phase,aa);
            }
            ENDCG
        }
    }
}

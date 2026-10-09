Shader "Albion/DowntownRain" { SubShader { Tags { "Queue"="Transparent" "RenderType"="Transparent" } Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off Pass { CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
struct app {float4 vertex:POSITION;fixed4 color:COLOR;float2 uv:TEXCOORD0;};struct v2f {float4 pos:SV_POSITION;fixed4 color:COLOR;float2 uv:TEXCOORD0;};
v2f vert(app v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.color=v.color;o.uv=v.uv;return o;}
fixed4 frag(v2f i):SV_Target {i.color.a*=saturate(1-abs(i.uv.x-.5)*2);return i.color;}
ENDCG } } }

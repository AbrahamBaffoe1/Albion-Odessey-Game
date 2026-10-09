Shader "Albion/ConversationBlur" { Properties {_MainTex("Scene",2D)="white"{}} SubShader {Cull Off ZWrite Off ZTest Always Pass {CGPROGRAM
#pragma vertex vert_img
#pragma fragment frag
#include "UnityCG.cginc"
sampler2D _MainTex; float4 _MainTex_TexelSize;
fixed4 frag(v2f_img i):SV_Target {fixed4 c=0;for(int x=-3;x<=3;x++)for(int y=-3;y<=3;y++)c+=tex2D(_MainTex,i.uv+float2(x,y)*_MainTex_TexelSize.xy*2);c/=49;float lum=dot(c.rgb,float3(.2126,.7152,.0722));c.rgb=lerp(c.rgb,lum*float3(.09,.035,.4),.90);return c;}
ENDCG }} }

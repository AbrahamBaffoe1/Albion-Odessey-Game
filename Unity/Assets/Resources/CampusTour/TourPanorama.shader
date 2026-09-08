Shader "Odyssey/TourPanorama" {
 Properties { _MainTex ("Panorama", 2D) = "white" {} }
 SubShader { Tags { "Queue"="Background" } Cull Off ZWrite Off
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex;
 struct v2f { float4 pos:SV_POSITION; float3 dir:TEXCOORD0; };
 v2f vert(appdata_base v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex);o.dir=v.vertex.xyz;return o; }
 fixed4 frag(v2f i):SV_Target { float3 d=normalize(i.dir);float2 uv=float2(0.5+atan2(d.x,d.z)/6.2831853,0.5+asin(clamp(d.y,-1,1))/3.14159265);return tex2D(_MainTex,uv); }
 ENDCG }
 }
}

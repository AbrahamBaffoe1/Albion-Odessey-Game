Shader "Albion/CosmicTitle" {
 Properties { _Squirrel("Squirrel",2D)="black"{} _Clock("Clock",Float)=0 _Aspect("Aspect",Float)=1.6 }
 SubShader { Cull Off ZWrite Off ZTest Always Pass {
 CGPROGRAM
 #pragma vertex vert_img
 #pragma fragment frag
 #pragma target 3.0
 #include "UnityCG.cginc"
 sampler2D _Squirrel; float _Clock,_Aspect;
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
 float fbm(float2 p){float f=0,a=.5;for(int j=0;j<5;j++){f+=a*noise(p);p=mul(float2x2(.8,-.6,.6,.8),p)*2.03+13.2;a*=.5;}return f;}
 float4 animal(float2 uv,float side){
  float height=.76,width=height*(2.0/3.0)/_Aspect;
  float x=side<0?.015:1-.015-width;
  float2 q=(uv-float2(x,.12))/float2(width,height);
  if(side>0)q.x=1-q.x;
  if(any(q<0)||any(q>1))return 0;
  float t=_Clock+ (side>0?2.7:0);
  // Independent soft-rig weights keep feet planted while chest, head and tail move.
  float tail=(1-smoothstep(.4,.61,q.x))*smoothstep(.12,.72,q.y);
  float flick=pow(max(0,sin(t*.38)),14)*sin(t*5.2);
  q.x-=tail*(sin(t*1.18)*.017+flick*.012);
  q.y-=tail*sin(t*.93)*.007;
  float chest=exp(-dot((q-float2(.64,.4))*float2(3,3),(q-float2(.64,.4))*float2(3,3)));
  q.x+=(q.x-.6)*sin(t*1.7)*.014*chest;
  q.y-=sin(t*1.7)*.003*chest;
  float head=smoothstep(.53,.69,q.y)*smoothstep(.44,.61,q.x);
  float a=(sin(t*.57)*.019+sin(t*1.03)*.006)*head;
  float2 d=q-float2(.61,.57);q=float2(cos(a)*d.x-sin(a)*d.y,sin(a)*d.x+cos(a)*d.y)+float2(.61,.57);
  float4 c=tex2D(_Squirrel,q);c.rgb*=side<0?float3(1.05,.94,.82):float3(.8,.94,1.08);
  c.a*=smoothstep(0,.035,q.y);return c;
 }
 float4 frag(v2f_img i):SV_Target {
  float2 uv=i.uv,p=(uv-.5)*float2(_Aspect,1);float t=_Clock;
  float2 drift=float2(t*.012,-t*.007);
  float n=fbm(p*3+drift);float curls=fbm(p*5+float2(n*2,t*.014));
  float dust=pow(saturate(curls*.8+n*.55-.25),2);
  float band=exp(-pow((p.y-.14-sin(p.x*2+t*.06)*.13)*3.4,2));
  float3 hue=lerp(float3(.6,.28,.065),float3(.035,.25,.46),smoothstep(-.55,.5,p.x));
  float3 col=float3(.004,.008,.023)+hue*dust*(.42+band*1.3);
  col+=float3(.12,.17,.23)*pow(saturate(n*curls),3)*band;
  col*=.10;
  // Three parallax star layers. Twinkle is smooth and never a full-screen flash.
  for(int k=0;k<3;k++){
   float scale=90+k*53;float2 cell=(p+float2(t*.0009*(k+1),t*.0003))*scale;
   float2 id=floor(cell),f=frac(cell)-.5;float seed=hash(id+17*k);
   float star=exp(-dot(f,f)*(220+100*k))*step(.979,seed);
   col+=star*(.35+.3*sin(t*(.7+seed)+seed*43))*lerp(float3(1,.75,.4),float3(.5,.8,1),seed);
  }
  float cycle=floor(t/11),age=fmod(t,11);float2 start=float2(.7+hash(float2(cycle,1))*.5,.48);
  float2 delta=p-(start+float2(-.55,-.25)*age);float along=dot(delta,normalize(float2(.55,.25))),across=dot(delta,normalize(float2(-.25,.55)));
  float meteor=exp(-abs(across)*800)*exp(-max(0,along)*18)*step(0,along)*step(age,1.7)*smoothstep(0,.25,age);
  col+=meteor*float3(.45,.65,.85)*.35;
  // Keep the central navigation quiet and readable.
  col*=1-.48*exp(-p.x*p.x*12)*smoothstep(.15,-.5,p.y);
  float4 left=animal(uv,-1),right=animal(uv,1);col=lerp(col,left.rgb,left.a);col=lerp(col,right.rgb,right.a);
  return float4(col,1);
 }
 ENDCG
 }}
}

Shader "Ataka/ScenarioVHS" {
 Properties { _Peephole("Peephole",Float)=0 _Wear("Signal wear",Float)=0 _Lens("Lens",Float)=.08 _LumaBlur("Luma blur",Float)=2 _ChromaBlur("Color blur",Float)=8 _ChromaShift("Color delay",Float)=3 _GhostPixels("Ghost delay",Float)=9 _GhostWeight("Ghost",Float)=.08 _Aberration("Edge RGB",Float)=2 _Grain("Grain",Float)=.045 _Jitter("Line jitter",Float)=.4 _Scanlines("Scanlines",Float)=.025 _Disruption("Disruption",Float)=0 }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline"} Pass {
 ZTest Always ZWrite Off Cull Off
 HLSLPROGRAM
 #pragma vertex Vert
 #pragma fragment Frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
 float _Wear; float _Peephole;
 float _Lens,_LumaBlur,_ChromaBlur,_ChromaShift,_GhostPixels,_GhostWeight,_Aberration,_Grain,_Jitter,_Scanlines,_Disruption;
 float4 _VhsOutputSize;
 float Hash(float2 p) { uint2 v=asuint((int2)floor(p));uint h=v.x*1664525u+v.y*1013904223u+374761393u;h=(h^(h>>13u))*1274126177u;h^=h>>16u;return (h&0x00ffffffu)/16777216.0; }
 float SmoothNoise(float2 p) {float2 i=floor(p),f=frac(p); f=f*f*(3-2*f);return lerp(lerp(Hash(i),Hash(i+float2(1,0)),f.x),lerp(Hash(i+float2(0,1)),Hash(i+1),f.x),f.y);}
 float3 Yiq(float3 c){return float3(dot(c,float3(.299,.587,.114)),dot(c,float3(.596,-.274,-.322)),dot(c,float3(.211,-.523,.312)));}
 float3 Rgb(float3 c){return float3(dot(c,float3(1,.956,.621)),dot(c,float3(1,-.272,-.647)),dot(c,float3(1,-1.106,1.703)));}
 float3 Read(float2 uv){return LinearToSRGB(max(0,SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,saturate(uv)).rgb));}
 half4 Frag(Varyings input):SV_Target {
  float2 size=max(_VhsOutputSize.xy,float2(1920,1080)), px=1/size;
  float unit=size.y/1080, tick=floor(_Time.y*25),time=_Time.y;
  float2 d=(input.texcoord-.5)*2;
  float2 uv=.5 + .5*d*(1+(_Lens+_Peephole*.28)*dot(d,d))*.955;
  float y=uv.y*size.y;
  uv.x+=sin(y*.03+time*2)*_Jitter*unit*px.x;
  float tracking=step(19.45,fmod(time,20))*smoothstep(.72,.86,uv.y);
  uv.x+=(tracking*.8+_Disruption*8)*sin(y*.12+tick)*unit*px.x;
  uv.y=lerp(uv.y,frac(uv.y+sin(tick)*.055),_Disruption);
  float3 center=Yiq(Read(uv)); float luma=0; float2 iq=0;
  // Luminance and chroma have separate horizontal bandwidth. No vertical blur.
  [unroll] for(int k=-3;k<=3;k++) {
   float weight=k==0?.28:abs(k)==1?.22:abs(k)==2?.11:.03;
   luma+=Yiq(Read(uv+float2(k*_LumaBlur/3*unit*px.x,0))).x*weight;
   // Negative sample offset carries an object's color to the RIGHT of its edge.
   iq+=Yiq(Read(uv+float2((k*_ChromaBlur/3-_ChromaShift)*unit*px.x,0))).yz*weight;
  }
  luma=lerp(luma,Yiq(Read(uv-float2(_GhostPixels*unit*px.x,0))).x,_GhostWeight);
  float3 col=Rgb(float3(luma,iq));
  float edge=saturate(dot(d,d)/1.5), shift=lerp(.5,_Aberration,edge)*unit*px.x;
  float3 left=Read(uv-float2(shift,0)),right=Read(uv+float2(shift,0));
  col.r+=.5*(left.r-Read(uv).r);col.b+=.5*(right.b-Read(uv).b);
  float2 p=input.texcoord*size / (float2(1.8,1.6)*unit);
  float n=SmoothNoise(p+float2(tick*71,tick*13))-.5;
  float nc=SmoothNoise(p+float2(tick*31+47,tick*17))-.5;
  col+=(n+float3(nc*.18,0,-nc*.18))*_Grain*(1.2-saturate(luma))*(1+_Disruption*5);
  // Low-amplitude, broader signal noise: no fake film scratches or pixel quantization.
  float aged=SmoothNoise(input.texcoord*size/float2(5,3)+float2(tick*19,tick*11))-.5;
  col+=aged*.008*_Wear*(1-saturate(luma));
  col*=1-_Scanlines*(.5+.5*cos(input.texcoord.y*size.y*3.14159265/unit));
  col=lerp(float3(.03,.015,.02),1,saturate(col));
  float2 roundMask=d*float2(size.x/size.y,1); col*=lerp(1,1-smoothstep(.75,.94,length(roundMask)),_Peephole); return half4(SRGBToLinear(saturate(col)),1);
 }
 ENDHLSL
 } }
}

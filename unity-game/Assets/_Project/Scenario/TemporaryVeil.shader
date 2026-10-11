Shader "Ataka/TemporaryVeil" {
 SubShader {Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Geometry+20"} Pass {
 Cull Off ZWrite On
 HLSLPROGRAM
 #pragma vertex Vert
 #pragma fragment Frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A {float4 vertex:POSITION;float2 uv:TEXCOORD0;};struct V {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};
 V Vert(A v){V o;o.pos=TransformObjectToHClip(v.vertex.xyz);o.uv=v.uv;return o;}
 half4 Frag(V i,FRONT_FACE_TYPE front:FRONT_FACE_SEMANTIC):SV_Target {
  float noise=frac(sin(dot(floor(i.uv*512),float2(127.1,311.7)))*43758.5);
  float3 skin=float3(.34,.19,.12)*(0.87+noise*.2);float3 inner=float3(.11,.005,.008)*(0.8+noise*.2);
  return half4(IS_FRONT_VFACE(front,skin,inner),1);
 }
 ENDHLSL
 } }
}

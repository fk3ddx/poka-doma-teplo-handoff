Shader "Ataka/Scenario Wet Mark"
{
 Properties { _WetFactor ("Floor multiplier", Range(0,1)) = 0.55 }
 SubShader {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-10" "RenderType"="Transparent" }
  Pass {
   Name "WetMultiply"
   Tags { "LightMode"="SRPDefaultUnlit" }
   // Preserve the already lit floor texture, including blackout. No opaque black disc.
   Blend DstColor Zero
   ColorMask RGB
   ZWrite Off
   ZTest LEqual
   Cull Off
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
    float _WetFactor;
   CBUFFER_END
   struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
   struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
   Varyings Vert(Attributes v) {
    Varyings o; o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.uv=v.uv;return o;
   }
   half4 Frag(Varyings i):SV_Target {
    float2 p=i.uv*2-1;
    float a=atan2(p.y,p.x);
    float edge=0.73+0.045*sin(5*a+0.6)+0.025*sin(9*a-0.4)+0.018*cos(13*a);
    float coverage=1-smoothstep(edge-0.10,edge+0.035,length(p));
    return half4(lerp(1.0,_WetFactor,coverage).xxx,1);
   }
   ENDHLSL
  }
 }
}

Shader "Ataka/PlanarMirror" {
 Properties { _ReflectionTex("Reflected room",2D)="black"{} }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"} Cull Off
 Pass { HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A {float4 positionOS:POSITION;};struct V {float4 p:SV_POSITION;float4 reflected:TEXCOORD0;};
 TEXTURE2D(_ReflectionTex);SAMPLER(sampler_ReflectionTex);
 CBUFFER_START(UnityPerMaterial) float4x4 _ReflectionVP;CBUFFER_END
 V vert(A a){V o;float3 world=TransformObjectToWorld(a.positionOS.xyz);o.p=TransformWorldToHClip(world);o.reflected=mul(_ReflectionVP,float4(world,1));return o;}
 half4 frag(V i):SV_Target {float2 uv=i.reflected.xy/i.reflected.w*.5+.5;
 #if UNITY_UV_STARTS_AT_TOP
 uv.y=1-uv.y;
 #endif
 half3 c=SAMPLE_TEXTURE2D(_ReflectionTex,sampler_ReflectionTex,uv).rgb;return half4(c*half3(.79,.82,.79)+half3(.008,.010,.009),1);}
 ENDHLSL }
 }
}

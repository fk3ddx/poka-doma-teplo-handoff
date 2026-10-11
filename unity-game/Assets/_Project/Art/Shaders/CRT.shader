Shader "Ataka/CRT" {
 Properties { _BaseMap("Programme",2D)="white"{} _Power("Power",Float)=1 }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
 Pass { HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A {float4 positionOS:POSITION;float2 uv:TEXCOORD0;};struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;};
 TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);CBUFFER_START(UnityPerMaterial) float4 _BaseMap_ST;float _Power;CBUFFER_END
 V vert(A a){V o;o.p=TransformObjectToHClip(a.positionOS.xyz);o.uv=a.uv;return o;}
 half4 frag(V i):SV_Target {float2 uv=float2(1-i.uv.x,i.uv.y);float band=.95+.05*sin(uv.y*700);float noise=frac(sin(dot(uv+floor(_Time.y*24),float2(12.9898,78.233)))*43758.5453);float edge=smoothstep(0,.035,uv.x)*smoothstep(0,.035,uv.y)*smoothstep(0,.035,1-uv.x)*smoothstep(0,.035,1-uv.y);float3 c=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv).rgb;return half4((c*band*.55+(noise-.5)*.015)*edge*_Power+.006,1);}
 ENDHLSL }
 }
}

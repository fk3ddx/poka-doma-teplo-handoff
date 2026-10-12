Shader "Ataka/ExteriorAtmosphere" {
 Properties { _BaseMap("Surface",2D)="white"{} _BaseColor("Tint",Color)=(1,1,1,1) _EmissionMap("Window",2D)="white"{} [HDR]_EmissionColor("Window light",Color)=(0,0,0,1) _FogColor("Exterior mist",Color)=(.19,.25,.30,1) _Density("Mist density",Float)=.025 _LightLevel("Night sky light",Float)=.48 _Power("Electricity",Float)=1 }
 SubShader {Tags {"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"} Pass {Tags{"LightMode"="SRPDefaultUnlit"} Cull Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);TEXTURE2D(_EmissionMap);SAMPLER(sampler_EmissionMap);
 CBUFFER_START(UnityPerMaterial)
 float4 _BaseMap_ST;half4 _BaseColor,_EmissionColor,_FogColor;float _Density,_LightLevel,_Power;
 CBUFFER_END
 struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
 struct V {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float2 uv:TEXCOORD2;};
 V vert(A i){V o;o.world=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(i.normalOS);o.uv=TRANSFORM_TEX(i.uv,_BaseMap);return o;}
 half4 frag(V i):SV_Target{
 half3 base=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb*_BaseColor.rgb;
 float lambert=.22+.78*saturate(dot(normalize(i.normal),normalize(float3(.3,.8,.4))));
 half3 color=base*half3(.42,.57,.72)*lambert*_LightLevel;
 // Pools belong to the external streetlamps; no new illumination is injected into the interior.
 float pool=0;float2 p=i.world.xz;pool+=exp(-dot(p-float2(-11.3,-15),p-float2(-11.3,-15))*.055);pool+=exp(-dot(p-float2(11.7,-15),p-float2(11.7,-15))*.055);pool+=exp(-dot(p-float2(27.7,-15),p-float2(27.7,-15))*.055);
 color+=base*half3(1,.48,.16)*pool*saturate(i.normal.y)*.7*_Power;
 color+=SAMPLE_TEXTURE2D(_EmissionMap,sampler_EmissionMap,i.uv).rgb*_EmissionColor.rgb*_Power;
 float distanceToCamera=distance(i.world,_WorldSpaceCameraPos);float fog=1-exp(-pow(max(0,distanceToCamera-6)*_Density,2));
 return half4(lerp(color,_FogColor.rgb,saturate(fog)),1);
 }
 ENDHLSL
 }} }

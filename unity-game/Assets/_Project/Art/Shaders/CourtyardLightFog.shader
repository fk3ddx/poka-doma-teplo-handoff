Shader "Ataka/CourtyardLightFog" {
 Properties { _Density("Medium density",Float)=0.12 _Gain("Scattering strength",Float)=5 }
 SubShader {
  Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent-20" }
  Pass {
   Tags { "LightMode"="UniversalForward" }
   Blend One One
   ZWrite Off ZTest Always Cull Front
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 3.5
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
   struct A { float4 positionOS:POSITION; };
   struct V { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; };
   CBUFFER_START(UnityPerMaterial)
    float4 _CenterRadius, _SourceColor, _LightDirection, _Cone;
    float _Density, _Gain;
   CBUFFER_END
   V vert(A a){V o;o.positionWS=TransformObjectToWorld(a.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);return o;}
   half4 frag(V i):SV_Target {
    float3 origin=GetCameraPositionWS(), direction=normalize(i.positionWS-origin);
    float3 delta=origin-_CenterRadius.xyz;
    float b=dot(delta,direction), c=dot(delta,delta)-_CenterRadius.w*_CenterRadius.w;
    float discriminant=b*b-c;if(discriminant<=0)return 0;
    float root=sqrt(discriminant), start=max(0,-b-root), end=-b+root;
    float2 uv=i.positionCS.xy/_ScaledScreenParams.xy;
    float depth=SampleSceneDepth(uv);
    #if !UNITY_REVERSED_Z
     depth=lerp(UNITY_NEAR_CLIP_VALUE,1,depth);
    #endif
    float3 opaquePosition=ComputeWorldSpacePosition(uv,depth,UNITY_MATRIX_I_VP);
    end=min(end,distance(origin,opaquePosition)-0.01);
    if(end<=start)return 0;
    const int steps=24;
    float stepLength=(end-start)/steps;
    // Fixed screen dither breaks integration bands without animated sparkle.
    float jitter=frac(52.9829189*frac(dot(i.positionCS.xy,float2(.06711056,.00583715))));
    float transmittance=1;float3 scattering=0;
    [loop] for(int sampleIndex=0;sampleIndex<steps;sampleIndex++){
     float t=start+(sampleIndex+jitter)*stepLength;
     float3 p=origin+direction*t, fromLight=p-_CenterRadius.xyz;
     float distanceSquared=dot(fromLight,fromLight);
     float edge=saturate(1-distanceSquared/(_CenterRadius.w*_CenterRadius.w));
     float variation=.86+.14*sin(p.x*.73+p.y*.31)*sin(p.z*.57-p.y*.43);
     float extinction=_Density*edge*edge*variation;
     float optical=1-exp(-extinction*stepLength);
     float cosine=dot(fromLight*rsqrt(max(distanceSquared,.0001)),_LightDirection.xyz);
     float cone=smoothstep(_Cone.y,_Cone.x,cosine);
     // A weak isotropic lobe softens the halo above the downlight cone.
     float illumination=(.15+.85*cone)/(1+distanceSquared*.5);
     float distanceFog=exp(-_Cone.z*_Cone.z*t*t);
     scattering+=transmittance*optical*illumination*_SourceColor.rgb*_Gain*distanceFog;
     transmittance*=1-optical;
    }
    // Base atmospheric extinction already comes from the scene's distance fog.
    // Add only local in-scattering; otherwise overlapping volumes darken the yard twice.
    return half4(scattering,0);
   }
   ENDHLSL
  }
 }
}

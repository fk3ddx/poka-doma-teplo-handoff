Shader "Ataka/DepthText" {
 Properties { _MainTex("Font atlas",2D)="white"{} }
 SubShader { Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
  Pass { ZTest LEqual ZWrite Off Cull Off Blend SrcAlpha OneMinusSrcAlpha
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
   struct A { float4 position:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR; };
   struct V { float4 position:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR; };
   V vert(A i){V o;o.position=TransformObjectToHClip(i.position.xyz);o.uv=i.uv;o.color=i.color;return o;}
   half4 frag(V i):SV_Target { half alpha=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).a;return half4(i.color.rgb,i.color.a*alpha); }
   ENDHLSL
  }
 }
}

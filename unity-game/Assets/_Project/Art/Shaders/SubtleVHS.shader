Shader "Ataka/SubtleVHS"
{
    Properties { _Strength("Strength", Range(0,1)) = 1 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "SubtleVHS"
            ZWrite Off ZTest Always Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            float _Strength;
            float Noise(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float tick = floor(_Time.y * 24);
                float lineNoise = Noise(float2(floor(uv.y * 480),tick));
                uv.x += (lineNoise-.5) * .00065 * _Strength;
                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,uv);
                float grain = (Noise(uv * _ScreenParams.xy + tick)-.5) * .035;
                float scan = sin(uv.y * _ScreenParams.y * 3.14159) * .002;
                // Multiplicative grain preserves black and stays subtle after linear-to-sRGB conversion.
                color.rgb *= 1 + (grain+scan) * _Strength;
                return color;
            }
            ENDHLSL
        }
    }
}

Shader "Ataka/SubtleVHS"
{
    Properties
    {
        _Strength("VHS strength", Range(0,1)) = 1
        _ColorLevels("Color levels per channel", Range(16,128)) = 64
        _DitherStrength("Dither strength", Range(0,1)) = 0.65
        _Grain("Grain", Range(0,0.05)) = 0.012
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "RetroColorAndVHS"
            ZWrite Off ZTest Always Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            float _Strength, _ColorLevels, _DitherStrength, _Grain;
            float Noise(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float Bayer4(uint2 p)
            {
                // Stable 4x4 ordered pattern at the actual internal pixel resolution.
                const float b[16] = {0,8,2,10, 12,4,14,6, 3,11,1,9, 15,7,13,5};
                return (b[(p.y & 3u)*4u + (p.x & 3u)] + .5)/16.0 - .5;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float2 resolution = _BlitTexture_TexelSize.zw;
                uint2 pixel = (uint2)floor(uv * resolution);
                float tick = floor(_Time.y * 24);
                float lineNoise = Noise(float2(pixel.y, tick));
                // Rare one-pixel tracking displacement, never continuous camera shake.
                float tracking = step(.994, Noise(float2(tick, 7))) * (lineNoise-.5);
                uv.x += tracking * _Strength / max(1.0, resolution.x);
                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv);
                float3 display = LinearToSRGB(saturate(color.rgb));
                float luminance = dot(display, float3(.2126,.7152,.0722));
                display += (Noise(float2(pixel) + tick)-.5) * _Grain * saturate(luminance*4);
                display *= 1.0 - .004 * _Strength * (pixel.y & 1u);
                float levels = max(2.0, _ColorLevels) - 1.0;
                display = floor(saturate(display) * levels + .5 + Bayer4(pixel)*_DitherStrength)/levels;
                return half4(SRGBToLinear(saturate(display)), color.a);
            }
            ENDHLSL
        }
    }
}

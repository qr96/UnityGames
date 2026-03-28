Shader "Custom/URP/PixelArtPost"
{
    Properties
    {
        _MainTex          ("Texture",          2D)            = "white" {}
        _PixelWidth       ("Pixel Width",      Int)           = 320
        _PixelHeight      ("Pixel Height",     Int)           = 180
        _ColorLevels      ("Color Levels",     Range(2, 32))  = 8
        _OutlineThreshold ("Outline Threshold",Range(0,1))    = 0.1
        _OutlineColor     ("Outline Color",    Color)         = (0,0,0,1)
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off Blend Off

        Pass
        {
            Name "PixelArtPostPass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            // URP 코어 라이브러리 (Unity 6 경로)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // Full Screen Pass Renderer Feature는 이 hlsl만 있으면 됨
            // _BlitTexture, Vert, Varyings 모두 자동 제공
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            // Blit.hlsl이 _BlitTexture / sampler_LinearClamp 제공
            // _MainTex는 Blitter가 자동으로 _BlitTexture에 바인딩함

            TEXTURE2D(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);

            int   _PixelWidth;
            int   _PixelHeight;
            float _ColorLevels;
            float _OutlineThreshold;
            float4 _OutlineColor;

            // ── 색상 양자화 ────────────────────────────────────────────
            float3 Quantize(float3 c, float levels)
            {
                return floor(c * levels + 0.5) / levels;
            }

            TEXTURE2D(_CameraDepthNormalsTexture);
            SAMPLER(sampler_CameraDepthNormalsTexture);

            // ── 노멀 기반 엣지 (더 얇고 깔끔) ────────────────────────
            float SobelEdge(float2 uv)
            {
                float2 tx = float2(1.0 / _PixelWidth, 1.0 / _PixelHeight);

                // 깊이 차이 (오브젝트 외곽)
                float d0 = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, uv).r;
                float d1 = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, uv + tx * float2(1, 0)).r;
                float d2 = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, uv + tx * float2(0, 1)).r;
                float depthEdge = abs(d0 - d1) + abs(d0 - d2);

                // 노멀 차이 (면과 면 사이 경계)
                float3 n0 = SAMPLE_TEXTURE2D(_CameraDepthNormalsTexture, sampler_CameraDepthNormalsTexture, uv).rgb * 2 - 1;
                float3 n1 = SAMPLE_TEXTURE2D(_CameraDepthNormalsTexture, sampler_CameraDepthNormalsTexture, uv + tx * float2(1, 0)).rgb * 2 - 1;
                float3 n2 = SAMPLE_TEXTURE2D(_CameraDepthNormalsTexture, sampler_CameraDepthNormalsTexture, uv + tx * float2(0, 1)).rgb * 2 - 1;
                float normalEdge = length(n0 - n1) + length(n0 - n2);

                // 둘을 합산 (깊이는 외곽, 노멀은 면 경계 담당)
                return depthEdge * 10.0 + normalEdge * 0.5;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // ① UV를 픽셀 그리드에 스냅
                float2 uv = input.texcoord;
                float2 snapped = float2(
                    floor(uv.x * _PixelWidth)  / _PixelWidth,
                    floor(uv.y * _PixelHeight) / _PixelHeight
                );

                // ② 색상 샘플 + 양자화
                float3 col = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, snapped).rgb;
                col = Quantize(col, _ColorLevels);

                // ③ 외곽선 합성
                float edge    = SobelEdge(snapped);
                float outline = step(_OutlineThreshold, edge);
                col = lerp(col, _OutlineColor.rgb, outline * _OutlineColor.a);

                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
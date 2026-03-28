Shader "Custom/URP/GroundUnlit"
{
    Properties
    {
        _BaseMap        ("Texture",    2D)           = "white" {}
        _BaseColor      ("Color",      Color)         = (1,1,1,1)
        _Brightness     ("Brightness", Range(0, 2))   = 1.0
        _Saturation     ("Saturation", Range(0, 2))   = 1.0
        _Contrast       ("Contrast",   Range(0, 2))   = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"     = "Opaque"
            "Queue"          = "Geometry"
        }

        Pass
        {
            Name "GroundUnlitPass"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 posOS : POSITION;
                float2 uv    : TEXCOORD0;
            };

            struct Varyings
            {
                float4 posCS : SV_POSITION;
                float2 uv    : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float  _Brightness;
                float  _Saturation;
                float  _Contrast;
            CBUFFER_END

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.posCS = TransformObjectToHClip(IN.posOS.xyz);
                OUT.uv    = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

                // 밝기
                col.rgb *= _Brightness;

                // 채도
                half grey = dot(col.rgb, half3(0.299, 0.587, 0.114));
                col.rgb = lerp(half3(grey, grey, grey), col.rgb, _Saturation);

                // 대비
                col.rgb = (col.rgb - 0.5) * _Contrast + 0.5;
                col.rgb = saturate(col.rgb);

                return col;
            }
            ENDHLSL
        }
    }
}
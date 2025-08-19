Shader "Custom/URP_GaussianBlur"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _BlurSize("Blur Size", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Overlay" }

        Pass
        {
            Name "GaussianBlur"
            ZTest Always Cull Off ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float _BlurSize;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 col = half4(0,0,0,0);

                // 9-tap Gaussian blur
                float2 uv = IN.uv;
                float2 offset = _BlurSize / _ScreenParams.xy;

                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-offset.x, -offset.y)) * 0.0625;
                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0, -offset.y)) * 0.125;
                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(offset.x, -offset.y)) * 0.0625;

                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-offset.x, 0)) * 0.125;
                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0, 0)) * 0.25;
                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(offset.x, 0)) * 0.125;

                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-offset.x, offset.y)) * 0.0625;
                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0, offset.y)) * 0.125;
                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(offset.x, offset.y)) * 0.0625;

                return col;
            }

            ENDHLSL
        }
    }
}

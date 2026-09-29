Shader "Lean60/PlatformBox"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _EdgeWidth ("Edge Width", Float) = 0.07
        _ObjectScale ("Object Scale", Vector) = (1,1,1,0)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float4 _ObjectScale;
            float _EdgeWidth;
        CBUFFER_END

        struct Attributes
        {
            float4 positionOS : POSITION;
            float4 color : COLOR;
            float2 uv : TEXCOORD0;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float4 color : COLOR;
            float2 uv : TEXCOORD0;
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
            output.uv = input.uv;
            output.color = input.color * _Color;
            return output;
        }

        half4 Frag(Varyings input) : SV_Target
        {
            float2 worldSize = max(abs(_ObjectScale.xy), 1e-4);
            float e = min(_EdgeWidth, min(worldSize.x, worldSize.y) * 0.4);
            float2 edgeUv = e / worldSize;
            bool rim = input.uv.x < edgeUv.x || input.uv.x > 1.0 - edgeUv.x
                    || input.uv.y < edgeUv.y || input.uv.y > 1.0 - edgeUv.y;
            half4 color = rim ? half4(1, 1, 1, 1) : half4(0, 0, 0, 1);
            color *= input.color;
            color.rgb *= color.a;
            return color;
        }
        ENDHLSL

        Pass
        {
            Name "SpriteUnlit"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }

        Pass
        {
            Name "SpriteUnlitForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}

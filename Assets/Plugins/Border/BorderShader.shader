Shader "Border/BorderShader"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
    }

    SubShader
    {
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Off
        Blend One Zero

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile __ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color : COLOR;
                half2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            float4 _SpriteUV;
            float4 _Color;

            float2 _SrcPixel;
            float2 _DstPixel;
            float2 _Scale;

            sampler2D _MainTex;

            float check(float2 final, float2 pixel, float2 offset)
            {
                float2 coord = final + pixel * offset;
                half4 neighbour = tex2D(_MainTex, coord);

                if (coord.x < _SpriteUV.x
                 || coord.y < _SpriteUV.y
                 || coord.x > _SpriteUV.z
                 || coord.y > _SpriteUV.w)
                {
                    neighbour *= 0;
                }

                return neighbour.a;
            }

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.worldPosition = IN.vertex;
                OUT.vertex = mul(UNITY_MATRIX_MVP, OUT.worldPosition);

                OUT.texcoord = IN.texcoord;

                #ifdef UNITY_HALF_TEXEL_OFFSET
                OUT.vertex.xy += (_ScreenParams.zw - 1.0)*float2(-1,1);
                #endif

                OUT.color = IN.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 pixel  = _SrcPixel;
                float2 offset = _DstPixel;
                float2 scale  = _Scale;

                float2 corrected = (IN.texcoord - offset) * scale;
                float2 final = lerp(_SpriteUV.xy, _SpriteUV.zw, corrected);

                fixed4 color = tex2D(_MainTex, final);

                if (corrected.x < 0
                 || corrected.y < 0
                 || corrected.x > 1
                 || corrected.y > 1)
                {
                    color *= 0;
                }

                float mult = 0;

                mult += check(final, pixel, float2( 0,  1));
                mult += check(final, pixel, float2( 0, -1));
                mult += check(final, pixel, float2( 1,  1));
                mult += check(final, pixel, float2( 1, -1));
                mult += check(final, pixel, float2(-1,  1));
                mult += check(final, pixel, float2(-1, -1));
                mult += check(final, pixel, float2(-1,  0));
                mult += check(final, pixel, float2( 1,  0));

                mult *= 1 - check(final, pixel, float2(0, 0));

                if (mult > 0)
                {
                    return fixed4(1, 1, 1, 1);
                }
                else
                {
                    return fixed4(0, 0, 0, 0);
                }
            }

            ENDCG
        }
    }
}

Shader "Sprites/Palette-Swap"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
		_Color("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap("Pixel snap", Float) = 0

		_Palette00("Palette00", Color) = (1, 1, 1, 1)
		_Palette01("Palette01", Color) = (1, 1, 1, 1)
		_Palette02("Palette02", Color) = (1, 1, 1, 1)
		_Palette03("Palette03", Color) = (1, 1, 1, 1)
		_Palette04("Palette04", Color) = (1, 1, 1, 1)
		_Palette05("Palette05", Color) = (1, 1, 1, 1)
		_Palette06("Palette06", Color) = (1, 1, 1, 1)
		_Palette07("Palette07", Color) = (1, 1, 1, 1)
		_Palette08("Palette08", Color) = (1, 1, 1, 1)
		_Palette09("Palette09", Color) = (1, 1, 1, 1)
		_Palette10("Palette10", Color) = (1, 1, 1, 1)
		_Palette11("Palette11", Color) = (1, 1, 1, 1)
		_Palette12("Palette12", Color) = (1, 1, 1, 1)
		_Palette13("Palette13", Color) = (1, 1, 1, 1)
		_Palette14("Palette14", Color) = (1, 1, 1, 1)
		_Palette15("Palette15", Color) = (1, 1, 1, 1)
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
		}

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
		{
			CGPROGRAM
	#pragma vertex vert
	#pragma fragment frag
	#pragma multi_compile _ PIXELSNAP_ON
	#pragma shader_feature ETC1_EXTERNAL_ALPHA
	#include "UnityCG.cginc"

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
				float2 texcoord  : TEXCOORD0;
			};

			fixed4 _Color;

			fixed4 _Palette00;
			fixed4 _Palette01;
			fixed4 _Palette02;
			fixed4 _Palette03;
			fixed4 _Palette04;
			fixed4 _Palette05;
			fixed4 _Palette06;
			fixed4 _Palette07;
			fixed4 _Palette08;
			fixed4 _Palette09;
			fixed4 _Palette10;
			fixed4 _Palette11;
			fixed4 _Palette12;
			fixed4 _Palette13;
			fixed4 _Palette14;
			fixed4 _Palette15;

			v2f vert(appdata_t IN)
			{
				v2f OUT;
				OUT.vertex = mul(UNITY_MATRIX_MVP, IN.vertex);
				OUT.texcoord = IN.texcoord;
				OUT.color = IN.color * _Color;
		#ifdef PIXELSNAP_ON
				OUT.vertex = UnityPixelSnap(OUT.vertex);
		#endif

				return OUT;
			}

			sampler2D _MainTex;
			sampler2D _AlphaTex;

			fixed4 SampleSpriteTexture(float2 uv)
			{
				fixed4 color = tex2D(_MainTex, uv);

		#if ETC1_EXTERNAL_ALPHA
				// get the color from an external texture (usecase: Alpha support for ETC1 on android)
				color.a = tex2D(_AlphaTex, uv).r;
		#endif //ETC1_EXTERNAL_ALPHA

				return color;
			}

			fixed4 frag(v2f IN) : SV_Target
			{
				fixed4 col = SampleSpriteTexture(IN.texcoord) * IN.color;
				
				int c = col.r * 16;

				     if (c == 0)  col.rgb = _Palette00.rgb;
				else if (c == 1)  col.rgb = _Palette01.rgb;
				else if (c == 2)  col.rgb = _Palette02.rgb;
				else if (c == 3)  col.rgb = _Palette03.rgb;
				else if (c == 4)  col.rgb = _Palette04.rgb;
				else if (c == 5)  col.rgb = _Palette05.rgb;
				else if (c == 6)  col.rgb = _Palette06.rgb;
				else if (c == 7)  col.rgb = _Palette07.rgb;
				else if (c == 9)  col.rgb = _Palette09.rgb;
				else if (c == 10) col.rgb = _Palette10.rgb;
				else if (c == 11) col.rgb = _Palette11.rgb;
				else if (c == 12) col.rgb = _Palette12.rgb;
				else if (c == 13) col.rgb = _Palette13.rgb;
				else if (c == 14) col.rgb = _Palette14.rgb;
				else if (c == 15) col.rgb = _Palette15.rgb;

				return col;
			}
			ENDCG
		}
    }
}

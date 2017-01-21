Shader "Unlit/PaletteSwap"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		[PerRendererData] _Cutout("Cutout", Range(0, 1)) = 1
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
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float _Cutout;

			float _Cycle;
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

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);
				
				int c = floor(col.a * 256) % 16;
				//half cut = col.g;

				if (c != 0) c = (c + floor(_Cycle)) % 16;

				if (c ==  0) col = _Palette00;
				if (c ==  1) col = _Palette01;
				if (c ==  2) col = _Palette02;
				if (c ==  3) col = _Palette03;
				if (c ==  4) col = _Palette04;
				if (c ==  5) col = _Palette05;
				if (c ==  6) col = _Palette06;
				if (c ==  7) col = _Palette07;
				if (c ==  8) col = _Palette08;
				if (c ==  9) col = _Palette09;
				if (c == 10) col = _Palette10;
				if (c == 11) col = _Palette11;
				if (c == 12) col = _Palette12;
				if (c == 13) col = _Palette13;
				if (c == 14) col = _Palette14;
				if (c == 15) col = _Palette15;

				//col.a *= step(cut, _Cutout);

				return col;
			}
			ENDCG
		}
	}
}

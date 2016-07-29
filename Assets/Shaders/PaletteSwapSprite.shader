Shader "Unlit/PaletteSwap"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
		LOD 100

		Cull Off
		Lighting Off
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
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
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);
			 
				int c = col.r * 15;

				if (c ==  0) return _Palette00;
				if (c ==  1) return _Palette01;
				if (c ==  2) return _Palette02;
				if (c ==  3) return _Palette03;
				if (c ==  4) return _Palette04;
				if (c ==  5) return _Palette05;
				if (c ==  6) return _Palette06;
				if (c ==  7) return _Palette07;
				if (c ==  8) return _Palette08;
				if (c ==  9) return _Palette09;
				if (c == 10) return _Palette10;
				if (c == 11) return _Palette11;
				if (c == 12) return _Palette12;
				if (c == 13) return _Palette13;
				if (c == 14) return _Palette14;
				if (c == 15) return _Palette15;

				return col;
			}
			ENDCG
		}
	}
}

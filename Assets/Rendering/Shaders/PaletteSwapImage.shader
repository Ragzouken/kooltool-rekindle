﻿Shader "UI/PaletteSwap"
{
	Properties
	{
		[PerRendererData] _MainTex ("Texture", 2D) = "white" {}

		_Color("Tint", Color) = (1,1,1,1)

		_StencilComp("Stencil Comparison", Float) = 8
		_Stencil("Stencil ID", Float) = 0
		_StencilOp("Stencil Operation", Float) = 0
		_StencilWriteMask("Stencil Write Mask", Float) = 255
		_StencilReadMask("Stencil Read Mask", Float) = 255

		_ColorMask("Color Mask", Float) = 15

		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Use Alpha Clip", Float) = 0
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

		Stencil
		{
			Ref[_Stencil]
			Comp[_StencilComp]
			Pass[_StencilOp]
			ReadMask[_StencilReadMask]
			WriteMask[_StencilWriteMask]
		}

		Cull Off
		Lighting Off
		ZWrite Off
		ZTest[unity_GUIZTestMode]
		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask[_ColorMask]

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

		fixed4 _Color;
		fixed4 _TextureSampleAdd;
		float4 _ClipRect;

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

		sampler2D _MainTex;

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

			fixed4 frag (v2f IN) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, IN.texcoord) ;
				//col += _TextureSampleAdd;
				col *= IN.color;

				int c = floor(col.a * 256) % 16;

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

				col.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);

#ifdef UNITY_UI_ALPHACLIP
				clip(col.a - 0.001);
#endif

				return col;
			}
			ENDCG
		}
	}
}

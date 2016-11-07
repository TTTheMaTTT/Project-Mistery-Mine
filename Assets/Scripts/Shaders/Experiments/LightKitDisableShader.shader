Shader "Experiments/LightKitDisableShader" {
	
	Properties
	{
		_MainTex("Base (RGB)", 2D) = "white" {}
	}
		SubShader
	{

		Tags{ "Queue" = "Transparent+1" }

		ZWrite Off
		ZTest Always
		Cull Off

		Pass
	{
		CGPROGRAM
#pragma fragmentoption ARB_precision_hint_fastest
#pragma vertex vert
#pragma fragment frag

#include "UnityCG.cginc"


	// uniforms
	sampler2D _MainTex;
	uniform float4 _MainTex_ST;
	struct vertexInput
	{
		float4 vertex : POSITION; // position (in object coordinates, i.e. local or model coordinates)
		float4 texcoord : TEXCOORD0;  // 0th set of texture coordinates (a.k.a. “UV”; between 0 and 1)
	};


	struct fragmentInput
	{
		float4 pos : SV_POSITION;
		float4 color : COLOR0;
		half2 uv : TEXCOORD0;
	};


	fragmentInput vert(vertexInput i)
	{
		fragmentInput o;
		o.pos = mul(UNITY_MATRIX_MVP, i.vertex);
		o.uv = TRANSFORM_TEX(i.texcoord, _MainTex);

		return o;
	}


	half4 frag(fragmentInput i) : COLOR
	{
		half4 black = (0, 0, 0, 0);

		return  black;
	}

		ENDCG
	}
	}

		FallBack "Diffuse"
}

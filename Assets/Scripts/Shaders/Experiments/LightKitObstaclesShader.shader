Shader "Experiments/LightKitObstaclesShader" 
{
	Properties {
		_MainTex("Base (RGB)", 2D) = "white" {}
		_Color ("Color", Color) = (1,1,1,1)
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
		fixed4 _Color;

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
			half4 main = tex2D(_MainTex, i.uv);

#if UNITY_UV_STARTS_AT_TOP
			i.uv.y = 1.0f - i.uv.y;
#endif

			half4 black = (0, 0, 0, 0);

			float nd = step(0.01, main.a);

			return  (1-nd)*black+nd*_Color;
		}

			ENDCG
		} 
		} 

			FallBack "Diffuse"
}

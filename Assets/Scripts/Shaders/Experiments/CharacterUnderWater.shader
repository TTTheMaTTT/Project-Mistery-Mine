Shader "Experiments/CharacterUnderWater"
{
	Properties
	{
		_MainTex("Base (RGB) Trans (A)", 2D) = "white" {}
		_WaterColour("Water Colour", Color) = (1,1,1,1)
		_UnderWaterColour("Under Water Colour ",Color)=(1,1,1,1)
	}

		SubShader
	{
		Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Opaque" }
		ZWrite On Lighting Off Cull Off Fog{ Mode Off } Blend SrcAlpha OneMinusSrcAlpha

		GrabPass{ "_GrabTexture" }

		Pass
	{
		CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"

		sampler2D _GrabTexture;

	sampler2D _MainTex;
	fixed4 _WaterColour;
	fixed4 _UnderWaterColour;

	struct vin_vct
	{
		float4 vertex : POSITION;
		float4 color : COLOR;
		float2 texcoord : TEXCOORD0;
	};

	struct v2f_vct
	{
		float4 vertex : POSITION;
		fixed4 color : COLOR;
		float2 texcoord : TEXCOORD0;

		float4 uvgrab : TEXCOORD1;
	};

	// Vertex function 
	v2f_vct vert(vin_vct v)
	{
		v2f_vct o;
		o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
		o.color = v.color;

		o.texcoord = v.texcoord;

		o.uvgrab = ComputeGrabScreenPos(o.vertex);
		return o;
	}

	// Fragment function
	half4 frag(v2f_vct i) : COLOR
	{
		half4 mainColour = tex2D(_MainTex, i.texcoord);

		fixed4 col = tex2Dproj(_GrabTexture, UNITY_PROJ_COORD(i.uvgrab));
		float dN = //step(_WaterColour.r, col.r)*step(col.r, _WaterColour.r)*
			       //step(_WaterColour.g, col.g)*step(col.g, _WaterColour.g)*
			       step(_WaterColour.b, col.b)*step(col.b, _WaterColour.b+.02)*step(0.1,mainColour.a)	;
		return mainColour*(1 - dN) + dN*_UnderWaterColour;
	}

		ENDCG
	}
	}
}

Shader "Experiments/Blue" {
	SubShader{
		Tags{ "RenderType" = "Opaque" "Queue" = "Geometry+2" }
		
		Pass{

		CGPROGRAM
#include "UnityCG.cginc"
#pragma vertex vert
#pragma fragment frag
	struct appdata {
		float4 vertex : POSITION;
	};
	struct v2f {
		float4 pos : SV_POSITION;
	};
	v2f vert(appdata v) {
		v2f o;
		o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
		return o;
	}
	half4 frag(v2f i) : SV_Target{
		return half4(0,1,1,1);
	}
		ENDCG
	}

		Pass{
		Stencil{
		Ref 1
		Comp equal
	}

		CGPROGRAM
#include "UnityCG.cginc"
#pragma vertex vert
#pragma fragment frag
	struct appdata {
		float4 vertex : POSITION;
	};
	struct v2f {
		float4 pos : SV_POSITION;
	};
	v2f vert(appdata v) {
		v2f o;
		o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
		return o;
	}
	half4 frag(v2f i) : SV_Target{
		return half4(0,0,1,1);
	}
		ENDCG
	}

	}
}
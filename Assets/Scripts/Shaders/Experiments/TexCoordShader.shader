Shader "Experiments/TexCoordShader" {
	Properties {
		_MainTex ("Main Texture", 2D) = "white" {}
		_TexCoordX("Texture Coordinate X", Range(0.0,1.0)) = 1.0
		_TexCoordY("Texture Coordinate Y", Range(0.0,1.0))=1.0
	}
		SubShader{
			Tags { "RenderType" = "Opaque" }
			Cull Off
			LOD 200

				Pass{

			CGPROGRAM
	#pragma vertex vert
	#pragma fragment frag

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

		v2f vert(appdata_t IN)
		{
			v2f OUT;
			OUT.vertex = mul(UNITY_MATRIX_MVP, IN.vertex);
			OUT.texcoord = IN.texcoord;
			OUT.color = IN.color;

			return OUT;
		}

		sampler2D _MainTex;
		half _TexCoordX;
		half _TexCoordY;

		fixed4 frag(v2f IN) : SV_Target
		{
			float2 nTexCoord=(_TexCoordX,_TexCoordY);
			fixed4 c = tex2D(_MainTex, nTexCoord);
			c.rgb *= c.a;
			return c;
		}

			ENDCG
		}
		}

}

Shader "MysteryMine/LineSpriteShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Tiling("Tiling", Float) = 1
		_Length("Length",Float) = 1
	}	
	SubShader
		{

			Pass{
			Tags{ "Queue" = "Transparent+1" "RenderType" = "Opaque" }

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
				float3 wPos : TEXCOORD1;	// World position
			};

			float _Tiling;
			float _Length;

			v2f vert(appdata_t IN)
			{
				v2f OUT;
				OUT.vertex = mul(UNITY_MATRIX_MVP, IN.vertex);
				OUT.texcoord = IN.texcoord;
				OUT.color = IN.color;
				OUT.wPos= mul(unity_ObjectToWorld, IN.vertex).xyz;

				return OUT;
			}

			sampler2D _MainTex;

			fixed4 frag(v2f IN) : SV_Target
			{
				float2 nTexcoord = (0,fmod(IN.texcoord.x*_Tiling,1));
				fixed4 c = tex2D(_MainTex, nTexcoord) * IN.color;
				c.rgb *= c.a;
				return c;
			}

			ENDCG
		}
		}
}

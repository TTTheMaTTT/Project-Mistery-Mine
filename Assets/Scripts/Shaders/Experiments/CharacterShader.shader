Shader "Experiments/CharacterShader" // Шейдер, предназначенный для корректной отрисовки персонажей
{
	Properties
	{
		[PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
		_Color("Tint", Color) = (1,1,1,1)
		[MaterialToggle] PixelSnap("Pixel snap", Float) = 0

		_SilhouetteColor("Silhouette Color", Color) = (1,1,1,1)
	}

	SubShader
	{
		Tags {"Queue" = "Transparent+1" "RenderType" = "Opaque"}

		Pass
		{

			Name "BASE"
			//ZWrite Off
			//ZTest LEqual
			Cull Off
			Blend SrcAlpha OneMinusSrcAlpha
			Lighting On

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

		fixed4 _Color;

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

		fixed4 frag(v2f IN) : SV_Target
		{
			fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;

		c.rgb *= c.a;

		return c;
		}
			ENDCG
		}

			Pass
		{
		Name "Silhouette"
		Stencil
		{
			Ref 1
			Comp equal
			Pass keep
		}
			Cull Off
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
#pragma vertex vert
#pragma fragment frag

			sampler2D _MainTex;

		struct appdata {
			float4 vertex : POSITION;
			float2 texcoord : TEXCOORD0;
		};

		struct v2f
		{
			float4 vertex   : SV_POSITION;
			float2 texcoord  : TEXCOORD0;
		};

		fixed4 _SilhouetteColor;

		v2f vert(appdata v) 
		{
			v2f o;
			o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
			o.texcoord = v.texcoord;
			return o;
		}

		fixed4 frag(v2f IN) : SV_Target
		{
			fixed4 c = tex2D(_MainTex, IN.texcoord)*_SilhouetteColor;

			//float nd = step(0.01, c.a);
			//c.rgba = c.rgba*(1-nd)+nd*(1, 1, 1, 1)*_SilhouetteColor;
			c.rgb *= c.a;

			return c;
		}
			ENDCG
		}

	}

}

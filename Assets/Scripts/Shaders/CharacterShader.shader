Shader "MysteryMine/CharacterShader" // Шейдер, предназначенный для корректной отрисовки персонажей
{
	Properties
	{
		[PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
		//[PerRendererData]_Color1("Color 1", Color) = (1,1,1,1)
		//[PerRendererData]_Color2("Color 2", Color) = (1,1,1,1)
		//[PerRendererData]_ColorCoof1("Color Coefficient 1",Float) = 0.5
		//[PerRendererData]_ColorCoof2("Color Coefficient 2",Float) = 0.5
		[PerRendererData]_MixedColor ("Mixed Color",Color) = (1,1,1,1)
		[PerRendererData]_SilhouetteMixedColor("Silhouette Mixed Color", Color)=(0,0,1,1)
		[MaterialToggle] PixelSnap("Pixel snap", Float) = 0

		// Параметры, определяющие цвет контура, и сам факт отрисовки контура
		[PerRendererData] _Outline("Outline", Float) = 0
		[PerRendererData] _OutlineColor("Outline Color", Color) = (0,1,0,1)

		//_SilhouetteColor("Silhouette Color", Color) = (1,1,1,1)
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

		fixed4 _MixedColor;
		float _Outline;
		fixed4 _OutlineColor;

		v2f vert(appdata_t IN)
		{
			v2f OUT;
			OUT.vertex = mul(UNITY_MATRIX_MVP, IN.vertex);
			OUT.texcoord = IN.texcoord;
			OUT.color = IN.color*_MixedColor;
#ifdef PIXELSNAP_ON
			OUT.vertex = UnityPixelSnap(OUT.vertex);
#endif

			return OUT;
		}

		sampler2D _MainTex;
		float4 _MainTex_TexelSize;

		fixed4 frag(v2f IN) : SV_Target
		{
			fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;

			// Если включена опция "Outline", то разукрашиваем края
			if (_Outline > 0 && c.a != 0) {
				// Рассматриваем 4 соседних пикселя
				fixed4 pixelUp = tex2D(_MainTex, IN.texcoord + fixed2(0, _MainTex_TexelSize.y / 3));
				fixed4 pixelDown = tex2D(_MainTex, IN.texcoord - fixed2(0, _MainTex_TexelSize.y / 3));
				fixed4 pixelRight = tex2D(_MainTex, IN.texcoord + fixed2(_MainTex_TexelSize.x / 3, 0));
				fixed4 pixelLeft = tex2D(_MainTex, IN.texcoord - fixed2(_MainTex_TexelSize.x / 3, 0));

				// Если хотя бы один из соседей прозрачен - значит разукрашиваем данный пиксель в цвт контура
				if (pixelUp.a * pixelDown.a * pixelRight.a * pixelLeft.a == 0) {
					c.rgba = fixed4(1, 1, 1, 1) * _OutlineColor;
				}
			}

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
			//ZWrite On
			//ZTest Always
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
#pragma vertex vert
#pragma fragment frag

		sampler2D _MainTex;
		float4 _MainTex_TexelSize;
		fixed4 _Color;

		struct appdata 
		{
			float4 vertex : POSITION;
			float2 texcoord : TEXCOORD0;
			float4 color    : COLOR;
		};

		struct v2f
		{
			float4 vertex   : SV_POSITION;
			float2 texcoord  : TEXCOORD0;
			fixed4 color    : COLOR;
		};

		//fixed4 _SilhouetteColor;
		fixed4 _SilhouetteMixedColor;
		float _Outline;
		fixed4 _OutlineColor;

		v2f vert(appdata v) 
		{
			v2f o;
			o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
			o.texcoord = v.texcoord;
			o.color = v.color*_SilhouetteMixedColor;
			return o;
		}

		fixed4 frag(v2f IN) : SV_Target
		{
			fixed4 c = tex2D(_MainTex, IN.texcoord)*IN.color;

			//float nd = step(0.01, c.a);
			//c.rgba = c.rgba*(1-nd)+nd*(1, 1, 1, 1)*_SilhouetteColor;
			// Если включена опция "Outline", то разукрашиваем края
			if (_Outline > 0 && c.a != 0) 
			{
				// Рассматриваем 4 соседних пикселя
				fixed4 pixelUp = tex2D(_MainTex, IN.texcoord + fixed2(0, _MainTex_TexelSize.y / 3));
				fixed4 pixelDown = tex2D(_MainTex, IN.texcoord - fixed2(0, _MainTex_TexelSize.y / 3));
				fixed4 pixelRight = tex2D(_MainTex, IN.texcoord + fixed2(_MainTex_TexelSize.x / 3, 0));
				fixed4 pixelLeft = tex2D(_MainTex, IN.texcoord - fixed2(_MainTex_TexelSize.x / 3, 0));

				// Если хотя бы один из соседей прозрачен - значит разукрашиваем данный пиксель в цвт контура
				if (pixelUp.a * pixelDown.a * pixelRight.a * pixelLeft.a == 0) {
					c.rgba = fixed4(1, 1, 1, 1) * _OutlineColor;
				}
			}
			
			c.rgb *= c.a;

			return c;
		}
			ENDCG
		}

	}

}

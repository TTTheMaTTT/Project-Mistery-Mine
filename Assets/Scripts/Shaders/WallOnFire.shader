// Шейдер, осуществляющее попиксельное искажённие изображения, которое имитирует искажение вида за костром.

Shader "MysteryMine/WallOnFire" {
	Properties
	{
		_DistAmt("Distortion", range(0,256)) = 10 //Амплитуда искажения
		_TintAmt("Tint Power",range(0,1))=.5//Насколько сильно подкрашивается под текстуру _MainTex
		_MainTex("Tint Color (RGB)", 2D) = "white" {} //Оттенок искажений (обычная текстура, накладывающаяся поверх искажённого изображения)
		_NoiseTex("NoiseTex", 2D) = "white" {} //Величина искажений в конкретной координате задаётся картой нормалей
	}

		CGINCLUDE
#pragma fragmentoption ARB_precision_hint_fastest
#pragma fragmentoption ARB_fog_exp2
#include "UnityCG.cginc"

	sampler2D _GrabTexture : register(s0);
	float4 _GrabTexture_TexelSize;
	sampler2D _NoiseTex : register(s1);
	sampler2D _MainTex : register(s2);

	struct v2f {
		float4 vertex : POSITION;
		float4 uvgrab : TEXCOORD0;
		float2 uvmain : TEXCOORD2;
	};

	uniform float _DistAmt;
	uniform float _TintAmt;

	half4 frag(v2f i) : COLOR
	{
		// Рассчёт искажений
		float2 displacedTexCoord = i.uvgrab.xy + float2(tex2D(_NoiseTex, i.uvgrab.xy / 300 + float2((_Time.w % 50) / 50, 0)).z - .5,
													 tex2D(_NoiseTex, i.uvgrab.xy / 300 + float2(0, (_Time.w % 50) / 50)).z - .5) * _DistAmt / 20;

		half4 col = tex2D(_GrabTexture, displacedTexCoord);
		half4 tint = tex2D(_MainTex, i.uvmain);
		return (tint*_TintAmt + half4(1,1,1,1)*(1-_TintAmt)) * col;
	}
		ENDCG

		Category {

		// Отрисовка происходит в последнюю очередь, когда неискажённое изображение уже отрисованно
		Tags{ "Queue" = "Transparent+100" "RenderType" = "Opaque" }


			SubShader{

			// В этом проходе заносится в отдельную текстуру изоdбражение за отрисовываевым объектом
			GrabPass{
			Name "BASE"
			Tags{ "LightMode" = "Always" }
		}

			//В этом проходе уже накладываются искажения на изображение, полученном в предыдущем проходе
			Pass{
			Name "BASE"
			Tags{ "LightMode" = "Always" }

			CGPROGRAM
#pragma vertex vert
#pragma fragment frag

		struct appdata_t {
			float4 vertex : POSITION;
			float2 texcoord: TEXCOORD0;
		};

		v2f vert(appdata_t v)
		{
			v2f o;
			o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
#if UNITY_UV_STARTS_AT_TOP
			float scale = -1.0;
#else
			float scale = 1.0;
#endif
			o.uvgrab.xy = (float2(o.vertex.x, o.vertex.y*scale) + o.vertex.w) * 0.5;
			o.uvgrab.zw = o.vertex.zw;
			o.uvmain = MultiplyUV(UNITY_MATRIX_TEXTURE2, v.texcoord);
			return o;
		}
		ENDCG
		}
		}

			SubShader{
			Blend DstColor Zero
			Pass{
			Name "BASE"
			SetTexture[_MainTex]{ combine texture }
		}
		}
	}

}

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "MysteryMine/WaterShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}//Текстура воды
		_LightColour("LightColour", Color) = (1,1,1,1)//Цвет просвета

		_CosAngle("CosAngle",Range(-1,1)) = 0.0//Косинус угла наклона просветов
		_T("Period",Float) = 2.0//Период появления и исчезновения просветов
		_L("MaxDistance",Float) = 1

		 _t1("t1", Float) = 0.0//начальное время
		_x1("x1", Float) = 0.1//Позиция
		_l1("l1",Float)=.15//Ширина

		_t2("t2", Float) = 0.0
		_x2("x2", Float) = 0.1
		_l2("l2",Float) = .15

		_t3("t3", Float) = 0.0
		_x3("x3", Float) = 0.1
		_l3("l3",Float) = .15

		_t4("t4", Float) = 0.0
		_x4("x4", Float) = 0.1
		_l4("l4",Float) = .15
	}
	SubShader
	{
	
		Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True"}

		Pass
		{
			Stencil
		{
			Ref 1
			Comp always
			Pass replace
		}

			CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"

		sampler2D _MainTex;
		fixed4 _LightColour;

		float _CosAngle;
		float _T;
		float _L;

		float _t1;
		float _x1;
		float _l1;

		float _t2;
		float _x2;
		float _l2;

		float _t3;
		float _x3;
		float _l3;

		float _t4;
		float _x4;
		float _l4;

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
			float3 wPos : TEXCOORD1;	// World position
		};

		// Vertex function 
		v2f_vct vert(vin_vct v)
		{
			v2f_vct o;
			o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
			o.color = v.color;

			o.texcoord = v.texcoord;
			o.wPos= mul(unity_ObjectToWorld, v.vertex).xyz;

			return o;
		}

		float sinusoid(float t)
		{
			float2 c = 3.1415;
			return (1.0 + sin(t / _T * 2 * c)) / 2.0;
		}

		// Fragment function
		half4 frag(v2f_vct i) : COLOR
		{
			half4 mainColour = tex2D(_MainTex, i.texcoord);

			float nx = step(i.wPos.x - i.wPos.y*_CosAngle, 0);
			float x = (1-nx)*fmod(i.wPos.x - i.wPos.y*_CosAngle,_L)+nx*(_L-fmod(-i.wPos.x + i.wPos.y*_CosAngle, _L));
			float time = _Time[1];

			float nd1 = step(x, _x1 + _l1)*step(_x1 - _l1/2.0, x);
			float nd2 = step(x, _x2 + _l2)*step(_x2 - _l2/2.0, x);
			float nd3 = step(x, _x3 + _l3)*step(_x3 - _l3/2.0, x);
			float nd4 = step(x, _x4 + _l4)*step(_x4 - _l4/2.0, x);

			float koof = saturate(nd1*sinusoid(time+_t1)+ nd2*sinusoid(time + _t2) + nd3*sinusoid(time + _t3) + nd4*sinusoid(time + _t4));

			return mainColour * (1.0-koof) + _LightColour*koof;
		}

			ENDCG
		}

	}
}

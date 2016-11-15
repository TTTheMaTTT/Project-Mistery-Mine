Shader "MysteryMine/Sprite Light Kit/Sprite Light"
{
	Properties
	{
		_MainTex ( "Sprite Texture", 2D ) = "white" {}
		//_ObstacleTex("Obstacle Texture", 2D) = "white"{}
		_Color("Tint", Color) = (1, 1, 1, 1)
		[MaterialToggle] PixelSnap("Pixel snap", Float) = 0
		[Enum(UnityEngine.Rendering.BlendMode)] _BlendSrc("Blend Source", Float) = 5
		[Enum(UnityEngine.Rendering.BlendMode)] _BlendDst("Blend Destination", Float) = 1
		//_Scale("Scale", Float)=1
		//_ObstacleMul("Obstacle Mul", Float) = 500
		_UVXOffset("UV X Offset", float) = 0			//Receive input from UV coordinate X offset
		_UVYOffset("UV Y Offset", float) = 0
		_EmissionColorMul("Emission color mul", Float) = 1
		//_LightPositionX("Light Position X",Range(0,1))=1
		//_LightPositionY("Light Position Y",Range(0,1)) = 1
	}

	SubShader
	{
		Tags
		{ 
			"Queue"="Transparent" 
			"IgnoreProjector"="True" 
			"RenderType"="Transparent" 
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
		}

		Cull Off
		Lighting Off
		ZWrite Off

		Blend [_BlendSrc] [_BlendDst]

		Pass
		{
CGPROGRAM

#pragma vertex vert
#pragma fragment frag
#pragma multi_compile _ PIXELSNAP_ON
#pragma glsl_no_auto_normalization
#include "UnityCG.cginc"

	uniform float _UVXOffset;
	uniform float _UVYOffset;

struct appdata_t
{
	float4 vertex   : POSITION;
	float4 color    : COLOR;
	float2 texcoord : TEXCOORD0;
	//fixed4 normal : TEXCOORD1;
};

struct v2f
{
	float4 vertex   : SV_POSITION;
	fixed4 color    : COLOR;
	half2 texcoord  : TEXCOORD0;
	//half4 scrPos : TEXCOORD2;
	//half4 scrPosCenter : TEXCOORD1;
};


fixed4 _Color;
//float _Scale;
sampler2D _MainTex;
//sampler2D _ObstacleTex;
//half _ObstacleMul;
half _EmissionColorMul;

//half _LightPositionX, _LightPositionY;

v2f vert( appdata_t IN )
{
	v2f OUT;
	OUT.vertex = mul( UNITY_MATRIX_MVP, IN.vertex );
	OUT.texcoord = IN.texcoord;
	OUT.texcoord = half2((IN.texcoord.x + _UVXOffset), (IN.texcoord.y + _UVYOffset));
	OUT.color = IN.color * _Color;
	#ifdef PIXELSNAP_ON
	OUT.vertex = UnityPixelSnap( OUT.vertex );
	#endif

	//OUT.scrPos = ComputeScreenPos(OUT.vertex);
	//OUT.scrPosCenter = IN.normal;

	return OUT;
}


fixed4 frag( v2f IN ) : SV_Target
{
	//fixed4 c = tex2D( _MainTex, IN.texcoord ) * IN.color*_Scale;
	//c.rgb *= c.a;

	//fixed2 thisPos = (IN.scrPos.xy / IN.scrPos.w);
	//fixed2 centerPos = (_LightPositionX, _LightPositionY);
	//const fixed sub = 1;

	//fixed m = _ObstacleMul*length((thisPos-centerPos)*fixed2(_ScreenParams.x / _ScreenParams.y, 1)*sub);
	//fixed m = 1;

	fixed4 tex = tex2D(_MainTex, IN.texcoord);

	//clip(tex.a - 0.005);

	fixed4 col = IN.color*fixed4(tex.rgb, 1)*tex.a;
	
	/*
	col *= saturate(1 - tex2D(_ObstacleTex, thisPos).a*m);

	
	fixed pos = 1;


	pos -= sub; col *= saturate(1 - tex2D(_ObstacleTex, lerp(centerPos, thisPos, pos))*m);
	pos -= sub; col *= saturate(1 - tex2D(_ObstacleTex, lerp(centerPos, thisPos, pos))*m);
	pos -= sub; col *= saturate(1 - tex2D(_ObstacleTex, lerp(centerPos, thisPos, pos))*m);
	pos -= sub; col *= saturate(1 - tex2D(_ObstacleTex, lerp(centerPos, thisPos, pos))*m);
	pos -= sub; col *= saturate(1 - tex2D(_ObstacleTex, lerp(centerPos, thisPos, pos))*m);

	pos -= sub; col *= saturate(1 - tex2D(_ObstacleTex, lerp(centerPos, thisPos, pos))*m);
	pos -= sub; col *= saturate(1 - tex2D(_ObstacleTex, lerp(centerPos, thisPos, pos))*m);
	pos -= sub; col *= saturate(1 - tex2D(_ObstacleTex, lerp(centerPos, thisPos, pos))*m);
	pos -= sub; col *= saturate(1 - tex2D(_ObstacleTex, lerp(centerPos, thisPos, pos))*m);*/

	col.rgb *= _EmissionColorMul;		

	return col;
}

ENDCG
		}
	}
}

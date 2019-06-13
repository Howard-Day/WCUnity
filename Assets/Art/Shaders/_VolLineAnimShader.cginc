// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'


	
	#include "UnityCG.cginc"
	#include "RetroAA.cginc"

	float _LineWidth;
	float _LineScale;
	float _pixelsPerUnit;
	float _animSpeed;
	float _animTiles;

	float4 AlignToPixelGrid(float4 vertex)
	{
		float4 worldPos = mul(unity_ObjectToWorld, vertex);

		worldPos.x = floor(worldPos.x * _pixelsPerUnit + 0.5) / _pixelsPerUnit;
		worldPos.y = floor(worldPos.y * _pixelsPerUnit + 0.5) / _pixelsPerUnit;

		return mul(unity_WorldToObject, worldPos);
	}

	struct a2v
	{
		float4 vertex : POSITION;
		float3 otherPos : NORMAL; // object-space position of the other end
		half2 texcoord : TEXCOORD0;
		float2 texcoord1 : TEXCOORD1;
	};
	
	struct v2f
	{
		float4 pos : SV_POSITION;
		half2 uv : TEXCOORD0;
	};
	
	v2f vert (a2v v)
	{
		v2f o;
		//float rand = mul (unity_ObjectToWorld, v.vertex).x; 
		float animY = (floor(frac(_Time*-_animSpeed)*_animTiles)/_animTiles) + (v.texcoord.y/_animTiles);
		o.uv = float2(v.texcoord.x,animY);
		
#if UNITY_VERSION >= 540
		float4 clipPos = UnityObjectToClipPos(v.vertex);
		float4 clipPos_other = UnityObjectToClipPos(float4(v.otherPos, 1.0));
#else
		float4 clipPos = UnityObjectToClipPos(v.vertex);
		float4 clipPos_other = UnityObjectToClipPos(float4(v.otherPos, 1.0));
#endif

		float aspectRatio = _ScreenParams.x / _ScreenParams.y;
		float invAspectRatio = _ScreenParams.y / _ScreenParams.x;

		float2 ssPos = float2(clipPos.x * aspectRatio, clipPos.y);
		float2 ssPos_other = float2(clipPos_other.x * aspectRatio, clipPos_other.y);

		float scaledLineWidth = _LineWidth * _LineScale;

#ifndef FOV_SCALING_OFF
		float t = unity_CameraProjection._m11;
		float fov = atan(1.0f / t) * 114.59155902616464175359630962821; // = 2 * 180 / UNITY_PI = 2 * rad2deg
		scaledLineWidth = scaledLineWidth * 60.0 / fov;
#endif
		
		// screen-space offset vector:
		float2 lineDirProj = scaledLineWidth * normalize(
			ssPos.xy/clipPos.w - // screen-space pos of current end
			ssPos_other.xy/clipPos_other.w // screen-space position of the other end
		) * sign(clipPos.w) * sign(clipPos_other.w);
		
		float2 offset =
			v.texcoord1.x * lineDirProj +
			v.texcoord1.y * float2(lineDirProj.y, -lineDirProj.x)
		;
		
		clipPos.x += offset.x / aspectRatio;
		clipPos.y += offset.y;

	    float4 alignedPos = AlignToPixelGrid(clipPos);
		o.pos = alignedPos;//UnityObjectToClipPos(alignedPos);

		return o;
	}
	
#if !defined(VOL_LINE_SHDMODE_FAST)
	fixed _LightSaberFactor;
	fixed4 _Color;
#endif
	sampler2D _MainTex;
	float4 _MainTex_ST;
	float4 _MainTex_TexelSize;
	fixed4 frag(v2f i) : SV_Target
	{
		fixed4 tx = RetroAA(_MainTex, i.uv, _MainTex_TexelSize);//tex2D(_MainTex, i.uv);
		
		return tx;

	}


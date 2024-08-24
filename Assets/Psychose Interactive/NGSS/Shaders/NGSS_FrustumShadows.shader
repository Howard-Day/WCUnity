Shader "Hidden/NGSS_FrustumShadows"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	}
	
	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		CGINCLUDE

		#pragma vertex vert
		#pragma fragment frag
		//#pragma exclude_renderers gles d3d9		
		#pragma target 3.0

		#include "UnityCG.cginc"
		half4 _MainTex_ST;
	/*
#if !defined(UNITY_SINGLE_PASS_STEREO)
#define UnityStereoTransformScreenSpaceTex(uv) (uv)
#endif*/
		struct appdata
		{
			float4 vertex : POSITION;
			float2 uv : TEXCOORD0;
			UNITY_VERTEX_INPUT_INSTANCE_ID //Insert
		};

		struct v2f
		{
			float4 vertex : SV_POSITION;
			float2 uv : TEXCOORD0;
			//float2 uv2 : TEXCOORD0;
			//float4 screenPos : TEXCOORD1;
			UNITY_VERTEX_INPUT_INSTANCE_ID
			UNITY_VERTEX_OUTPUT_STEREO
		};

		//float2 _Jitter_Offset;

		v2f vert(appdata v)
		{
			v2f o;

			UNITY_SETUP_INSTANCE_ID(v); //Insert
		    UNITY_INITIALIZE_OUTPUT(v2f, o); //Insert
		    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); //Insert

		    o.vertex = UnityObjectToClipPos(v.vertex);
			//o.vertex.xy += _Jitter_Offset / _ScreenParams.xy;
			//o.uv = v.uv;//NGSS 2.0
			//o.uv = ComputeNonStereoScreenPos(o.vertex).xy;//NGSS 2.0
			o.uv.xy = v.uv;
			
			// #if UNITY_SINGLE_PASS_STEREO
		 //    // If Single-Pass Stereo mode is active, transform the
		 //    // coordinates to get the correct output UV for the current eye.
		 //    float4 scaleOffset = unity_StereoScaleOffset[unity_StereoEyeIndex];
		 //    o.uv = (o.uv - scaleOffset.zw) / scaleOffset.xy;
		 //    #endif

			//o.uv = UnityStereoTransformScreenSpaceTex(v.uv);

			#if UNITY_UV_STARTS_AT_TOP
			//o.uv2 = UnityStereoTransformScreenSpaceTex(v.uv);
			if (_MainTex_ST.y < 0.0)
				o.uv.y = 1.0 - o.uv.y;
			#endif
			
			//o.uv += _Jitter_Offset;

			//o.screenPos = ComputeScreenPos(o.vertex);

			return o;
		}

		ENDCG

	Pass // render screen space rt shadows
	{
		CGPROGRAM
		#pragma multi_compile_local _ NGSS_USE_DITHERING
		//#pragma multi_compile_local _ NGSS_RAY_SCREEN_SCALE
		//#pragma multi_compile_local _ NGSS_USE_LOCAL_SHADOWS
		#pragma multi_compile_local _ NGSS_DEFERRED_OPTIMIZATION
		#pragma multi_compile_local _ SHADOWS_SPLIT_SPHERES

		UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);//_MainTex
		float4 _CameraDepthTexture_TexelSize;//_MainTex_TexelSize
		UNITY_DECLARE_SCREENSPACE_TEXTURE(_CameraGBufferTexture2);

		//float4x4 unity_CameraInvProjection;
		//float4x4 InverseView;//camera to world
		//float4x4 InverseViewProj;//clip to world
		//float4x4 InverseProj;//clip to camera			
		float4x4 WorldToView;//world to camera

		float3 LightDir;
		float3 LightDirWorld;
		float4 LightPosRange;//XYZ = world position, W = range * range
		half ShadowsDistanceStart;
		half RaySamples;
		half RayThickness;
		half RayScale;
		half RayScreenScale;
		half TemporalJitter;
		//float ShadowsFade;
		half ShadowsBias;
		//float RaySamplesScale;
		half BackfaceOpacity;

		#define ditherPattern float4x4(0.0,0.5,0.125,0.625, 0.75,0.22,0.875,0.375, 0.1875,0.6875,0.0625,0.5625, 0.9375,0.4375,0.8125,0.3125)
		
		fixed4 frag(v2f input) : SV_Target
		{
			UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input); //Insert
			
			float2 coord = input.uv;
						
			float shadow = 1.0;

			#if defined(NGSS_DEFERRED_OPTIMIZATION)
				float3 worldNormal = normalize(UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CameraGBufferTexture2, UnityStereoTransformScreenSpaceTex(coord.xy)).xyz * 2 - 1);
				#if !defined(NGSS_USE_LOCAL_SHADOWS)
					//if (dot(mul(UNITY_MATRIX_V, float4(worldNormal.xyz, 0.0)), LightDir * float3(1.0, 1.0, -1.0)) < 0.0) { return BackfaceOpacity; }//WorldToView
					if (dot(worldNormal, LightDirWorld.xyz) <= 0.0) { return BackfaceOpacity; }					
				#endif
			#endif

			float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, UnityStereoTransformScreenSpaceTex(coord.xy)).r;
			float linearDepth = LinearEyeDepth(depth);//same as Linear01Depth(depth) * _ProjectionParams.z;

			#if !defined(SHADOWS_SPLIT_SPHERES)
			if (linearDepth < ShadowsDistanceStart) return 1.0;
			#endif

			#if defined(UNITY_REVERSED_Z)
				depth = 1.0 - depth;
			#endif
			
			//clip(depth - 0.5);
			//if(UNITY_Z_0_FAR_FROM_CLIPSPACE(depth) < 0.25) return 1.0;

			coord.xy += TemporalJitter;
			
			float4 clipPos = float4(float3(UnityStereoTransformScreenSpaceTex(coord.xy), depth) * 2.0 - 1.0, 1.0);

			float4 viewPos = mul(unity_CameraInvProjection, clipPos);//go from clip to view space | InverseProj			

			viewPos.xyz /= viewPos.w;
			//viewPos.z *= -1;
			
			float4 wpos = mul(unity_CameraToWorld, float4(viewPos.xyz,1));

			#if defined(SHADOWS_SPLIT_SPHERES)
				float3 wdir = wpos - _WorldSpaceCameraPos;
			    half z = length(wdir);
				if (z < ShadowsDistanceStart) return 1.0;
			#endif
			
			#if defined(NGSS_USE_LOCAL_SHADOWS)				
				float3 lightDirection = LightPosRange.xyz - wpos.xyz;
				if (dot(lightDirection, lightDirection) > LightPosRange.w) { return 1.0; }
			#endif
			
			#if defined(NGSS_DEFERRED_OPTIMIZATION)
				#if defined(NGSS_USE_LOCAL_SHADOWS)					
					if (dot(worldNormal.xyz, normalize(lightDirection)) < 0.0) { return BackfaceOpacity; }
				#endif
			#else
				/*float3 worldNormal = normalize(cross(ddx(wpos.xyz), ddy(wpos.xyz)));//compute world normal from world position using ddx and ddy
				#if defined(NGSS_USE_LOCAL_SHADOWS)					
					float3 lightDirection = normalize(LightPosRange.xyz - wpos.xyz);
				#else
					float3 lightDirection = LightDirWorld;
				#endif
				if (dot(worldNormal, lightDirection) <= 0.0) return  BackfaceOpacity;*/
			#endif

			float samplers = RaySamples;//lerp(RaySamples / -viewPos.z, RaySamples, RaySamplesScale);//reduce samplers over distance
			
			#if defined(NGSS_USE_LOCAL_SHADOWS)
				//float3 rayDir = normalize(mul(WorldToView, float4(LightPosRange.xyz - wpos.xyz, 0.0)).xyz);//W == 1.0 treat as position | W == 0.0 treat as direction
				float3 rayDir = normalize(mul(WorldToView, float4(LightPosRange.xyz, 1.0)) - viewPos).xyz;
			#else
				float3 rayDir = LightDir * float3(1.0, 1.0, -1.0);//step size and length;
			#endif

			//#if defined(NGSS_RAY_SCREEN_SCALE)
			//rayDir = rayDir / samplers * -viewPos.z * RayScale;
			//#else
			//rayDir = rayDir * (RayScale / samplers);//fixed length
			//#endif

			rayDir = lerp(rayDir * (RayScale / samplers), rayDir / samplers * -viewPos.z * RayScale, RayScreenScale);

			#if defined(NGSS_USE_DITHERING)				
				//float3 rayPos = viewPos.xyz + rayDir * saturate(frac(sin(dot(coord.xy, float2(12.9898, 78.223))) * 43758.5453));
				float3 rayPos = viewPos.xyz + rayDir * ditherPattern[coord.x * _ScreenParams.x % 4][coord.y * _ScreenParams.y % 4];
				//rayDir = normalize(rayDir * ditherPattern[input.uv.x * _ScreenParams.x % 4][input.uv.y * _ScreenParams.y % 4]) / samplers * -viewPos.z * RayScale;			
			#else
				float3 rayPos = viewPos.xyz;
			#endif

			float bias = (viewPos.z * ShadowsBias);

			//rayDir.xy /= float2(1.0 / _ScreenParams.z, 1.0 / _ScreenParams.w);

			for (float i = 0; i < samplers; i++)
			{
				rayPos += rayDir;

				float4 rayPosProj = mul(unity_CameraProjection, float4(rayPos.xyz, 0.0));
				rayPosProj.xy = rayPosProj.xy / rayPosProj.w * 0.5 + 0.5;//0-1 
				//rayPosProj.xy = (rayPosProj.xy / rayPosProj.w + 1.0) / 2.0;

				if (rayPosProj.x < 0.001 || rayPosProj.x > 0.999 || rayPosProj.y < 0.001 || rayPosProj.y > 0.999) { return 1.0; }

				float lDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, UnityStereoTransformScreenSpaceTex(float4(rayPosProj.xy, 0, 0))).r);
				//float lDepth = LinearEyeDepth(tex2Dlod(_CameraDepthTexture, float4(UnityStereoScreenSpaceUVAdjust(rayPosProj.xy, _MainTex_ST), 0, 0)).r);

				float depthDiff = -rayPos.z - lDepth + bias;//0.02

				//shadow *= depthDiff > 0.0 && depthDiff < RayThickness? i / samplers * ShadowsFade : 1.0;
				if (depthDiff > 0.0 && depthDiff < RayThickness * -viewPos.z)
				{
					//shadow = 0.0;
					float fade = i / samplers;
					shadow = pow(fade, 5);//fade
					break;
				}
			}

			//return shadow;//test
			#if defined(SHADOWS_SPLIT_SPHERES)
			float fadeDiff = smoothstep(ShadowsDistanceStart, max(0.0, ShadowsDistanceStart + 10), z);
			#else
			float fadeDiff = smoothstep(ShadowsDistanceStart, max(0.0, ShadowsDistanceStart + 10), linearDepth);
			#endif
			
			return lerp(1.0, shadow, fadeDiff);
		}
		ENDCG
	}

	Pass // simple and bilateral blur (taking into account screen depth)
	{
		CGPROGRAM
		#pragma multi_compile_local _ NGSS_FAST_BLUR

		UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

		float ShadowsSoftness;
		//float ShadowsOpacity;

		UNITY_DECLARE_SCREENSPACE_TEXTURE(_MainTex);
		//sampler2D NGSS_ContactShadowsTexture;
		//float4 NGSS_ContactShadowsTexture_ST;

		float2 ShadowsKernel;
		float ShadowsEdgeTolerance;

		fixed4 frag(v2f input) : COLOR0
		{
			UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input); //Insert
			
			float weights = 0.0;
			float result = 0.0;
			float2 offsets = float2(1.0 / _ScreenParams.x, 1.0 / _ScreenParams.y) * ShadowsKernel.xy * ShadowsSoftness;

			#if defined(NGSS_FAST_BLUR)

				for (float i = -2; i <= 2; i++)
				{
					float2 offs = i * offsets;
					float shadow = tex2D(_MainTex, input.uv + offs.xy).r;
					result += shadow;
				}

				return result / 3;
				//return lerp(ShadowsOpacity, 1.0, result / 3);				

			#else

				float depth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, UnityStereoTransformScreenSpaceTex(input.uv)));
				//offsets /= depth/depth;//adjust kernel size over distance

				for (float i = -2; i <= 2; i++)
				{
					float2 offs = i * offsets;
					float curDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, UnityStereoTransformScreenSpaceTex(input.uv + offs.xy)));

					float curWeight = saturate(1.0 - abs(depth - curDepth) / ShadowsEdgeTolerance);
					//float curWeight = saturate( ShadowsEdgeTolerance - abs(depth - curDepth));
					float shadow = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, UnityStereoTransformScreenSpaceTex(input.uv + offs.xy)).r;
					result += shadow * curWeight;
					weights += curWeight;
				}

				return result / weights;
				//return lerp(ShadowsOpacity, 1.0, result / weights);

			#endif
		}

		ENDCG
	}

	}
	Fallback Off
}

Shader "Hidden/NGSS_DenoiseShader"
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
			//o.uv = v.uv;//NGSS 2.0
			//o.uv = ComputeNonStereoScreenPos(o.vertex).xy;
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
		
		Pass // simple and bilateral blur (taking into account screen depth)
		{
			CGPROGRAM
			
			UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
			UNITY_DECLARE_SCREENSPACE_TEXTURE(_MainTex);

			//half ShadowsOpacity;
			half DenoiseSoftness;
			half2 DenoiseKernel;
			half DenoiseEdgeTolerance;

			/*
			//OLD DENOISER, KEEPING IT HERE FOR REFERENCE
			uniform float NGSS_DENOISER_ITERATIONS = 4;
			uniform float NGSS_DENOISER_BLUR = 1;
			uniform float NGSS_DENOISER_EDGE_TOLERANCE = 1;

			inline fixed unitySampleShadow(unityShadowCoord4 shadowCoord)
			{
				half shadowAttenuation = 1.0f;

				//shadowCoord.xyz /= shadowCoord.w;
				shadowAttenuation = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_ShadowMapTexture, shadowCoord);	

				UNITY_BRANCH
				if (NGSS_DENOISER_ITERATIONS > 0)
				{
					half center = shadowAttenuation;
					float shadow = 0.0;
					float total = 0.0;

					float2 softness = NGSS_DENOISER_BLUR * _ShadowMapTexture_TexelSize.zw;
					for (float x = -NGSS_DENOISER_ITERATIONS; x <= NGSS_DENOISER_ITERATIONS; x++)
					{
						for (float y = -NGSS_DENOISER_ITERATIONS; y <= NGSS_DENOISER_ITERATIONS; y++)
						{
							//fixed sampleSM = tex2D(_ShadowMapTexture, UnityStereoTransformScreenSpaceTex(float4(shadowCoord.xy + float2(x, y) / softness, shadowCoord.zw)));
							fixed sampleSM = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_ShadowMapTexture, float4(shadowCoord.xy + float2(x, y) / softness, shadowCoord.zw));
							fixed weight = saturate(1.0 - abs(center - sampleSM) / NGSS_DENOISER_EDGE_TOLERANCE);
							//weight = pow(weight, expo);
							shadow += sampleSM * weight;
							total += weight;
						}
					}

					shadowAttenuation = shadow / total;
				}

				return shadowAttenuation;
			}
			 */

			fixed4 frag(v2f input) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input); //Insert
				
				float weights = 0.0;
				float result = 0.0;
				float2 offsets = float2(1.0 / _ScreenParams.x, 1.0 / _ScreenParams.y) * DenoiseKernel.xy * DenoiseSoftness;

				float depth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, UnityStereoTransformScreenSpaceTex(input.uv)));
				//offsets /= depth/depth;//adjust kernel size over distance

				for (float i = -2; i <= 2; i++)
				{
					float2 offs = i * offsets;
					float curDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, UnityStereoTransformScreenSpaceTex(input.uv + offs.xy)));

					float curWeight = saturate(1.0 - abs(depth - curDepth) / DenoiseEdgeTolerance);
					//float curWeight = saturate( DenoiseEdgeTolerance - abs(depth - curDepth));
					float shadow = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, UnityStereoTransformScreenSpaceTex(input.uv + offs.xy)).r;
					result += shadow * curWeight;
					weights += curWeight;
				}

				return result / weights;
				//return lerp(ShadowsOpacity, 1.0, result / weights);
			}
			ENDCG
		}
		
		Pass // mix
		{
			Cull Off ZWrite Off ZTest Always
			//Blend DstColor Zero
			//Blend Off			

			CGPROGRAM
			
			UNITY_DECLARE_SCREENSPACE_TEXTURE(NGSS_DenoiseTexture);
			
			fixed4 frag (v2f input) : SV_Target
			{
				return UNITY_SAMPLE_SCREENSPACE_TEXTURE(NGSS_DenoiseTexture, UnityStereoTransformScreenSpaceTex(input.uv)).rrrr;
			}
			ENDCG
		}
	}
	Fallback Off
}

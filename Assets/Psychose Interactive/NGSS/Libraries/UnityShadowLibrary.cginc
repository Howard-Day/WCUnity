// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

#ifndef UNITY_BUILTIN_SHADOW_LIBRARY_INCLUDED
#define UNITY_BUILTIN_SHADOW_LIBRARY_INCLUDED

// Shadowmap helpers.
#if defined( SHADOWS_SCREEN ) && defined( LIGHTMAP_ON )
	#define HANDLE_SHADOWS_BLENDING_IN_GI 1
#endif

#if (UNITY_VERSION < 2017)
#else
#define unityShadowCoord float
#define unityShadowCoord2 float2
#define unityShadowCoord3 float3
#define unityShadowCoord4 float4
#define unityShadowCoord4x4 float4x4
#endif

half    UnitySampleShadowmap_PCF7x7(float4 coord, float3 receiverPlaneDepthBias);   // Samples the shadowmap based on PCF filtering (7x7 kernel)
half    UnitySampleShadowmap_PCF5x5(float4 coord, float3 receiverPlaneDepthBias);   // Samples the shadowmap based on PCF filtering (5x5 kernel)
half    UnitySampleShadowmap_PCF3x3(float4 coord, float3 receiverPlaneDepthBias);   // Samples the shadowmap based on PCF filtering (3x3 kernel)
float3  UnityGetReceiverPlaneDepthBias(float3 shadowCoord, float biasbiasMultiply); // Receiver plane depth bias

//NGSS START --------------------------------------------------------------------------------------------------------------------------------------------------------------

// Note: Only for Advanced users! Change only DEFINE values based on it's right side explanation. Don't comment/uncomment defines or uniforms that contains values, or you'll risk shader compile errors!

//Samplers per frag
//#define NGSS_FILTER_SAMPLERS 48									//Used for the final filtering of shadows. Recommended values: Mobile = 16, Consoles = 24, Desktop VR = 32, Desktop High = 48, Desktop Ultra 64
//#define NGSS_TEST_SAMPLERS 16										//Used to test blocker and early bail out algorithms. This value should never go beyond half Filter_Samplers count or you will slow down the algorithm
uniform int NGSS_TEST_SAMPLERS = 16;
uniform int NGSS_FILTER_SAMPLERS = 32;
//
uniform float NGSS_LOCAL_SAMPLING_DISTANCE = 75;
uniform float NGSS_GLOBAL_OPACITY = 0.0;
#define NGSS_GLOBAL_OPACITY_DEFINED

//Optimizations
//#define NGSS_USE_EARLY_BAILOUT_OPTIMIZATION

//Noise
//#define NGSS_SHADOWS_DITHERING									//Improve noise by stabilizing patterns over a screen space grid
//#define NGSS_BANDING_TO_NOISE_RATIO 1.0							//Defines the amount of banding to noise ratio, if 1 = 100% noise, if 0 = 100 banding
uniform float NGSS_NOISE_TO_DITHERING_SCALE = 0;
uniform float NGSS_BANDING_TO_NOISE_RATIO = 1.0;

//Bias
//#define NGSS_USE_SLOPE_BIAS											//Currently only works with Spot shadows. Need a way to implement something similar to Point shadows
#define NGSS_BIAS_FADE 0.015
#define NGSS_BIAS_SCALE 1.0											//Defines the scale of the Slope Based Bias algorithm (If 1.0 == 100%)

uniform float NGSS_PCSS_FILTER_LOCAL_MIN = 0.0125; 					//Close to blocker (If 0.0 == Hard Shadows). This value cannot be higher than NGSS_PCSS_FILTER_LOCAL_MAX
uniform float NGSS_PCSS_FILTER_LOCAL_MAX = 1.0; 					//Far from blocker (If 1.0 == Soft Shadows). This value cannot be smaller than NGSS_PCSS_FILTER_LOCAL_MIN
//uniform float NGSS_PCSS_LOCAL_BLOCKER_BIAS = 0.0;					//Allows to define an extra bias only on the blocker search algorithm

uniform sampler2D unity_RandomRotation16;
#define NGSS_PCSS_RANDOM_ROTATED_TEXTURE_DEFINED

uniform float NGSS_FORCE_HARD_SHADOWS = 0.0;

//uniform sampler2D _BlueNoiseTexture;
//uniform float4 _BlueNoiseTexture_TexelSize;
/*
Defines names 			Target platforms
SHADER_API_D3D11 		Direct3D 11
SHADER_API_GLCORE 		Desktop OpenGL “core” (GL 3/4)
SHADER_API_GLES 		OpenGL ES 2.0
SHADER_API_GLES3 		OpenGL ES 3.0/3.1
SHADER_API_METAL 		iOS/Mac Metal
SHADER_API_VULKAN 		Vulkan
SHADER_API_D3D11_9X 	Direct3D 11 “feature level 9.x” target for Universal Windows Platform
SHADER_API_PS4 			PlayStation 4. SHADER_API_PSSL is also defined
SHADER_API_SWITCH
SHADER_API_XBOXONE
SHADER_API_MOBILE 		all general mobile platforms (GLES, GLES3, METAL)
*/
//NGSS SUPPORT
#if (SHADER_TARGET < 30 || defined(SHADER_API_D3D9) || defined(SHADER_API_GLES) || defined(SHADER_API_PSP2) || defined(SHADER_API_N3DS))
    #define NGSS_NO_SUPPORT
#else
	#define NGSS_SUPPORT_LOCAL
#endif

#define ditherPatternLocal float4x4(0.0,0.5,0.125,0.625, 0.75,0.22,0.875,0.375, 0.1875,0.6875,0.0625,0.5625, 0.9375,0.4375,0.8125,0.3125)

float LocalRand01(float3 seed)
{
	float dt = dot(seed, float3(12.9898, 78.233, 45.5432));// project seed on random constant vector   
	return frac(sin(dt) * 43758.5453);// return only fractional part
}

int LocalRandInt(float3 seed, int maxInt)
{
	return int((float(maxInt) * LocalRand01(seed), maxInt) % 16);//fmod() function equivalent as % operator
}

float3 LocalRandDir(float3 seed)
{
	return lerp((0).xxx, frac(cross(seed, float3(0.6711056f, 0.583715f, 0.983751f)) * 43758.5453), NGSS_BANDING_TO_NOISE_RATIO);	
}

float2 VogelDiskSample(int sampleIndex, int samplesCount, float phi)
{
	//float phi = 3.14159265359f;//UNITY_PI;
	float GoldenAngle = 2.4f;

	float r = sqrt(sampleIndex + 0.5f) / sqrt(samplesCount);
	float theta = sampleIndex * GoldenAngle + phi;

	float sine, cosine;
	sincos(theta, sine, cosine);
	
	return float2(r * cosine, r * sine);
}

uniform sampler2D _BlueNoiseTexture;
uniform float4 _BlueNoiseTexture_TexelSize;

//the result of this multiply by 2pi and give it as param to VogelDiskSample then add the resulting coords to original sample coords
float InterleavedGradientNoise(float2 position_screen)
{
	//DITHERING
	float ditherValue = ditherPatternLocal[position_screen.x * _ScreenParams.x % 4][position_screen.y * _ScreenParams.y % 4];
	
	//WHITE NOISE
	//float2 spos = tex2D(unity_RandomRotation16, position_screen.xy).xy;
	//float2 magic = float2(
        //23.14069263277926, // e^pi (Gelfond's constant)
         //2.665144142690225 // 2^sqrt(2) (Gelfondâ€“Schneider constant)
    //);
	//float noiseValue = frac(cos(dot(spos, magic)) * 12345.6789);
	
	//BLUE NOISE
	float noiseValue = tex2D(_BlueNoiseTexture, position_screen.xy * _BlueNoiseTexture_TexelSize.xy * _ScreenParams.xy).r;
	
    return lerp(noiseValue, ditherValue, NGSS_NOISE_TO_DITHERING_SCALE);	
}

// Derivatives of light-space depth with respect to texture coordinates (nVIDIA)
float2 DepthGradient(float2 uv, float z)
{
	float2 dz_duv = 0;
#if defined(NGSS_USE_SLOPE_BIAS)
	float3 duvdist_dx = ddx(float3(uv,z));
	float3 duvdist_dy = ddy(float3(uv,z));

	dz_duv.x = duvdist_dy.y * duvdist_dx.z;
	dz_duv.x -= duvdist_dx.y * duvdist_dy.z;

	dz_duv.y = duvdist_dx.x * duvdist_dy.z;
	dz_duv.y -= duvdist_dy.x * duvdist_dx.z;

	dz_duv /= (duvdist_dx.x * duvdist_dy.y) - (duvdist_dx.y * duvdist_dy.x);
#endif
	return dz_duv;
}
//Isidoro Slope Bias Method (AMD)
float2 SlopeBasedBias(float3 projCoords)
{
	float2 dz_duv = 0;
#if defined(NGSS_USE_SLOPE_BIAS)
	//Packing derivatives of u,v, and distance to light source w.r.t. screen space x, and y
	float3 duvdist_dx = ddx(projCoords);
	float3 duvdist_dy = ddy(projCoords);
	//Invert texture Jacobian and use chain rule to compute ddist/du and ddist/dv
	//  |ddist/du| = |du/dx  du/dy|-T  * |ddist/dx|
	//  |ddist/dv|   |dv/dx  dv/dy|      |ddist/dy|
	//Multiply ddist/dx and ddist/dy by inverse transpose of Jacobian
	float invDet = 1.0 / ((duvdist_dx.x * duvdist_dy.y) - (duvdist_dx.y * duvdist_dy.x));//NGSS_BIAS_SCALE = Bias multiply	
	
	//Top row of 2x2
	dz_duv.x = duvdist_dy.y * duvdist_dx.z; // invJtrans[0][0] * ddist_dx
	dz_duv.x -= duvdist_dx.y * duvdist_dy.z; // invJtrans[0][1] * ddist_dy
	//Bottom row of 2x2
	dz_duv.y = duvdist_dx.x * duvdist_dy.z; // invJtrans[1][1] * ddist_dy
	dz_duv.y -= duvdist_dy.x * duvdist_dx.z; // invJtrans[1][0] * ddist_dx
	dz_duv *= invDet;// * (1 - _LightShadowData.g);//* NGSS_BIAS_SCALE;
#endif
	return dz_duv;
}
// Derivatives of light-space depth with respect to texture coordinates (nVIDIA)
float SlopeBasedBiasCombine(float z0, float2 dz_duv, float2 offset)
{
//we need a way to define this before we get here
#if defined(SHADOWMAPSAMPLER_AND_TEXELSIZE_DEFINED)
	// Static depth biasing to make up for incorrect fractional sampling on the shadow map grid.
	float z1 = -min(dot(_ShadowMapTexture_TexelSize.xy, abs(dz_duv)), 0.01);//NGSS_RECEIVER_PLANE_MIN_FRACTIONAL_ERROR_LOCAL
	#if defined(UNITY_REVERSED_Z)
		z1 *= -1;
	#endif
	z0 += z1;
#endif
	//return z0 + (dz_duv.x * offset.x) + (dz_duv.y * offset.y);//AMD
	z0 += dot(dz_duv, offset);
	return z0;
}
//John Isidoro (AMD)
float SlopeBasedBiasCombineAmd(float projCoords_Z, float2 ddist_duv, float2 texCoordOffset)
{
	return projCoords_Z + (ddist_duv.x * texCoordOffset.x) + (ddist_duv.y * texCoordOffset.y);//w?
}

// ------------------------------------------------------------------
// Spot light shadows
// ------------------------------------------------------------------

#if defined (SHADOWS_DEPTH) && defined (SPOT)

	//INLINE SAMPLING //GLCORE support SM3.5 (required for Inline Sampling) but seems to be confused about Z buffer being inverted, so skip it
	#if (UNITY_VERSION < 2017) || defined(NGSS_NO_SUPPORT) || defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES3)// || defined(SHADER_API_METAL)
		//#define NO_INLINE_SAMPLERS_SUPPORT
	#else
		#define NGSS_CAN_USE_PCSS_FILTER
		SamplerState my_linear_clamp_smp;
	#endif

	// declare shadowmap
    #if !defined(SHADOWMAPSAMPLER_DEFINED)
        UNITY_DECLARE_SHADOWMAP(_ShadowMapTexture);
        #define SHADOWMAPSAMPLER_DEFINED
    #endif

    // shadow sampling offsets and texel size
    #if defined (SHADOWS_SOFT)
        float4 _ShadowOffsets[4];
        float4 _ShadowMapTexture_TexelSize;
        #define SHADOWMAPSAMPLER_AND_TEXELSIZE_DEFINED
    #endif
	
	#if defined (NGSS_CAN_USE_PCSS_FILTER)
	float2 BLOCKER_SEARCH_SPOT(float4 coord, float diskRadius, float randPied, int samplers, float2 dz_duv)
	{
		//BLOCKER SEARCH	
		float blockerCount = 0;
		float avgBlockerDistance = 0.0;		
		float sampleDepth = coord.z;
		
		for (int i = 0; i < samplers; ++i)
		{
			float2 rotatedOffset = VogelDiskSample(i, samplers, randPied) * diskRadius;			
			#if defined(NGSS_USE_SLOPE_BIAS)
			sampleDepth = SlopeBasedBiasCombineAmd(coord.z, dz_duv, rotatedOffset);
			#endif
			half closestDepth = _ShadowMapTexture.SampleLevel(my_linear_clamp_smp, coord.xy + rotatedOffset, 0.0);
			
			//blockerCount++;
			//avgBlockerDistance += closestDepth;

			/*
			#if defined(UNITY_REVERSED_Z)
			if (closestDepth > sampleDepth)//coord.z
			#else
			if (closestDepth < sampleDepth)//coord.z
			#endif
			{
				blockerCount++;
				avgBlockerDistance += closestDepth;
			}*/
			
			//No conditional branching
			#if defined(UNITY_REVERSED_Z)
			float sum = closestDepth >= sampleDepth;
			blockerCount += sum;
			avgBlockerDistance += closestDepth * sum;
			#else
			float sum = closestDepth <= sampleDepth;
			blockerCount += sum;
			avgBlockerDistance += closestDepth * sum;
			#endif			
		}

		return float2(avgBlockerDistance / blockerCount, blockerCount);
	}
	#endif//NGSS_CAN_USE_PCSS_FILTER
	
	float PCF_FILTER_SPOT(float4 coord, float diskRadius, float randPied, int samplers, float2 dz_duv)
	{
		float result = 0.0;
		float sampleDepth = coord.z;
		
		for (int i = 0; i < samplers; ++i)
		{
			float2 rotatedOffset = VogelDiskSample(i, samplers, randPied) * diskRadius;
			#if defined(NGSS_USE_SLOPE_BIAS)
			sampleDepth = SlopeBasedBiasCombineAmd(coord.z, dz_duv, rotatedOffset);
			#endif
			
			result += UNITY_SAMPLE_SHADOW(_ShadowMapTexture, float3(coord.xy + rotatedOffset, sampleDepth)).r;//coord.zw
		}
		half shadow = result / samplers;

		return shadow;
	}
	
	inline fixed UnitySampleShadowmapNGSS(float4 shadowCoord, float3 screenPos)
	{
		//float screen_pixel_x = 1.0 / _ScreenParams.x;
		//float screen_pixel_y = 1.0 / _ScreenParams.y;		
		//if(screenPos.x + screen_pixel_x > 1.0 || screenPos.x - screen_pixel_x < 0.0 || screenPos.y + screen_pixel_y > 1.0 || screenPos.y - screen_pixel_y < 0.0)//if outside screen clip it
			//return 1.0;
			
		// DX11 feature level 9.x shader compiler (d3dcompiler_47 at least)
		// has a bug where trying to do more than one shadowmap sample fails compilation
		// with "inconsistent sampler usage". Until that is fixed, just never compile
		// multi-tap shadow variant on d3d11_9x.
			
		#if !defined(NGSS_NO_SUPPORT)
		// Fallback to 1-tap shadows
		if (NGSS_FORCE_HARD_SHADOWS > 0.0)
		{
		#endif
			// Fallback to 1-tap shadows
			#if defined (SHADOWS_NATIVE)
			half shadowFallback = UNITY_SAMPLE_SHADOW_PROJ(_ShadowMapTexture, shadowCoord);
			shadowFallback = lerp(_LightShadowData.r, 1.0f, shadowFallback);//NGSS_GLOBAL_OPACITY == _LightShadowData.r
			#else
			half shadowFallback = SAMPLE_DEPTH_TEXTURE_PROJ(_ShadowMapTexture, UNITY_PROJ_COORD(shadowCoord)) < (shadowCoord.z / shadowCoord.w) ? _LightShadowData.r : 1.0;//NGSS_GLOBAL_OPACITY == _LightShadowData.r
			#endif
			return shadowFallback;
		#if !defined(NGSS_NO_SUPPORT)
		}
		#endif

		float4 coord = shadowCoord;
		coord.xyz /= coord.w;
		
		//Slope base bias
		//float2 dz_duv = DepthGradientLocal(coord.xy, coord.z);//nVIDIA
		float2 dz_duv = SlopeBasedBias(coord.xyz);//AMD
		
		//float4 wpos = mul(shadowCoord, unity_WorldToShadow[0]);
		//float4 cpos = UnityWorldToClipPos(mul(inverseMat(unity_WorldToShadow[0]), coord).xyz);
		//float4 cpos = UnityWorldToClipPos(wpos);
		//float4 spos = ComputeScreenPos(cpos);
		//spos.xyz /= spos.w;//screen pos
		
		float randPied = InterleavedGradientNoise(screenPos.xy) * UNITY_FOUR_PI / max(1.0, screenPos.z);
		
		//float diskRadius = 0.5 / (1-_LightShadowData.r) / (shadowCoord.z / (_LightShadowData.z + _LightShadowData.w));
		float diskRadius = (1.0 - _LightShadowData.r);
		//diskRadius = clamp(diskRadius * (coord.z * 0.5) , 0.05, 1.0);//try to keep same softness with distance
		//PCF = 0.0175 | PCSS = 0.05
		
		int samplers_test = clamp(NGSS_TEST_SAMPLERS * (1.0 - min(screenPos.z, NGSS_LOCAL_SAMPLING_DISTANCE) / NGSS_LOCAL_SAMPLING_DISTANCE), 4, 64);
		//int samplers_test = clamp(NGSS_TEST_SAMPLERS, 4, 64);
		
		#if defined (NGSS_CAN_USE_PCSS_FILTER) && defined(SHADOWS_SOFT)//PCSS
			//half diskRadiusPCF = diskRadius * 0.05;//normal
			//half diskRadiusPCF = diskRadius * 4.0 * coord.z;//scale over distance
			half diskRadiusPCF = lerp(0.05, 2.0 * coord.z, 0.5) * diskRadius;//* coord.z behaves exactly as POINT PCSS
			
			float2 distances = BLOCKER_SEARCH_SPOT(coord, diskRadiusPCF, randPied, samplers_test, dz_duv);
			
			if( distances.y == 0.0 )//There are no occluders so early out (this saves filtering)
				return 1.0;
			//#if defined(NGSS_USE_EARLY_BAILOUT_OPTIMIZATION)
			else if (distances.y == samplers_test)//There are 100% occluders so early out (this saves filtering)
				return NGSS_GLOBAL_OPACITY;
			//#endif//NGSS_USE_EARLY_BAILOUT_OPTIMIZATION
			
			//clamping the kernel size to avoid hard shadows at close ranges
			//diskRadius *= clamp(distances.x, NGSS_PCSS_FILTER_POINT_MIN, NGSS_PCSS_FILTER_POINT_MAX);
			#if defined(NGSS_USE_SLOPE_BIAS)			
			float coordz = SlopeBasedBiasCombineAmd(coord.z, dz_duv, (0).xx);
			float dist = (coordz - distances.x)/(distances.x);
			#else
			float dist = (coord.z - distances.x)/(distances.x);
			#endif
			
			//diskRadiusPCF *= dist;//normal
			diskRadiusPCF *= lerp(-NGSS_PCSS_FILTER_LOCAL_MIN, NGSS_PCSS_FILTER_LOCAL_MAX, dist);
			
		#else// NO NGSS_CAN_USE_PCSS_FILTER
			half diskRadiusPCF = diskRadius * 0.0175;//lerp(0.0175, coord.z, 0.75);//* coord.z behaves exactly as POINT PCSS
			half shadowTest = PCF_FILTER_SPOT(coord, diskRadiusPCF, randPied, samplers_test, dz_duv);
			
			if(shadowTest == 1.0 )//There are no occluders so early out (this saves filtering)
				return 1.0;
			else if (shadowTest == 0.0)//There are 100% occluders so early out (this saves filtering)
				return NGSS_GLOBAL_OPACITY;
		#endif//NGSS_CAN_USE_PCSS_FILTER
		
		int samplers = clamp(NGSS_FILTER_SAMPLERS * (1.0 - min(screenPos.z, NGSS_LOCAL_SAMPLING_DISTANCE) / NGSS_LOCAL_SAMPLING_DISTANCE), 4, 64);
		//int samplers = clamp(NGSS_FILTER_SAMPLERS, 4, 64);
		
		half shadow = PCF_FILTER_SPOT(coord, diskRadiusPCF, randPied, samplers, dz_duv);
		
		return lerp(NGSS_GLOBAL_OPACITY, 1.0, shadow);
	}
	
	inline fixed UnitySampleShadowmap(float4 shadowCoord)
	{
		float2 spos = tex2D(unity_RandomRotation16, shadowCoord.xy * _ScreenParams.xy * 16).xy;
		return UnitySampleShadowmapNGSS(shadowCoord, float3(spos, 0.0));
	}

#endif // #if defined (SHADOWS_DEPTH) && defined (SPOT)

// ------------------------------------------------------------------
// Point light shadows
// ------------------------------------------------------------------

#if defined (SHADOWS_CUBE)
	
	//INLINE SAMPLING for CubeMaps are only available in 2017.3 and forward	
	#if defined(SHADOWS_CUBE_IN_DEPTH_TEX)
	#define NGSS_CAN_USE_PCSS_FILTER
	SamplerState my_linear_clamp_smp;
	
	UNITY_DECLARE_TEXCUBE_SHADOWMAP(_ShadowMapTexture);
	inline half computeShadowDist(float3 vec)
    {
		//_LightPositionRange; // xyz = pos, w = 1/range
		//_LightProjectionParams; // for point light projection: x = zfar / (znear - zfar), y = (znear * zfar) / (znear - zfar), z=shadow bias, w=shadow scale bias
		
        float3 absVec = abs(vec);
		
		//modd
        //float3 biasVec = normalize(absVec);
        //absVec -= biasVec * _LightProjectionParams.z;//bias
        absVec = max(float3(0.0, 0.0, 0.0), absVec);

        float dominantAxis = max(max(absVec.x, absVec.y), absVec.z); // TODO use max3() instead
        dominantAxis = max(0.0, dominantAxis - _LightProjectionParams.z);// shadow bias from point light is apllied here.
        //dominantAxis *= _LightProjectionParams.w; // extra bias no needed now
        float mydist = -_LightProjectionParams.x + _LightProjectionParams.y / dominantAxis; // project to shadow map clip space [0; 1]
		
        #if defined(UNITY_REVERSED_Z)
        mydist = 1.0 - mydist; // depth buffers are reversed! Additionally we can move this to CPP code!
        #endif

        return mydist;        
    }
	#else//NO SHADOWS_CUBE_IN_DEPTH_TEX
	UNITY_DECLARE_TEXCUBE(_ShadowMapTexture);
	inline float SampleCubeDistance(float3 vec)
	{
		// DX9 with SM2.0, and DX11 FL 9.x do not have texture LOD sampling.
	#if ((SHADER_TARGET < 25) && defined(SHADER_API_D3D9)) || defined(SHADER_API_D3D11_9X)
		return UnityDecodeCubeShadowDepth(texCUBE(_ShadowMapTexture, vec));
	#else
		return UnityDecodeCubeShadowDepth(UNITY_SAMPLE_TEXCUBE_LOD(_ShadowMapTexture, vec, 0));
	#endif
	}

	#endif
	
	inline half UnitySampleShadowmapNGSS(float3 vec, float3 screenPos) //screenPos the same pos as when fetching screen space shadow mask
	{
		//float screen_pixel_x = 1.0 / _ScreenParams.x;
		//float screen_pixel_y = 1.0 / _ScreenParams.y;		
		//if(screenPos.x + screen_pixel_x > 1.0 || screenPos.x - screen_pixel_x < 0.0 || screenPos.y + screen_pixel_y > 1.0 || screenPos.y - screen_pixel_y < 0.0)//if outside screen clip it
			//return 1.0;
			
		#if defined(SHADOWS_CUBE_IN_DEPTH_TEX)
		//_LightPositionRange; // xyz = pos, w = 1/range
		//_LightProjectionParams; // for point light projection: x = zfar / (znear - zfar), y = (znear * zfar) / (znear - zfar), z=shadow bias, w=shadow scale bias
		float mydist = computeShadowDist(vec);		
		#else
		//To get world pos back, simply add _LightPositionRange.xyz to vec
		//receiver distance in 0-1 range
		float mydist = length(vec) * _LightPositionRange.w;
		//mydist *= _LightProjectionParams.w; // bias
		#endif
		
		#if !defined(NGSS_NO_SUPPORT)
		// Fallback to 1-tap shadows
		if (NGSS_FORCE_HARD_SHADOWS > 0.0)
		{
		#endif
			#if defined (SHADOWS_CUBE_IN_DEPTH_TEX)
				half shadowFallback = UNITY_SAMPLE_TEXCUBE_SHADOW(_ShadowMapTexture, float4(vec, mydist));
				return lerp(_LightShadowData.r, 1.0, shadowFallback);//NGSS_GLOBAL_OPACITY == _LightShadowData.r
			#else
				half shadowVal = UnityDecodeCubeShadowDepth(UNITY_SAMPLE_TEXCUBE(_ShadowMapTexture, vec));
				half shadowFallback = shadowVal < mydist ? _LightShadowData.r : 1.0;//NGSS_GLOBAL_OPACITY == _LightShadowData.r
				return shadowFallback;
			#endif
		#if !defined(NGSS_NO_SUPPORT)
		}
		#endif
		
		//float3 wpos = vec + _LightPositionRange.xyz;
		//float4 cpos = UnityWorldToClipPos(wpos);
		//float4 spos = ComputeScreenPos(cpos);
		//spos.xyz /= spos.w;//Screen pos 
		//if( abs(cpos.x) > 1.0 || abs(cpos.y) > 1.0)//if outside screen clip it
			//return 1.0;
		//float fragDist = 1.0 - (length(wpos - _WorldSpaceCameraPos.xyz) * _LightPositionRange.w);
		
		float randPied = InterleavedGradientNoise(screenPos.xy) * UNITY_FOUR_PI / max(1.0, screenPos.z);
		
		// Tangent plane
		float3 xaxis = normalize(cross(vec, vec.zxy));//vec.zxy;//LocalRandDir(screenPos2.xyz);
		float3 yaxis = normalize(cross(vec, xaxis));
		
		float shadow = 0.0;
		//get radius in 0 to 1 range as it comes inverted for no reason
		float diskRadius = (1 - _LightShadowData.r);
		
		xaxis *= diskRadius * 0.25;
		yaxis *= diskRadius * 0.25;

		#if defined(NGSS_USE_SLOPE_BIAS)
		//float2 dz_duv = SlopeBasedBias(float3(cross(xaxis, yaxis).xy, mydist));//AMD
		//mydist = SlopeBasedBiasCombine(mydist, dz_duv, (0).xx);
		#endif
		
		//BLOCKER SEARCH	
		float blockerCount = 0;
		float avgBlockerDistance = 0.0;
		half diskRadiusFinal = 0.35;
		
		int samplers_test = clamp(NGSS_TEST_SAMPLERS * (1.0 - min(screenPos.z, NGSS_LOCAL_SAMPLING_DISTANCE) / NGSS_LOCAL_SAMPLING_DISTANCE), 4, 64);
		//int samplers_test = clamp(NGSS_TEST_SAMPLERS, 4, 64);
				
		#if defined(SHADOWS_SOFT)//PCSS	TEST
		
			for (int i = 0; i < samplers_test; ++i)
			{
				float2 rotatedOffset = VogelDiskSample(i, samplers_test, randPied);
				float3 sampleDir = xaxis * rotatedOffset.x + yaxis * rotatedOffset.y;
				float3 vecOffset = vec + sampleDir;
				//float sampleDepth = BiasedZLocal(shadowDepth, dz_duv, sampleOffset);//nVIDIA
				
			#if defined(SHADOWS_CUBE_IN_DEPTH_TEX)
				
				half myOffsetDist = computeShadowDist(vecOffset);
				//Can speeded up with Gather and GatherRed (they can sample 4 surrounding pixels at the same time at once)
				half closestDepth = _ShadowMapTexture.SampleLevel(my_linear_clamp_smp, vecOffset, 0.0);
				
				/*
				#if defined(UNITY_REVERSED_Z)
				if (closestDepth >= myOffsetDist)//mydist)
				#else
				if (closestDepth <= myOffsetDist)//mydist)
				#endif
				{
					blockerCount++;
					avgBlockerDistance += closestDepth;
				}*/
				
				//No conditional branching
				#if defined(UNITY_REVERSED_Z)
				float sum = closestDepth > myOffsetDist;
				#else
				float sum = closestDepth < myOffsetDist;
				#endif
				//GLCORE dont play well with PCSS so skip it
				#if defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES3)// || defined(SHADER_API_METAL)
				blockerCount += sum;
				#else
				blockerCount += sum;
				avgBlockerDistance += closestDepth * sum;
				#endif				
				
			#else// NO SHADOWS_CUBE_IN_DEPTH_TEX
			
				half closestDepth = SampleCubeDistance(vecOffset).r;
				/*
				if (closestDepth < mydist)
				{
					blockerCount++;
					avgBlockerDistance += closestDepth;
				}
				*/
				float sum = closestDepth < mydist;
				blockerCount += sum;
				avgBlockerDistance += closestDepth * sum;
				
			#endif//SHADOWS_CUBE_IN_DEPTH_TEX		
			}
			
			if( blockerCount == 0.0 )//There are no occluders so early out (this saves filtering)
				return 1.0;
			//#if defined(NGSS_USE_EARLY_BAILOUT_OPTIMIZATION)
			else if (blockerCount == samplers_test)//There are 100% occluders so early out (this saves filtering)
				return NGSS_GLOBAL_OPACITY;
			//#endif//NGSS_USE_EARLY_BAILOUT_OPTIMIZATION
			
			//GLCORE support SM3.5 (required for Inline Sampling) but seems to be confused about Z buffer being inverted, so skip it
			#if defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES3)// || defined(SHADER_API_METAL)
				diskRadiusFinal = 0.35;//Lerp between PCF and PCSS
			#else
				avgBlockerDistance /= blockerCount;
				
				//avgBlockerDistance = _LightProjectionParams.y / (avgBlockerDistance + _LightProjectionParams.x);//Convert from light to world space
				//clamping the kernel size to avoid hard shadows at close ranges
				//diskRadius *= clamp(avgBlockerDistance, NGSS_PCSS_FILTER_POINT_MIN, NGSS_PCSS_FILTER_POINT_MAX);
				//#if (UNITY_VERSION <= 570 || UNITY_VERSION == 20171 || UNITY_VERSION == 201711 || UNITY_VERSION == 201712 || UNITY_VERSION == 201713 || UNITY_VERSION == 20172 || UNITY_VERSION == 201721 || UNITY_VERSION == 201722)
				#if (UNITY_VERSION <= 570 || UNITY_VERSION < 201730)
				float dist = ((mydist - avgBlockerDistance)/(mydist));
				diskRadiusFinal = lerp(0.0, 1.0, dist);//in earlier versions of Unity (before 201730) we dont expose PCSS properties so hardcoding these values
				#else
				float dist = ((mydist - avgBlockerDistance)/(avgBlockerDistance));
				diskRadiusFinal = lerp(-NGSS_PCSS_FILTER_LOCAL_MIN, NGSS_PCSS_FILTER_LOCAL_MAX, dist);
				#endif
				
			#endif	
			
		#else //PCF TEST
			half shadowTest = 0.0;
			for (int j = 0; j < samplers_test; ++j)
			{
				float2 rotatedOffset = VogelDiskSample(j, samplers_test, randPied);
				float3 sampleDir = xaxis * rotatedOffset.x + yaxis * rotatedOffset.y;
				
			#if defined(SHADOWS_CUBE_IN_DEPTH_TEX)
				
				float3 vecOffset = vec + sampleDir * diskRadiusFinal;

				half myOffsetDist = computeShadowDist(vecOffset);

				#if defined(NGSS_USE_SLOPE_BIAS)
				//float2 dz_duv = SlopeBasedBias(float3(xaxis.z * rotatedOffset.x, yaxis.z * rotatedOffset.y, myOffsetDist));//AMD
				//myOffsetDist = SlopeBasedBiasCombine(myOffsetDist, dz_duv, (0).xx);
				#endif
				
				shadowTest += UNITY_SAMPLE_TEXCUBE_SHADOW(_ShadowMapTexture, float4(vecOffset, myOffsetDist));
				//shadowTest += UNITY_SAMPLE_TEXCUBE_SHADOW(_ShadowMapTexture, float4(vecOffset, mydist);
				
			#else
				
				float closestDepth = SampleCubeDistance(vec + sampleDir * diskRadiusFinal).r;
				shadowTest += (mydist - closestDepth < 0.0) ? 1.0 : 0.0;
				
			#endif//SHADOWS_CUBE_IN_DEPTH_TEX
			}
			shadowTest /= samplers_test;
			
			if(shadowTest == 1.0 )//There are no occluders so early out (this saves filtering)
				return 1.0;
			else if (shadowTest == 0.0)//There are 100% occluders so early out (this saves filtering)
				return NGSS_GLOBAL_OPACITY;
			
		#endif
		
		int samplers = clamp(NGSS_FILTER_SAMPLERS * (1.0 - min(screenPos.z, NGSS_LOCAL_SAMPLING_DISTANCE) / NGSS_LOCAL_SAMPLING_DISTANCE), 4, 64);
		//int samplers = clamp(NGSS_FILTER_SAMPLERS, 4, 64);
		
		//PCF FILTERING
		for (int j = 0; j < samplers; ++j)
		{
			float2 rotatedOffset = VogelDiskSample(j, samplers, randPied);
			float3 sampleDir = xaxis * rotatedOffset.x + yaxis * rotatedOffset.y;
			
		#if defined(SHADOWS_CUBE_IN_DEPTH_TEX)
			
			float3 vecOffset = vec + sampleDir * diskRadiusFinal;
            half myOffsetDist = computeShadowDist(vecOffset);
			shadow += UNITY_SAMPLE_TEXCUBE_SHADOW(_ShadowMapTexture, float4(vecOffset, myOffsetDist));
			//shadow += UNITY_SAMPLE_TEXCUBE_SHADOW(_ShadowMapTexture, float4(vecOffset, mydist);
			
		#else
			
			float closestDepth = SampleCubeDistance(vec + sampleDir * diskRadiusFinal).r;
			shadow += (mydist - closestDepth < 0.0) ? 1.0 : 0.0;
			
		#endif//SHADOWS_CUBE_IN_DEPTH_TEX
		}
		
		return lerp(NGSS_GLOBAL_OPACITY, 1.0, shadow / samplers);
	}
	
	inline half UnitySampleShadowmap(float3 vec)
	{
		float3 wpos = vec + _LightPositionRange.xyz;
		//float3 spos = mul(unity_WorldToCamera, float4(wpos, 1.0)).xyz;
		//float4 cpos = ComputeScreenPos(UnityViewToClipPos(spos));
		float4 cpos = ComputeScreenPos(UnityWorldToClipPos(wpos));
		return UnitySampleShadowmapNGSS(vec, float3(cpos.xy / cpos.w, distance(_WorldSpaceCameraPos, wpos)));
	}

#endif // #if defined (SHADOWS_CUBE)


// ------------------------------------------------------------------
// Baked shadows
// ------------------------------------------------------------------

#if UNITY_LIGHT_PROBE_PROXY_VOLUME

half4 LPPV_SampleProbeOcclusion(float3 worldPos)
{
    const float transformToLocal = unity_ProbeVolumeParams.y;
    const float texelSizeX = unity_ProbeVolumeParams.z;

    //The SH coefficients textures and probe occlusion are packed into 1 atlas.
    //-------------------------
    //| ShR | ShG | ShB | Occ |
    //-------------------------

    float3 position = (transformToLocal == 1.0f) ? mul(unity_ProbeVolumeWorldToObject, float4(worldPos, 1.0)).xyz : worldPos;

    //Get a tex coord between 0 and 1
    float3 texCoord = (position - unity_ProbeVolumeMin.xyz) * unity_ProbeVolumeSizeInv.xyz;

    // Sample fourth texture in the atlas
    // We need to compute proper U coordinate to sample.
    // Clamp the coordinate otherwize we'll have leaking between ShB coefficients and Probe Occlusion(Occ) info
    texCoord.x = max(texCoord.x * 0.25f + 0.75f, 0.75f + 0.5f * texelSizeX);

    return UNITY_SAMPLE_TEX3D_SAMPLER(unity_ProbeVolumeSH, unity_ProbeVolumeSH, texCoord);
}

#endif //#if UNITY_LIGHT_PROBE_PROXY_VOLUME

// ------------------------------------------------------------------
// Used by the forward rendering path
fixed UnitySampleBakedOcclusion (float2 lightmapUV, float3 worldPos)
{
    #if defined (SHADOWS_SHADOWMASK)
        #if defined(LIGHTMAP_ON)
			//fixed4 rawOcclusionMask = UNITY_SAMPLE_TEX2D(unity_ShadowMask, lightmapUV.xy);//UNITY 2018
			fixed4 rawOcclusionMask = UNITY_SAMPLE_TEX2D_SAMPLER(unity_ShadowMask, unity_Lightmap, lightmapUV.xy);//Unity 2017 and below
        #else
            fixed4 rawOcclusionMask = fixed4(1.0, 1.0, 1.0, 1.0);
            #if UNITY_LIGHT_PROBE_PROXY_VOLUME
                if (unity_ProbeVolumeParams.x == 1.0)
                    rawOcclusionMask = LPPV_SampleProbeOcclusion(worldPos);
                else
                    rawOcclusionMask = UNITY_SAMPLE_TEX2D(unity_ShadowMask, lightmapUV.xy);
            #else
                rawOcclusionMask = UNITY_SAMPLE_TEX2D(unity_ShadowMask, lightmapUV.xy);
            #endif
        #endif
        return saturate(dot(rawOcclusionMask, unity_OcclusionMaskSelector));

    #else

        //In forward dynamic objects can only get baked occlusion from LPPV, light probe occlusion is done on the CPU by attenuating the light color.
        fixed atten = 1.0f;
        #if defined(UNITY_INSTANCING_ENABLED) && defined(UNITY_USE_SHCOEFFS_ARRAYS)
            // ...unless we are doing instancing, and the attenuation is packed into SHC array's .w component.
            atten = unity_SHC.w;
        #endif

        #if UNITY_LIGHT_PROBE_PROXY_VOLUME && !defined(LIGHTMAP_ON) && !UNITY_STANDARD_SIMPLE
            fixed4 rawOcclusionMask = atten.xxxx;
            if (unity_ProbeVolumeParams.x == 1.0)
                rawOcclusionMask = LPPV_SampleProbeOcclusion(worldPos);
            return saturate(dot(rawOcclusionMask, unity_OcclusionMaskSelector));
        #endif

        return atten;
    #endif
}

// ------------------------------------------------------------------
// Used by the deferred rendering path (in the gbuffer pass)
fixed4 UnityGetRawBakedOcclusions(float2 lightmapUV, float3 worldPos)
{
    #if defined (SHADOWS_SHADOWMASK)
        #if defined(LIGHTMAP_ON)
            //return UNITY_SAMPLE_TEX2D(unity_ShadowMask, lightmapUV.xy);//Unity 2018
			return UNITY_SAMPLE_TEX2D_SAMPLER(unity_ShadowMask, unity_Lightmap, lightmapUV.xy);//Unity 2017 and below
        #else
            half4 probeOcclusion = unity_ProbesOcclusion;

            #if UNITY_LIGHT_PROBE_PROXY_VOLUME
                if (unity_ProbeVolumeParams.x == 1.0)
                    probeOcclusion = LPPV_SampleProbeOcclusion(worldPos);
            #endif

            return probeOcclusion;
        #endif
    #else
        return fixed4(1.0, 1.0, 1.0, 1.0);
    #endif
}

// ------------------------------------------------------------------
// Used by both the forward and the deferred rendering path
half UnityMixRealtimeAndBakedShadows(half realtimeShadowAttenuation, half bakedShadowAttenuation, half fade)
{
    // -- Static objects --
    // FWD BASE PASS
    // ShadowMask mode          = LIGHTMAP_ON + SHADOWS_SHADOWMASK + LIGHTMAP_SHADOW_MIXING
    // Distance shadowmask mode = LIGHTMAP_ON + SHADOWS_SHADOWMASK
    // Subtractive mode         = LIGHTMAP_ON + LIGHTMAP_SHADOW_MIXING
    // Pure realtime direct lit = LIGHTMAP_ON

    // FWD ADD PASS
    // ShadowMask mode          = SHADOWS_SHADOWMASK + LIGHTMAP_SHADOW_MIXING
    // Distance shadowmask mode = SHADOWS_SHADOWMASK
    // Pure realtime direct lit = LIGHTMAP_ON

    // DEFERRED LIGHTING PASS
    // ShadowMask mode          = LIGHTMAP_ON + SHADOWS_SHADOWMASK + LIGHTMAP_SHADOW_MIXING
    // Distance shadowmask mode = LIGHTMAP_ON + SHADOWS_SHADOWMASK
    // Pure realtime direct lit = LIGHTMAP_ON

    // -- Dynamic objects --
    // FWD BASE PASS + FWD ADD ASS
    // ShadowMask mode          = LIGHTMAP_SHADOW_MIXING
    // Distance shadowmask mode = N/A
    // Subtractive mode         = LIGHTMAP_SHADOW_MIXING (only matter for LPPV. Light probes occlusion being done on CPU)
    // Pure realtime direct lit = N/A

    // DEFERRED LIGHTING PASS
    // ShadowMask mode          = SHADOWS_SHADOWMASK + LIGHTMAP_SHADOW_MIXING
    // Distance shadowmask mode = SHADOWS_SHADOWMASK
    // Pure realtime direct lit = N/A

    #if !defined(SHADOWS_DEPTH) && !defined(SHADOWS_SCREEN) && !defined(SHADOWS_CUBE)
        #if defined(LIGHTMAP_ON) && defined (LIGHTMAP_SHADOW_MIXING) && !defined (SHADOWS_SHADOWMASK)
            //In subtractive mode when there is no shadow we kill the light contribution as direct as been baked in the lightmap.
            return 0.0;
        #else
            return bakedShadowAttenuation;
        #endif
    #endif

    #if (SHADER_TARGET <= 20) || UNITY_STANDARD_SIMPLE
        //no fading nor blending on SM 2.0 because of instruction count limit.
        #if defined(SHADOWS_SHADOWMASK) || defined(LIGHTMAP_SHADOW_MIXING)
            return realtimeShadowAttenuation * bakedShadowAttenuation;//min(realtimeShadowAttenuation, bakedShadowAttenuation);
        #else
            return realtimeShadowAttenuation;
        #endif
    #endif

    #if defined(LIGHTMAP_SHADOW_MIXING)
        //Subtractive or shadowmask mode
        realtimeShadowAttenuation = saturate(realtimeShadowAttenuation + fade);
		return realtimeShadowAttenuation * bakedShadowAttenuation;//min(realtimeShadowAttenuation, bakedShadowAttenuation);
    #endif

    //In distance shadowmask or realtime shadow fadeout we lerp toward the baked shadows (bakedShadowAttenuation will be 1 if no baked shadows)
    return lerp(realtimeShadowAttenuation, bakedShadowAttenuation, fade);
}

// ------------------------------------------------------------------
// Shadow fade
// ------------------------------------------------------------------

float UnityComputeShadowFadeDistance(float3 wpos, float z)
{
    float sphereDist = distance(wpos, unity_ShadowFadeCenterAndType.xyz);
    return lerp(z, sphereDist, unity_ShadowFadeCenterAndType.w);
}

// ------------------------------------------------------------------
half UnityComputeShadowFade(float fadeDist)
{
    return saturate(fadeDist * _LightShadowData.z + _LightShadowData.w);
}


// ------------------------------------------------------------------
//  Bias
// ------------------------------------------------------------------

/**
* Computes the receiver plane depth bias for the given shadow coord in screen space.
* Inspirations:
*   http://mynameismjp.wordpress.com/2013/09/10/shadow-maps/
*   http://amd-dev.wpengine.netdna-cdn.com/wordpress/media/2012/10/Isidoro-ShadowMapping.pdf
*/
float3 UnityGetReceiverPlaneDepthBias(float3 shadowCoord, float biasMultiply)
{
    // Should receiver plane bias be used? This estimates receiver slope using derivatives,
    // and tries to tilt the PCF kernel along it. However, when doing it in screenspace from the depth texture
    // (ie all light in deferred and directional light in both forward and deferred), the derivatives are wrong
    // on edges or intersections of objects, leading to shadow artifacts. Thus it is disabled by default.
    float3 biasUVZ = 0;

#if defined(UNITY_USE_RECEIVER_PLANE_BIAS) && defined(SHADOWMAPSAMPLER_AND_TEXELSIZE_DEFINED)
    float3 dx = ddx(shadowCoord);
    float3 dy = ddy(shadowCoord);

    biasUVZ.x = dy.y * dx.z - dx.y * dy.z;
    biasUVZ.y = dx.x * dy.z - dy.x * dx.z;
    biasUVZ.xy *= biasMultiply / ((dx.x * dy.y) - (dx.y * dy.x));

    // Static depth biasing to make up for incorrect fractional sampling on the shadow map grid.
    const float UNITY_RECEIVER_PLANE_MIN_FRACTIONAL_ERROR = 0.01f;
    float fractionalSamplingError = dot(_ShadowMapTexture_TexelSize.xy, abs(biasUVZ.xy));
    biasUVZ.z = -min(fractionalSamplingError, UNITY_RECEIVER_PLANE_MIN_FRACTIONAL_ERROR);
    #if defined(UNITY_REVERSED_Z)
        biasUVZ.z *= -1;
    #endif
#endif

    return biasUVZ;
}

/**
* Combines the different components of a shadow coordinate and returns the final coordinate.
* See UnityGetReceiverPlaneDepthBias
*/
float3 UnityCombineShadowcoordComponents(float2 baseUV, float2 deltaUV, float depth, float3 receiverPlaneDepthBias)
{
    float3 uv = float3(baseUV + deltaUV, depth + receiverPlaneDepthBias.z);
    uv.z += dot(deltaUV, receiverPlaneDepthBias.xy);
    return uv;
}

// ------------------------------------------------------------------
//  PCF Filtering helpers
// ------------------------------------------------------------------

/**
* Assuming a isoceles rectangle triangle of height "triangleHeight" (as drawn below).
* This function return the area of the triangle above the first texel.
*
* |\      <-- 45 degree slop isosceles rectangle triangle
* | \
* ----    <-- length of this side is "triangleHeight"
* _ _ _ _ <-- texels
*/
float _UnityInternalGetAreaAboveFirstTexelUnderAIsocelesRectangleTriangle(float triangleHeight)
{
    return triangleHeight - 0.5;
}

/**
* Assuming a isoceles triangle of 1.5 texels height and 3 texels wide lying on 4 texels.
* This function return the area of the triangle above each of those texels.
*    |    <-- offset from -0.5 to 0.5, 0 meaning triangle is exactly in the center
*   / \   <-- 45 degree slop isosceles triangle (ie tent projected in 2D)
*  /   \
* _ _ _ _ <-- texels
* X Y Z W <-- result indices (in computedArea.xyzw and computedAreaUncut.xyzw)
*/
void _UnityInternalGetAreaPerTexel_3TexelsWideTriangleFilter(float offset, out float4 computedArea, out float4 computedAreaUncut)
{
    //Compute the exterior areas
    float offset01SquaredHalved = (offset + 0.5) * (offset + 0.5) * 0.5;
    computedAreaUncut.x = computedArea.x = offset01SquaredHalved - offset;
    computedAreaUncut.w = computedArea.w = offset01SquaredHalved;

    //Compute the middle areas
    //For Y : We find the area in Y of as if the left section of the isoceles triangle would
    //intersect the axis between Y and Z (ie where offset = 0).
    computedAreaUncut.y = _UnityInternalGetAreaAboveFirstTexelUnderAIsocelesRectangleTriangle(1.5 - offset);
    //This area is superior to the one we are looking for if (offset < 0) thus we need to
    //subtract the area of the triangle defined by (0,1.5-offset), (0,1.5+offset), (-offset,1.5).
    float clampedOffsetLeft = min(offset,0);
    float areaOfSmallLeftTriangle = clampedOffsetLeft * clampedOffsetLeft;
    computedArea.y = computedAreaUncut.y - areaOfSmallLeftTriangle;

    //We do the same for the Z but with the right part of the isoceles triangle
    computedAreaUncut.z = _UnityInternalGetAreaAboveFirstTexelUnderAIsocelesRectangleTriangle(1.5 + offset);
    float clampedOffsetRight = max(offset,0);
    float areaOfSmallRightTriangle = clampedOffsetRight * clampedOffsetRight;
    computedArea.z = computedAreaUncut.z - areaOfSmallRightTriangle;
}

/**
 * Assuming a isoceles triangle of 1.5 texels height and 3 texels wide lying on 4 texels.
 * This function return the weight of each texels area relative to the full triangle area.
 */
void _UnityInternalGetWeightPerTexel_3TexelsWideTriangleFilter(float offset, out float4 computedWeight)
{
    float4 dummy;
    _UnityInternalGetAreaPerTexel_3TexelsWideTriangleFilter(offset, computedWeight, dummy);
    computedWeight *= 0.44444;//0.44 == 1/(the triangle area)
}

/**
* Assuming a isoceles triangle of 2.5 texel height and 5 texels wide lying on 6 texels.
* This function return the weight of each texels area relative to the full triangle area.
*  /       \
* _ _ _ _ _ _ <-- texels
* 0 1 2 3 4 5 <-- computed area indices (in texelsWeights[])
*/
void _UnityInternalGetWeightPerTexel_5TexelsWideTriangleFilter(float offset, out float3 texelsWeightsA, out float3 texelsWeightsB)
{
    //See _UnityInternalGetAreaPerTexel_3TexelTriangleFilter for details.
    float4 computedArea_From3texelTriangle;
    float4 computedAreaUncut_From3texelTriangle;
    _UnityInternalGetAreaPerTexel_3TexelsWideTriangleFilter(offset, computedArea_From3texelTriangle, computedAreaUncut_From3texelTriangle);

    //Triangle slop is 45 degree thus we can almost reuse the result of the 3 texel wide computation.
    //the 5 texel wide triangle can be seen as the 3 texel wide one but shifted up by one unit/texel.
    //0.16 is 1/(the triangle area)
    texelsWeightsA.x = 0.16 * (computedArea_From3texelTriangle.x);
    texelsWeightsA.y = 0.16 * (computedAreaUncut_From3texelTriangle.y);
    texelsWeightsA.z = 0.16 * (computedArea_From3texelTriangle.y + 1);
    texelsWeightsB.x = 0.16 * (computedArea_From3texelTriangle.z + 1);
    texelsWeightsB.y = 0.16 * (computedAreaUncut_From3texelTriangle.z);
    texelsWeightsB.z = 0.16 * (computedArea_From3texelTriangle.w);
}

/**
* Assuming a isoceles triangle of 3.5 texel height and 7 texels wide lying on 8 texels.
* This function return the weight of each texels area relative to the full triangle area.
*  /           \
* _ _ _ _ _ _ _ _ <-- texels
* 0 1 2 3 4 5 6 7 <-- computed area indices (in texelsWeights[])
*/
void _UnityInternalGetWeightPerTexel_7TexelsWideTriangleFilter(float offset, out float4 texelsWeightsA, out float4 texelsWeightsB)
{
    //See _UnityInternalGetAreaPerTexel_3TexelTriangleFilter for details.
    float4 computedArea_From3texelTriangle;
    float4 computedAreaUncut_From3texelTriangle;
    _UnityInternalGetAreaPerTexel_3TexelsWideTriangleFilter(offset, computedArea_From3texelTriangle, computedAreaUncut_From3texelTriangle);

    //Triangle slop is 45 degree thus we can almost reuse the result of the 3 texel wide computation.
    //the 7 texel wide triangle can be seen as the 3 texel wide one but shifted up by two unit/texel.
    //0.081632 is 1/(the triangle area)
    texelsWeightsA.x = 0.081632 * (computedArea_From3texelTriangle.x);
    texelsWeightsA.y = 0.081632 * (computedAreaUncut_From3texelTriangle.y);
    texelsWeightsA.z = 0.081632 * (computedAreaUncut_From3texelTriangle.y + 1);
    texelsWeightsA.w = 0.081632 * (computedArea_From3texelTriangle.y + 2);
    texelsWeightsB.x = 0.081632 * (computedArea_From3texelTriangle.z + 2);
    texelsWeightsB.y = 0.081632 * (computedAreaUncut_From3texelTriangle.z + 1);
    texelsWeightsB.z = 0.081632 * (computedAreaUncut_From3texelTriangle.z);
    texelsWeightsB.w = 0.081632 * (computedArea_From3texelTriangle.w);
}

// ------------------------------------------------------------------
//  PCF Filtering
// ------------------------------------------------------------------

/**
* PCF gaussian shadowmap filtering based on a 3x3 kernel (9 taps no PCF hardware support)
*/
half UnitySampleShadowmap_PCF3x3NoHardwareSupport(float4 coord, float3 receiverPlaneDepthBias)
{
    half shadow = 1;

#ifdef SHADOWMAPSAMPLER_AND_TEXELSIZE_DEFINED
    // when we don't have hardware PCF sampling, then the above 5x5 optimized PCF really does not work.
    // Fallback to a simple 3x3 sampling with averaged results.
    float2 base_uv = coord.xy;
    float2 ts = _ShadowMapTexture_TexelSize.xy;
    shadow = 0;
    shadow += UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(-ts.x, -ts.y), coord.z, receiverPlaneDepthBias));
    shadow += UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(0, -ts.y), coord.z, receiverPlaneDepthBias));
    shadow += UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(ts.x, -ts.y), coord.z, receiverPlaneDepthBias));
    shadow += UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(-ts.x, 0), coord.z, receiverPlaneDepthBias));
    shadow += UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(0, 0), coord.z, receiverPlaneDepthBias));
    shadow += UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(ts.x, 0), coord.z, receiverPlaneDepthBias));
    shadow += UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(-ts.x, ts.y), coord.z, receiverPlaneDepthBias));
    shadow += UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(0, ts.y), coord.z, receiverPlaneDepthBias));
    shadow += UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(ts.x, ts.y), coord.z, receiverPlaneDepthBias));
    shadow /= 9.0;
#endif

    return shadow;
}

/**
* PCF tent shadowmap filtering based on a 3x3 kernel (optimized with 4 taps)
*/
half UnitySampleShadowmap_PCF3x3Tent(float4 coord, float3 receiverPlaneDepthBias)
{
    half shadow = 1;

#ifdef SHADOWMAPSAMPLER_AND_TEXELSIZE_DEFINED

    #ifndef SHADOWS_NATIVE
        // when we don't have hardware PCF sampling, fallback to a simple 3x3 sampling with averaged results.
        return UnitySampleShadowmap_PCF3x3NoHardwareSupport(coord, receiverPlaneDepthBias);
    #endif

    // tent base is 3x3 base thus covering from 9 to 12 texels, thus we need 4 bilinear PCF fetches
    float2 tentCenterInTexelSpace = coord.xy * _ShadowMapTexture_TexelSize.zw;
    float2 centerOfFetchesInTexelSpace = floor(tentCenterInTexelSpace + 0.5);
    float2 offsetFromTentCenterToCenterOfFetches = tentCenterInTexelSpace - centerOfFetchesInTexelSpace;

    // find the weight of each texel based
    float4 texelsWeightsU, texelsWeightsV;
    _UnityInternalGetWeightPerTexel_3TexelsWideTriangleFilter(offsetFromTentCenterToCenterOfFetches.x, texelsWeightsU);
    _UnityInternalGetWeightPerTexel_3TexelsWideTriangleFilter(offsetFromTentCenterToCenterOfFetches.y, texelsWeightsV);

    // each fetch will cover a group of 2x2 texels, the weight of each group is the sum of the weights of the texels
    float2 fetchesWeightsU = texelsWeightsU.xz + texelsWeightsU.yw;
    float2 fetchesWeightsV = texelsWeightsV.xz + texelsWeightsV.yw;

    // move the PCF bilinear fetches to respect texels weights
    float2 fetchesOffsetsU = texelsWeightsU.yw / fetchesWeightsU.xy + float2(-1.5,0.5);
    float2 fetchesOffsetsV = texelsWeightsV.yw / fetchesWeightsV.xy + float2(-1.5,0.5);
    fetchesOffsetsU *= _ShadowMapTexture_TexelSize.xx;
    fetchesOffsetsV *= _ShadowMapTexture_TexelSize.yy;

    // fetch !
    float2 bilinearFetchOrigin = centerOfFetchesInTexelSpace * _ShadowMapTexture_TexelSize.xy;
    shadow =  fetchesWeightsU.x * fetchesWeightsV.x * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.x, fetchesOffsetsV.x), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.y * fetchesWeightsV.x * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.y, fetchesOffsetsV.x), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.x * fetchesWeightsV.y * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.x, fetchesOffsetsV.y), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.y * fetchesWeightsV.y * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.y, fetchesOffsetsV.y), coord.z, receiverPlaneDepthBias));
#endif

    return shadow;
}

/**
* PCF tent shadowmap filtering based on a 5x5 kernel (optimized with 9 taps)
*/
half UnitySampleShadowmap_PCF5x5Tent(float4 coord, float3 receiverPlaneDepthBias)
{
    half shadow = 1;

#ifdef SHADOWMAPSAMPLER_AND_TEXELSIZE_DEFINED

    #ifndef SHADOWS_NATIVE
        // when we don't have hardware PCF sampling, fallback to a simple 3x3 sampling with averaged results.
        return UnitySampleShadowmap_PCF3x3NoHardwareSupport(coord, receiverPlaneDepthBias);
    #endif

    // tent base is 5x5 base thus covering from 25 to 36 texels, thus we need 9 bilinear PCF fetches
    float2 tentCenterInTexelSpace = coord.xy * _ShadowMapTexture_TexelSize.zw;
    float2 centerOfFetchesInTexelSpace = floor(tentCenterInTexelSpace + 0.5);
    float2 offsetFromTentCenterToCenterOfFetches = tentCenterInTexelSpace - centerOfFetchesInTexelSpace;

    // find the weight of each texel based on the area of a 45 degree slop tent above each of them.
    float3 texelsWeightsU_A, texelsWeightsU_B;
    float3 texelsWeightsV_A, texelsWeightsV_B;
    _UnityInternalGetWeightPerTexel_5TexelsWideTriangleFilter(offsetFromTentCenterToCenterOfFetches.x, texelsWeightsU_A, texelsWeightsU_B);
    _UnityInternalGetWeightPerTexel_5TexelsWideTriangleFilter(offsetFromTentCenterToCenterOfFetches.y, texelsWeightsV_A, texelsWeightsV_B);

    // each fetch will cover a group of 2x2 texels, the weight of each group is the sum of the weights of the texels
    float3 fetchesWeightsU = float3(texelsWeightsU_A.xz, texelsWeightsU_B.y) + float3(texelsWeightsU_A.y, texelsWeightsU_B.xz);
    float3 fetchesWeightsV = float3(texelsWeightsV_A.xz, texelsWeightsV_B.y) + float3(texelsWeightsV_A.y, texelsWeightsV_B.xz);

    // move the PCF bilinear fetches to respect texels weights
    float3 fetchesOffsetsU = float3(texelsWeightsU_A.y, texelsWeightsU_B.xz) / fetchesWeightsU.xyz + float3(-2.5,-0.5,1.5);
    float3 fetchesOffsetsV = float3(texelsWeightsV_A.y, texelsWeightsV_B.xz) / fetchesWeightsV.xyz + float3(-2.5,-0.5,1.5);
    fetchesOffsetsU *= _ShadowMapTexture_TexelSize.xxx;
    fetchesOffsetsV *= _ShadowMapTexture_TexelSize.yyy;

    // fetch !
    float2 bilinearFetchOrigin = centerOfFetchesInTexelSpace * _ShadowMapTexture_TexelSize.xy;
    shadow  = fetchesWeightsU.x * fetchesWeightsV.x * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.x, fetchesOffsetsV.x), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.y * fetchesWeightsV.x * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.y, fetchesOffsetsV.x), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.z * fetchesWeightsV.x * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.z, fetchesOffsetsV.x), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.x * fetchesWeightsV.y * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.x, fetchesOffsetsV.y), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.y * fetchesWeightsV.y * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.y, fetchesOffsetsV.y), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.z * fetchesWeightsV.y * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.z, fetchesOffsetsV.y), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.x * fetchesWeightsV.z * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.x, fetchesOffsetsV.z), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.y * fetchesWeightsV.z * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.y, fetchesOffsetsV.z), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.z * fetchesWeightsV.z * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.z, fetchesOffsetsV.z), coord.z, receiverPlaneDepthBias));
#endif

    return shadow;
}

/**
* PCF tent shadowmap filtering based on a 7x7 kernel (optimized with 16 taps)
*/
half UnitySampleShadowmap_PCF7x7Tent(float4 coord, float3 receiverPlaneDepthBias)
{
    half shadow = 1;

#ifdef SHADOWMAPSAMPLER_AND_TEXELSIZE_DEFINED

    #ifndef SHADOWS_NATIVE
        // when we don't have hardware PCF sampling, fallback to a simple 3x3 sampling with averaged results.
        return UnitySampleShadowmap_PCF3x3NoHardwareSupport(coord, receiverPlaneDepthBias);
    #endif

    // tent base is 7x7 base thus covering from 49 to 64 texels, thus we need 16 bilinear PCF fetches
    float2 tentCenterInTexelSpace = coord.xy * _ShadowMapTexture_TexelSize.zw;
    float2 centerOfFetchesInTexelSpace = floor(tentCenterInTexelSpace + 0.5);
    float2 offsetFromTentCenterToCenterOfFetches = tentCenterInTexelSpace - centerOfFetchesInTexelSpace;

    // find the weight of each texel based on the area of a 45 degree slop tent above each of them.
    float4 texelsWeightsU_A, texelsWeightsU_B;
    float4 texelsWeightsV_A, texelsWeightsV_B;
    _UnityInternalGetWeightPerTexel_7TexelsWideTriangleFilter(offsetFromTentCenterToCenterOfFetches.x, texelsWeightsU_A, texelsWeightsU_B);
    _UnityInternalGetWeightPerTexel_7TexelsWideTriangleFilter(offsetFromTentCenterToCenterOfFetches.y, texelsWeightsV_A, texelsWeightsV_B);

    // each fetch will cover a group of 2x2 texels, the weight of each group is the sum of the weights of the texels
    float4 fetchesWeightsU = float4(texelsWeightsU_A.xz, texelsWeightsU_B.xz) + float4(texelsWeightsU_A.yw, texelsWeightsU_B.yw);
    float4 fetchesWeightsV = float4(texelsWeightsV_A.xz, texelsWeightsV_B.xz) + float4(texelsWeightsV_A.yw, texelsWeightsV_B.yw);

    // move the PCF bilinear fetches to respect texels weights
    float4 fetchesOffsetsU = float4(texelsWeightsU_A.yw, texelsWeightsU_B.yw) / fetchesWeightsU.xyzw + float4(-3.5,-1.5,0.5,2.5);
    float4 fetchesOffsetsV = float4(texelsWeightsV_A.yw, texelsWeightsV_B.yw) / fetchesWeightsV.xyzw + float4(-3.5,-1.5,0.5,2.5);
    fetchesOffsetsU *= _ShadowMapTexture_TexelSize.xxxx;
    fetchesOffsetsV *= _ShadowMapTexture_TexelSize.yyyy;

    // fetch !
    float2 bilinearFetchOrigin = centerOfFetchesInTexelSpace * _ShadowMapTexture_TexelSize.xy;
    shadow  = fetchesWeightsU.x * fetchesWeightsV.x * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.x, fetchesOffsetsV.x), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.y * fetchesWeightsV.x * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.y, fetchesOffsetsV.x), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.z * fetchesWeightsV.x * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.z, fetchesOffsetsV.x), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.w * fetchesWeightsV.x * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.w, fetchesOffsetsV.x), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.x * fetchesWeightsV.y * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.x, fetchesOffsetsV.y), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.y * fetchesWeightsV.y * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.y, fetchesOffsetsV.y), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.z * fetchesWeightsV.y * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.z, fetchesOffsetsV.y), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.w * fetchesWeightsV.y * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.w, fetchesOffsetsV.y), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.x * fetchesWeightsV.z * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.x, fetchesOffsetsV.z), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.y * fetchesWeightsV.z * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.y, fetchesOffsetsV.z), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.z * fetchesWeightsV.z * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.z, fetchesOffsetsV.z), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.w * fetchesWeightsV.z * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.w, fetchesOffsetsV.z), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.x * fetchesWeightsV.w * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.x, fetchesOffsetsV.w), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.y * fetchesWeightsV.w * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.y, fetchesOffsetsV.w), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.z * fetchesWeightsV.w * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.z, fetchesOffsetsV.w), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.w * fetchesWeightsV.w * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.w, fetchesOffsetsV.w), coord.z, receiverPlaneDepthBias));
#endif

    return shadow;
}

/**
* PCF gaussian shadowmap filtering based on a 3x3 kernel (optimized with 4 taps)
*
* Algorithm: http://the-witness.net/news/2013/09/shadow-mapping-summary-part-1/
* Implementation example: http://mynameismjp.wordpress.com/2013/09/10/shadow-maps/
*/
half UnitySampleShadowmap_PCF3x3Gaussian(float4 coord, float3 receiverPlaneDepthBias)
{
    half shadow = 1;

#ifdef SHADOWMAPSAMPLER_AND_TEXELSIZE_DEFINED

    #ifndef SHADOWS_NATIVE
        // when we don't have hardware PCF sampling, fallback to a simple 3x3 sampling with averaged results.
        return UnitySampleShadowmap_PCF3x3NoHardwareSupport(coord, receiverPlaneDepthBias);
    #endif

    const float2 offset = float2(0.5, 0.5);
    float2 uv = (coord.xy * _ShadowMapTexture_TexelSize.zw) + offset;
    float2 base_uv = (floor(uv) - offset) * _ShadowMapTexture_TexelSize.xy;
    float2 st = frac(uv);

    float2 uw = float2(3 - 2 * st.x, 1 + 2 * st.x);
    float2 u = float2((2 - st.x) / uw.x - 1, (st.x) / uw.y + 1);
    u *= _ShadowMapTexture_TexelSize.x;

    float2 vw = float2(3 - 2 * st.y, 1 + 2 * st.y);
    float2 v = float2((2 - st.y) / vw.x - 1, (st.y) / vw.y + 1);
    v *= _ShadowMapTexture_TexelSize.y;

    half sum = 0;

    sum += uw[0] * vw[0] * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(u[0], v[0]), coord.z, receiverPlaneDepthBias));
    sum += uw[1] * vw[0] * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(u[1], v[0]), coord.z, receiverPlaneDepthBias));
    sum += uw[0] * vw[1] * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(u[0], v[1]), coord.z, receiverPlaneDepthBias));
    sum += uw[1] * vw[1] * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(u[1], v[1]), coord.z, receiverPlaneDepthBias));

    shadow = sum / 16.0f;
#endif

    return shadow;
}

/**
* PCF gaussian shadowmap filtering based on a 5x5 kernel (optimized with 9 taps)
*
* Algorithm: http://the-witness.net/news/2013/09/shadow-mapping-summary-part-1/
* Implementation example: http://mynameismjp.wordpress.com/2013/09/10/shadow-maps/
*/
half UnitySampleShadowmap_PCF5x5Gaussian(float4 coord, float3 receiverPlaneDepthBias)
{
    half shadow = 1;

#ifdef SHADOWMAPSAMPLER_AND_TEXELSIZE_DEFINED

    #ifndef SHADOWS_NATIVE
        // when we don't have hardware PCF sampling, fallback to a simple 3x3 sampling with averaged results.
        return UnitySampleShadowmap_PCF3x3NoHardwareSupport(coord, receiverPlaneDepthBias);
    #endif

    const float2 offset = float2(0.5, 0.5);
    float2 uv = (coord.xy * _ShadowMapTexture_TexelSize.zw) + offset;
    float2 base_uv = (floor(uv) - offset) * _ShadowMapTexture_TexelSize.xy;
    float2 st = frac(uv);

    float3 uw = float3(4 - 3 * st.x, 7, 1 + 3 * st.x);
    float3 u = float3((3 - 2 * st.x) / uw.x - 2, (3 + st.x) / uw.y, st.x / uw.z + 2);
    u *= _ShadowMapTexture_TexelSize.x;

    float3 vw = float3(4 - 3 * st.y, 7, 1 + 3 * st.y);
    float3 v = float3((3 - 2 * st.y) / vw.x - 2, (3 + st.y) / vw.y, st.y / vw.z + 2);
    v *= _ShadowMapTexture_TexelSize.y;

    half sum = 0.0f;

    half3 accum = uw * vw.x;
    sum += accum.x * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(u.x, v.x), coord.z, receiverPlaneDepthBias));
    sum += accum.y * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(u.y, v.x), coord.z, receiverPlaneDepthBias));
    sum += accum.z * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(u.z, v.x), coord.z, receiverPlaneDepthBias));

    accum = uw * vw.y;
    sum += accum.x *  UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(u.x, v.y), coord.z, receiverPlaneDepthBias));
    sum += accum.y *  UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(u.y, v.y), coord.z, receiverPlaneDepthBias));
    sum += accum.z *  UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(u.z, v.y), coord.z, receiverPlaneDepthBias));

    accum = uw * vw.z;
    sum += accum.x * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(u.x, v.z), coord.z, receiverPlaneDepthBias));
    sum += accum.y * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(u.y, v.z), coord.z, receiverPlaneDepthBias));
    sum += accum.z * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(u.z, v.z), coord.z, receiverPlaneDepthBias));
    shadow = sum / 144.0f;

#endif

    return shadow;
}

half UnitySampleShadowmap_PCF3x3(float4 coord, float3 receiverPlaneDepthBias)
{
    return UnitySampleShadowmap_PCF3x3Tent(coord, receiverPlaneDepthBias);
}

half UnitySampleShadowmap_PCF5x5(float4 coord, float3 receiverPlaneDepthBias)
{
    return UnitySampleShadowmap_PCF5x5Tent(coord, receiverPlaneDepthBias);
}

half UnitySampleShadowmap_PCF7x7(float4 coord, float3 receiverPlaneDepthBias)
{
    return UnitySampleShadowmap_PCF7x7Tent(coord, receiverPlaneDepthBias);
}

#endif // UNITY_BUILTIN_SHADOW_LIBRARY_INCLUDED

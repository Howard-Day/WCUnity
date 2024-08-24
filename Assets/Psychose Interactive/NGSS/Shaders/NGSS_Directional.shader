// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

// Collects cascaded shadows into screen space buffer
Shader "Hidden/NGSS_Directional" {
Properties {
    _ShadowMapTexture ("", any) = "" {}
    _ODSWorldTexture("", 2D) = "" {}
}

CGINCLUDE

UNITY_DECLARE_SHADOWMAP(_ShadowMapTexture);
#ifndef SHADOWMAPSAMPLER_AND_TEXELSIZE_DEFINED
#define SHADOWMAPSAMPLER_AND_TEXELSIZE_DEFINED
float4 _ShadowMapTexture_TexelSize;		
#endif
sampler2D _ODSWorldTexture;

#include "UnityCG.cginc"
#include "UnityShadowLibrary.cginc"

// Configuration

// Should receiver plane bias be used? This estimates receiver slope using derivatives,
// and tries to tilt the PCF kernel along it. However, since we're doing it in screenspace
// from the depth texture, the derivatives are wrong on edges or intersections of objects,
// leading to possible shadow artifacts. So it's disabled by default.
// See also UnityGetReceiverPlaneDepthBias in UnityShadowLibrary.cginc.
//#define UNITY_USE_RECEIVER_PLANE_BIAS

struct appdata {
    float4 vertex : POSITION;
    float2 texcoord : TEXCOORD0;
#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
    float3 ray0 : TEXCOORD1;
    float3 ray1 : TEXCOORD2;
#else
    float3 ray : TEXCOORD1;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f {

    float4 pos : SV_POSITION;

    // xy uv / zw screenpos
    float4 uv : TEXCOORD0;
    // View space ray, for perspective case
    float3 ray : TEXCOORD1;
    // Orthographic view space positions (need xy as well for oblique matrices)
    float3 orthoPosNear : TEXCOORD2;
    float3 orthoPosFar  : TEXCOORD3;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

v2f vert (appdata v)
{
    v2f o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    float4 clipPos;
#if defined(STEREO_CUBEMAP_RENDER_ON)
    clipPos = mul(UNITY_MATRIX_VP, mul(unity_ObjectToWorld, v.vertex));
#else
    clipPos = UnityObjectToClipPos(v.vertex);
#endif
    o.pos = clipPos;
    o.uv.xy = v.texcoord;

    // unity_CameraInvProjection at the PS level.
    o.uv.zw = ComputeNonStereoScreenPos(clipPos);

    // Perspective case
#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
    o.ray = unity_StereoEyeIndex == 0 ? v.ray0 : v.ray1;
#else
    o.ray = v.ray;
#endif

    // To compute view space position from Z buffer for orthographic case,
    // we need different code than for perspective case. We want to avoid
    // doing matrix multiply in the pixel shader: less operations, and less
    // constant registers used. Particularly with constant registers, having
    // unity_CameraInvProjection in the pixel shader would push the PS over SM2.0
    // limits.
    clipPos.y *= _ProjectionParams.x;
    float3 orthoPosNear = mul(unity_CameraInvProjection, float4(clipPos.x,clipPos.y,-1,1)).xyz;
    float3 orthoPosFar  = mul(unity_CameraInvProjection, float4(clipPos.x,clipPos.y, 1,1)).xyz;
    orthoPosNear.z *= -1;
    orthoPosFar.z *= -1;
    o.orthoPosNear = orthoPosNear;
    o.orthoPosFar = orthoPosFar;

    return o;
}

// ------------------------------------------------------------------
//  Helpers
// ------------------------------------------------------------------
UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
// sizes of cascade projections, relative to first one
float4 unity_ShadowCascadeScales;

//
// Keywords based defines
//
#if defined (SHADOWS_SPLIT_SPHERES)
    #define GET_CASCADE_WEIGHTS(wpos, z, dsqr)	getCascadeWeights_splitSpheres(wpos, dsqr) 
#else
    #define GET_CASCADE_WEIGHTS(wpos, z, dsqr)    getCascadeWeights( wpos, z )
#endif

#if defined (SHADOWS_SINGLE_CASCADE)
    #define GET_SHADOW_COORDINATES(wpos,cascadeWeights) getShadowCoord_SingleCascade(wpos)
#else
    #define GET_SHADOW_COORDINATES(wpos,cascadeWeights) getShadowCoord(wpos,cascadeWeights)
#endif

/**
 * Gets the cascade weights based on the world position of the fragment.
 * Returns a float4 with only one component set that corresponds to the appropriate cascade.
 */
inline fixed4 getCascadeWeights(float3 wpos, float z)
{
    fixed4 zNear = float4( z >= _LightSplitsNear );
    fixed4 zFar = float4( z < _LightSplitsFar );
    fixed4 weights = zNear * zFar;
    return weights;
}

/**
 * Gets the cascade weights based on the world position of the fragment and the poisitions of the split spheres for each cascade.
 * Returns a float4 with only one component set that corresponds to the appropriate cascade.
 */
inline fixed4 getCascadeWeights_splitSpheres(float3 wpos, out float4 dsqr)
{
    float3 fromCenter0 = wpos.xyz - unity_ShadowSplitSpheres[0].xyz;
    float3 fromCenter1 = wpos.xyz - unity_ShadowSplitSpheres[1].xyz;
    float3 fromCenter2 = wpos.xyz - unity_ShadowSplitSpheres[2].xyz;
    float3 fromCenter3 = wpos.xyz - unity_ShadowSplitSpheres[3].xyz;
    dsqr = float4(dot(fromCenter0,fromCenter0), dot(fromCenter1,fromCenter1), dot(fromCenter2,fromCenter2), dot(fromCenter3,fromCenter3));
	fixed4 weights = float4(dsqr < unity_ShadowSplitSqRadii);
	//dsqr = float4(length(fromCenter0), length(fromCenter1), length(fromCenter2), length(fromCenter3));
    //fixed4 weights = float4(dsqr < sqrt(unity_ShadowSplitSqRadii));
    weights.yzw = saturate(weights.yzw - weights.xyz);
    return weights;
}

/**
 * Returns the shadowmap coordinates for the given fragment based on the world position and z-depth.
 * These coordinates belong to the shadowmap atlas that contains the maps for all cascades.
 */
inline float4 getShadowCoord( float4 wpos, fixed4 cascadeWeights )
{
    float3 sc0 = mul (unity_WorldToShadow[0], wpos).xyz;
    float3 sc1 = mul (unity_WorldToShadow[1], wpos).xyz;
    float3 sc2 = mul (unity_WorldToShadow[2], wpos).xyz;
    float3 sc3 = mul (unity_WorldToShadow[3], wpos).xyz;
	
    float4 shadowMapCoordinate = float4((sc0 * cascadeWeights.x) + (sc1 * cascadeWeights.y) + (sc2 * cascadeWeights.z) + (sc3 * cascadeWeights.w), 1);
#if defined(UNITY_REVERSED_Z)
    float  noCascadeWeights = 1 - dot(cascadeWeights, float4(1, 1, 1, 1));
    shadowMapCoordinate.z += noCascadeWeights;
#endif
    return shadowMapCoordinate;
}

inline float4 getShadowCoordFinal( float3 sc0, float3 sc1, float3 sc2, float3 sc3, fixed4 cascadeWeights )
{
	//float3 sc0 = mul (unity_WorldToShadow[0], wpos).xyz;
    //float3 sc1 = mul (unity_WorldToShadow[1], wpos).xyz;
    //float3 sc2 = mul (unity_WorldToShadow[2], wpos).xyz;
    //float3 sc3 = mul (unity_WorldToShadow[3], wpos).xyz;
	
    float4 shadowMapCoordinate = float4((sc0 * cascadeWeights.x) + (sc1 * cascadeWeights.y) + (sc2 * cascadeWeights.z) + (sc3 * cascadeWeights.w), 1);
#if defined(UNITY_REVERSED_Z)
    float  noCascadeWeights = 1 - dot(cascadeWeights, float4(1, 1, 1, 1));
    shadowMapCoordinate.z += noCascadeWeights;
#endif
    return shadowMapCoordinate;
}

/**
 * Same as the getShadowCoord; but optimized for single cascade
 */
inline float4 getShadowCoord_SingleCascade( float4 wpos )
{
    return float4( mul (unity_WorldToShadow[0], wpos).xyz, 0);
}

/**
* Get camera space coord from depth and inv projection matrices
*/
inline float3 computeCameraSpacePosFromDepthAndInvProjMat(v2f i)
{
    float zdepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv.xy);

    #if defined(UNITY_REVERSED_Z)
        zdepth = 1 - zdepth;
    #endif

    // View position calculation for oblique clipped projection case.
    // this will not be as precise nor as fast as the other method
    // (which computes it from interpolated ray & depth) but will work
    // with funky projections.
    float4 clipPos = float4(i.uv.zw, zdepth, 1.0);
    clipPos.xyz = 2.0f * clipPos.xyz - 1.0f;
    float4 camPos = mul(unity_CameraInvProjection, clipPos);
    camPos.xyz /= camPos.w;
    camPos.z *= -1;
    return camPos.xyz;
}

/**
* Get camera space coord from depth and info from VS
*/
inline float3 computeCameraSpacePosFromDepthAndVSInfo(v2f i)
{
    float zdepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv.xy);

    // 0..1 linear depth, 0 at camera, 1 at far plane.
    float depth = lerp(Linear01Depth(zdepth), zdepth, unity_OrthoParams.w);
#if defined(UNITY_REVERSED_Z)
    zdepth = 1 - zdepth;
#endif

    // view position calculation for perspective & ortho cases
    float3 vposPersp = i.ray * depth;
    float3 vposOrtho = lerp(i.orthoPosNear, i.orthoPosFar, zdepth);
    // pick the perspective or ortho position as needed
    float3 camPos = lerp(vposPersp, vposOrtho, unity_OrthoParams.w);
    return camPos.xyz;
}

inline float3 computeCameraSpacePosFromDepth(v2f i);

//NGSS START--------------------------------------------------------------------------------------------------------------------------------------------------------------

// Note: Do not alter any of these or you risk shader compile errors

uniform float4 NGSS_CASCADES_SPLITS;
uniform float NGSS_CASCADES_COUNT = 4;
uniform float NGSS_CASCADE_BLEND_DISTANCE = 0.25;
uniform float NGSS_CASCADES_SOFTNESS_NORMALIZATION = 1;

uniform float NGSS_NOISE_TO_DITHERING_SCALE_DIR = 1;

uniform int NGSS_FILTER_SAMPLERS_DIR = 32;
uniform int NGSS_TEST_SAMPLERS_DIR = 16;

//uniform float NGSS_DOT_ANGLE = 1.0;

//INLINE SAMPLING
#if (SHADER_TARGET < 30  || UNITY_VERSION <= 570 || defined(SHADER_API_D3D9) || defined(SHADER_API_GLES) || defined(SHADER_API_PSP2) || defined(SHADER_API_N3DS))
	//#define NO_INLINE_SAMPLERS_SUPPORT
	//#define NGSS_NO_SUPPORT_DIR
#elif (defined(NGSS_PCSS_FILTER_DIR) && !defined(UNITY_NO_SCREENSPACE_SHADOWS))
	#define NGSS_CAN_USE_PCSS_FILTER_DIR
	SamplerState my_linear_clamp_smp2;
#endif

//uniform float NGSS_RECEIVER_PLANE_MIN_FRACTIONAL_ERROR_DIR = 0.01;
uniform float NGSS_DIR_SAMPLING_DISTANCE = 75.0;
//uniform float NGSS_DIR_NOISE_DISTANCE = 15.0;
uniform float NGSS_GLOBAL_SOFTNESS = 0.01;
uniform float NGSS_PCSS_FILTER_DIR_MIN = 0.05;
uniform float NGSS_PCSS_FILTER_DIR_MAX = 0.25;
uniform float NGSS_BIAS_FADE_DIR = 0.001;

uniform sampler2D _BlueNoiseTextureDir;
uniform float4 _BlueNoiseTextureDir_TexelSize;

#define ditherPatternDir float4x4(0.0,0.5,0.125,0.625, 0.75,0.22,0.875,0.375, 0.1875,0.6875,0.0625,0.5625, 0.9375,0.4375,0.8125,0.3125)

float2 VogelDiskSampleDir(int sampleIndex, int samplesCount, float phi)
{
	//float phi = 3.14159265359f;//UNITY_PI;
	float GoldenAngle = 2.4f;

	//float r = sqrt(sampleIndex + 0.5f) / sqrt(samplesCount);
	float r = sqrt(sampleIndex + 0.5f) / sqrt(samplesCount);
	float theta = sampleIndex * GoldenAngle + phi;

	float sine, cosine;
	sincos(theta, sine, cosine);
	
	return float2(r * cosine, r * sine);
}
/*
float OrderedDitheringDir(float x, float y, float c0)
{
    //dither matrix reference: https://en.wikipedia.org/wiki/Ordered_dithering
    const static float dither[64] = {
        0, 32, 8, 40, 2, 34, 10, 42,
        48, 16, 56, 24, 50, 18, 58, 26 ,
        12, 44, 4, 36, 14, 46, 6, 38 ,
        60, 28, 52, 20, 62, 30, 54, 22,
        3, 35, 11, 43, 1, 33, 9, 41,
        51, 19, 59, 27, 49, 17, 57, 25,
        15, 47, 7, 39, 13, 45, 5, 37,
        63, 31, 55, 23, 61, 29, 53, 21 };

    int xMat = int(x) & 7;
    int yMat = int(y) & 7;

    float limit = (dither[yMat * 8 + xMat] + 11.0) / 64.0;
    //could also use saturate(step(0.995, c0) + limit*(c0));
    //original step(limit, c0 + 0.01);

    return lerp(limit*c0, 1.0, c0);
}*/

float InterleavedGradientNoiseDir(float2 position_screen)
{
	//dithering
	float ditherValue = ditherPatternDir[position_screen.x * _ScreenParams.x % 4][position_screen.y * _ScreenParams.y % 4];
	//float ditherValue = OrderedDitheringDir(position_screen.x * _ScreenParams.x, position_screen.y * _ScreenParams.y, 0.25) * UNITY_FOUR_PI;
	
	//white noise
	//float2 magic = position_screen.xy * 10;//float2( 23.14069263277926f, 2.665144142690225f);//float2(0.06711056f, 0.00583715f)
    //return frac(sin(dot(magic, magic)) * 43758.5453f) * UNITY_TWO_PI;
	
	//blue noise
	//float noiseValue = tex2D(unity_RandomRotation16, position_screen.xy * _BlueNoiseTextureDir_TexelSize.xy * _ScreenParams.xy).r * 100;
	float noiseValue = tex2D(_BlueNoiseTextureDir, position_screen.xy * _BlueNoiseTextureDir_TexelSize.xy * _ScreenParams.xy).r;
	return lerp(noiseValue, ditherValue, NGSS_NOISE_TO_DITHERING_SCALE_DIR) * UNITY_TWO_PI;
}

float3 ReceiverPlaneDepthBiasNGSS(float3 shadowCoord, float biasMultiply)
{
    // Should receiver plane bias be used? This estimates receiver slope using derivatives, and tries to tilt the PCF kernel along it. However, when doing it in screenspace from the depth texture
    // (ie all light in deferred and directional light in both forward and deferred), the derivatives are wrong on edges or intersections of objects, leading to shadow artifacts. Thus it is disabled by default.
    float3 biasUVZ = 0;

#if defined(NGSS_USE_RECEIVER_PLANE_BIAS) && defined(SHADOWMAPSAMPLER_AND_TEXELSIZE_DEFINED)
    float3 dx = ddx(shadowCoord);
    float3 dy = ddy(shadowCoord);

    biasUVZ.x = dy.y * dx.z - dx.y * dy.z;
    biasUVZ.y = dx.x * dy.z - dy.x * dx.z;
    biasUVZ.xy *= biasMultiply / ((dx.x * dy.y) - (dx.y * dy.x));

    // Static depth biasing to make up for incorrect fractional sampling on the shadow map grid.    
    float fractionalSamplingError = dot(_ShadowMapTexture_TexelSize.xy, abs(biasUVZ.xy));
    biasUVZ.z = -min(fractionalSamplingError, 0.01);//NGSS_RECEIVER_PLANE_MIN_FRACTIONAL_ERROR_DIR
    #if defined(UNITY_REVERSED_Z)
        biasUVZ.z *= -1;
    #endif
#endif

    return biasUVZ;
}

//Combines the different components of a shadow coordinate and returns the final coordinate. See ReceiverPlaneDepthBiasNGSS
float3 CombineShadowCoordsNGSS(float2 baseUV, float2 deltaUV, float depth, float3 receiverPlaneDepthBias)
{
    float3 uv = float3(baseUV + deltaUV, depth + receiverPlaneDepthBias.z);
    uv.z += dot(deltaUV, receiverPlaneDepthBias.xy);
    return uv;
}

/********************************************************************************/

#if defined(NGSS_CAN_USE_PCSS_FILTER_DIR)
//BlockerSearch
float2 BlockerSearch(float2 uv, float receiver, float searchUV, float3 receiverPlaneDepthBias, float Sampler_Number, float randPied, uint cascadeIndex)
{
	float avgBlockerDepth = 0.0;
	float numBlockers = 0.0;
	float blockerSum = 0.0;
	float depth = _ShadowMapTexture.SampleLevel(my_linear_clamp_smp2, uv.xy, 0.0);

	int samplers = Sampler_Number;// / (cascadeIndex / 2);
	for (int i = 0; i < samplers; i++)
	{
		float2 offset = VogelDiskSampleDir(i, samplers, randPied) * searchUV;
		float3 biasedCoords = float3(uv + offset, receiver);
		
		#if defined(NGSS_USE_RECEIVER_PLANE_BIAS)
		biasedCoords = CombineShadowCoordsNGSS(uv.xy, offset, receiver, receiverPlaneDepthBias);
		#endif
		
		#if !defined(SHADOWS_SINGLE_CASCADE)
		biasedCoords.xy = clamp(biasedCoords.xy, 0, cascadeIndex / NGSS_CASCADES_COUNT * 4 * 0.499);//make sure we are not sampling outside the current cascade
		#endif
		
		float shadowMapDepth = _ShadowMapTexture.SampleLevel(my_linear_clamp_smp2, biasedCoords.xy, 0.0);
		//_ShadowMapTexture.SampleCmpLevelZero(sampler_ShadowMapTexture, biasedCoords.xy, biasedCoords.z);// works with HLSL syntax (probably not with GLSL)
		/*
		#if defined(UNITY_REVERSED_Z)
		if (shadowMapDepth >= biasedCoords.z)
		#else
		if (shadowMapDepth <= biasedCoords.z)
		#endif
		{
			blockerSum += shadowMapDepth;
			numBlockers++;
		}
		*/
		//No conditional branching
		#if defined(UNITY_REVERSED_Z)
		float sum = shadowMapDepth >= biasedCoords.z;
		blockerSum += shadowMapDepth * sum;
		numBlockers += sum;
		#else
		float sum = shadowMapDepth <= biasedCoords.z;
		blockerSum += shadowMapDepth * sum;
		numBlockers += sum;
		#endif
		
	}

	avgBlockerDepth = blockerSum / numBlockers;
	/*
	#if defined(UNITY_REVERSED_Z)
	avgBlockerDepth = max(depth, blockerSum / numBlockers);
	#else
	avgBlockerDepth = min(depth, blockerSum / numBlockers);
	#endif
	*/
#if defined(UNITY_REVERSED_Z)
	avgBlockerDepth = 1.0 - avgBlockerDepth;
#endif

	return float2(avgBlockerDepth, numBlockers);
}
#endif//NGSS_CAN_USE_PCSS_FILTER_DIR

//PCF
float PCF_FilterDir(float2 uv, float receiver, float diskRadius, float3 receiverPlaneDepthBias, float Sampler_Number, float randPied, uint cascadeIndex)
{
	float sum = 0.0f;
	//if(cascadeIndex == 4)
		//return 0;
	int samplers = Sampler_Number;// / (cascadeIndex / 2);
	for (int i = 0; i < samplers; i++)
	{
		float2 offset = VogelDiskSampleDir(i, samplers, randPied) * diskRadius;
		float3 biasedCoords = float3(uv + offset, receiver);
		
		#if defined(NGSS_USE_RECEIVER_PLANE_BIAS)
		biasedCoords = CombineShadowCoordsNGSS(uv.xy, offset, receiver, receiverPlaneDepthBias);
		#endif
		
		#if !defined(SHADOWS_SINGLE_CASCADE)
		biasedCoords.xy = clamp(biasedCoords.xy, 0, cascadeIndex / NGSS_CASCADES_COUNT * 4 * 0.499);//make sure we are not sampling outside the current cascade
		#endif
		
		float value = UNITY_SAMPLE_SHADOW(_ShadowMapTexture, biasedCoords);
		sum += value;
	}

	return sum / samplers;
}

//Main Function
float NGSS_Main(float4 coord, float3 receiverPlaneDepthBias, float2 screenPos, float worldDst, uint cascadeIndex)
{
	float sampler_dst = (1.0 - min(worldDst, NGSS_DIR_SAMPLING_DISTANCE) / NGSS_DIR_SAMPLING_DISTANCE);
	
	float randPied = InterleavedGradientNoiseDir(screenPos) / max(1.0, worldDst);//* (1.0 - min(worldDst, NGSS_DIR_NOISE_DISTANCE) / NGSS_DIR_NOISE_DISTANCE);
	#if defined(SHADOWS_SPLIT_SPHERES)
	float shadowSoftness = clamp(NGSS_GLOBAL_SOFTNESS, 0.001, 0.25);
	#else
	float3 viewDir = mul((float3x3)unity_CameraToWorld, float3(0,0,1));
	float DOT_ANGLE = 1 - abs(dot(viewDir, float3(0.0, 1.0, 0.0)));
	float shadowSoftness = NGSS_GLOBAL_SOFTNESS * 0.005 / clamp(DOT_ANGLE, 0.1, 1.0);
	#endif
	
	float2 uv = coord.xy;
	float receiver = coord.z;// - ditherValue;
	float shadow = 1.0;
	float diskRadius = 0.01;
	
#if !defined(SHADOWS_SINGLE_CASCADE)
//reduce the softness of consecutive cascades (stiching)
#if defined(NGSS_CAN_USE_PCSS_FILTER_DIR)
	shadowSoftness /= lerp(cascadeIndex, clamp(NGSS_CASCADES_SPLITS[cascadeIndex - 1] / 0.1, 0, 3), NGSS_CASCADES_SOFTNESS_NORMALIZATION);//clamping to 4 will soft all cascades but its too hard on PCSS so skip 4th cascade
#else
	shadowSoftness /= lerp(cascadeIndex, NGSS_CASCADES_SPLITS[cascadeIndex - 1] / 0.1, NGSS_CASCADES_SOFTNESS_NORMALIZATION);
#endif
#endif//SHADOWS_SINGLE_CASCADE
	
	int samplers_test = clamp(NGSS_TEST_SAMPLERS_DIR * sampler_dst, 4, 128);//adding a minimal sampling value to avoid black shadowed light
	//int samplers_test = clamp(NGSS_TEST_SAMPLERS_DIR, 4, 128);

#if defined(NGSS_CAN_USE_PCSS_FILTER_DIR)
	float2 blockerResults = BlockerSearch(uv, receiver, shadowSoftness * 0.5, receiverPlaneDepthBias, samplers_test, randPied, cascadeIndex);
	//return blockerResults.x;//visualizing blockerResults
	if (blockerResults.y == 0.0)//There are no occluders so early out (this saves filtering)
		return 1.0;
//#if defined(NGSS_USE_EARLY_BAILOUT_OPTIMIZATION_DIR)
	//else if (blockerResults.y == samplers_test)//There are 100% occluders so early out (this saves filtering but introduces artefacts)
		//return 0.0;//can sample 4 pixels instead of just returning 0
//#endif	

//PCSS FORMULA: penumbraSize = (receiver - avBlocker) * lightSize / receiver;
#if defined(UNITY_REVERSED_Z)
	float penumbra = ((1.0 - receiver) - blockerResults.x) / (1 - blockerResults.x);
#else
	float penumbra = (receiver - blockerResults.x) / blockerResults.x;
#endif
	
	//diskRadius = clamp(penumbra, NGSS_PCSS_FILTER_DIR_MIN, NGSS_PCSS_FILTER_DIR_MAX) * shadowSoftness;
	diskRadius = lerp(NGSS_PCSS_FILTER_DIR_MIN, NGSS_PCSS_FILTER_DIR_MAX, penumbra) * shadowSoftness;
	
#else//NO PCSS FILTERING
	#if defined(UNITY_NO_SCREENSPACE_SHADOWS)
	diskRadius = shadowSoftness * 0.03125;
	#else
	diskRadius = shadowSoftness * 0.125;
	#endif
	
//#endif
#endif//NGSS_CAN_USE_PCSS_FILTER_DIR

	shadow = PCF_FilterDir(uv, receiver, diskRadius, receiverPlaneDepthBias, samplers_test, randPied, cascadeIndex);
	if (shadow == 1.0)//If all pixels are lit early bail out
		return 1.0;
	else if (shadow == 0.0)//If all pixels are shadowed early bail out (introduces tiny black pixels, not really visible)
		return 0.0;
	
	int samplers = clamp(NGSS_FILTER_SAMPLERS_DIR * sampler_dst, 4, 128);//adding a minimal sampling value to avoid black shadowed light
	//int samplers = clamp(NGSS_FILTER_SAMPLERS_DIR, 4, 128);

	//float Sampler_Number = (int)clamp(Sampler_Number * (diskRadius / NGSS_GLOBAL_SOFTNESS), Sampler_Number * 0.5, Sampler_Number);
	shadow = PCF_FilterDir(uv, receiver, diskRadius, receiverPlaneDepthBias, samplers, randPied, cascadeIndex);
	
	//shadow = 3*(shadow)^2-2*(shadow)^3
	
	return shadow;	
}

//Hard shadow
fixed4 frag_hard (v2f i) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); // required for sampling the correct slice of the shadow map render texture array
    float4 wpos;
    float3 vpos;

#if defined(STEREO_CUBEMAP_RENDER_ON)
    wpos.xyz = tex2D(_ODSWorldTexture, i.uv.xy).xyz;
    wpos.w = 1.0f;
    vpos = mul(unity_WorldToCamera, wpos).xyz;
#else
    vpos = computeCameraSpacePosFromDepth(i);
    wpos = mul (unity_CameraToWorld, float4(vpos,1));
#endif
	float4 dsqr;
    //fixed4 cascadeWeights = GET_CASCADE_WEIGHTS (wpos, vpos.z);
	fixed4 cascadeWeights = GET_CASCADE_WEIGHTS(wpos, vpos.z, dsqr);
    float4 shadowCoord = GET_SHADOW_COORDINATES(wpos, cascadeWeights);

    //1 tap hard shadow
    fixed shadow = UNITY_SAMPLE_SHADOW(_ShadowMapTexture, shadowCoord);
    shadow = lerp(_LightShadowData.r, 1.0, shadow);

    fixed4 res = shadow;
    return res;
}

//Soft shadow
fixed4 frag_pcfSoft(v2f i) : SV_Target
{
//#if defined(NGSS_HARD_SHADOWS_DIR)
	//Hard shadows from soft shadows? muhahaha ^^
	//return frag_hard(i);
//#endif
	
	//Return one if you want only ContactShadows, keep in mind that the cascaded depth are still rendered
	//return 1.0;
	
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); // required for sampling the correct slice of the shadow map render texture array
    float4 wpos;
    float3 vpos;

#if defined(STEREO_CUBEMAP_RENDER_ON)
    wpos.xyz = tex2D(_ODSWorldTexture, i.uv.xy).xyz;
    wpos.w = 1.0f;
    vpos = mul(unity_WorldToCamera, wpos).xyz;
#else
    vpos = computeCameraSpacePosFromDepth(i);

    // sample the cascade the pixel belongs to
    wpos = mul(unity_CameraToWorld, float4(vpos,1));
#endif
	
	float4 dsqr;
    fixed4 cascadeWeights = GET_CASCADE_WEIGHTS(wpos, vpos.z, dsqr);
	//fixed4 cascadeWeights = GET_CASCADE_WEIGHTS(wpos, vpos.z);//linear
    //float4 coord = GET_SHADOW_COORDINATES(wpos, cascadeWeights);
	float3 sc0, sc1, sc2, sc3;
#if defined (SHADOWS_SINGLE_CASCADE)
    float4 coord = getShadowCoord_SingleCascade(wpos);
#else
	sc0 = mul (unity_WorldToShadow[0], wpos).xyz;
    sc1 = mul (unity_WorldToShadow[1], wpos).xyz;
    sc2 = mul (unity_WorldToShadow[2], wpos).xyz;
    sc3 = mul (unity_WorldToShadow[3], wpos).xyz;	
    float4 coord = getShadowCoordFinal(sc0, sc1, sc2, sc3, cascadeWeights);
#endif
	
    float3 receiverPlaneDepthBias = 0.0;
#ifdef NGSS_USE_RECEIVER_PLANE_BIAS
    // Reveiver plane depth bias: need to calculate it based on shadow coordinate
    // as it would be in first cascade; otherwise derivatives
    // at cascade boundaries will be all wrong. So compute
    // it from cascade 0 UV, and scale based on which cascade we're in.
	#if defined (SHADOWS_SINGLE_CASCADE)
	float3 coordCascade0 = coord;
	#else
    float3 coordCascade0 = getShadowCoord_SingleCascade(wpos);
	#endif
    float biasMultiply = dot(cascadeWeights,unity_ShadowCascadeScales);
    receiverPlaneDepthBias = ReceiverPlaneDepthBiasNGSS(coordCascade0.xyz, biasMultiply);
#endif
/*
	//Reconstructing screen position using depth
	float zdepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv.xy);
#if defined(UNITY_REVERSED_Z)
	zdepth = 1 - zdepth;
#endif
	float4 clipPos = float4(i.uv.zw, zdepth, 1.0);
*/
	fixed cascadeIndex = 1;
#if !defined(SHADOWS_SINGLE_CASCADE)
	cascadeIndex += 4 - dot(cascadeWeights, half4(4, 3, 2, 1));
#endif
	
	float wdst = distance(_WorldSpaceCameraPos, wpos.xyz);
	
	//float shadow = UnitySampleShadowmap_PCF7x7Tent(coord, receiverPlaneDepthBias);
	float shadow = NGSS_Main(coord, receiverPlaneDepthBias, i.uv.xy, wdst, cascadeIndex);

	// Blend between shadow cascades if enabled. No need when 1 cascade
#if defined(NGSS_USE_CASCADE_BLENDING) && !defined(SHADOWS_SINGLE_CASCADE)
	
#if defined(SHADOWS_SPLIT_SPHERES)

	float3 wdir = wpos - _WorldSpaceCameraPos;
    half4 z4 = dsqr / unity_ShadowSplitSqRadii.xyzw;
    z4 = ( unity_ShadowSplitSqRadii > 0 ) ? z4.xyzw : ( 0 ).xxxx;
    z4 *= dsqr < dot( wdir, wdir ).xxxx;	
#else
	half4 z4 = (float4(vpos.z, vpos.z, vpos.z, vpos.z) - _LightSplitsNear) / (_LightSplitsFar - _LightSplitsNear);
#endif

	half alpha = saturate(dot(z4 * cascadeWeights, half4(1, 1, 1, 1)));
	//alpha = saturate(alpha);
		
	UNITY_BRANCH
	if (alpha > 1.0 - NGSS_CASCADE_BLEND_DISTANCE)
	{
		// get alpha to 0..1 range over the blend distance
		alpha = (alpha - (1.0 - NGSS_CASCADE_BLEND_DISTANCE)) / NGSS_CASCADE_BLEND_DISTANCE;
		
		// sample next cascade
		cascadeWeights = fixed4(0, cascadeWeights.xyz);
		//coord = GET_SHADOW_COORDINATES(wpos, cascadeWeights);
		coord = getShadowCoordFinal(sc0, sc1, sc2, sc3, cascadeWeights);

#if defined(NGSS_USE_RECEIVER_PLANE_BIAS)
		biasMultiply = dot(cascadeWeights, unity_ShadowCascadeScales);
		receiverPlaneDepthBias = ReceiverPlaneDepthBiasNGSS(coordCascade0.xyz, biasMultiply);
#endif

		//half shadowNextCascade = UnitySampleShadowmap_PCF3x3(coord, receiverPlaneDepthBias);
		half shadowNextCascade = NGSS_Main(coord, receiverPlaneDepthBias, i.uv.xy, wdst, cascadeIndex + 1);
		
		//shadow = lerp(shadow, min(shadow, shadowNextCascade), alpha);//saturate(alpha)
		shadow = lerp(shadow, shadowNextCascade, alpha);//saturate(alpha)
	}
	
#endif
	
	//return lerp(_LightShadowData.r, 1.0, shadow);
	return shadow + _LightShadowData.r;
}
ENDCG

// ----------------------------------------------------------------------------------------
// Subshaders that does NGSS filterings while collecting shadows.
// Compatible with: DX11, DX12, GLCORE, PS4, XB1, GLES3.0, SWITCH, Metal, Vulkan and equivalent SM3.0 and newer APIs

SubShader
{
	Tags { "ShadowmapFilter" = "HardShadow" }
	Pass
	{
		ZWrite Off ZTest Always Cull Off

		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag_hard
		#pragma multi_compile_shadowcollector
		#pragma exclude_renderers gles d3d9
		
		inline float3 computeCameraSpacePosFromDepth(v2f i)
		{
			return computeCameraSpacePosFromDepthAndVSInfo(i);
		}
		ENDCG
	}
}
// This version does inv projection at the PS level, slower and less precise however more general.
SubShader
{
	Tags { "ShadowmapFilter" = "HardShadow_FORCE_INV_PROJECTION_IN_PS" }
	Pass
	{
		ZWrite Off ZTest Always Cull Off

		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag_hard
		#pragma multi_compile_shadowcollector
		#pragma exclude_renderers gles d3d9
		
		inline float3 computeCameraSpacePosFromDepth(v2f i)
		{
			return computeCameraSpacePosFromDepthAndInvProjMat(i);
		}
		ENDCG
	}
}
Subshader
{
	Tags {"ShadowmapFilter" = "PCF_SOFT"}//Unity 2017
	Pass
	{
		ZWrite Off ZTest Always Cull Off

		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag_pcfSoft
		#pragma multi_compile _ NGSS_PCSS_FILTER_DIR
		#pragma multi_compile _ NGSS_USE_CASCADE_BLENDING
		//#pragma shader_feature NGSS_USE_EARLY_BAILOUT_OPTIMIZATION_DIR
		//#pragma shader_feature NGSS_USE_BIAS_FADE_DIR
		//#pragma shader_feature NGSS_HARD_SHADOWS_DIR
		#pragma multi_compile _ NGSS_USE_RECEIVER_PLANE_BIAS
		#pragma exclude_renderers gles d3d9
		#pragma multi_compile_shadowcollector
		#pragma target 3.0

		inline float3 computeCameraSpacePosFromDepth(v2f i)
		{
			return computeCameraSpacePosFromDepthAndVSInfo(i);
		}
		ENDCG
	}
}
// This version does inv projection at the PS level, slower and less precise however more general.
Subshader
{
	Tags{ "ShadowmapFilter" = "PCF_SOFT_FORCE_INV_PROJECTION_IN_PS" }//Unity 2017
	Pass
	{
		ZWrite Off ZTest Always Cull Off

		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag_pcfSoft
		#pragma multi_compile _ NGSS_PCSS_FILTER_DIR
		#pragma multi_compile _ NGSS_USE_CASCADE_BLENDING
		//#pragma shader_feature NGSS_USE_EARLY_BAILOUT_OPTIMIZATION_DIR
		//#pragma shader_feature NGSS_USE_BIAS_FADE_DIR		
		//#pragma shader_feature NGSS_HARD_SHADOWS_DIR
		#pragma multi_compile _ NGSS_USE_RECEIVER_PLANE_BIAS
		#pragma exclude_renderers gles d3d9
		#pragma multi_compile_shadowcollector
		#pragma target 3.0

		inline float3 computeCameraSpacePosFromDepth(v2f i)
		{
			return computeCameraSpacePosFromDepthAndInvProjMat(i);
		}
		ENDCG
	}
}
Fallback Off
}
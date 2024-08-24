#ifndef UNITY_DEFERRED_LIBRARY_INCLUDED
#define UNITY_DEFERRED_LIBRARY_INCLUDED

// Deferred lighting / shading helpers


// --------------------------------------------------------
// Vertex shader

struct unity_v2f_deferred {
    float4 pos : SV_POSITION;
    float4 uv : TEXCOORD0;
    float3 ray : TEXCOORD1;
};

float _LightAsQuad;

unity_v2f_deferred vert_deferred (float4 vertex : POSITION, float3 normal : NORMAL)
{
    unity_v2f_deferred o;
    o.pos = UnityObjectToClipPos(vertex);
    o.uv = ComputeScreenPos(o.pos);
    o.ray = UnityObjectToViewPos(vertex) * float3(-1,-1,1);

    // normal contains a ray pointing from the camera to one of near plane's
    // corners in camera space when we are drawing a full screen quad.
    // Otherwise, when rendering 3D shapes, use the ray calculated here.
    o.ray = lerp(o.ray, normal, _LightAsQuad);

    return o;
}


// --------------------------------------------------------
// Shared uniforms


UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

float4 _LightDir;
float4 _LightPos;
float4 _LightColor;
float4 unity_LightmapFade;
float4x4 unity_WorldToLight;
sampler2D_float _LightTextureB0;

#if defined (POINT_COOKIE)
samplerCUBE_float _LightTexture0;
#else
sampler2D_float _LightTexture0;
#endif

#if defined (SHADOWS_SCREEN)
sampler2D _ShadowMapTexture;
float4 _ShadowMapTexture_TexelSize;	
#endif

#if defined (SHADOWS_SHADOWMASK)
sampler2D _CameraGBufferTexture4;
#endif

// --------------------------------------------------------
// Shadow/fade helpers

// Receiver plane depth bias create artifacts when depth is retrieved from
// the depth buffer. see UnityGetReceiverPlaneDepthBias in UnityShadowLibrary.cginc
#ifdef UNITY_USE_RECEIVER_PLANE_BIAS
    #undef UNITY_USE_RECEIVER_PLANE_BIAS
#endif

#include "UnityShadowLibrary.cginc"


//Note :
// SHADOWS_SHADOWMASK + LIGHTMAP_SHADOW_MIXING -> ShadowMask mode
// SHADOWS_SHADOWMASK only -> Distance shadowmask mode

// --------------------------------------------------------
half UnityDeferredSampleShadowMask(float2 uv)
{
    half shadowMaskAttenuation = 1.0f;

    #if defined (SHADOWS_SHADOWMASK)
        half4 shadowMask = tex2D(_CameraGBufferTexture4, uv);
        shadowMaskAttenuation = saturate(dot(shadowMask, unity_OcclusionMaskSelector));
    #endif

    return shadowMaskAttenuation;
}
/////////////////////////////////NGSS FRUSTUM SHADOWS//////////////////////////

uniform float NGSS_FRUSTUM_SHADOWS_ENABLED = 0;
uniform float NGSS_FRUSTUM_SHADOWS_OPACITY = 0;
sampler2D NGSS_FrustumShadowsTexture;

/////////////////////////////////NGSS DENOISER/////////////////////////////////

uniform float NGSS_DENOISER_ITERATIONS = 4;
uniform float NGSS_DENOISER_BLUR = 1;
uniform float NGSS_DENOISER_EDGE_TOLERANCE = 1;

// --------------------------------------------------------
half UnityDeferredSampleRealtimeShadow(half fade, float3 vec, float2 uv)
{
    half shadowAttenuation = 1.0f;

    #if defined (DIRECTIONAL) || defined (DIRECTIONAL_COOKIE)
        #if defined(SHADOWS_SCREEN)			
			#if defined(UNITY_NO_SCREENSPACE_SHADOWS)//CASCADED SHADOWS OFF
				
				shadowAttenuation = tex2D(_ShadowMapTexture, uv).r;

			#else
			
				half center = tex2D(_ShadowMapTexture, uv).r;
				float shadow = 0.0;
				float total = 0.0;
				
				float2 softness = NGSS_DENOISER_BLUR * _ShadowMapTexture_TexelSize.zw;
				for (float x = -NGSS_DENOISER_ITERATIONS; x <= NGSS_DENOISER_ITERATIONS; x++)
				{
					for (float y = -NGSS_DENOISER_ITERATIONS; y <= NGSS_DENOISER_ITERATIONS; y++)
					{
						half sampleSM = tex2D(_ShadowMapTexture, uv + float2(x, y) / softness).r;
						float weight = saturate(1.0 - abs(center - sampleSM) / NGSS_DENOISER_EDGE_TOLERANCE);
						//weight = pow(weight, expo);
						shadow += sampleSM * weight;
						total += weight;
					}
				}
				
				shadowAttenuation = shadow / total;
			#endif
        #endif
    #endif

    #if defined(UNITY_FAST_COHERENT_DYNAMIC_BRANCHING) && defined(SHADOWS_SOFT) && !defined(LIGHTMAP_SHADOW_MIXING)
    //avoid expensive shadows fetches in the distance where coherency will be good
    UNITY_BRANCH
    if (fade < (1.0f - 1e-2f))
    {
    #endif

        #if defined(SPOT)
            #if defined(SHADOWS_DEPTH)
                float4 shadowCoord = mul(unity_WorldToShadow[0], float4(vec, 1));
				#if defined (NGSS_SUPPORT_LOCAL)
				shadowAttenuation = UnitySampleShadowmapNGSS(shadowCoord, float3(uv, distance(_WorldSpaceCameraPos, vec)));
				#else
				shadowAttenuation = UnitySampleShadowmap(shadowCoord);
				#endif
            #endif
        #endif

        #if defined (POINT) || defined (POINT_COOKIE)
            #if defined(SHADOWS_CUBE)
				#if defined (NGSS_SUPPORT_LOCAL)
				shadowAttenuation = UnitySampleShadowmapNGSS(vec, float3(uv, distance(_WorldSpaceCameraPos, vec)));
				#else
				shadowAttenuation = UnitySampleShadowmap(vec);
				#endif
            #endif
        #endif

    #if defined(UNITY_FAST_COHERENT_DYNAMIC_BRANCHING) && defined(SHADOWS_SOFT) && !defined(LIGHTMAP_SHADOW_MIXING)
    }
    #endif

    return shadowAttenuation;
}
// --------------------------------------------------------
// For backward compatibility only has UnityDeferredComputeFadeDistance
// has been renamed to UnityComputeShadowFadeDistance in Unity 5.6
float UnityDeferredComputeFadeDistance(float3 wpos, float z)
{
	return UnityComputeShadowFadeDistance(wpos, z);
}
// --------------------------------------------------------
half UnityDeferredComputeShadow(float3 vec, float fadeDist, float2 uv)
{

    half fade                      = UnityComputeShadowFade(fadeDist);
    half shadowMaskAttenuation     = UnityDeferredSampleShadowMask(uv);
    half realtimeShadowAttenuation = UnityDeferredSampleRealtimeShadow(fade, vec, uv);

    return UnityMixRealtimeAndBakedShadows(realtimeShadowAttenuation, shadowMaskAttenuation, fade);
}

// --------------------------------------------------------
// Common lighting data calculation (direction, attenuation, ...)
void UnityDeferredCalculateLightParams (
	unity_v2f_deferred i,
	out float3 outWorldPos,
	out float2 outUV,
	out half3 outLightDir,
	out float outAtten,
	out float outFadeDist,
	out float outShadow)
{
	i.ray = i.ray * (_ProjectionParams.z / i.ray.z);
	float2 uv = i.uv.xy / i.uv.w;
	
	// read depth and reconstruct world position
	float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
	depth = Linear01Depth (depth);
	float4 vpos = float4(i.ray * depth,1);
	float3 wpos = mul (unity_CameraToWorld, vpos).xyz;

	float fadeDist = UnityDeferredComputeFadeDistance(wpos, vpos.z);

	float shadowStrength = 1;
	
	// spot light case
	#if defined (SPOT)	
		float3 tolight = _LightPos.xyz - wpos;
		half3 lightDir = normalize (tolight);
		
		float4 uvCookie = mul (unity_WorldToLight, float4(wpos,1));
		// negative bias because http://aras-p.info/blog/2010/01/07/screenspace-vs-mip-mapping/
		float atten = tex2Dbias (_LightTexture0, float4(uvCookie.xy / uvCookie.w, 0, -8)).w;
		atten *= uvCookie.w < 0;
		float att = dot(tolight, tolight) * _LightPos.w;
		atten *= tex2D (_LightTextureB0, att.rr).UNITY_ATTEN_CHANNEL;
		
		//atten *= UnityDeferredComputeShadow (wpos, fadeDist, uv);
		shadowStrength = UnityDeferredComputeShadow (wpos, fadeDist, uv);
	
	// directional light case		
	#elif defined (DIRECTIONAL) || defined (DIRECTIONAL_COOKIE)
		half3 lightDir = -_LightDir.xyz;
		float atten = 1.0;
		
		//atten *= UnityDeferredComputeShadow (wpos, fadeDist, uv);
		shadowStrength = UnityDeferredComputeShadow (wpos, fadeDist, uv);
		shadowStrength = NGSS_FRUSTUM_SHADOWS_ENABLED > 0.0 ? min(shadowStrength, saturate(tex2D(NGSS_FrustumShadowsTexture, uv).r + NGSS_FRUSTUM_SHADOWS_OPACITY)) : shadowStrength; //next version will require one RT for local and one RT for directional
		
		#if defined (DIRECTIONAL_COOKIE)
		atten *= tex2Dbias (_LightTexture0, float4(mul(unity_WorldToLight, half4(wpos,1)).xy, 0, -8)).w;
		#endif //DIRECTIONAL_COOKIE
	
	// point light case	
	#elif defined (POINT) || defined (POINT_COOKIE)
		float3 tolight = wpos - _LightPos.xyz;
		half3 lightDir = -normalize (tolight);
		
		float att = dot(tolight, tolight) * _LightPos.w;
		float atten = tex2D (_LightTextureB0, att.rr).UNITY_ATTEN_CHANNEL;
		
		//atten *= UnityDeferredComputeShadow (tolight, fadeDist, uv);
		shadowStrength = UnityDeferredComputeShadow (tolight, fadeDist, uv);
		
		#if defined (POINT_COOKIE)
		atten *= texCUBEbias(_LightTexture0, float4(mul(unity_WorldToLight, half4(wpos,1)).xyz, -8)).w;
		#endif //POINT_COOKIE	
	#else
		half3 lightDir = 0;
		float atten = 0;
	#endif

	outWorldPos = wpos;
	outUV = uv;
	outLightDir = lightDir;
	outAtten = atten;
	outFadeDist = fadeDist;

	outShadow = shadowStrength;

}

#endif // UNITY_DEFERRED_LIBRARY_INCLUDED

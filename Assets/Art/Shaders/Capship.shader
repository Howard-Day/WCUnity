// Made with Amplify Shader Editor v1.9.6.3
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Capship"
{
	Properties
	{
		_Dithering("Dithering", 2D) = "white" {}
		_OutlineWidth("OutlineWidth", Float) = 0
		_LightingShading("LightingShading", 2D) = "white" {}
		_OutlineTint("OutlineTint", Color) = (0.2808037,0.2740374,0.362,1)
		_AmbientColor("AmbientColor", Color) = (0.2808037,0.2740374,0.362,1)
		_ColorMap1("ColorMap", 2D) = "white" {}
		_HullMap("Hull Map", 2D) = "white" {}
		_HullMask("Hull Mask", 2D) = "white" {}
		[Toggle(_VERTCOLOREMISSIVE_ON)] _VertColorEmissive("Vert Color Emissive", Float) = 0
		_Specular("Specular", Float) = 0
		_SpecColorAMount("SpecColorAMount", Float) = 1
		_SpecGloss("SpecGloss", Float) = 32
		_WindowColor("WindowColor", Color) = (1,1,1,0)
		_WindowBrightness("WindowBrightness", Float) = 2
		[HideInInspector] _texcoord2( "", 2D ) = "white" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ }
		Cull Front
		CGPROGRAM
		#pragma target 3.0
		#pragma surface outlineSurf Outline nofog  keepalpha noshadow noambient novertexlights nolightmap nodynlightmap nodirlightmap nometa noforwardadd vertex:outlineVertexDataFunc 
		
		void outlineVertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float clampDepth = -UnityObjectToViewPos( v.vertex.xyz ).z * _ProjectionParams.w;
			o.clampDepth = -UnityObjectToViewPos( v.vertex.xyz ).z * _ProjectionParams.w;
			float temp_output_186_0 = pow( clampDepth , 0.9 );
			float lerpResult17 = lerp( 0.0125 , 20.0 , temp_output_186_0);
			float3 ase_objectScale = float3( length( unity_ObjectToWorld[ 0 ].xyz ), length( unity_ObjectToWorld[ 1 ].xyz ), length( unity_ObjectToWorld[ 2 ].xyz ) );
			float localMyCustomExpression206 = MyCustomExpression206();
			float outlineVar = ( ( ( lerpResult17 / ase_objectScale ) * _OutlineWidth * 1.775 ) / localMyCustomExpression206 ).x;
			v.vertex.xyz += ( v.normal * outlineVar );
		}
		inline half4 LightingOutline( SurfaceOutput s, half3 lightDir, half atten ) { return half4 ( 0,0,0, s.Alpha); }
		void outlineSurf( Input i, inout SurfaceOutput o )
		{
			float4 temp_cast_0 = (0.0).xxxx;
			#ifdef _VERTCOLOREMISSIVE_ON
				float4 staticSwitch128 = ( i.vertexColor * 1.125 );
			#else
				float4 staticSwitch128 = temp_cast_0;
			#endif
			float4 ase_screenPos = float4( i.screenPos.xyz , i.screenPos.w + 0.00000000001 );
			float4 ase_screenPosNorm = ase_screenPos / ase_screenPos.w;
			ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
			float4 tex2DNode2 = tex2D( _Dithering, ase_screenPosNorm.xy );
			float Dither134 = tex2DNode2.r;
			float4 temp_cast_2 = (Dither134).xxxx;
			float4 lerpResult130 = lerp( staticSwitch128 , temp_cast_2 , 0.05);
			float div136=256.0/float(32);
			float4 posterize136 = ( floor( lerpResult130 * div136 ) / div136 );
			float4 temp_cast_3 = (0.5).xxxx;
			float2 uv_HullMask = i.uv_texcoord * _HullMask_ST.xy + _HullMask_ST.zw;
			float4 tex2DNode122 = tex2Dbias( _HullMask, float4( uv_HullMask, 0, -2.0) );
			float4 lerpResult123 = lerp( i.vertexColor , temp_cast_3 , tex2DNode122.r);
			float4 temp_cast_4 = (0.5).xxxx;
			#ifdef _VERTCOLOREMISSIVE_ON
				float4 staticSwitch126 = temp_cast_4;
			#else
				float4 staticSwitch126 = lerpResult123;
			#endif
			float2 uv1_ColorMap1 = i.uv2_texcoord2 * _ColorMap1_ST.xy + _ColorMap1_ST.zw;
			float4 tex2DNode105 = tex2D( _ColorMap1, uv1_ColorMap1 );
			float2 uv_HullMap = i.uv_texcoord * _HullMap_ST.xy + _HullMap_ST.zw;
			float4 tex2DNode121 = tex2Dbias( _HullMap, float4( uv_HullMap, 0, -1.0) );
			float temp_output_192_0 = pow( tex2DNode105.g , 0.75 );
			float3 ase_worldPos = i.worldPos;
			float3 ase_worldViewDir = normalize( UnityWorldSpaceViewDir( ase_worldPos ) );
			float3 ase_worldNormal = WorldNormalVector( i, float3( 0, 0, 1 ) );
			float fresnelNdotV41 = dot( ase_worldNormal, ase_worldViewDir );
			float fresnelNode41 = ( 0.0 + 1.0 * pow( 1.0 - fresnelNdotV41, 3.0 ) );
			float lerpResult185 = lerp( ( temp_output_192_0 * ( ( 1.0 - fresnelNode41 ) * 0.3 ) ) , tex2DNode2.r , 0.0025);
			float3 ase_vertexNormal = mul( unity_WorldToObject, float4( ase_worldNormal, 0 ) );
			ase_vertexNormal = normalize( ase_vertexNormal );
			float3 objToWorldDir180 = mul( unity_ObjectToWorld, float4( ase_vertexNormal, 0 ) ).xyz;
			#if defined(LIGHTMAP_ON) && UNITY_VERSION < 560 //aseld
			float3 ase_worldlightDir = 0;
			#else //aseld
			float3 ase_worldlightDir = normalize( UnityWorldSpaceLightDir( ase_worldPos ) );
			#endif //aseld
			float3 temp_output_61_0 = ( ase_worldlightDir * float3( -1,-1,-1 ) );
			float dotResult51 = dot( objToWorldDir180 , temp_output_61_0 );
			float lerpResult59 = lerp( min( ( ( dotResult51 * 1.0 ) + 0.25 ) , temp_output_192_0 ) , tex2DNode2.r , 0.1);
			float2 appendResult57 = (float2(( 1.0 - max( lerpResult185 , ( lerpResult59 * 0.5 ) ) ) , 0.0));
			float dotResult23 = dot( objToWorldDir180 , ase_worldlightDir );
			float AO155 = temp_output_192_0;
			float lerpResult7 = lerp( ( ( ( ( dotResult23 * 0.5 ) + 0.5 ) * 0.55 ) * saturate( ( 1 + ( AO155 * 0.5 ) ) ) ) , tex2DNode2.r , 0.0125);
			float2 appendResult47 = (float2(pow( ( 1.0 - max( lerpResult185 , lerpResult7 ) ) , 2.0 ) , 0.0));
			float4 temp_output_104_0 = ( ( staticSwitch126 * tex2DNode105.r * 4.0 * float4( tex2DNode121.rgb , 0.0 ) ) * float4( max( ( _AmbientColor.rgb * tex2D( _LightingShading, appendResult57 ).rgb ) , tex2D( _LightingShading, appendResult47 ).rgb ) , 0.0 ) * 1.0 );
			float dotResult194 = dot( ( ase_worldlightDir * -1.0 ) , ase_worldViewDir );
			float temp_output_202_0 = pow( AO155 , 5.0 );
			float temp_output_199_0 = saturate( ( dotResult194 * temp_output_202_0 ) );
			float NdotL164 = dotResult23;
			float temp_output_186_0 = pow( i.clampDepth , 0.9 );
			float DistanceBlend169 = temp_output_186_0;
			float lerpResult170 = lerp( 0.75 , 2.5 , DistanceBlend169);
			float dotResult295 = dot( ase_worldlightDir , ase_worldViewDir );
			float Shadows304 = 1;
			float div157=256.0/float(8);
			float4 posterize157 = ( floor( ( ( ( pow( temp_output_199_0 , 256.0 ) * 64.0 ) + pow( temp_output_199_0 , 2.0 ) + ( saturate( ( temp_output_202_0 + 0.0 ) ) * ( saturate( NdotL164 ) + 0.0 ) * lerpResult170 ) + ( saturate( (0.0 + (pow( saturate( dotResult295 ) , 14.0 ) - 0.0) * (1.0 - 0.0) / (0.15 - 0.0)) ) * 0.65 * Shadows304 ) ) * pow( i.vertexColor , 3.0 ) ) * div157 ) / div157 );
			float4 OutlineLighten160 = posterize157;
			o.Emission = ( ( posterize136 * 0.5 ) + ( temp_output_104_0 * float4( _OutlineTint.rgb , 0.0 ) * AO155 ) + OutlineLighten160 ).rgb;
			o.Normal = float3(0,0,-1);
		}
		ENDCG
		

		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" }
		Cull Back
		CGINCLUDE
		#include "UnityPBSLighting.cginc"
		#include "UnityCG.cginc"
		#include "Lighting.cginc"
		#pragma target 4.0
		#pragma shader_feature_local _VERTCOLOREMISSIVE_ON
		#ifdef UNITY_PASS_SHADOWCASTER
			#undef INTERNAL_DATA
			#undef WorldReflectionVector
			#undef WorldNormalVector
			#define INTERNAL_DATA half3 internalSurfaceTtoW0; half3 internalSurfaceTtoW1; half3 internalSurfaceTtoW2;
			#define WorldReflectionVector(data,normal) reflect (data.worldRefl, half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal)))
			#define WorldNormalVector(data,normal) half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal))
		#endif
		struct Input
		{
			float4 vertexColor : COLOR;
			float2 uv_texcoord;
			float2 uv2_texcoord2;
			float3 worldPos;
			float3 worldNormal;
			float4 screenPos;
			float3 viewDir;
			INTERNAL_DATA
			float clampDepth;
		};

		struct SurfaceOutputCustomLightingCustom
		{
			half3 Albedo;
			half3 Normal;
			half3 Emission;
			half Metallic;
			half Smoothness;
			half Occlusion;
			half Alpha;
			Input SurfInput;
			UnityGIInput GIData;
		};

		uniform sampler2D _HullMask;
		uniform float4 _HullMask_ST;
		uniform sampler2D _ColorMap1;
		uniform float4 _ColorMap1_ST;
		uniform sampler2D _HullMap;
		uniform float4 _HullMap_ST;
		uniform float4 _AmbientColor;
		uniform sampler2D _LightingShading;
		uniform sampler2D _Dithering;
		uniform float4 _WindowColor;
		uniform float _WindowBrightness;
		uniform float _SpecGloss;
		uniform float _SpecColorAMount;
		uniform float _Specular;
		uniform float4 _OutlineTint;
		uniform float _OutlineWidth;


		float MyCustomExpression206(  )
		{
			 return  unity_CameraProjection._m11;
		}


		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			v.vertex.xyz += 0;
			v.vertex.w = 1;
		}

		inline half4 LightingStandardCustomLighting( inout SurfaceOutputCustomLightingCustom s, half3 viewDir, UnityGI gi )
		{
			UnityGIInput data = s.GIData;
			Input i = s.SurfInput;
			half4 c = 0;
			#ifdef UNITY_PASS_FORWARDBASE
			float ase_lightAtten = data.atten;
			if( _LightColor0.a == 0)
			ase_lightAtten = 0;
			#else
			float3 ase_lightAttenRGB = gi.light.color / ( ( _LightColor0.rgb ) + 0.000001 );
			float ase_lightAtten = max( max( ase_lightAttenRGB.r, ase_lightAttenRGB.g ), ase_lightAttenRGB.b );
			#endif
			#if defined(HANDLE_SHADOWS_BLENDING_IN_GI)
			half bakedAtten = UnitySampleBakedOcclusion(data.lightmapUV.xy, data.worldPos);
			float zDist = dot(_WorldSpaceCameraPos - data.worldPos, UNITY_MATRIX_V[2].xyz);
			float fadeDist = UnityComputeShadowFadeDistance(data.worldPos, zDist);
			ase_lightAtten = UnityMixRealtimeAndBakedShadows(data.atten, bakedAtten, UnityComputeShadowFade(fadeDist));
			#endif
			float4 temp_cast_1 = (0.5).xxxx;
			float2 uv_HullMask = i.uv_texcoord * _HullMask_ST.xy + _HullMask_ST.zw;
			float4 tex2DNode122 = tex2Dbias( _HullMask, float4( uv_HullMask, 0, -2.0) );
			float4 lerpResult123 = lerp( i.vertexColor , temp_cast_1 , tex2DNode122.r);
			float4 temp_cast_2 = (0.5).xxxx;
			#ifdef _VERTCOLOREMISSIVE_ON
				float4 staticSwitch126 = temp_cast_2;
			#else
				float4 staticSwitch126 = lerpResult123;
			#endif
			float2 uv1_ColorMap1 = i.uv2_texcoord2 * _ColorMap1_ST.xy + _ColorMap1_ST.zw;
			float4 tex2DNode105 = tex2D( _ColorMap1, uv1_ColorMap1 );
			float2 uv_HullMap = i.uv_texcoord * _HullMap_ST.xy + _HullMap_ST.zw;
			float4 tex2DNode121 = tex2Dbias( _HullMap, float4( uv_HullMap, 0, -1.0) );
			float temp_output_192_0 = pow( tex2DNode105.g , 0.75 );
			float3 ase_worldPos = i.worldPos;
			float3 ase_worldViewDir = normalize( UnityWorldSpaceViewDir( ase_worldPos ) );
			float3 ase_worldNormal = i.worldNormal;
			float fresnelNdotV41 = dot( ase_worldNormal, ase_worldViewDir );
			float fresnelNode41 = ( 0.0 + 1.0 * pow( 1.0 - fresnelNdotV41, 3.0 ) );
			float4 ase_screenPos = float4( i.screenPos.xyz , i.screenPos.w + 0.00000000001 );
			float4 ase_screenPosNorm = ase_screenPos / ase_screenPos.w;
			ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
			float4 tex2DNode2 = tex2D( _Dithering, ase_screenPosNorm.xy );
			float lerpResult185 = lerp( ( temp_output_192_0 * ( ( 1.0 - fresnelNode41 ) * 0.3 ) ) , tex2DNode2.r , 0.0025);
			float3 ase_vertexNormal = mul( unity_WorldToObject, float4( ase_worldNormal, 0 ) );
			ase_vertexNormal = normalize( ase_vertexNormal );
			float3 objToWorldDir180 = mul( unity_ObjectToWorld, float4( ase_vertexNormal, 0 ) ).xyz;
			#if defined(LIGHTMAP_ON) && UNITY_VERSION < 560 //aseld
			float3 ase_worldlightDir = 0;
			#else //aseld
			float3 ase_worldlightDir = Unity_SafeNormalize( UnityWorldSpaceLightDir( ase_worldPos ) );
			#endif //aseld
			float3 temp_output_61_0 = ( ase_worldlightDir * float3( -1,-1,-1 ) );
			float dotResult51 = dot( objToWorldDir180 , temp_output_61_0 );
			float lerpResult59 = lerp( min( ( ( dotResult51 * 1.0 ) + 0.25 ) , temp_output_192_0 ) , tex2DNode2.r , 0.1);
			float2 appendResult57 = (float2(( 1.0 - max( lerpResult185 , ( lerpResult59 * 0.5 ) ) ) , 0.0));
			float dotResult23 = dot( objToWorldDir180 , ase_worldlightDir );
			float AO155 = temp_output_192_0;
			float lerpResult7 = lerp( ( ( ( ( dotResult23 * 0.5 ) + 0.5 ) * 0.55 ) * saturate( ( ase_lightAtten + ( AO155 * 0.5 ) ) ) ) , tex2DNode2.r , 0.0125);
			float2 appendResult47 = (float2(pow( ( 1.0 - max( lerpResult185 , lerpResult7 ) ) , 2.0 ) , 0.0));
			float4 temp_output_104_0 = ( ( staticSwitch126 * tex2DNode105.r * 4.0 * float4( tex2DNode121.rgb , 0.0 ) ) * float4( max( ( _AmbientColor.rgb * tex2D( _LightingShading, appendResult57 ).rgb ) , tex2D( _LightingShading, appendResult47 ).rgb ) , 0.0 ) * 1.0 );
			float4 temp_cast_6 = (0.0).xxxx;
			#ifdef _VERTCOLOREMISSIVE_ON
				float4 staticSwitch128 = ( i.vertexColor * 1.125 );
			#else
				float4 staticSwitch128 = temp_cast_6;
			#endif
			float Dither134 = tex2DNode2.r;
			float4 temp_cast_7 = (Dither134).xxxx;
			float4 lerpResult130 = lerp( staticSwitch128 , temp_cast_7 , 0.05);
			float div136=256.0/float(32);
			float4 posterize136 = ( floor( lerpResult130 * div136 ) / div136 );
			float3 Color217 = tex2DNode121.rgb;
			float lerpResult265 = lerp( tex2DNode122.g , tex2DNode2.r , 0.05);
			float4 temp_cast_9 = (lerpResult265).xxxx;
			float div267=256.0/float(16);
			float4 posterize267 = ( floor( temp_cast_9 * div267 ) / div267 );
			float fresnelNdotV270 = dot( ase_worldNormal, ase_worldViewDir );
			float fresnelNode270 = ( 0.0 + 2.0 * pow( 1.0 - fresnelNdotV270, 3.0 ) );
			float3 normalizeResult284 = normalize( ( i.viewDir + ase_worldlightDir ) );
			float dotResult278 = dot( normalizeResult284 , objToWorldDir180 );
			float NDotH282 = pow( saturate( dotResult278 ) , _SpecGloss );
			float3 normalizeResult249 = normalize( ( i.viewDir + temp_output_61_0 ) );
			float dotResult250 = dot( objToWorldDir180 , normalizeResult249 );
			float InvNDotH253 = pow( saturate( dotResult250 ) , _SpecGloss );
			float lerpResult258 = lerp( ( ( saturate( NDotH282 ) * ase_lightAtten ) + ( saturate( InvNDotH253 ) * AO155 * 0.5 ) ) , tex2DNode2.r , 0.05);
			float4 temp_cast_11 = (lerpResult258).xxxx;
			float div259=256.0/float(50);
			float4 posterize259 = ( floor( temp_cast_11 * div259 ) / div259 );
			float4 temp_cast_13 = (1.0).xxxx;
			float4 VertColor240 = i.vertexColor;
			float4 lerpResult242 = lerp( temp_cast_13 , VertColor240 , _SpecColorAMount);
			float4 Specular237 = saturate( ( posterize259 * float4( Color217 , 0.0 ) * lerpResult242 * _Specular ) );
			c.rgb = ( temp_output_104_0 + posterize136 + float4( ( Color217 * tex2DNode122.b * 2.0 ) , 0.0 ) + ( posterize267 * float4( _WindowColor.rgb , 0.0 ) * _WindowBrightness * ( 1.0 - fresnelNode270 ) ) + Specular237 ).rgb;
			c.a = 1;
			return c;
		}

		inline void LightingStandardCustomLighting_GI( inout SurfaceOutputCustomLightingCustom s, UnityGIInput data, inout UnityGI gi )
		{
			s.GIData = data;
		}

		void surf( Input i , inout SurfaceOutputCustomLightingCustom o )
		{
			o.SurfInput = i;
			float3 temp_cast_0 = (0.0).xxx;
			o.Albedo = temp_cast_0;
		}

		ENDCG
		CGPROGRAM
		#pragma surface surf StandardCustomLighting keepalpha fullforwardshadows exclude_path:deferred vertex:vertexDataFunc 

		ENDCG
		Pass
		{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 4.0
			#pragma multi_compile_shadowcaster
			#pragma multi_compile UNITY_PASS_SHADOWCASTER
			#pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
			#include "HLSLSupport.cginc"
			#if ( SHADER_API_D3D11 || SHADER_API_GLCORE || SHADER_API_GLES || SHADER_API_GLES3 || SHADER_API_METAL || SHADER_API_VULKAN )
				#define CAN_SKIP_VPOS
			#endif
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "UnityPBSLighting.cginc"
			struct v2f
			{
				V2F_SHADOW_CASTER;
				float4 customPack1 : TEXCOORD1;
				float4 screenPos : TEXCOORD2;
				float4 tSpace0 : TEXCOORD3;
				float4 tSpace1 : TEXCOORD4;
				float4 tSpace2 : TEXCOORD5;
				half4 color : COLOR0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
			v2f vert( appdata_full v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID( v );
				UNITY_INITIALIZE_OUTPUT( v2f, o );
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );
				UNITY_TRANSFER_INSTANCE_ID( v, o );
				Input customInputData;
				vertexDataFunc( v, customInputData );
				float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				half3 worldNormal = UnityObjectToWorldNormal( v.normal );
				half3 worldTangent = UnityObjectToWorldDir( v.tangent.xyz );
				half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
				half3 worldBinormal = cross( worldNormal, worldTangent ) * tangentSign;
				o.tSpace0 = float4( worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x );
				o.tSpace1 = float4( worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y );
				o.tSpace2 = float4( worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z );
				o.customPack1.xy = customInputData.uv_texcoord;
				o.customPack1.xy = v.texcoord;
				o.customPack1.zw = customInputData.uv2_texcoord2;
				o.customPack1.zw = v.texcoord1;
				TRANSFER_SHADOW_CASTER_NORMALOFFSET( o )
				o.screenPos = ComputeScreenPos( o.pos );
				o.color = v.color;
				return o;
			}
			half4 frag( v2f IN
			#if !defined( CAN_SKIP_VPOS )
			, UNITY_VPOS_TYPE vpos : VPOS
			#endif
			) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				Input surfIN;
				UNITY_INITIALIZE_OUTPUT( Input, surfIN );
				surfIN.uv_texcoord = IN.customPack1.xy;
				surfIN.uv2_texcoord2 = IN.customPack1.zw;
				float3 worldPos = float3( IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w );
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				surfIN.viewDir = worldViewDir;
				surfIN.worldPos = worldPos;
				surfIN.worldNormal = float3( IN.tSpace0.z, IN.tSpace1.z, IN.tSpace2.z );
				surfIN.internalSurfaceTtoW0 = IN.tSpace0.xyz;
				surfIN.internalSurfaceTtoW1 = IN.tSpace1.xyz;
				surfIN.internalSurfaceTtoW2 = IN.tSpace2.xyz;
				surfIN.screenPos = IN.screenPos;
				surfIN.vertexColor = IN.color;
				SurfaceOutputCustomLightingCustom o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutputCustomLightingCustom, o )
				surf( surfIN, o );
				#if defined( CAN_SKIP_VPOS )
				float2 vpos = IN.pos;
				#endif
				SHADOW_CASTER_FRAGMENT( IN )
			}
			ENDCG
		}
	}

	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19603
Node;AmplifyShaderEditor.SamplerNode;105;-2400,-640;Inherit;True;Property;_ColorMap1;ColorMap;5;0;Create;True;0;0;0;False;0;False;-1;None;adf0bf0f7bd8aa54d9c866c63aa1afe6;True;1;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.WorldSpaceLightDirHlpNode;21;-2960,-640;Inherit;False;True;1;0;FLOAT;0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.NormalVertexDataNode;50;-2944,-864;Inherit;False;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PowerNode;192;-2112,-560;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;0.75;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;61;-2560,-736;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;-1,-1,-1;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;227;-2944,-1168;Inherit;False;World;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.TransformDirectionNode;180;-2752,-864;Inherit;False;Object;World;False;Fast;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RegisterLocalVarNode;155;-1952,-416;Inherit;False;AO;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;283;-2720,-1088;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;248;-2624,-1312;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;156;-3088,304;Inherit;False;155;AO;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;51;-2336,-752;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;23;-2512,-80;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NormalizeNode;284;-2592,-1088;Inherit;False;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.NormalizeNode;249;-2496,-1312;Inherit;False;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.LightAttenuation;72;-2928,208;Inherit;False;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;110;-2848,304;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;85;-2192,-752;Inherit;False;2;2;0;FLOAT;0.5;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;26;-2384,-128;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.FresnelNode;41;-2912,-448;Inherit;False;Standard;WorldNormal;ViewDir;False;False;5;0;FLOAT3;0,0,1;False;4;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;3;False;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;278;-2432,-1088;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;250;-2304,-1312;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;43;-2448,-448;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;74;-2688,208;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;86;-2048,-752;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.25;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;27;-2224,-128;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScreenPosInputsNode;1;-2160,32;Float;False;0;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;247;-2272,-1200;Inherit;False;Property;_SpecGloss;SpecGloss;11;0;Create;True;0;0;0;False;0;False;32;12;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;279;-2272,-1088;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;275;-2144,-1312;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;75;-2576,208;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;44;-2192,-368;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.3;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;115;-2096,-128;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.55;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldSpaceLightDirHlpNode;193;-1824,-2112;Inherit;False;False;1;0;FLOAT;0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;197;-1616,-2048;Inherit;False;Constant;_Float1;Float 1;11;0;Create;True;0;0;0;False;0;False;-1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;195;-1648,-1968;Inherit;False;World;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleMinOpNode;106;-1776,-608;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;2;-1968,16;Inherit;True;Property;_Dithering;Dithering;0;0;Create;True;0;0;0;False;0;False;-1;1dec9d0afe198024b9a43a07728f9376;1dec9d0afe198024b9a43a07728f9376;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.PowerNode;281;-2064,-1088;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;12;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;251;-1936,-1312;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;12;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;24;-1936,-128;Inherit;False;2;2;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;108;-1936,-352;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SurfaceDepthNode;39;-400,736;Inherit;False;1;1;0;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;158;-1600,-1808;Inherit;False;155;AO;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;196;-1488,-2112;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.DotProductOpNode;295;-1392,-2272;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;59;-1472,-272;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;282;-1904,-1088;Inherit;False;NDotH;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;253;-1776,-1312;Inherit;False;InvNDotH;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;7;-1472,64;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0.0125;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;185;-1680,-192;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0.0025;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;62;-1264,-128;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;186;-144,736;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;0.9;False;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;194;-1280,-2096;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;164;-2380.372,-24.18481;Inherit;False;NdotL;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;202;-1408,-1808;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;302;-1280,-2272;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;285;-2032,352;Inherit;False;282;NDotH;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;256;-2160,608;Inherit;False;253;InvNDotH;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMaxOpNode;190;-1280,48;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMaxOpNode;42;-1088,-176;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;200;-1152,-1984;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;165;-1792,-1696;Inherit;False;164;NdotL;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;169;64,960;Inherit;False;DistanceBlend;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;297;-1120,-2272;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;14;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;291;-2128,400;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;287;-1840,352;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;263;-2008.804,682.5513;Inherit;False;155;AO;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;257;-1968,608;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;293;-1976.719,760.1326;Inherit;False;Constant;_Float11;Float 11;14;0;Create;True;0;0;0;False;0;False;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;11;-1104,64;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;60;-928,-128;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;174;-1264,-1696;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;168;-1216,-1808;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;171;-1296,-1552;Inherit;False;169;DistanceBlend;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;199;-976,-1984;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;299;-864,-2288;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0.15;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;304;-2624,304;Inherit;False;Shadows;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;116;-1472,-1040;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;288;-1696,352;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;262;-1824,608;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;57;-736,-128;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PowerNode;120;-928,64;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;102;-1136,-400;Inherit;True;Property;_LightingShading;LightingShading;2;0;Create;True;0;0;0;False;0;False;None;065cb2792dab7d74d9941bb9b785f165;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.RangedFloatNode;124;-1440,-880;Inherit;False;Constant;_Float3;Float 3;7;0;Create;True;0;0;0;False;0;False;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;122;-1568,-784;Inherit;True;Property;_HullMask;Hull Mask;7;0;Create;True;0;0;0;False;0;False;-1;None;7bc1494caecd9a045b39cbf7680e8f92;True;0;False;white;Auto;False;Object;-1;MipBias;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;-2;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleAddOpNode;209;-1088,-1696;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;181;-1088,-1808;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;170;-1056,-1600;Inherit;False;3;0;FLOAT;0.75;False;1;FLOAT;2.5;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;198;-800,-1984;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;256;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;300;-656,-2288;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;298;-656,-2224;Inherit;False;Constant;_Float12;Float 12;14;0;Create;True;0;0;0;False;0;False;0.65;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;303;-688,-2160;Inherit;False;304;Shadows;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;121;-1536,-1376;Inherit;True;Property;_HullMap;Hull Map;6;0;Create;True;0;0;0;False;0;False;-1;None;d0bf3a5a51f67014d872847b2511dbf4;True;0;False;white;Auto;False;Object;-1;MipBias;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;-1;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.RegisterLocalVarNode;240;-1168,-1168;Inherit;False;VertColor;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;292;-1536,480;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;47;-736,64;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;58;-592,-144;Inherit;True;Property;_TextureSample1;Texture Sample 0;2;0;Create;True;0;0;0;False;0;False;-1;None;065cb2792dab7d74d9941bb9b785f165;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.ColorNode;189;-560,-336;Inherit;False;Property;_AmbientColor;AmbientColor;4;0;Create;True;0;0;0;False;0;False;0.2808037,0.2740374,0.362,1;0.7163636,0.688,1,1;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.RangedFloatNode;127;-1456,-1120;Inherit;False;Constant;_Float5;Float 5;8;0;Create;True;0;0;0;False;0;False;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;123;-1216,-832;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;1,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;150;-1168,-960;Inherit;False;Constant;_Float13;Float 13;10;0;Create;True;0;0;0;False;0;False;1.125;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;162;-784,-1760;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;213;-800,-1888;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;212;-640,-1984;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;64;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;296;-464,-2272;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;1;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;245;-1536,880;Inherit;False;240;VertColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;244;-1488,960;Inherit;False;Property;_SpecColorAMount;SpecColorAMount;10;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;243;-1472,816;Inherit;False;Constant;_Float10;Float 10;12;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;217;-1168,-1360;Inherit;False;Color;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.LerpOp;258;-1376,480;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0.05;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;45;-592,64;Inherit;True;Property;_TextureSample0;Texture Sample 0;1;0;Create;True;0;0;0;False;0;False;-1;None;065cb2792dab7d74d9941bb9b785f165;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;188;-272,-160;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;119;-560,-448;Inherit;False;Constant;_Float2;Float 0;6;0;Create;True;0;0;0;False;0;False;4;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;118;-560,-544;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;126;-1232,-1088;Inherit;False;Property;_VertColorEmissive;Vert Color Emissive;8;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;COLOR;0,0,0,0;False;0;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;5;COLOR;0,0,0,0;False;6;COLOR;0,0,0,0;False;7;COLOR;0,0,0,0;False;8;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;129;-944,-1072;Inherit;False;Constant;_Float6;Float 6;8;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;149;-992,-1008;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;134;-1648,176;Inherit;False;Dither;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;182;-416,-1760;Inherit;False;4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;210;-448,-1632;Inherit;False;False;2;0;COLOR;0,0,0,0;False;1;FLOAT;3;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;239;-1264,960;Inherit;False;Property;_Specular;Specular;9;0;Create;True;0;0;0;False;0;False;0;0.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;235;-1312,752;Inherit;False;217;Color;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.LerpOp;242;-1280,832;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.PosterizeNode;259;-1216,480;Inherit;False;50;2;1;COLOR;0,0,0,0;False;0;INT;50;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;114;-160,80;Inherit;False;Constant;_Float0;Float 0;6;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMaxOpNode;64;-96,-48;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;117;-336,-624;Inherit;False;4;4;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT3;0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;132;-656,-960;Inherit;False;134;Dither;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;135;-656,-880;Inherit;False;Constant;_Float8;Float 8;8;0;Create;True;0;0;0;False;0;False;0.05;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;128;-768,-1056;Inherit;False;Property;_VertColorEmissive1;Vert Color Emissive;8;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Reference;126;True;True;All;9;1;COLOR;0,0,0,0;False;0;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;5;COLOR;0,0,0,0;False;6;COLOR;0,0,0,0;False;7;COLOR;0,0,0,0;False;8;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;183;-224,-1760;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;234;-1040,480;Inherit;False;4;4;0;COLOR;0,0,0,0;False;1;FLOAT3;0,0,0;False;2;COLOR;0,0,0,0;False;3;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;17;96,688;Inherit;False;3;0;FLOAT;0.0125;False;1;FLOAT;20;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ObjectScaleNode;28;80,816;Inherit;False;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;104;32,-64;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;130;-432,-1024;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.PosterizeNode;157;-16,-1760;Inherit;False;8;2;1;COLOR;0,0,0,0;False;0;INT;8;False;1;COLOR;0
Node;AmplifyShaderEditor.FresnelNode;270;-1040,-880;Inherit;False;Standard;WorldNormal;ViewDir;False;False;5;0;FLOAT3;0,0,1;False;4;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;2;False;3;FLOAT;3;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;294;-896,480;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;101;336,784;Inherit;False;Property;_OutlineWidth;OutlineWidth;1;0;Create;True;0;0;0;False;0;False;0;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;215;336,848;Inherit;False;Constant;_Float7;Float 7;11;0;Create;True;0;0;0;False;0;False;1.775;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;31;352,688;Inherit;False;2;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.LerpOp;265;-1088,-704;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0.05;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;99;240,176;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;98;-32,224;Inherit;False;Property;_OutlineTint;OutlineTint;3;0;Create;True;0;0;0;False;0;False;0.2808037,0.2740374,0.362,1;0.3358588,0.3211765,0.468,1;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.GetLocalVarNode;305;-16,432;Inherit;False;155;AO;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.PosterizeNode;136;-240,-960;Inherit;False;32;2;1;COLOR;0,0,0,0;False;0;INT;32;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;160;144,-1760;Inherit;False;OutlineLighten;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;154;256,192;Inherit;False;Constant;_Float15;Float 15;10;0;Create;True;0;0;0;False;0;False;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;274;-780.5918,-1299.536;Inherit;False;Constant;_Float9;Float 9;14;0;Create;True;0;0;0;False;0;False;2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;266;-960,-608;Inherit;False;Property;_WindowColor;WindowColor;12;0;Create;True;0;0;0;False;0;False;1,1,1,0;0.376,1,0.7039059,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.RangedFloatNode;269;-912,-416;Inherit;False;Property;_WindowBrightness;WindowBrightness;13;0;Create;True;0;0;0;False;0;False;2;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;271;-811.0171,-798.8644;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;237;-752,544;Inherit;False;Specular;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.CustomExpressionNode;206;528,800;Inherit;False; return  unity_CameraProjection._m11@;1;Create;0;My Custom Expression;True;False;0;;False;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;100;528,688;Inherit;False;3;3;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.PosterizeNode;267;-896,-704;Inherit;False;16;2;1;COLOR;0,0,0,0;False;0;INT;16;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;95;288,304;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;161;256,416;Inherit;False;160;OutlineLighten;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;153;400,176;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;273;-624,-1376;Inherit;False;3;3;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;268;-672,-688;Inherit;False;4;4;0;COLOR;0,0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;238;688,160;Inherit;False;237;Specular;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;207;752,656;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;152;608,368;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;4;976,-288;Inherit;False;Constant;_Float4;Float 4;1;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;264;944,-32;Inherit;False;5;5;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT3;0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.OutlineNode;12;912,528;Inherit;False;0;True;None;0;0;Front;True;True;True;True;0;False;;3;0;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.PowerNode;301;-672,-2080;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;0.25;False;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;1200,-288;Float;False;True;-1;4;ASEMaterialInspector;0;0;CustomLighting;Capship;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;;0;False;;False;0;False;;0;False;;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;ForwardOnly;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;True;0;0;False;;0;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0.01;0,0,0,0.02745098;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;-1;-1;-1;-1;0;False;1;=;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;16;FLOAT4;0,0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;192;0;105;2
WireConnection;61;0;21;0
WireConnection;180;0;50;0
WireConnection;155;0;192;0
WireConnection;283;0;227;0
WireConnection;283;1;21;0
WireConnection;248;0;227;0
WireConnection;248;1;61;0
WireConnection;51;0;180;0
WireConnection;51;1;61;0
WireConnection;23;0;180;0
WireConnection;23;1;21;0
WireConnection;284;0;283;0
WireConnection;249;0;248;0
WireConnection;110;0;156;0
WireConnection;85;0;51;0
WireConnection;26;0;23;0
WireConnection;278;0;284;0
WireConnection;278;1;180;0
WireConnection;250;0;180;0
WireConnection;250;1;249;0
WireConnection;43;0;41;0
WireConnection;74;0;72;0
WireConnection;74;1;110;0
WireConnection;86;0;85;0
WireConnection;27;0;26;0
WireConnection;279;0;278;0
WireConnection;275;0;250;0
WireConnection;75;0;74;0
WireConnection;44;0;43;0
WireConnection;115;0;27;0
WireConnection;106;0;86;0
WireConnection;106;1;192;0
WireConnection;2;1;1;0
WireConnection;281;0;279;0
WireConnection;281;1;247;0
WireConnection;251;0;275;0
WireConnection;251;1;247;0
WireConnection;24;0;115;0
WireConnection;24;1;75;0
WireConnection;108;0;192;0
WireConnection;108;1;44;0
WireConnection;196;0;193;0
WireConnection;196;1;197;0
WireConnection;295;0;193;0
WireConnection;295;1;195;0
WireConnection;59;0;106;0
WireConnection;59;1;2;1
WireConnection;282;0;281;0
WireConnection;253;0;251;0
WireConnection;7;0;24;0
WireConnection;7;1;2;1
WireConnection;185;0;108;0
WireConnection;185;1;2;1
WireConnection;62;0;59;0
WireConnection;186;0;39;0
WireConnection;194;0;196;0
WireConnection;194;1;195;0
WireConnection;164;0;23;0
WireConnection;202;0;158;0
WireConnection;302;0;295;0
WireConnection;190;0;185;0
WireConnection;190;1;7;0
WireConnection;42;0;185;0
WireConnection;42;1;62;0
WireConnection;200;0;194;0
WireConnection;200;1;202;0
WireConnection;169;0;186;0
WireConnection;297;0;302;0
WireConnection;291;0;72;0
WireConnection;287;0;285;0
WireConnection;257;0;256;0
WireConnection;11;0;190;0
WireConnection;60;0;42;0
WireConnection;174;0;165;0
WireConnection;168;0;202;0
WireConnection;199;0;200;0
WireConnection;299;0;297;0
WireConnection;304;0;72;0
WireConnection;288;0;287;0
WireConnection;288;1;291;0
WireConnection;262;0;257;0
WireConnection;262;1;263;0
WireConnection;262;2;293;0
WireConnection;57;0;60;0
WireConnection;120;0;11;0
WireConnection;209;0;174;0
WireConnection;181;0;168;0
WireConnection;170;2;171;0
WireConnection;198;0;199;0
WireConnection;300;0;299;0
WireConnection;240;0;116;0
WireConnection;292;0;288;0
WireConnection;292;1;262;0
WireConnection;47;0;120;0
WireConnection;58;0;102;0
WireConnection;58;1;57;0
WireConnection;123;0;116;0
WireConnection;123;1;124;0
WireConnection;123;2;122;1
WireConnection;162;0;181;0
WireConnection;162;1;209;0
WireConnection;162;2;170;0
WireConnection;213;0;199;0
WireConnection;212;0;198;0
WireConnection;296;0;300;0
WireConnection;296;1;298;0
WireConnection;296;2;303;0
WireConnection;217;0;121;5
WireConnection;258;0;292;0
WireConnection;258;1;2;1
WireConnection;45;0;102;0
WireConnection;45;1;47;0
WireConnection;188;0;189;5
WireConnection;188;1;58;5
WireConnection;118;0;105;1
WireConnection;126;1;123;0
WireConnection;126;0;127;0
WireConnection;149;0;116;0
WireConnection;149;1;150;0
WireConnection;134;0;2;1
WireConnection;182;0;212;0
WireConnection;182;1;213;0
WireConnection;182;2;162;0
WireConnection;182;3;296;0
WireConnection;210;0;116;0
WireConnection;242;0;243;0
WireConnection;242;1;245;0
WireConnection;242;2;244;0
WireConnection;259;1;258;0
WireConnection;64;0;188;0
WireConnection;64;1;45;5
WireConnection;117;0;126;0
WireConnection;117;1;118;0
WireConnection;117;2;119;0
WireConnection;117;3;121;5
WireConnection;128;1;129;0
WireConnection;128;0;149;0
WireConnection;183;0;182;0
WireConnection;183;1;210;0
WireConnection;234;0;259;0
WireConnection;234;1;235;0
WireConnection;234;2;242;0
WireConnection;234;3;239;0
WireConnection;17;2;186;0
WireConnection;104;0;117;0
WireConnection;104;1;64;0
WireConnection;104;2;114;0
WireConnection;130;0;128;0
WireConnection;130;1;132;0
WireConnection;130;2;135;0
WireConnection;157;1;183;0
WireConnection;294;0;234;0
WireConnection;31;0;17;0
WireConnection;31;1;28;0
WireConnection;265;0;122;2
WireConnection;265;1;2;1
WireConnection;99;0;104;0
WireConnection;136;1;130;0
WireConnection;160;0;157;0
WireConnection;271;0;270;0
WireConnection;237;0;294;0
WireConnection;100;0;31;0
WireConnection;100;1;101;0
WireConnection;100;2;215;0
WireConnection;267;1;265;0
WireConnection;95;0;99;0
WireConnection;95;1;98;5
WireConnection;95;2;305;0
WireConnection;153;0;136;0
WireConnection;153;1;154;0
WireConnection;273;0;217;0
WireConnection;273;1;122;3
WireConnection;273;2;274;0
WireConnection;268;0;267;0
WireConnection;268;1;266;5
WireConnection;268;2;269;0
WireConnection;268;3;271;0
WireConnection;207;0;100;0
WireConnection;207;1;206;0
WireConnection;152;0;153;0
WireConnection;152;1;95;0
WireConnection;152;2;161;0
WireConnection;264;0;104;0
WireConnection;264;1;136;0
WireConnection;264;2;273;0
WireConnection;264;3;268;0
WireConnection;264;4;238;0
WireConnection;12;0;152;0
WireConnection;12;1;207;0
WireConnection;301;0;158;0
WireConnection;0;0;4;0
WireConnection;0;13;264;0
WireConnection;0;11;12;0
ASEEND*/
//CHKSM=8D62ED1B23E15DCC2F312EFCB5750E169095F9DE
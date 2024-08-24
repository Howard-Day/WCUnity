// Made with Amplify Shader Editor v1.9.6.3
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "ShipSprite"
{
	Properties
	{
		_Cutoff( "Mask Clip Value", Float ) = 0.5
		_Dithering("Dithering", 2D) = "white" {}
		_MipBias("MipBias", Float) = 0
		_MainTex("MainTex", 2D) = "white" {}
		_LightingShading("LightingShading", 2D) = "white" {}
		_CloakAmount("CloakAmount", Range( 0 , 1)) = 0
		_CloakSparks("CloakSparks", 2D) = "white" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "AlphaTest+0" }
		Cull Back
		CGINCLUDE
		#include "UnityPBSLighting.cginc"
		#include "UnityShaderVariables.cginc"
		#include "Lighting.cginc"
		#pragma target 3.0
		struct Input
		{
			float2 uv_texcoord;
			float4 screenPos;
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

		uniform sampler2D _MainTex;
		uniform float4 _MainTex_ST;
		uniform float _MipBias;
		uniform float _CloakAmount;
		uniform sampler2D _CloakSparks;
		uniform sampler2D _LightingShading;
		uniform sampler2D _Dithering;
		uniform float _Cutoff = 0.5;

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
			float2 uv_MainTex = i.uv_texcoord * _MainTex_ST.xy + _MainTex_ST.zw;
			float4 tex2DNode1 = tex2Dbias( _MainTex, float4( uv_MainTex, 0, _MipBias) );
			float grayscale27 = Luminance(tex2DNode1.rgb);
			float lerpResult30 = lerp( 0.0 , 0.25 , _CloakAmount);
			float lerpResult31 = lerp( 0.0 , 0.2501 , _CloakAmount);
			float lerpResult32 = lerp( 1.0 , 0.4 , _CloakAmount);
			float lerpResult33 = lerp( 1.0 , 0.401 , _CloakAmount);
			float lerpResult29 = lerp( tex2DNode1.a , ( tex2DNode1.a * saturate( (0.0 + (grayscale27 - lerpResult30) * (1.0 - 0.0) / (lerpResult31 - lerpResult30)) ) * ( 1.0 - saturate( (0.0 + (grayscale27 - lerpResult32) * (1.0 - 0.0) / (lerpResult33 - lerpResult32)) ) ) ) , _CloakAmount);
			float4 color37 = IsGammaSpace() ? float4(0.00125,0,0.04,1) : float4(9.674922E-05,0,0.003095975,1);
			float4 lerpResult36 = lerp( float4( 0,0,0,0 ) , color37 , _CloakAmount);
			float4 temp_cast_2 = (1.0).xxxx;
			float4 color13 = IsGammaSpace() ? float4(0.06037735,0.09660377,0.64,0) : float4(0.004934957,0.009503818,0.3672465,0);
			float4 lerpResult9 = lerp( temp_cast_2 , color13 , _CloakAmount);
			// *** BEGIN Flipbook UV Animation vars ***
			// Total tiles of Flipbook Texture
			float fbtotaltiles73 = min( 16.0 * 8.0, 64.0 + 1 );
			// Offsets for cols and rows of Flipbook Texture
			float fbcolsoffset73 = 1.0f / 16.0;
			float fbrowsoffset73 = 1.0f / 8.0;
			// Speed of animation
			float fbspeed73 = _Time.y * 30.0;
			// UV Tiling (col and row offset)
			float2 fbtiling73 = float2(fbcolsoffset73, fbrowsoffset73);
			// UV Offset - calculate current tile linear index, and convert it to (X * coloffset, Y * rowoffset)
			// Calculate current tile linear index
			float fbcurrenttileindex73 = floor( fmod( fbspeed73 + 0.0, fbtotaltiles73) );
			fbcurrenttileindex73 += ( fbcurrenttileindex73 < 0) ? fbtotaltiles73 : 0;
			// Obtain Offset X coordinate from current tile linear index
			float fblinearindextox73 = round ( fmod ( fbcurrenttileindex73, 16.0 ) );
			// Multiply Offset X by coloffset
			float fboffsetx73 = fblinearindextox73 * fbcolsoffset73;
			// Obtain Offset Y coordinate from current tile linear index
			float fblinearindextoy73 = round( fmod( ( fbcurrenttileindex73 - fblinearindextox73 ) / 16.0, 8.0 ) );
			// Reverse Y to get tiles from Top to Bottom
			fblinearindextoy73 = (int)(8.0-1) - fblinearindextoy73;
			// Multiply Offset Y by rowoffset
			float fboffsety73 = fblinearindextoy73 * fbrowsoffset73;
			// UV Offset
			float2 fboffset73 = float2(fboffsetx73, fboffsety73);
			// Flipbook UV
			half2 fbuv73 = i.uv_texcoord * fbtiling73 + fboffset73;
			// *** END Flipbook UV Animation vars ***
			int flipbookFrame73 = ( ( int )fbcurrenttileindex73);
			float4 temp_output_54_0 = ( 0.0125 * ( tex2DNode1 + -0.5 ) );
			float4 tex2DNode44 = tex2D( _CloakSparks, ( float4( fbuv73, 0.0 , 0.0 ) + temp_output_54_0 + temp_output_54_0 ).rg );
			float2 uv_TexCoord50 = i.uv_texcoord * float2( 4,4 );
			float fbtotaltiles45 = min( 16.0 * 8.0, 64.0 + 1 );
			float fbcolsoffset45 = 1.0f / 16.0;
			float fbrowsoffset45 = 1.0f / 8.0;
			float fbspeed45 = _Time.y * 15.0;
			float2 fbtiling45 = float2(fbcolsoffset45, fbrowsoffset45);
			float fbcurrenttileindex45 = floor( fmod( fbspeed45 + 0.0, fbtotaltiles45) );
			fbcurrenttileindex45 += ( fbcurrenttileindex45 < 0) ? fbtotaltiles45 : 0;
			float fblinearindextox45 = round ( fmod ( fbcurrenttileindex45, 16.0 ) );
			float fboffsetx45 = fblinearindextox45 * fbcolsoffset45;
			float fblinearindextoy45 = round( fmod( ( fbcurrenttileindex45 - fblinearindextox45 ) / 16.0, 8.0 ) );
			fblinearindextoy45 = (int)(8.0-1) - fblinearindextoy45;
			float fboffsety45 = fblinearindextoy45 * fbrowsoffset45;
			float2 fboffset45 = float2(fboffsetx45, fboffsety45);
			half2 fbuv45 = ( frac( uv_TexCoord50 ) + frac( ( 0.25 * _Time.y ) ) ) * fbtiling45 + fboffset45;
			int flipbookFrame45 = ( ( int )fbcurrenttileindex45);
			float4 tex2DNode72 = tex2D( _CloakSparks, ( float4( fbuv45, 0.0 , 0.0 ) + temp_output_54_0 + temp_output_54_0 + temp_output_54_0 ).rg );
			float4 lerpResult90 = lerp( float4( 1,1,1,0 ) , color13 , 0.5);
			float4 lerpResult70 = lerp( ( lerpResult36 + ( lerpResult9 * tex2DNode1 ) ) , ( max( tex2DNode44 , tex2DNode72 ) * lerpResult90 * 1.5 ) , saturate( ( max( tex2DNode44.a , tex2DNode72.a ) * (0.0 + (( 1.0 - saturate( abs( ( ( _CloakAmount * 2.0 ) + -1.0 ) ) ) ) - 0.0) * (1.0 - 0.0) / (0.001 - 0.0)) * 256.0 ) ));
			float4 ase_screenPos = float4( i.screenPos.xyz , i.screenPos.w + 0.00000000001 );
			float4 ase_screenPosNorm = ase_screenPos / ase_screenPos.w;
			ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
			float lerpResult94 = lerp( ase_lightAtten , tex2D( _Dithering, ase_screenPosNorm.xy ).r , 0.025);
			float2 appendResult96 = (float2(( 1.0 - lerpResult94 ) , 0.0));
			c.rgb = ( lerpResult70 * float4( tex2D( _LightingShading, appendResult96 ).rgb , 0.0 ) ).rgb;
			c.a = 1;
			clip( lerpResult29 - _Cutoff );
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
		#pragma surface surf StandardCustomLighting keepalpha fullforwardshadows 

		ENDCG
		Pass
		{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
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
				float2 customPack1 : TEXCOORD1;
				float3 worldPos : TEXCOORD2;
				float4 screenPos : TEXCOORD3;
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
				float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				half3 worldNormal = UnityObjectToWorldNormal( v.normal );
				o.customPack1.xy = customInputData.uv_texcoord;
				o.customPack1.xy = v.texcoord;
				o.worldPos = worldPos;
				TRANSFER_SHADOW_CASTER_NORMALOFFSET( o )
				o.screenPos = ComputeScreenPos( o.pos );
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
				float3 worldPos = IN.worldPos;
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				surfIN.screenPos = IN.screenPos;
				SurfaceOutputCustomLightingCustom o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutputCustomLightingCustom, o )
				surf( surfIN, o );
				UnityGI gi;
				UNITY_INITIALIZE_OUTPUT( UnityGI, gi );
				o.Alpha = LightingStandardCustomLighting( o, worldViewDir, gi ).a;
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
Node;AmplifyShaderEditor.SimpleTimeNode;48;-2032,-800;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;5;-1680,-16;Inherit;False;Property;_MipBias;MipBias;2;0;Create;True;0;0;0;False;0;False;0;-1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;7;-1744,-192;Inherit;True;Property;_MainTex;MainTex;3;0;Create;True;0;0;0;False;0;False;None;e0e1315594d2e094ab55d4c32cc28734;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.RangedFloatNode;14;-2096,-480;Inherit;False;Property;_CloakAmount;CloakAmount;5;0;Create;True;0;0;0;False;0;False;0;0.345;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;59;-1824,-928;Inherit;False;2;2;0;FLOAT;0.25;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;50;-1888,-1056;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;4,4;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;1;-1360,-64;Inherit;True;Property;_MainTex2;MainTex2;1;0;Create;True;0;0;0;False;0;False;-1;None;66a274dcc6319be4f8acc49a8aebce62;True;0;False;white;Auto;False;Object;-1;MipBias;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.FractNode;60;-1680,-976;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;83;-1376,-480;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;82;-1408,-576;Inherit;False;Constant;_Float5;Float 5;5;0;Create;True;0;0;0;False;0;False;-0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.FractNode;80;-1680,-1056;Inherit;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;51;-1472,-1056;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0.25;False;1;FLOAT2;0
Node;AmplifyShaderEditor.WireNode;76;-1424,-832;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;84;-1216,-480;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;-1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;81;-1248,-608;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;-0.5;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;88;-1296,-704;Inherit;False;Constant;_Float6;Float 6;5;0;Create;True;0;0;0;False;0;False;0.0125;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;75;-1776,-1184;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;54;-1120,-704;Inherit;False;2;2;0;FLOAT;0.04;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.AbsOpNode;85;-1088,-480;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCFlipBookUVAnimation;73;-1248,-1184;Inherit;False;0;0;7;0;FLOAT2;0,0;False;1;FLOAT;16;False;2;FLOAT;8;False;3;FLOAT;30;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;64;False;4;FLOAT2;0;FLOAT;1;FLOAT;2;INT;3
Node;AmplifyShaderEditor.TFHCFlipBookUVAnimation;45;-1248,-912;Inherit;False;0;0;7;0;FLOAT2;0,0;False;1;FLOAT;16;False;2;FLOAT;8;False;3;FLOAT;15;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;64;False;4;FLOAT2;0;FLOAT;1;FLOAT;2;INT;3
Node;AmplifyShaderEditor.WireNode;34;-1568,-288;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScreenPosInputsNode;100;32,720;Float;False;0;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;53;-960,-912;Inherit;False;4;4;0;FLOAT2;0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.TexturePropertyNode;56;-1200,-1392;Inherit;True;Property;_CloakSparks;CloakSparks;6;0;Create;True;0;0;0;False;0;False;None;3a1dcaa4f9d69ab468c3973a2b62a491;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.SimpleAddOpNode;79;-944,-1184;Inherit;False;3;3;0;FLOAT2;0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode;86;-960,-480;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;32;-1360,384;Inherit;False;3;0;FLOAT;1;False;1;FLOAT;0.4;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;33;-1360,496;Inherit;False;3;0;FLOAT;1;False;1;FLOAT;0.401;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCGrayscale;27;-1008,176;Inherit;False;0;1;0;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LightAttenuation;105;512,720;Inherit;False;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;99;224,704;Inherit;True;Property;_Dithering;Dithering;1;0;Create;True;0;0;0;False;0;False;-1;1dec9d0afe198024b9a43a07728f9376;1dec9d0afe198024b9a43a07728f9376;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.RangedFloatNode;93;544,800;Inherit;False;Constant;_Float7;Float 7;1;0;Create;True;0;0;0;False;0;False;0.025;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;10;-928,-240;Inherit;False;Constant;_Float2;Float 2;3;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;44;-800,-1232;Inherit;True;Property;_Flicker;Flicker;4;0;Create;True;0;0;0;False;0;False;-1;c0d4630fcc8b837428c7a051ec2f0e19;c0d4630fcc8b837428c7a051ec2f0e19;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.OneMinusNode;87;-800,-480;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;72;-800,-992;Inherit;True;Property;_Flicker1;Flicker;4;0;Create;True;0;0;0;False;0;False;-1;c0d4630fcc8b837428c7a051ec2f0e19;c0d4630fcc8b837428c7a051ec2f0e19;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.ColorNode;13;-992,-176;Inherit;False;Constant;_CloakColor;CloakColor;3;0;Create;True;0;0;0;False;0;False;0.06037735,0.09660377,0.64,0;0,0,0,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.LerpOp;30;-1360,160;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0.25;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;31;-1360,272;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0.2501;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;19;-752,256;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;94;764.605,727.9219;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;37;-576,-224;Inherit;False;Constant;_SpaceColor;SpaceColor;3;0;Create;True;0;0;0;False;0;False;0.00125,0,0.04,1;0,0,0,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.LerpOp;9;-752,-176;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;71;-592,-304;Inherit;False;Constant;_Float9;Float 9;5;0;Create;True;0;0;0;False;0;False;256;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;68;-624,-480;Inherit;False;5;0;FLOAT;0.1;False;1;FLOAT;0;False;2;FLOAT;0.001;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMaxOpNode;78;-448,-960;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;92;-536.9331,-750.655;Inherit;False;Constant;_Float4;Float 4;5;0;Create;True;0;0;0;False;0;False;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;26;-544,256;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;18;-752,96;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;95;924.605,727.9219;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;8;-304,-128;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;36;-304,-240;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;52;-416,-464;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMaxOpNode;77;-448,-1104;Inherit;False;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;91;-307.9331,-687.655;Inherit;False;Constant;_Float3;Float 3;5;0;Create;True;0;0;0;False;0;False;1.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;90;-315.9331,-823.655;Inherit;False;3;0;COLOR;1,1,1,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0.3;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode;25;-544,96;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;24;-384,256;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;96;1068.605,727.9219;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TexturePropertyNode;98;976,544;Inherit;True;Property;_LightingShading;LightingShading;4;0;Create;True;0;0;0;False;0;False;065cb2792dab7d74d9941bb9b785f165;065cb2792dab7d74d9941bb9b785f165;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.SimpleAddOpNode;35;-16,-144;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode;67;-224,-464;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;89;-176,-912;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;28;-192,80;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;70;176,-144;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;97;1232,704;Inherit;True;Property;_TextureSample0;Texture Sample 0;1;0;Create;True;0;0;0;False;0;False;-1;None;065cb2792dab7d74d9941bb9b785f165;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.RangedFloatNode;2;176,-208;Inherit;False;Constant;_Float0;Float 0;1;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;3;176,-32;Inherit;False;Constant;_Float1;Float 1;1;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;29;176,48;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SinTimeNode;16;-2448,-320;Inherit;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FunctionNode;65;-1600,-624;Inherit;False;Triangle Wave;-1;;2;51ec3c8d117f3ec4fa3742c3e00d535b;0;1;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;38;-2288,-256;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;1.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;41;-2112,-256;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;-1;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;39;-1920,-256;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;106;1584,32;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT3;0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;1744,-208;Float;False;True;-1;2;ASEMaterialInspector;0;0;CustomLighting;ShipSprite;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;;0;False;;False;0;False;;0;False;;False;0;Custom;0.5;True;True;0;True;Opaque;;AlphaTest;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;True;0;0;False;;0;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;0;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;16;FLOAT4;0,0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;59;1;48;0
WireConnection;1;0;7;0
WireConnection;1;2;5;0
WireConnection;60;0;59;0
WireConnection;83;0;14;0
WireConnection;80;0;50;0
WireConnection;51;0;80;0
WireConnection;51;1;60;0
WireConnection;76;0;48;0
WireConnection;84;0;83;0
WireConnection;81;0;1;0
WireConnection;81;1;82;0
WireConnection;54;0;88;0
WireConnection;54;1;81;0
WireConnection;85;0;84;0
WireConnection;73;0;75;0
WireConnection;73;5;76;0
WireConnection;45;0;51;0
WireConnection;45;5;48;0
WireConnection;34;0;14;0
WireConnection;53;0;45;0
WireConnection;53;1;54;0
WireConnection;53;2;54;0
WireConnection;53;3;54;0
WireConnection;79;0;73;0
WireConnection;79;1;54;0
WireConnection;79;2;54;0
WireConnection;86;0;85;0
WireConnection;32;2;34;0
WireConnection;33;2;34;0
WireConnection;27;0;1;0
WireConnection;99;1;100;0
WireConnection;44;0;56;0
WireConnection;44;1;79;0
WireConnection;87;0;86;0
WireConnection;72;0;56;0
WireConnection;72;1;53;0
WireConnection;30;2;34;0
WireConnection;31;2;34;0
WireConnection;19;0;27;0
WireConnection;19;1;32;0
WireConnection;19;2;33;0
WireConnection;94;0;105;0
WireConnection;94;1;99;1
WireConnection;94;2;93;0
WireConnection;9;0;10;0
WireConnection;9;1;13;0
WireConnection;9;2;34;0
WireConnection;68;0;87;0
WireConnection;78;0;44;4
WireConnection;78;1;72;4
WireConnection;26;0;19;0
WireConnection;18;0;27;0
WireConnection;18;1;30;0
WireConnection;18;2;31;0
WireConnection;95;0;94;0
WireConnection;8;0;9;0
WireConnection;8;1;1;0
WireConnection;36;1;37;0
WireConnection;36;2;34;0
WireConnection;52;0;78;0
WireConnection;52;1;68;0
WireConnection;52;2;71;0
WireConnection;77;0;44;0
WireConnection;77;1;72;0
WireConnection;90;1;13;0
WireConnection;90;2;92;0
WireConnection;25;0;18;0
WireConnection;24;0;26;0
WireConnection;96;0;95;0
WireConnection;35;0;36;0
WireConnection;35;1;8;0
WireConnection;67;0;52;0
WireConnection;89;0;77;0
WireConnection;89;1;90;0
WireConnection;89;2;91;0
WireConnection;28;0;1;4
WireConnection;28;1;25;0
WireConnection;28;2;24;0
WireConnection;70;0;35;0
WireConnection;70;1;89;0
WireConnection;70;2;67;0
WireConnection;97;0;98;0
WireConnection;97;1;96;0
WireConnection;29;0;1;4
WireConnection;29;1;28;0
WireConnection;29;2;34;0
WireConnection;65;1;14;0
WireConnection;38;0;16;4
WireConnection;41;0;38;0
WireConnection;39;0;41;0
WireConnection;106;0;70;0
WireConnection;106;1;97;5
WireConnection;0;0;2;0
WireConnection;0;10;29;0
WireConnection;0;13;106;0
ASEEND*/
//CHKSM=C83B57730E65B7FF22C2A3533E2E945254FB2153
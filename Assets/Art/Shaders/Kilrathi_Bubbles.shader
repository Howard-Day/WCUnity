// Made with Amplify Shader Editor v1.9.4.2
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Kilrathi_Bubbles"
{
	Properties
	{
		_FrontFluid("Front Fluid", 2D) = "white" {}
		_FrontBubbles("Front Bubbles", 2D) = "white" {}
		_BackFluid("Back Fluid", 2D) = "white" {}
		_MotionVectors("MotionVectors", Vector) = (0,0,0,0)
		_SphereDistortion("Sphere Distortion", 2D) = "bump" {}
		_Dithering("Dithering", 2D) = "white" {}
		[HideInInspector] _texcoord2( "", 2D ) = "white" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Back
		CGINCLUDE
		#include "UnityShaderVariables.cginc"
		#include "UnityPBSLighting.cginc"
		#include "Lighting.cginc"
		#pragma target 3.0
		struct Input
		{
			float4 vertexColor : COLOR;
			float2 uv2_texcoord2;
			float2 uv_texcoord;
			float4 screenPos;
		};

		uniform sampler2D _BackFluid;
		uniform float3 _MotionVectors;
		uniform sampler2D _SphereDistortion;
		uniform sampler2D _FrontFluid;
		uniform sampler2D _FrontBubbles;
		uniform sampler2D _Dithering;


struct Gradient
{
	int type;
	int colorsLength;
	int alphasLength;
	float4 colors[8];
	float2 alphas[8];
	};


Gradient NewGradient(int type, int colorsLength, int alphasLength, 
		float4 colors0, float4 colors1, float4 colors2, float4 colors3, float4 colors4, float4 colors5, float4 colors6, float4 colors7,
		float2 alphas0, float2 alphas1, float2 alphas2, float2 alphas3, float2 alphas4, float2 alphas5, float2 alphas6, float2 alphas7)
{
	Gradient g;
	g.type = type;
	g.colorsLength = colorsLength;
	g.alphasLength = alphasLength;
	g.colors[ 0 ] = colors0;
	g.colors[ 1 ] = colors1;
	g.colors[ 2 ] = colors2;
	g.colors[ 3 ] = colors3;
	g.colors[ 4 ] = colors4;
	g.colors[ 5 ] = colors5;
	g.colors[ 6 ] = colors6;
	g.colors[ 7 ] = colors7;
	g.alphas[ 0 ] = alphas0;
	g.alphas[ 1 ] = alphas1;
	g.alphas[ 2 ] = alphas2;
	g.alphas[ 3 ] = alphas3;
	g.alphas[ 4 ] = alphas4;
	g.alphas[ 5 ] = alphas5;
	g.alphas[ 6 ] = alphas6;
	g.alphas[ 7 ] = alphas7;
	return g;
}


		float3 RotateAroundAxis( float3 center, float3 original, float3 u, float angle )
		{
			original -= center;
			float C = cos( angle );
			float S = sin( angle );
			float t = 1 - C;
			float m00 = t * u.x * u.x + C;
			float m01 = t * u.x * u.y - S * u.z;
			float m02 = t * u.x * u.z + S * u.y;
			float m10 = t * u.x * u.y + S * u.z;
			float m11 = t * u.y * u.y + C;
			float m12 = t * u.y * u.z - S * u.x;
			float m20 = t * u.x * u.z - S * u.y;
			float m21 = t * u.y * u.z + S * u.x;
			float m22 = t * u.z * u.z + C;
			float3x3 finalMatrix = float3x3( m00, m01, m02, m10, m11, m12, m20, m21, m22 );
			return mul( finalMatrix, original ) + center;
		}


float4 SampleGradient( Gradient gradient, float time )
{
	float3 color = gradient.colors[0].rgb;
	UNITY_UNROLL
	for (int c = 1; c < 8; c++)
	{
	float colorPos = saturate((time - gradient.colors[c-1].w) / ( 0.00001 + (gradient.colors[c].w - gradient.colors[c-1].w)) * step(c, (float)gradient.colorsLength-1));
	color = lerp(color, gradient.colors[c].rgb, lerp(colorPos, step(0.01, colorPos), gradient.type));
	}
	#ifndef UNITY_COLORSPACE_GAMMA
	color = half3(GammaToLinearSpaceExact(color.r), GammaToLinearSpaceExact(color.g), GammaToLinearSpaceExact(color.b));
	#endif
	float alpha = gradient.alphas[0].x;
	UNITY_UNROLL
	for (int a = 1; a < 8; a++)
	{
	float alphaPos = saturate((time - gradient.alphas[a-1].y) / ( 0.00001 + (gradient.alphas[a].y - gradient.alphas[a-1].y)) * step(a, (float)gradient.alphasLength-1));
	alpha = lerp(alpha, gradient.alphas[a].x, lerp(alphaPos, step(0.01, alphaPos), gradient.type));
	}
	return float4(color, alpha);
}


		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float temp_output_1_0 = 0.0;
			float3 temp_cast_0 = (temp_output_1_0).xxx;
			o.Albedo = temp_cast_0;
			Gradient gradient61 = NewGradient( 0, 2, 2, float4( 0, 0, 0, 0 ), float4( 1, 1, 1, 0.6882429 ), 0, 0, 0, 0, 0, 0, float2( 1, 0 ), float2( 1, 1 ), 0, 0, 0, 0, 0, 0 );
			float temp_output_10_0 = ( _MotionVectors.z * 1.0 );
			float mulTime21 = _Time.y * -0.5;
			float temp_output_19_0 = ( ( -1.0 * _MotionVectors.x ) + mulTime21 + i.uv2_texcoord2.x );
			float2 appendResult49 = (float2(temp_output_19_0 , ( _MotionVectors.y * 0.25 )));
			float3 rotatedValue80 = RotateAroundAxis( float3( ( float2( 0.5,0.5 ) + appendResult49 ) ,  0.0 ), float3( ( i.uv_texcoord + appendResult49 ) ,  0.0 ), float3( 0,0,1 ), temp_output_10_0 );
			float lerpResult154 = lerp( 0.0625 , 0.25 , i.vertexColor.a);
			float lerpResult157 = lerp( 0.55 , 0.45 , i.vertexColor.a);
			float3 tex2DNode30 = UnpackNormal( tex2D( _SphereDistortion, i.uv_texcoord ) );
			float2 temp_output_34_0 = (tex2DNode30).xy;
			float2 temp_output_44_0 = ( temp_output_34_0 * ( 1.0 - tex2DNode30.b ) );
			float3 lerpResult52 = lerp( (rotatedValue80*lerpResult154 + lerpResult157) , float3( temp_output_44_0 ,  0.0 ) , 0.15);
			float4 tex2DNode51 = tex2D( _BackFluid, lerpResult52.xy );
			float temp_output_83_0 = ( pow( tex2DNode30.b , 4.0 ) * 1.0 );
			float2 appendResult7 = (float2(temp_output_19_0 , ( _MotionVectors.y * -0.25 )));
			float3 rotatedValue9 = RotateAroundAxis( float3( ( float2( 0.5,0.5 ) + appendResult7 ) ,  0.0 ), float3( ( i.uv_texcoord + appendResult7 ) ,  0.0 ), float3( 0,0,1 ), temp_output_10_0 );
			float3 lerpResult28 = lerp( (rotatedValue9*lerpResult154 + lerpResult157) , float3( temp_output_44_0 ,  0.0 ) , 0.15);
			float4 tex2DNode3 = tex2D( _FrontFluid, lerpResult28.xy );
			float mulTime139 = _Time.y * -0.5;
			float2 appendResult136 = (float2(( i.uv_texcoord.x + i.uv2_texcoord2.x + ( _MotionVectors.x * -0.5 ) ) , ( i.uv_texcoord.y + mulTime139 + i.uv2_texcoord2.y )));
			float2 lerpResult140 = lerp( (( appendResult136 * float2( 1.5,1.5 ) )*lerpResult154 + lerpResult157) , ( temp_output_34_0 * pow( ( 1.0 - tex2DNode30.b ) , 0.25 ) ) , 0.35);
			float4 tex2DNode134 = tex2D( _FrontBubbles, lerpResult140 );
			float4 lerpResult142 = lerp( ( ( tex2DNode3 * 0.5 ) + ( tex2DNode30.b * 0.35 ) ) , tex2DNode134 , tex2DNode134.a);
			float4 lerpResult55 = lerp( ( ( tex2DNode51 * 0.4 ) * ( temp_output_83_0 + temp_output_83_0 + 1.0 ) ) , lerpResult142 , tex2DNode3.a);
			float4 ase_screenPos = float4( i.screenPos.xyz , i.screenPos.w + 0.00000000001 );
			float4 ase_screenPosNorm = ase_screenPos / ase_screenPos.w;
			ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
			float4 tex2DNode67 = tex2D( _Dithering, ase_screenPosNorm.xy );
			float4 lerpResult66 = lerp( ( lerpResult55 * 1.5 ) , tex2DNode67 , 0.025);
			float4 temp_output_65_0 = ( floor( ( lerpResult66 * 16.0 ) ) / 16.0 );
			Gradient gradient112 = NewGradient( 0, 2, 2, float4( 0, 0, 0, 0.6882429 ), float4( 0.75, 0.75, 0.75, 0.997055 ), 0, 0, 0, 0, 0, 0, float2( 1, 0 ), float2( 1, 1 ), 0, 0, 0, 0, 0, 0 );
			float4 temp_cast_11 = (0.2).xxxx;
			o.Emission = ( ( i.vertexColor * SampleGradient( gradient61, temp_output_65_0.r ) ) + SampleGradient( gradient112, ( temp_output_65_0 - temp_cast_11 ).r ) ).rgb;
			o.Metallic = temp_output_1_0;
			o.Smoothness = 1.0;
			Gradient gradient121 = NewGradient( 0, 2, 2, float4( 0, 0, 0, 0 ), float4( 1, 1, 1, 1 ), 0, 0, 0, 0, 0, 0, float2( 1, 0 ), float2( 1, 1 ), 0, 0, 0, 0, 0, 0 );
			float4 temp_cast_14 = (( ( 2.0 * pow( max( ( tex2DNode51.a * 1.0 ) , tex2DNode3.a ) , 0.5 ) * pow( tex2DNode30.b , 6.0 ) ) + ( tex2DNode134.a * 0.0 ) )).xxxx;
			float4 lerpResult122 = lerp( temp_cast_14 , tex2DNode67 , 0.25);
			o.Alpha = SampleGradient( gradient121, ( floor( ( lerpResult122 * 3.0 ) ) / 3.0 ).r ).r;
		}

		ENDCG
		CGPROGRAM
		#pragma surface surf Standard alpha:fade keepalpha fullforwardshadows exclude_path:deferred 

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
			sampler3D _DitherMaskLOD;
			struct v2f
			{
				V2F_SHADOW_CASTER;
				float4 customPack1 : TEXCOORD1;
				float3 worldPos : TEXCOORD2;
				float4 screenPos : TEXCOORD3;
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
				float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				half3 worldNormal = UnityObjectToWorldNormal( v.normal );
				o.customPack1.xy = customInputData.uv2_texcoord2;
				o.customPack1.xy = v.texcoord1;
				o.customPack1.zw = customInputData.uv_texcoord;
				o.customPack1.zw = v.texcoord;
				o.worldPos = worldPos;
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
				surfIN.uv2_texcoord2 = IN.customPack1.xy;
				surfIN.uv_texcoord = IN.customPack1.zw;
				float3 worldPos = IN.worldPos;
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				surfIN.screenPos = IN.screenPos;
				surfIN.vertexColor = IN.color;
				SurfaceOutputStandard o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutputStandard, o )
				surf( surfIN, o );
				#if defined( CAN_SKIP_VPOS )
				float2 vpos = IN.pos;
				#endif
				half alphaRef = tex3D( _DitherMaskLOD, float3( vpos.xy * 0.25, o.Alpha * 0.9375 ) ).a;
				clip( alphaRef - 0.01 );
				SHADOW_CASTER_FRAGMENT( IN )
			}
			ENDCG
		}
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19402
Node;AmplifyShaderEditor.Vector3Node;8;-2240,-64;Inherit;False;Property;_MotionVectors;MotionVectors;3;0;Create;True;0;0;0;False;0;False;0,0,0;-0.07440157,-0.2038426,0.6171685;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.TextureCoordinatesNode;133;-2192,208;Inherit;False;1;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleTimeNode;21;-2160,128;Inherit;False;1;0;FLOAT;-0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;26;-1968,-32;Inherit;False;2;2;0;FLOAT;-1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;19;-1792,-16;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;11;-1824,112;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;-0.25;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;4;-2048,-240;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;114;-1200,352;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;7;-1552,48;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;50;-1792,-128;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.25;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;49;-1552,-160;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;10;-1552,144;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;139;-2160,352;Inherit;False;1;0;FLOAT;-0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;152;-1904,304;Inherit;False;2;2;0;FLOAT;-4;False;1;FLOAT;-0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;5;-1344,-16;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;79;-1344,80;Inherit;False;2;2;0;FLOAT2;0.5,0.5;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.VertexColorNode;156;-1376,-688;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;30;-992,336;Inherit;True;Property;_SphereDistortion;Sphere Distortion;4;0;Create;True;0;0;0;False;0;False;-1;None;297ff98da3df7094fbd2020f5e34caf4;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;48;-1344,-240;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;81;-1376,-128;Inherit;False;2;2;0;FLOAT2;0.5,0.5;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ComponentMaskNode;34;-688,336;Inherit;False;True;True;False;False;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;137;-1696,240;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RotateAboutAxisNode;9;-1136,-64;Inherit;False;False;4;0;FLOAT3;0,0,1;False;1;FLOAT;25;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.LerpOp;154;-1040,-640;Inherit;False;3;0;FLOAT;0.0625;False;1;FLOAT;0.25;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;138;-1696,368;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;157;-1040,-512;Inherit;False;3;0;FLOAT;0.55;False;1;FLOAT;0.45;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;45;-672,416;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RotateAboutAxisNode;80;-1120,-272;Inherit;False;False;4;0;FLOAT3;0,0,1;False;1;FLOAT;25;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;39;-560,-32;Inherit;False;Constant;_Float2;Float 2;4;0;Create;True;0;0;0;False;0;False;0.15;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;136;-1552,240;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;47;-800,-96;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT;0.125;False;2;FLOAT;0.525;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;44;-464,336;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.OneMinusNode;159;-672,528;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;53;-800,-256;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT;0.125;False;2;FLOAT;0.525;False;1;FLOAT3;0
Node;AmplifyShaderEditor.LerpOp;28;-256,80;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;158;-1360,224;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;1.5,1.5;False;1;FLOAT2;0
Node;AmplifyShaderEditor.WireNode;94;-512,112;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;161;-512,544;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;0.25;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;52;-240,-304;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;135;-1120,144;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT;0.125;False;2;FLOAT;0.55;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;3;-64,32;Inherit;True;Property;_FrontFluid;Front Fluid;0;0;Create;True;0;0;0;False;0;False;-1;0431467aaf846a74c93120924fd71e32;0431467aaf846a74c93120924fd71e32;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;104;64,224;Inherit;False;Constant;_Float8;Float 3;5;0;Create;True;0;0;0;False;0;False;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;110;-112,-80;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;4;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;87;-576,480;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;149;-368,800;Inherit;False;Constant;_Float12;Float 12;6;0;Create;True;0;0;0;False;0;False;0.35;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;160;-352,512;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;51;-64,-352;Inherit;True;Property;_BackFluid;Back Fluid;2;0;Create;True;0;0;0;False;0;False;-1;0431467aaf846a74c93120924fd71e32;9edbad5347f2c054f806deb23902027f;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;57;64,-160;Inherit;False;Constant;_Float3;Float 3;5;0;Create;True;0;0;0;False;0;False;0.4;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;103;256,32;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;86;64,304;Inherit;False;2;2;0;FLOAT;0.5;False;1;FLOAT;0.35;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;132;192,-32;Inherit;False;Constant;_Float11;Float 11;5;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;83;64,-80;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;140;-96,752;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;56;240,-352;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;85;416,32;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;130;352,-128;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;2;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;134;80,752;Inherit;True;Property;_FrontBubbles;Front Bubbles;1;0;Create;True;0;0;0;False;0;False;-1;0431467aaf846a74c93120924fd71e32;fccdc07079843064db29d5b67a283007;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;106;352,144;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;142;528,32;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.WireNode;148;576,240;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;131;416,-352;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;55;704,-64;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;102;743.1108,66.67542;Inherit;False;Constant;_Float7;Float 7;5;0;Create;True;0;0;0;False;0;False;1.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMaxOpNode;54;768,144;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;144;912,576;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;126;96,448;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;6;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScreenPosInputsNode;69;640,272;Float;False;0;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;101;881.1108,-50.32458;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;68;928,64;Inherit;False;Constant;_Float5;Float 5;6;0;Create;True;0;0;0;False;0;False;0.025;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;109;912,144;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;127;1152,448;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;146;1008,544;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;67;832,256;Inherit;True;Property;_Dithering;Dithering;5;0;Create;True;0;0;0;False;0;False;-1;1dec9d0afe198024b9a43a07728f9376;1dec9d0afe198024b9a43a07728f9376;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;66;1184,-64;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;63;1360,0;Inherit;False;Constant;_Float4;Float 4;5;0;Create;True;0;0;0;False;0;False;16;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;125;1232,112;Inherit;False;3;3;0;FLOAT;2;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;145;1216,256;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;62;1520,-48;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;124;1232,272;Inherit;False;Constant;_Float9;Float 5;6;0;Create;True;0;0;0;False;0;False;0.25;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;143;1392,112;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FloorOpNode;64;1664,-48;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;116;1504,448;Inherit;False;Constant;_Float6;Float 4;5;0;Create;True;0;0;0;False;0;False;3;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;122;1536,144;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;65;1776,-64;Inherit;False;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;117;1664,400;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;129;1808,144;Inherit;False;Constant;_Float10;Float 10;5;0;Create;True;0;0;0;False;0;False;0.2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GradientNode;61;1680,-176;Inherit;False;0;2;2;0,0,0,0;1,1,1,0.6882429;1,0;1,1;0;1;OBJECT;0
Node;AmplifyShaderEditor.GradientSampleNode;59;1952,-112;Inherit;True;2;0;OBJECT;;False;1;FLOAT;0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FloorOpNode;118;1808,400;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;128;2000,96;Inherit;False;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.VertexColorNode;153;2048,-432;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GradientNode;112;1728,64;Inherit;False;0;2;2;0,0,0,0.6882429;0.75,0.75,0.75,0.997055;1,0;1,1;0;1;OBJECT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;98;2288,-128;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GradientSampleNode;113;2144,64;Inherit;True;2;0;OBJECT;;False;1;FLOAT;0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleDivideOpNode;119;1920,384;Inherit;False;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.GradientNode;121;1824,272;Inherit;False;0;2;2;0,0,0,0;1,1,1,1;1,0;1,1;0;1;OBJECT;0
Node;AmplifyShaderEditor.ColorNode;100;2032,-272;Inherit;False;Constant;_Color0;Color 0;5;0;Create;True;0;0;0;False;0;False;0.427451,0.003921569,0.6117647,1;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;84;-224,-144;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;1;2368,-336;Inherit;False;Constant;_Float0;Float 0;0;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;2;2368,-272;Inherit;False;Constant;_Float1;Float 1;0;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;111;2464,-128;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GradientSampleNode;120;2096,336;Inherit;True;2;0;OBJECT;;False;1;FLOAT;0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;2864,-160;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;Kilrathi_Bubbles;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Back;0;False;;0;False;;False;0;False;;0;False;;False;0;Transparent;0.5;True;True;0;False;Transparent;;Transparent;ForwardOnly;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;True;2;5;False;;10;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;17;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;16;FLOAT4;0,0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;26;1;8;1
WireConnection;19;0;26;0
WireConnection;19;1;21;0
WireConnection;19;2;133;1
WireConnection;11;0;8;2
WireConnection;7;0;19;0
WireConnection;7;1;11;0
WireConnection;50;0;8;2
WireConnection;49;0;19;0
WireConnection;49;1;50;0
WireConnection;10;0;8;3
WireConnection;152;0;8;1
WireConnection;5;0;4;0
WireConnection;5;1;7;0
WireConnection;79;1;7;0
WireConnection;30;1;114;0
WireConnection;48;0;4;0
WireConnection;48;1;49;0
WireConnection;81;1;49;0
WireConnection;34;0;30;0
WireConnection;137;0;4;1
WireConnection;137;1;133;1
WireConnection;137;2;152;0
WireConnection;9;1;10;0
WireConnection;9;2;79;0
WireConnection;9;3;5;0
WireConnection;154;2;156;4
WireConnection;138;0;4;2
WireConnection;138;1;139;0
WireConnection;138;2;133;2
WireConnection;157;2;156;4
WireConnection;45;0;30;3
WireConnection;80;1;10;0
WireConnection;80;2;81;0
WireConnection;80;3;48;0
WireConnection;136;0;137;0
WireConnection;136;1;138;0
WireConnection;47;0;9;0
WireConnection;47;1;154;0
WireConnection;47;2;157;0
WireConnection;44;0;34;0
WireConnection;44;1;45;0
WireConnection;159;0;30;3
WireConnection;53;0;80;0
WireConnection;53;1;154;0
WireConnection;53;2;157;0
WireConnection;28;0;47;0
WireConnection;28;1;44;0
WireConnection;28;2;39;0
WireConnection;158;0;136;0
WireConnection;94;0;30;3
WireConnection;161;0;159;0
WireConnection;52;0;53;0
WireConnection;52;1;44;0
WireConnection;52;2;39;0
WireConnection;135;0;158;0
WireConnection;135;1;154;0
WireConnection;135;2;157;0
WireConnection;3;1;28;0
WireConnection;110;0;94;0
WireConnection;87;0;30;3
WireConnection;160;0;34;0
WireConnection;160;1;161;0
WireConnection;51;1;52;0
WireConnection;103;0;3;0
WireConnection;103;1;104;0
WireConnection;86;0;87;0
WireConnection;83;0;110;0
WireConnection;140;0;135;0
WireConnection;140;1;160;0
WireConnection;140;2;149;0
WireConnection;56;0;51;0
WireConnection;56;1;57;0
WireConnection;85;0;103;0
WireConnection;85;1;86;0
WireConnection;130;0;83;0
WireConnection;130;1;83;0
WireConnection;130;2;132;0
WireConnection;134;1;140;0
WireConnection;106;0;51;4
WireConnection;142;0;85;0
WireConnection;142;1;134;0
WireConnection;142;2;134;4
WireConnection;148;0;3;4
WireConnection;131;0;56;0
WireConnection;131;1;130;0
WireConnection;55;0;131;0
WireConnection;55;1;142;0
WireConnection;55;2;148;0
WireConnection;54;0;106;0
WireConnection;54;1;3;4
WireConnection;144;0;134;4
WireConnection;126;0;87;0
WireConnection;101;0;55;0
WireConnection;101;1;102;0
WireConnection;109;0;54;0
WireConnection;127;0;126;0
WireConnection;146;0;144;0
WireConnection;67;1;69;0
WireConnection;66;0;101;0
WireConnection;66;1;67;0
WireConnection;66;2;68;0
WireConnection;125;1;109;0
WireConnection;125;2;127;0
WireConnection;145;0;146;0
WireConnection;62;0;66;0
WireConnection;62;1;63;0
WireConnection;143;0;125;0
WireConnection;143;1;145;0
WireConnection;64;0;62;0
WireConnection;122;0;143;0
WireConnection;122;1;67;0
WireConnection;122;2;124;0
WireConnection;65;0;64;0
WireConnection;65;1;63;0
WireConnection;117;0;122;0
WireConnection;117;1;116;0
WireConnection;59;0;61;0
WireConnection;59;1;65;0
WireConnection;118;0;117;0
WireConnection;128;0;65;0
WireConnection;128;1;129;0
WireConnection;98;0;153;0
WireConnection;98;1;59;0
WireConnection;113;0;112;0
WireConnection;113;1;128;0
WireConnection;119;0;118;0
WireConnection;119;1;116;0
WireConnection;84;0;94;0
WireConnection;111;0;98;0
WireConnection;111;1;113;0
WireConnection;120;0;121;0
WireConnection;120;1;119;0
WireConnection;0;0;1;0
WireConnection;0;2;111;0
WireConnection;0;3;1;0
WireConnection;0;4;2;0
WireConnection;0;9;120;0
ASEEND*/
//CHKSM=686F7FB5C41B94EB6CB5246689800094824EF5EA
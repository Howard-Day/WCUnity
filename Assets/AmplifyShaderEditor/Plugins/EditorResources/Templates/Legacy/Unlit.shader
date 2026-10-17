Shader /*ase_name*/ "Hidden/Built-In/Unlit" /*end*/
{
	Properties
	{
		/*ase_props*/
	}
	
	SubShader
	{
		/*ase_subshader_options:Name=Additional Options
			Option:Surface:Opaque,Transparent:Opaque
				Opaque:SetPropertyOnSubShader:RenderType,Opaque
				Opaque:SetPropertyOnSubShader:RenderQueue,Geometry
				Opaque:SetPropertyOnPass:Unlit:ZWrite,On
				Opaque:ShowOption:  Keep Alpha
				Opaque:HideOption:  Blend
				Opaque:RefreshOption:Alpha Clipping
				Opaque:RemoveDefine:ASE_SURFACE_TRANSPARENT
				Transparent:SetPropertyOnSubShader:RenderType,Transparent
				Transparent:SetPropertyOnSubShader:RenderQueue,Transparent
				Transparent:SetPropertyOnPass:Unlit:ZWrite,Off
				Transparent:HideOption:  Keep Alpha
				Transparent:ShowOption:  Blend
				Transparent:SetDefine:ASE_SURFACE_TRANSPARENT
			Option:  Keep Alpha:false,true:false
				true:SetDefine:ASE_OPAQUE_KEEP_ALPHA
				false:RemoveDefine:ASE_OPAQUE_KEEP_ALPHA
			Option:  Blend:Alpha,Premultiply,Additive,Multiply,Custom:Alpha
				Alpha:SetPropertyOnPass:Unlit:BlendRGB,SrcAlpha,OneMinusSrcAlpha
				Premultiply:SetPropertyOnPass:Unlit:BlendRGB,One,OneMinusSrcAlpha
				Additive:SetPropertyOnPass:Unlit:BlendRGB,One,One
				Multiply:SetPropertyOnPass:Unlit:BlendRGB,DstColor,Zero
				Alpha,Premultiply,Additive:SetPropertyOnPass:Unlit:BlendAlpha,One,OneMinusSrcAlpha
				Multiply:SetPropertyOnPass:Unlit:BlendAlpha,One,Zero
				disable:SetPropertyOnPass:Unlit:BlendRGB,One,Zero
				disable:SetPropertyOnPass:Unlit:BlendAlpha,One,Zero
			Option:Alpha Clipping:false,true:false
				true:ShowOption:  Use Shadow Threshold
				true:ShowPort:Alpha Clip Threshold
				true:SetDefine:_ALPHATEST_ON
				false:HideOption:  Use Shadow Threshold
				false:HidePort:Alpha Clip Threshold
				false:RemoveDefine:_ALPHATEST_ON
			Option:  Use Shadow Threshold:false,true:false
				true:SetDefine:_ALPHATEST_SHADOW_ON 1
				true:ShowPort:Alpha Clip Threshold Shadow
				true:SetShaderProperty:_UseShadowThreshold,1
				false,disable:RemoveDefine:_ALPHATEST_SHADOW_ON 1
				false,disable:HidePort:Alpha Clip Threshold Shadow
			Option:Cast Shadows:false,true:true
				true:IncludePass:ShadowCaster
				false,disable:ExcludePass:ShadowCaster
				true?Alpha Clipping=true:ShowOption:  Use Shadow Threshold
				false:HideOption:  Use Shadow Threshold
			Option:Write Depth:false,true:false
				true:SetDefine:ASE_WRITE_DEPTH
				true:ShowOption:  Conservative
				true:ShowPort:ExtraPrePass:Depth
				true:ShowPort:Unlit:Depth
				false,disable:RemoveDefine:ASE_WRITE_DEPTH
				false,disable:HideOption:  Conservative
				false,disable:HidePort:ExtraPrePass:Depth
				false,disable:HidePort:Unlit:Depth
			Option:  Conservative:false,true:false
				true:SetDefine:ASE_WRITE_DEPTH_CONSERVATIVE
				false,disable:RemoveDefine:ASE_WRITE_DEPTH_CONSERVATIVE
			Option:Extra Pre Pass:false,true:false
				true:IncludePass:ExtraPrePass
				false,disable:ExcludePass:ExtraPrePass
			Option:Vertex Position,InvertActionOnDeselection:Absolute,Relative:Relative
				Absolute:SetDefine:ASE_ABSOLUTE_VERTEX_POS 1
				Absolute:SetPortName:_Vertex,Vertex Position
				Relative:SetPortName:_Vertex,Vertex Offset
				Absolute:SetPortName:ExtraPrePass:3,Vertex Position
				Relative:SetPortName:ExtraPrePass:3,Vertex Offset
		*/

		/*ase_unity_cond_begin:<=10000000*/
			// A list of master node input port IDs; will be excluded from generated shaders.
			//  0 => Frag: Color
			//  8 => Frag: Alpha Clip Threshold
			//  9 => Frag: Alpha Clip Threshold Shadow
			//  7 => Frag: Alpha
			// 15 => Vert: Vertex Offset
			// 16 => Vert: Vertex Normal
			// 17 => Vert: Vertex Tangent
			// 28 => Frag: Depth
		/*ase_unity_cond_end*/

		Tags
		{
			"RenderType"="Opaque"
		}

		LOD 0

		ZWrite On
		ZClip True
		Cull Back
		AlphaToMask Off
		ColorMask RGBA
		Blend One Zero, One Zero
		BlendOp Add, Add
		Offset 0,0

		/*ase_stencil*/

		/*ase_all_modules*/

		CGINCLUDE
			#pragma target 3.5
			#pragma exclude_renderers d3d9 // ensure rendering platforms toggle list is visible

			#ifndef GLOBAL_HEADER_INCLUDED
			#define GLOBAL_HEADER_INCLUDED
			float4 ComputeClipSpacePosition( float2 screenPosNorm, float deviceDepth )
			{
				float4 positionCS = float4( screenPosNorm * 2.0 - 1.0, deviceDepth, 1.0 );
			#if UNITY_UV_STARTS_AT_TOP
				positionCS.y = -positionCS.y;
			#endif
				return positionCS;
			}
			#endif
		ENDCG

		/*ase_pass*/
		Pass
		{
			Name "ExtraPrePass"
			
			Blend One Zero
			Cull Back
			ZWrite On
			ZTest LEqual
			Offset 0,0
			ColorMask RGBA

			/*ase_stencil*/

			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_instancing
				#include "UnityCG.cginc"

				/*ase_pragma*/

				#if defined(ASE_WRITE_DEPTH_CONSERVATIVE) && (SHADER_TARGET >= 45)
					#define ASE_SV_DEPTH SV_DepthLessEqual
					#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
				#else
					#define ASE_SV_DEPTH SV_Depth
					#define ASE_SV_POSITION_QUALIFIERS
				#endif

				struct appdata
				{
					float4 vertex : POSITION;
					half3 normal : NORMAL;
					half4 tangent : TANGENT;
					float4 texcoord1 : TEXCOORD1;
					float4 texcoord2 : TEXCOORD2;
					/*ase_vdata:p=p;t=t;n=n;uv1=tc1.xyzw;uv2=tc2.xyzw*/
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				struct v2f
				{
					ASE_SV_POSITION_QUALIFIERS float4 pos : SV_POSITION;
					/*ase_interp(0,):sp=sp*/
					UNITY_VERTEX_INPUT_INSTANCE_ID
					UNITY_VERTEX_OUTPUT_STEREO
				};

				/*ase_globals*/

				/*ase_funcs*/

				v2f vert( appdata v /*ase_vert_input*/ )
				{
					UNITY_SETUP_INSTANCE_ID(v);
					v2f o;
					UNITY_INITIALIZE_OUTPUT(v2f,o);
					UNITY_TRANSFER_INSTANCE_ID(v,o);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

					/*ase_vert_code:v=appdata;o=v2f*/

					#ifdef ASE_ABSOLUTE_VERTEX_POS
						float3 defaultVertexValue = v.vertex.xyz;
					#else
						float3 defaultVertexValue = float3(0, 0, 0);
					#endif
					float3 vertexValue = /*ase_vert_out:Vertex Offset;Float3;3;-1;_VertexP*/defaultVertexValue/*end*/;
					#ifdef ASE_ABSOLUTE_VERTEX_POS
						v.vertex.xyz = vertexValue;
					#else
						v.vertex.xyz += vertexValue;
					#endif
					v.vertex.w = 1;
					v.normal = /*ase_vert_out:Vertex Normal;Float3;4;-1;_VertexNormalP*/v.normal/*end*/;
					v.tangent = /*ase_vert_out:Vertex Tangent;Float4;5;-1;_VertexTangentP*/v.tangent/*end*/;

					float3 positionWS = mul( unity_ObjectToWorld, v.vertex ).xyz;
					half3 normalWS = UnityObjectToWorldNormal( v.normal );
					half3 tangentWS = UnityObjectToWorldDir( v.tangent.xyz );

					o.pos = UnityObjectToClipPos( v.vertex );

				#if defined( ASE_SHADOWS )
					UNITY_TRANSFER_SHADOW( o, v.texcoord );
				#endif
					return o;
				}

				half4 frag( v2f IN /*ase_frag_input*/
							#if defined( ASE_WRITE_DEPTH )
								, out float outputDepth : ASE_SV_DEPTH
							#endif
							) : SV_Target
				{
					UNITY_SETUP_INSTANCE_ID( IN );
					UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

					/*ase_local_var:spn*/float4 ScreenPosNorm = float4( IN.pos.xy * ( _ScreenParams.zw - 1.0 ), IN.pos.zw );
					/*ase_local_var:sp*/float4 ClipPos = ComputeClipSpacePosition( ScreenPosNorm.xy, IN.pos.z ) * IN.pos.w;
					/*ase_local_var:spu*/float4 ScreenPos = ComputeScreenPos( ClipPos );

					/*ase_frag_code:IN=v2f*/

					half3 Color = /*ase_frag_out:Color;Float3;0;-1;_ColorP*/half3( 0, 0, 0 )/*end*/;
					half Alpha = /*ase_frag_out:Alpha;Float;1;-1;_AlphaP*/1/*end*/;
					half AlphaClipThreshold = /*ase_frag_out:Alpha Clip Threshold;Float;2;-1;_AlphaClipP*/0.5/*end*/;

					#if defined( ASE_WRITE_DEPTH )
						outputDepth = /*ase_frag_out:Depth;Float;28;-1;_DeviceDepth*/IN.pos.z/*end*/;
					#endif

					#ifdef _ALPHATEST_ON
						clip( Alpha - AlphaClipThreshold );
					#endif

					return half4( Color, Alpha );
				}
			ENDCG
		}

		/*ase_pass*/
		Pass
		{
			/*ase_main_pass*/
			Name "Unlit"

			Cull Back
			ZWrite On
			ZTest LEqual
			Offset 0,0
			ColorMask RGBA
			Blend One Zero, One Zero
			BlendOp Add, Add

			/*ase_stencil*/

			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_instancing
				#include "UnityCG.cginc"

				/*ase_pragma*/

				#if defined(ASE_WRITE_DEPTH_CONSERVATIVE) && (SHADER_TARGET >= 45)
					#define ASE_SV_DEPTH SV_DepthLessEqual
					#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
				#else
					#define ASE_SV_DEPTH SV_Depth
					#define ASE_SV_POSITION_QUALIFIERS
				#endif

				struct appdata
				{
					float4 vertex : POSITION;
					float3 normal : NORMAL;
					float4 tangent : TANGENT;
					/*ase_vdata:p=p;n=n;t=t*/
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				struct v2f
				{
					ASE_SV_POSITION_QUALIFIERS float4 pos : SV_POSITION;
					/*ase_interp(0,):sp=sp.xyzw*/
					UNITY_VERTEX_INPUT_INSTANCE_ID
					UNITY_VERTEX_OUTPUT_STEREO
				};

				/*ase_globals*/

				/*ase_funcs*/

				v2f vert( appdata v /*ase_vert_input*/ )
				{
					UNITY_SETUP_INSTANCE_ID(v);
					v2f o;
					UNITY_INITIALIZE_OUTPUT(v2f,o);
					UNITY_TRANSFER_INSTANCE_ID(v,o);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

					/*ase_vert_code:v=appdata;o=v2f*/

					#ifdef ASE_ABSOLUTE_VERTEX_POS
						float3 defaultVertexValue = v.vertex.xyz;
					#else
						float3 defaultVertexValue = float3(0, 0, 0);
					#endif
					float3 vertexValue = /*ase_vert_out:Vertex Offset;Float3;15;-1;_Vertex*/defaultVertexValue/*end*/;
					#ifdef ASE_ABSOLUTE_VERTEX_POS
						v.vertex.xyz = vertexValue;
					#else
						v.vertex.xyz += vertexValue;
					#endif
					v.vertex.w = 1;
					v.normal = /*ase_vert_out:Vertex Normal;Float3;16;-1;_VertexNormal*/v.normal/*end*/;
					v.tangent = /*ase_vert_out:Vertex Tangent;Float4;17;-1;_VertexTangent*/v.tangent/*end*/;

					o.pos = UnityObjectToClipPos( v.vertex );

					#if defined( ASE_SHADOWS )
						UNITY_TRANSFER_SHADOW( o, v.texcoord );
					#endif
					return o;
				}

				half4 frag( v2f IN /*ase_frag_input*/
							#if defined( ASE_WRITE_DEPTH )
								, out float outputDepth : SV_Depth
							#endif
				) : SV_Target
				{
					UNITY_SETUP_INSTANCE_ID( IN );
					UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

					/*ase_local_var:spn*/float4 ScreenPosNorm = float4( IN.pos.xy * ( _ScreenParams.zw - 1.0 ), IN.pos.zw );
					/*ase_local_var:sp*/float4 ClipPos = ComputeClipSpacePosition( ScreenPosNorm.xy, IN.pos.z ) * IN.pos.w;
					/*ase_local_var:spu*/float4 ScreenPos = ComputeScreenPos( ClipPos );

					/*ase_frag_code:IN=v2f*/

					float3 Color = /*ase_frag_out:Color;Float3;0;-1;_Color*/float3( 1, 1, 1 )/*end*/;
					float Alpha = /*ase_frag_out:Alpha;Float;7;-1;_Alpha*/1/*end*/;
					half AlphaClipThreshold = /*ase_frag_out:Alpha Clip Threshold;Float;8;-1;_AlphaClip*/0.5/*end*/;
					half AlphaClipThresholdShadow = /*ase_frag_out:Alpha Clip Threshold Shadow;Float;9;-1;_AlphaClipShadow*/0.5/*end*/;

					#if defined( ASE_WRITE_DEPTH )
						outputDepth = /*ase_frag_out:Depth;Float;28;-1;_DeviceDepth*/IN.pos.z/*end*/;
					#endif

					#ifdef _ALPHATEST_ON
						clip( Alpha - AlphaClipThreshold );
					#endif

				#if defined( ASE_SURFACE_TRANSPARENT ) || defined( ASE_OPAQUE_KEEP_ALPHA )
					return half4( Color, Alpha );
				#else
					return half4( Color, 1.0 );
				#endif
				}
			ENDCG
		}

		/*ase_pass*/
		Pass
		{
			/*ase_hide_pass*/
			Name "ShadowCaster"
			Tags { "LightMode"="ShadowCaster" }

			ZWrite On
			ZTest LEqual
			AlphaToMask Off

			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_shadowcaster
				#ifndef UNITY_PASS_SHADOWCASTER
					#define UNITY_PASS_SHADOWCASTER
				#endif
				#include "UnityCG.cginc"

				/*ase_pragma*/

				#if defined(ASE_WRITE_DEPTH_CONSERVATIVE) && (SHADER_TARGET >= 45)
					#define ASE_SV_DEPTH SV_DepthLessEqual
					#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
				#else
					#define ASE_SV_DEPTH SV_Depth
					#define ASE_SV_POSITION_QUALIFIERS
				#endif

				struct appdata
				{
					float4 vertex : POSITION;
					float3 normal : NORMAL;
					float4 tangent : TANGENT;
					/*ase_vdata:p=p;n=n;t=t*/
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				struct v2f
				{
					ASE_SV_POSITION_QUALIFIERS UNITY_POSITION( pos );
					V2F_SHADOW_CASTER_NOPOS
					/*ase_interp(1,):sp=sp*/
					UNITY_VERTEX_INPUT_INSTANCE_ID
					UNITY_VERTEX_OUTPUT_STEREO
				};

				#ifdef UNITY_STANDARD_USE_DITHER_MASK
					sampler3D _DitherMaskLOD;
				#endif

				/*ase_globals*/

				/*ase_funcs*/

				v2f vert( appdata v /*ase_vert_input*/ )
				{
					UNITY_SETUP_INSTANCE_ID( v );
					v2f o;
					UNITY_INITIALIZE_OUTPUT( v2f, o );
					UNITY_TRANSFER_INSTANCE_ID( v, o );
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );

					/*ase_vert_code:v=appdata;o=v2f*/

					#ifdef ASE_ABSOLUTE_VERTEX_POS
						float3 defaultVertexValue = v.vertex.xyz;
					#else
						float3 defaultVertexValue = float3(0, 0, 0);
					#endif
					float3 vertexValue = /*ase_vert_out:Vertex Offset;Float3;15;-1;_Vertex*/defaultVertexValue/*end*/;
					#ifdef ASE_ABSOLUTE_VERTEX_POS
						v.vertex.xyz = vertexValue;
					#else
						v.vertex.xyz += vertexValue;
					#endif
					v.vertex.w = 1;
					v.normal = /*ase_vert_out:Vertex Normal;Float3;16;-1;_VertexNormal*/v.normal/*end*/;
					v.tangent = /*ase_vert_out:Vertex Tangent;Float4;17;-1;_VertexTangent*/v.tangent/*end*/;

					TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
					return o;
				}

				half4 frag( v2f IN /*ase_frag_input*/
							#if defined( ASE_WRITE_DEPTH )
								, out float outputDepth : SV_Depth
							#endif
							) : SV_Target
				{
					UNITY_SETUP_INSTANCE_ID(IN);

					#ifdef LOD_FADE_CROSSFADE
						UNITY_APPLY_DITHER_CROSSFADE(IN.pos.xy);
					#endif

					/*ase_frag_code:IN=v2f*/

					float Alpha = /*ase_frag_out:Alpha;Float;7;-1;_Alpha*/1/*end*/;
					half AlphaClipThreshold = /*ase_frag_out:Alpha Clip Threshold;Float;8;-1;_AlphaClip*/0.5/*end*/;
					half AlphaClipThresholdShadow = /*ase_frag_out:Alpha Clip Threshold Shadow;Float;9;-1;_AlphaClipShadow*/0.5/*end*/;

					#if defined( ASE_WRITE_DEPTH )
						outputDepth = /*ase_frag_out:Depth;Float;28;-1;_DeviceDepth*/IN.pos.z/*end*/;
					#endif

					#ifdef _ALPHATEST_SHADOW_ON
						if (unity_LightShadowBias.z != 0.0)
							clip(Alpha - AlphaClipThresholdShadow);
						#ifdef _ALPHATEST_ON
						else
							clip(Alpha - AlphaClipThreshold);
						#endif
					#else
						#ifdef _ALPHATEST_ON
							clip(Alpha - AlphaClipThreshold);
						#endif
					#endif

					#ifdef UNITY_STANDARD_USE_DITHER_MASK
						half alphaRef = tex3D(_DitherMaskLOD, float3(IN.pos.xy*0.25,Alpha*0.9375)).a;
						clip(alphaRef - 0.01);
					#endif

					SHADOW_CASTER_FRAGMENT(IN)
				}
			ENDCG
		}
		/*ase_pass_end*/
	}
	/*ase_lod*/
	CustomEditor "AmplifyShaderEditor.MaterialInspector"
}

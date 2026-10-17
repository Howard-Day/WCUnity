Shader /*ase_name*/ "Hidden/Built-In/Wireframe" /*end*/
{
	Properties
	{
		_WireframeSmoothing ( "Wireframe Smoothing", Range( 0, 10 ) ) = 1
		_WireframeThickness ( "Wireframe Thickness", Range( 0, 10 ) ) = 1

		/*ase_props*/
	}

	SubShader
	{
		/*ase_subshader_options:Name=Additional Options
			Option:Vertex Position,InvertActionOnDeselection:Absolute,Relative:Relative
				Absolute:SetDefine:ASE_ABSOLUTE_VERTEX_POS 1
				Absolute:SetPortName:_VertexOffset,Vertex Position
				Relative:SetPortName:_VertexOffset,Vertex Offset
			Option:Wireframe Mode:Opaque,Alpha Blend,Alpha Custom:Opaque
				Opaque:RemoveDefine:ASE_WIREFRAME_ALPHA
				Opaque:ShowPort:_SurfaceColor
				Opaque:SetPropertyOnPass:Unlit:BlendRGB,One,Zero
				Opaque:SetPropertyOnSubShader:RenderType,Opaque
				Opaque:SetPropertyOnSubShader:RenderQueue,Geometry
				Opaque:SetPropertyOnSubShader:ZWrite,On
				Alpha Blend:SetDefine:ASE_WIREFRAME_ALPHA
				Alpha Blend:HidePort:_SurfaceColor
				Alpha Blend:SetPropertyOnPass:Unlit:BlendRGB,SrcAlpha,OneMinusSrcAlpha
				Alpha Blend:SetPropertyOnSubShader:RenderType,Transparent
				Alpha Blend:SetPropertyOnSubShader:RenderQueue,Transparent
				Alpha Blend:SetPropertyOnSubShader:ZWrite,Off
				Alpha Custom:SetDefine:ASE_WIREFRAME_ALPHA
				Alpha Custom:HidePort:_SurfaceColor
		*/

		/*ase_unity_cond_begin:<=10000000*/
			// A list of master node input port IDs; will be excluded from generated shaders.
			//  0 => Frag: Wire Color
			//  1 => Frag: Surface Color
			//  2 => Vert: Vertex Offset
		/*ase_unity_cond_end*/

		Tags
		{
			"RenderType"="Opaque"
		}

		LOD 0

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
			/*ase_main_pass*/
			Name "Unlit"

			Blend One Zero

			CGPROGRAM
				#pragma require geometry

				#pragma vertex vert_setup
				#pragma geometry geom
				#pragma fragment frag

				#pragma multi_compile_instancing
				#include "UnityCG.cginc"

				/*ase_pragma*/

				struct appdata
				{
					float4 vertex : POSITION;
					/*ase_vdata:p=p*/
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				struct v2f
				{
					float4 pos : SV_POSITION;
					float3 baryCoords : TEXCOORD0;
					/*ase_interp(1,):sp=sp.xyzw*/
					UNITY_VERTEX_INPUT_INSTANCE_ID
					UNITY_VERTEX_OUTPUT_STEREO
				};

				/*ase_globals*/

				/*ase_funcs*/

				appdata vert_setup( appdata v )
				{
					return v;
				}

				v2f vert_apply( appdata v, float3 baryCoords /*ase_vert_input*/ )
				{
					v2f o;
					UNITY_SETUP_INSTANCE_ID( v );
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );
					UNITY_TRANSFER_INSTANCE_ID( v, o );

					/*ase_vert_code:v=appdata;o=v2f*/

					float3 vertexValue = float3( 0, 0, 0 );
					#if ASE_ABSOLUTE_VERTEX_POS
						vertexValue = v.vertex.xyz;
					#endif

					vertexValue = /*ase_vert_out:Vertex Offset;Float3;2;-1;_VertexOffset*/vertexValue/*end*/;

					#if ASE_ABSOLUTE_VERTEX_POS
						v.vertex.xyz = vertexValue;
					#else
						v.vertex.xyz += vertexValue;
					#endif

					o.pos = UnityObjectToClipPos( v.vertex );
					o.baryCoords = baryCoords;
					return o;
				}

				[maxvertexcount(15)]
				void geom( triangle appdata input[3], uint pid : SV_PrimitiveID, inout TriangleStream<v2f> outStream )
				{
					outStream.Append( vert_apply( input[ 0 ], float3( 1, 0, 0 ) ) );
					outStream.Append( vert_apply( input[ 1 ], float3( 0, 1, 0 ) ) );
					outStream.Append( vert_apply( input[ 2 ], float3( 0, 0, 1 ) ) );
				}

				float _WireframeSmoothing;
				float _WireframeThickness;

				half ComputeWireframe( float3 baryCoords )
				{
					float3 deltas = fwidth( baryCoords );
					float3 smoothing = deltas * _WireframeSmoothing;
					float3 thickness = deltas * _WireframeThickness;
					baryCoords = smoothstep( thickness, thickness + smoothing, baryCoords );
					float minBary = min( baryCoords.x, min( baryCoords.y, baryCoords.z ) );
					return 1 - minBary;
				}

				half4 frag( v2f IN /*ase_frag_input*/ ) : SV_Target
				{
					UNITY_SETUP_INSTANCE_ID( IN );
					UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

					/*ase_local_var:spn*/float4 ScreenPosNorm = float4( IN.pos.xy * ( _ScreenParams.zw - 1.0 ), IN.pos.zw );
					/*ase_local_var:sp*/float4 ClipPos = ComputeClipSpacePosition( ScreenPosNorm.xy, IN.pos.z ) * IN.pos.w;
					/*ase_local_var:spu*/float4 ScreenPos = ComputeScreenPos( ClipPos );

					/*ase_frag_code:IN=v2f*/

					half3 wireColor = /*ase_frag_out:Wire Color;Float3;0;-1;_WireColor*/half3( 0, 0, 0 )/*end*/;
					half3 surfaceColor = /*ase_frag_out:Surface Color;Float3;1;-1;_SurfaceColor*/half3( 1, 1, 1 )/*end*/;

					half wire = ComputeWireframe( IN.baryCoords );

					half4 finalColor;
				#if defined( ASE_WIREFRAME_ALPHA )
					finalColor.rgb = wireColor;
					finalColor.a = wire;
				#else
					finalColor.rgb = lerp( surfaceColor.rgb, wireColor.rgb, wire );
					finalColor.a = 1;
				#endif
					return finalColor;
				}
			ENDCG
		}
	}
	/*ase_lod*/
	CustomEditor "AmplifyShaderEditor.MaterialInspector"
}

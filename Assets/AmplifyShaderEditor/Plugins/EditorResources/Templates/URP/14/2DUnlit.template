Shader /*ase_name*/ "Hidden/Universal/Sprite Unlit" /*end*/
{
	Properties
	{
		/*ase_props*/

		[HideInInspector][NoScaleOffset] unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
	}

	SubShader
	{
		PackageRequirements
		{
			"com.unity.render-pipelines.universal": "[14.0,15.0]"
		}

		/*ase_subshader_options:Name=Additional Options
			Option:Alpha Clipping:false,true:false
				true:SetDefine:ALPHA_CLIP_THRESHOLD
				true:ShowPort:Alpha Clip Threshold
				false,disable:RemoveDefine:ALPHA_CLIP_THRESHOLD
				false,disable:HidePort:Alpha Clip Threshold
			Option:Vertex Position:Absolute,Relative:Relative
				Absolute:SetDefine:ASE_ABSOLUTE_VERTEX_POS 1
				Absolute:SetPortName:_Vertex,Vertex Position
				Relative:RemoveDefine:ASE_ABSOLUTE_VERTEX_POS 1
				Relative:SetPortName:_Vertex,Vertex Offset
			Option:Debug Display:false,true:false
				true:SetDefine:pragma multi_compile _ DEBUG_DISPLAY
				false,disable:RemoveDefine:pragma multi_compile _ DEBUG_DISPLAY
			Option:External Alpha:false,true:false
				true:SetDefine:pragma multi_compile _ ETC1_EXTERNAL_ALPHA
				false,disable:RemoveDefine:pragma multi_compile _ ETC1_EXTERNAL_ALPHA
		*/

		/*ase_unity_cond_begin:<=10000000*/
			// A list of master node input port IDs; will be excluded from generated shaders.
			//  0 => Frag: Color
			// .1 => Frag: Alpha
			//  2 => Frag: Mask
			//  3 => Frag: Normal
			//  4 => Vert: Vertex Offset
			//  5 => Vert: Vertex Normal
			//  6 => Vert: Vertex Tangent
			//  7 => Frag: Alpha Clip Threshold
		/*ase_unity_cond_end*/

		Tags
		{
			"RenderPipeline" = "UniversalPipeline"
			"RenderType" = "Transparent"
			"Queue" = "Transparent+0"
			"UniversalMaterialType" = "Unlit"
			"ShaderGraphShader"="true"
			"ShaderGraphTargetId"=""
		}

		LOD 0

		Cull Off

		HLSLINCLUDE
		#pragma target 2.0
		#pragma prefer_hlslcc gles
		#pragma exclude_renderers d3d9 // ensure rendering platforms toggle list is visible

		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"

		ENDHLSL

		/*ase_pass*/
		Pass
		{
			Name "Sprite Unlit"
            Tags
            {
                "LightMode" = "Universal2D"
            }

			Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
			ZTest LEqual
			ZWrite Off
			Offset 0,0
			ColorMask RGBA

			/*ase_stencil*/

			HLSLPROGRAM

			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            #define _SURFACE_TYPE_TRANSPARENT 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_COLOR
            #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
            #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
            #define VARYINGS_NEED_POSITION_WS
            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_COLOR
            #define FEATURES_GRAPH_VERTEX

			#define SHADERPASS SHADERPASS_SPRITEUNLIT

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			/*ase_unity_cond_begin:>=20220359*/
			#if ASE_SRP_VERSION >= 140012
			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
			#endif
			/*ase_unity_cond_end*/

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"

			/*ase_unity_cond_begin:>=20220316*/
			#if ASE_SRP_VERSION >=140009
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#endif
			/*ase_unity_cond_end*/

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SurfaceData2D.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging2D.hlsl"

			/*ase_pragma*/

			half4 _RendererColor;

			/*ase_globals*/

			struct VertexInput
			{
				float3 positionOS : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float4 uv0 : TEXCOORD0;
				float4 color : COLOR;
				/*ase_vdata:p=p;n=n;t=t;c=c;uv0=tc0*/
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 positionCS : SV_POSITION;
				float4 texCoord0 : TEXCOORD0;
				float4 color : TEXCOORD1;
				float3 positionWS : TEXCOORD2;
				/*ase_interp(3,):sp=sp;uv0=tc0;c=tc1;wp=tc2*/
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			#if ETC1_EXTERNAL_ALPHA
				TEXTURE2D(_AlphaTex); SAMPLER(sampler_AlphaTex);
				float _EnableAlphaTexture;
			#endif

			/*ase_funcs*/

			VertexOutput vert( VertexInput v /*ase_vert_input*/ )
			{
				VertexOutput o = (VertexOutput)0;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

			#ifdef UNITY_INSTANCING_ENABLED
				v.positionOS = UnityFlipSprite( v.positionOS, unity_SpriteProps.xy );
			#endif

				/*ase_vert_code:v=VertexInput;o=VertexOutput*/

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.positionOS;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = /*ase_vert_out:Vertex Offset;Float3;4;-1;_Vertex*/defaultVertexValue/*end*/;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.positionOS = vertexValue;
				#else
					v.positionOS += vertexValue;
				#endif
				v.normal = /*ase_vert_out:Vertex Normal;Float3;5;-1;_VNormal*/v.normal/*end*/;
				v.tangent.xyz = /*ase_vert_out:Vertex Tangent;Float3;6;-1;_VTangent*/v.tangent.xyz/*end*/;

				VertexPositionInputs vertexInput = GetVertexPositionInputs(v.positionOS);

				o.positionCS = vertexInput.positionCS;
				o.positionWS = vertexInput.positionWS;
				o.texCoord0 = v.uv0;
				o.color = v.color * _RendererColor;
			#ifdef UNITY_INSTANCING_ENABLED
				o.color *= unity_SpriteColor;
			#endif
				return o;
			}

			half4 frag( VertexOutput IN /*ase_frag_input*/ ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

				/*ase_local_var:sp*/float4 positionCS = IN.positionCS;
				/*ase_local_var:wp*/float3 positionWS = IN.positionWS;

				/*ase_frag_code:IN=VertexOutput*/

				float4 Color = /*ase_frag_out:Color;Float4;0;-1;_Color*/float4( 1, 1, 1, 1 )/*end*/;
				float AlphaClipThreshold = /*ase_frag_out:Alpha Clip Threshold;Float;7;-1;_AlphaClip*/0.5/*end*/;

			#if defined( ALPHA_CLIP_THRESHOLD )
				clip( Color.a - AlphaClipThreshold );
			#endif

			#if defined(DEBUG_DISPLAY)
				SurfaceData2D surfaceData;
				InitializeSurfaceData(Color.rgb, Color.a, surfaceData);
				InputData2D inputData;
				InitializeInputData(positionWS.xy, half2(IN.texCoord0.xy), inputData);
				half4 debugColor = 0;

				SETUP_DEBUG_DATA_2D(inputData, IN.positionWS);

				if (CanDebugOverrideOutputColor(surfaceData, inputData, debugColor))
				{
					return debugColor;
				}
			#endif

			#if ETC1_EXTERNAL_ALPHA
				float4 alpha = SAMPLE_TEXTURE2D(_AlphaTex, sampler_AlphaTex, IN.texCoord0.xy);
				Color.a = lerp( Color.a, alpha.r, _EnableAlphaTexture);
			#endif

				Color *= IN.color;

				return Color;
			}

			ENDHLSL
		}

		/*ase_pass*/
		Pass
		{
			/*ase_hide_pass:SyncP*/
            Name "Sprite Unlit Forward"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

			Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
			ZTest LEqual
			ZWrite Off
			Offset 0,0
			ColorMask RGBA

			/*ase_stencil*/

			HLSLPROGRAM

			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            #define _SURFACE_TYPE_TRANSPARENT 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_COLOR
            #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
            #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
            #define VARYINGS_NEED_POSITION_WS
            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_COLOR
            #define FEATURES_GRAPH_VERTEX

			#define SHADERPASS SHADERPASS_SPRITEFORWARD

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			/*ase_unity_cond_begin:>=20220359*/
			#if ASE_SRP_VERSION >= 140012
			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
			#endif
			/*ase_unity_cond_end*/

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			
			/*ase_unity_cond_begin:>=20220316*/
			#if ASE_SRP_VERSION >=140009
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#endif
			/*ase_unity_cond_end*/

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SurfaceData2D.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging2D.hlsl"

			/*ase_pragma*/

			half4 _RendererColor;

			/*ase_globals*/

			struct VertexInput
			{
				float3 positionOS : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float4 uv0 : TEXCOORD0;
				float4 color : COLOR;
				/*ase_vdata:p=p;n=n;t=t;c=c;uv0=tc0*/
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 positionCS : SV_POSITION;
				float4 texCoord0 : TEXCOORD0;
				float4 color : TEXCOORD1;
				float3 positionWS : TEXCOORD2;
				/*ase_interp(3,):sp=sp;uv0=tc0;c=tc1;wp=tc2*/
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			#if ETC1_EXTERNAL_ALPHA
				TEXTURE2D( _AlphaTex ); SAMPLER( sampler_AlphaTex );
				float _EnableAlphaTexture;
			#endif

			/*ase_funcs*/

			VertexOutput vert( VertexInput v /*ase_vert_input*/ )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

			#ifdef UNITY_INSTANCING_ENABLED
				v.positionOS = UnityFlipSprite( v.positionOS, unity_SpriteProps.xy );
			#endif

				/*ase_vert_code:v=VertexInput;o=VertexOutput*/

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.positionOS;
				#else
					float3 defaultVertexValue = float3( 0, 0, 0 );
				#endif
				float3 vertexValue = /*ase_vert_out:Vertex Offset;Float3;4;-1;_Vertex*/defaultVertexValue/*end*/;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.positionOS = vertexValue;
				#else
					v.positionOS += vertexValue;
				#endif
				v.normal = /*ase_vert_out:Vertex Normal;Float3;5;-1;_VNormal*/v.normal/*end*/;
				v.tangent.xyz = /*ase_vert_out:Vertex Tangent;Float3;6;-1;_VTangent*/v.tangent.xyz/*end*/;

				VertexPositionInputs vertexInput = GetVertexPositionInputs(v.positionOS);

				o.positionCS = vertexInput.positionCS;
				o.positionWS = vertexInput.positionWS;
				o.texCoord0 = v.uv0;
				o.color = v.color * _RendererColor;
			#ifdef UNITY_INSTANCING_ENABLED
				o.color *= unity_SpriteColor;
			#endif
				return o;
			}

			half4 frag( VertexOutput IN /*ase_frag_input*/ ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

				/*ase_local_var:sp*/float4 positionCS = IN.positionCS;
				/*ase_local_var:wp*/float3 positionWS = IN.positionWS;

				/*ase_frag_code:IN=VertexOutput*/

				float4 Color = /*ase_frag_out:Color;Float4;0;-1;_Color*/float4( 1, 1, 1, 1 )/*end*/;
				float AlphaClipThreshold = /*ase_frag_out:Alpha Clip Threshold;Float;7;-1;_AlphaClip*/0.5/*end*/;

			#if defined( ALPHA_CLIP_THRESHOLD )
				clip( Color.a - AlphaClipThreshold );
			#endif

			#if defined(DEBUG_DISPLAY)
				SurfaceData2D surfaceData;
				InitializeSurfaceData(Color.rgb, Color.a, surfaceData);
				InputData2D inputData;
				InitializeInputData(positionWS.xy, half2(IN.texCoord0.xy), inputData);
				half4 debugColor = 0;

				SETUP_DEBUG_DATA_2D(inputData, IN.positionWS);

				if (CanDebugOverrideOutputColor(surfaceData, inputData, debugColor))
				{
					return debugColor;
				}
			#endif

			#if ETC1_EXTERNAL_ALPHA
				float4 alpha = SAMPLE_TEXTURE2D( _AlphaTex, sampler_AlphaTex, IN.texCoord0.xy );
				Color.a = lerp( Color.a, alpha.r, _EnableAlphaTexture );
			#endif

				Color *= IN.color;

				return Color;
			}

			ENDHLSL
		}

		/*ase_pass*/
        Pass
        {
			/*ase_hide_pass*/
            Name "SceneSelectionPass"
            Tags
            {
                "LightMode" = "SceneSelectionPass"
            }

            Cull Off

            HLSLPROGRAM

			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            #define _SURFACE_TYPE_TRANSPARENT 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
            #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
            #define FEATURES_GRAPH_VERTEX

            #define SHADERPASS SHADERPASS_DEPTHONLY
			#define SCENESELECTIONPASS 1

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			/*ase_unity_cond_begin:>=20220359*/
			#if ASE_SRP_VERSION >= 140012
			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
			#endif
			/*ase_unity_cond_end*/

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			
			/*ase_unity_cond_begin:>=20220316*/
			#if ASE_SRP_VERSION >=140009
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#endif
			/*ase_unity_cond_end*/

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			/*ase_pragma*/

			/*ase_globals*/

            struct VertexInput
			{
				float3 positionOS : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				/*ase_vdata:p=p;n=n;t=t*/
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 positionCS : SV_POSITION;
				/*ase_interp(0,):sp=sp*/
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

            int _ObjectId;
            int _PassValue;

			/*ase_funcs*/

			VertexOutput vert(VertexInput v /*ase_vert_input*/)
			{
				VertexOutput o = (VertexOutput)0;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

			#ifdef UNITY_INSTANCING_ENABLED
				v.positionOS = UnityFlipSprite( v.positionOS, unity_SpriteProps.xy );
			#endif

				/*ase_vert_code:v=VertexInput;o=VertexOutput*/
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.positionOS;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = /*ase_vert_out:Vertex Offset;Float3;4;-1;_Vertex*/defaultVertexValue/*end*/;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.positionOS = vertexValue;
				#else
					v.positionOS += vertexValue;
				#endif

				VertexPositionInputs vertexInput = GetVertexPositionInputs(v.positionOS);

				o.positionCS = vertexInput.positionCS;
				return o;
			}

			half4 frag(VertexOutput IN/*ase_frag_input*/) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

				/*ase_frag_code:IN=VertexOutput*/

				float4 Color = /*ase_frag_out:Color;Float4;0;-1;_Color*/float4( 1, 1, 1, 1 )/*end*/;
				float AlphaClipThreshold = /*ase_frag_out:Alpha Clip Threshold;Float;7;-1;_AlphaClip*/0.5/*end*/;

				#if defined( ALPHA_CLIP_THRESHOLD )
					clip( Color.a - AlphaClipThreshold );
				#endif

				return half4(_ObjectId, _PassValue, 1.0, 1.0);
			}

            ENDHLSL
        }

		/*ase_pass*/
        Pass
        {
			/*ase_hide_pass*/
            Name "ScenePickingPass"
            Tags
            {
                "LightMode" = "Picking"
            }

			Cull Off

            HLSLPROGRAM

			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            #define _SURFACE_TYPE_TRANSPARENT 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
            #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
            #define FEATURES_GRAPH_VERTEX

            #define SHADERPASS SHADERPASS_DEPTHONLY
			#define SCENEPICKINGPASS 1

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			/*ase_unity_cond_begin:>=20220359*/
			#if ASE_SRP_VERSION >= 140012
			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
			#endif
			/*ase_unity_cond_end*/

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			
			/*ase_unity_cond_begin:>=20220316*/
			#if ASE_SRP_VERSION >=140009
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#endif
			/*ase_unity_cond_end*/

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

        	/*ase_pragma*/

			/*ase_globals*/

            struct VertexInput
			{
				float3 positionOS : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				/*ase_vdata:p=p;n=n;t=t*/
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 positionCS : SV_POSITION;
				/*ase_interp(0,):sp=sp*/
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

            float4 _SelectionID;

			/*ase_funcs*/

			VertexOutput vert(VertexInput v /*ase_vert_input*/ )
			{
				VertexOutput o = (VertexOutput)0;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

			#ifdef UNITY_INSTANCING_ENABLED
				v.positionOS = UnityFlipSprite( v.positionOS, unity_SpriteProps.xy );
			#endif

				/*ase_vert_code:v=VertexInput;o=VertexOutput*/

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.positionOS;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = /*ase_vert_out:Vertex Offset;Float3;4;-1;_Vertex*/defaultVertexValue/*end*/;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.positionOS = vertexValue;
				#else
					v.positionOS += vertexValue;
				#endif

				VertexPositionInputs vertexInput = GetVertexPositionInputs(v.positionOS);

				o.positionCS = vertexInput.positionCS;
				return o;
			}

			half4 frag(VertexOutput IN /*ase_frag_input*/) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

				/*ase_frag_code:IN=VertexOutput*/

				float4 Color = /*ase_frag_out:Color;Float4;0;-1;_Color*/float4( 1, 1, 1, 1 )/*end*/;
				float AlphaClipThreshold = /*ase_frag_out:Alpha Clip Threshold;Float;7;-1;_AlphaClip*/0.5/*end*/;

				#if defined( ALPHA_CLIP_THRESHOLD )
					clip( Color.a - AlphaClipThreshold );
				#endif

				return unity_SelectionID;
			}

            ENDHLSL
        }
		/*ase_pass_end*/
	}
	/*ase_lod*/

	/*ase_unity_cond_begin:<=10000000*/
	SubShader { Pass { } } // Prevent "shader is not supported on this GPU"; will be excluded from generated shaders.
	/*ase_unity_cond_end*/

	CustomEditor "UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI"
	FallBack "Hidden/Shader Graph/FallbackError"
}

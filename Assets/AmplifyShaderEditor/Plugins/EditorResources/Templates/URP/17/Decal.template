Shader  /*ase_name*/"Hidden/Universal/Decal"/*end*/
{
	Properties
    {
        /*ase_props*/

        [HideInInspector] _DrawOrder("Draw Order", Range(-50, 50)) = 0
        [HideInInspector][Enum(Depth Bias, 0, View Bias, 1)] _DecalMeshBiasType("DecalMesh BiasType", Float) = 0

        [HideInInspector] _DecalMeshDepthBias("DecalMesh DepthBias", Float) = 0
        [HideInInspector] _DecalMeshViewBias("DecalMesh ViewBias", Float) = 0

        [HideInInspector][NoScaleOffset] unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}

        //_DecalAngleFadeSupported("Decal Angle Fade Supported", Float) = 1
    }

    SubShader
    {
		PackageRequirements
		{
			"com.unity.render-pipelines.universal": "[17.0,18.0]"
		}

		/*ase_subshader_options:Name=Additional Options
			Option:Affect BaseColor:false,true:true
				true:SetDefine:_MATERIAL_AFFECTS_ALBEDO 1
				true:SetPropertyOnPass:DBufferProjector:ColorMask,true,true,true,true
				true:SetPropertyOnPass:DecalGBufferProjector:ColorMask,true,true,true,false
				true:SetPropertyOnPass:DBufferMesh:ColorMask,true,true,true,true
				true:SetPropertyOnPass:DecalGBufferMesh:ColorMask,true,true,true,false
				false:RemoveDefine:_MATERIAL_AFFECTS_ALBEDO 1
				false:SetPropertyOnPass:DBufferProjector:ColorMask,false,false,false,false
				false:SetPropertyOnPass:DecalGBufferProjector:ColorMask,false,false,false,false
				false:SetPropertyOnPass:DBufferMesh:ColorMask,false,false,false,false
				false:SetPropertyOnPass:DecalGBufferMesh:ColorMask,false,false,false,false
			Option:Affect Normal:false,true:true
				true:SetDefine:_MATERIAL_AFFECTS_NORMAL 1
				true:SetPropertyOnPass:DBufferProjector:ColorMask1,true,true,true,true
				true:SetPropertyOnPass:DecalGBufferProjector:ColorMask2,true,true,true,false
				true:SetPropertyOnPass:DBufferMesh:ColorMask1,true,true,true,true
				true:SetPropertyOnPass:DecalGBufferMesh:ColorMask2,true,true,true,false
				true:ShowPort:DecalScreenSpaceProjector:Normal
				true:ShowOption:  Blend
				false:RemoveDefine:_MATERIAL_AFFECTS_NORMAL 1
				false:SetPropertyOnPass:DBufferProjector:ColorMask1,false,false,false,false
				false:SetPropertyOnPass:DecalGBufferProjector:ColorMask2,false,false,false,false
				false:SetPropertyOnPass:DBufferMesh:ColorMask1,false,false,false,false
				false:SetPropertyOnPass:DecalGBufferMesh:ColorMask2,false,false,false,false
				false:HidePort:DecalScreenSpaceProjector:Normal
				false:HideOption:  Blend
				false:SetOption:  Blend,0
			Option:  Blend:false,true:true
				true:SetDefine:_MATERIAL_AFFECTS_NORMAL_BLEND 1
				true:ShowPort:DecalScreenSpaceProjector:Normal Alpha
				false:RemoveDefine:_MATERIAL_AFFECTS_NORMAL_BLEND 1
				false:HidePort:DecalScreenSpaceProjector:Normal Alpha
			Option:Affect MAOS:false,true:false
				true:SetDefine: _MATERIAL_AFFECTS_MAOS 1
				true:SetPropertyOnPass:DBufferProjector:ColorMask2,true,true,true,true
				true:SetPropertyOnPass:DBufferMesh:ColorMask2,true,true,true,true
				true:ShowPort:DecalScreenSpaceProjector:Metallic
				true:ShowPort:DecalScreenSpaceProjector:Occlusion
				true:ShowPort:DecalScreenSpaceProjector:Smoothness
				true:ShowPort:DecalScreenSpaceProjector:MAOS Alpha
				false:RemoveDefine: _MATERIAL_AFFECTS_MAOS 1
				false:SetPropertyOnPass:DBufferProjector:ColorMask2,false,false,false,false
				false:SetPropertyOnPass:DBufferMesh:ColorMask2,false,false,false,false
				false:HidePort:DecalScreenSpaceProjector:Metallic
				false:HidePort:DecalScreenSpaceProjector:Occlusion
				false:HidePort:DecalScreenSpaceProjector:Smoothness
				false:HidePort:DecalScreenSpaceProjector:MAOS Alpha
			Option:Affect Emission:false,true:false
				true:SetDefine:_MATERIAL_AFFECTS_EMISSION 1
				true:IncludePass:DecalProjectorForwardEmissive
				true:IncludePass:DecalMeshForwardEmissive
				true:ShowPort:DecalScreenSpaceProjector:Emission
				false:RemoveDefine:_MATERIAL_AFFECTS_EMISSION 1
				false:ExcludePass:DecalProjectorForwardEmissive
				false:ExcludePass:DecalMeshForwardEmissive
				false:HidePort:DecalScreenSpaceProjector:Emission
			Option:Support LOD CrossFade:false,true:false
			    true:SetDefine:DBufferMesh:USE_UNITY_CROSSFADE 1
				true:SetDefine:DecalScreenSpaceMesh:USE_UNITY_CROSSFADE 1
				true:SetDefine:DecalProjectorForwardEmissive:USE_UNITY_CROSSFADE 1
				true:SetDefine:DecalGBufferMesh:USE_UNITY_CROSSFADE 1
				true:SetDefine:DBufferMesh:pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
				true:SetDefine:DecalScreenSpaceMesh:pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
				true:SetDefine:DecalProjectorForwardEmissive:pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
				true:SetDefine:DecalGBufferMesh:pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
				false:RemoveDefine:DBufferMesh:USE_UNITY_CROSSFADE 1
				false:RemoveDefine:DecalScreenSpaceMesh:USE_UNITY_CROSSFADE 1
				false:RemoveDefine:DecalProjectorForwardEmissive:USE_UNITY_CROSSFADE 1
				false:RemoveDefine:DecalGBufferMesh:USE_UNITY_CROSSFADE 1
				false:RemoveDefine:DBufferMesh:pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
				false:RemoveDefine:DecalScreenSpaceMesh:pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
				false:RemoveDefine:DecalProjectorForwardEmissive:pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
				false:RemoveDefine:DecalGBufferMesh:pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
			Option:Angle Fade:false,true:true
				true:SetDefine:DBufferProjector:DECAL_ANGLE_FADE 1
				true:SetDefine:DecalScreenSpaceProjector:DECAL_ANGLE_FADE 1
				true:SetDefine:DecalProjectorForwardEmissive:DECAL_ANGLE_FADE 1
				true:SetDefine:DecalGBufferProjector:DECAL_ANGLE_FADE 1
				false:RemoveDefine:DBufferProjector:DECAL_ANGLE_FADE 1
				false:RemoveDefine:DecalScreenSpaceProjector:DECAL_ANGLE_FADE 1
				false:RemoveDefine:DecalProjectorForwardEmissive:DECAL_ANGLE_FADE 1
				false:RemoveDefine:DecalGBufferProjector:DECAL_ANGLE_FADE 1
				true:SetShaderProperty:_DecalAngleFadeSupported,[HideInInspector] _DecalAngleFadeSupported("Decal Angle Fade Supported", Float) = 1
				false:SetShaderProperty:_DecalAngleFadeSupported,//[HideInInspector] _DecalAngleFadeSupported("Decal Angle Fade Supported", Float) = 1
		*/

		/*ase_unity_cond_begin:<=10000000*/
			// A list of master node input port IDs; will be excluded from generated shaders.
			//  0 => Frag: Base Color
			//  1 => Frag: Alpha
			//  2 => Frag: Normal
			//  3 => Frag: Normal Alpha
			//  4 => Frag: Metallic
			//  5 => Frag: Occlusion
			//  6 => Frag: Smoothness
			//  7 => Frag: MAOS Alpha
			//  8 => Frag: Emission
		/*ase_unity_cond_end*/

		Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "PreviewType"="Plane"
		    "DisableBatching" = "LODFading"
            "ShaderGraphShader"="true"
			"ShaderGraphTargetId" = "UniversalDecalSubTarget"
        }

		LOD 0

		HLSLINCLUDE
		#pragma target 3.5
		#pragma prefer_hlslcc gles
		#pragma exclude_renderers d3d9 // ensure rendering platforms toggle list is visible

		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"

		#if ( UNITY_VERSION >= 60070000 )
			#define ASE_LIGHT_ACCUM3 URP_LIGHT_ACCUM3
			#define ASE_LIGHT_ACCUM4 URP_LIGHT_ACCUM4
		#else
			#define ASE_LIGHT_ACCUM3 half3
			#define ASE_LIGHT_ACCUM4 half4
		#endif
		ENDHLSL

		/*ase_pass*/
        Pass
        {
			/*ase_hide_pass*/
            /*ase_immutable*/ Name "DBufferProjector"
            Tags
            {
                "LightMode" = "DBufferProjector"
            }

			Cull Front
			Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			ZTest Greater
			ZWrite Off
			ColorMask RGBA
			ColorMask RGBA 1
			ColorMask RGBA 2

            HLSLPROGRAM

			#pragma vertex Vert
			#pragma fragment Frag

		    #pragma exclude_renderers gles gles3 glcore
			#pragma multi_compile_instancing
			#pragma editor_sync_compilation

			#pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
			#pragma multi_compile _ _DECAL_LAYERS

			#if ( UNITY_VERSION >= 60060000 )
			#pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
			#endif

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define HAVE_MESH_MODIFICATION
            #define SHADERPASS SHADERPASS_DBUFFER_PROJECTOR

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#if ( UNITY_VERSION >= 60010000 )
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
			#endif
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DecalInput.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderVariablesDecal.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#if _RENDER_PASS_ENABLED
			#define GBUFFER3 0
			#define GBUFFER4 1
			#if ( UNITY_VERSION >= 60000067 && UNITY_VERSION < 60010000 ) || ( UNITY_VERSION >= 60030004 )
			FRAMEBUFFER_INPUT_X_FLOAT(GBUFFER3);
			#else
			FRAMEBUFFER_INPUT_X_HALF(GBUFFER3);
			#endif
			#if ( UNITY_VERSION >= 60020000 )
			FRAMEBUFFER_INPUT_X_UINT(GBUFFER4);
			#else
			FRAMEBUFFER_INPUT_X_HALF(GBUFFER4);
			#endif
			#endif

			/*ase_pragma*/

			struct SurfaceDescription
			{
				float3 BaseColor;
				float Alpha;
				float3 NormalTS;
				float NormalAlpha;
				float Metallic;
				float Occlusion;
				float Smoothness;
				float MAOSAlpha;
			};

			struct Attributes
			{
				float3 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float4 tangentOS : TANGENT;
				/*ase_vdata:p=p;n=n;t=t*/
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				float4 positionCS : SV_POSITION;
				/*ase_interp(0,):sp=sp*/
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

            CBUFFER_START(UnityPerMaterial)
			float _DrawOrder;
			float _DecalMeshBiasType;
			float _DecalMeshDepthBias;
			float _DecalMeshViewBias;
			#if defined(DECAL_ANGLE_FADE)
			float _DecalAngleFadeSupported;
			#endif
			UNITY_TEXTURE_STREAMING_DEBUG_VARS;
			CBUFFER_END

            #ifdef SCENEPICKINGPASS
				float4 _SelectionID;
            #endif

            #ifdef SCENESELECTIONPASS
				int _ObjectId;
				int _PassValue;
            #endif

			/*ase_globals*/

			/*ase_funcs*/

            void GetSurfaceData(SurfaceDescription surfaceDescription, float angleFadeFactor, out DecalSurfaceData surfaceData)
            {
                half4x4 normalToWorld = UNITY_ACCESS_INSTANCED_PROP(Decal, _NormalToWorld);
                half fadeFactor = clamp(normalToWorld[0][3], 0.0f, 1.0f) * angleFadeFactor;
                float2 scale = float2(normalToWorld[3][0], normalToWorld[3][1]);
                float2 offset = float2(normalToWorld[3][2], normalToWorld[3][3]);

                ZERO_INITIALIZE(DecalSurfaceData, surfaceData);
                surfaceData.occlusion = half(1.0);
                surfaceData.smoothness = half(0);

                #ifdef _MATERIAL_AFFECTS_NORMAL
                    surfaceData.normalWS.w = half(1.0);
                #else
                    surfaceData.normalWS.w = half(0.0);
                #endif

                surfaceData.baseColor.xyz = half3(surfaceDescription.BaseColor);
                surfaceData.baseColor.w = half(surfaceDescription.Alpha * fadeFactor);

                #if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_GBUFFER_PROJECTOR)
                    #if defined(_MATERIAL_AFFECTS_NORMAL)
						surfaceData.normalWS.xyz = normalize(mul((half3x3)normalToWorld, surfaceDescription.NormalTS.xyz));
                    #else
						surfaceData.normalWS.xyz = normalize(normalToWorld[2].xyz);
                    #endif
                #elif (SHADERPASS == SHADERPASS_DBUFFER_MESH) || (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_MESH) || (SHADERPASS == SHADERPASS_DECAL_GBUFFER_MESH)
                    #if defined(_MATERIAL_AFFECTS_NORMAL)
                        float sgn = input.tangentWS.w;
                        float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
                        half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);
                        surfaceData.normalWS.xyz = normalize(TransformTangentToWorld(surfaceDescription.NormalTS, tangentToWorld));
                    #else
						surfaceData.normalWS.xyz = normalize(half3(input.normalWS));
                    #endif
                #endif

                surfaceData.normalWS.w = surfaceDescription.NormalAlpha * fadeFactor;

				#if defined( _MATERIAL_AFFECTS_MAOS )
					surfaceData.metallic = half(surfaceDescription.Metallic);
					surfaceData.occlusion = half(surfaceDescription.Occlusion);
					surfaceData.smoothness = half(surfaceDescription.Smoothness);
					surfaceData.MAOSAlpha = half(surfaceDescription.MAOSAlpha * fadeFactor);
				#endif
            }

            #if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_GBUFFER_PROJECTOR)
            #define DECAL_PROJECTOR
            #endif

            #if (SHADERPASS == SHADERPASS_DBUFFER_MESH) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_MESH) || (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_MESH) || (SHADERPASS == SHADERPASS_DECAL_GBUFFER_MESH)
            #define DECAL_MESH
            #endif

            #if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_DBUFFER_MESH)
            #define DECAL_DBUFFER
            #endif

            #if (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_MESH)
            #define DECAL_SCREEN_SPACE
            #endif

            #if (SHADERPASS == SHADERPASS_DECAL_GBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_GBUFFER_MESH)
            #define DECAL_GBUFFER
            #endif

            #if (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_PROJECTOR) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_MESH)
            #define DECAL_FORWARD_EMISSIVE
            #endif

            #if ((!defined(_MATERIAL_AFFECTS_NORMAL) && defined(_MATERIAL_AFFECTS_ALBEDO)) || (defined(_MATERIAL_AFFECTS_NORMAL) && defined(_MATERIAL_AFFECTS_NORMAL_BLEND))) && (defined(DECAL_SCREEN_SPACE) || defined(DECAL_GBUFFER))
            #define DECAL_RECONSTRUCT_NORMAL
            #elif defined(DECAL_ANGLE_FADE)
            #define DECAL_LOAD_NORMAL
            #endif

            #ifdef _DECAL_LAYERS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareRenderingLayerTexture.hlsl"
            #endif

            #if defined(DECAL_LOAD_NORMAL)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            #endif

            #if defined(DECAL_PROJECTOR) || defined(DECAL_RECONSTRUCT_NORMAL)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #endif

            #ifdef DECAL_MESH
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DecalMeshBiasTypeEnum.cs.hlsl"
            #endif

            #ifdef DECAL_RECONSTRUCT_NORMAL
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/NormalReconstruction.hlsl"
            #endif

			PackedVaryings Vert(Attributes inputMesh /*ase_vert_input*/ )
			{
				PackedVaryings packedOutput;
				ZERO_INITIALIZE(PackedVaryings, packedOutput);

				UNITY_SETUP_INSTANCE_ID(inputMesh);
				UNITY_TRANSFER_INSTANCE_ID(inputMesh, packedOutput);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(packedOutput);

				inputMesh.tangentOS = float4( 1, 0, 0, -1 );
				inputMesh.normalOS = float3( 0, 1, 0 );

				/*ase_vert_code:inputMesh=Attributes;packedOutput=PackedVaryings*/

				VertexPositionInputs vertexInput = GetVertexPositionInputs(inputMesh.positionOS.xyz);

				float3 positionWS = TransformObjectToWorld(inputMesh.positionOS);
				packedOutput.positionCS = TransformWorldToHClip(positionWS);

				return packedOutput;
			}

			void Frag(PackedVaryings packedInput,
				OUTPUT_DBUFFER(outDBuffer)
				/*ase_frag_input*/
			)
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
				UNITY_SETUP_INSTANCE_ID(packedInput);

				half angleFadeFactor = 1.0;

            // Only screen space needs flip logic, other passes do not setup needed properties so we skip here
            #if defined(DECAL_SCREEN_SPACE)
				TransformScreenUV(packedInput.positionCS, _ScreenSize.y);
            #endif

            #ifdef _DECAL_LAYERS
            #ifdef _RENDER_PASS_ENABLED
				#if ( UNITY_VERSION >= 60020000 )
				uint surfaceRenderingLayer = LOAD_FRAMEBUFFER_X_INPUT(GBUFFER4, packedInput.positionCS.xy).r;
				#else
				uint surfaceRenderingLayer = DecodeMeshRenderingLayer(LOAD_FRAMEBUFFER_X_INPUT(GBUFFER4, packedInput.positionCS.xy).r);
				#endif
            #else
				uint surfaceRenderingLayer = LoadSceneRenderingLayer(packedInput.positionCS.xy);
            #endif
				#if ( UNITY_VERSION >= 60030000 )
				uint projectorRenderingLayer = asuint(UNITY_ACCESS_INSTANCED_PROP(Decal, _DecalLayerMaskFromDecal));
				#else
				uint projectorRenderingLayer = uint(UNITY_ACCESS_INSTANCED_PROP(Decal, _DecalLayerMaskFromDecal));
				#endif
				clip((surfaceRenderingLayer & projectorRenderingLayer) - 0.1);
            #endif

			#if defined(DECAL_PROJECTOR)
			#if UNITY_REVERSED_Z
			#if _RENDER_PASS_ENABLED
				float depth = LOAD_FRAMEBUFFER_X_INPUT(GBUFFER3, packedInput.positionCS.xy).x;
			#else
				float depth = LoadSceneDepth(packedInput.positionCS.xy);
			#endif
			#else
			#if _RENDER_PASS_ENABLED
				float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, LOAD_FRAMEBUFFER_X_INPUT(GBUFFER3, packedInput.positionCS.xy));
			#else
			    // Adjust z to match NDC for OpenGL
				float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, LoadSceneDepth(packedInput.positionCS.xy));
			#endif
			#endif
			#endif

			#if defined(DECAL_RECONSTRUCT_NORMAL)
				#if defined(_RENDER_PASS_ENABLED) && ( ( UNITY_VERSION >= 60000067 && UNITY_VERSION < 60010000 ) || ( UNITY_VERSION >= 60030004 ) )
					half3 normalWS = half3(ReconstructNormalDerivative(packedInput.positionCS.xy, LOAD_FRAMEBUFFER_X_INPUT(GBUFFER3, packedInput.positionCS.xy).x));
				#elif defined(_DECAL_NORMAL_BLEND_HIGH)
					half3 normalWS = half3(ReconstructNormalTap9(packedInput.positionCS.xy));
				#elif defined(_DECAL_NORMAL_BLEND_MEDIUM)
					half3 normalWS = half3(ReconstructNormalTap5(packedInput.positionCS.xy));
				#else
					half3 normalWS = half3(ReconstructNormalDerivative(packedInput.positionCS.xy));
				#endif
			#elif defined(DECAL_LOAD_NORMAL)
				#if defined(_RENDER_PASS_ENABLED) && ( ( UNITY_VERSION >= 60000067 && UNITY_VERSION < 60010000 ) || ( UNITY_VERSION >= 60030004 ) )
					half3 normalWS = normalize(LOAD_FRAMEBUFFER_X_INPUT(GBUFFER2, packedInput.positionCS.xy).rgb);
				#else
					half3 normalWS = half3(LoadSceneNormals(packedInput.positionCS.xy));
				#endif
			#endif

				float2 positionSS = FoveatedRemapNonUniformToLinearCS(packedInput.positionCS.xy) * _ScreenSize.zw;

				float4 positionCS = ComputeClipSpacePosition( positionSS, depth );
				float4 hpositionVS = mul( UNITY_MATRIX_I_P, positionCS );

				/*ase_local_var:spn*/float4 ScreenPosNorm = float4( positionSS, positionCS.zw );
				/*ase_local_var:sp*/float4 ClipPos = positionCS * packedInput.positionCS.w;
				/*ase_local_var:spu*/float4 ScreenPos = ComputeScreenPos( ClipPos );
				/*ase_local_var:rwp*/float3 PositionRWS = mul( ( float3x3 )UNITY_MATRIX_I_V, hpositionVS.xyz / hpositionVS.w );
				/*ase_local_var:wp*/float3 PositionWS = PositionRWS + _WorldSpaceCameraPos;
				/*ase_local_var:op*/float3 PositionOS = TransformWorldToObject( PositionWS );
				/*ase_local_var:vp*/float3 PositionVS = TransformWorldToView( PositionWS );

				float3 positionDS = TransformWorldToObject(PositionWS);
				positionDS = positionDS * float3(1.0, -1.0, 1.0);

				float clipValue = 0.5 - Max3(abs(positionDS).x, abs(positionDS).y, abs(positionDS).z);
				clip(clipValue);

				float2 texCoord = positionDS.xz + float2(0.5, 0.5);

				float4x4 normalToWorld = UNITY_ACCESS_INSTANCED_PROP(Decal, _NormalToWorld);
				float2 scale = float2(normalToWorld[3][0], normalToWorld[3][1]);
				float2 offset = float2(normalToWorld[3][2], normalToWorld[3][3]);
				texCoord.xy = texCoord.xy * scale + offset;

				/*ase_local_var:uv0*/float2 texCoord0 = texCoord;
				/*ase_local_var:uv1*/float2 texCoord1 = texCoord;
				/*ase_local_var:uv2*/float2 texCoord2 = texCoord;
				/*ase_local_var:uv3*/float2 texCoord3 = texCoord;

				/*ase_local_var:wt*/float3 worldTangent = TransformObjectToWorldDir(float3(1, 0, 0));
				/*ase_local_var:wn*/float3 worldNormal = TransformObjectToWorldDir(float3(0, 1, 0));
				/*ase_local_var:wbt*/float3 worldBitangent = TransformObjectToWorldDir(float3(0, 0, 1));

			#ifdef DECAL_ANGLE_FADE
				half2 angleFade = half2(normalToWorld[1][3], normalToWorld[2][3]);
				if (angleFade.y < 0.0f)
				{
					half3 decalNormal = half3(normalToWorld[0].z, normalToWorld[1].z, normalToWorld[2].z);
					half dotAngle = dot(normalWS, decalNormal);
					angleFadeFactor = saturate(angleFade.x + angleFade.y * (dotAngle * (dotAngle - 2.0)));
				}
			#endif

				half3 viewDirectionWS = half3(1.0, 1.0, 1.0);
				DecalSurfaceData surfaceData;

				SurfaceDescription surfaceDescription = (SurfaceDescription)0;

				/*ase_frag_code:packedInput=PackedVaryings*/

				surfaceDescription.BaseColor = /*ase_frag_out:Base Color;Float3;0;-1;_BaseColor*/IsGammaSpace() ? float3(0.5, 0.5, 0.5) : SRGBToLinear(float3(0.5, 0.5, 0.5))/*end*/;
				surfaceDescription.Alpha = /*ase_frag_out:Alpha;Float;1;-1;_Alpha*/1/*end*/;
				surfaceDescription.NormalTS = /*ase_frag_out:Normal;Float3;2;-1;_NormalTS*/float3(0.0f, 0.0f, 1.0f)/*end*/;
				surfaceDescription.NormalAlpha = /*ase_frag_out:Normal Alpha;Float;3;-1;_NormalAlpha*/1/*end*/;

			#if defined( _MATERIAL_AFFECTS_MAOS )
				surfaceDescription.Metallic = /*ase_frag_out:Metallic;Float;4;-1;_Metallic*/0/*end*/;
				surfaceDescription.Occlusion = /*ase_frag_out:Occlusion;Float;5;-1;_Occlusion*/1/*end*/;
				surfaceDescription.Smoothness =/*ase_frag_out:Smoothness;Float;6;-1;_Smoothness*/ 0.5/*end*/;
				surfaceDescription.MAOSAlpha = /*ase_frag_out:MAOS Alpha;Float;7;-1;_MAOSAlpha*/1/*end*/;
			#endif

				GetSurfaceData(surfaceDescription, angleFadeFactor, surfaceData);
				ENCODE_INTO_DBUFFER(surfaceData, outDBuffer);

			}
            ENDHLSL
        }

		/*ase_pass*/
        Pass
        {
			/*ase_hide_pass*/
            /*ase_immutable*/ Name "DecalProjectorForwardEmissive"
            Tags
            {
                "LightMode" = "DecalProjectorForwardEmissive"
            }

			Cull Front
			Blend 0 SrcAlpha One
			ZTest Greater
			ZWrite Off

			HLSLPROGRAM

			#pragma vertex Vert
			#pragma fragment Frag
			#pragma multi_compile_instancing
			#pragma editor_sync_compilation

			#pragma multi_compile _ _DECAL_LAYERS

			#if ( UNITY_VERSION >= 60070000 )
			#pragma multi_compile _ _EXPOSURE
			#endif

			#if ( UNITY_VERSION >= 60060000 )
			#pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
			#endif

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define HAVE_MESH_MODIFICATION

            #define SHADERPASS SHADERPASS_FORWARD_EMISSIVE_PROJECTOR

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#if ( UNITY_VERSION >= 60010000 )
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
			#endif
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DecalInput.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderVariablesDecal.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"

		    #if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#if _RENDER_PASS_ENABLED
			#define GBUFFER3 0
			#define GBUFFER4 1
			#if ( UNITY_VERSION >= 60000067 && UNITY_VERSION < 60010000 ) || ( UNITY_VERSION >= 60030004 )
			FRAMEBUFFER_INPUT_X_FLOAT(GBUFFER3);
			#else
			FRAMEBUFFER_INPUT_X_HALF(GBUFFER3);
			#endif
			#if ( UNITY_VERSION >= 60020000 )
			FRAMEBUFFER_INPUT_X_UINT(GBUFFER4);
			#else
			FRAMEBUFFER_INPUT_X_HALF(GBUFFER4);
			#endif
			#endif

			/*ase_pragma*/

			struct SurfaceDescription
			{
				float Alpha;
				float3 Emission;
			};

			struct Attributes
			{
				float3 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float4 tangentOS : TANGENT;
				/*ase_vdata:p=p;n=n;t=t*/
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				float4 positionCS : SV_POSITION;
				/*ase_interp(0,):sp=sp*/
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

            CBUFFER_START(UnityPerMaterial)
			float _DrawOrder;
			float _DecalMeshBiasType;
			float _DecalMeshDepthBias;
			float _DecalMeshViewBias;
			#if defined(DECAL_ANGLE_FADE)
			float _DecalAngleFadeSupported;
			#endif
			UNITY_TEXTURE_STREAMING_DEBUG_VARS;
			CBUFFER_END

            #ifdef SCENEPICKINGPASS
				float4 _SelectionID;
            #endif

            #ifdef SCENESELECTIONPASS
				int _ObjectId;
				int _PassValue;
			#endif

			/*ase_globals*/

			/*ase_funcs*/

            void GetSurfaceData(SurfaceDescription surfaceDescription, float angleFadeFactor, out DecalSurfaceData surfaceData)
            {
                half4x4 normalToWorld = UNITY_ACCESS_INSTANCED_PROP(Decal, _NormalToWorld);
                half fadeFactor = clamp(normalToWorld[0][3], 0.0f, 1.0f) * angleFadeFactor;
                float2 scale = float2(normalToWorld[3][0], normalToWorld[3][1]);
                float2 offset = float2(normalToWorld[3][2], normalToWorld[3][3]);

                ZERO_INITIALIZE(DecalSurfaceData, surfaceData);
                surfaceData.occlusion = half(1.0);
                surfaceData.smoothness = half(0);

                #ifdef _MATERIAL_AFFECTS_NORMAL
                    surfaceData.normalWS.w = half(1.0);
                #else
                    surfaceData.normalWS.w = half(0.0);
                #endif

				#if defined( _MATERIAL_AFFECTS_EMISSION )
                	surfaceData.emissive.rgb = half3(surfaceDescription.Emission.rgb * fadeFactor);
				#endif

                surfaceData.baseColor.w = half(surfaceDescription.Alpha * fadeFactor);
            }

            #if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_GBUFFER_PROJECTOR)
            #define DECAL_PROJECTOR
            #endif

            #if (SHADERPASS == SHADERPASS_DBUFFER_MESH) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_MESH) || (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_MESH) || (SHADERPASS == SHADERPASS_DECAL_GBUFFER_MESH)
            #define DECAL_MESH
            #endif

            #if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_DBUFFER_MESH)
            #define DECAL_DBUFFER
            #endif

            #if (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_MESH)
            #define DECAL_SCREEN_SPACE
            #endif

            #if (SHADERPASS == SHADERPASS_DECAL_GBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_GBUFFER_MESH)
            #define DECAL_GBUFFER
            #endif

            #if (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_PROJECTOR) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_MESH)
            #define DECAL_FORWARD_EMISSIVE
            #endif

            #if ((!defined(_MATERIAL_AFFECTS_NORMAL) && defined(_MATERIAL_AFFECTS_ALBEDO)) || (defined(_MATERIAL_AFFECTS_NORMAL) && defined(_MATERIAL_AFFECTS_NORMAL_BLEND))) && (defined(DECAL_SCREEN_SPACE) || defined(DECAL_GBUFFER))
            #define DECAL_RECONSTRUCT_NORMAL
            #elif defined(DECAL_ANGLE_FADE)
            #define DECAL_LOAD_NORMAL
            #endif

            #ifdef _DECAL_LAYERS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareRenderingLayerTexture.hlsl"
            #endif

            #if defined(DECAL_LOAD_NORMAL)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            #endif

            #if defined(DECAL_PROJECTOR) || defined(DECAL_RECONSTRUCT_NORMAL)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #endif

            #ifdef DECAL_MESH
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DecalMeshBiasTypeEnum.cs.hlsl"
            #endif

            #ifdef DECAL_RECONSTRUCT_NORMAL
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/NormalReconstruction.hlsl"
            #endif

			PackedVaryings Vert(Attributes inputMesh /*ase_vert_input*/ )
			{
				PackedVaryings packedOutput;
				ZERO_INITIALIZE(PackedVaryings, packedOutput);

				UNITY_SETUP_INSTANCE_ID(inputMesh);
				UNITY_TRANSFER_INSTANCE_ID(inputMesh, packedOutput);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(packedOutput);

				inputMesh.tangentOS = float4( 1, 0, 0, -1 );
				inputMesh.normalOS = float3( 0, 1, 0 );

				/*ase_vert_code:inputMesh=Attributes;packedOutput=PackedVaryings*/

				VertexPositionInputs vertexInput = GetVertexPositionInputs(inputMesh.positionOS.xyz);
				float3 positionWS = TransformObjectToWorld(inputMesh.positionOS);
				packedOutput.positionCS = TransformWorldToHClip(positionWS);

				return packedOutput;
			}

			void Frag(PackedVaryings packedInput,
				out half4 outEmissive : SV_Target0
				/*ase_frag_input*/
			)
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
				UNITY_SETUP_INSTANCE_ID(packedInput);

				half angleFadeFactor = 1.0;

            // Only screen space needs flip logic, other passes do not setup needed properties so we skip here
            #if defined(DECAL_SCREEN_SPACE)
				TransformScreenUV(packedInput.positionCS, _ScreenSize.y);
            #endif

            #ifdef _DECAL_LAYERS
            #ifdef _RENDER_PASS_ENABLED
				#if ( UNITY_VERSION >= 60020000 )
				uint surfaceRenderingLayer = LOAD_FRAMEBUFFER_X_INPUT(GBUFFER4, packedInput.positionCS.xy).r;
				#else
				uint surfaceRenderingLayer = DecodeMeshRenderingLayer(LOAD_FRAMEBUFFER_X_INPUT(GBUFFER4, packedInput.positionCS.xy).r);
				#endif
            #else
				uint surfaceRenderingLayer = LoadSceneRenderingLayer(packedInput.positionCS.xy);
            #endif
				#if ( UNITY_VERSION >= 60030000 )
				uint projectorRenderingLayer = asuint(UNITY_ACCESS_INSTANCED_PROP(Decal, _DecalLayerMaskFromDecal));
				#else
				uint projectorRenderingLayer = uint(UNITY_ACCESS_INSTANCED_PROP(Decal, _DecalLayerMaskFromDecal));
				#endif
				clip((surfaceRenderingLayer & projectorRenderingLayer) - 0.1);
            #endif

			#if defined(DECAL_PROJECTOR)
			#if UNITY_REVERSED_Z
			#if _RENDER_PASS_ENABLED
				float depth = LOAD_FRAMEBUFFER_X_INPUT(GBUFFER3, packedInput.positionCS.xy).x;
			#else
				float depth = LoadSceneDepth(packedInput.positionCS.xy);
			#endif
			#else
			#if _RENDER_PASS_ENABLED
				float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, LOAD_FRAMEBUFFER_X_INPUT(GBUFFER3, packedInput.positionCS.xy));
			#else
			    // Adjust z to match NDC for OpenGL
				float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, LoadSceneDepth(packedInput.positionCS.xy));
			#endif
			#endif
			#endif

			#if defined(DECAL_RECONSTRUCT_NORMAL)
				#if defined(_RENDER_PASS_ENABLED) && ( ( UNITY_VERSION >= 60000067 && UNITY_VERSION < 60010000 ) || ( UNITY_VERSION >= 60030004 ) )
					half3 normalWS = half3(ReconstructNormalDerivative(packedInput.positionCS.xy, LOAD_FRAMEBUFFER_X_INPUT(GBUFFER3, packedInput.positionCS.xy).x));
				#elif defined(_DECAL_NORMAL_BLEND_HIGH)
					half3 normalWS = half3(ReconstructNormalTap9(packedInput.positionCS.xy));
				#elif defined(_DECAL_NORMAL_BLEND_MEDIUM)
					half3 normalWS = half3(ReconstructNormalTap5(packedInput.positionCS.xy));
				#else
					half3 normalWS = half3(ReconstructNormalDerivative(packedInput.positionCS.xy));
				#endif
			#elif defined(DECAL_LOAD_NORMAL)
				#if defined(_RENDER_PASS_ENABLED) && ( ( UNITY_VERSION >= 60000067 && UNITY_VERSION < 60010000 ) || ( UNITY_VERSION >= 60030004 ) )
					half3 normalWS = normalize(LOAD_FRAMEBUFFER_X_INPUT(GBUFFER2, packedInput.positionCS.xy).rgb);
				#else
					half3 normalWS = half3(LoadSceneNormals(packedInput.positionCS.xy));
				#endif
			#endif

				float2 positionSS = FoveatedRemapNonUniformToLinearCS(packedInput.positionCS.xy) * _ScreenSize.zw;

				float4 positionCS = ComputeClipSpacePosition( positionSS, depth );
				float4 hpositionVS = mul( UNITY_MATRIX_I_P, positionCS );

				/*ase_local_var:spn*/float4 ScreenPosNorm = float4( positionSS, positionCS.zw );
				/*ase_local_var:sp*/float4 ClipPos = positionCS * packedInput.positionCS.w;
				/*ase_local_var:spu*/float4 ScreenPos = ComputeScreenPos( ClipPos );
				/*ase_local_var:rwp*/float3 PositionRWS = mul( ( float3x3 )UNITY_MATRIX_I_V, hpositionVS.xyz / hpositionVS.w );
				/*ase_local_var:wp*/float3 PositionWS = PositionRWS + _WorldSpaceCameraPos;
				/*ase_local_var:op*/float3 PositionOS = TransformWorldToObject( PositionWS );
				/*ase_local_var:vp*/float3 PositionVS = TransformWorldToView( PositionWS );

				float3 positionDS = TransformWorldToObject(PositionWS);
				positionDS = positionDS * float3(1.0, -1.0, 1.0);

				float clipValue = 0.5 - Max3(abs(positionDS).x, abs(positionDS).y, abs(positionDS).z);
				clip(clipValue);

				float2 texCoord = positionDS.xz + float2(0.5, 0.5);

				float4x4 normalToWorld = UNITY_ACCESS_INSTANCED_PROP(Decal, _NormalToWorld);
				float2 scale = float2(normalToWorld[3][0], normalToWorld[3][1]);
				float2 offset = float2(normalToWorld[3][2], normalToWorld[3][3]);
				texCoord.xy = texCoord.xy * scale + offset;

			#ifdef DECAL_ANGLE_FADE
				half2 angleFade = half2(normalToWorld[1][3], normalToWorld[2][3]);
				if (angleFade.y < 0.0f)
				{
					half3 decalNormal = half3(normalToWorld[0].z, normalToWorld[1].z, normalToWorld[2].z);
					half dotAngle = dot(normalWS, decalNormal);
					angleFadeFactor = saturate(angleFade.x + angleFade.y * (dotAngle * (dotAngle - 2.0)));
				}
			#endif

				half3 viewDirectionWS = half3(1.0, 1.0, 1.0);
				DecalSurfaceData surfaceData;

				SurfaceDescription surfaceDescription = (SurfaceDescription)0;

				/*ase_local_var:uv0*/float2 texCoord0 = texCoord;
				/*ase_local_var:uv1*/float2 texCoord1 = texCoord;
				/*ase_local_var:uv2*/float2 texCoord2 = texCoord;
				/*ase_local_var:uv3*/float2 texCoord3 = texCoord;

				/*ase_local_var:wt*/float3 worldTangent = TransformObjectToWorldDir(float3(1, 0, 0));
				/*ase_local_var:wn*/float3 worldNormal = TransformObjectToWorldDir(float3(0, 1, 0));
				/*ase_local_var:wbt*/float3 worldBitangent = TransformObjectToWorldDir(float3(0, 0, 1));

				/*ase_frag_code:packedInput=PackedVaryings*/

				surfaceDescription.Alpha = /*ase_frag_out:Alpha;Float;1;-1;_Alpha*/1/*end*/;

			#if defined( _MATERIAL_AFFECTS_EMISSION )
				surfaceDescription.Emission = /*ase_frag_out:Emission;Float3;8;-1;_Emission*/float3(0, 0, 0)/*end*/;
			#endif

				GetSurfaceData( surfaceDescription, angleFadeFactor, surfaceData);

				#if ( UNITY_VERSION >= 60070000 )
				outEmissive.rgb = ClampExposed(GetPreExposureMultiplier() * surfaceData.emissive);
				#else
				outEmissive.rgb = surfaceData.emissive * GetCurrentExposureMultiplier();
				#endif
				outEmissive.a = surfaceData.baseColor.a;

			}
            ENDHLSL
        }

		/*ase_pass*/
        Pass
        {
			/*ase_main_pass*/
            /*ase_immutable*/ Name "DecalScreenSpaceProjector"
            Tags
            {
                "LightMode" = "DecalScreenSpaceProjector"
            }

			Cull Front
			Blend SrcAlpha OneMinusSrcAlpha
			ZTest Greater
			ZWrite Off

			HLSLPROGRAM

			#pragma vertex Vert
			#pragma fragment Frag
			#pragma multi_compile_instancing
			#if ( UNITY_VERSION < 60010000 )
			#pragma multi_compile_fog
			#endif
			#pragma editor_sync_compilation

			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
			#if ( UNITY_VERSION >= 60030000 )
			#pragma multi_compile_fragment _ _SCREEN_SPACE_IRRADIANCE
			#endif

			#if ( UNITY_VERSION >= 60070000 )
			#pragma multi_compile _ _EXPOSURE
			#endif

			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS

			#if ( UNITY_VERSION >= 60070000 )
			#pragma multi_compile _ _LIGHT_FALLOFF_QUADRATIC
			#endif

            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

			#if ( UNITY_VERSION >= 60070000 )
			#pragma multi_compile_fragment _ _CLUSTER_LIGHT_LOOP
			#elif ( UNITY_VERSION >= 60010000 )
			#pragma multi_compile _ _CLUSTER_LIGHT_LOOP
			#else
			#pragma multi_compile _ _FORWARD_PLUS
			#endif

			#pragma multi_compile_fragment _ _LIGHT_COOKIES

			#if ( UNITY_VERSION >= 60070000 )
			#pragma multi_compile_fragment _ _VOLUMETRIC_FOG
			#endif

			#pragma multi_compile_fragment _ DEBUG_DISPLAY
			#pragma multi_compile _DECAL_NORMAL_BLEND_LOW _DECAL_NORMAL_BLEND_MEDIUM _DECAL_NORMAL_BLEND_HIGH
			#pragma multi_compile _ _DECAL_LAYERS

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define ATTRIBUTES_NEED_NORMAL
			#define ATTRIBUTES_NEED_TEXCOORD0
            #define VARYINGS_NEED_NORMAL_WS
			#define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_VIEWDIRECTION_WS
            #define VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
            #define VARYINGS_NEED_SH
            #define VARYINGS_NEED_STATIC_LIGHTMAP_UV
            #define VARYINGS_NEED_DYNAMIC_LIGHTMAP_UV

            #define HAVE_MESH_MODIFICATION

            #define SHADERPASS SHADERPASS_DECAL_SCREEN_SPACE_PROJECTOR

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#if ( UNITY_VERSION >= 60010000 )
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
			#endif
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DecalInput.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderVariablesDecal.hlsl"			

		    #if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#if _RENDER_PASS_ENABLED
			#define GBUFFER3 0
			#define GBUFFER4 1
			#if ( UNITY_VERSION >= 60000067 && UNITY_VERSION < 60010000 ) || ( UNITY_VERSION >= 60030004 )
			FRAMEBUFFER_INPUT_X_FLOAT(GBUFFER3);
			#else
			FRAMEBUFFER_INPUT_X_HALF(GBUFFER3);
			#endif
			#if ( UNITY_VERSION >= 60020000 )
			FRAMEBUFFER_INPUT_X_UINT(GBUFFER4);
			#else
			FRAMEBUFFER_INPUT_X_HALF(GBUFFER4);
			#endif
			#endif

			/*ase_pragma*/

			struct SurfaceDescription
			{
				float3 BaseColor;
				float Alpha;
				float3 NormalTS;
				float NormalAlpha;
				float Metallic;
				float Occlusion;
				float Smoothness;
				float MAOSAlpha;
				float3 Emission;
			};

			struct Attributes
			{
				float3 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float4 tangentOS : TANGENT;
				/*ase_vdata:p=p;n=n;t=t*/
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				float4 positionCS : SV_POSITION;
				float3 normalWS : TEXCOORD0;
				float3 viewDirectionWS : TEXCOORD1;
				float4 lightmapUVs : TEXCOORD2; // @diogo: packs both static (xy) and dynamic (zw)
				float3 sh : TEXCOORD3;
				float4 fogFactorAndVertexLight : TEXCOORD4;
				#ifdef USE_APV_PROBE_OCCLUSION
					float4 probeOcclusion : TEXCOORD5;
				#endif
				/*ase_interp(6,):sp=sp;wn=tc0;wvd=tc1*/
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

            CBUFFER_START(UnityPerMaterial)
			float _DrawOrder;
			float _DecalMeshBiasType;
			float _DecalMeshDepthBias;
			float _DecalMeshViewBias;
			#if defined(DECAL_ANGLE_FADE)
			float _DecalAngleFadeSupported;
			#endif
			UNITY_TEXTURE_STREAMING_DEBUG_VARS;
			CBUFFER_END

            #ifdef SCENEPICKINGPASS
				float4 _SelectionID;
            #endif

            #ifdef SCENESELECTIONPASS
				int _ObjectId;
				int _PassValue;
            #endif

			/*ase_globals*/

			/*ase_funcs*/

            void GetSurfaceData(SurfaceDescription surfaceDescription, float angleFadeFactor, out DecalSurfaceData surfaceData)
            {
                half4x4 normalToWorld = UNITY_ACCESS_INSTANCED_PROP(Decal, _NormalToWorld);
                half fadeFactor = clamp(normalToWorld[0][3], 0.0f, 1.0f) * angleFadeFactor;
                float2 scale = float2(normalToWorld[3][0], normalToWorld[3][1]);
                float2 offset = float2(normalToWorld[3][2], normalToWorld[3][3]);

                ZERO_INITIALIZE(DecalSurfaceData, surfaceData);
                surfaceData.occlusion = half(1.0);
                surfaceData.smoothness = half(0);

                #ifdef _MATERIAL_AFFECTS_NORMAL
                    surfaceData.normalWS.w = half(1.0);
                #else
                    surfaceData.normalWS.w = half(0.0);
                #endif

				#if defined( _MATERIAL_AFFECTS_EMISSION )
                	surfaceData.emissive.rgb = half3(surfaceDescription.Emission.rgb * fadeFactor);
				#endif

                surfaceData.baseColor.xyz = half3(surfaceDescription.BaseColor);
                surfaceData.baseColor.w = half(surfaceDescription.Alpha * fadeFactor);

                #if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_GBUFFER_PROJECTOR)
                    #if defined(_MATERIAL_AFFECTS_NORMAL)
						surfaceData.normalWS.xyz = normalize(mul((half3x3)normalToWorld, surfaceDescription.NormalTS.xyz));
                    #else
						surfaceData.normalWS.xyz = normalize(normalToWorld[2].xyz);
                    #endif
                #elif (SHADERPASS == SHADERPASS_DBUFFER_MESH) || (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_MESH) || (SHADERPASS == SHADERPASS_DECAL_GBUFFER_MESH)
                    #if defined(_MATERIAL_AFFECTS_NORMAL)
                        float sgn = input.tangentWS.w;
                        float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
                        half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);
                        surfaceData.normalWS.xyz = normalize(TransformTangentToWorld(surfaceDescription.NormalTS, tangentToWorld));
                    #else
						surfaceData.normalWS.xyz = normalize(half3(input.normalWS));
                    #endif
                #endif

                surfaceData.normalWS.w = surfaceDescription.NormalAlpha * fadeFactor;

				#if defined( _MATERIAL_AFFECTS_MAOS )
					surfaceData.metallic = half(surfaceDescription.Metallic);
					surfaceData.occlusion = half(surfaceDescription.Occlusion);
					surfaceData.smoothness = half(surfaceDescription.Smoothness);
					surfaceData.MAOSAlpha = half(surfaceDescription.MAOSAlpha * fadeFactor);
				#endif
            }

            #if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_GBUFFER_PROJECTOR)
            #define DECAL_PROJECTOR
            #endif

            #if (SHADERPASS == SHADERPASS_DBUFFER_MESH) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_MESH) || (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_MESH) || (SHADERPASS == SHADERPASS_DECAL_GBUFFER_MESH)
            #define DECAL_MESH
            #endif

            #if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_DBUFFER_MESH)
            #define DECAL_DBUFFER
            #endif

            #if (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_MESH)
            #define DECAL_SCREEN_SPACE
            #endif

            #if (SHADERPASS == SHADERPASS_DECAL_GBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_GBUFFER_MESH)
            #define DECAL_GBUFFER
            #endif

            #if (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_PROJECTOR) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_MESH)
            #define DECAL_FORWARD_EMISSIVE
            #endif

            #if ((!defined(_MATERIAL_AFFECTS_NORMAL) && defined(_MATERIAL_AFFECTS_ALBEDO)) || (defined(_MATERIAL_AFFECTS_NORMAL) && defined(_MATERIAL_AFFECTS_NORMAL_BLEND))) && (defined(DECAL_SCREEN_SPACE) || defined(DECAL_GBUFFER))
            #define DECAL_RECONSTRUCT_NORMAL
            #elif defined(DECAL_ANGLE_FADE)
            #define DECAL_LOAD_NORMAL
            #endif

            #ifdef _DECAL_LAYERS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareRenderingLayerTexture.hlsl"
            #endif

            #if defined(DECAL_LOAD_NORMAL)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            #endif

            #if defined(DECAL_PROJECTOR) || defined(DECAL_RECONSTRUCT_NORMAL)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #endif

            #ifdef DECAL_MESH
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DecalMeshBiasTypeEnum.cs.hlsl"
            #endif

            #ifdef DECAL_RECONSTRUCT_NORMAL
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/NormalReconstruction.hlsl"
            #endif

			void InitializeInputData(PackedVaryings input, float3 positionWS, half3 normalWS, half3 viewDirectionWS, out InputData inputData)
			{
				inputData = (InputData)0;

				inputData.positionWS = positionWS;
				inputData.normalWS = normalWS;
				inputData.viewDirectionWS = viewDirectionWS;
				#if ( UNITY_VERSION >= 60070000 )
					inputData.shadowCoord = MainLightShadowsAvailable() ? TransformWorldToShadowCoord(positionWS) : float4(0, 0, 0, 0);
				#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
					inputData.shadowCoord = TransformWorldToShadowCoord(positionWS);
				#else
					inputData.shadowCoord = float4(0, 0, 0, 0);
				#endif
				#if ( UNITY_VERSION >= 60070000 )
					inputData.preExposureMultiplier = GetPreExposureMultiplier();
				#endif

				#ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
					inputData.fogCoord = InitializeInputDataFog(float4(positionWS, 1.0), input.fogFactorAndVertexLight.x);
					inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
				#endif

				#if ( UNITY_VERSION >= 60070000 )
					GIParams giParams = (GIParams)0;
					#if defined(LIGHTMAP_ON)
						giParams.staticLightmapUV = input.lightmapUVs.xy;
					#else
						giParams.vertexSH = half3(input.sh);
					#endif
					#if defined(DYNAMICLIGHTMAP_ON)
						giParams.dynamicLightmapUV = input.lightmapUVs.zw;
					#endif
					#if defined(USE_APV_PROBE_OCCLUSION)
						giParams.vertexProbeOcclusion = input.probeOcclusion;
					#endif
					giParams.positionWS = inputData.positionWS;
					giParams.normalWS = inputData.normalWS;
					giParams.viewDirWS = inputData.viewDirectionWS;
					giParams.positionSS = input.positionCS.xy;
					InitializeBakedGI( giParams, inputData.bakedGI, inputData.shadowMask );
				#elif defined(_SCREEN_SPACE_IRRADIANCE) && ( UNITY_VERSION >= 60030000 )
					#if ( UNITY_VERSION >= 60060000 )
						inputData.bakedGI = SAMPLE_GI(_ScreenSpaceIrradiance, input.positionCS.xy, inputData.normalWS);
					#else
						inputData.bakedGI = SAMPLE_GI(_ScreenSpaceIrradiance, input.positionCS.xy);
					#endif
				#elif defined(VARYINGS_NEED_DYNAMIC_LIGHTMAP_UV) && defined(DYNAMICLIGHTMAP_ON)
					inputData.bakedGI = SAMPLE_GI(input.lightmapUVs.xy, input.lightmapUVs.zw, half3(input.sh), normalWS);
					#if defined(VARYINGS_NEED_STATIC_LIGHTMAP_UV)
					inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUVs.xy);
					#endif
				#elif defined(VARYINGS_NEED_STATIC_LIGHTMAP_UV)
				#if !defined(LIGHTMAP_ON) && (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
    				inputData.bakedGI = SAMPLE_GI(input.sh,
					GetAbsolutePositionWS(inputData.positionWS),
					inputData.normalWS,
					inputData.viewDirectionWS,
					input.positionCS.xy,
					input.probeOcclusion,
					inputData.shadowMask);
				#else
					inputData.bakedGI = SAMPLE_GI(input.lightmapUVs.xy, half3(input.sh), normalWS);
					#if defined(VARYINGS_NEED_STATIC_LIGHTMAP_UV)
					inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUVs.xy);
					#endif
				#endif
				#endif

				#if defined(DEBUG_DISPLAY)
					#if defined(VARYINGS_NEED_DYNAMIC_LIGHTMAP_UV) && defined(DYNAMICLIGHTMAP_ON)
						inputData.dynamicLightmapUV = input.lightmapUVs.zw;
					#endif
					#if defined(VARYINGS_NEED_STATIC_LIGHTMAP_UV) && defined(LIGHTMAP_ON)
						inputData.staticLightmapUV = input.lightmapUVs.xy;
					#elif defined(VARYINGS_NEED_SH)
						inputData.vertexSH = input.sh;
					#endif
					#if defined(USE_APV_PROBE_OCCLUSION)
						inputData.probeOcclusion = input.probeOcclusion;
					#endif
				#endif

				inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
			}

			void GetSurface(DecalSurfaceData decalSurfaceData, inout SurfaceData surfaceData)
			{
				surfaceData.albedo = decalSurfaceData.baseColor.rgb;
				surfaceData.metallic = saturate(decalSurfaceData.metallic);
				surfaceData.specular = 0;
				surfaceData.smoothness = saturate(decalSurfaceData.smoothness);
				surfaceData.occlusion = decalSurfaceData.occlusion;
				surfaceData.emission = decalSurfaceData.emissive;
				surfaceData.alpha = saturate(decalSurfaceData.baseColor.w);
				surfaceData.clearCoatMask = 0;
				surfaceData.clearCoatSmoothness = 1;
			}

			PackedVaryings Vert(Attributes inputMesh /*ase_vert_input*/ )
			{
				PackedVaryings packedOutput;
				ZERO_INITIALIZE(PackedVaryings, packedOutput);

				UNITY_SETUP_INSTANCE_ID(inputMesh);
				UNITY_TRANSFER_INSTANCE_ID(inputMesh, packedOutput);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(packedOutput);

				inputMesh.tangentOS = float4( 1, 0, 0, -1 );
				inputMesh.normalOS = float3( 0, 1, 0 );

				/*ase_vert_code:inputMesh=Attributes;packedOutput=PackedVaryings*/

				VertexPositionInputs vertexInput = GetVertexPositionInputs(inputMesh.positionOS.xyz);
				float3 positionWS = TransformObjectToWorld(inputMesh.positionOS);

				float3 normalWS = TransformObjectToWorldNormal(inputMesh.normalOS);

				packedOutput.positionCS = TransformWorldToHClip(positionWS);

				half fogFactor = 0;
				#if !defined(_FOG_FRAGMENT)
					fogFactor = ComputeFogFactor(packedOutput.positionCS.z);
				#endif
				half3 vertexLight = VertexLighting(positionWS, normalWS);
				packedOutput.fogFactorAndVertexLight = half4(fogFactor, vertexLight);

				packedOutput.normalWS.xyz =  normalWS;
				packedOutput.viewDirectionWS.xyz =  GetWorldSpaceViewDir(positionWS);

				#if defined(LIGHTMAP_ON)
					OUTPUT_LIGHTMAP_UV(inputMesh.uv1, unity_LightmapST, packedOutput.lightmapUVs.xy);
				#endif

				#if defined(DYNAMICLIGHTMAP_ON)
					packedOutput.lightmapUVs.zw = inputMesh.uv2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
				#endif

				#if !defined(LIGHTMAP_ON)
					packedOutput.sh.xyz =  float3(SampleSHVertex(half3(normalWS)));
				#endif

				return packedOutput;
			}

			void Frag(PackedVaryings packedInput,
				out half4 outColor : SV_Target0
				/*ase_frag_input*/
			)
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
				UNITY_SETUP_INSTANCE_ID(packedInput);

				half angleFadeFactor = 1.0;

            // Only screen space needs flip logic, other passes do not setup needed properties so we skip here
			// to-do check DecalScreenSpaceProjector pass
            //#if defined(DECAL_SCREEN_SPACE)
				//TransformScreenUV(packedInput.positionCS, _ScreenSize.y);
            //#endif

            #ifdef _DECAL_LAYERS
            #ifdef _RENDER_PASS_ENABLED
				#if ( UNITY_VERSION >= 60020000 )
				uint surfaceRenderingLayer = LOAD_FRAMEBUFFER_X_INPUT(GBUFFER4, packedInput.positionCS.xy).r;
				#else
				uint surfaceRenderingLayer = DecodeMeshRenderingLayer(LOAD_FRAMEBUFFER_X_INPUT(GBUFFER4, packedInput.positionCS.xy).r);
				#endif
            #else
				uint surfaceRenderingLayer = LoadSceneRenderingLayer(packedInput.positionCS.xy);
            #endif
				#if ( UNITY_VERSION >= 60030000 )
				uint projectorRenderingLayer = asuint(UNITY_ACCESS_INSTANCED_PROP(Decal, _DecalLayerMaskFromDecal));
				#else
				uint projectorRenderingLayer = uint(UNITY_ACCESS_INSTANCED_PROP(Decal, _DecalLayerMaskFromDecal));
				#endif
				clip((surfaceRenderingLayer & projectorRenderingLayer) - 0.1);
            #endif

			#if defined(DECAL_PROJECTOR)
			#if UNITY_REVERSED_Z
			#if _RENDER_PASS_ENABLED
				float depth = LOAD_FRAMEBUFFER_X_INPUT(GBUFFER3, packedInput.positionCS.xy).x;
			#else
				float depth = LoadSceneDepth(packedInput.positionCS.xy);
			#endif
			#else
			#if _RENDER_PASS_ENABLED
				float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, LOAD_FRAMEBUFFER_X_INPUT(GBUFFER3, packedInput.positionCS.xy));
			#else
			    // Adjust z to match NDC for OpenGL
				float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, LoadSceneDepth(packedInput.positionCS.xy));
			#endif
			#endif
			#endif

			#if defined(DECAL_RECONSTRUCT_NORMAL)
				#if defined(_RENDER_PASS_ENABLED) && ( ( UNITY_VERSION >= 60000067 && UNITY_VERSION < 60010000 ) || ( UNITY_VERSION >= 60030004 ) )
					half3 normalWS = half3(ReconstructNormalDerivative(packedInput.positionCS.xy, LOAD_FRAMEBUFFER_X_INPUT(GBUFFER3, packedInput.positionCS.xy).x));
				#elif defined(_DECAL_NORMAL_BLEND_HIGH)
					half3 normalWS = half3(ReconstructNormalTap9(packedInput.positionCS.xy));
				#elif defined(_DECAL_NORMAL_BLEND_MEDIUM)
					half3 normalWS = half3(ReconstructNormalTap5(packedInput.positionCS.xy));
				#else
					half3 normalWS = half3(ReconstructNormalDerivative(packedInput.positionCS.xy));
				#endif
			#elif defined(DECAL_LOAD_NORMAL)
				#if defined(_RENDER_PASS_ENABLED) && ( ( UNITY_VERSION >= 60000067 && UNITY_VERSION < 60010000 ) || ( UNITY_VERSION >= 60030004 ) )
					half3 normalWS = normalize(LOAD_FRAMEBUFFER_X_INPUT(GBUFFER2, packedInput.positionCS.xy).rgb);
				#else
					half3 normalWS = half3(LoadSceneNormals(packedInput.positionCS.xy));
				#endif
			#endif

				float2 positionSS = FoveatedRemapNonUniformToLinearCS(packedInput.positionCS.xy) * _ScreenSize.zw;

				float4 positionCS = ComputeClipSpacePosition( positionSS, depth );
				float4 hpositionVS = mul( UNITY_MATRIX_I_P, positionCS );

				/*ase_local_var:spn*/float4 ScreenPosNorm = float4( positionSS, positionCS.zw );
				/*ase_local_var:sp*/float4 ClipPos = positionCS * packedInput.positionCS.w;
				/*ase_local_var:spu*/float4 ScreenPos = ComputeScreenPos( ClipPos );
				/*ase_local_var:rwp*/float3 PositionRWS = mul( ( float3x3 )UNITY_MATRIX_I_V, hpositionVS.xyz / hpositionVS.w );
				/*ase_local_var:wp*/float3 PositionWS = PositionRWS + _WorldSpaceCameraPos;
				/*ase_local_var:op*/float3 PositionOS = TransformWorldToObject( PositionWS );
				/*ase_local_var:vp*/float3 PositionVS = TransformWorldToView( PositionWS );

				float3 positionDS = TransformWorldToObject(PositionWS);
				positionDS = positionDS * float3(1.0, -1.0, 1.0);

				float clipValue = 0.5 - Max3(abs(positionDS).x, abs(positionDS).y, abs(positionDS).z);
				clip(clipValue);

				float2 texCoord = positionDS.xz + float2(0.5, 0.5);

				float4x4 normalToWorld = UNITY_ACCESS_INSTANCED_PROP(Decal, _NormalToWorld);
				float2 scale = float2(normalToWorld[3][0], normalToWorld[3][1]);
				float2 offset = float2(normalToWorld[3][2], normalToWorld[3][3]);
				texCoord.xy = texCoord.xy * scale + offset;

				/*ase_local_var:uv0*/float2 texCoord0 = texCoord;
				/*ase_local_var:uv1*/float2 texCoord1 = texCoord;
				/*ase_local_var:uv2*/float2 texCoord2 = texCoord;
				/*ase_local_var:uv3*/float2 texCoord3 = texCoord;

				/*ase_local_var:wt*/float3 worldTangent = TransformObjectToWorldDir(float3(1, 0, 0));
				/*ase_local_var:wn*/float3 worldNormal = TransformObjectToWorldDir(float3(0, 1, 0));
				/*ase_local_var:wbt*/float3 worldBitangent = TransformObjectToWorldDir(float3(0, 0, 1));

			#ifdef DECAL_ANGLE_FADE
				half2 angleFade = half2(normalToWorld[1][3], normalToWorld[2][3]);
				if (angleFade.y < 0.0f)
				{
					half3 decalNormal = half3(normalToWorld[0].z, normalToWorld[1].z, normalToWorld[2].z);
					half dotAngle = dot(normalWS, decalNormal);
					angleFadeFactor = saturate(angleFade.x + angleFade.y * (dotAngle * (dotAngle - 2.0)));
				}
			#endif

				half3 viewDirectionWS = half3(packedInput.viewDirectionWS);

				DecalSurfaceData surfaceData;

				/*ase_frag_code:packedInput=PackedVaryings*/

				SurfaceDescription surfaceDescription = (SurfaceDescription)0;

				surfaceDescription.BaseColor = /*ase_frag_out:Base Color;Float3;0;-1;_BaseColor*/IsGammaSpace() ? float3(0.5, 0.5, 0.5) : SRGBToLinear(float3(0.5, 0.5, 0.5))/*end*/;
				surfaceDescription.Alpha = /*ase_frag_out:Alpha;Float;1;-1;_Alpha*/1/*end*/;
				surfaceDescription.NormalTS = /*ase_frag_out:Normal;Float3;2;-1;_NormalTS*/float3(0.0f, 0.0f, 1.0f)/*end*/;
				surfaceDescription.NormalAlpha = /*ase_frag_out:Normal Alpha;Float;3;-1;_NormalAlpha*/1/*end*/;
			#if defined( _MATERIAL_AFFECTS_MAOS )
				surfaceDescription.Metallic = /*ase_frag_out:Metallic;Float;4;-1;_Metallic*/0/*end*/;
				surfaceDescription.Occlusion = /*ase_frag_out:Occlusion;Float;5;-1;_Occlusion*/1/*end*/;
				surfaceDescription.Smoothness = /*ase_frag_out:Smoothness;Float;6;-1;_Smoothness*/0.5/*end*/;
				surfaceDescription.MAOSAlpha = /*ase_frag_out:MAOS Alpha;Float;7;-1;_MAOSAlpha*/1/*end*/;
			#endif

			#if defined( _MATERIAL_AFFECTS_EMISSION )
				surfaceDescription.Emission = /*ase_frag_out:Emission;Float3;8;-1;_Emission*/float3(0, 0, 0)/*end*/;
			#endif

				GetSurfaceData( surfaceDescription, angleFadeFactor, surfaceData);

				half3 normalToPack = surfaceData.normalWS.xyz;
			#ifdef DECAL_RECONSTRUCT_NORMAL
				surfaceData.normalWS.xyz = normalize(lerp(normalWS.xyz, surfaceData.normalWS.xyz, surfaceData.normalWS.w));
			#endif

				InputData inputData;
				InitializeInputData( packedInput, PositionWS, surfaceData.normalWS.xyz, viewDirectionWS, inputData);

				SurfaceData surface = (SurfaceData)0;
				GetSurface(surfaceData, surface);

				ASE_LIGHT_ACCUM4 color = UniversalFragmentPBR(inputData, surface);
				color.rgb = MixFog(color.rgb, inputData.fogCoord);
				#if ( UNITY_VERSION >= 60070000 )
				color.rgb = ClampExposed(inputData.preExposureMultiplier * color.rgb);
				#endif
				outColor = color;

			}
			ENDHLSL
        }

		/*ase_pass*/
        Pass
        {
            /*ase_hide_pass*/
			/*ase_immutable*/ Name "DecalGBufferProjector"
            Tags
            {
                "LightMode" = "DecalGBufferProjector"
            }

			Cull Front
			Blend 0 SrcAlpha OneMinusSrcAlpha
			Blend 1 SrcAlpha OneMinusSrcAlpha
			Blend 2 SrcAlpha OneMinusSrcAlpha
			Blend 3 SrcAlpha OneMinusSrcAlpha
			ZTest Greater
			ZWrite Off
			ColorMask RGB
			ColorMask 0 1
			ColorMask RGB 2
			ColorMask RGB 3

			HLSLPROGRAM

			#pragma vertex Vert
			#pragma fragment Frag
			#pragma multi_compile_instancing
			#if ( UNITY_VERSION < 60010000 )
			#pragma multi_compile_fog
			#endif
			#pragma editor_sync_compilation

			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
			#pragma multi_compile _DECAL_NORMAL_BLEND_LOW _DECAL_NORMAL_BLEND_MEDIUM _DECAL_NORMAL_BLEND_HIGH
			#pragma multi_compile _ _DECAL_LAYERS
			#pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
			#pragma multi_compile_fragment _ _RENDER_PASS_ENABLED

			#if ( UNITY_VERSION >= 60070000 )
			#pragma multi_compile _ _EXPOSURE
			#endif

			//fix for SRP Additional Lighting in Deferred+
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS

			#if ( UNITY_VERSION >= 60070000 )
			#pragma multi_compile_fragment _ _CLUSTER_LIGHT_LOOP
			#elif ( UNITY_VERSION >= 60010000 )
			#pragma multi_compile _ _CLUSTER_LIGHT_LOOP
			#else
			#pragma multi_compile _ _FORWARD_PLUS
			#endif

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define ATTRIBUTES_NEED_NORMAL
			#define ATTRIBUTES_NEED_TEXCOORD0
			#define VARYINGS_NEED_NORMAL_WS
			#define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_VIEWDIRECTION_WS
            #define VARYINGS_NEED_SH
            #define VARYINGS_NEED_STATIC_LIGHTMAP_UV
            #define VARYINGS_NEED_DYNAMIC_LIGHTMAP_UV

            #define HAVE_MESH_MODIFICATION

            #define SHADERPASS SHADERPASS_DECAL_GBUFFER_PROJECTOR

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#if ( UNITY_VERSION >= 60010000 )
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
			#endif
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
			#if ( UNITY_VERSION >= 60010000 )
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GBufferOutput.hlsl"
			#else
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl"
			#endif
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DecalInput.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderVariablesDecal.hlsl"

			#if ( UNITY_VERSION >= 60040010 )
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GBufferOutputFormat.hlsl"
			#endif

		    #if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#if _RENDER_PASS_ENABLED
			#define GBUFFER3 0
			#define GBUFFER4 1
			#if ( UNITY_VERSION >= 60000067 && UNITY_VERSION < 60010000 ) || ( UNITY_VERSION >= 60030004 )
			FRAMEBUFFER_INPUT_X_FLOAT(GBUFFER3);
			#else
			FRAMEBUFFER_INPUT_X_HALF(GBUFFER3);
			#endif
			#if ( UNITY_VERSION >= 60020000 )
			FRAMEBUFFER_INPUT_X_UINT(GBUFFER4);
			#else
			FRAMEBUFFER_INPUT_X_HALF(GBUFFER4);
			#endif
			#endif

			/*ase_pragma*/

			struct SurfaceDescription
			{
				float3 BaseColor;
				float Alpha;
				float3 NormalTS;
				float NormalAlpha;
				float Metallic;
				float Occlusion;
				float Smoothness;
				float MAOSAlpha;
				float3 Emission;
			};

			struct Attributes
			{
				float3 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float4 tangentOS : TANGENT;
				/*ase_vdata:p=p;n=n;t=t*/
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				float4 positionCS : SV_POSITION;
				float3 normalWS : TEXCOORD0;
				float3 viewDirectionWS : TEXCOORD1;
				float4 lightmapUVs : TEXCOORD2; // @diogo: packs both static (xy) and dynamic (zw)
				float3 sh : TEXCOORD3;
				#ifdef USE_APV_PROBE_OCCLUSION
					float4 probeOcclusion : TEXCOORD4;
				#endif
				/*ase_interp(5,):sp=sp;wn=tc0;wvd=tc1*/
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

            CBUFFER_START(UnityPerMaterial)
			float _DrawOrder;
			float _DecalMeshBiasType;
			float _DecalMeshDepthBias;
			float _DecalMeshViewBias;
			#if defined(DECAL_ANGLE_FADE)
			float _DecalAngleFadeSupported;
			#endif
			UNITY_TEXTURE_STREAMING_DEBUG_VARS;
			CBUFFER_END

            #ifdef SCENEPICKINGPASS
				float4 _SelectionID;
            #endif

            #ifdef SCENESELECTIONPASS
				int _ObjectId;
				int _PassValue;
            #endif

			/*ase_globals*/

			/*ase_funcs*/

            void GetSurfaceData(SurfaceDescription surfaceDescription, float angleFadeFactor, out DecalSurfaceData surfaceData)
            {
                half4x4 normalToWorld = UNITY_ACCESS_INSTANCED_PROP(Decal, _NormalToWorld);
                half fadeFactor = clamp(normalToWorld[0][3], 0.0f, 1.0f) * angleFadeFactor;
                float2 scale = float2(normalToWorld[3][0], normalToWorld[3][1]);
                float2 offset = float2(normalToWorld[3][2], normalToWorld[3][3]);

                ZERO_INITIALIZE(DecalSurfaceData, surfaceData);
                surfaceData.occlusion = half(1.0);
                surfaceData.smoothness = half(0);

                #ifdef _MATERIAL_AFFECTS_NORMAL
                    surfaceData.normalWS.w = half(1.0);
                #else
                    surfaceData.normalWS.w = half(0.0);
                #endif

				#if defined( _MATERIAL_AFFECTS_EMISSION )
					surfaceData.emissive.rgb = half3(surfaceDescription.Emission.rgb * fadeFactor);
				#endif

                surfaceData.baseColor.xyz = half3(surfaceDescription.BaseColor);
                surfaceData.baseColor.w = half(surfaceDescription.Alpha * fadeFactor);

                #if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_GBUFFER_PROJECTOR)
                    #if defined(_MATERIAL_AFFECTS_NORMAL)
						surfaceData.normalWS.xyz = normalize(mul((half3x3)normalToWorld, surfaceDescription.NormalTS.xyz));
                    #else
						surfaceData.normalWS.xyz = normalize(normalToWorld[2].xyz);
                    #endif
                #elif (SHADERPASS == SHADERPASS_DBUFFER_MESH) || (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_MESH) || (SHADERPASS == SHADERPASS_DECAL_GBUFFER_MESH)
                    #if defined(_MATERIAL_AFFECTS_NORMAL)
                        float sgn = input.tangentWS.w;
                        float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
                        half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);
                        surfaceData.normalWS.xyz = normalize(TransformTangentToWorld(surfaceDescription.NormalTS, tangentToWorld));
                    #else
						surfaceData.normalWS.xyz = normalize(half3(input.normalWS));
                    #endif
                #endif

                surfaceData.normalWS.w = surfaceDescription.NormalAlpha * fadeFactor;

				#if defined( _MATERIAL_AFFECTS_MAOS )
					surfaceData.metallic = half(surfaceDescription.Metallic);
					surfaceData.occlusion = half(surfaceDescription.Occlusion);
					surfaceData.smoothness = half(surfaceDescription.Smoothness);
					surfaceData.MAOSAlpha = half(surfaceDescription.MAOSAlpha * fadeFactor);
				#endif
            }

            #if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_GBUFFER_PROJECTOR)
            #define DECAL_PROJECTOR
            #endif

            #if (SHADERPASS == SHADERPASS_DBUFFER_MESH) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_MESH) || (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_MESH) || (SHADERPASS == SHADERPASS_DECAL_GBUFFER_MESH)
            #define DECAL_MESH
            #endif

            #if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_DBUFFER_MESH)
            #define DECAL_DBUFFER
            #endif

            #if (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_MESH)
            #define DECAL_SCREEN_SPACE
            #endif

            #if (SHADERPASS == SHADERPASS_DECAL_GBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_GBUFFER_MESH)
            #define DECAL_GBUFFER
            #endif

            #if (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_PROJECTOR) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_MESH)
            #define DECAL_FORWARD_EMISSIVE
            #endif

            #if ((!defined(_MATERIAL_AFFECTS_NORMAL) && defined(_MATERIAL_AFFECTS_ALBEDO)) || (defined(_MATERIAL_AFFECTS_NORMAL) && defined(_MATERIAL_AFFECTS_NORMAL_BLEND))) && (defined(DECAL_SCREEN_SPACE) || defined(DECAL_GBUFFER))
            #define DECAL_RECONSTRUCT_NORMAL
            #elif defined(DECAL_ANGLE_FADE)
            #define DECAL_LOAD_NORMAL
            #endif

            #ifdef _DECAL_LAYERS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareRenderingLayerTexture.hlsl"
            #endif

            #if defined(DECAL_LOAD_NORMAL)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            #endif

            #if defined(DECAL_PROJECTOR) || defined(DECAL_RECONSTRUCT_NORMAL)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #endif

            #ifdef DECAL_MESH
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DecalMeshBiasTypeEnum.cs.hlsl"
            #endif

            #ifdef DECAL_RECONSTRUCT_NORMAL
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/NormalReconstruction.hlsl"
            #endif

			void InitializeInputData(PackedVaryings input, float3 positionWS, half3 normalWS, half3 viewDirectionWS, out InputData inputData)
			{
				inputData = (InputData)0;

				inputData.positionWS = positionWS;
				inputData.normalWS = normalWS;
				inputData.viewDirectionWS = viewDirectionWS;
				#if ( UNITY_VERSION >= 60070000 )
					inputData.shadowCoord = MainLightShadowsAvailable() ? TransformWorldToShadowCoord(positionWS) : float4(0, 0, 0, 0);
				#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
					inputData.shadowCoord = TransformWorldToShadowCoord(positionWS);
				#else
					inputData.shadowCoord = float4(0, 0, 0, 0);
				#endif
				#if ( UNITY_VERSION >= 60070000 )
					inputData.preExposureMultiplier = GetPreExposureMultiplier();
				#endif

				#ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
					inputData.fogCoord = InitializeInputDataFog(float4(positionWS, 1.0), input.fogFactorAndVertexLight.x);
					inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
				#endif

				#if ( UNITY_VERSION >= 60070000 )
					GIParams giParams = (GIParams)0;
					#if defined(LIGHTMAP_ON)
						giParams.staticLightmapUV = input.lightmapUVs.xy;
					#else
						giParams.vertexSH = half3(input.sh);
					#endif
					#if defined(DYNAMICLIGHTMAP_ON)
						giParams.dynamicLightmapUV = input.lightmapUVs.zw;
					#endif
					#if defined(USE_APV_PROBE_OCCLUSION)
						giParams.vertexProbeOcclusion = input.probeOcclusion;
					#endif
					giParams.positionWS = inputData.positionWS;
					giParams.normalWS = inputData.normalWS;
					giParams.viewDirWS = inputData.viewDirectionWS;
					giParams.positionSS = input.positionCS.xy;
					InitializeBakedGI( giParams, inputData.bakedGI, inputData.shadowMask );
				#elif defined(_SCREEN_SPACE_IRRADIANCE) && ( UNITY_VERSION >= 60030000 )
					#if ( UNITY_VERSION >= 60060000 )
						inputData.bakedGI = SAMPLE_GI(_ScreenSpaceIrradiance, input.positionCS.xy, inputData.normalWS);
					#else
						inputData.bakedGI = SAMPLE_GI(_ScreenSpaceIrradiance, input.positionCS.xy);
					#endif
				#elif defined(VARYINGS_NEED_DYNAMIC_LIGHTMAP_UV) && defined(DYNAMICLIGHTMAP_ON)
					inputData.bakedGI = SAMPLE_GI(input.lightmapUVs.xy, input.lightmapUVs.zw, half3(input.sh), normalWS);
					#if defined(VARYINGS_NEED_STATIC_LIGHTMAP_UV)
					inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUVs.xy);
					#endif
				#elif defined(VARYINGS_NEED_STATIC_LIGHTMAP_UV)
				#if !defined(LIGHTMAP_ON) && (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
    				inputData.bakedGI = SAMPLE_GI(input.sh,
					GetAbsolutePositionWS(inputData.positionWS),
					inputData.normalWS,
					inputData.viewDirectionWS,
					input.positionCS.xy,
					input.probeOcclusion,
					inputData.shadowMask);
				#else
					inputData.bakedGI = SAMPLE_GI(input.lightmapUVs.xy, half3(input.sh), normalWS);
					#if defined(VARYINGS_NEED_STATIC_LIGHTMAP_UV)
					inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUVs.xy);
					#endif
				#endif
				#endif

				#if defined(DEBUG_DISPLAY)
					#if defined(VARYINGS_NEED_DYNAMIC_LIGHTMAP_UV) && defined(DYNAMICLIGHTMAP_ON)
						inputData.dynamicLightmapUV = input.lightmapUVs.zw;
					#endif
					#if defined(VARYINGS_NEED_STATIC_LIGHTMAP_UV) && defined(LIGHTMAP_ON)
						inputData.staticLightmapUV = input.lightmapUVs.xy;
					#elif defined(VARYINGS_NEED_SH)
						inputData.vertexSH = input.sh;
					#endif
					#if defined(USE_APV_PROBE_OCCLUSION)
						inputData.probeOcclusion = input.probeOcclusion;
					#endif
				#endif

				inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
			}

			void GetSurface(DecalSurfaceData decalSurfaceData, inout SurfaceData surfaceData)
			{
				surfaceData.albedo = decalSurfaceData.baseColor.rgb;
				surfaceData.metallic = saturate(decalSurfaceData.metallic);
				surfaceData.specular = 0;
				surfaceData.smoothness = saturate(decalSurfaceData.smoothness);
				surfaceData.occlusion = decalSurfaceData.occlusion;
				surfaceData.emission = decalSurfaceData.emissive;
				surfaceData.alpha = saturate(decalSurfaceData.baseColor.w);
				surfaceData.clearCoatMask = 0;
				surfaceData.clearCoatSmoothness = 1;
			}

			PackedVaryings Vert(Attributes inputMesh /*ase_vert_input*/ )
			{
				PackedVaryings packedOutput;
				ZERO_INITIALIZE(PackedVaryings, packedOutput);

				UNITY_SETUP_INSTANCE_ID(inputMesh);
				UNITY_TRANSFER_INSTANCE_ID(inputMesh, packedOutput);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(packedOutput);

				inputMesh.tangentOS = float4( 1, 0, 0, -1 );
				inputMesh.normalOS = float3( 0, 1, 0 );

				/*ase_vert_code:inputMesh=Attributes;packedOutput=PackedVaryings*/

				float3 positionWS = TransformObjectToWorld(inputMesh.positionOS);
				float3 normalWS = TransformObjectToWorldNormal(inputMesh.normalOS);

				packedOutput.positionCS = TransformWorldToHClip(positionWS);
				packedOutput.normalWS.xyz =  normalWS;
				packedOutput.viewDirectionWS.xyz =  GetWorldSpaceViewDir(positionWS);

				#if defined(LIGHTMAP_ON)
					OUTPUT_LIGHTMAP_UV(inputMesh.uv1, unity_LightmapST, packedOutput.lightmapUVs.xy);
				#endif

				#if defined(DYNAMICLIGHTMAP_ON)
					packedOutput.lightmapUVs.zw = inputMesh.uv2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
				#endif

				#if !defined(LIGHTMAP_ON)
					packedOutput.sh = float3(SampleSHVertex(half3(normalWS)));
				#endif

				return packedOutput;
			}

			void Frag(PackedVaryings packedInput,
				#if ( UNITY_VERSION >= 60010000 )
				out GBufferFragOutput fragmentOutput
				#else
				out FragmentOutput fragmentOutput
				#endif
				/*ase_frag_input*/ )
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
				UNITY_SETUP_INSTANCE_ID(packedInput);

				half angleFadeFactor = 1.0;

            // Only screen space needs flip logic, other passes do not setup needed properties so we skip here
            #if defined(DECAL_SCREEN_SPACE)
				TransformScreenUV(packedInput.positionCS, _ScreenSize.y);
            #endif

            #ifdef _DECAL_LAYERS
            #ifdef _RENDER_PASS_ENABLED
				#if ( UNITY_VERSION >= 60020000 )
				uint surfaceRenderingLayer = LOAD_FRAMEBUFFER_X_INPUT(GBUFFER4, packedInput.positionCS.xy).r;
				#else
				uint surfaceRenderingLayer = DecodeMeshRenderingLayer(LOAD_FRAMEBUFFER_X_INPUT(GBUFFER4, packedInput.positionCS.xy).r);
				#endif
            #else
				uint surfaceRenderingLayer = LoadSceneRenderingLayer(packedInput.positionCS.xy);
            #endif
				#if ( UNITY_VERSION >= 60030000 )
				uint projectorRenderingLayer = asuint(UNITY_ACCESS_INSTANCED_PROP(Decal, _DecalLayerMaskFromDecal));
				#else
				uint projectorRenderingLayer = uint(UNITY_ACCESS_INSTANCED_PROP(Decal, _DecalLayerMaskFromDecal));
				#endif
				clip((surfaceRenderingLayer & projectorRenderingLayer) - 0.1);
            #endif

			#if defined(DECAL_PROJECTOR)
			#if UNITY_REVERSED_Z
			#if _RENDER_PASS_ENABLED
				float depth = LOAD_FRAMEBUFFER_X_INPUT(GBUFFER3, packedInput.positionCS.xy).x;
			#else
				float depth = LoadSceneDepth(packedInput.positionCS.xy);
			#endif
			#else
			#if _RENDER_PASS_ENABLED
				float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, LOAD_FRAMEBUFFER_X_INPUT(GBUFFER3, packedInput.positionCS.xy));
			#else
			    // Adjust z to match NDC for OpenGL
				float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, LoadSceneDepth(packedInput.positionCS.xy));
			#endif
			#endif
			#endif

			#if defined(DECAL_RECONSTRUCT_NORMAL)
				#if defined(_RENDER_PASS_ENABLED) && ( ( UNITY_VERSION >= 60000067 && UNITY_VERSION < 60010000 ) || ( UNITY_VERSION >= 60030004 ) )
					half3 normalWS = half3(ReconstructNormalDerivative(packedInput.positionCS.xy, LOAD_FRAMEBUFFER_X_INPUT(GBUFFER3, packedInput.positionCS.xy).x));
				#elif defined(_DECAL_NORMAL_BLEND_HIGH)
					half3 normalWS = half3(ReconstructNormalTap9(packedInput.positionCS.xy));
				#elif defined(_DECAL_NORMAL_BLEND_MEDIUM)
					half3 normalWS = half3(ReconstructNormalTap5(packedInput.positionCS.xy));
				#else
					half3 normalWS = half3(ReconstructNormalDerivative(packedInput.positionCS.xy));
				#endif
			#elif defined(DECAL_LOAD_NORMAL)
				#if defined(_RENDER_PASS_ENABLED) && ( ( UNITY_VERSION >= 60000067 && UNITY_VERSION < 60010000 ) || ( UNITY_VERSION >= 60030004 ) )
					half3 normalWS = normalize(LOAD_FRAMEBUFFER_X_INPUT(GBUFFER2, packedInput.positionCS.xy).rgb);
				#else
					half3 normalWS = half3(LoadSceneNormals(packedInput.positionCS.xy));
				#endif
			#endif

				float2 positionSS = FoveatedRemapNonUniformToLinearCS(packedInput.positionCS.xy) * _ScreenSize.zw;

				float4 positionCS = ComputeClipSpacePosition( positionSS, depth );
				float4 hpositionVS = mul( UNITY_MATRIX_I_P, positionCS );

				/*ase_local_var:spn*/float4 ScreenPosNorm = float4( positionSS, positionCS.zw );
				/*ase_local_var:sp*/float4 ClipPos = positionCS * packedInput.positionCS.w;
				/*ase_local_var:spu*/float4 ScreenPos = ComputeScreenPos( ClipPos );
				/*ase_local_var:rwp*/float3 PositionRWS = mul( ( float3x3 )UNITY_MATRIX_I_V, hpositionVS.xyz / hpositionVS.w );
				/*ase_local_var:wp*/float3 PositionWS = PositionRWS + _WorldSpaceCameraPos;
				/*ase_local_var:op*/float3 PositionOS = TransformWorldToObject( PositionWS );
				/*ase_local_var:vp*/float3 PositionVS = TransformWorldToView( PositionWS );

				float3 positionDS = TransformWorldToObject(PositionWS);
				positionDS = positionDS * float3(1.0, -1.0, 1.0);

				float clipValue = 0.5 - Max3(abs(positionDS).x, abs(positionDS).y, abs(positionDS).z);
				clip(clipValue);

				float2 texCoord = positionDS.xz + float2(0.5, 0.5);

				float4x4 normalToWorld = UNITY_ACCESS_INSTANCED_PROP(Decal, _NormalToWorld);
				float2 scale = float2(normalToWorld[3][0], normalToWorld[3][1]);
				float2 offset = float2(normalToWorld[3][2], normalToWorld[3][3]);
				texCoord.xy = texCoord.xy * scale + offset;

				/*ase_local_var:uv0*/float2 texCoord0 = texCoord;
				/*ase_local_var:uv1*/float2 texCoord1 = texCoord;
				/*ase_local_var:uv2*/float2 texCoord2 = texCoord;
				/*ase_local_var:uv3*/float2 texCoord3 = texCoord;

				/*ase_local_var:wt*/float3 worldTangent = TransformObjectToWorldDir(float3(1, 0, 0));
				/*ase_local_var:wn*/float3 worldNormal = TransformObjectToWorldDir(float3(0, 1, 0));
				/*ase_local_var:wbt*/float3 worldBitangent = TransformObjectToWorldDir(float3(0, 0, 1));

			#ifdef DECAL_ANGLE_FADE
				half2 angleFade = half2(normalToWorld[1][3], normalToWorld[2][3]);
				if (angleFade.y < 0.0f)
				{
					half3 decalNormal = half3(normalToWorld[0].z, normalToWorld[1].z, normalToWorld[2].z);
					half dotAngle = dot(normalWS, decalNormal);
					angleFadeFactor = saturate(angleFade.x + angleFade.y * (dotAngle * (dotAngle - 2.0)));
				}
			#endif

				half3 viewDirectionWS = half3(packedInput.viewDirectionWS);
				DecalSurfaceData surfaceData;

				SurfaceDescription surfaceDescription = (SurfaceDescription)0;

				/*ase_frag_code:packedInput=PackedVaryings*/

				surfaceDescription.BaseColor = /*ase_frag_out:Base Color;Float3;0;-1;_BaseColor*/IsGammaSpace() ? float3(0.5, 0.5, 0.5) : SRGBToLinear(float3(0.5, 0.5, 0.5))/*end*/;
				surfaceDescription.Alpha = /*ase_frag_out:Alpha;Float;1;-1;_Alpha*/1/*end*/;
				surfaceDescription.NormalTS = /*ase_frag_out:Normal;Float3;2;-1;_NormalTS*/float3(0.0f, 0.0f, 1.0f)/*end*/;
				surfaceDescription.NormalAlpha = /*ase_frag_out:Normal Alpha;Float;3;-1;_NormalAlpha*/1/*end*/;

			#if defined( _MATERIAL_AFFECTS_MAOS )
				surfaceDescription.Metallic = /*ase_frag_out:Metallic;Float;4;-1;_Metallic*/0/*end*/;
				surfaceDescription.Occlusion =/*ase_frag_out:Occlusion;Float;5;-1;_Occlusion*/1/*end*/;
				surfaceDescription.Smoothness = /*ase_frag_out:Smoothness;Float;6;-1;_Smoothness*/0.5/*end*/;
				surfaceDescription.MAOSAlpha = /*ase_frag_out:MAOS Alpha;Float;7;-1;_MAOSAlpha*/1/*end*/;
			#endif

			#if defined( _MATERIAL_AFFECTS_EMISSION )
				surfaceDescription.Emission = /*ase_frag_out:Emission;Float;8;-1;_Emission*/float3(0, 0, 0)/*end*/;
			#endif

				GetSurfaceData(surfaceDescription, angleFadeFactor, surfaceData);

				// Need to reconstruct normal here for inputData.bakedGI, but also save off surfaceData.normalWS for correct GBuffer blending
				half3 normalToPack = surfaceData.normalWS.xyz;

			#ifdef DECAL_RECONSTRUCT_NORMAL
				surfaceData.normalWS.xyz = normalize(lerp(normalWS.xyz, surfaceData.normalWS.xyz, surfaceData.normalWS.w));
			#endif

				InputData inputData;
				InitializeInputData(packedInput, PositionWS, surfaceData.normalWS.xyz, viewDirectionWS, inputData);

				SurfaceData surface = (SurfaceData)0;
				GetSurface(surfaceData, surface);

				BRDFData brdfData;
				InitializeBRDFData(surface.albedo, surface.metallic, 0, surface.smoothness, surface.alpha, brdfData);

            // Skip GI if there is no abledo
			#ifdef _MATERIAL_AFFECTS_ALBEDO
				Light mainLight = GetMainLight(inputData.shadowCoord, inputData.positionWS, inputData.shadowMask);
				MixRealtimeAndBakedGI(mainLight, surfaceData.normalWS.xyz, inputData.bakedGI, inputData.shadowMask);
				ASE_LIGHT_ACCUM3 color = GlobalIllumination(brdfData, inputData.bakedGI, surface.occlusion, surfaceData.normalWS.xyz, inputData.viewDirectionWS);
			#else
				ASE_LIGHT_ACCUM3 color = 0;
			#endif

				// ShaderPassDecal.hlsl
				// We can not use usual GBuffer functions (etc. BRDFDataToGbuffer) as we use alpha for blending
				#pragma warning (disable : 3578) // The output value isn't completely initialized.
			#if ( UNITY_VERSION >= 60010000 )
				half3 packedNormalWS = PackGBufferNormal(surfaceData.normalWS.xyz);
				fragmentOutput = (GBufferFragOutput)0;
				fragmentOutput.gBuffer0 = half4(surfaceData.baseColor.rgb, surfaceData.baseColor.a);
				fragmentOutput.gBuffer1 = 0;
				fragmentOutput.gBuffer2 = half4(packedNormalWS, surfaceData.normalWS.a);
				#if ( UNITY_VERSION >= 60070000 )
				fragmentOutput.color = half4(ClampExposed(inputData.preExposureMultiplier * (surfaceData.emissive + color)), surfaceData.baseColor.a);
				#else
				fragmentOutput.color = half4(surfaceData.emissive + color, surfaceData.baseColor.a);
				#endif
				#if defined(GBUFFER_FEATURE_SHADOWMASK)
					fragmentOutput.shadowMask = inputData.shadowMask; // will have unity_ProbesOcclusion value if subtractive lighting is used (baked)
				#endif
			#else
				half3 packedNormalWS = PackNormal(surfaceData.normalWS.xyz);
				fragmentOutput.GBuffer0 = half4(surfaceData.baseColor.rgb, surfaceData.baseColor.a);
				fragmentOutput.GBuffer1 = 0;
				fragmentOutput.GBuffer2 = half4(packedNormalWS, surfaceData.normalWS.a);
				fragmentOutput.GBuffer3 = half4(surfaceData.emissive + color, surfaceData.baseColor.a);
				#if OUTPUT_SHADOWMASK
					fragmentOutput.GBuffer4 = inputData.shadowMask; // will have unity_ProbesOcclusion value if subtractive lighting is used (baked)
				#endif
			#endif

			#pragma warning (default : 3578) // Restore output value isn't completely initialized.

			#if defined(DECAL_FORWARD_EMISSIVE)
				// Emissive need to be pre-exposed
				#if ( UNITY_VERSION >= 60070000 )
				outEmissive.rgb = ClampExposed(GetPreExposureMultiplier() * surfaceData.emissive);
				#else
				outEmissive.rgb = surfaceData.emissive * GetCurrentExposureMultiplier();
				#endif
				outEmissive.a = surfaceData.baseColor.a;
			#endif
			}
            ENDHLSL
        }

		/*ase_pass*/
        Pass
        {
            /*ase_hide_pass*/
			/*ase_immutable*/ Name "DBufferMesh"
            Tags
            {
                "LightMode" = "DBufferMesh"
            }

			Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			ZTest LEqual
			ZWrite Off
			ColorMask RGBA
			ColorMask RGBA 1
			ColorMask RGBA 2

			HLSLPROGRAM

			#pragma vertex Vert
			#pragma fragment Frag
			#pragma multi_compile_instancing
			#pragma editor_sync_compilation

			#pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
			#pragma multi_compile _ _DECAL_LAYERS

			#if ( UNITY_VERSION >= 60060000 )
			#pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
			#endif

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_TEXCOORD1
            #define ATTRIBUTES_NEED_TEXCOORD2
            #define VARYINGS_NEED_POSITION_WS
            #define VARYINGS_NEED_NORMAL_WS
            #define VARYINGS_NEED_TANGENT_WS
            #define VARYINGS_NEED_TEXCOORD0

            #define HAVE_MESH_MODIFICATION

            #define SHADERPASS SHADERPASS_DBUFFER_MESH

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#if ( UNITY_VERSION >= 60010000 )
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
			#endif
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DecalInput.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderVariablesDecal.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#if _RENDER_PASS_ENABLED
			#define GBUFFER3 0
			#define GBUFFER4 1
			#if ( UNITY_VERSION >= 60000067 && UNITY_VERSION < 60010000 ) || ( UNITY_VERSION >= 60030004 )
			FRAMEBUFFER_INPUT_X_FLOAT(GBUFFER3);
			#else
			FRAMEBUFFER_INPUT_X_HALF(GBUFFER3);
			#endif
			#if ( UNITY_VERSION >= 60020000 )
			FRAMEBUFFER_INPUT_X_UINT(GBUFFER4);
			#else
			FRAMEBUFFER_INPUT_X_HALF(GBUFFER4);
			#endif
			#endif

            /*ase_pragma*/

			struct SurfaceDescription
			{
				float3 BaseColor;
				float Alpha;
				float3 NormalTS;
				float NormalAlpha;
				float Metallic;
				float Occlusion;
				float Smoothness;
				float MAOSAlpha;
			};

			struct Attributes
			{
				float3 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float4 tangentOS : TANGENT;
				float4 uv0 : TEXCOORD0;
				float4 uv1 : TEXCOORD1;
				float4 uv2 : TEXCOORD2;
				/*ase_vdata:p=p;n=n;t=t;uv0=tc0;uv1=tc1;uv2=tc2*/
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				float3 normalWS : TEXCOORD1;
				float4 tangentWS : TEXCOORD2;
				float4 texCoord0 : TEXCOORD3;
				/*ase_interp(4,):sp=sp;wp=tc0;wn=tc1;wt=tc2;uv0=tc3*/
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

            CBUFFER_START(UnityPerMaterial)
			float _DrawOrder;
			float _DecalMeshBiasType;
			float _DecalMeshDepthBias;
			float _DecalMeshViewBias;
			#if defined(DECAL_ANGLE_FADE)
			float _DecalAngleFadeSupported;
			#endif
			UNITY_TEXTURE_STREAMING_DEBUG_VARS;
			CBUFFER_END

            #ifdef SCENEPICKINGPASS
				float4 _SelectionID;
            #endif

            #ifdef SCENESELECTIONPASS
				int _ObjectId;
				int _PassValue;
            #endif

			/*ase_globals*/

			/*ase_funcs*/

            void GetSurfaceData(PackedVaryings input, SurfaceDescription surfaceDescription, out DecalSurfaceData surfaceData)
            {
                #if defined(LOD_FADE_CROSSFADE)
					LODFadeCrossFade( input.positionCS );
                #endif

                half fadeFactor = half(1.0);

                ZERO_INITIALIZE(DecalSurfaceData, surfaceData);
                surfaceData.occlusion = half(1.0);
                surfaceData.smoothness = half(0);

                #ifdef _MATERIAL_AFFECTS_NORMAL
                    surfaceData.normalWS.w = half(1.0);
                #else
                    surfaceData.normalWS.w = half(0.0);
                #endif

                surfaceData.baseColor.xyz = half3(surfaceDescription.BaseColor);
                surfaceData.baseColor.w = half(surfaceDescription.Alpha * fadeFactor);

                #if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_GBUFFER_PROJECTOR)
                    #if defined(_MATERIAL_AFFECTS_NORMAL)
						surfaceData.normalWS.xyz = normalize(mul((half3x3)normalToWorld, surfaceDescription.NormalTS.xyz));
                    #else
						surfaceData.normalWS.xyz = normalize(normalToWorld[2].xyz);
                    #endif
                #elif (SHADERPASS == SHADERPASS_DBUFFER_MESH) || (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_MESH) || (SHADERPASS == SHADERPASS_DECAL_GBUFFER_MESH)
                    #if defined(_MATERIAL_AFFECTS_NORMAL)
                        float sgn = input.tangentWS.w;
                        float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
                        half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);
                        surfaceData.normalWS.xyz = normalize(TransformTangentToWorld(surfaceDescription.NormalTS, tangentToWorld));
                    #else
						surfaceData.normalWS.xyz = normalize(half3(input.normalWS));
                    #endif
                #endif

                surfaceData.normalWS.w = surfaceDescription.NormalAlpha * fadeFactor;

				#if defined( _MATERIAL_AFFECTS_MAOS )
					surfaceData.metallic = half(surfaceDescription.Metallic);
					surfaceData.occlusion = half(surfaceDescription.Occlusion);
					surfaceData.smoothness = half(surfaceDescription.Smoothness);
					surfaceData.MAOSAlpha = half(surfaceDescription.MAOSAlpha * fadeFactor);
				#endif
            }

            #if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_GBUFFER_PROJECTOR)
            #define DECAL_PROJECTOR
            #endif

            #if (SHADERPASS == SHADERPASS_DBUFFER_MESH) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_MESH) || (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_MESH) || (SHADERPASS == SHADERPASS_DECAL_GBUFFER_MESH)
            #define DECAL_MESH
            #endif

            #if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_DBUFFER_MESH)
            #define DECAL_DBUFFER
            #endif

            #if (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_MESH)
            #define DECAL_SCREEN_SPACE
            #endif

            #if (SHADERPASS == SHADERPASS_DECAL_GBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_GBUFFER_MESH)
            #define DECAL_GBUFFER
            #endif

            #if (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_PROJECTOR) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_MESH)
            #define DECAL_FORWARD_EMISSIVE
            #endif

            #if ((!defined(_MATERIAL_AFFECTS_NORMAL) && defined(_MATERIAL_AFFECTS_ALBEDO)) || (defined(_MATERIAL_AFFECTS_NORMAL) && defined(_MATERIAL_AFFECTS_NORMAL_BLEND))) && (defined(DECAL_SCREEN_SPACE) || defined(DECAL_GBUFFER))
            #define DECAL_RECONSTRUCT_NORMAL
            #elif defined(DECAL_ANGLE_FADE)
            #define DECAL_LOAD_NORMAL
            #endif

            #ifdef _DECAL_LAYERS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareRenderingLayerTexture.hlsl"
            #endif

            #if defined(DECAL_LOAD_NORMAL)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            #endif

            #if defined(DECAL_PROJECTOR) || defined(DECAL_RECONSTRUCT_NORMAL)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #endif

            #ifdef DECAL_MESH
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DecalMeshBiasTypeEnum.cs.hlsl"
            #endif

            #ifdef DECAL_RECONSTRUCT_NORMAL
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/NormalReconstruction.hlsl"
            #endif

			void MeshDecalsPositionZBias(inout PackedVaryings input)
			{
            #if UNITY_REVERSED_Z
				input.positionCS.z -= _DecalMeshDepthBias;
            #else
				input.positionCS.z += _DecalMeshDepthBias;
            #endif
			}

			PackedVaryings Vert(Attributes inputMesh /*ase_vert_input*/ )
			{
				if (_DecalMeshBiasType == DECALMESHDEPTHBIASTYPE_VIEW_BIAS)
				{
					float3 viewDirectionOS = GetObjectSpaceNormalizeViewDir(inputMesh.positionOS);
					inputMesh.positionOS += viewDirectionOS * (_DecalMeshViewBias);
				}

				PackedVaryings packedOutput;
				ZERO_INITIALIZE(PackedVaryings, packedOutput);

				UNITY_SETUP_INSTANCE_ID(inputMesh);
				UNITY_TRANSFER_INSTANCE_ID(inputMesh, packedOutput);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(packedOutput);

				/*ase_vert_code:inputMesh=Attributes;packedOutput=PackedVaryings*/

				VertexPositionInputs vertexInput = GetVertexPositionInputs(inputMesh.positionOS.xyz);

				float3 positionWS = TransformObjectToWorld(inputMesh.positionOS);

				float3 normalWS = TransformObjectToWorldNormal(inputMesh.normalOS);
				float4 tangentWS = float4(TransformObjectToWorldDir(inputMesh.tangentOS.xyz), inputMesh.tangentOS.w);

				packedOutput.positionWS.xyz =  positionWS;
				packedOutput.normalWS.xyz =  normalWS;
				packedOutput.tangentWS.xyzw =  tangentWS;
				packedOutput.texCoord0.xyzw =  inputMesh.uv0;
				packedOutput.positionCS = TransformWorldToHClip(positionWS);

				if (_DecalMeshBiasType == DECALMESHDEPTHBIASTYPE_DEPTH_BIAS)
				{
					MeshDecalsPositionZBias(packedOutput);
				}

				return packedOutput;
			}

			void Frag(PackedVaryings packedInput,
				OUTPUT_DBUFFER(outDBuffer)
				/*ase_frag_input*/
			)
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
				UNITY_SETUP_INSTANCE_ID(packedInput);

				half angleFadeFactor = 1.0;

            // Only screen space needs flip logic, other passes do not setup needed properties so we skip here
            #if defined(DECAL_SCREEN_SPACE)
				TransformScreenUV(packedInput.positionCS, _ScreenSize.y);
            #endif

            #ifdef _DECAL_LAYERS
            #ifdef _RENDER_PASS_ENABLED
				#if ( UNITY_VERSION >= 60020000 )
				uint surfaceRenderingLayer = LOAD_FRAMEBUFFER_X_INPUT(GBUFFER4, packedInput.positionCS.xy).r;
				#else
				uint surfaceRenderingLayer = DecodeMeshRenderingLayer(LOAD_FRAMEBUFFER_X_INPUT(GBUFFER4, packedInput.positionCS.xy).r);
				#endif
            #else
				uint surfaceRenderingLayer = LoadSceneRenderingLayer(packedInput.positionCS.xy);
            #endif
				#if ( UNITY_VERSION >= 60030000 )
				uint projectorRenderingLayer = asuint(UNITY_ACCESS_INSTANCED_PROP(Decal, _DecalLayerMaskFromDecal));
				#else
				uint projectorRenderingLayer = uint(UNITY_ACCESS_INSTANCED_PROP(Decal, _DecalLayerMaskFromDecal));
				#endif
				clip((surfaceRenderingLayer & projectorRenderingLayer) - 0.1);
            #endif

			#if defined(DECAL_PROJECTOR)
			#if UNITY_REVERSED_Z
			#if _RENDER_PASS_ENABLED
				float depth = LOAD_FRAMEBUFFER_X_INPUT(GBUFFER3, packedInput.positionCS.xy).x;
			#else
				float depth = LoadSceneDepth(packedInput.positionCS.xy);
			#endif
			#else
			#if _RENDER_PASS_ENABLED
				float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, LOAD_FRAMEBUFFER_X_INPUT(GBUFFER3, packedInput.positionCS.xy));
			#else
			    // Adjust z to match NDC for OpenGL
				float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, LoadSceneDepth(packedInput.positionCS.xy));
			#endif
			#endif
			#endif

			#if defined(DECAL_RECONSTRUCT_NORMAL)
				#if defined(_RENDER_PASS_ENABLED) && ( ( UNITY_VERSION >= 60000067 && UNITY_VERSION < 60010000 ) || ( UNITY_VERSION >= 60030004 ) )
					half3 normalWS = half3(ReconstructNormalDerivative(packedInput.positionCS.xy, LOAD_FRAMEBUFFER_X_INPUT(GBUFFER3, packedInput.positionCS.xy).x));
				#elif defined(_DECAL_NORMAL_BLEND_HIGH)
					half3 normalWS = half3(ReconstructNormalTap9(packedInput.positionCS.xy));
				#elif defined(_DECAL_NORMAL_BLEND_MEDIUM)
					half3 normalWS = half3(ReconstructNormalTap5(packedInput.positionCS.xy));
				#else
					half3 normalWS = half3(ReconstructNormalDerivative(packedInput.positionCS.xy));
				#endif
			#elif defined(DECAL_LOAD_NORMAL)
				#if defined(_RENDER_PASS_ENABLED) && ( ( UNITY_VERSION >= 60000067 && UNITY_VERSION < 60010000 ) || ( UNITY_VERSION >= 60030004 ) )
					half3 normalWS = normalize(LOAD_FRAMEBUFFER_X_INPUT(GBUFFER2, packedInput.positionCS.xy).rgb);
				#else
					half3 normalWS = half3(LoadSceneNormals(packedInput.positionCS.xy));
				#endif
			#endif

				float2 positionSS = FoveatedRemapNonUniformToLinearCS(packedInput.positionCS.xy) * _ScreenSize.zw;

				float3 positionWS = packedInput.positionWS.xyz;
				half3 viewDirectionWS = half3(1.0, 1.0, 1.0);

				DecalSurfaceData surfaceData;
				SurfaceDescription surfaceDescription;

				/*ase_frag_code:packedInput=PackedVaryings*/

				surfaceDescription.BaseColor = /*ase_frag_out:Base Color;Float3;0;-1;_BaseColor*/IsGammaSpace() ? float3(0.5, 0.5, 0.5) : SRGBToLinear(float3(0.5, 0.5, 0.5))/*end*/;
				surfaceDescription.Alpha = /*ase_frag_out:Alpha;Float;1;-1;_Alpha*/1/*end*/;
				surfaceDescription.NormalTS = /*ase_frag_out:Normal;Float3;2;-1;_NormalTS*/float3(0.0f, 0.0f, 1.0f)/*end*/;
				surfaceDescription.NormalAlpha = /*ase_frag_out:Normal Alpha;Float;3;-1;_NormalAlpha*/1/*end*/;

			#if defined( _MATERIAL_AFFECTS_MAOS )
				surfaceDescription.Metallic = /*ase_frag_out:Metallic;Float;4;-1;_Metallic*/0/*end*/;
				surfaceDescription.Occlusion = /*ase_frag_out:Occlusion;Float;5;-1;_Occlusion*/1/*end*/;
				surfaceDescription.Smoothness = /*ase_frag_out:Smoothness;Float;6;-1;_Smoothness*/0.5/*end*/;
				surfaceDescription.MAOSAlpha = /*ase_frag_out:MAOS Alpha;Float;7;-1;_MAOSAlpha*/1/*end*/;
			#endif

				GetSurfaceData(packedInput, surfaceDescription, surfaceData);
				ENCODE_INTO_DBUFFER(surfaceData, outDBuffer);

			}

            ENDHLSL
        }

		/*ase_pass*/
        Pass
        {
            /*ase_hide_pass*/
			/*ase_immutable*/ Name "DecalMeshForwardEmissive"
            Tags
            {
                "LightMode" = "DecalMeshForwardEmissive"
            }

			Blend 0 SrcAlpha One
			ZTest LEqual
			ZWrite Off

			HLSLPROGRAM

			#pragma vertex Vert
			#pragma fragment Frag
			#pragma multi_compile_instancing
			#pragma editor_sync_compilation

			#pragma multi_compile _ _DECAL_LAYERS

			#if ( UNITY_VERSION >= 60070000 )
			#pragma multi_compile _ _EXPOSURE
			#endif

			#if ( UNITY_VERSION >= 60060000 )
			#pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
			#endif

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_TEXCOORD1
            #define ATTRIBUTES_NEED_TEXCOORD2
            #define VARYINGS_NEED_POSITION_WS
            #define VARYINGS_NEED_NORMAL_WS
            #define VARYINGS_NEED_TANGENT_WS
            #define VARYINGS_NEED_TEXCOORD0

            #define HAVE_MESH_MODIFICATION

            #define SHADERPASS SHADERPASS_FORWARD_EMISSIVE_MESH

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#if ( UNITY_VERSION >= 60010000 )
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
			#endif
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DecalInput.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderVariablesDecal.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#if _RENDER_PASS_ENABLED
			#define GBUFFER3 0
			#define GBUFFER4 1
			#if ( UNITY_VERSION >= 60000067 && UNITY_VERSION < 60010000 ) || ( UNITY_VERSION >= 60030004 )
			FRAMEBUFFER_INPUT_X_FLOAT(GBUFFER3);
			#else
			FRAMEBUFFER_INPUT_X_HALF(GBUFFER3);
			#endif
			#if ( UNITY_VERSION >= 60020000 )
			FRAMEBUFFER_INPUT_X_UINT(GBUFFER4);
			#else
			FRAMEBUFFER_INPUT_X_HALF(GBUFFER4);
			#endif
			#endif

			/*ase_pragma*/

			struct SurfaceDescription
			{
				float3 BaseColor;
				float Alpha;
				float3 NormalTS;
				float NormalAlpha;
				float Metallic;
				float Occlusion;
				float Smoothness;
				float MAOSAlpha;
				float3 Emission;
			};

            struct Attributes
			{
				float3 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float4 tangentOS : TANGENT;
				float4 uv0 : TEXCOORD0;
				float4 uv1 : TEXCOORD1;
				float4 uv2 : TEXCOORD2;
				/*ase_vdata:p=p;n=n;t=t;uv0=tc0;uv1=tc1;uv2=tc2*/
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				float3 normalWS : TEXCOORD1;
				float4 tangentWS : TEXCOORD2;
				float4 texCoord0 : TEXCOORD3;
				/*ase_interp(4,):sp=sp;wp=tc0;wn=tc1;wt=tc2;uv0=tc3*/
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

            CBUFFER_START(UnityPerMaterial)
			float _DrawOrder;
			float _DecalMeshBiasType;
			float _DecalMeshDepthBias;
			float _DecalMeshViewBias;
			#if defined(DECAL_ANGLE_FADE)
			float _DecalAngleFadeSupported;
			#endif
			UNITY_TEXTURE_STREAMING_DEBUG_VARS;
			CBUFFER_END

            #ifdef SCENEPICKINGPASS
				float4 _SelectionID;
            #endif

            #ifdef SCENESELECTIONPASS
				int _ObjectId;
				int _PassValue;
            #endif

			/*ase_globals*/

			/*ase_funcs*/

            void GetSurfaceData(SurfaceDescription surfaceDescription, float4 positionCS, out DecalSurfaceData surfaceData)
            {
                #if defined(LOD_FADE_CROSSFADE)
					LODFadeCrossFade( positionCS );
                #endif

                half fadeFactor = half(1.0);

                ZERO_INITIALIZE(DecalSurfaceData, surfaceData);
                surfaceData.occlusion = half(1.0);
                surfaceData.smoothness = half(0);

                #ifdef _MATERIAL_AFFECTS_NORMAL
                    surfaceData.normalWS.w = half(1.0);
                #else
                    surfaceData.normalWS.w = half(0.0);
                #endif

				#if defined( _MATERIAL_AFFECTS_EMISSION )
					surfaceData.emissive.rgb = half3(surfaceDescription.Emission.rgb * fadeFactor);
				#endif

                surfaceData.baseColor.xyz = half3(surfaceDescription.BaseColor);
                surfaceData.baseColor.w = half(surfaceDescription.Alpha * fadeFactor);

                surfaceData.normalWS.w = surfaceDescription.NormalAlpha * fadeFactor;

				#if defined( _MATERIAL_AFFECTS_MAOS )
					surfaceData.metallic = half(surfaceDescription.Metallic);
					surfaceData.occlusion = half(surfaceDescription.Occlusion);
					surfaceData.smoothness = half(surfaceDescription.Smoothness);
					surfaceData.MAOSAlpha = half(surfaceDescription.MAOSAlpha * fadeFactor);
				#endif
            }

            #if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_GBUFFER_PROJECTOR)
            #define DECAL_PROJECTOR
            #endif

            #if (SHADERPASS == SHADERPASS_DBUFFER_MESH) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_MESH) || (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_MESH) || (SHADERPASS == SHADERPASS_DECAL_GBUFFER_MESH)
            #define DECAL_MESH
            #endif

            #if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_DBUFFER_MESH)
            #define DECAL_DBUFFER
            #endif

            #if (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_MESH)
            #define DECAL_SCREEN_SPACE
            #endif

            #if (SHADERPASS == SHADERPASS_DECAL_GBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_GBUFFER_MESH)
            #define DECAL_GBUFFER
            #endif

            #if (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_PROJECTOR) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_MESH)
            #define DECAL_FORWARD_EMISSIVE
            #endif

            #if ((!defined(_MATERIAL_AFFECTS_NORMAL) && defined(_MATERIAL_AFFECTS_ALBEDO)) || (defined(_MATERIAL_AFFECTS_NORMAL) && defined(_MATERIAL_AFFECTS_NORMAL_BLEND))) && (defined(DECAL_SCREEN_SPACE) || defined(DECAL_GBUFFER))
            #define DECAL_RECONSTRUCT_NORMAL
            #elif defined(DECAL_ANGLE_FADE)
            #define DECAL_LOAD_NORMAL
            #endif

            #ifdef _DECAL_LAYERS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareRenderingLayerTexture.hlsl"
            #endif

            #if defined(DECAL_LOAD_NORMAL)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            #endif

            #if defined(DECAL_PROJECTOR) || defined(DECAL_RECONSTRUCT_NORMAL)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #endif

            #ifdef DECAL_MESH
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DecalMeshBiasTypeEnum.cs.hlsl"
            #endif

            #ifdef DECAL_RECONSTRUCT_NORMAL
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/NormalReconstruction.hlsl"
            #endif

			void MeshDecalsPositionZBias(inout PackedVaryings input)
			{
			#if UNITY_REVERSED_Z
				input.positionCS.z -= _DecalMeshDepthBias;
			#else
				input.positionCS.z += _DecalMeshDepthBias;
			#endif
			}

			PackedVaryings Vert(Attributes inputMesh /*ase_vert_input*/ )
			{
				if (_DecalMeshBiasType == DECALMESHDEPTHBIASTYPE_VIEW_BIAS)
				{
					float3 viewDirectionOS = GetObjectSpaceNormalizeViewDir(inputMesh.positionOS);
					inputMesh.positionOS += viewDirectionOS * (_DecalMeshViewBias);
				}

				PackedVaryings packedOutput;
				ZERO_INITIALIZE(PackedVaryings, packedOutput);

				UNITY_SETUP_INSTANCE_ID(inputMesh);
				UNITY_TRANSFER_INSTANCE_ID(inputMesh, packedOutput);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(packedOutput);

				/*ase_vert_code:inputMesh=Attributes;packedOutput=PackedVaryings*/

				VertexPositionInputs vertexInput = GetVertexPositionInputs(inputMesh.positionOS.xyz);

				float3 positionWS = TransformObjectToWorld(inputMesh.positionOS);
				float3 normalWS = TransformObjectToWorldNormal(inputMesh.normalOS);
				float4 tangentWS = float4(TransformObjectToWorldDir(inputMesh.tangentOS.xyz), inputMesh.tangentOS.w);

				packedOutput.positionCS = TransformWorldToHClip(positionWS);

				if (_DecalMeshBiasType == DECALMESHDEPTHBIASTYPE_DEPTH_BIAS)
				{
					MeshDecalsPositionZBias(packedOutput);
				}

				packedOutput.positionWS.xyz = positionWS;
				packedOutput.normalWS.xyz =  normalWS;
				packedOutput.tangentWS.xyzw =  tangentWS;
				packedOutput.texCoord0.xyzw =  inputMesh.uv0;

				return packedOutput;
			}

			void Frag(PackedVaryings packedInput,
				out half4 outEmissive : SV_Target0
				/*ase_frag_input*/
			)
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
				UNITY_SETUP_INSTANCE_ID(packedInput);

				half angleFadeFactor = 1.0;

            // Only screen space needs flip logic, other passes do not setup needed properties so we skip here
            #if defined(DECAL_SCREEN_SPACE)
				TransformScreenUV(packedInput.positionCS, _ScreenSize.y);
            #endif

            #ifdef _DECAL_LAYERS
            #ifdef _RENDER_PASS_ENABLED
				#if ( UNITY_VERSION >= 60020000 )
				uint surfaceRenderingLayer = LOAD_FRAMEBUFFER_X_INPUT(GBUFFER4, packedInput.positionCS.xy).r;
				#else
				uint surfaceRenderingLayer = DecodeMeshRenderingLayer(LOAD_FRAMEBUFFER_X_INPUT(GBUFFER4, packedInput.positionCS.xy).r);
				#endif
            #else
				uint surfaceRenderingLayer = LoadSceneRenderingLayer(packedInput.positionCS.xy);
            #endif
				#if ( UNITY_VERSION >= 60030000 )
				uint projectorRenderingLayer = asuint(UNITY_ACCESS_INSTANCED_PROP(Decal, _DecalLayerMaskFromDecal));
				#else
				uint projectorRenderingLayer = uint(UNITY_ACCESS_INSTANCED_PROP(Decal, _DecalLayerMaskFromDecal));
				#endif
				clip((surfaceRenderingLayer & projectorRenderingLayer) - 0.1);
            #endif

			#if defined(DECAL_PROJECTOR)
			#if UNITY_REVERSED_Z
			#if _RENDER_PASS_ENABLED
				float depth = LOAD_FRAMEBUFFER_X_INPUT(GBUFFER3, packedInput.positionCS.xy).x;
			#else
				float depth = LoadSceneDepth(packedInput.positionCS.xy);
			#endif
			#else
			#if _RENDER_PASS_ENABLED
				float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, LOAD_FRAMEBUFFER_X_INPUT(GBUFFER3, packedInput.positionCS.xy));
			#else
			    // Adjust z to match NDC for OpenGL
				float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, LoadSceneDepth(packedInput.positionCS.xy));
			#endif
			#endif
			#endif

			#if defined(DECAL_RECONSTRUCT_NORMAL)
				#if defined(_RENDER_PASS_ENABLED) && ( ( UNITY_VERSION >= 60000067 && UNITY_VERSION < 60010000 ) || ( UNITY_VERSION >= 60030004 ) )
					half3 normalWS = half3(ReconstructNormalDerivative(packedInput.positionCS.xy, LOAD_FRAMEBUFFER_X_INPUT(GBUFFER3, packedInput.positionCS.xy).x));
				#elif defined(_DECAL_NORMAL_BLEND_HIGH)
					half3 normalWS = half3(ReconstructNormalTap9(packedInput.positionCS.xy));
				#elif defined(_DECAL_NORMAL_BLEND_MEDIUM)
					half3 normalWS = half3(ReconstructNormalTap5(packedInput.positionCS.xy));
				#else
					half3 normalWS = half3(ReconstructNormalDerivative(packedInput.positionCS.xy));
				#endif
			#elif defined(DECAL_LOAD_NORMAL)
				#if defined(_RENDER_PASS_ENABLED) && ( ( UNITY_VERSION >= 60000067 && UNITY_VERSION < 60010000 ) || ( UNITY_VERSION >= 60030004 ) )
					half3 normalWS = normalize(LOAD_FRAMEBUFFER_X_INPUT(GBUFFER2, packedInput.positionCS.xy).rgb);
				#else
					half3 normalWS = half3(LoadSceneNormals(packedInput.positionCS.xy));
				#endif
			#endif

				float2 positionSS = FoveatedRemapNonUniformToLinearCS(packedInput.positionCS.xy) * _ScreenSize.zw;

				float3 positionWS = packedInput.positionWS.xyz;
				half3 viewDirectionWS = half3(1.0, 1.0, 1.0);

				DecalSurfaceData surfaceData;
				SurfaceDescription surfaceDescription = (SurfaceDescription)0;

				/*ase_frag_code:packedInput=PackedVaryings*/

				surfaceDescription.BaseColor = /*ase_frag_out:Base Color;Float3;0;-1;_BaseColor*/IsGammaSpace() ? float3(0.5, 0.5, 0.5) : SRGBToLinear(float3(0.5, 0.5, 0.5))/*end*/;
				surfaceDescription.Alpha = /*ase_frag_out:Alpha;Float;1;-1;_Alpha*/1/*end*/;
				surfaceDescription.NormalTS = /*ase_frag_out:Normal;Float3;2;-1;_NormalTS*/float3(0.0f, 0.0f, 1.0f)/*end*/;
				surfaceDescription.NormalAlpha = /*ase_frag_out:Normal Alpha;Float;3;-1;_NormalAlpha*/1/*end*/;

			#if defined( _MATERIAL_AFFECTS_MAOS )
				surfaceDescription.Metallic = /*ase_frag_out:Metallic;Float;4;-1;_Metallic*/0/*end*/;
				surfaceDescription.Occlusion = /*ase_frag_out:Occlusion;Float;5;-1;_Occlusion*/1/*end*/;
				surfaceDescription.Smoothness = /*ase_frag_out:Smoothness;Float;6;-1;_Smoothness*/0.5/*end*/;
				surfaceDescription.MAOSAlpha = /*ase_frag_out:MAOS Alpha;Float;7;-1;_MAOSAlpha*/1/*end*/;
			#endif

			#if defined( _MATERIAL_AFFECTS_EMISSION )
				surfaceDescription.Emission = /*ase_frag_out:Emission;Float3;8;-1;_Emission*/float3(0, 0, 0)/*end*/;
			#endif

				GetSurfaceData(surfaceDescription, packedInput.positionCS, surfaceData);

				#if ( UNITY_VERSION >= 60070000 )
				outEmissive.rgb = ClampExposed(GetPreExposureMultiplier() * surfaceData.emissive);
				#else
				outEmissive.rgb = surfaceData.emissive * GetCurrentExposureMultiplier();
				#endif
				outEmissive.a = surfaceData.baseColor.a;

			}
            ENDHLSL
        }

		/*ase_pass*/
        Pass
        {
            /*ase_hide_pass*/
			/*ase_immutable*/ Name "DecalScreenSpaceMesh"
            Tags
            {
                "LightMode" = "DecalScreenSpaceMesh"
            }

			Blend SrcAlpha OneMinusSrcAlpha
			ZTest LEqual
			ZWrite Off

			HLSLPROGRAM

			#pragma vertex Vert
			#pragma fragment Frag
			#pragma multi_compile_instancing
			#if ( UNITY_VERSION < 60010000 )
			#pragma multi_compile_fog
			#endif
			#pragma editor_sync_compilation

			#pragma multi_compile _ LIGHTMAP_ON
			#pragma multi_compile _ DYNAMICLIGHTMAP_ON
			#pragma multi_compile _ DIRLIGHTMAP_COMBINED
			#pragma multi_compile _ USE_LEGACY_LIGHTMAPS

			#if ( UNITY_VERSION < 60070000 ) && defined( DOTS_INSTANCING_ON ) && !defined( USE_LEGACY_LIGHTMAPS )
				// @diogo: URP passes its Texture2D dynamic lightmap to Texture2DArray lightmap params
				#undef DYNAMICLIGHTMAP_ON
			#endif

			#if ( UNITY_VERSION >= 60010000 )
			#pragma multi_compile _ LIGHTMAP_BICUBIC_SAMPLING
			#endif

			#if ( UNITY_VERSION >= 60030000 )
			#pragma multi_compile _ REFLECTION_PROBE_ROTATION
			#endif

			#if ( UNITY_VERSION >= 60070000 )
			#pragma multi_compile _ _EXPOSURE
			#endif

			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS

			#if ( UNITY_VERSION >= 60070000 )
			#pragma multi_compile _ _LIGHT_FALLOFF_QUADRATIC
			#endif

			#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
			#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
			#pragma multi_compile _ SHADOWS_SHADOWMASK

			#if ( UNITY_VERSION >= 60070000 )
			#pragma multi_compile_fragment _ _CLUSTER_LIGHT_LOOP
			#elif ( UNITY_VERSION >= 60010000 )
			#pragma multi_compile _ _CLUSTER_LIGHT_LOOP
			#else
			#pragma multi_compile _ _FORWARD_PLUS
			#endif

			#pragma multi_compile _DECAL_NORMAL_BLEND_LOW _DECAL_NORMAL_BLEND_MEDIUM _DECAL_NORMAL_BLEND_HIGH
			#pragma multi_compile_fragment _ DEBUG_DISPLAY
			#pragma multi_compile _ _DECAL_LAYERS

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_TEXCOORD1
            #define ATTRIBUTES_NEED_TEXCOORD2
            #define VARYINGS_NEED_POSITION_WS
            #define VARYINGS_NEED_NORMAL_WS
            #define VARYINGS_NEED_VIEWDIRECTION_WS
            #define VARYINGS_NEED_TANGENT_WS
            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
            #define VARYINGS_NEED_SH
            #define VARYINGS_NEED_STATIC_LIGHTMAP_UV
            #define VARYINGS_NEED_DYNAMIC_LIGHTMAP_UV

            #define HAVE_MESH_MODIFICATION

            #define SHADERPASS SHADERPASS_DECAL_SCREEN_SPACE_MESH

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#if ( UNITY_VERSION >= 60010000 )
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
			#endif
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DecalInput.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderVariablesDecal.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#if _RENDER_PASS_ENABLED
			#define GBUFFER3 0
			#define GBUFFER4 1
			#if ( UNITY_VERSION >= 60000067 && UNITY_VERSION < 60010000 ) || ( UNITY_VERSION >= 60030004 )
			FRAMEBUFFER_INPUT_X_FLOAT(GBUFFER3);
			#else
			FRAMEBUFFER_INPUT_X_HALF(GBUFFER3);
			#endif
			#if ( UNITY_VERSION >= 60020000 )
			FRAMEBUFFER_INPUT_X_UINT(GBUFFER4);
			#else
			FRAMEBUFFER_INPUT_X_HALF(GBUFFER4);
			#endif
			#endif

			/*ase_pragma*/

            struct SurfaceDescription
			{
				float3 BaseColor;
				float Alpha;
				float3 NormalTS;
				float NormalAlpha;
				float Metallic;
				float Occlusion;
				float Smoothness;
				float MAOSAlpha;
				float3 Emission;
			};

            struct Attributes
			{
				float3 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float4 tangentOS : TANGENT;
				float4 uv0 : TEXCOORD0;
				float4 uv1 : TEXCOORD1;
				float4 uv2 : TEXCOORD2;
				/*ase_vdata:p=p;n=n;t=t;uv0=tc0;uv1=tc1;uv2=tc2*/
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				float3 normalWS : TEXCOORD1;
				float4 tangentWS : TEXCOORD2;
				float4 texCoord0 : TEXCOORD3;
				float3 viewDirectionWS : TEXCOORD4;
				float4 lightmapUVs : TEXCOORD5; // @diogo: packs both static (xy) and dynamic (zw)
				float3 sh : TEXCOORD6;
				float4 fogFactorAndVertexLight : TEXCOORD7;
				#ifdef USE_APV_PROBE_OCCLUSION
					float4 probeOcclusion : TEXCOORD8;
				#endif
				/*ase_interp(9,):sp=sp;wp=tc0;wn=tc1;wt=tc2;uv0=tc3;wvd=tc4*/
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

            CBUFFER_START(UnityPerMaterial)
			float _DrawOrder;
			float _DecalMeshBiasType;
			float _DecalMeshDepthBias;
			float _DecalMeshViewBias;
			#if defined(DECAL_ANGLE_FADE)
			float _DecalAngleFadeSupported;
			#endif
			UNITY_TEXTURE_STREAMING_DEBUG_VARS;
			CBUFFER_END

            #ifdef SCENEPICKINGPASS
				float4 _SelectionID;
            #endif

            #ifdef SCENESELECTIONPASS
				int _ObjectId;
				int _PassValue;
            #endif

			/*ase_globals*/

			/*ase_funcs*/

            void GetSurfaceData(PackedVaryings input, SurfaceDescription surfaceDescription, out DecalSurfaceData surfaceData)
            {
                #if defined(LOD_FADE_CROSSFADE)
					LODFadeCrossFade( input.positionCS );
                #endif

                half fadeFactor = half(1.0);

                ZERO_INITIALIZE(DecalSurfaceData, surfaceData);
                surfaceData.occlusion = half(1.0);
                surfaceData.smoothness = half(0);

                #ifdef _MATERIAL_AFFECTS_NORMAL
                    surfaceData.normalWS.w = half(1.0);
                #else
                    surfaceData.normalWS.w = half(0.0);
                #endif

				#if defined( _MATERIAL_AFFECTS_EMISSION )
					surfaceData.emissive.rgb = half3(surfaceDescription.Emission.rgb * fadeFactor);
				#endif

                surfaceData.baseColor.xyz = half3(surfaceDescription.BaseColor);
                surfaceData.baseColor.w = half(surfaceDescription.Alpha * fadeFactor);

                #if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_GBUFFER_PROJECTOR)
                    #if defined(_MATERIAL_AFFECTS_NORMAL)
						surfaceData.normalWS.xyz = normalize(mul((half3x3)normalToWorld, surfaceDescription.NormalTS.xyz));
                    #else
						surfaceData.normalWS.xyz = normalize(normalToWorld[2].xyz);
                    #endif
                #elif (SHADERPASS == SHADERPASS_DBUFFER_MESH) || (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_MESH) || (SHADERPASS == SHADERPASS_DECAL_GBUFFER_MESH)
                    #if defined(_MATERIAL_AFFECTS_NORMAL)
                        float sgn = input.tangentWS.w;
                        float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
                        half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);
                        surfaceData.normalWS.xyz = normalize(TransformTangentToWorld(surfaceDescription.NormalTS, tangentToWorld));
                    #else
						surfaceData.normalWS.xyz = normalize(half3(input.normalWS));
                    #endif
                #endif

                surfaceData.normalWS.w = surfaceDescription.NormalAlpha * fadeFactor;

				#if defined( _MATERIAL_AFFECTS_MAOS )
					surfaceData.metallic = half(surfaceDescription.Metallic);
					surfaceData.occlusion = half(surfaceDescription.Occlusion);
					surfaceData.smoothness = half(surfaceDescription.Smoothness);
					surfaceData.MAOSAlpha = half(surfaceDescription.MAOSAlpha * fadeFactor);
				#endif
            }

            #if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_GBUFFER_PROJECTOR)
            #define DECAL_PROJECTOR
            #endif

            #if (SHADERPASS == SHADERPASS_DBUFFER_MESH) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_MESH) || (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_MESH) || (SHADERPASS == SHADERPASS_DECAL_GBUFFER_MESH)
            #define DECAL_MESH
            #endif

            #if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_DBUFFER_MESH)
            #define DECAL_DBUFFER
            #endif

            #if (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_MESH)
            #define DECAL_SCREEN_SPACE
            #endif

            #if (SHADERPASS == SHADERPASS_DECAL_GBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_GBUFFER_MESH)
            #define DECAL_GBUFFER
            #endif

            #if (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_PROJECTOR) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_MESH)
            #define DECAL_FORWARD_EMISSIVE
            #endif

            #if ((!defined(_MATERIAL_AFFECTS_NORMAL) && defined(_MATERIAL_AFFECTS_ALBEDO)) || (defined(_MATERIAL_AFFECTS_NORMAL) && defined(_MATERIAL_AFFECTS_NORMAL_BLEND))) && (defined(DECAL_SCREEN_SPACE) || defined(DECAL_GBUFFER))
            #define DECAL_RECONSTRUCT_NORMAL
            #elif defined(DECAL_ANGLE_FADE)
            #define DECAL_LOAD_NORMAL
            #endif

            #ifdef _DECAL_LAYERS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareRenderingLayerTexture.hlsl"
            #endif

            #if defined(DECAL_LOAD_NORMAL)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            #endif

            #if defined(DECAL_PROJECTOR) || defined(DECAL_RECONSTRUCT_NORMAL)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #endif

            #ifdef DECAL_MESH
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DecalMeshBiasTypeEnum.cs.hlsl"
            #endif

            #ifdef DECAL_RECONSTRUCT_NORMAL
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/NormalReconstruction.hlsl"
            #endif

			void MeshDecalsPositionZBias(inout PackedVaryings input)
			{
			#if UNITY_REVERSED_Z
				input.positionCS.z -= _DecalMeshDepthBias;
			#else
				input.positionCS.z += _DecalMeshDepthBias;
			#endif
			}

			void InitializeInputData(PackedVaryings input, float3 positionWS, half3 normalWS, half3 viewDirectionWS, out InputData inputData)
			{
				inputData = (InputData)0;

				inputData.positionWS = positionWS;
				inputData.normalWS = normalWS;
				inputData.viewDirectionWS = viewDirectionWS;
				#if ( UNITY_VERSION >= 60070000 )
					inputData.shadowCoord = MainLightShadowsAvailable() ? TransformWorldToShadowCoord(positionWS) : float4(0, 0, 0, 0);
				#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
					inputData.shadowCoord = TransformWorldToShadowCoord(positionWS);
				#else
					inputData.shadowCoord = float4(0, 0, 0, 0);
				#endif
				#if ( UNITY_VERSION >= 60070000 )
					inputData.preExposureMultiplier = GetPreExposureMultiplier();
				#endif

				#ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
					inputData.fogCoord = InitializeInputDataFog(float4(positionWS, 1.0), input.fogFactorAndVertexLight.x);
					inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
				#endif

				#if ( UNITY_VERSION >= 60070000 )
					GIParams giParams = (GIParams)0;
					#if defined(LIGHTMAP_ON)
						giParams.staticLightmapUV = input.lightmapUVs.xy;
					#else
						giParams.vertexSH = half3(input.sh);
					#endif
					#if defined(DYNAMICLIGHTMAP_ON)
						giParams.dynamicLightmapUV = input.lightmapUVs.zw;
					#endif
					#if defined(USE_APV_PROBE_OCCLUSION)
						giParams.vertexProbeOcclusion = input.probeOcclusion;
					#endif
					giParams.positionWS = inputData.positionWS;
					giParams.normalWS = inputData.normalWS;
					giParams.viewDirWS = inputData.viewDirectionWS;
					giParams.positionSS = input.positionCS.xy;
					InitializeBakedGI( giParams, inputData.bakedGI, inputData.shadowMask );
				#elif defined(_SCREEN_SPACE_IRRADIANCE) && ( UNITY_VERSION >= 60030000 )
					#if ( UNITY_VERSION >= 60060000 )
						inputData.bakedGI = SAMPLE_GI(_ScreenSpaceIrradiance, input.positionCS.xy, inputData.normalWS);
					#else
						inputData.bakedGI = SAMPLE_GI(_ScreenSpaceIrradiance, input.positionCS.xy);
					#endif
				#elif defined(VARYINGS_NEED_DYNAMIC_LIGHTMAP_UV) && defined(DYNAMICLIGHTMAP_ON)
					inputData.bakedGI = SAMPLE_GI(input.lightmapUVs.xy, input.lightmapUVs.zw, half3(input.sh), normalWS);
					#if defined(VARYINGS_NEED_STATIC_LIGHTMAP_UV)
					inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUVs.xy);
					#endif
				#elif defined(VARYINGS_NEED_STATIC_LIGHTMAP_UV)
				#if !defined(LIGHTMAP_ON) && (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
    				inputData.bakedGI = SAMPLE_GI(input.sh,
					GetAbsolutePositionWS(inputData.positionWS),
					inputData.normalWS,
					inputData.viewDirectionWS,
					input.positionCS.xy,
					input.probeOcclusion,
					inputData.shadowMask);
				#else
					inputData.bakedGI = SAMPLE_GI(input.lightmapUVs.xy, half3(input.sh), normalWS);
					#if defined(VARYINGS_NEED_STATIC_LIGHTMAP_UV)
					inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUVs.xy);
					#endif
				#endif
				#endif

				#if defined(DEBUG_DISPLAY)
					#if defined(VARYINGS_NEED_DYNAMIC_LIGHTMAP_UV) && defined(DYNAMICLIGHTMAP_ON)
						inputData.dynamicLightmapUV = input.lightmapUVs.zw;
					#endif
					#if defined(VARYINGS_NEED_STATIC_LIGHTMAP_UV) && defined(LIGHTMAP_ON)
						inputData.staticLightmapUV = input.lightmapUVs.xy;
					#elif defined(VARYINGS_NEED_SH)
						inputData.vertexSH = input.sh;
					#endif
					#if defined(USE_APV_PROBE_OCCLUSION)
						inputData.probeOcclusion = input.probeOcclusion;
					#endif
				#endif

				inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
			}

			void GetSurface(DecalSurfaceData decalSurfaceData, inout SurfaceData surfaceData)
			{
				surfaceData.albedo = decalSurfaceData.baseColor.rgb;
				surfaceData.metallic = saturate(decalSurfaceData.metallic);
				surfaceData.specular = 0;
				surfaceData.smoothness = saturate(decalSurfaceData.smoothness);
				surfaceData.occlusion = decalSurfaceData.occlusion;
				surfaceData.emission = decalSurfaceData.emissive;
				surfaceData.alpha = saturate(decalSurfaceData.baseColor.w);
				surfaceData.clearCoatMask = 0;
				surfaceData.clearCoatSmoothness = 1;
			}

			PackedVaryings Vert(Attributes inputMesh /*ase_vert_input*/ )
			{
				if (_DecalMeshBiasType == DECALMESHDEPTHBIASTYPE_VIEW_BIAS)
				{
					float3 viewDirectionOS = GetObjectSpaceNormalizeViewDir(inputMesh.positionOS);
					inputMesh.positionOS += viewDirectionOS * (_DecalMeshViewBias);
				}

				PackedVaryings packedOutput;
				ZERO_INITIALIZE(PackedVaryings, packedOutput);

				UNITY_SETUP_INSTANCE_ID(inputMesh);
				UNITY_TRANSFER_INSTANCE_ID(inputMesh, packedOutput);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(packedOutput);

				/*ase_vert_code:inputMesh=Attributes;packedOutput=PackedVaryings*/

				VertexPositionInputs vertexInput = GetVertexPositionInputs(inputMesh.positionOS.xyz);
				float3 positionWS = TransformObjectToWorld(inputMesh.positionOS);
				float3 normalWS = TransformObjectToWorldNormal(inputMesh.normalOS);
				float4 tangentWS = float4(TransformObjectToWorldDir(inputMesh.tangentOS.xyz), inputMesh.tangentOS.w);

				packedOutput.positionCS = TransformWorldToHClip(positionWS);

				half fogFactor = 0;
				#if !defined(_FOG_FRAGMENT)
					fogFactor = ComputeFogFactor(packedOutput.positionCS.z);
				#endif

				half3 vertexLight = VertexLighting(positionWS, normalWS);
				packedOutput.fogFactorAndVertexLight = half4(fogFactor, vertexLight);

				if (_DecalMeshBiasType == DECALMESHDEPTHBIASTYPE_DEPTH_BIAS)
				{
					MeshDecalsPositionZBias(packedOutput);
				}

				packedOutput.positionWS.xyz = positionWS;
				packedOutput.normalWS.xyz =  normalWS;
				packedOutput.tangentWS.xyzw =  tangentWS;
				packedOutput.texCoord0.xyzw =  inputMesh.uv0;
				packedOutput.viewDirectionWS.xyz =  GetWorldSpaceViewDir(positionWS);

				#if defined(LIGHTMAP_ON)
					OUTPUT_LIGHTMAP_UV(inputMesh.uv1, unity_LightmapST, packedOutput.lightmapUVs.xy);
				#endif

				#if defined(DYNAMICLIGHTMAP_ON)
					packedOutput.lightmapUVs.zw = inputMesh.uv2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
				#endif

				#if !defined(LIGHTMAP_ON)
					packedOutput.sh = float3(SampleSHVertex(half3(normalWS)));
				#endif

				return packedOutput;
			}

			void Frag(PackedVaryings packedInput,
						out half4 outColor : SV_Target0
						/*ase_frag_input*/
					)
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
				UNITY_SETUP_INSTANCE_ID(packedInput);

				half angleFadeFactor = 1.0;

            // Only screen space needs flip logic, other passes do not setup needed properties so we skip here
			// To-Do check DecalScreenSpaceMesh pass
            //#if defined(DECAL_SCREEN_SPACE)
				//TransformScreenUV(packedInput.positionCS, _ScreenSize.y);
            //#endif

            #ifdef _DECAL_LAYERS
            #ifdef _RENDER_PASS_ENABLED
				#if ( UNITY_VERSION >= 60020000 )
				uint surfaceRenderingLayer = LOAD_FRAMEBUFFER_X_INPUT(GBUFFER4, packedInput.positionCS.xy).r;
				#else
				uint surfaceRenderingLayer = DecodeMeshRenderingLayer(LOAD_FRAMEBUFFER_X_INPUT(GBUFFER4, packedInput.positionCS.xy).r);
				#endif
            #else
				uint surfaceRenderingLayer = LoadSceneRenderingLayer(packedInput.positionCS.xy);
            #endif
				#if ( UNITY_VERSION >= 60030000 )
				uint projectorRenderingLayer = asuint(UNITY_ACCESS_INSTANCED_PROP(Decal, _DecalLayerMaskFromDecal));
				#else
				uint projectorRenderingLayer = uint(UNITY_ACCESS_INSTANCED_PROP(Decal, _DecalLayerMaskFromDecal));
				#endif
				clip((surfaceRenderingLayer & projectorRenderingLayer) - 0.1);
            #endif

			#if defined(DECAL_PROJECTOR)
			#if UNITY_REVERSED_Z
			#if _RENDER_PASS_ENABLED
				float depth = LOAD_FRAMEBUFFER_X_INPUT(GBUFFER3, packedInput.positionCS.xy).x;
			#else
				float depth = LoadSceneDepth(packedInput.positionCS.xy);
			#endif
			#else
			#if _RENDER_PASS_ENABLED
				float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, LOAD_FRAMEBUFFER_X_INPUT(GBUFFER3, packedInput.positionCS.xy));
			#else
			    // Adjust z to match NDC for OpenGL
				float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, LoadSceneDepth(packedInput.positionCS.xy));
			#endif
			#endif
			#endif

			#if defined(DECAL_RECONSTRUCT_NORMAL)
				#if defined(_RENDER_PASS_ENABLED) && ( ( UNITY_VERSION >= 60000067 && UNITY_VERSION < 60010000 ) || ( UNITY_VERSION >= 60030004 ) )
					half3 normalWS = half3(ReconstructNormalDerivative(packedInput.positionCS.xy, LOAD_FRAMEBUFFER_X_INPUT(GBUFFER3, packedInput.positionCS.xy).x));
				#elif defined(_DECAL_NORMAL_BLEND_HIGH)
					half3 normalWS = half3(ReconstructNormalTap9(packedInput.positionCS.xy));
				#elif defined(_DECAL_NORMAL_BLEND_MEDIUM)
					half3 normalWS = half3(ReconstructNormalTap5(packedInput.positionCS.xy));
				#else
					half3 normalWS = half3(ReconstructNormalDerivative(packedInput.positionCS.xy));
				#endif
			#elif defined(DECAL_LOAD_NORMAL)
				#if defined(_RENDER_PASS_ENABLED) && ( ( UNITY_VERSION >= 60000067 && UNITY_VERSION < 60010000 ) || ( UNITY_VERSION >= 60030004 ) )
					half3 normalWS = normalize(LOAD_FRAMEBUFFER_X_INPUT(GBUFFER2, packedInput.positionCS.xy).rgb);
				#else
					half3 normalWS = half3(LoadSceneNormals(packedInput.positionCS.xy));
				#endif
			#endif

				float2 positionSS = FoveatedRemapNonUniformToLinearCS(packedInput.positionCS.xy) * _ScreenSize.zw;

				float3 positionWS = packedInput.positionWS.xyz;
				half3 viewDirectionWS = half3(packedInput.viewDirectionWS);

				DecalSurfaceData surfaceData;
				SurfaceDescription surfaceDescription = (SurfaceDescription)0;

				/*ase_frag_code:packedInput=PackedVaryings*/

				surfaceDescription.BaseColor = /*ase_frag_out:Base Color;Float3;0;-1;_BaseColor*/IsGammaSpace() ? float3(0.5, 0.5, 0.5) : SRGBToLinear(float3(0.5, 0.5, 0.5))/*end*/;
				surfaceDescription.Alpha = /*ase_frag_out:Alpha;Float;1;-1;_Alpha*/1/*end*/;
				surfaceDescription.NormalTS = /*ase_frag_out:Normal;Float3;2;-1;_NormalTS*/float3(0.0f, 0.0f, 1.0f)/*end*/;
				surfaceDescription.NormalAlpha = /*ase_frag_out:Normal Alpha;Float;3;-1;_NormalAlpha*/1/*end*/;

			#if defined( _MATERIAL_AFFECTS_MAOS )
				surfaceDescription.Metallic = /*ase_frag_out:Metallic;Float;4;-1;_Metallic*/0/*end*/;
				surfaceDescription.Occlusion = /*ase_frag_out:Occlusion;Float;5;-1;_Occlusion*/1/*end*/;
				surfaceDescription.Smoothness = /*ase_frag_out:Smoothness;Float;6;-1;_Smoothness*/0.5/*end*/;
				surfaceDescription.MAOSAlpha = /*ase_frag_out:MAOS Alpha;Float;7;-1;_MAOSAlpha*/1/*end*/;
			#endif

			#if defined( _MATERIAL_AFFECTS_EMISSION )
				surfaceDescription.Emission = /*ase_frag_out:Emission;Float3;8;-1;_Emission*/float3(0, 0, 0)/*end*/;
			#endif

				GetSurfaceData(packedInput, surfaceDescription, surfaceData);

				half3 normalToPack = surfaceData.normalWS.xyz;
			#ifdef DECAL_RECONSTRUCT_NORMAL
				surfaceData.normalWS.xyz = normalize(lerp(normalWS.xyz, surfaceData.normalWS.xyz, surfaceData.normalWS.w));
			#endif

				InputData inputData;
				InitializeInputData(packedInput, positionWS, surfaceData.normalWS.xyz, viewDirectionWS, inputData);

				SurfaceData surface = (SurfaceData)0;
				GetSurface(surfaceData, surface);

				ASE_LIGHT_ACCUM4 color = UniversalFragmentPBR(inputData, surface);
				color.rgb = MixFog(color.rgb, inputData.fogCoord);
				#if ( UNITY_VERSION >= 60070000 )
				color.rgb = ClampExposed(inputData.preExposureMultiplier * color.rgb);
				#endif
				outColor = color;

			}
            ENDHLSL
        }

		/*ase_pass*/
        Pass
        {
            /*ase_hide_pass*/
			/*ase_immutable*/ Name "DecalGBufferMesh"
            Tags
            {
                "LightMode" = "DecalGBufferMesh"
            }

			Blend 0 SrcAlpha OneMinusSrcAlpha
			Blend 1 SrcAlpha OneMinusSrcAlpha
			Blend 2 SrcAlpha OneMinusSrcAlpha
			Blend 3 SrcAlpha OneMinusSrcAlpha
			ZWrite Off
			ColorMask RGB
			ColorMask 0 1
			ColorMask RGB 2
			ColorMask RGB 3

			HLSLPROGRAM

			#pragma vertex Vert
			#pragma fragment Frag
			#pragma multi_compile_instancing
			#if ( UNITY_VERSION < 60010000 )
			#pragma multi_compile_fog
			#endif
			#pragma editor_sync_compilation

			#if ( UNITY_VERSION >= 60070000 )
			#pragma multi_compile _ _EXPOSURE
			#endif

			#pragma multi_compile _ LIGHTMAP_ON
			#pragma multi_compile _ DYNAMICLIGHTMAP_ON
			#pragma multi_compile _ DIRLIGHTMAP_COMBINED
			#pragma multi_compile _ USE_LEGACY_LIGHTMAPS

			#if ( UNITY_VERSION < 60070000 ) && defined( DOTS_INSTANCING_ON ) && !defined( USE_LEGACY_LIGHTMAPS )
				// @diogo: URP passes its Texture2D dynamic lightmap to Texture2DArray lightmap params
				#undef DYNAMICLIGHTMAP_ON
			#endif

			#if ( UNITY_VERSION >= 60010000 )
			#pragma multi_compile _ LIGHTMAP_BICUBIC_SAMPLING
			#endif

			#if ( UNITY_VERSION >= 60030000 )
			#pragma multi_compile _ REFLECTION_PROBE_ROTATION
			#endif

			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
			#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING

			#if ( UNITY_VERSION >= 60070000 )
			#pragma multi_compile _ SHADOWS_SHADOWMASK
			#else
			#pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
			#endif

			#pragma multi_compile _DECAL_NORMAL_BLEND_LOW _DECAL_NORMAL_BLEND_MEDIUM _DECAL_NORMAL_BLEND_HIGH
			#pragma multi_compile _ _DECAL_LAYERS
			#pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
			#pragma multi_compile_fragment _ _RENDER_PASS_ENABLED

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_TEXCOORD1
            #define ATTRIBUTES_NEED_TEXCOORD2
            #define VARYINGS_NEED_POSITION_WS
            #define VARYINGS_NEED_NORMAL_WS
            #define VARYINGS_NEED_VIEWDIRECTION_WS
            #define VARYINGS_NEED_TANGENT_WS
            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
            #define VARYINGS_NEED_SH
            #define VARYINGS_NEED_STATIC_LIGHTMAP_UV
            #define VARYINGS_NEED_DYNAMIC_LIGHTMAP_UV

            #define HAVE_MESH_MODIFICATION

            #define SHADERPASS SHADERPASS_DECAL_GBUFFER_MESH

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#if ( UNITY_VERSION >= 60010000 )
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
			#endif
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
			#if ( UNITY_VERSION >= 60010000 )
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GBufferOutput.hlsl"
			#else
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl"
			#endif
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DecalInput.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderVariablesDecal.hlsl"

			#if ( UNITY_VERSION >= 60040010 )
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GBufferOutputFormat.hlsl"
			#endif

			#if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#if _RENDER_PASS_ENABLED
			#define GBUFFER3 0
			#define GBUFFER4 1
			#if ( UNITY_VERSION >= 60000067 && UNITY_VERSION < 60010000 ) || ( UNITY_VERSION >= 60030004 )
			FRAMEBUFFER_INPUT_X_FLOAT(GBUFFER3);
			#else
			FRAMEBUFFER_INPUT_X_HALF(GBUFFER3);
			#endif
			#if ( UNITY_VERSION >= 60020000 )
			FRAMEBUFFER_INPUT_X_UINT(GBUFFER4);
			#else
			FRAMEBUFFER_INPUT_X_HALF(GBUFFER4);
			#endif
			#endif

			/*ase_pragma*/

			struct SurfaceDescription
			{
				float3 BaseColor;
				float Alpha;
				float3 NormalTS;
				float NormalAlpha;
				float Metallic;
				float Occlusion;
				float Smoothness;
				float MAOSAlpha;
				float3 Emission;
			};

            struct Attributes
			{
				float3 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float4 tangentOS : TANGENT;
				float4 uv0 : TEXCOORD0;
				float4 uv1 : TEXCOORD1;
				float4 uv2 : TEXCOORD2;
				/*ase_vdata:p=p;n=n;t=t;uv0=tc0;uv1=tc1;uv2=tc2*/
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				float3 normalWS : TEXCOORD1;
				float4 tangentWS : TEXCOORD2;
				float4 texCoord0 : TEXCOORD3;
				float3 viewDirectionWS : TEXCOORD4;
				float4 lightmapUVs : TEXCOORD5; // @diogo: packs both static (xy) and dynamic (zw)
				float3 sh : TEXCOORD6;
				float4 fogFactorAndVertexLight : TEXCOORD7;
				#ifdef USE_APV_PROBE_OCCLUSION
					float4 probeOcclusion : TEXCOORD10;
				#endif
				/*ase_interp(9,):sp=sp;wp=tc0;wn=tc1;wt=tc2;uv0=tc3;wvd=tc4*/
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

            CBUFFER_START(UnityPerMaterial)
			float _DrawOrder;
			float _DecalMeshBiasType;
			float _DecalMeshDepthBias;
			float _DecalMeshViewBias;
			#if defined(DECAL_ANGLE_FADE)
			float _DecalAngleFadeSupported;
			#endif
			UNITY_TEXTURE_STREAMING_DEBUG_VARS;
			CBUFFER_END

            #ifdef SCENEPICKINGPASS
				float4 _SelectionID;
            #endif

            #ifdef SCENESELECTIONPASS
				int _ObjectId;
				int _PassValue;
            #endif

			/*ase_globals*/

			/*ase_funcs*/

            void GetSurfaceData(PackedVaryings input, SurfaceDescription surfaceDescription, out DecalSurfaceData surfaceData)
            {
                #if defined(LOD_FADE_CROSSFADE)
					LODFadeCrossFade( input.positionCS );
                #endif

                half fadeFactor = half(1.0);

                ZERO_INITIALIZE(DecalSurfaceData, surfaceData);
                surfaceData.occlusion = half(1.0);
                surfaceData.smoothness = half(0);

                #ifdef _MATERIAL_AFFECTS_NORMAL
                    surfaceData.normalWS.w = half(1.0);
                #else
                    surfaceData.normalWS.w = half(0.0);
                #endif

				#if defined( _MATERIAL_AFFECTS_EMISSION )
					surfaceData.emissive.rgb = half3(surfaceDescription.Emission.rgb * fadeFactor);
				#endif

                surfaceData.baseColor.xyz = half3(surfaceDescription.BaseColor);
                surfaceData.baseColor.w = half(surfaceDescription.Alpha * fadeFactor);

                #if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_GBUFFER_PROJECTOR)
                    #if defined(_MATERIAL_AFFECTS_NORMAL)
						surfaceData.normalWS.xyz = normalize(mul((half3x3)normalToWorld, surfaceDescription.NormalTS.xyz));
                    #else
						surfaceData.normalWS.xyz = normalize(normalToWorld[2].xyz);
                    #endif
                #elif (SHADERPASS == SHADERPASS_DBUFFER_MESH) || (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_MESH) || (SHADERPASS == SHADERPASS_DECAL_GBUFFER_MESH)
                    #if defined(_MATERIAL_AFFECTS_NORMAL)
                        float sgn = input.tangentWS.w;
                        float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
                        half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);
                        surfaceData.normalWS.xyz = normalize(TransformTangentToWorld(surfaceDescription.NormalTS, tangentToWorld));
                    #else
						surfaceData.normalWS.xyz = normalize(half3(input.normalWS));
                    #endif
                #endif

                surfaceData.normalWS.w = surfaceDescription.NormalAlpha * fadeFactor;

				#if defined( _MATERIAL_AFFECTS_MAOS )
					surfaceData.metallic = half(surfaceDescription.Metallic);
					surfaceData.occlusion = half(surfaceDescription.Occlusion);
					surfaceData.smoothness = half(surfaceDescription.Smoothness);
					surfaceData.MAOSAlpha = half(surfaceDescription.MAOSAlpha * fadeFactor);
				#endif
            }

            #if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_GBUFFER_PROJECTOR)
            #define DECAL_PROJECTOR
            #endif

            #if (SHADERPASS == SHADERPASS_DBUFFER_MESH) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_MESH) || (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_MESH) || (SHADERPASS == SHADERPASS_DECAL_GBUFFER_MESH)
            #define DECAL_MESH
            #endif

            #if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_DBUFFER_MESH)
            #define DECAL_DBUFFER
            #endif

            #if (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_MESH)
            #define DECAL_SCREEN_SPACE
            #endif

            #if (SHADERPASS == SHADERPASS_DECAL_GBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_GBUFFER_MESH)
            #define DECAL_GBUFFER
            #endif

            #if (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_PROJECTOR) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_MESH)
            #define DECAL_FORWARD_EMISSIVE
            #endif

            #if ((!defined(_MATERIAL_AFFECTS_NORMAL) && defined(_MATERIAL_AFFECTS_ALBEDO)) || (defined(_MATERIAL_AFFECTS_NORMAL) && defined(_MATERIAL_AFFECTS_NORMAL_BLEND))) && (defined(DECAL_SCREEN_SPACE) || defined(DECAL_GBUFFER))
            #define DECAL_RECONSTRUCT_NORMAL
            #elif defined(DECAL_ANGLE_FADE)
            #define DECAL_LOAD_NORMAL
            #endif

            #ifdef _DECAL_LAYERS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareRenderingLayerTexture.hlsl"
            #endif

            #if defined(DECAL_LOAD_NORMAL)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            #endif

            #if defined(DECAL_PROJECTOR) || defined(DECAL_RECONSTRUCT_NORMAL)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #endif

            #ifdef DECAL_MESH
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DecalMeshBiasTypeEnum.cs.hlsl"
            #endif

            #ifdef DECAL_RECONSTRUCT_NORMAL
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/NormalReconstruction.hlsl"
            #endif

			void MeshDecalsPositionZBias(inout PackedVaryings input)
			{
			#if UNITY_REVERSED_Z
				input.positionCS.z -= _DecalMeshDepthBias;
			#else
				input.positionCS.z += _DecalMeshDepthBias;
			#endif
			}

			void InitializeInputData(PackedVaryings input, float3 positionWS, half3 normalWS, half3 viewDirectionWS, out InputData inputData)
			{
				inputData = (InputData)0;

				inputData.positionWS = positionWS;
				inputData.normalWS = normalWS;
				inputData.viewDirectionWS = viewDirectionWS;
				#if ( UNITY_VERSION >= 60070000 )
					inputData.shadowCoord = MainLightShadowsAvailable() ? TransformWorldToShadowCoord(positionWS) : float4(0, 0, 0, 0);
				#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
					inputData.shadowCoord = TransformWorldToShadowCoord(positionWS);
				#else
					inputData.shadowCoord = float4(0, 0, 0, 0);
				#endif
				#if ( UNITY_VERSION >= 60070000 )
					inputData.preExposureMultiplier = GetPreExposureMultiplier();
				#endif

				#ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
					inputData.fogCoord = InitializeInputDataFog(float4(positionWS, 1.0), input.fogFactorAndVertexLight.x);
					inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
				#endif

				#if ( UNITY_VERSION >= 60070000 )
					GIParams giParams = (GIParams)0;
					#if defined(LIGHTMAP_ON)
						giParams.staticLightmapUV = input.lightmapUVs.xy;
					#else
						giParams.vertexSH = half3(input.sh);
					#endif
					#if defined(DYNAMICLIGHTMAP_ON)
						giParams.dynamicLightmapUV = input.lightmapUVs.zw;
					#endif
					#if defined(USE_APV_PROBE_OCCLUSION)
						giParams.vertexProbeOcclusion = input.probeOcclusion;
					#endif
					giParams.positionWS = inputData.positionWS;
					giParams.normalWS = inputData.normalWS;
					giParams.viewDirWS = inputData.viewDirectionWS;
					giParams.positionSS = input.positionCS.xy;
					InitializeBakedGI( giParams, inputData.bakedGI, inputData.shadowMask );
				#elif defined(_SCREEN_SPACE_IRRADIANCE) && ( UNITY_VERSION >= 60030000 )
					#if ( UNITY_VERSION >= 60060000 )
						inputData.bakedGI = SAMPLE_GI(_ScreenSpaceIrradiance, input.positionCS.xy, inputData.normalWS);
					#else
						inputData.bakedGI = SAMPLE_GI(_ScreenSpaceIrradiance, input.positionCS.xy);
					#endif
				#elif defined(VARYINGS_NEED_DYNAMIC_LIGHTMAP_UV) && defined(DYNAMICLIGHTMAP_ON)
					inputData.bakedGI = SAMPLE_GI(input.lightmapUVs.xy, input.lightmapUVs.zw, half3(input.sh), normalWS);
					#if defined(VARYINGS_NEED_STATIC_LIGHTMAP_UV)
					inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUVs.xy);
					#endif
				#elif defined(VARYINGS_NEED_STATIC_LIGHTMAP_UV)
				#if !defined(LIGHTMAP_ON) && (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
    				inputData.bakedGI = SAMPLE_GI(input.sh,
					GetAbsolutePositionWS(inputData.positionWS),
					inputData.normalWS,
					inputData.viewDirectionWS,
					input.positionCS.xy,
					input.probeOcclusion,
					inputData.shadowMask);
				#else
					inputData.bakedGI = SAMPLE_GI(input.lightmapUVs.xy, half3(input.sh), normalWS);
					#if defined(VARYINGS_NEED_STATIC_LIGHTMAP_UV)
					inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUVs.xy);
					#endif
				#endif
				#endif

				#if defined(DEBUG_DISPLAY)
					#if defined(VARYINGS_NEED_DYNAMIC_LIGHTMAP_UV) && defined(DYNAMICLIGHTMAP_ON)
						inputData.dynamicLightmapUV = input.lightmapUVs.zw;
					#endif
					#if defined(VARYINGS_NEED_STATIC_LIGHTMAP_UV) && defined(LIGHTMAP_ON)
						inputData.staticLightmapUV = input.lightmapUVs.xy;
					#elif defined(VARYINGS_NEED_SH)
						inputData.vertexSH = input.sh;
					#endif
					#if defined(USE_APV_PROBE_OCCLUSION)
						inputData.probeOcclusion = input.probeOcclusion;
					#endif
				#endif

				inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
			}

			void GetSurface(DecalSurfaceData decalSurfaceData, inout SurfaceData surfaceData)
			{
				surfaceData.albedo = decalSurfaceData.baseColor.rgb;
				surfaceData.metallic = saturate(decalSurfaceData.metallic);
				surfaceData.specular = 0;
				surfaceData.smoothness = saturate(decalSurfaceData.smoothness);
				surfaceData.occlusion = decalSurfaceData.occlusion;
				surfaceData.emission = decalSurfaceData.emissive;
				surfaceData.alpha = saturate(decalSurfaceData.baseColor.w);
				surfaceData.clearCoatMask = 0;
				surfaceData.clearCoatSmoothness = 1;
			}

			PackedVaryings Vert(Attributes inputMesh /*ase_vert_input*/ )
			{
				if (_DecalMeshBiasType == DECALMESHDEPTHBIASTYPE_VIEW_BIAS)
				{
					float3 viewDirectionOS = GetObjectSpaceNormalizeViewDir(inputMesh.positionOS);
					inputMesh.positionOS += viewDirectionOS * (_DecalMeshViewBias);
				}

				PackedVaryings packedOutput;
				ZERO_INITIALIZE(PackedVaryings, packedOutput);

				UNITY_SETUP_INSTANCE_ID(inputMesh);
				UNITY_TRANSFER_INSTANCE_ID(inputMesh, packedOutput);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(packedOutput);

				/*ase_vert_code:inputMesh=Attributes;packedOutput=PackedVaryings*/

				VertexPositionInputs vertexInput = GetVertexPositionInputs(inputMesh.positionOS.xyz);

				float3 positionWS = TransformObjectToWorld(inputMesh.positionOS);
				float3 normalWS = TransformObjectToWorldNormal(inputMesh.normalOS);
				float4 tangentWS = float4(TransformObjectToWorldDir(inputMesh.tangentOS.xyz), inputMesh.tangentOS.w);

				packedOutput.positionCS = TransformWorldToHClip(positionWS);

				if (_DecalMeshBiasType == DECALMESHDEPTHBIASTYPE_DEPTH_BIAS)
				{
					MeshDecalsPositionZBias(packedOutput);
				}

				packedOutput.positionWS.xyz = positionWS;
				packedOutput.normalWS.xyz =  normalWS;
				packedOutput.tangentWS.xyzw =  tangentWS;
				packedOutput.texCoord0.xyzw =  inputMesh.uv0;
				packedOutput.viewDirectionWS.xyz =  GetWorldSpaceViewDir(positionWS);

				#if defined(LIGHTMAP_ON)
					OUTPUT_LIGHTMAP_UV(inputMesh.uv1, unity_LightmapST, packedOutput.lightmapUVs.xy);
				#endif

				#if defined(DYNAMICLIGHTMAP_ON)
					packedOutput.lightmapUVs.zw = inputMesh.uv2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
				#endif

				#if !defined(LIGHTMAP_ON)
					packedOutput.sh.xyz =  float3(SampleSHVertex(half3(normalWS)));
				#endif

				half fogFactor = 0;
				#if !defined(_FOG_FRAGMENT)
						fogFactor = ComputeFogFactor(packedOutput.positionCS.z);
				#endif

				half3 vertexLight = VertexLighting(positionWS, normalWS);
				packedOutput.fogFactorAndVertexLight = half4(fogFactor, vertexLight);

				return packedOutput;
			}

			void Frag(PackedVaryings packedInput,
				#if ( UNITY_VERSION >= 60010000 )
				out GBufferFragOutput fragmentOutput
				#else
				out FragmentOutput fragmentOutput
				#endif
				/*ase_frag_input*/ )
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
				UNITY_SETUP_INSTANCE_ID(packedInput);

				half angleFadeFactor = 1.0;

            // Only screen space needs flip logic, other passes do not setup needed properties so we skip here
            #if defined(DECAL_SCREEN_SPACE)
				TransformScreenUV(packedInput.positionCS, _ScreenSize.y);
            #endif

            #ifdef _DECAL_LAYERS
            #ifdef _RENDER_PASS_ENABLED
				#if ( UNITY_VERSION >= 60020000 )
				uint surfaceRenderingLayer = LOAD_FRAMEBUFFER_X_INPUT(GBUFFER4, packedInput.positionCS.xy).r;
				#else
				uint surfaceRenderingLayer = DecodeMeshRenderingLayer(LOAD_FRAMEBUFFER_X_INPUT(GBUFFER4, packedInput.positionCS.xy).r);
				#endif
            #else
				uint surfaceRenderingLayer = LoadSceneRenderingLayer(packedInput.positionCS.xy);
            #endif
				#if ( UNITY_VERSION >= 60030000 )
				uint projectorRenderingLayer = asuint(UNITY_ACCESS_INSTANCED_PROP(Decal, _DecalLayerMaskFromDecal));
				#else
				uint projectorRenderingLayer = uint(UNITY_ACCESS_INSTANCED_PROP(Decal, _DecalLayerMaskFromDecal));
				#endif
				clip((surfaceRenderingLayer & projectorRenderingLayer) - 0.1);
            #endif

			#if defined(DECAL_PROJECTOR)
			#if UNITY_REVERSED_Z
			#if _RENDER_PASS_ENABLED
				float depth = LOAD_FRAMEBUFFER_X_INPUT(GBUFFER3, packedInput.positionCS.xy).x;
			#else
				float depth = LoadSceneDepth(packedInput.positionCS.xy);
			#endif
			#else
			#if _RENDER_PASS_ENABLED
				float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, LOAD_FRAMEBUFFER_X_INPUT(GBUFFER3, packedInput.positionCS.xy));
			#else
			    // Adjust z to match NDC for OpenGL
				float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, LoadSceneDepth(packedInput.positionCS.xy));
			#endif
			#endif
			#endif

			#if defined(DECAL_RECONSTRUCT_NORMAL)
				#if defined(_RENDER_PASS_ENABLED) && ( ( UNITY_VERSION >= 60000067 && UNITY_VERSION < 60010000 ) || ( UNITY_VERSION >= 60030004 ) )
					half3 normalWS = half3(ReconstructNormalDerivative(packedInput.positionCS.xy, LOAD_FRAMEBUFFER_X_INPUT(GBUFFER3, packedInput.positionCS.xy).x));
				#elif defined(_DECAL_NORMAL_BLEND_HIGH)
					half3 normalWS = half3(ReconstructNormalTap9(packedInput.positionCS.xy));
				#elif defined(_DECAL_NORMAL_BLEND_MEDIUM)
					half3 normalWS = half3(ReconstructNormalTap5(packedInput.positionCS.xy));
				#else
					half3 normalWS = half3(ReconstructNormalDerivative(packedInput.positionCS.xy));
				#endif
			#elif defined(DECAL_LOAD_NORMAL)
				#if defined(_RENDER_PASS_ENABLED) && ( ( UNITY_VERSION >= 60000067 && UNITY_VERSION < 60010000 ) || ( UNITY_VERSION >= 60030004 ) )
					half3 normalWS = normalize(LOAD_FRAMEBUFFER_X_INPUT(GBUFFER2, packedInput.positionCS.xy).rgb);
				#else
					half3 normalWS = half3(LoadSceneNormals(packedInput.positionCS.xy));
				#endif
			#endif

				float2 positionSS = FoveatedRemapNonUniformToLinearCS(packedInput.positionCS.xy) * _ScreenSize.zw;

				float3 positionWS = packedInput.positionWS.xyz;
				half3 viewDirectionWS = half3(packedInput.viewDirectionWS);

				DecalSurfaceData surfaceData;
				SurfaceDescription surfaceDescription = (SurfaceDescription)0;

				/*ase_frag_code:packedInput=PackedVaryings*/

				surfaceDescription.BaseColor = /*ase_frag_out:Base Color;Float3;0;-1;_BaseColor*/IsGammaSpace() ? float3(0.5, 0.5, 0.5) : SRGBToLinear(float3(0.5, 0.5, 0.5))/*end*/;
				surfaceDescription.Alpha = /*ase_frag_out:Alpha;Float;1;-1;_Alpha*/1/*end*/;
				surfaceDescription.NormalTS = /*ase_frag_out:Normal;Float3;2;-1;_NormalTS*/float3(0.0f, 0.0f, 1.0f)/*end*/;
				surfaceDescription.NormalAlpha = /*ase_frag_out:Normal Alpha;Float;3;-1;_NormalAlpha*/1/*end*/;

			#if defined( _MATERIAL_AFFECTS_MAOS )
				surfaceDescription.Metallic = /*ase_frag_out:Metallic;Float;4;-1;_Metallic*/0/*end*/;
				surfaceDescription.Occlusion = /*ase_frag_out:Occlusion;Float;5;-1;_Occlusion*/1/*end*/;
				surfaceDescription.Smoothness = /*ase_frag_out:Smoothness;Float;6;-1;_Smoothness*/0.5/*end*/;
				surfaceDescription.MAOSAlpha = /*ase_frag_out:MAOS Alpha;Float;7;-1;_ColorP*/1/*end*/;
			#endif

			#if defined( _MATERIAL_AFFECTS_EMISSION )
				surfaceDescription.Emission = /*ase_frag_out:Emission;Float3;8;-1;_Emission*/float3(0, 0, 0)/*end*/;
			#endif

				GetSurfaceData(packedInput, surfaceDescription, surfaceData);

				half3 normalToPack = surfaceData.normalWS.xyz;
			#ifdef DECAL_RECONSTRUCT_NORMAL
				surfaceData.normalWS.xyz = normalize(lerp(normalWS.xyz, surfaceData.normalWS.xyz, surfaceData.normalWS.w));
			#endif

				InputData inputData;
				InitializeInputData(packedInput, positionWS, surfaceData.normalWS.xyz, viewDirectionWS, inputData);

				SurfaceData surface = (SurfaceData)0;
				GetSurface(surfaceData, surface);

				BRDFData brdfData;
				InitializeBRDFData(surface.albedo, surface.metallic, 0, surface.smoothness, surface.alpha, brdfData);

			#ifdef _MATERIAL_AFFECTS_ALBEDO
				Light mainLight = GetMainLight(inputData.shadowCoord, inputData.positionWS, inputData.shadowMask);
				MixRealtimeAndBakedGI(mainLight, surfaceData.normalWS.xyz, inputData.bakedGI, inputData.shadowMask);
				ASE_LIGHT_ACCUM3 color = GlobalIllumination(brdfData, inputData.bakedGI, surface.occlusion, surfaceData.normalWS.xyz, inputData.viewDirectionWS);
			#else
				ASE_LIGHT_ACCUM3 color = 0;
			#endif

				// ShaderPassDecal.hlsl
				// We can not use usual GBuffer functions (etc. BRDFDataToGbuffer) as we use alpha for blending
				#pragma warning (disable : 3578) // The output value isn't completely initialized.

			#if ( UNITY_VERSION >= 60010000 )
				half3 packedNormalWS = PackGBufferNormal(normalToPack);
				fragmentOutput.gBuffer0 = half4(surfaceData.baseColor.rgb, surfaceData.baseColor.a);
				fragmentOutput.gBuffer1 = 0;
				fragmentOutput.gBuffer2 = half4(packedNormalWS, surfaceData.normalWS.a);
				#if ( UNITY_VERSION >= 60070000 )
				fragmentOutput.color = half4(ClampExposed(inputData.preExposureMultiplier * (surfaceData.emissive + color)), surfaceData.baseColor.a);
				#else
				fragmentOutput.color = half4(surfaceData.emissive + color, surfaceData.baseColor.a);
				#endif
				#if defined(GBUFFER_FEATURE_SHADOWMASK)
					fragmentOutput.shadowMask = inputData.shadowMask;
				#endif
			#else
				half3 packedNormalWS = PackNormal(normalToPack);
				fragmentOutput.GBuffer0 = half4(surfaceData.baseColor.rgb, surfaceData.baseColor.a);
				fragmentOutput.GBuffer1 = 0;
				fragmentOutput.GBuffer2 = half4(packedNormalWS, surfaceData.normalWS.a);
				fragmentOutput.GBuffer3 = half4(surfaceData.emissive + color, surfaceData.baseColor.a);
				#if OUTPUT_SHADOWMASK
					fragmentOutput.GBuffer4 = inputData.shadowMask; // will have unity_ProbesOcclusion value if subtractive lighting is used (baked)
				#endif
			#endif

			#pragma warning (default : 3578) // Restore output value isn't completely initialized.

			#if defined(DECAL_FORWARD_EMISSIVE)
				// Emissive need to be pre-exposed
				#if ( UNITY_VERSION >= 60070000 )
				outEmissive.rgb = ClampExposed(GetPreExposureMultiplier() * surfaceData.emissive);
				#else
				outEmissive.rgb = surfaceData.emissive * GetCurrentExposureMultiplier();
				#endif
				outEmissive.a = surfaceData.baseColor.a;
			#endif

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

            Cull Back

			HLSLPROGRAM

			#pragma multi_compile_instancing
			#pragma editor_sync_compilation
			#pragma vertex Vert
			#pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define HAVE_MESH_MODIFICATION

            #define SHADERPASS SHADERPASS_DEPTHONLY
			#define SCENEPICKINGPASS 1

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DecalInput.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderVariablesDecal.hlsl"

			#if _RENDER_PASS_ENABLED
			#define GBUFFER3 0
			#define GBUFFER4 1
			#if ( UNITY_VERSION >= 60000067 && UNITY_VERSION < 60010000 ) || ( UNITY_VERSION >= 60030004 )
			FRAMEBUFFER_INPUT_X_FLOAT(GBUFFER3);
			#else
			FRAMEBUFFER_INPUT_X_HALF(GBUFFER3);
			#endif
			#if ( UNITY_VERSION >= 60020000 )
			FRAMEBUFFER_INPUT_X_UINT(GBUFFER4);
			#else
			FRAMEBUFFER_INPUT_X_HALF(GBUFFER4);
			#endif
			#endif

			/*ase_pragma*/

			struct Attributes
			{
				float3 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float4 tangentOS : TANGENT;
				/*ase_vdata:p=p;n=n;t=t*/
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				float4 positionCS : SV_POSITION;
				/*ase_interp(0,):sp=sp*/
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

            CBUFFER_START(UnityPerMaterial)
			float _DrawOrder;
			float _DecalMeshBiasType;
			float _DecalMeshDepthBias;
			float _DecalMeshViewBias;
			#if defined(DECAL_ANGLE_FADE)
			float _DecalAngleFadeSupported;
			#endif
			UNITY_TEXTURE_STREAMING_DEBUG_VARS;
			CBUFFER_END

            #ifdef SCENEPICKINGPASS
				float4 _SelectionID;
            #endif

            #ifdef SCENESELECTIONPASS
				int _ObjectId;
				int _PassValue;
            #endif

			/*ase_globals*/

			/*ase_funcs*/

            #if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_GBUFFER_PROJECTOR)
            #define DECAL_PROJECTOR
            #endif

            #if (SHADERPASS == SHADERPASS_DBUFFER_MESH) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_MESH) || (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_MESH) || (SHADERPASS == SHADERPASS_DECAL_GBUFFER_MESH)
            #define DECAL_MESH
            #endif

            #if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_DBUFFER_MESH)
            #define DECAL_DBUFFER
            #endif

            #if (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_MESH)
            #define DECAL_SCREEN_SPACE
            #endif

            #if (SHADERPASS == SHADERPASS_DECAL_GBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_GBUFFER_MESH)
            #define DECAL_GBUFFER
            #endif

            #if (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_PROJECTOR) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_MESH)
            #define DECAL_FORWARD_EMISSIVE
            #endif

            #if ((!defined(_MATERIAL_AFFECTS_NORMAL) && defined(_MATERIAL_AFFECTS_ALBEDO)) || (defined(_MATERIAL_AFFECTS_NORMAL) && defined(_MATERIAL_AFFECTS_NORMAL_BLEND))) && (defined(DECAL_SCREEN_SPACE) || defined(DECAL_GBUFFER))
            #define DECAL_RECONSTRUCT_NORMAL
            #elif defined(DECAL_ANGLE_FADE)
            #define DECAL_LOAD_NORMAL
            #endif

            #ifdef _DECAL_LAYERS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareRenderingLayerTexture.hlsl"
            #endif

            #if defined(DECAL_LOAD_NORMAL)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            #endif

            #if defined(DECAL_PROJECTOR) || defined(DECAL_RECONSTRUCT_NORMAL)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #endif

            #ifdef DECAL_MESH
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DecalMeshBiasTypeEnum.cs.hlsl"
            #endif

            #ifdef DECAL_RECONSTRUCT_NORMAL
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/NormalReconstruction.hlsl"
            #endif

			PackedVaryings Vert(Attributes inputMesh /*ase_vert_input*/ )
			{
				PackedVaryings packedOutput;
				ZERO_INITIALIZE(PackedVaryings, packedOutput);

				UNITY_SETUP_INSTANCE_ID(inputMesh);
				UNITY_TRANSFER_INSTANCE_ID(inputMesh, packedOutput);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(packedOutput);

				inputMesh.tangentOS = float4( 1, 0, 0, -1 );
				inputMesh.normalOS = float3( 0, 1, 0 );

				/*ase_vert_code:inputMesh=Attributes;packedOutput=PackedVaryings*/

				float3 positionWS = TransformObjectToWorld(inputMesh.positionOS);
				packedOutput.positionCS = TransformWorldToHClip(positionWS);

				return packedOutput;
			}

			void Frag(PackedVaryings packedInput,
				out float4 outColor : SV_Target0
				/*ase_frag_input*/
			)
			{
				UNITY_SETUP_INSTANCE_ID(packedInput);
				/*ase_frag_code:packedInput=PackedVaryings*/

				float3 BaseColor = /*ase_frag_out:Base Color;Float3;0;-1;_BaseColor*/IsGammaSpace() ? float3(0.5, 0.5, 0.5) : SRGBToLinear(float3(0.5, 0.5, 0.5))/*end*/;

				outColor = unity_SelectionID;
			}
			ENDHLSL
        }
    }
	/*ase_lod*/

	/*ase_unity_cond_begin:<=10000000*/
	SubShader { Pass { } } // Prevent "shader is not supported on this GPU"; will be excluded from generated shaders.
	/*ase_unity_cond_end*/

	CustomEditor "UnityEditor.Rendering.Universal.DecalShaderGraphGUI"
    FallBack "Hidden/Shader Graph/FallbackError"
}

// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using UnityEditor;

using System;
using System.Collections.Generic;

namespace AmplifyShaderEditor
{
	[Serializable]
	public class TerrainDrawInstancedHelper
	{
		private readonly string[] InstancedPragmas =
		{
			"multi_compile_instancing",
			"instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap forwardadd"
		};

		private readonly string[] InstancedPragmasSRP =
		{
			"multi_compile_instancing",
			"instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap"
		};

		private readonly string[] InstancedGlobalsSRP =
		{
			"#ifdef UNITY_INSTANCING_ENABLED//ASE Terrain Instancing",
			"\tTEXTURE2D(_TerrainHeightmapTexture);//ASE Terrain Instancing",
			"\tTEXTURE2D( _TerrainNormalmapTexture);//ASE Terrain Instancing",
			"\tSAMPLER(sampler_TerrainNormalmapTexture);//ASE Terrain Instancing",
			"#endif//ASE Terrain Instancing",
			"UNITY_INSTANCING_BUFFER_START( Terrain )//ASE Terrain Instancing",
			"\tUNITY_DEFINE_INSTANCED_PROP( float4, _TerrainPatchInstanceData )//ASE Terrain Instancing",
			"UNITY_INSTANCING_BUFFER_END( Terrain)//ASE Terrain Instancing",
			"CBUFFER_START( UnityTerrain)//ASE Terrain Instancing",
			"\t#ifdef UNITY_INSTANCING_ENABLED//ASE Terrain Instancing",
			"\t\tfloat4 _TerrainHeightmapRecipSize;//ASE Terrain Instancing",
			"\t\tfloat4 _TerrainHeightmapScale;//ASE Terrain Instancing",
			"\t#endif//ASE Terrain Instancing",
			"CBUFFER_END//ASE Terrain Instancing"
		};

		private readonly string[] InstancedGlobalsDefault =
		{
			"#ifdef UNITY_INSTANCING_ENABLED//ASE Terrain Instancing",
			"\tsampler2D _TerrainHeightmapTexture;//ASE Terrain Instancing",
			"\tsampler2D _TerrainNormalmapTexture;//ASE Terrain Instancing",
			"#endif//ASE Terrain Instancing",
			"UNITY_INSTANCING_BUFFER_START( Terrain )//ASE Terrain Instancing",
			"\tUNITY_DEFINE_INSTANCED_PROP( float4, _TerrainPatchInstanceData )//ASE Terrain Instancing",
			"UNITY_INSTANCING_BUFFER_END( Terrain)//ASE Terrain Instancing",
			"CBUFFER_START( UnityTerrain)//ASE Terrain Instancing",
			"\t#ifdef UNITY_INSTANCING_ENABLED//ASE Terrain Instancing",
			"\t\tfloat4 _TerrainHeightmapRecipSize;//ASE Terrain Instancing",
			"\t\tfloat4 _TerrainHeightmapScale;//ASE Terrain Instancing",
			"\t#endif//ASE Terrain Instancing",
			"CBUFFER_END//ASE Terrain Instancing"
		};

		private readonly string TerrainApplyMeshModificationInstruction =
			"#if defined( ASE_INSTANCED_TERRAIN ) && !defined( ASE_TESSELLATION )\n" +
			"\tTerrainApplyMeshModification( {0}.xyz, {1}, {2} );\n" +
			"#endif\n";

		private readonly string TerrainApplyMeshModificationInstructionVControl =
			"#if defined( ASE_INSTANCED_TERRAIN )\n" +
			"\tTerrainApplyMeshModification( {0}.xyz, {1}, {2} );\n" +
			"#endif\n";

		private readonly string[] TerrainApplyMeshModificationFunction =
		{
			"void TerrainApplyMeshModification( inout float3 position, inout half3 normal, inout float4 texcoord )\n",
			"{\n",
			"#ifdef UNITY_INSTANCING_ENABLED\n",
			"\tfloat2 patchVertex = position.xy;\n",
			"\tfloat4 instanceData = UNITY_ACCESS_INSTANCED_PROP( Terrain, _TerrainPatchInstanceData );\n",
			"\tfloat4 uvscale = instanceData.z * _TerrainHeightmapRecipSize;\n",
			"\tfloat4 uvoffset = instanceData.xyxy * uvscale;\n",
			"\tuvoffset.xy += 0.5f * _TerrainHeightmapRecipSize.xy;\n",
			"\tfloat2 sampleCoords = (patchVertex.xy * uvscale.xy + uvoffset.xy);\n",
			"\ttexcoord.xyzw = float4(patchVertex.xy * uvscale.zw + uvoffset.zw, 0, 0);\n",
			"\tfloat height = UnpackHeightmap( tex2Dlod( _TerrainHeightmapTexture, float4(sampleCoords, 0, 0) ) );\n",
			"\tposition.xz = (patchVertex.xy + instanceData.xy) * _TerrainHeightmapScale.xz * instanceData.z;\n",
			"\tposition.y = height * _TerrainHeightmapScale.y;\n",
			"\tnormal = tex2Dlod( _TerrainNormalmapTexture, texcoord.xyzw ).rgb * 2 - 1;\n",
			"#endif\n",
			"}\n"
		};

		private readonly string[] TerrainApplyMeshModificationFunctionSRP =
		{
			"void TerrainApplyMeshModification( inout float3 position, inout half3 normal, inout float4 texcoord )\n",
			"{\n",
			"#ifdef UNITY_INSTANCING_ENABLED\n",
			"\tfloat2 patchVertex = position.xy;\n",
			"\tfloat4 instanceData = UNITY_ACCESS_INSTANCED_PROP( Terrain, _TerrainPatchInstanceData );\n",
			"\tfloat2 sampleCoords = ( patchVertex.xy + instanceData.xy ) * instanceData.z;\n",
			"\tfloat height = UnpackHeightmap( _TerrainHeightmapTexture.Load( int3( sampleCoords, 0 ) ) );\n",
			"\tposition.xz = sampleCoords* _TerrainHeightmapScale.xz;\n",
			"\tposition.y = height* _TerrainHeightmapScale.y;\n",
			"\t#ifdef ENABLE_TERRAIN_PERPIXEL_NORMAL\n",
			"\t\tnormal = float3(0, 1, 0);\n",
			"\t#else\n",
			"\t\tnormal = _TerrainNormalmapTexture.Load(int3(sampleCoords, 0)).rgb* 2 - 1;\n",
			"\t#endif\n",
			"\ttexcoord.xy = sampleCoords* _TerrainHeightmapRecipSize.zw;\n",
			"#endif\n",
			"}\n"
		};

		private readonly string TerrainApplyMeshModificationInstructionStandard = "TerrainApplyMeshModification({0});";
		private readonly string[] TerrainApplyMeshModificationFunctionStandard =
		{
			"void TerrainApplyMeshModification( inout {0} v )",
			"#if defined(UNITY_INSTANCING_ENABLED) && !defined(SHADER_API_D3D11_9X)",
			"\tfloat2 patchVertex = v.vertex.xy;",
			"\tfloat4 instanceData = UNITY_ACCESS_INSTANCED_PROP(Terrain, _TerrainPatchInstanceData);",
			"\t",
			"\tfloat4 uvscale = instanceData.z * _TerrainHeightmapRecipSize;",
			"\tfloat4 uvoffset = instanceData.xyxy * uvscale;",
			"\tuvoffset.xy += 0.5f * _TerrainHeightmapRecipSize.xy;",
			"\tfloat2 sampleCoords = (patchVertex.xy * uvscale.xy + uvoffset.xy);",
			"\t",
			"\tfloat hm = UnpackHeightmap(tex2Dlod(_TerrainHeightmapTexture, float4(sampleCoords, 0, 0)));",
			"\tv.vertex.xz = (patchVertex.xy + instanceData.xy) * _TerrainHeightmapScale.xz * instanceData.z;",
			"\tv.vertex.y = hm * _TerrainHeightmapScale.y;",
			"\tv.vertex.w = 1.0f;",
			"\t",
			"\tv.texcoord.xy = (patchVertex.xy * uvscale.zw + uvoffset.zw);",
			"\tv.texcoord3 = v.texcoord2 = v.texcoord1 = v.texcoord;",
			"\t",
			"\t#ifdef TERRAIN_INSTANCED_PERPIXEL_NORMAL",
			"\t\tv.normal = float3(0, 1, 0);",
			"\t\t//data.tc.zw = sampleCoords;",
			"\t#else",
			"\t\tfloat3 nor = tex2Dlod(_TerrainNormalmapTexture, float4(sampleCoords, 0, 0)).xyz;",
			"\t\tv.normal = 2.0f * nor - 1.0f;",
			"\t#endif",
			"#endif",
		};

		private const string TerrainPickingPass = "Hidden/Nature/Terrain/Utilities/PICKING";
		private const string TerrainSelectionPass = "Hidden/Nature/Terrain/Utilities/SELECTION";

		private readonly string[] AdditionalUsePasses = { TerrainPickingPass, TerrainSelectionPass };
		private readonly string DrawInstancedLabel = "Instanced Terrain";

		[SerializeField]
		private bool m_enable = false;

		public void Draw( UndoParentNode owner )
		{
			m_enable = owner.EditorGUILayoutToggle( DrawInstancedLabel, m_enable );
		}

		public string GenerateVControlShaderCode( string position, string normal, string texcoord )
		{
			return string.Format( TerrainApplyMeshModificationInstructionVControl, position, normal, texcoord );
		}

		public void UpdateDataCollectorForTemplates( ref MasterNodeDataCollector dataCollector, ref List<string> vertexInstructions )
		{
			if( m_enable )
			{
				dataCollector.AddToDefines( -1, "ASE_INSTANCED_TERRAIN" );

				var multiPassMasterNode = dataCollector.MasterNode as TemplateMultiPassMasterNode;
				if ( multiPassMasterNode != null && multiPassMasterNode.IsMainOutputNode )
				{
					// @diogo: add selection/picking passes ONLY if they don't already exist in the template
					var pickingPass = multiPassMasterNode.ContainerGraph.GetPassWithTag( multiPassMasterNode.LODIndex, "LightMode", "Picking" );
					var selectionPass = multiPassMasterNode.ContainerGraph.GetPassWithTag( multiPassMasterNode.LODIndex, "LightMode", "SceneSelectionPass" );

					if ( pickingPass != null && !multiPassMasterNode.PassSelector.IsVisible( pickingPass.PassIdx ) )
					{
						dataCollector.AddUsePass( TerrainPickingPass, false );
					}

					if ( selectionPass != null && !multiPassMasterNode.PassSelector.IsVisible( selectionPass.PassIdx ) )
					{
						dataCollector.AddUsePass( TerrainSelectionPass, false );
					}
				}

				string[] instancedPragmas = dataCollector.IsSRP ? InstancedPragmasSRP : InstancedPragmas;
				for( int i = 0; i < instancedPragmas.Length; i++ )
				{
					dataCollector.AddToPragmas( -1, instancedPragmas[ i ] );
				}

				TemplateFunctionData functionData = dataCollector.TemplateDataCollectorInstance.CurrentTemplateData.VertexFunctionData;

				string position = dataCollector.TemplateDataCollectorInstance.GetVertexPosition( WirePortDataType.OBJECT, PrecisionType.Float, false, MasterNodePortCategory.Vertex );
				string normal = dataCollector.TemplateDataCollectorInstance.GetVertexNormal( PrecisionType.Float, false, MasterNodePortCategory.Vertex );
				string texcoord = dataCollector.TemplateDataCollectorInstance.GetUV( 0, MasterNodePortCategory.Vertex );



				if( dataCollector.IsSRP )
				{
					dataCollector.AddFunction( TerrainApplyMeshModificationFunctionSRP[ 0 ], TerrainApplyMeshModificationFunctionSRP, false );

					for( int i = 0; i < InstancedGlobalsSRP.Length; i++ )
					{
						dataCollector.AddToUniforms( -1, InstancedGlobalsSRP[ i ] );
					}
				}
				else
				{
					dataCollector.AddFunction( TerrainApplyMeshModificationFunction[ 0 ], TerrainApplyMeshModificationFunction, false );

					for( int i = 0; i < InstancedGlobalsDefault.Length; i++ )
					{
						dataCollector.AddToUniforms( -1, InstancedGlobalsDefault[ i ] );
					}
				}

				string vertexVarName = dataCollector.TemplateDataCollectorInstance.CurrentTemplateData.VertexFunctionData.InVarName;
				vertexInstructions.Insert( 0, string.Format( TerrainApplyMeshModificationInstruction, position, normal, texcoord ) );
			}
		}

		public void UpdateDataCollectorForStandard( ref MasterNodeDataCollector dataCollector )
		{
			if( m_enable )
			{
				dataCollector.AddToDefines( -1, "ASE_INSTANCED_TERRAIN" );

				for( int i = 0; i < AdditionalUsePasses.Length; i++ )
				{
					dataCollector.AddUsePass( AdditionalUsePasses[ i ], false );
				}

				for( int i = 0; i < InstancedPragmas.Length; i++ )
				{
					dataCollector.AddToPragmas( -1, InstancedPragmas[ i ] );
				}
				string functionBody = string.Empty;

				string functionHeader = string.Format( TerrainApplyMeshModificationFunctionStandard[ 0 ], dataCollector.SurfaceVertexStructure );
				IOUtils.AddFunctionHeader( ref functionBody, functionHeader );
				for( int i = 1; i < TerrainApplyMeshModificationFunctionStandard.Length; i++ )
				{
					IOUtils.AddFunctionLine( ref functionBody, TerrainApplyMeshModificationFunctionStandard[ i ] );
				}
				IOUtils.CloseFunctionBody( ref functionBody );

				dataCollector.AddFunction( functionHeader, functionBody );
				for( int i = 0; i < InstancedGlobalsDefault.Length; i++ )
				{
					dataCollector.AddToUniforms( -1, InstancedGlobalsDefault[ i ] );
				}

				dataCollector.AddVertexInstruction( string.Format( TerrainApplyMeshModificationInstructionStandard, "v" ) );
			}
		}

		public void ReadFromString( ref uint index, ref string[] nodeParams )
		{
			m_enable = Convert.ToBoolean( nodeParams[ index++ ] );
		}

		public void WriteToString( ref string nodeInfo )
		{
			IOUtils.AddFieldValueToString( ref nodeInfo, m_enable );
		}

		public bool Enabled { get { return m_enable; } }
	}
}

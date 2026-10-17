// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEngine;
using UnityEditor;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Terrain Properties", "Miscellaneous", "Provides access to properties of the actively rendered Terrain. Only available on URP/HDRP 6000.4 ( 17.4 ) and above.\n\nMax Local Height: maximum local height stored in the Terrain heightmap ( not normalized )\nBasemap Distance: distance set in the Terrain's settings\nLayers Count: number of Terrain Layers assigned to the Terrain" )]
	public sealed class TerrainPropertiesNode : ParentNode
	{
		private const int TerrainPropertiesMinVersion = 170400;

		// URP TerrainLitInput.hlsl declares all three inside CBUFFER_START( _Terrain ); they must live in that named
		// cbuffer to bind ( as loose globals they would land in $Globals and not receive the terrain renderer's values ).
		// Guarded ( check only, never #define'd here ) so that if a Terrain Lit template includes the real header first,
		// it defines the macro and this fallback block is skipped instead of duplicating the cbuffer.
		private readonly string[] URPTerrainUniforms =
		{
			"#ifndef UNIVERSAL_TERRAIN_LIT_INPUT_INCLUDED",
			"CBUFFER_START( _Terrain )",
			"float4 _TerrainHeightmapScale;",
			"float _TerrainBasemapDistance;",
			"half _NumLayersCount;",
			"CBUFFER_END",
			"#endif",
		};

		// HDRP scatters the three: _TerrainHeightmapScale in TerrainLitData.hlsl's CBUFFER_START( UnityTerrain ),
		// _TerrainBasemapDistance in TerrainLit_Splatmap.hlsl and _NumLayersCount in TerrainLit_Splatmap_Includes.hlsl,
		// the latter two as loose globals. None of those headers expose a usable include guard, so this is unguarded.
		private readonly string[] HDRPTerrainUniforms =
		{
			"CBUFFER_START( UnityTerrain )",
			"float4 _TerrainHeightmapScale;",
			"CBUFFER_END",
			"float _TerrainBasemapDistance;",
			"uint _NumLayersCount;",
		};

		// _TerrainHeightmapScale.y stores the normalized height ( hmScale.y / kMaxHeight ); SG's Terrain Properties
		// node recovers the un-normalized max local height by multiplying back by 0.5.
		private const string MaxLocalHeightStr = "( _TerrainHeightmapScale.y * 0.5 )";
		private const string BasemapDistanceStr = "_TerrainBasemapDistance";
		private const string LayersCountStr = "_NumLayersCount";

		public const string NodeErrorMsg = "Only valid on URP/HDRP 6000.4+";
		public const string ErrorOnCompilationMsg = "Attempting to use a URP/HDRP 6000.4+ specific node on incorrect SRP, RP or version.";

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddOutputPort( WirePortDataType.FLOAT, "Max Local Height" );
			AddOutputPort( WirePortDataType.FLOAT, "Basemap Distance" );
			AddOutputPort( WirePortDataType.INT, "Layers Count" );
			m_errorMessageTooltip = NodeErrorMsg;
			m_errorMessageTypeIsError = NodeMessageType.Error;
			m_autoWrapProperties = false;
		}

		public override void OnNodeLogicUpdate( DrawInfo drawInfo )
		{
			base.OnNodeLogicUpdate( drawInfo );
			m_showErrorMessage = ( ContainerGraph.CurrentCanvasMode == NodeAvailability.SurfaceShader ) ||
								 ( ContainerGraph.CurrentCanvasMode == NodeAvailability.TemplateShader &&
									ContainerGraph.CurrentSRPType != TemplateSRPType.URP &&
									ContainerGraph.CurrentSRPType != TemplateSRPType.HDRP );
		}

		public override void DrawProperties()
		{
			base.DrawProperties();
			if ( m_showErrorMessage )
			{
				EditorGUILayout.HelpBox( NodeErrorMsg, MessageType.Error );
			}
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			bool isURP = dataCollector.CurrentSRPType == TemplateSRPType.URP;
			bool isHDRP = dataCollector.CurrentSRPType == TemplateSRPType.HDRP;
			if ( !dataCollector.IsSRP || ( !isURP && !isHDRP ) || ASEPackageManagerHelper.CurrentSRPVersion < TerrainPropertiesMinVersion )
			{
				UIUtils.ShowMessage( ErrorOnCompilationMsg, MessageSeverity.Error );
				return GenerateErrorValue( outputId );
			}

			string[] terrainUniforms = isURP ? URPTerrainUniforms : HDRPTerrainUniforms;
			for ( int i = 0; i < terrainUniforms.Length; i++ )
			{
				dataCollector.AddToUniforms( UniqueId, terrainUniforms[ i ] );
			}

			switch ( outputId )
			{
				default:
				case 0: return MaxLocalHeightStr;
				case 1: return BasemapDistanceStr;
				case 2: return LayersCountStr;
			}
		}
	}
}

// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Unity.Scripting.LifecycleManagement;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "URP Sample Buffer", "Miscellaneous", "URP Sample Buffer samples a buffer directly from the Camera. Only available on URP.\n\nUV: Normalized screen position, defaults to the current pixel\n\nNormal WS: Needs camera normals, requested by e.g. SSAO, Decals or a Full Screen Pass\nMotion Vectors: Needs motion vectors, requested by e.g. TAA or a Full Screen Pass; valid from transparents onwards\nBlit Source: Only bound during full screen blits, e.g. Full Screen Pass with Fetch Color Buffer" )]
	[NoAutoStaticsCleanup]
	public sealed class URPSampleBufferNode : ParentNode
	{
		private enum SourceBufferOption
		{
			NormalWS = 0,
			MotionVectors,
			BlitSource
			// @diogo: add new ones at the bottom to ensure unique id
		}

		[SerializeField]
		private SourceBufferOption m_sourceBuffer = SourceBufferOption.NormalWS;

		private const string URPSampleBufferTitle = "URP Sample Buffer";
		private const string SourceBufferLabel = "Source Buffer";

		private const string ErrorOnCompilationMsg = "\"URP Sample Buffer\" node requires URP.";
		private const string NodeSRPErrorMsg = "Only valid on URP";

		private class SourceBufferConfig
		{
			public string optionName;
			public WirePortDataType outputFormat;
			public string define;
			public string uniform;
			public string[] function;
		}

		// @diogo: indexed by SourceBufferOption
		private static readonly SourceBufferConfig[] SourceBufferConfigs =
		{
			new SourceBufferConfig()
			{
				optionName = "Normal WS",
				outputFormat = WirePortDataType.FLOAT3,
				define = "REQUIRE_NORMAL_TEXTURE 1",
				function = new string[]
				{
					"float3 URPSampleBuffer_NormalWS( float2 uv )\n",
					"{\n",
					"\treturn SHADERGRAPH_SAMPLE_SCENE_NORMAL( uv );\n",
					"}\n"
				}
			},
			new SourceBufferConfig()
			{
				optionName = "Motion Vectors",
				outputFormat = WirePortDataType.FLOAT2,
				uniform = "TEXTURE2D_X( _MotionVectorTexture );",
				function = new string[]
				{
					"float2 URPSampleBuffer_MotionVectors( float2 uv )\n",
					"{\n",
					"\tuint2 pixelCoords = uint2( uv * _ScreenSize.xy );\n",
					"\treturn LOAD_TEXTURE2D_X_LOD( _MotionVectorTexture, pixelCoords, 0 ).xy;\n",
					"}\n"
				}
			},
			new SourceBufferConfig()
			{
				optionName = "Blit Source",
				outputFormat = WirePortDataType.FLOAT4,
				uniform = "TEXTURE2D_X( _BlitTexture );",
				function = new string[]
				{
					"float4 URPSampleBuffer_BlitSource( float2 uv )\n",
					"{\n",
					"\tuint2 pixelCoords = uint2( uv * _ScreenSize.xy );\n",
					"\treturn LOAD_TEXTURE2D_X_LOD( _BlitTexture, pixelCoords, 0 );\n",
					"}\n"
				}
			}
		};

		private static readonly string[] SourceBufferOptions = SourceBufferConfigs.Select( config => config.optionName ).ToArray();

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( WirePortDataType.FLOAT2, false, "UV" );
			AddOutputPort( WirePortDataType.FLOAT3, Constants.EmptyPortValue );

			m_errorMessageTooltip = NodeSRPErrorMsg;
			m_errorMessageTypeIsError = NodeMessageType.Error;
			m_autoWrapProperties = true;

			UpdatePorts();
		}

		private void UpdatePorts()
		{
			SourceBufferConfig config = SourceBufferConfigs[ ( int )m_sourceBuffer ];
			m_outputPorts[ 0 ].ChangeProperties( config.optionName, config.outputFormat, false );
		}

		public override void DrawProperties()
		{
			base.DrawProperties();

			EditorGUI.BeginChangeCheck();
			m_sourceBuffer = ( SourceBufferOption )EditorGUILayoutPopup( SourceBufferLabel, ( int )m_sourceBuffer, SourceBufferOptions );
			if ( EditorGUI.EndChangeCheck() )
			{
				UpdatePorts();
			}

			if ( m_showErrorMessage )
			{
				EditorGUILayout.HelpBox( NodeSRPErrorMsg, MessageType.Error );
			}
		}

		public override void DrawTitle( Rect titlePos )
		{
			const string subTitleFormat = "Source( {0} )";
			SetAdditonalTitleTextOnCallback( SourceBufferConfigs[ ( int )m_sourceBuffer ].optionName, subTitleFormat, ( instance, newSubTitle ) => instance.AdditonalTitleContent.text = string.Format( subTitleFormat, newSubTitle ) );

			if ( ContainerGraph.LodLevel <= ParentGraph.NodeLOD.LOD3 )
			{
				GUI.Label( titlePos, URPSampleBufferTitle, UIUtils.GetCustomStyle( CustomStyle.NodeTitle ) );
			}
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if ( !dataCollector.IsSRP || !dataCollector.TemplateDataCollectorInstance.IsURP )
			{
				UIUtils.ShowMessage( ErrorOnCompilationMsg, MessageSeverity.Error );
				return GenerateErrorValue();
			}

			if ( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
			{
				return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );
			}

			SourceBufferConfig config = SourceBufferConfigs[ ( int )m_sourceBuffer ];

			string uv;
			if ( m_inputPorts[ 0 ].IsConnected )
			{
				uv = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
			}
			else
			{
				uv = dataCollector.TemplateDataCollectorInstance.GetScreenPosNormalized( CurrentPrecisionType ) + ".xy";
			}

			if ( config.define != null )
			{
				dataCollector.AddToDirectives( config.define, -1, AdditionalLineType.Define );
			}

			if ( config.uniform != null )
			{
				dataCollector.AddToUniforms( -1, config.uniform );
			}

			dataCollector.AddFunction( config.function[ 0 ], config.function, false );

			string sourceBufferName = m_sourceBuffer.ToString();
			string value = string.Format( "URPSampleBuffer_{0}( {1} )", sourceBufferName, uv );
			RegisterLocalVariable( 0, value, ref dataCollector, string.Format( "urpSampleBuffer_{0}{1}", sourceBufferName, OutputId ) );
			return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );
		}

		public override void OnNodeLogicUpdate( DrawInfo drawInfo )
		{
			base.OnNodeLogicUpdate( drawInfo );
			m_showErrorMessage = ( ContainerGraph.CurrentCanvasMode == NodeAvailability.SurfaceShader ) ||
								 ( ContainerGraph.CurrentCanvasMode == NodeAvailability.TemplateShader && ContainerGraph.CurrentSRPType != TemplateSRPType.URP );
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			Enum.TryParse<SourceBufferOption>( GetCurrentParam( ref nodeParams ), out m_sourceBuffer );
			UpdatePorts();
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_sourceBuffer );
		}
	}
}

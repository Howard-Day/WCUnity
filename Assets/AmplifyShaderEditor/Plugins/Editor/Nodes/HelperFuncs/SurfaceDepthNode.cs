using System;
using UnityEditor;
using UnityEngine;
namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Surface Depth", "Surface Data", "Returns the surface view depth" )]
	public sealed class SurfaceDepthNode : ParentNode
	{
		[SerializeField]
		private int m_viewSpaceInt = 0;
		private DepthMode m_depthMode { get { return ( DepthMode )m_viewSpaceInt; } }

		private UpperLeftWidgetHelper m_upperLeftWidget = new UpperLeftWidgetHelper();

		private void UpdateAdditonalTitleText()
		{
			SetAdditonalTitleText( string.Format( Constants.SubTitleModeFormatStr, GeneratorUtils.DepthModeStr[ m_viewSpaceInt ] ) );
		}

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( WirePortDataType.FLOAT3, false, "Vertex Position" );
			AddOutputPort( WirePortDataType.FLOAT, "Depth" );
			m_autoWrapProperties = true;
			m_hasLeftDropdown = true;
			UpdateAdditonalTitleText();
		}

		public override void Destroy()
		{
			base.Destroy();
			m_upperLeftWidget = null;
		}

		public override void AfterCommonInit()
		{
			base.AfterCommonInit();
			if( PaddingTitleLeft == 0 )
			{
				PaddingTitleLeft = Constants.PropertyPickerWidth + Constants.IconsLeftRightMargin;
				if( PaddingTitleRight == 0 )
					PaddingTitleRight = Constants.PropertyPickerWidth + Constants.IconsLeftRightMargin;
			}
		}

		public override void Draw( DrawInfo drawInfo )
		{
			base.Draw( drawInfo );
			EditorGUI.BeginChangeCheck();
			m_viewSpaceInt = m_upperLeftWidget.DrawWidget( this, m_viewSpaceInt, GeneratorUtils.DepthModeStr );
			if( EditorGUI.EndChangeCheck() )
			{
				UpdateAdditonalTitleText();
			}
		}

		public override void DrawProperties()
		{
			base.DrawProperties();
			EditorGUI.BeginChangeCheck();
			m_viewSpaceInt = EditorGUILayoutPopup( "Depth Mode", m_viewSpaceInt, GeneratorUtils.DepthModeStr );
			if( EditorGUI.EndChangeCheck() )
			{
				UpdateAdditonalTitleText();
			}
		}

		private string ApplyLinearDepthModifier( ref MasterNodeDataCollector dataCollector, string instruction )
		{
			switch ( m_depthMode )
			{
				case DepthMode.DepthLinearEye: instruction = GeneratorUtils.ApplyLinearDepthModifier( ref dataCollector, instruction, m_depthMode ); break;
				case DepthMode.DepthLinear01: instruction = GeneratorUtils.ApplyLinearDepthModifier( ref dataCollector, instruction, m_depthMode ); break;
				case DepthMode.DepthEye: instruction = string.Format( "( {0} ) * ( _ProjectionParams.z - _ProjectionParams.y )", instruction ); break;
				case DepthMode.Depth01:
				default: break;
			}
			return instruction;
		}


		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if ( dataCollector.PortCategory == MasterNodePortCategory.Tessellation )
			{
				UIUtils.ShowNoVertexModeNodeMessage( this );
				return "0";
			}

			if ( dataCollector.IsTemplate )
			{
				if( m_inputPorts[ 0 ].IsConnected )
				{
					string vertexPos = "vertexPos" + OutputId;
					GenerateInputInVertex( ref dataCollector, 0, vertexPos, false );
					
					string clipPos = dataCollector.TemplateDataCollectorInstance.GetClipPosForValue( vertexPos, OutputId );
					string varName = GeneratorUtils.DepthModeVarNameStr[ m_viewSpaceInt ] + OutputId;

					string instruction = string.Format( "{0}.z / {0}.w", clipPos );
					instruction = ApplyLinearDepthModifier( ref dataCollector, instruction );

					dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, WirePortDataType.FLOAT, varName, instruction );
					return varName;
				}
				else
				{
					return dataCollector.TemplateDataCollectorInstance.GetSurfaceDepth( m_depthMode, CurrentPrecisionType );
				}
			}

			dataCollector.AddToIncludes( UniqueId, Constants.UnityShaderVariables );

			if ( dataCollector.PortCategory == MasterNodePortCategory.Vertex )
			{
				string vertexPos;
				string varName;
				if ( m_inputPorts[ 0 ].IsConnected )
				{
					vertexPos = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
					varName = GeneratorUtils.DepthModeVarNameStr[ m_viewSpaceInt ] + OutputId;
				}
				else
				{
					vertexPos = Constants.VertexShaderInputStr + ".vertex.xyz";
					varName = GeneratorUtils.DepthModeVarNameStr[ m_viewSpaceInt ];
				}

				string instruction = string.Format( "UnityObjectToClipPos( {0} ).zw", vertexPos );
				string clipDepth = "clipDepth" + OutputId;
				dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, WirePortDataType.FLOAT2, clipDepth, instruction );

				instruction = string.Format( "{0}.x / {0}.y", clipDepth );
				instruction = ApplyLinearDepthModifier( ref dataCollector, instruction );

				dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, WirePortDataType.FLOAT3, varName, instruction );
				return varName;
			}

			if ( m_inputPorts[ 0 ].IsConnected )
			{
				if ( dataCollector.TesselationActive )
				{
					if ( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
						return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );

					string vertexPos = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
					string instruction = string.Format( "UnityObjectToClipPos( {0} ).zw", vertexPos );
					string clipDepth = "clipDepth" + OutputId;
					string varName = GeneratorUtils.DepthModeVarNameStr[ m_viewSpaceInt ] + OutputId;

					dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, WirePortDataType.FLOAT2, clipDepth, instruction );

					instruction = string.Format( "{0}.x / {0}.y", clipDepth );
					instruction = ApplyLinearDepthModifier( ref dataCollector, instruction );

					RegisterLocalVariable( 0, instruction, ref dataCollector, varName );
					return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );
				}
				else
				{
					var savedPortCategory = dataCollector.PortCategory;
					dataCollector.PortCategory = MasterNodePortCategory.Vertex;
					string vertexPos = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
					string varName = GeneratorUtils.DepthModeVarNameStr[ m_viewSpaceInt ] + OutputId;
					dataCollector.PortCategory = savedPortCategory;

					string clipDepth = "clipDepth" + OutputId;
					dataCollector.AddToInput( UniqueId, clipDepth, WirePortDataType.FLOAT2 );

					string clipDepthOutput = string.Format( "{0}.{1}", Constants.VertexShaderOutputStr, clipDepth );
					string clipDepthInput = string.Format( "{0}.{1}", Constants.InputVarStr, clipDepth );

					string vertexInstruction = string.Format( "UnityObjectToClipPos( {0} ).zw", vertexPos );
					dataCollector.AddToVertexLocalVariables( UniqueId, CurrentPrecisionType, WirePortDataType.FLOAT2, clipDepth, vertexInstruction );
					dataCollector.AddToVertexLocalVariables( UniqueId, clipDepthOutput, clipDepth );

					string fragmentInstruction = string.Format( "{0}.x / {0}.y", clipDepthInput );
					fragmentInstruction = ApplyLinearDepthModifier( ref dataCollector, fragmentInstruction );
					
					dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, WirePortDataType.FLOAT, varName, fragmentInstruction );
					return varName;
				}
			}
			else
			{
				return GeneratorUtils.GenerateSurfaceDepth( ref dataCollector, UniqueId, CurrentPrecisionType, m_depthMode );
			}
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			m_viewSpaceInt = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			UpdateAdditonalTitleText();
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_viewSpaceInt );
		}
	}

}

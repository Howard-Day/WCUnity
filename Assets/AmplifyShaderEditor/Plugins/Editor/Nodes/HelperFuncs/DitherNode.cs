// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using UnityEditor;
using System;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Dither", "Camera And Screen", "Generates a dithering pattern. Optional input Screen Position expected in Normalized coordinates." )]
	public sealed class DitherNode : ParentNode
	{
		private string m_functionHeader = "Dither4x4Bayer( {0}, {1} )";
		private string m_functionBody = string.Empty;

		[SerializeField]
		private int m_selectedPatternInt = 0;

		[SerializeField]
		private bool m_customScreenPos = false;

		private readonly string[] PatternsFuncStr = { "4x4Bayer", "8x8Bayer", "NoiseTex" };
		private readonly string[] PatternsStr = { "4x4 Bayer", "8x8 Bayer", "Noise Texture" };

		private UpperLeftWidgetHelper m_upperLeftWidget = new UpperLeftWidgetHelper();

		private InputPort m_driverPort;
		private InputPort m_screenPosPort;
		private InputPort m_texPort;
		private InputPort m_ssPort;

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );

			m_driverPort = AddInputPort( WirePortDataType.FLOAT, false, Constants.EmptyPortValue );

			m_screenPosPort = AddInputPort( WirePortDataType.FLOAT4, false, "Screen Position" );

			m_texPort = AddInputPort( WirePortDataType.SAMPLER2D, false, "Pattern" );
			m_texPort.CreatePortRestrictions( WirePortDataType.SAMPLER2D );

			m_ssPort = AddInputPort( WirePortDataType.SAMPLERSTATE, false, "SS" );
			m_ssPort.CreatePortRestrictions( WirePortDataType.SAMPLERSTATE );

			AddOutputPort( WirePortDataType.FLOAT, Constants.EmptyPortValue );
			m_textLabelWidth = 110;
			m_autoWrapProperties = true;
			m_hasLeftDropdown = true;
			SetAdditonalTitleText( string.Format( Constants.SubTitleTypeFormatStr, PatternsStr[ m_selectedPatternInt ] ) );
			UpdatePorts();
		}

		public override void Destroy()
		{
			base.Destroy();
			m_upperLeftWidget = null;
			m_texPort = null;
			m_ssPort = null;
		}

		public override void AfterCommonInit()
		{
			base.AfterCommonInit();
			if ( PaddingTitleLeft == 0 )
			{
				PaddingTitleLeft = Constants.PropertyPickerWidth + Constants.IconsLeftRightMargin;
				if ( PaddingTitleRight == 0 )
					PaddingTitleRight = Constants.PropertyPickerWidth + Constants.IconsLeftRightMargin;
			}
		}

		public override void OnConnectedOutputNodeChanges( int outputPortId, int otherNodeId, int otherPortId, string name, WirePortDataType type )
		{
			base.OnConnectedOutputNodeChanges( outputPortId, otherNodeId, otherPortId, name, type );
			if ( !m_texPort.CheckValidType( type ) )
			{
				m_texPort.FullDeleteConnections();
				UIUtils.ShowMessage( UniqueId, "Dithering node only accepts SAMPLER2D input type.\nTexture Object connected changed to " + type + ", connection was lost, please review and update accordingly.", MessageSeverity.Warning );
			}
		}

		public override void Draw( DrawInfo drawInfo )
		{
			base.Draw( drawInfo );
			EditorGUI.BeginChangeCheck();
			m_selectedPatternInt = m_upperLeftWidget.DrawWidget( this, m_selectedPatternInt, PatternsStr );
			if ( EditorGUI.EndChangeCheck() )
			{
				UpdatePorts();
			}
		}

		public override void DrawProperties()
		{
			base.DrawProperties();
			EditorGUI.BeginChangeCheck();
			m_selectedPatternInt = EditorGUILayoutPopup( "Pattern", m_selectedPatternInt, PatternsStr );
			if ( EditorGUI.EndChangeCheck() )
			{
				UpdatePorts();
			}
			EditorGUI.BeginChangeCheck();
			m_customScreenPos = EditorGUILayoutToggle( "Screen Position", m_customScreenPos );
			if ( EditorGUI.EndChangeCheck() )
			{
				UpdatePorts();
			}

			if ( m_customScreenPos )
			{
				EditorGUILayout.HelpBox( "Screen Position input is expected in Normalized (0..1) screen coordinates.", MessageType.Info );
			}
		}

		private void UpdatePorts()
		{
			m_texPort.Visible = ( m_selectedPatternInt == 2 );
			m_ssPort.Visible = ( m_selectedPatternInt == 2 );
			m_screenPosPort.Visible = m_customScreenPos;
			m_sizeIsDirty = true;
			SetAdditonalTitleText( string.Format( Constants.SubTitleTypeFormatStr, PatternsStr[ m_selectedPatternInt ] ) );
		}

		private void GeneratePattern( ref MasterNodeDataCollector dataCollector )
		{
			SetAdditonalTitleText( string.Format( Constants.SubTitleTypeFormatStr, PatternsStr[ m_selectedPatternInt ] ) );
			switch ( m_selectedPatternInt )
			{
				default:
				case 0:
				{
					m_functionBody = string.Empty;
					m_functionHeader = "Dither" + PatternsFuncStr[ m_selectedPatternInt ] + "( {0}, {1} )";
					IOUtils.AddFunctionHeader( ref m_functionBody, "inline float Dither" + PatternsFuncStr[ m_selectedPatternInt ] + "( uint x, uint y )" );
					IOUtils.AddFunctionLine( ref m_functionBody, "const float dither[ 16 ] = {" );
					IOUtils.AddFunctionLine( ref m_functionBody, "     1,  9,  3, 11," );
					IOUtils.AddFunctionLine( ref m_functionBody, "    13,  5, 15,  7," );
					IOUtils.AddFunctionLine( ref m_functionBody, "     4, 12,  2, 10," );
					IOUtils.AddFunctionLine( ref m_functionBody, "    16,  8, 14,  6 };" );
					IOUtils.AddFunctionLine( ref m_functionBody, "uint r = y * 4 + x;" );
					IOUtils.AddFunctionLine( ref m_functionBody, "return dither[ min( r, 15 ) ] / 16; // same # of instructions as pre-dividing due to compiler magic" );
					IOUtils.CloseFunctionBody( ref m_functionBody );
				}
				break;
				case 1:
				{
					m_functionBody = string.Empty;
					m_functionHeader = "Dither" + PatternsFuncStr[ m_selectedPatternInt ] + "( {0}, {1} )";
					IOUtils.AddFunctionHeader( ref m_functionBody, "inline float Dither" + PatternsFuncStr[ m_selectedPatternInt ] + "( uint x, uint y )" );
					IOUtils.AddFunctionLine( ref m_functionBody, "const float dither[ 64 ] = {" );
					IOUtils.AddFunctionLine( ref m_functionBody, "     1, 49, 13, 61,  4, 52, 16, 64," );
					IOUtils.AddFunctionLine( ref m_functionBody, "    33, 17, 45, 29, 36, 20, 48, 32," );
					IOUtils.AddFunctionLine( ref m_functionBody, "     9, 57,  5, 53, 12, 60,  8, 56," );
					IOUtils.AddFunctionLine( ref m_functionBody, "    41, 25, 37, 21, 44, 28, 40, 24," );
					IOUtils.AddFunctionLine( ref m_functionBody, "     3, 51, 15, 63,  2, 50, 14, 62," );
					IOUtils.AddFunctionLine( ref m_functionBody, "    35, 19, 47, 31, 34, 18, 46, 30," );
					IOUtils.AddFunctionLine( ref m_functionBody, "    11, 59,  7, 55, 10, 58,  6, 54," );
					IOUtils.AddFunctionLine( ref m_functionBody, "    43, 27, 39, 23, 42, 26, 38, 22};" );
					IOUtils.AddFunctionLine( ref m_functionBody, "uint r = y * 8 + x;" );
					IOUtils.AddFunctionLine( ref m_functionBody, "return dither[ min( r, 63 ) ] / 64; // same # of instructions as pre-dividing due to compiler magic" );
					IOUtils.CloseFunctionBody( ref m_functionBody );
				}
				break;
				case 2:
				{
					ParentGraph outsideGraph = UIUtils.CurrentWindow.OutsideGraph;
					// @diogo: a template-declared legacy sampler can only be sampled through the texXD intrinsics (#241)
					bool legacySampler = GeneratorUtils.IsLegacyTemplateSampler( m_texPort );

					// @diogo: legacy sampling drops the sampler parameter, so that variant needs its own name
					string functionName = "Dither" + PatternsFuncStr[ m_selectedPatternInt ] + ( legacySampler ? "Legacy" : string.Empty );

					m_functionBody = string.Empty;
					m_functionHeader = functionName + "( {0}, {1}, {2} )";

					IOUtils.AddFunctionHeader( ref m_functionBody, "inline float " + functionName +
						"( float4 screenPos, " + GeneratorUtils.GetPropertyDeclaraction( "noiseTexture", TextureType.Texture2D, ", ", legacySampler ) +
						GeneratorUtils.GetSamplerDeclaraction( "samplernoiseTexture", TextureType.Texture2D, ", ", legacySampler ) + "float4 noiseTexelSize )" );

					string screenParams = dataCollector.IsURP ? "_ScaledScreenParams" : "_ScreenParams";
					string samplingCall = GeneratorUtils.GenerateSamplingCall( ref dataCollector, WirePortDataType.SAMPLER2D, "noiseTexture",
						"samplernoiseTexture", "screenPos.xy * " + screenParams + ".xy * noiseTexelSize.xy", legacySampler, MipType.MipLevel, "0" );

					IOUtils.AddFunctionLine( ref m_functionBody, "float dither = " + samplingCall + ".g;" );
					IOUtils.AddFunctionLine( ref m_functionBody, "float ditherRate = noiseTexelSize.x * noiseTexelSize.y;" );
					IOUtils.AddFunctionLine( ref m_functionBody, "dither = ( 1 - ditherRate ) * dither + ditherRate;" );
					IOUtils.AddFunctionLine( ref m_functionBody, "return dither;" );
					IOUtils.CloseFunctionBody( ref m_functionBody );
				}
				break;
			}
		}

		public override void PropagateNodeData( NodeData nodeData, ref MasterNodeDataCollector dataCollector )
		{
			base.PropagateNodeData( nodeData, ref dataCollector );
			dataCollector.UsingCustomScreenPos = true;
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalVar )
		{
			if ( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
			{
				return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );
			}

			GeneratePattern( ref dataCollector );

			if ( !( dataCollector.IsTemplate && dataCollector.IsSRP ) )
			{
				dataCollector.AddToIncludes( UniqueId, Constants.UnityShaderVariables );
			}

			string varName = string.Empty;
			if ( m_customScreenPos && m_screenPosPort.IsConnected )
			{
				varName = "ditherScreenPos" + OutputId;
				string customScreenPosVal = m_screenPosPort.GeneratePortInstructions( ref dataCollector );
				dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, WirePortDataType.FLOAT4, varName, customScreenPosVal );
			}
			else
			{
				if ( dataCollector.TesselationActive && dataCollector.IsFragmentCategory )
				{
					varName = GeneratorUtils.GenerateScreenPositionNormalizedOnFrag( ref dataCollector, UniqueId, CurrentPrecisionType );
				}
				else
				{
					if ( dataCollector.IsTemplate )
					{
						varName = dataCollector.TemplateDataCollectorInstance.GetScreenPosNormalized( CurrentPrecisionType );
					}
					else
					{
						varName = GeneratorUtils.GenerateScreenPositionNormalized( ref dataCollector, UniqueId, CurrentPrecisionType, !dataCollector.UsingCustomScreenPos );
					}
				}
			}

			m_showErrorMessage = false;
			string functionResult = "";
			string noiseTex = string.Empty;

			if ( m_selectedPatternInt == 0 || m_selectedPatternInt == 1 )
			{
				// 4x4 or 8x8 Bayer
				string size = ( m_selectedPatternInt == 0 ) ? "4" : "8";
				string screenParams = dataCollector.IsURP ? "_ScaledScreenParams" : "_ScreenParams";
				string ditherScreenPosPixel = "ditherScreenPosPixel" + OutputId;

				dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, WirePortDataType.FLOAT2, ditherScreenPosPixel, string.Format( "{0}.xy * {1}.xy", varName, screenParams ) );

				string coordX = string.Format( "fmod( {0}.x, {1} )", ditherScreenPosPixel, size );
				string coordY = string.Format( "fmod( {0}.y, {1} )", ditherScreenPosPixel, size );

				functionResult = dataCollector.AddFunctions( m_functionHeader, m_functionBody, coordX, coordY );
			}
			else if ( m_selectedPatternInt == 2 )
			{
				// Noise Texture
				if ( !m_texPort.IsConnected )
				{
					m_showErrorMessage = true;
					m_errorMessageTypeIsError = NodeMessageType.Warning;
					m_errorMessageTooltip = "Please connect a texture object to the Pattern input port to generate a proper dithered pattern";
					return "0";
				}
				else
				{
					ParentGraph outsideGraph = UIUtils.CurrentWindow.OutsideGraph;
					noiseTex = m_texPort.GeneratePortInstructions( ref dataCollector );
					// @diogo: a template-declared legacy sampler can only be sampled through the texXD intrinsics (#241)
					bool legacySampler = GeneratorUtils.IsLegacyTemplateSampler( m_texPort );

					dataCollector.AddToUniforms( UniqueId, "float4 " + noiseTex + "_TexelSize;", dataCollector.IsSRP );
					if ( outsideGraph.SamplingMacros && !legacySampler )
					{
						string sampler = string.Empty;
						if ( m_ssPort.IsConnected )
						{
							sampler = m_ssPort.GeneratePortInstructions( ref dataCollector );
						}
						else
						{
							sampler = GeneratorUtils.GenerateSamplerState( ref dataCollector, UniqueId, noiseTex, VariableMode.Create );
						}
						functionResult = dataCollector.AddFunctions( m_functionHeader, m_functionBody, varName, noiseTex + ", " + sampler, noiseTex + "_TexelSize" );
					}
					else
					{
						functionResult = dataCollector.AddFunctions( m_functionHeader, m_functionBody, varName, noiseTex, noiseTex + "_TexelSize" );
					}
				}
			}

			dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, WirePortDataType.FLOAT, "dither" + OutputId, functionResult );

			if ( m_driverPort.IsConnected )
			{
				string driver = m_driverPort.GeneratePortInstructions( ref dataCollector );
				dataCollector.AddLocalVariable( UniqueId, "dither" + OutputId + " = step( dither" + OutputId + ", saturate( " + driver + " * 1.00001 ) );" );
			}

			m_outputPorts[ 0 ].SetLocalValue( "dither" + OutputId, dataCollector.PortCategory );

			return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			m_selectedPatternInt = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			if ( UIUtils.CurrentShaderVersion() > 15404 )
			{
				m_customScreenPos = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
			}
			UpdatePorts();
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_selectedPatternInt );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_customScreenPos );
		}
	}
}

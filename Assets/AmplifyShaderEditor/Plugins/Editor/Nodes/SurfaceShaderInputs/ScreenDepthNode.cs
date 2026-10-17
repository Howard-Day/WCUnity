// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using UnityEditor;

using System;
namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Screen Depth", "Camera And Screen", "Given a screen position returns the depth of the scene to the object as seen by the camera" )]
	public sealed class ScreenDepthNode : ParentNode
	{
		

		[SerializeField]
		private int m_viewSpaceInt = ( int )DepthMode.DepthEye;
		private DepthMode m_depthMode { get { return ( DepthMode )m_viewSpaceInt; } }

		private UpperLeftWidgetHelper m_upperLeftWidget = new UpperLeftWidgetHelper();

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( WirePortDataType.FLOAT4, false, "Pos" );
			AddOutputPort( WirePortDataType.FLOAT, "Depth" );
			m_autoWrapProperties = true;
			m_hasLeftDropdown = true;
			UpdateAdditonalTitleText();
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

		public override void Destroy()
		{
			base.Destroy();
			m_upperLeftWidget = null;
		}

		private void UpdateAdditonalTitleText()
		{
			SetAdditonalTitleText( string.Format( Constants.SubTitleModeFormatStr, GeneratorUtils.DepthModeStr[ m_viewSpaceInt ] ) );
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
			if ( EditorGUI.EndChangeCheck() )
			{
				UpdateAdditonalTitleText();
			}
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if ( dataCollector.PortCategory == MasterNodePortCategory.Tessellation )
			{
				UIUtils.ShowNoVertexModeNodeMessage( this );
				return "0";
			}

			if ( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
				return GetOutputColorItem( 0, outputId, m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory ) );

			if ( !( dataCollector.IsTemplate && dataCollector.IsSRP ) )
				dataCollector.AddToIncludes( UniqueId, Constants.UnityCgLibFuncs );

			if ( !dataCollector.IsTemplate || dataCollector.TemplateDataCollectorInstance.CurrentSRPType != TemplateSRPType.HDRP )
			{
				if ( dataCollector.IsTemplate && dataCollector.CurrentSRPType == TemplateSRPType.URP )
				{
					dataCollector.AddToDirectives( Constants.CameraDepthTextureURPEnabler, -1, AdditionalLineType.Define );
				}
				else
				{
					dataCollector.AddToUniforms( UniqueId, Constants.CameraDepthTextureValue );
					dataCollector.AddToUniforms( UniqueId, Constants.CameraDepthTextureTexelSize );
				}
			}


			string screenPosNorm = string.Empty;
			if ( m_inputPorts[ 0 ].IsConnected )
			{
				screenPosNorm = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
			}
			else
			{
				if ( dataCollector.IsTemplate )
				{
					if ( !dataCollector.TemplateDataCollectorInstance.GetCustomInterpolatedData( TemplateInfoOnSematics.SCREEN_POSITION_NORMALIZED, WirePortDataType.FLOAT4, PrecisionType.Float, ref screenPosNorm, true, MasterNodePortCategory.Fragment ) )
					{
						screenPosNorm = GeneratorUtils.GenerateScreenPositionNormalized( ref dataCollector, UniqueId, CurrentPrecisionType, !dataCollector.UsingCustomScreenPos );
					}
				}
				else
				{
					screenPosNorm = GeneratorUtils.GenerateScreenPositionNormalized( ref dataCollector, UniqueId, CurrentPrecisionType, !dataCollector.UsingCustomScreenPos );
				}
			}

			string screenDepthInstruction = TemplateHelperFunctions.CreateDepthFetch( dataCollector, screenPosNorm );

			if ( m_depthMode == DepthMode.DepthLinearEye || m_depthMode == DepthMode.DepthLinear01 )
			{
				string viewSpace = ( m_depthMode == DepthMode.DepthLinearEye ) ? "LinearEyeDepth" : "Linear01Depth";
				if ( dataCollector.IsTemplate && dataCollector.IsSRP )
				{
					screenDepthInstruction = string.Format( "{0}( {1}, _ZBufferParams )", viewSpace, screenDepthInstruction );
				}
				else
				{
					screenDepthInstruction = string.Format( "{0}( {1} )", viewSpace, screenDepthInstruction );
				}
			}
			else if ( m_depthMode == DepthMode.DepthEye )
			{
				screenDepthInstruction = string.Format( "{0} * ( _ProjectionParams.z - _ProjectionParams.y )", screenDepthInstruction );
			}

			dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, WirePortDataType.FLOAT, GeneratorUtils.DepthModeVarNameStr[ m_viewSpaceInt ] + OutputId, screenDepthInstruction );

			m_outputPorts[ 0 ].SetLocalValue( GeneratorUtils.DepthModeVarNameStr[ m_viewSpaceInt ] + OutputId, dataCollector.PortCategory );
			return GetOutputColorItem( 0, outputId, GeneratorUtils.DepthModeVarNameStr[ m_viewSpaceInt ] + OutputId );
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			m_viewSpaceInt = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			if( UIUtils.CurrentShaderVersion() >= 13901 && UIUtils.CurrentShaderVersion() < 19702 )
			{
				bool convertToLinear = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
				if ( !convertToLinear )
				{
					if ( m_viewSpaceInt == ( int )DepthMode.DepthLinearEye )
					{
						m_viewSpaceInt = ( int )DepthMode.DepthEye;
					}
					else if ( m_viewSpaceInt == ( int )DepthMode.DepthLinear01 )
					{
						m_viewSpaceInt = ( int )DepthMode.Depth01;
					}
				}
			}

			UpdateAdditonalTitleText();
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_viewSpaceInt );
		}
	}

}

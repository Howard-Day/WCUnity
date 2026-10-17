// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using UnityEditor;
using System;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Screen Position", "Camera And Screen", "Gives access to the screen coordinates of the mesh vertex or fragment, where the X and Y values represent the horizontal and vertical positions. Use the Mode dropdown to choose the desired output mode." )]
	public sealed class ScreenPosInputsNode : SurfaceShaderINParentNode
	{
		enum Mode
		{
			Normalized = 0,
			Raw,
			Center,
			Tiled,
			Pixel
		};

		private readonly string[] m_outputTypeStr = { "Normalized", "Raw", "Center", "Tiled", "Pixel" };

		[SerializeField]
		private int m_outputTypeInt = ( int )Mode.Normalized;

		[SerializeField]
		private bool m_scaleAndOffset = false;

		private UpperLeftWidgetHelper m_upperLeftWidget = new UpperLeftWidgetHelper();

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			m_currentInput = SurfaceInputs.SCREEN_POS;
			InitialSetup();
			m_textLabelWidth = 65;
			m_autoWrapProperties = true;

			m_hasLeftDropdown = true;
			m_previewShaderGUID = "a5e7295278a404175b732f1516fb68a6";

			if( UIUtils.CurrentWindow != null && UIUtils.CurrentWindow.CurrentGraph != null && UIUtils.CurrentShaderVersion() <= 2400 )
			{
				m_outputTypeInt = ( int )Mode.Raw;
				m_previewMaterialPassId = ( int )m_outputTypeInt;
			}

			ConfigureHeader();
		}

		public override void Draw( DrawInfo drawInfo )
		{
			base.Draw( drawInfo );
			EditorGUI.BeginChangeCheck();
			m_outputTypeInt = m_upperLeftWidget.DrawWidget( this, m_outputTypeInt, m_outputTypeStr );
			if( EditorGUI.EndChangeCheck() )
			{
				ConfigureHeader();
			}
		}

		public override void DrawProperties()
		{
			//base.DrawProperties();

			EditorGUI.BeginChangeCheck();
			m_outputTypeInt = EditorGUILayoutPopup( "Mode", m_outputTypeInt, m_outputTypeStr );
			if( EditorGUI.EndChangeCheck() )
			{
				ConfigureHeader();
			}
		}

		void ConfigureHeader()
		{
			SetAdditonalTitleText( string.Format( Constants.SubTitleModeFormatStr, m_outputTypeStr[ m_outputTypeInt ] ) );
			m_previewMaterialPassId = m_outputTypeInt;
		}

		public override void Reset()
		{
			base.Reset();
		}

		public override void Destroy()
		{
			base.Destroy();
			m_upperLeftWidget = null;
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalVar )
		{
			if ( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
			{
				return GetOutputVectorItem( 0, outputId, m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory ) );
			}
			m_currentPrecisionType = PrecisionType.Float;

			// TODO: these kinds of calls need a serious cleanup, too much redundancy
			string screenPos = string.Empty;
			if ( dataCollector.TesselationActive && dataCollector.IsFragmentCategory || dataCollector.IsTemplate )
			{
				switch ( ( Mode )m_outputTypeInt )
				{
					case Mode.Normalized: screenPos = GeneratorUtils.GenerateScreenPositionNormalizedOnFrag( ref dataCollector, UniqueId, CurrentPrecisionType ); break;
					case Mode.Raw: screenPos = GeneratorUtils.GenerateScreenPositionRawOnFrag( ref dataCollector, UniqueId, CurrentPrecisionType ); break;
					case Mode.Center: screenPos = GeneratorUtils.GenerateScreenPositionCenterOnFrag( ref dataCollector, UniqueId, CurrentPrecisionType ); break;
					case Mode.Tiled: screenPos = GeneratorUtils.GenerateScreenPositionTiledOnFrag( ref dataCollector, UniqueId, CurrentPrecisionType ); break;
					case Mode.Pixel: screenPos = GeneratorUtils.GenerateScreenPositionPixelOnFrag( ref dataCollector, UniqueId, CurrentPrecisionType ); break;
				}
			}
			else
			{
				switch ( ( Mode )m_outputTypeInt )
				{
					case Mode.Normalized: screenPos = GeneratorUtils.GenerateScreenPositionNormalized( ref dataCollector, UniqueId, CurrentPrecisionType ); break;
					case Mode.Raw: screenPos = GeneratorUtils.GenerateScreenPositionRaw( ref dataCollector, UniqueId, CurrentPrecisionType ); break;
					case Mode.Center: screenPos = GeneratorUtils.GenerateScreenPositionCenter( ref dataCollector, UniqueId, CurrentPrecisionType ); break;
					case Mode.Tiled: screenPos = GeneratorUtils.GenerateScreenPositionTiled( ref dataCollector, UniqueId, CurrentPrecisionType ); break;
					case Mode.Pixel: screenPos = GeneratorUtils.GenerateScreenPositionPixel( ref dataCollector, UniqueId, CurrentPrecisionType ); break;
				}
			}

			m_outputPorts[ 0 ].SetLocalValue( screenPos, dataCollector.PortCategory );
			return GetOutputVectorItem( 0, outputId, screenPos );

		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			if( UIUtils.CurrentShaderVersion() > 2400 )
			{
				if( UIUtils.CurrentShaderVersion() < 6102 )
				{
					bool project = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
					m_outputTypeInt = project ? ( int )Mode.Normalized : ( int )Mode.Raw;
				}
				else
				{
					m_outputTypeInt = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
				}
			}

			if( UIUtils.CurrentShaderVersion() > 3107 )
			{
				m_scaleAndOffset = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
				m_scaleAndOffset = false;
			}

			ConfigureHeader();
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_outputTypeInt );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_scaleAndOffset );
		}
	}
}

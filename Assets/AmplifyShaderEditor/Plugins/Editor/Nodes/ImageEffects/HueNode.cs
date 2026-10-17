// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using UnityEditor;
using System;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Hue", "Image Effects", "Offsets the hue of the input color by Offset, in degrees or normalized", tags: "hue shift rotate color" )]
	public sealed class HueNode : ParentNode
	{
		private const string RangeStr = "Range";
		private const string SubTitleRangeFormatStr = "Range( {0} )";
		private readonly string[] m_rangeValues = { "Degrees", "Normalized" };

		[SerializeField]
		private int m_range = 0;

		private UpperLeftWidgetHelper m_upperLeftWidget = new UpperLeftWidgetHelper();

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( WirePortDataType.FLOAT3, false, "In" );
			AddInputPort( WirePortDataType.FLOAT, false, "Offset" );
			AddOutputPort( WirePortDataType.FLOAT3, Constants.EmptyPortValue );
			m_textLabelWidth = 60;
			m_useInternalPortData = true;
			m_hasLeftDropdown = true;
			m_autoWrapProperties = true;
			m_customPrecision = true;
			m_previewShaderGUID = "f6aa245786644cbaa42f95b1c37f7a48";
			UpdateFromSelected();
		}

		public override void AfterCommonInit()
		{
			base.AfterCommonInit();
			if ( PaddingTitleLeft == 0 )
			{
				PaddingTitleLeft = Constants.PropertyPickerWidth + Constants.IconsLeftRightMargin;
				if ( PaddingTitleRight == 0 )
				{
					PaddingTitleRight = Constants.PropertyPickerWidth + Constants.IconsLeftRightMargin;
				}
			}
		}

		public override void Destroy()
		{
			base.Destroy();
			m_upperLeftWidget = null;
		}

		void UpdateFromSelected()
		{
			m_previewMaterialPassId = m_range;
			SetAdditonalTitleText( string.Format( SubTitleRangeFormatStr, m_rangeValues[ m_range ] ) );
		}

		public override void Draw( DrawInfo drawInfo )
		{
			base.Draw( drawInfo );
			EditorGUI.BeginChangeCheck();
			m_range = m_upperLeftWidget.DrawWidget( this, m_range, m_rangeValues );
			if ( EditorGUI.EndChangeCheck() )
			{
				UpdateFromSelected();
			}
		}

		public override void DrawProperties()
		{
			base.DrawProperties();
			EditorGUI.BeginChangeCheck();
			m_range = EditorGUILayoutPopup( RangeStr, m_range, m_rangeValues );
			if ( EditorGUI.EndChangeCheck() )
			{
				UpdateFromSelected();
			}
			DrawPrecisionProperty();
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if ( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
			{
				return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );
			}

			string precisionString = UIUtils.PrecisionWirePortToCgType( CurrentPrecisionType, WirePortDataType.FLOAT );
			RGBToHSVNode.AddRGBToHSVFunction( ref dataCollector, precisionString );
			HSVToRGBNode.AddHSVToRGBFunction( ref dataCollector, precisionString );

			string input = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
			string offset = m_inputPorts[ 1 ].GeneratePortInstructions( ref dataCollector );
			if ( m_range == 0 )
			{
				offset = string.Format( "( {0} / 360.0 )", offset );
			}

			string hsvVarName = "hueHSV" + OutputId;
			dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, WirePortDataType.FLOAT3, hsvVarName, string.Format( RGBToHSVNode.RGBToHSVHeader, input ) );

			// @diogo: Shader Graph's explicit hue wrap is omitted, HSVToRGB's frac() already wraps it
			string result = string.Format( "HSVToRGB( {0}3( {1}.x + {2}, {1}.y, {1}.z ) )", precisionString, hsvVarName, offset );
			RegisterLocalVariable( 0, result, ref dataCollector, "hueVar" + OutputId );
			return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_range );
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			m_range = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			UpdateFromSelected();
		}
	}
}

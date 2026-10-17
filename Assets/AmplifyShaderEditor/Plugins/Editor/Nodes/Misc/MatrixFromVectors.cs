using UnityEngine;
using UnityEditor;
using System;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Matrix Create", "Matrix Operators", "Create a matrix from vectors.", tags: "matrix from vectors create" )]
	public sealed class MatrixFromVectors : ParentNode
	{
		[SerializeField]
		private WirePortDataType m_selectedOutputType = WirePortDataType.FLOAT3x3;

		[SerializeField]
		private int m_selectedOutputTypeInt = 1;

		[SerializeField]
		private eVectorFromMatrixMode m_mode = eVectorFromMatrixMode.Row;

		private string[] m_defaultValuesStr = { "[0]", "[1]", "[2]", "[3]" };

		private readonly string[] _outputValueTypes ={  "Matrix 2x2", "Matrix 3x3", "Matrix 4x4"};

		private UpperLeftWidgetHelper m_upperLeftWidget = new UpperLeftWidgetHelper();

		private const string SubtitleFormat = "{0}, {1}";

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( WirePortDataType.FLOAT4, false, "[0]" );
			AddInputPort( WirePortDataType.FLOAT4, false, "[1]" );
			AddInputPort( WirePortDataType.FLOAT4, false, "[2]" );
			AddInputPort( WirePortDataType.FLOAT4, false, "[3]" );
			AddOutputPort( m_selectedOutputType, Constants.EmptyPortValue );
			m_inputPorts[ 0 ].AutoDrawInternalData = true;
			m_inputPorts[ 1 ].AutoDrawInternalData = true;
			m_inputPorts[ 2 ].AutoDrawInternalData = true;
			m_inputPorts[ 3 ].AutoDrawInternalData = true;
			m_textLabelWidth = 90;
			m_autoWrapProperties = true;
			m_hasLeftDropdown = true;
			UpdatePorts();
			UpdateSubtitle();
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

		private void UpdateSubtitle()
		{
			string type;
			switch ( m_selectedOutputType )
			{
				case WirePortDataType.FLOAT2x2: type = "2x2"; break;
				case WirePortDataType.FLOAT3x3: type = "3x3"; break;
				default: type = "4x4"; break;
			}

			SetAdditonalTitleText( string.Format( SubtitleFormat, type, m_mode ) );
		}

		public override void Destroy()
		{
			base.Destroy();
			m_upperLeftWidget = null;
		}

		public override void Draw( DrawInfo drawInfo )
		{
			base.Draw( drawInfo );
			EditorGUI.BeginChangeCheck();
			m_selectedOutputTypeInt = m_upperLeftWidget.DrawWidget( this, m_selectedOutputTypeInt, _outputValueTypes );
			if( EditorGUI.EndChangeCheck() )
			{
				switch( m_selectedOutputTypeInt )
				{
					case 0: m_selectedOutputType = WirePortDataType.FLOAT2x2; break;
					case 1: m_selectedOutputType = WirePortDataType.FLOAT3x3; break;
					case 2: m_selectedOutputType = WirePortDataType.FLOAT4x4; break;
				}

				UpdatePorts();
			}
		}

		public override void DrawProperties()
		{
			base.DrawProperties();

			EditorGUI.BeginChangeCheck();
			m_mode = ( eVectorFromMatrixMode )EditorGUILayoutEnumPopup( "Mode", m_mode );
			if( EditorGUI.EndChangeCheck() )
			{
				UpdateSubtitle();
			}

			EditorGUI.BeginChangeCheck();
			m_selectedOutputTypeInt = EditorGUILayoutPopup( "Output type", m_selectedOutputTypeInt, _outputValueTypes );
			if( EditorGUI.EndChangeCheck() )
			{
				switch( m_selectedOutputTypeInt )
				{
					case 0: m_selectedOutputType = WirePortDataType.FLOAT2x2; break;
					case 1: m_selectedOutputType = WirePortDataType.FLOAT3x3; break;
					case 2: m_selectedOutputType = WirePortDataType.FLOAT4x4; break;
				}

				UpdatePorts();
				UpdateSubtitle();
			}

			int count = 0;
			switch ( m_selectedOutputType )
			{
				case WirePortDataType.FLOAT2x2: count = 2; break;
				case WirePortDataType.FLOAT3x3: count = 3; break;
				case WirePortDataType.FLOAT4x4: count = 4; break;
			}

			for ( int i = 0; i < count; i++ )
			{
				if ( !m_inputPorts[ i ].IsConnected )
				{
					m_inputPorts[ i ].ShowInternalData( this, true, m_defaultValuesStr[ i ] );
				}
			}
		}

		void UpdatePorts()
		{
			m_sizeIsDirty = true;
			ChangeOutputType( m_selectedOutputType, false );
			switch( m_selectedOutputType )
			{
				case WirePortDataType.FLOAT2x2:
				m_inputPorts[ 0 ].ChangeType( WirePortDataType.FLOAT2, false );
				m_inputPorts[ 1 ].ChangeType( WirePortDataType.FLOAT2, false );
				m_inputPorts[ 2 ].ChangeType( WirePortDataType.FLOAT2, false );
				m_inputPorts[ 3 ].ChangeType( WirePortDataType.FLOAT2, false );
				m_inputPorts[ 2 ].Visible = false;
				m_inputPorts[ 3 ].Visible = false;
				break;
				case WirePortDataType.FLOAT3x3:
				m_inputPorts[ 0 ].ChangeType( WirePortDataType.FLOAT3, false );
				m_inputPorts[ 1 ].ChangeType( WirePortDataType.FLOAT3, false );
				m_inputPorts[ 2 ].ChangeType( WirePortDataType.FLOAT3, false );
				m_inputPorts[ 3 ].ChangeType( WirePortDataType.FLOAT3, false );
				m_inputPorts[ 2 ].Visible = true;
				m_inputPorts[ 3 ].Visible = false;
				break;
				case WirePortDataType.FLOAT4x4:
				m_inputPorts[ 0 ].ChangeType( WirePortDataType.FLOAT4, false );
				m_inputPorts[ 1 ].ChangeType( WirePortDataType.FLOAT4, false );
				m_inputPorts[ 2 ].ChangeType( WirePortDataType.FLOAT4, false );
				m_inputPorts[ 3 ].ChangeType( WirePortDataType.FLOAT4, false );
				m_inputPorts[ 2 ].Visible = true;
				m_inputPorts[ 3 ].Visible = true;
				break;
			}
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			base.GenerateShaderForOutput( outputId, ref dataCollector, ignoreLocalvar );
			string result = "";
			switch( m_selectedOutputType )
			{
				case WirePortDataType.FLOAT2x2:
				if( m_mode == eVectorFromMatrixMode.Row )
				{
					result = "float2x2( " + m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector ) + ", "
						+ m_inputPorts[ 1 ].GeneratePortInstructions( ref dataCollector ) + " )";
				}
				else
				{
					string vec0 = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
					string vec1 = m_inputPorts[ 1 ].GeneratePortInstructions( ref dataCollector );
					result = string.Format( "float2x2( {0}.x, {1}.x, {0}.y, {1}.y )", vec0, vec1 );
				}
				break;
				case WirePortDataType.FLOAT3x3:
				if( m_mode == eVectorFromMatrixMode.Row )
				{
					result = "float3x3( " + m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector ) + ", "
					+ m_inputPorts[ 1 ].GeneratePortInstructions( ref dataCollector ) + ", "
					+ m_inputPorts[ 2 ].GeneratePortInstructions( ref dataCollector ) + " )";
				}
				else
				{
					string vec0 = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
					string vec1 = m_inputPorts[ 1 ].GeneratePortInstructions( ref dataCollector );
					string vec2 = m_inputPorts[ 2 ].GeneratePortInstructions( ref dataCollector );
					result = string.Format( "float3x3( {0}.x, {1}.x, {2}.x, {0}.y, {1}.y, {2}.y, {0}.z, {1}.z, {2}.z )", vec0, vec1, vec2 );
				}
				break;
				case WirePortDataType.FLOAT4x4:
				if( m_mode == eVectorFromMatrixMode.Row )
				{
					result = "float4x4( " + m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector ) + ", "
					+ m_inputPorts[ 1 ].GeneratePortInstructions( ref dataCollector ) + ", "
					+ m_inputPorts[ 2 ].GeneratePortInstructions( ref dataCollector ) + ", "
					+ m_inputPorts[ 3 ].GeneratePortInstructions( ref dataCollector ) + " )";
				}
				else
				{
					string vec0 = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
					string vec1 = m_inputPorts[ 1 ].GeneratePortInstructions( ref dataCollector );
					string vec2 = m_inputPorts[ 2 ].GeneratePortInstructions( ref dataCollector );
					string vec3 = m_inputPorts[ 3 ].GeneratePortInstructions( ref dataCollector );
					result = string.Format( "float4x4( {0}.x, {1}.x, {2}.x, {3}.x, {0}.y, {1}.y, {2}.y, {3}.y, {0}.z, {1}.z, {2}.z, {3}.z, {0}.w, {1}.w, {2}.w, {3}.w )", vec0, vec1, vec2, vec3 );
				}
				break;
			}

			return result;
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			m_selectedOutputType = (WirePortDataType)Enum.Parse( typeof( WirePortDataType ), GetCurrentParam( ref nodeParams ) );

			if ( UIUtils.CurrentShaderVersion() > 19900 )
			{
				m_mode = ( eVectorFromMatrixMode )Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			}
			else if( UIUtils.CurrentShaderVersion() > 15310 )
			{
				bool rowsFromVector = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
				m_mode = rowsFromVector ? eVectorFromMatrixMode.Row : eVectorFromMatrixMode.Column;
			}
			switch ( m_selectedOutputType )
			{
				case WirePortDataType.FLOAT2x2:
					m_selectedOutputTypeInt = 0;
					break;
				case WirePortDataType.FLOAT3x3:
					m_selectedOutputTypeInt = 1;
					break;
				case WirePortDataType.FLOAT4x4:
					m_selectedOutputTypeInt = 2;
					break;
			}
			UpdatePorts();
			UpdateSubtitle();
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_selectedOutputType );
			IOUtils.AddFieldValueToString( ref nodeInfo, ( int )m_mode );
		}
	}
}

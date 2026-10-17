using System;
using UnityEngine;
using UnityEditor;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Matrix Split", "Matrix Operators", "Split a matrix into vectors" )]
	public sealed class MatrixSplit : ParentNode
	{
		[SerializeField]
		private eVectorFromMatrixMode m_mode = eVectorFromMatrixMode.Row;

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( WirePortDataType.FLOAT4x4, false, Constants.EmptyPortValue );
			m_inputPorts[ 0 ].CreatePortRestrictions( WirePortDataType.FLOAT2x2, WirePortDataType.FLOAT3x3, WirePortDataType.FLOAT4x4 );
			AddOutputPort( WirePortDataType.FLOAT4, "[0]" );
			AddOutputPort( WirePortDataType.FLOAT4, "[1]" );
			AddOutputPort( WirePortDataType.FLOAT4, "[2]" );
			AddOutputPort( WirePortDataType.FLOAT4, "[3]" );
			m_useInternalPortData = true;
			m_autoWrapProperties = true;
			UpdateSubtitle();
		}

		private void UpdateSubtitle()
		{
			string type;
			switch ( m_inputPorts[ 0 ].DataType )
			{
				case WirePortDataType.FLOAT2x2: type = "2x2"; break;
				case WirePortDataType.FLOAT3x3: type = "3x3"; break;
				default: type = "4x4"; break;
			}

			SetAdditonalTitleText( string.Format( "{0}, {1}", type, m_mode ) );
		}

		public override void OnInputPortConnected( int portId, int otherNodeId, int otherPortId, bool activateNode = true )
		{
			base.OnInputPortConnected( portId, otherNodeId, otherPortId, activateNode );
			UpdatePorts();
		}

		public override void OnConnectedOutputNodeChanges( int inputPortId, int otherNodeId, int otherPortId, string name, WirePortDataType type )
		{
			base.OnConnectedOutputNodeChanges( inputPortId, otherNodeId, otherPortId, name, type );
			UpdatePorts();
		}

		void UpdatePorts()
		{
			m_inputPorts[ 0 ].MatchPortToConnection();

			WirePortDataType type;
			int visibleCount;

			if ( m_inputPorts[ 0 ].DataType == WirePortDataType.FLOAT2x2 )
			{
				type = WirePortDataType.FLOAT2;
				visibleCount = 2;
			}
			else if ( m_inputPorts[ 0 ].DataType == WirePortDataType.FLOAT3x3 )
			{
				type = WirePortDataType.FLOAT3;
				visibleCount = 3;
			}
			else
			{
				type = WirePortDataType.FLOAT4;
				visibleCount = 4;
			}

			for ( int i = 0; i < m_outputPorts.Count; i++ )
			{
				m_outputPorts[ i ].ChangeType( type, false );
				m_outputPorts[ i ].Visible = ( i < visibleCount );
			}

			m_sizeIsDirty = true;
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			string value = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
			if ( !m_inputPorts[ 0 ].DataTypeIsMatrix )
			{
				value = UIUtils.CastPortType( ref dataCollector, CurrentPrecisionType, value, m_inputPorts[ 0 ].DataType, WirePortDataType.FLOAT4x4, value );
			}

			if ( m_mode == eVectorFromMatrixMode.Row )
			{
				value += "[ " + outputId + " ]";
			}
			else
			{
				string formatStr = value + "[ {0} ]" + "[ " + outputId + " ]";
				int count = 4;
				if ( m_inputPorts[ 0 ].DataType == WirePortDataType.FLOAT4x4 )
				{
					value = "float4( ";
				}
				else if ( m_inputPorts[ 0 ].DataType == WirePortDataType.FLOAT3x3 )
				{
					count = 3;
					value = "float3( ";
				}
				else if ( m_inputPorts[ 0 ].DataType == WirePortDataType.FLOAT2x2 )
				{
					count = 2;
					value = "float2( ";
				}

				for ( int i = 0; i < count; i++ )
				{
					value += string.Format( formatStr, i );
					if ( i != ( count - 1 ) )
					{
						value += ", ";
					}
				}
				value += " )";
			}
			return value;
		}

		public override void DrawProperties()
		{
			EditorGUI.BeginChangeCheck();
			m_mode = (eVectorFromMatrixMode)EditorGUILayoutEnumPopup( "Mode", m_mode );
			if ( EditorGUI.EndChangeCheck() )
			{
				UpdateSubtitle();
			}
			base.DrawProperties();
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_mode );
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			m_mode = ( eVectorFromMatrixMode ) Enum.Parse( typeof( eVectorFromMatrixMode ), GetCurrentParam( ref nodeParams ) );
			UpdateSubtitle();
		}
	}
}

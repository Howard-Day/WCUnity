// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using UnityEditor;
using System;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Flip", "Vector Operators", "Flip value sign on specific channels from vectors or color components.", tags: "invert sign" )]
	public sealed class FlipNode : ParentNode
	{
		[SerializeField]
		private bool[] m_selection = { true, true, true, true };

		[SerializeField] 
		private string[] m_labels;

		private static readonly int _Flip = Shader.PropertyToID( "_Flip" );
		private static readonly int _Count = Shader.PropertyToID( "_Count" );

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( WirePortDataType.FLOAT4, false, Constants.EmptyPortValue );
			AddOutputPort( WirePortDataType.FLOAT4, Constants.EmptyPortValue );

			m_inputPorts[ 0 ].CreatePortRestrictions(	WirePortDataType.FLOAT,
														WirePortDataType.FLOAT2,
														WirePortDataType.FLOAT3,
														WirePortDataType.FLOAT4,
														WirePortDataType.COLOR,
														WirePortDataType.INT );

			m_useInternalPortData = true;
			m_autoWrapProperties = true;
			m_selectedLocation = PreviewLocation.TopCenter;
			m_labels = new string[] { "X", "Y", "Z", "W" };
			m_previewShaderGUID = "99b235eb03070cd4ab7470cda5a77e2d";
			SetAdditonalTitleText( "Value( XYZW )" );
		}

		public override void SetPreviewInputs()
		{
			base.SetPreviewInputs();

			var dataType = m_inputPorts[ 0 ].DataType;
			var flip = new Vector4( 0, 0, 0, 0 );
			int count = 1;

			flip.x = m_selection[ 0 ] ? 1 : 0;
			if ( dataType >= WirePortDataType.FLOAT2 )
			{
				flip.y = m_selection[ 1 ] ? 1 : 0;
				count++;
			}
			if ( dataType >= WirePortDataType.FLOAT3 )
			{
				flip.z = m_selection[ 2 ] ? 1 : 0;
				count++;
			}
			if ( dataType == WirePortDataType.FLOAT4 || dataType == WirePortDataType.COLOR )
			{
				flip.w = m_selection[ 3 ] ? 1 : 0;
				count++;
			}

			PreviewMaterial.SetVector( _Flip, flip );
			PreviewMaterial.SetInt( _Count, count );
		}

		public override void OnInputPortConnected( int portId, int otherNodeId, int otherPortId, bool activateNode = true )
		{
			base.OnInputPortConnected( portId, otherNodeId, otherPortId, activateNode );
			m_inputPorts[ 0 ].MatchPortToConnection();
			m_outputPorts[ 0 ].ChangeType( InputPorts[ 0 ].DataType, false );
			UpdateTitle();
		}

		public override void OnConnectedOutputNodeChanges( int outputPortId, int otherNodeId, int otherPortId, string name, WirePortDataType type )
		{
			base.OnConnectedOutputNodeChanges( outputPortId, otherNodeId, otherPortId, name, type );
			m_inputPorts[ 0 ].MatchPortToConnection();
			m_outputPorts[ 0 ].ChangeType( InputPorts[ 0 ].DataType, false );
			UpdateTitle();
		}

		public override void OnInputPortDisconnected( int portId )
		{
			base.OnInputPortDisconnected( portId );
			UpdateTitle();
		}

		private void UpdateTitle()
		{
			var dataType = m_inputPorts[ 0 ].DataType;
			var additionalText = ( m_selection[ 0 ] ? "1" : "0" );
			if ( dataType >= WirePortDataType.FLOAT2 )
			{
				additionalText += ", " + ( m_selection[ 1 ] ? "1" : "0" );
			}
			if ( dataType >= WirePortDataType.FLOAT3 )
			{
				additionalText += ", " + ( m_selection[ 2 ] ? "1" : "0" );
			}
			if ( ( dataType == WirePortDataType.FLOAT4 || dataType == WirePortDataType.COLOR ) )
			{
				additionalText += ", " + ( m_selection[ 3 ] ? "1" : "0" );
			}
			SetAdditonalTitleText( ( additionalText.Length > 0 ) ? "Value( " + additionalText + " )" : string.Empty );
		}

		public override void DrawProperties()
		{
			base.DrawProperties();

			EditorGUILayout.BeginVertical();

			int count = 0;
			switch ( m_inputPorts[ 0 ].DataType )
			{
				case WirePortDataType.FLOAT4:
				case WirePortDataType.OBJECT:
				case WirePortDataType.COLOR: count = 4; break;
				case WirePortDataType.FLOAT3: count = 3; break;
				case WirePortDataType.FLOAT2: count = 2; break;
				case WirePortDataType.FLOAT:
				case WirePortDataType.INT: count = 1; break;
				case WirePortDataType.FLOAT3x3:
				case WirePortDataType.FLOAT4x4: break;
			}

			if ( count > 0 )
			{
				for ( int i = 0; i < count; i++ )
				{
					m_selection[ i ] = EditorGUILayoutToggleLeft( m_labels[ i ], m_selection[ i ] );
					m_labels[ i ] = UIUtils.GetComponentForPosition( i, m_inputPorts[ 0 ].DataType ).ToUpper();
				}
			}

			EditorGUILayout.EndVertical();
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalVar )
		{
			if ( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
			{
				return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );
			}
			
			string inputValue = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
			string outputType = UIUtils.PrecisionWirePortToCgType( CurrentPrecisionType, m_outputPorts[ 0 ].DataType );

			var dataType = m_inputPorts[ 0 ].DataType;
			string flip = outputType + "( " + ( m_selection[ 0 ] ? "1" : "0" );

			if ( dataType >= WirePortDataType.FLOAT2 )
			{
				flip += ", " + ( m_selection[ 1 ] ? "1" : "0" );
			}
			if ( dataType >= WirePortDataType.FLOAT3 )
			{
				flip += ", " + ( m_selection[ 2 ] ? "1" : "0" );
			}
			if ( ( dataType == WirePortDataType.FLOAT4 || dataType == WirePortDataType.COLOR ) )
			{
				flip += ", " + ( m_selection[ 3 ] ? "1" : "0" );
			}
			flip += " )";

			string result = string.Format( "( ( {0} * -2 + 1 ) * {1} )", flip, inputValue );
			return CreateOutputLocalVariable( 0, result, ref dataCollector );
		}

		public string GetComponentForPosition( int i )
		{
			switch ( i )
			{
				case 0: return ( ( m_outputPorts[ 0 ].DataType == WirePortDataType.COLOR ) ? "r" : "x" );
				case 1: return ( ( m_outputPorts[ 0 ].DataType == WirePortDataType.COLOR ) ? "g" : "y" );
				case 2: return ( ( m_outputPorts[ 0 ].DataType == WirePortDataType.COLOR ) ? "b" : "z" );
				case 3: return ( ( m_outputPorts[ 0 ].DataType == WirePortDataType.COLOR ) ? "a" : "w" );
			}
			return string.Empty;
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			for ( int i = 0; i < 4; i++ )
			{
				m_selection[ i ] = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
			}
			UpdateTitle();
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			for ( int i = 0; i < 4; i++ )
			{
				IOUtils.AddFieldValueToString( ref nodeInfo, m_selection[ i ] );
			}
		}
	}
}

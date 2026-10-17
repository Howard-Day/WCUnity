// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;

using UnityEngine;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Switch by Face", "Miscellaneous", "Switch which automaticaly uses a Face variable to select which input to use" )]
	public class SwitchByFaceNode : DynamicTypeNode
	{
		private const string SwitchOp = "((({0}>0)?({1}):({2})))";
		private const string NamelessStructError = "Switch by Face node cannot declare a struct whose name did not reach its ports.";

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			m_inputPorts[ 0 ].Name = "Front";
			m_inputPorts[ 1 ].Name = "Back";
			// @diogo: DynamicTypeNode's whitelist excludes STRUCT; add it back just for Front/Back
			m_inputPorts[ 0 ].AddPortRestrictions( WirePortDataType.STRUCT );
			m_inputPorts[ 1 ].AddPortRestrictions( WirePortDataType.STRUCT );
			m_textLabelWidth = 50;
			m_previewShaderGUID = "f4edf6febb54dc743b25bd5b56facea8";
		}

		public override StructFieldMask FieldDemandForInput( int inputIndex, StructFieldMask received )
		{
			return received;
		}

		public override void OnConnectedOutputNodeChanges( int inputPortId, int otherNodeId, int otherPortId, string name, WirePortDataType type )
		{
			base.OnConnectedOutputNodeChanges( inputPortId, otherNodeId, otherPortId, name, type );
			UpdateStructConnection();
		}

		public override void OnInputPortConnected( int portId, int otherNodeId, int otherPortId, bool activateNode = true )
		{
			base.OnInputPortConnected( portId, otherNodeId, otherPortId, activateNode );
			UpdateStructConnection();
		}

		public override void OnInputPortDisconnected( int portId )
		{
			base.OnInputPortDisconnected( portId );
			UpdateStructConnection();
		}

		// @diogo: DynamicTypeNode resolves connected ports by priority and STRUCT ranks lowest, so a wired
		// struct never wins there; here it decides the type outright instead, on both ports and the
		// output, and drops back to FLOAT once no struct is left wired
		private void UpdateStructConnection()
		{
			string structName = string.Empty;
			bool hasStruct = false;

			for ( int i = 0; i < 2; i++ )
			{
				if ( m_inputPorts[ i ].IsConnected )
				{
					WirePort source = m_inputPorts[ i ].GetOutputConnection( 0 );
					if ( source.DataType == WirePortDataType.STRUCT )
					{
						structName = source.StructName;
						hasStruct = true;
						break;
					}
				}
			}

			if ( hasStruct )
			{
				m_inputPorts[ 0 ].ChangeType( WirePortDataType.STRUCT, structName, false );
				m_inputPorts[ 1 ].ChangeType( WirePortDataType.STRUCT, structName, false );
				m_outputPorts[ 0 ].ChangeType( WirePortDataType.STRUCT, structName, false );
			}
			else if ( m_outputPorts[ 0 ].DataType == WirePortDataType.STRUCT )
			{
				m_inputPorts[ 0 ].ChangeType( WirePortDataType.FLOAT, string.Empty, false );
				m_inputPorts[ 1 ].ChangeType( WirePortDataType.FLOAT, string.Empty, false );
				m_outputPorts[ 0 ].ChangeType( WirePortDataType.FLOAT, string.Empty, false );
			}
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if ( ReportStructMix( 0, 1 ) )
			{
				return m_outputPorts[ 0 ].ErrorValue;
			}

			if ( dataCollector.PortCategory == MasterNodePortCategory.Tessellation )
			{
				UIUtils.ShowMessage( UniqueId, m_nodeAttribs.Name + " does not work on Tessellation port" );
				return GenerateErrorValue();
			}

			if ( dataCollector.PortCategory == MasterNodePortCategory.Vertex )
			{
				if ( dataCollector.TesselationActive )
				{
					UIUtils.ShowMessage( UniqueId, m_nodeAttribs.Name + " does not work properly on Vertex/Tessellation ports" );
					return GenerateErrorValue();
				}
				else
				{
					UIUtils.ShowMessage( UniqueId , FaceVariableNode.FaceOnVertexWarning , MessageSeverity.Warning );
					string faceVariable = GeneratorUtils.GenerateVertexFace( ref dataCollector , UniqueId );

					if( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
						return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );

					string frontValue = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
					string backValue = m_inputPorts[ 1 ].GeneratePortInstructions( ref dataCollector );

					if ( m_outputPorts[ 0 ].DataType == WirePortDataType.STRUCT )
					{
						return GenerateStructSwitch( faceVariable, frontValue, backValue, ref dataCollector );
					}

					string finalResult = string.Format( SwitchOp , faceVariable , frontValue , backValue );
					RegisterLocalVariable( 0 , finalResult , ref dataCollector , "switchResult" + OutputId );
					return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );
				}
			}

			if ( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
				return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );

			string front = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
			string back = m_inputPorts[ 1 ].GeneratePortInstructions( ref dataCollector );

			if ( dataCollector.CurrentCanvasMode == NodeAvailability.TemplateShader )
			{
				dataCollector.AddToInput( UniqueId, SurfaceInputs.FRONT_FACE );
			}
			else
			{
				dataCollector.AddToInput( UniqueId, SurfaceInputs.FRONT_FACE_VFACE );
			}

			string variable = string.Empty;
			if ( dataCollector.IsTemplate )
			{
				variable = dataCollector.TemplateDataCollectorInstance.GetVFace( UniqueId );
			}
			else
			{
				variable = ( ( dataCollector.PortCategory == MasterNodePortCategory.Vertex ) ? Constants.VertexShaderOutputStr : Constants.InputVarStr ) + "." + Constants.IsFrontFaceVariable;
			}

			if ( m_outputPorts[ 0 ].DataType == WirePortDataType.STRUCT )
			{
				return GenerateStructSwitch( variable, front, back, ref dataCollector );
			}

			string value = string.Format( SwitchOp, variable, front, back );
			RegisterLocalVariable( 0, value, ref dataCollector, "switchResult" + OutputId );
			return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );
		}

		// @diogo: HLSL has no ternary on a struct, so it takes an if/else assignment into a local instead,
		// with the same face>0 ? front : back semantics as SwitchOp
		private string GenerateStructSwitch( string faceVariable, string frontValue, string backValue, ref MasterNodeDataCollector dataCollector )
		{
			string structName = m_outputPorts[ 0 ].StructName;
			if ( string.IsNullOrEmpty( structName ) )
			{
				UIUtils.ShowMessage( UniqueId, NamelessStructError, MessageSeverity.Error );
				return m_outputPorts[ 0 ].ErrorValue;
			}

			string varName = "switchResult" + OutputId;
			dataCollector.AddLocalVariable( UniqueId, structName + " " + varName + " = " + backValue + ";", true );
			dataCollector.AddLocalVariable( UniqueId, "if( " + faceVariable + " > 0 )", true );
			dataCollector.AddLocalVariable( UniqueId, "\t" + varName + " = " + frontValue + ";", true );
			m_outputPorts[ 0 ].SetLocalValue( varName, dataCollector.PortCategory );
			return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );
		}
	}
}

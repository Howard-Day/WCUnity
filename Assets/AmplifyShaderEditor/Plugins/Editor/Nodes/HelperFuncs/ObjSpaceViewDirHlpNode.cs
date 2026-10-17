// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEngine;
using UnityEditor;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Object Space View Dir", "Object Transform", "Object space direction (not normalized) from given object space vertex position towards the camera",castType:null, shortcutKey:KeyCode.None, available:false, deprecated:true )]
	public sealed class ObjSpaceViewDirHlpNode : HelperParentNode
	{
		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			m_funcType = "ObjSpaceViewDir";
			//TODO: revisit this later
			m_funcLWFormatOverride = "( mul(GetWorldToObjectMatrix(), float4(_WorldSpaceCameraPos.xyz, 1)).xyz - {0}.xyz )";
			m_funcHDFormatOverride = "( mul(GetWorldToObjectMatrix(), float4(_WorldSpaceCameraPos.xyz, 1)).xyz - {0}.xyz )";
			m_inputPorts[ 0 ].ChangeType( WirePortDataType.FLOAT4, false );
			m_inputPorts[ 0 ].Vector4InternalData = new UnityEngine.Vector4( 0, 0, 0, 1 );
			m_outputPorts[ 0 ].ChangeType( WirePortDataType.FLOAT3, false );
			m_outputPorts[ 0 ].Name = "XYZ";
			AddOutputPort( WirePortDataType.FLOAT, "X" );
			AddOutputPort( WirePortDataType.FLOAT, "Y" );
			AddOutputPort( WirePortDataType.FLOAT, "Z" );
			m_previewShaderGUID = "c7852de24cec4a744b5358921e23feee";
			
			m_errorMessageTooltip = "This node still works but is now deprecated. Please use \"View Dir\" instead.";
			m_errorMessageTypeIsError = NodeMessageType.Warning;
			m_showErrorMessage = true;
		}

		protected override void OnUniqueIDAssigned()
		{
			base.OnUniqueIDAssigned();
			m_localVarName = "objectSpaceViewDir" + OutputId;
		}

		public override void DrawProperties()
		{
			base.DrawProperties();
			if ( m_showErrorMessage )
			{
				EditorGUILayout.HelpBox( m_errorMessageTooltip, MessageType.Warning );
			}
		}
	}
}

// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using System;
namespace AmplifyShaderEditor
{
	[Serializable]
	public class ConstVecShaderVariable : ShaderVariablesNode
	{
		[SerializeField]
		protected string m_value;
		protected string m_valueHDRP;
		protected string m_valueURP;
		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			ChangeOutputProperties( 0, " ", WirePortDataType.FLOAT4 );
			AddOutputPort( WirePortDataType.FLOAT, "0" );
			AddOutputPort( WirePortDataType.FLOAT, "1" );
			AddOutputPort( WirePortDataType.FLOAT, "2" );
			AddOutputPort( WirePortDataType.FLOAT, "3" );
		}
		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			base.GenerateShaderForOutput( outputId, ref dataCollector, ignoreLocalvar );

			string value = m_value;
			if ( dataCollector.IsTemplate )
			{
				if( dataCollector.TemplateDataCollectorInstance.CurrentSRPType == TemplateSRPType.HDRP && !string.IsNullOrEmpty( m_valueHDRP ) )
				{
					value = m_valueHDRP;
				}
				else if ( dataCollector.TemplateDataCollectorInstance.CurrentSRPType == TemplateSRPType.URP && !string.IsNullOrEmpty( m_valueURP ) )
				{
					value = m_valueURP;
				}
			}

			switch ( outputId )
			{
				case 0: return value;
				case 1: return ( value + ".x" );
				case 2: return ( value + ".y" );
				case 3: return ( value + ".z" );
				case 4: return ( value + ".w" );
			}

			UIUtils.ShowMessage( UniqueId, "ConstVecShaderVariable generating empty code", MessageSeverity.Warning );
			return string.Empty;
		}

	}
}

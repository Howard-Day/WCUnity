// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEngine;

namespace AmplifyShaderEditor
{
	[Serializable]
	public class ConstantShaderVariable : ShaderVariablesNode
	{
		[SerializeField]
		protected string m_value;

		[SerializeField]
		protected string m_valueHDRP = string.Empty;

		[SerializeField]
		protected string m_valueURP = string.Empty;

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			base.GenerateShaderForOutput( outputId, ref dataCollector, ignoreLocalvar );
			if( dataCollector.IsTemplate )
			{
				if( dataCollector.TemplateDataCollectorInstance.CurrentSRPType == TemplateSRPType.HDRP && !string.IsNullOrEmpty( m_valueHDRP ) )
					return m_valueHDRP;

				if( dataCollector.TemplateDataCollectorInstance.CurrentSRPType == TemplateSRPType.URP && !string.IsNullOrEmpty( m_valueURP ))
					return m_valueURP;
			}
			return m_value;
		}
	}
}

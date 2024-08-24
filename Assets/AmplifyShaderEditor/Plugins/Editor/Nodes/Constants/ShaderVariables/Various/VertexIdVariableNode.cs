// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Vertex ID", "Vertex Data", "Indicates current vertex number" )]
	public class VertexIdVariableNode : ParentNode
	{
		private const string VertexIdRegistry = "uint {0} : SV_VertexID;";

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddOutputPort( WirePortDataType.INT, "Out" );
			m_previewShaderGUID = "5934bf2c10b127a459177a3b622cea65";
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if ( dataCollector.PortCategory == MasterNodePortCategory.Tessellation )
			{
				UIUtils.ShowMessage( UniqueId, m_nodeAttribs.Name + " does not work on Tessellation port" );
				return m_outputPorts[0].ErrorValue;
			}

			if ( dataCollector.IsTemplate )
			{
				return dataCollector.TemplateDataCollectorInstance.GetVertexId();
			}
			else
			{
				string name = TemplateHelperFunctions.SemanticsDefaultName[ TemplateSemantics.SV_VertexID ];
				if ( dataCollector.IsFragmentCategory )
				{
					GenerateValueInVertex( ref dataCollector, WirePortDataType.UINT, Constants.VertexShaderInputStr + "." + name, name, true );
					return Constants.InputVarStr + "." + name;
				}
				else
				{
					return Constants.VertexShaderInputStr + "." + name;
				}
			}
		}
		public override void PropagateNodeData( NodeData nodeData, ref MasterNodeDataCollector dataCollector )
		{
			if( !dataCollector.IsTemplate )
			{
				string name = TemplateHelperFunctions.SemanticsDefaultName[ TemplateSemantics.SV_VertexID ];
				dataCollector.AddCustomAppData( string.Format( VertexIdRegistry, name ) );
			}

			base.PropagateNodeData( nodeData, ref dataCollector );
		}
	}
}

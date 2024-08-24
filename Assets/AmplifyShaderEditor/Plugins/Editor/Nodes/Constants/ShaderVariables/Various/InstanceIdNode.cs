// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEngine;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Instance ID", "Vertex Data", "Indicates the per-instance identifier" )]
	public class InstanceIdNode : ParentNode
	{
		private readonly string[] InstancingVariableAttrib =
		{   "uint currInstanceId = 0;",
			"#if defined(UNITY_INSTANCING_ENABLED) || defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)",
			"currInstanceId = unity_InstanceID;",
			"#endif"
		};

		private const string InstanceIdRegistry = "uint {0} : SV_InstanceID;";

		private const string TemplateSVInstanceIdVar = "instanceID";
		private const string InstancingInnerVariable = "currInstanceId";
		private bool m_useSVSemantic = false;
		private bool m_procedural = false;

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddOutputPort( WirePortDataType.INT, "Out" );
			m_previewShaderGUID = "03febce56a8cf354b90e7d5180c1dbd7";
		}

		public override void OnNodeLogicUpdate( DrawInfo drawInfo )
		{
			base.OnNodeLogicUpdate( drawInfo );
			m_autoWrapProperties = !m_containerGraph.IsStandardSurface;
		}

		public override void DrawProperties()
		{
			base.DrawProperties();

			if( !m_containerGraph.IsStandardSurface )
			{
				m_useSVSemantic = EditorGUILayoutToggle( "Use SV semantic" , m_useSVSemantic );
			}

			m_procedural = EditorGUILayoutToggle( "Procedural", m_procedural );
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if ( dataCollector.PortCategory == MasterNodePortCategory.Tessellation )
			{
				UIUtils.ShowMessage( UniqueId, m_nodeAttribs.Name + " does not work on Tessellation port" );
				return m_outputPorts[ 0 ].ErrorValue;
			}

			if ( m_procedural )
			{
				if ( dataCollector.IsSRP && ( dataCollector.CurrentPassName.Contains( "Forward" ) || dataCollector.CurrentPassName.Contains( "GBuffer" ) ) )
				{
					dataCollector.AddToPragmas( UniqueId, "instancing_options renderinglayer procedural:ASEProceduralSetup" );
				}
				else
				{
					dataCollector.AddToPragmas( UniqueId, "instancing_options procedural:ASEProceduralSetup" );
				}
				dataCollector.AddToPragmas( UniqueId, "multi_compile_instancing" );
				dataCollector.AddFunction( "ASEProceduralSetup()", "void ASEProceduralSetup() { }" );
			}

			if ( dataCollector.IsTemplate )
			{
				dataCollector.TemplateDataCollectorInstance.SetupInstancing();
				if ( m_useSVSemantic )
				{
					return dataCollector.TemplateDataCollectorInstance.GetInstanceId();
				}
			}
			else
			{
				string name = TemplateHelperFunctions.SemanticsDefaultName[ TemplateSemantics.SV_InstanceID ];
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
			
			if( !dataCollector.HasLocalVariable( InstancingVariableAttrib[ 0 ] ) )
			{
				dataCollector.AddLocalVariable( UniqueId, InstancingVariableAttrib[ 0 ] ,true );
				dataCollector.AddLocalVariable( UniqueId, InstancingVariableAttrib[ 1 ] ,true );
				dataCollector.AddLocalVariable( UniqueId, InstancingVariableAttrib[ 2 ] ,true );
				dataCollector.AddLocalVariable( UniqueId, InstancingVariableAttrib[ 3 ] ,true );
			}
			return InstancingInnerVariable;
		}

		public override void PropagateNodeData( NodeData nodeData, ref MasterNodeDataCollector dataCollector )
		{
			if ( !dataCollector.IsTemplate )
			{
				string name = TemplateHelperFunctions.SemanticsDefaultName[ TemplateSemantics.SV_InstanceID ];
				dataCollector.AddCustomAppData( string.Format( InstanceIdRegistry, name ) );
			}

			base.PropagateNodeData( nodeData, ref dataCollector );
		}
	
		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			if( UIUtils.CurrentShaderVersion() > 18915 )
			{
				m_useSVSemantic = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
			}
			if ( UIUtils.CurrentShaderVersion() >= 19500 )
			{
				m_procedural = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
			}
		}

		public override void WriteToString( ref string nodeInfo , ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo , ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo , m_useSVSemantic );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_procedural );
		}
	}
}

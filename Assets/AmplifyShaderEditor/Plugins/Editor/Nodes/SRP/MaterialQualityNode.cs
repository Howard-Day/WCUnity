// Amplify Shader Editor - Visual Shader vEditing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using UnityEditor;
using System;

namespace AmplifyShaderEditor
{
	[NodeAttributes( "Material Quality", "Logical Operators", "Choose between separate branches according to currently selected Quality (SRP only) ", Available = true )]
	public class MaterialQualityNode : ParentNode
	{
		private const string SRPError = "Node intended to be used only on SRP templates as it makes use of keywords defined over that environment.";

		private const string MaxKeyword = "MATERIAL_QUALITY_HIGH";
		private const string MedKeyword = "MATERIAL_QUALITY_MEDIUM";
		private const string MinKeyword = "MATERIAL_QUALITY_LOW";
		private const string MaterialPragmas = "#pragma multi_compile " + MaxKeyword + " " + MedKeyword + " " + MinKeyword;
		private readonly string[] MaterialCode =
		{
			"#if defined("+MaxKeyword+")",
			"#elif defined("+MedKeyword+")",
			"#else",
			"#endif"
		};
		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( WirePortDataType.FLOAT, false, "High" );
			AddInputPort( WirePortDataType.FLOAT, false, "Medium" );
			AddInputPort( WirePortDataType.FLOAT, false, "Low" );

			// @diogo: a texture object is bound, not assigned, so no quality arm can reach the result local
			for ( int i = 0; i < m_inputPorts.Count; i++ )
			{
				m_inputPorts[ i ].AddPortForbiddenTypes( WirePortDataType.SAMPLER1D, WirePortDataType.SAMPLER2D, WirePortDataType.SAMPLER3D,
					WirePortDataType.SAMPLERCUBE, WirePortDataType.SAMPLER2DARRAY, WirePortDataType.SAMPLERSTATE );
			}

			AddOutputPort( WirePortDataType.FLOAT, Constants.EmptyPortValue );
			m_errorMessageTypeIsError = NodeMessageType.Error;
			m_errorMessageTooltip = SRPError;
			m_previewShaderGUID = "06e8f551333d4c53b98ff12d11470868";
		}

		public override void OnNodeLogicUpdate( DrawInfo drawInfo )
		{
			base.OnNodeLogicUpdate( drawInfo );
			m_showErrorMessage = ( ContainerGraph.CurrentCanvasMode == NodeAvailability.SurfaceShader ) ||
									( ContainerGraph.CurrentCanvasMode == NodeAvailability.TemplateShader && !ContainerGraph.IsSRP );
		}
		public override void OnInputPortConnected( int portId , int otherNodeId , int otherPortId , bool activateNode = true )
		{
			base.OnInputPortConnected( portId , otherNodeId , otherPortId , activateNode );
			UpdateConnections();
		}

		public override void OnConnectedOutputNodeChanges( int inputPortId , int otherNodeId , int otherPortId , string name , WirePortDataType type )
		{
			base.OnConnectedOutputNodeChanges( inputPortId , otherNodeId , otherPortId , name , type );
			UpdateConnections();
		}

		public override void OnInputPortDisconnected( int portId )
		{
			base.OnInputPortDisconnected( portId );
			UpdateConnections();
		}

		private void UpdateConnections()
		{
			WirePortDataType mainType = WirePortDataType.FLOAT;

			int highest = UIUtils.GetPriority( mainType );
			for( int i = 0 ; i < m_inputPorts.Count ; i++ )
			{
				if( m_inputPorts[ i ].IsConnected )
				{
					WirePortDataType portType = m_inputPorts[ i ].GetOutputConnection().DataType;
					if( UIUtils.GetPriority( portType ) > highest )
					{
						mainType = portType;
						highest = UIUtils.GetPriority( portType );
					}
				}
			}

			for( int i = 0 ; i < m_inputPorts.Count ; i++ )
			{
				m_inputPorts[ i ].ChangeType( mainType , false );
			}

			m_outputPorts[ 0 ].ChangeType( mainType , false );
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
				return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );

			dataCollector.AddToDirectives( MaterialPragmas );

			// @diogo: same preprocessor-arm treatment as Switch: analyze every arm first, pre-generate what a
			// scoped one shares, generate the rest flat; nothing scoped keeps the legacy text (#674, #443)
			BranchArmAnalysis[] arms = new BranchArmAnalysis[ m_inputPorts.Count ];
			bool anyScoped = false;
			for ( int i = 0; i < m_inputPorts.Count; i++ )
			{
				arms[ i ] = BranchScopeUtils.AnalyzeArm( this, m_inputPorts[ i ], ref dataCollector, divergentFlow: false );
				anyScoped = anyScoped || arms[ i ].Scoped;
			}

			string[] qualityValues = new string[ m_inputPorts.Count ];
			for ( int i = 0; i < m_inputPorts.Count; i++ )
			{
				if ( arms[ i ].Scoped )
				{
					BranchScopeUtils.PreGenerateFrontier( arms[ i ], ref dataCollector );
				}
				else
				{
					qualityValues[ i ] = m_inputPorts[ i ].GeneratePortInstructions( ref dataCollector );
				}
			}

			string localVarName = "currQuality" + OutputId;
			dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, m_outputPorts[ 0 ].DataType, localVarName, "0" );

			//High
			dataCollector.AddLocalVariable( UniqueId, MaterialCode[ 0 ], true );
			EmitArm( arms[ 0 ], 0, localVarName, qualityValues[ 0 ], anyScoped, "defined( " + MaxKeyword + " )", ref dataCollector );

			//Medium
			dataCollector.AddLocalVariable( UniqueId, MaterialCode[ 1 ], true );
			EmitArm( arms[ 1 ], 1, localVarName, qualityValues[ 1 ], anyScoped, "defined( " + MedKeyword + " )", ref dataCollector );

			//Low
			dataCollector.AddLocalVariable( UniqueId, MaterialCode[ 2 ], true );
			EmitArm( arms[ 2 ], 2, localVarName, qualityValues[ 2 ], anyScoped, "!defined( " + MaxKeyword + " ) && !defined( " + MedKeyword + " )", ref dataCollector );
			m_outputPorts[ 0 ].SetLocalValue( localVarName, dataCollector.PortCategory );

			dataCollector.AddLocalVariable( UniqueId, MaterialCode[ 3 ], true );
			return localVarName;
		}

		private void EmitArm( BranchArmAnalysis arm, int portIdx, string localVarName, string value, bool anyScoped, string condition, ref MasterNodeDataCollector dataCollector )
		{
			if ( anyScoped )
			{
				BranchScopeUtils.EmitBracedArm( UniqueId, arm, m_inputPorts[ portIdx ], localVarName, value, condition, ref dataCollector );
			}
			else
			{
				dataCollector.AddLocalVariable( UniqueId, localVarName, value, true, true );
			}
		}
	}
}

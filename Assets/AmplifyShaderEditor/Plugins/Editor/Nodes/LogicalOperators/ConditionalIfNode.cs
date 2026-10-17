// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using System;
using System.Collections.Generic;
//using UnityEditor;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "If", "Logical Operators", "Conditional comparison between A with B.",tags:"branch" )]
	public sealed class ConditionalIfNode : ParentNode
	{
		private const string UseUnityBranchesStr = "Dynamic Branching";
		private const string UnityBranchStr = "UNITY_BRANCH ";
		private const string NamelessStructError = "If node cannot declare a struct whose name did not reach its ports.";

		private readonly string[] IfOps = { "if( {0} > {1} )",
											"if( {0} == {1} )",
											"if( {0} < {1} )",
											"if( {0} >= {1} )",
											"if( {0} <= {1} )",
											"if( {0} != {1} )" };

		//private WirePortDataType m_inputMainDataType = WirePortDataType.FLOAT;
		private WirePortDataType m_outputMainDataType = WirePortDataType.FLOAT;
		private string[] m_results = { string.Empty, string.Empty, string.Empty };

		[SerializeField]
		private bool m_useUnityBranch = false;

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( WirePortDataType.FLOAT, false, "A" );
			AddInputPort( WirePortDataType.FLOAT, false, "B" );
			m_inputPorts[ 0 ].AddPortRestrictions( WirePortDataType.FLOAT, WirePortDataType.INT );
			m_inputPorts[ 1 ].AddPortRestrictions( WirePortDataType.FLOAT, WirePortDataType.INT );

			AddInputPort( WirePortDataType.FLOAT, false, "A > B" );
			AddInputPort( WirePortDataType.FLOAT, false, "A == B" );
			AddInputPort( WirePortDataType.FLOAT, false, "A < B" );

			// @diogo: a struct is assigned into the result local like any value, but a texture object is
			// bound, not assigned, so no branch can carry one
			for ( int i = 2; i < m_inputPorts.Count; i++ )
			{
				m_inputPorts[ i ].AcceptAllTypes();
				m_inputPorts[ i ].AddPortForbiddenTypes( WirePortDataType.SAMPLER1D, WirePortDataType.SAMPLER2D, WirePortDataType.SAMPLER3D,
					WirePortDataType.SAMPLERCUBE, WirePortDataType.SAMPLER2DARRAY, WirePortDataType.SAMPLERSTATE );
			}

			AddOutputPort( WirePortDataType.FLOAT, Constants.EmptyPortValue );
			m_inputPorts[ 0 ].AutoDrawInternalData = true;
			m_inputPorts[ 1 ].AutoDrawInternalData = true;
			m_textLabelWidth = 131;
			//m_useInternalPortData = true;
			m_autoWrapProperties = true;
			m_previewShaderGUID = "f6fb4d46bddf29e45a8a3ddfed75d0c0";
		}

		public override StructFieldMask FieldDemandForInput( int inputIndex, StructFieldMask received )
		{
			// @diogo: A and B are read whole; the three result ports are arms
			return ( inputIndex >= 2 ) ? received : StructFieldMask.All;
		}

		public override void OnConnectedOutputNodeChanges( int inputPortId, int otherNodeId, int otherPortId, string name, WirePortDataType type )
		{
			base.OnConnectedOutputNodeChanges( inputPortId, otherNodeId, otherPortId, name, type );
			UpdateConnection( inputPortId );
		}

		public override void OnInputPortConnected( int portId, int otherNodeId, int otherPortId, bool activateNode = true )
		{
			base.OnInputPortConnected( portId, otherNodeId, otherPortId, activateNode );
			UpdateConnection( portId );
		}

		public override void DrawProperties()
		{
			base.DrawProperties();
			if( !m_inputPorts[ 0 ].IsConnected )
				m_inputPorts[ 0 ].FloatInternalData = EditorGUILayoutFloatField( m_inputPorts[ 0 ].Name, m_inputPorts[ 0 ].FloatInternalData );
			if( !m_inputPorts[ 1 ].IsConnected )
				m_inputPorts[ 1 ].FloatInternalData = EditorGUILayoutFloatField( m_inputPorts[ 1 ].Name, m_inputPorts[ 1 ].FloatInternalData );
			m_useUnityBranch = EditorGUILayoutToggle( UseUnityBranchesStr, m_useUnityBranch );
		}

		public override void OnInputPortDisconnected( int portId )
		{
			base.OnInputPortDisconnected( portId );
			UpdateConnection( portId );
		}

		//void TestMainInputDataType()
		//{
		//	WirePortDataType newType = WirePortDataType.FLOAT;
		//	if ( m_inputPorts[ 0 ].IsConnected && UIUtils.GetPriority( m_inputPorts[ 0 ].DataType ) > UIUtils.GetPriority( newType ) )
		//	{
		//		newType = m_inputPorts[ 0 ].DataType;
		//	}

		//	if ( m_inputPorts[ 1 ].IsConnected && ( UIUtils.GetPriority( m_inputPorts[ 1 ].DataType ) > UIUtils.GetPriority( newType ) ) )
		//	{
		//		newType = m_inputPorts[ 1 ].DataType;
		//	}

		//	m_inputMainDataType = newType;
		//}

		void TestMainOutputDataType()
		{
			WirePortDataType newType = WirePortDataType.FLOAT;
			string structName = string.Empty;
			for( int i = 2; i < 5; i++ )
			{
				// @diogo: a struct ranks below every scalar, yet nothing casts to or from one, so a wired
				// struct decides the type outright rather than losing to the float this defaults to
				if ( m_inputPorts[ i ].IsConnected && newType != WirePortDataType.STRUCT &&
					( m_inputPorts[ i ].DataType == WirePortDataType.STRUCT || UIUtils.GetPriority( m_inputPorts[ i ].DataType ) > UIUtils.GetPriority( newType ) ) )
				{
					newType = m_inputPorts[ i ].DataType;
					structName = m_inputPorts[ i ].StructName;
				}
			}

			if( newType != m_outputMainDataType )
			{
				m_outputMainDataType = newType;
			}
			m_outputPorts[ 0 ].ChangeType( m_outputMainDataType, structName, false );

			// @diogo: a free branch carries the struct's name too, so a second one can only be wired the
			// same struct, and drops it again once no struct is left
			for ( int i = 2; i < 5; i++ )
			{
				if ( !m_inputPorts[ i ].IsConnected && ( newType == WirePortDataType.STRUCT || m_inputPorts[ i ].DataType == WirePortDataType.STRUCT ) )
				{
					m_inputPorts[ i ].ChangeType( ( newType == WirePortDataType.STRUCT ) ? WirePortDataType.STRUCT : WirePortDataType.FLOAT, structName, false );
				}
			}
		}

		public void UpdateConnection( int portId )
		{
			m_inputPorts[ portId ].MatchPortToConnection();
			switch( portId )
			{
				//case 0:
				//case 1:
				//{
				//	TestMainInputDataType();
				//}
				//break;
				case 2:
				case 3:
				case 4:
				{
					TestMainOutputDataType();
				}
				break;
			}
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
				return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );

			if ( ReportStructMix( 2, 4 ) )
			{
				return m_outputPorts[ 0 ].ErrorValue;
			}

			// @diogo: a struct names itself on the port, since the wire type alone cannot spell it
			bool isStruct = m_outputPorts[ 0 ].DataType == WirePortDataType.STRUCT;
			if ( isStruct && string.IsNullOrEmpty( m_outputPorts[ 0 ].StructName ) )
			{
				UIUtils.ShowMessage( UniqueId, NamelessStructError, MessageSeverity.Error );
				return m_outputPorts[ 0 ].ErrorValue;
			}

			string AValue = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector);
			string BValue = m_inputPorts[ 1 ].GeneratePortInstructions( ref dataCollector );

			bool lequal = false;
			bool greater = false;
			bool lesser = false;
			bool gequal = false;
			bool equal = false;
			bool nequal = false;
			bool welse = false;
			bool midCon = false;

			if( m_inputPorts[ 2 ].IsConnected )
			{
				greater = true;
			}

			if( m_inputPorts[ 4 ].IsConnected )
			{
				lesser = true;
			}

			if( greater && m_inputPorts[ 2 ].GetOutputConnection() == m_inputPorts[ 3 ].GetOutputConnection() )
			{
				gequal = true;
			}

			if( lesser && m_inputPorts[ 4 ].GetOutputConnection() == m_inputPorts[ 3 ].GetOutputConnection() )
			{
				lequal = true;
			}

			if( m_inputPorts[ 2 ].GetOutputConnection() == m_inputPorts[ 4 ].GetOutputConnection() )
			{
				if( m_inputPorts[ 3 ].IsConnected )
					equal = true;
				else if( m_inputPorts[ 2 ].IsConnected )
					nequal = true;
			}

			if( m_inputPorts[ 3 ].IsConnected )
			{
				midCon = true;

				if( greater && lesser )
					welse = true;
			}

			// @diogo: the chain is laid out before anything is generated, so each arm knows upfront if it owns its value (#365)
			List<KeyValuePair<string, int>> chain = new List<KeyValuePair<string, int>>();

			if( lequal && gequal ) // all equal
			{
			}
			else if( !lequal && gequal ) // greater or equal
			{
				chain.Add( new KeyValuePair<string, int>( string.Format( IfOps[ 3 ], AValue, BValue ), 0 ) );

				if( welse )
				{
					chain.Add( new KeyValuePair<string, int>( "else", 2 ) );
				}
			}
			else if( lequal && !gequal )// lesser or equal
			{
				chain.Add( new KeyValuePair<string, int>( string.Format( IfOps[ 4 ], AValue, BValue ), 2 ) );

				if( welse )
				{
					chain.Add( new KeyValuePair<string, int>( "else", 0 ) );
				}
			}
			else if( nequal )// not equal
			{
				chain.Add( new KeyValuePair<string, int>( string.Format( IfOps[ 5 ], AValue, BValue ), 0 ) );
			}
			else if( equal )// equal
			{
				chain.Add( new KeyValuePair<string, int>( string.Format( IfOps[ 1 ], AValue, BValue ), 1 ) );

				if( welse )
				{
					chain.Add( new KeyValuePair<string, int>( "else", 0 ) );
				}
			}
			else if( lesser && !midCon && !greater ) // lesser
			{
				chain.Add( new KeyValuePair<string, int>( string.Format( IfOps[ 2 ], AValue, BValue ), 2 ) );
			}
			else if( greater && !midCon && !lesser ) // greater
			{
				chain.Add( new KeyValuePair<string, int>( string.Format( IfOps[ 0 ], AValue, BValue ), 0 ) );
			}
			else if( !greater && !midCon && !lesser ) // none
			{
				//dataCollector.AddLocalVariable( UniqueId, localVarDec );
			}
			else // all different
			{
				bool ifStarted = false;
				if( greater )
				{
					chain.Add( new KeyValuePair<string, int>( string.Format( IfOps[ 0 ], AValue, BValue ), 0 ) );
					ifStarted = true;
				}

				if( midCon )
				{
					chain.Add( new KeyValuePair<string, int>( ( ifStarted ? "else " : string.Empty ) +string.Format( IfOps[ 1 ], AValue, BValue ), 1 ) );
					ifStarted = true;
				}

				if( lesser )
				{
					chain.Add( new KeyValuePair<string, int>( "else " + string.Format( IfOps[ 2 ], AValue, BValue ), 2 ) );
				}
			}

			// @diogo: only a value the chain reads from a single arm can be generated inside it
			int[] armUses = new int[ 3 ];
			for ( int i = 0; i < chain.Count; i++ )
			{
				armUses[ chain[ i ].Value ]++;
			}

			BranchArmAnalysis[] arms = new BranchArmAnalysis[ 3 ];
			for ( int i = 0; i < 3; i++ )
			{
				arms[ i ] = ( armUses[ i ] == 1 ) ? BranchScopeUtils.AnalyzeArm( this, m_inputPorts[ i + 2 ], ref dataCollector ) : new BranchArmAnalysis();
			}

			// @diogo: the scope carries the same verdict the hazard scan used, or the divergence net misfires (#365)
			bool uniformCondition = BranchScopeUtils.ConditionIsUniform( this );

			bool braced = false;
			for ( int i = 0; i < 3; i++ )
			{
				if ( arms[ i ].Scoped )
				{
					braced = true;
					BranchScopeUtils.PreGenerateFrontier( arms[ i ], ref dataCollector );
				}
				else
				{
					m_results[ i ] = BranchScopeUtils.PreGenerateFallback( arms[ i ], m_inputPorts[ i + 2 ], ref dataCollector, m_outputMainDataType, ignoreLocalvar, true );
				}
			}

			string localVarName = "ifLocalVar" + OutputId;
			// @diogo: nothing converts a bare 0 to a struct, so its zero has to be cast
			string localVarDec = isStruct ?
				string.Format( "{0} {1} = ( {0} )0;", m_outputPorts[ 0 ].StructName, localVarName ) :
				string.Format( "{0} {1} = 0;", UIUtils.PrecisionWirePortToCgType( CurrentPrecisionType, m_outputPorts[ 0 ].DataType ), localVarName );

			dataCollector.AddLocalVariable( UniqueId, localVarDec, true );
			if ( m_useUnityBranch && !( lequal && gequal ) && !( !greater && !midCon && !lesser ) )
				dataCollector.AddLocalVariable( UniqueId, UnityBranchStr, true );

			if( lequal && gequal ) // all equal
			{
				dataCollector.AddLocalVariable( UniqueId, string.Format( "{0} = {1};", localVarName, m_results[ 1 ] ), true );
			}
			else
			{
				for ( int i = 0; i < chain.Count; i++ )
				{
					int result = chain[ i ].Value;
					dataCollector.AddLocalVariable( UniqueId, chain[ i ].Key, true );

					if ( braced )
					{
						dataCollector.AddLocalVariable( UniqueId, "{", true );
					}

					if ( arms[ result ].Scoped )
					{
						// @diogo: an open scope indents everything it emits, so only a flat arm needs a literal tab
						dataCollector.PushBranchScope( uniformCondition );
						m_results[ result ] = m_inputPorts[ result + 2 ].GenerateShaderForOutput( ref dataCollector, m_outputMainDataType, ignoreLocalvar, true );
						dataCollector.AddLocalVariable( UniqueId, string.Format( "{0} = {1};", localVarName, m_results[ result ] ), true );
						dataCollector.PopBranchScope();
					}
					else
					{
						dataCollector.AddLocalVariable( UniqueId, string.Format( "\t{0} = {1};", localVarName, m_results[ result ] ), true );
					}

					if ( braced )
					{
						dataCollector.AddLocalVariable( UniqueId, "}", true );
					}
				}
			}

			m_outputPorts[ 0 ].SetLocalValue( localVarName, dataCollector.PortCategory );
			return localVarName;
		}
		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			if( UIUtils.CurrentShaderVersion() > 4103 )
			{
				m_useUnityBranch = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
			}
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_useUnityBranch );
		}
	}
}

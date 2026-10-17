// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using UnityEditor;
using System;
using System.CodeDom;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Compare", "Logical Operators", "Compare A to B given the selected operator. If comparison is true return value of True else return value of False", tags: "If Ternary Compare Less Equal Not Greater" )]
	public sealed class Compare : ParentNode
	{
		private static readonly string[] LabelsSTR = { "Equal", "Not Equal", "Greater", "Greater Or Equal", "Less", "Less Or Equal" };

		private const string DynamicBranchStr = "Dynamic Branching";
		private const string UnityBranchStr = "UNITY_BRANCH ";
		private const string NamelessStructError = "Compare node cannot declare a struct whose name did not reach its ports.";

		enum Comparision
		{
			Equal,
			NotEqual,
			Greater,
			GreaterOrEqual,
			Less,
			LessOrEqual,
		}

		private WirePortDataType m_mainInputType = WirePortDataType.FLOAT;
		private WirePortDataType m_mainOutputType = WirePortDataType.FLOAT;

		private int m_cachedOperatorId = -1;

		[SerializeField]
		private Comparision m_comparision = Comparision.Equal;

		[SerializeField]
		private bool m_dynamicBranch = false;

		private UpperLeftWidgetHelper m_upperLeftWidget = new UpperLeftWidgetHelper();

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( WirePortDataType.FLOAT, false, "A" );
			AddInputPort( WirePortDataType.FLOAT, false, "B" );
			AddInputPort( WirePortDataType.FLOAT, false, "True" );
			AddInputPort( WirePortDataType.FLOAT, false, "False" );

			// @diogo: a texture object is bound, not assigned, so neither operand nor arm survives the compare.
			// A struct is let into True and False only, which take the branch form for one
			for ( int i = 0; i < m_inputPorts.Count; i++ )
			{
				if ( i >= 2 )
				{
					m_inputPorts[ i ].AcceptAllTypes();
				}
				m_inputPorts[ i ].AddPortForbiddenTypes( WirePortDataType.SAMPLER1D, WirePortDataType.SAMPLER2D, WirePortDataType.SAMPLER3D,
					WirePortDataType.SAMPLERCUBE, WirePortDataType.SAMPLER2DARRAY, WirePortDataType.SAMPLERSTATE );
			}

			AddOutputPort( WirePortDataType.FLOAT, Constants.EmptyPortValue );
			m_inputPorts[ 0 ].AutoDrawInternalData = true;
			m_inputPorts[ 1 ].AutoDrawInternalData = true;
			m_inputPorts[ 2 ].AutoDrawInternalData = true;
			m_inputPorts[ 3 ].AutoDrawInternalData = true;
			m_textLabelWidth = 100;
			m_autoWrapProperties = true;
			m_hasLeftDropdown = true;
			m_previewShaderGUID = "381937898f0c15747af1da09a751890c";
			UpdateTitle();
		}

		public override StructFieldMask FieldDemandForInput( int inputIndex, StructFieldMask received )
		{
			// @diogo: A and B are read whole; True and False are arms
			return ( inputIndex >= 2 ) ? received : StructFieldMask.All;
		}

		public override void Destroy()
		{
			base.Destroy();
			m_upperLeftWidget = null;
		}

		public override void Draw( DrawInfo drawInfo )
		{
			base.Draw( drawInfo );
			EditorGUI.BeginChangeCheck();
			m_comparision = (Comparision)m_upperLeftWidget.DrawWidget( this, (int)m_comparision, LabelsSTR );
			if( EditorGUI.EndChangeCheck() )
			{
				UpdateTitle();
			}
		}

		public override void SetPreviewInputs()
		{
			base.SetPreviewInputs();

			if( m_cachedOperatorId == -1 )
				m_cachedOperatorId = Shader.PropertyToID( "_Operator" );

			PreviewMaterial.SetInt( m_cachedOperatorId, (int)m_comparision );
		}

		void UpdateTitle()
		{
			switch( m_comparision )
			{
				default:
				case Comparision.Equal:
				m_additionalContent.text = "( A = B )";
				break;
				case Comparision.NotEqual:
				m_additionalContent.text = "( A \u2260 B )";
				break;
				case Comparision.Greater:
				m_additionalContent.text = "( A > B )";
				break;
				case Comparision.GreaterOrEqual:
				m_additionalContent.text = "( A \u2265 B )";
				break;
				case Comparision.Less:
				m_additionalContent.text = "( A < B )";
				break;
				case Comparision.LessOrEqual:
				m_additionalContent.text = "( A \u2264 B )";
				break;
			}
			m_sizeIsDirty = true;
		}

		public override void DrawProperties()
		{
			base.DrawProperties();
			EditorGUI.BeginChangeCheck();
			m_comparision = (Comparision)EditorGUILayoutEnumPopup( "", m_comparision );
			if( EditorGUI.EndChangeCheck() )
			{
				UpdateTitle();
			}

			for( int i = 0; i < m_inputPorts.Count; i++ )
			{
				if( m_inputPorts[ i ].ValidInternalData && !m_inputPorts[ i ].IsConnected && m_inputPorts[ i ].Visible )
				{
					m_inputPorts[ i ].ShowInternalData( this );
				}
			}

			m_dynamicBranch = EditorGUILayoutToggle( DynamicBranchStr, m_dynamicBranch );
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

		public override void OnInputPortDisconnected( int portId )
		{
			base.OnInputPortDisconnected( portId );

			int otherPortId = 0;
			if( portId < 2 )
			{
				otherPortId = ( portId == 0 ) ? 1 : 0;
				if( m_inputPorts[ otherPortId ].IsConnected )
				{
					m_mainInputType = m_inputPorts[ otherPortId ].DataType;
					m_inputPorts[ portId ].ChangeType( m_mainInputType, false );
				}
			}
			else
			{
				otherPortId = ( portId == 2 ) ? 3 : 2;
				if( m_inputPorts[ otherPortId ].IsConnected )
				{
					m_mainOutputType = m_inputPorts[ otherPortId ].DataType;
					m_inputPorts[ portId ].ChangeType( m_mainOutputType, m_inputPorts[ otherPortId ].StructName, false );
					m_outputPorts[ 0 ].ChangeType( m_mainOutputType, m_inputPorts[ otherPortId ].StructName, false );
				}
				// @diogo: a struct left on the free arms would go on refusing every other struct
				else if ( m_mainOutputType == WirePortDataType.STRUCT )
				{
					m_mainOutputType = WirePortDataType.FLOAT;
					m_inputPorts[ 2 ].ChangeType( m_mainOutputType, string.Empty, false );
					m_inputPorts[ 3 ].ChangeType( m_mainOutputType, string.Empty, false );
					m_outputPorts[ 0 ].ChangeType( m_mainOutputType, string.Empty, false );
				}
			}
		}

		public void UpdateConnection( int portId )
		{
			m_inputPorts[ portId ].MatchPortToConnection();
			int otherPortId = 0;
			WirePortDataType otherPortType = WirePortDataType.FLOAT;
			if( portId < 2 )
			{
				otherPortId = ( portId == 0 ) ? 1 : 0;
				otherPortType = m_inputPorts[ otherPortId ].IsConnected ? m_inputPorts[ otherPortId ].DataType : WirePortDataType.FLOAT;
				m_mainInputType = UIUtils.GetPriority( m_inputPorts[ portId ].DataType ) > UIUtils.GetPriority( otherPortType ) ? m_inputPorts[ portId ].DataType : otherPortType;
				if( !m_inputPorts[ otherPortId ].IsConnected )
				{
					m_inputPorts[ otherPortId ].ChangeType( m_mainInputType, false );
				}
			}
			else
			{
				otherPortId = ( portId == 2 ) ? 3 : 2;
				otherPortType = m_inputPorts[ otherPortId ].IsConnected ? m_inputPorts[ otherPortId ].DataType : WirePortDataType.FLOAT;
				m_mainOutputType = UIUtils.GetPriority( m_inputPorts[ portId ].DataType ) > UIUtils.GetPriority( otherPortType ) ? m_inputPorts[ portId ].DataType : otherPortType;

				// @diogo: a struct ranks below every scalar, yet nothing casts to or from one, so a wired
				// struct decides the type outright rather than losing to the float a free arm defaults to
				string structName = string.Empty;
				if ( m_inputPorts[ portId ].DataType == WirePortDataType.STRUCT )
				{
					m_mainOutputType = WirePortDataType.STRUCT;
					structName = m_inputPorts[ portId ].StructName;
				}
				else if ( otherPortType == WirePortDataType.STRUCT )
				{
					m_mainOutputType = WirePortDataType.STRUCT;
					structName = m_inputPorts[ otherPortId ].StructName;
				}

				m_outputPorts[ 0 ].ChangeType( m_mainOutputType, structName, false );

				if( !m_inputPorts[ otherPortId ].IsConnected )
				{
					m_inputPorts[ otherPortId ].ChangeType( m_mainOutputType, structName, false );
				}
			}
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if ( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
			{
				return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );
			}

			if ( ReportStructMix( 2, 3 ) )
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

			//Conditional Operator ?: has some shenanigans
			//If the first operand is of type bool, one of the following must hold for the second and third operands:
			//Both operands have compatible structure types.
			//Both operands are scalars with numeric or bool type.
			//Both operands are vectors with numeric or bool type, where the two vectors are of the same size, which is less than or equal to four.
			//If the first operand is a packed vector of bool, then the conditional selection is performed on an elementwise basis.Both the second and third operands must be numeric vectors of the same size as the first operand.
			WirePortDataType compatibleInputType = m_mainInputType;
			// @diogo: never cast to a struct - the operands stay numeric whatever the arms carry
			if ( m_mainInputType != WirePortDataType.FLOAT && m_mainInputType != WirePortDataType.INT && m_mainInputType != m_mainOutputType && !isStruct )
			{
				compatibleInputType = m_mainOutputType;
			}

			string a = m_inputPorts[ 0 ].GenerateShaderForOutput( ref dataCollector, compatibleInputType, ignoreLocalvar, true );
			string b = m_inputPorts[ 1 ].GenerateShaderForOutput( ref dataCollector, compatibleInputType, ignoreLocalvar, true );
			string op = string.Empty;
			switch( m_comparision )
			{
				default:
				case Comparision.Equal:
				op = "==";
				break;
				case Comparision.NotEqual:
				op = "!=";
				break;
				case Comparision.Greater:
				op = ">";
				break;
				case Comparision.GreaterOrEqual:
				op = ">=";
				break;
				case Comparision.Less:
				op = "<";
				break;
				case Comparision.LessOrEqual:
				op = "<=";
				break;
			}

			// @diogo: both arms are laid out before either is generated, so each knows upfront if it owns its value (#365)
			BranchArmAnalysis trueArm = BranchScopeUtils.AnalyzeArm( this, m_inputPorts[ 2 ], ref dataCollector );
			BranchArmAnalysis falseArm = BranchScopeUtils.AnalyzeArm( this, m_inputPorts[ 3 ], ref dataCollector );

			// @diogo: the scope carries the same verdict the hazard scan used, or the divergence net misfires (#365)
			bool uniformCondition = BranchScopeUtils.ConditionIsUniform( this );

			string result = string.Empty;
			string T = null;
			string F = null;

			// @diogo: HLSL has no ternary on a struct, so one always takes the branch form
			if ( m_dynamicBranch || trueArm.Scoped || falseArm.Scoped || isStruct )
			{
				if ( trueArm.Scoped )
				{
					BranchScopeUtils.PreGenerateFrontier( trueArm, ref dataCollector );
				}
				else
				{
					T = BranchScopeUtils.PreGenerateFallback( trueArm, m_inputPorts[ 2 ], ref dataCollector, m_mainOutputType, ignoreLocalvar, true );
				}

				if ( falseArm.Scoped )
				{
					BranchScopeUtils.PreGenerateFrontier( falseArm, ref dataCollector );
				}
				else
				{
					F = BranchScopeUtils.PreGenerateFallback( falseArm, m_inputPorts[ 3 ], ref dataCollector, m_mainOutputType, ignoreLocalvar, true );
				}

				string outType = isStruct ? m_outputPorts[ 0 ].StructName : UIUtils.PrecisionWirePortToCgType( CurrentPrecisionType, m_outputPorts[ 0 ].DataType );
				string varName = "compareResult" + OutputId;
				string branchAttr = m_dynamicBranch ? UnityBranchStr : string.Empty;

				dataCollector.AddLocalVariable( UniqueId, outType + " " + varName + " = ( " + outType + " )0;", true );
				dataCollector.AddLocalVariable( UniqueId, string.Format( "{0}if( {1} {2} {3} )", branchAttr, a, op, b ), true );
				dataCollector.AddLocalVariable( UniqueId, "{", true );
				if ( trueArm.Scoped )
				{
					// @diogo: an open scope indents everything it emits, so only a flat arm needs a literal tab
					dataCollector.PushBranchScope( uniformCondition );
					T = m_inputPorts[ 2 ].GenerateShaderForOutput( ref dataCollector, m_mainOutputType, ignoreLocalvar, true );
					dataCollector.AddLocalVariable( UniqueId, varName + " = " + T + ";", true );
					dataCollector.PopBranchScope();
				}
				else
				{
					dataCollector.AddLocalVariable( UniqueId, "\t" + varName + " = " + T + ";", true );
				}
				dataCollector.AddLocalVariable( UniqueId, "}", true );
				dataCollector.AddLocalVariable( UniqueId, "else", true );
				dataCollector.AddLocalVariable( UniqueId, "{", true );
				if ( falseArm.Scoped )
				{
					dataCollector.PushBranchScope( uniformCondition );
					F = m_inputPorts[ 3 ].GenerateShaderForOutput( ref dataCollector, m_mainOutputType, ignoreLocalvar, true );
					dataCollector.AddLocalVariable( UniqueId, varName + " = " + F + ";", true );
					dataCollector.PopBranchScope();
				}
				else
				{
					dataCollector.AddLocalVariable( UniqueId, "\t" + varName + " = " + F + ";", true );
				}
				dataCollector.AddLocalVariable( UniqueId, "}", true );

				m_outputPorts[ 0 ].SetLocalValue( varName, dataCollector.PortCategory );
				result = varName;
			}
			else
			{
				T = m_inputPorts[ 2 ].GenerateShaderForOutput( ref dataCollector, m_mainOutputType, ignoreLocalvar, true );
				F = m_inputPorts[ 3 ].GenerateShaderForOutput( ref dataCollector, m_mainOutputType, ignoreLocalvar, true );
				result = CreateOutputLocalVariable( 0, string.Format( "( {0} {2} {1} ? {3} : {4} )", a, b, op, T, F ), ref dataCollector );
			}
			return result;
		}

		public override void ReadFromDeprecated( ref string[] nodeParams, Type oldType = null )
		{
			base.ReadFromDeprecated( ref nodeParams, oldType );

			if( oldType == typeof( TFHCCompareEqual ) )
			{
				m_comparision = Comparision.Equal;
			}
			else
			if( oldType == typeof( TFHCCompareNotEqual ) )
			{
				m_comparision = Comparision.NotEqual;
			}
			else
			if( oldType == typeof( TFHCCompareGreater ) )
			{
				m_comparision = Comparision.Greater;
			} 
			else
			if( oldType == typeof( TFHCCompareGreaterEqual ) )
			{
				m_comparision = Comparision.GreaterOrEqual;
			}
			else
			if( oldType == typeof( TFHCCompareLower ) )
			{
				m_comparision = Comparision.Less;
			}
			else
			if( oldType == typeof( TFHCCompareLowerEqual ) )
			{
				m_comparision = Comparision.LessOrEqual;
			}

			UpdateTitle();
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );

			m_comparision = (Comparision)Convert.ToSingle( GetCurrentParam( ref nodeParams ) );

			if ( UIUtils.CurrentShaderVersion() >= 19913 )
			{
				m_dynamicBranch = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
			}

			UpdateTitle();
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );

			IOUtils.AddFieldValueToString( ref nodeInfo, (int)m_comparision );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_dynamicBranch );
		}
	}
}

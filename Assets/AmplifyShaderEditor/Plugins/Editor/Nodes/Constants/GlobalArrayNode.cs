// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>
//
// Custom Node Global Array
// Donated by Johann van Berkel

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Global Array" , "Constants And Properties" , "The node returns a value from a global array, which you can configure by entering the name of the array in the node's settings." , null , KeyCode.None , true , false , null , null , "Johann van Berkel" )]
	public sealed class GlobalArrayNode : ParentNode
	{
		private const string DefaultArrayName = "MyGlobalArray";
		private const string TypeStr = "Type";
		private const string AutoRangeCheckStr = "Range Check";
		private const string ArrayFormatStr = "{0}[{1}]";
		private const string JaggedArrayFormatStr = "{0}[{1}][{2}]";
		private const string IsJaggedStr = "Is Jagged";
		private const string AutoRegisterStr = "Auto-Register";

		private readonly string[] AvailableTypesLabel = { "Float" , "Color" , "Vector4" , "Matrix4x4" };
		private readonly WirePortDataType[] AvailableTypesValues = { WirePortDataType.FLOAT , WirePortDataType.COLOR , WirePortDataType.FLOAT4 , WirePortDataType.FLOAT4x4 };

		[SerializeField]
		private string m_name = DefaultArrayName;

		[SerializeField]
		private int m_indexX = 0;

		[SerializeField]
		private int m_indexY = 0;

		[SerializeField]
		private int m_arrayLengthX = 1;

		[SerializeField]
		private int m_arrayLengthY = 1;

		[SerializeField]
		private int m_type = 0;

		[SerializeField]
		private bool m_autoRangeCheck = false;

		[SerializeField]
		private bool m_isJagged = false;

		[SerializeField]
		private bool m_autoRegister = false;

		//////////////////////////////////////////////////////////////////
		private readonly Color ReferenceHeaderColor = new Color( 0f, 0.5f, 0.585f, 1.0f );

		private const float ReferenceIconSize = 19;
		private Rect m_iconPos;

		[SerializeField]
		private TexReferenceType m_referenceType = TexReferenceType.Object;

		[SerializeField]
		private int m_referenceArrayId = -1;

		[SerializeField]
		private int m_referenceNodeId = -1;

		private GlobalArrayNode m_referenceNode = null;

		private bool m_updated = false;

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );

			AddInputPort( WirePortDataType.INT , false , "Index" , -1 , MasterNodePortCategory.Fragment , 0 );
			AddInputPort( WirePortDataType.INT , false , "Index Y" , -1 , MasterNodePortCategory.Fragment , 2 );
			AddInputPort( WirePortDataType.INT , false , "Array Length" , -1 , MasterNodePortCategory.Fragment , 1 );
			AddInputPort( WirePortDataType.INT , false , "Array Length Y" , -1 , MasterNodePortCategory.Fragment , 3 );

			AddOutputPort( WirePortDataType.FLOAT , "Out" );

			m_textLabelWidth = 95;
			m_autoWrapProperties = true;
			SetAdditonalTitleText( string.Format( Constants.SubTitleValueFormatStr , m_name ) );
			UpdatePorts();
		}

		protected override void OnUniqueIDAssigned()
		{
			base.OnUniqueIDAssigned();
			// @diogo: only Object-mode arrays are pickable references; Instance-mode ones must stay out of the register
			if ( m_referenceType == TexReferenceType.Object )
			{
				EnsureUniqueArrayName();
				UIUtils.CurrentWindow.OutsideGraph.GlobalArrayNodes.AddNode( this );
			}

			if ( UniqueId > -1 )
			{
				UIUtils.CurrentWindow.OutsideGraph.GlobalArrayNodes.OnReorderEventComplete -= OnReorderEventComplete;
				UIUtils.CurrentWindow.OutsideGraph.GlobalArrayNodes.OnReorderEventComplete += OnReorderEventComplete;
			}
		}

		private void OnReorderEventComplete()
		{
			if ( m_referenceType == TexReferenceType.Instance && m_referenceNode != null )
			{
				m_referenceArrayId = UIUtils.CurrentWindow.OutsideGraph.GlobalArrayNodes.GetNodeRegisterIdx( m_referenceNode.UniqueId );
			}
		}

		private bool IsArrayNameTaken( string name )
		{
			List<GlobalArrayNode> nodes = UIUtils.CurrentWindow.OutsideGraph.GlobalArrayNodes.NodesList;
			int count = nodes.Count;
			for ( int i = 0; i < count; i++ )
			{
				if ( nodes[ i ] != null && nodes[ i ] != this && nodes[ i ].DataToArray == name )
				{
					return true;
				}
			}
			return false;
		}

		// @diogo: array names have no uniform registry; advance the trailing number until the name is free
		private void EnsureUniqueArrayName()
		{
			if ( !IsArrayNameTaken( m_name ) )
			{
				return;
			}

			int baseLength = m_name.Length;
			while ( baseLength > 0 && char.IsDigit( m_name[ baseLength - 1 ] ) )
			{
				baseLength--;
			}
			string nameBase = ( baseLength > 0 ) ? m_name.Substring( 0, baseLength ) : m_name;
			int index;
			int.TryParse( m_name.Substring( baseLength ), out index );
			string candidate;
			do
			{
				index++;
				candidate = nameBase + index;
			} while ( IsArrayNameTaken( candidate ) );
			m_name = candidate;
			SetAdditonalTitleText( string.Format( Constants.SubTitleValueFormatStr, m_name ) );
		}

		public override void Destroy()
		{
			base.Destroy();
			UIUtils.CurrentWindow.OutsideGraph.GlobalArrayNodes.RemoveNode( this );
			if ( UniqueId > -1 )
			{
				UIUtils.CurrentWindow.OutsideGraph.GlobalArrayNodes.OnReorderEventComplete -= OnReorderEventComplete;
			}
		}

		void UpdatePorts()
		{
			InputPort indexXPort = GetInputPortByUniqueId( 0 );
			InputPort arrayLengthPortX = GetInputPortByUniqueId( 1 );
			InputPort indexYPort = GetInputPortByUniqueId( 2 );
			InputPort arrayLengthPortY = GetInputPortByUniqueId( 3 );
			if( m_referenceType == TexReferenceType.Object )
			{
				m_headerColorModifier = Color.white;
				SetAdditonalTitleText( string.Format( Constants.SubTitleValueFormatStr , m_name ) );
				arrayLengthPortX.Visible = true;
				if( m_isJagged )
				{
					indexXPort.Name = "Index X";
					arrayLengthPortX.Name = "Array Length X";
					indexYPort.Visible = true;
					arrayLengthPortY.Visible = true;
				}
				else
				{
					indexXPort.Name = "Index";
					arrayLengthPortX.Name = "Array Length";
					indexYPort.Visible = false;
					arrayLengthPortY.Visible = false;
				}
			}
			else if( m_referenceNodeId > -1 )
			{
				m_headerColorModifier = ReferenceHeaderColor;
				if( m_referenceNode == null )
					m_referenceNode = UIUtils.GetNode( m_referenceNodeId ) as GlobalArrayNode;

				if( m_referenceNode != null )
				{
					SetAdditonalTitleText( string.Format( Constants.SubTitleRefNameFormatStr, m_referenceNode.DataToArray ) );
					arrayLengthPortX.Visible = false;
					arrayLengthPortY.Visible = false;
					if( m_referenceNode.IsJagged )
					{
						indexXPort.Name = "Index X";
						indexYPort.Visible = true;
					}
					else
					{
						indexXPort.Name = "Index";
						indexYPort.Visible = false;
					}
				}
			}
			else
			{
				// @diogo: an unassigned reference still tints the header and reads as Ref( None )
				m_headerColorModifier = ReferenceHeaderColor;
				SetAdditonalTitleText( string.Format( Constants.SubTitleRefNameFormatStr, Constants.NoStringValue ) );
			}
			m_sizeIsDirty = true;
		}

		void DrawObjectProperties()
		{
			EditorGUI.BeginChangeCheck();
			m_name = EditorGUILayoutStringField( "Name" , m_name );
			if( EditorGUI.EndChangeCheck() )
			{
				m_updated = true;
				m_name = UIUtils.RemoveInvalidCharacters( m_name );
				if( string.IsNullOrEmpty( m_name ) )
					m_name = DefaultArrayName;
				UIUtils.UpdateGlobalArrayDataNode( UniqueId , m_name );
				SetAdditonalTitleText( string.Format( Constants.SubTitleValueFormatStr , m_name ) );
			}


			m_autoRegister = EditorGUILayoutToggle( AutoRegisterStr , m_autoRegister );

			EditorGUI.BeginChangeCheck();
			m_isJagged = EditorGUILayoutToggle( IsJaggedStr , m_isJagged );
			if( EditorGUI.EndChangeCheck() )
			{
				m_updated = true;
				UpdatePorts();
			}

			InputPort indexXPort = GetInputPortByUniqueId( 0 );
			if( !indexXPort.IsConnected )
			{
				EditorGUI.BeginChangeCheck();
				m_indexX = EditorGUILayoutIntField( indexXPort.Name , m_indexX );
				if( EditorGUI.EndChangeCheck() )
				{
					m_indexX = Mathf.Clamp( m_indexX , 0 , ( ArrayLengthX - 1 ) );
				}
			}

			if( m_isJagged )
			{
				InputPort indexYPort = GetInputPortByUniqueId( 2 );
				if( !indexYPort.IsConnected )
				{
					EditorGUI.BeginChangeCheck();
					m_indexY = EditorGUILayoutIntField( indexYPort.Name , m_indexY );
					if( EditorGUI.EndChangeCheck() )
					{
						m_indexY = Mathf.Clamp( m_indexY , 0 , ( ArrayLengthY - 1 ) );
					}
				}
			}

			InputPort arrayLengthXPort = GetInputPortByUniqueId( 1 );
			if( !arrayLengthXPort.IsConnected )
			{
				EditorGUI.BeginChangeCheck();
				m_arrayLengthX = EditorGUILayoutIntField( arrayLengthXPort.Name , m_arrayLengthX );
				if( EditorGUI.EndChangeCheck() )
				{
					m_arrayLengthX = Mathf.Max( 1 , m_arrayLengthX );
				}
			}

			if( m_isJagged )
			{
				InputPort arrayLengthYPort = GetInputPortByUniqueId( 3 );
				if( !arrayLengthYPort.IsConnected )
				{
					EditorGUI.BeginChangeCheck();
					m_arrayLengthY = EditorGUILayoutIntField( arrayLengthYPort.Name , m_arrayLengthY );
					if( EditorGUI.EndChangeCheck() )
					{
						m_arrayLengthY = Mathf.Max( 1 , m_arrayLengthY );
					}
				}
			}

			EditorGUI.BeginChangeCheck();
			m_type = EditorGUILayoutPopup( TypeStr , m_type , AvailableTypesLabel );
			if( EditorGUI.EndChangeCheck() )
			{
				m_outputPorts[ 0 ].ChangeType( (WirePortDataType)AvailableTypesValues[ m_type ] , false );
			}

			m_autoRangeCheck = EditorGUILayoutToggle( AutoRangeCheckStr , m_autoRangeCheck );
		}

		public override void OnNodeLayout( DrawInfo drawInfo, NodeUpdateCache cache )
		{
			base.OnNodeLayout( drawInfo, cache );
			m_updated = false;
			if( m_referenceType == TexReferenceType.Instance )
			{
				// @diogo: same left link icon the other reference nodes show
				m_iconPos = m_globalPosition;
				m_iconPos.width = ReferenceIconSize * drawInfo.InvertedZoom;
				m_iconPos.height = ReferenceIconSize * drawInfo.InvertedZoom;
				m_iconPos.y = m_globalPosition.y + m_headerPosition.height * 0.5f - m_iconPos.height * 0.5f;
				m_iconPos.x += 5 * drawInfo.InvertedZoom;

				if( m_referenceNodeId > -1 && m_referenceNode == null )
				{
					m_referenceNode = UIUtils.GetNode( m_referenceNodeId ) as GlobalArrayNode;
					if( m_referenceNode == null )
					{
						m_referenceNodeId = -1;
					}
				}
				// @diogo: a node in Instance mode can't itself be a source ( no chaining ); a source switched
				// into Instance mode invalidates the link, so drop it fully to read Ref( None )
				if ( m_referenceNode != null && m_referenceNode.m_referenceType == TexReferenceType.Instance )
				{
					m_referenceNode = null;
					m_referenceArrayId = -1;
					m_referenceNodeId = -1;
					m_headerColorModifier = ReferenceHeaderColor;
					SetAdditonalTitleText( string.Format( Constants.SubTitleRefNameFormatStr, Constants.NoStringValue ) );
					m_sizeIsDirty = true;
				}
				if( m_referenceNode != null && m_referenceNode.Updated )
				{
					UpdatePorts();
				}
			}
		}

		public override void OnNodeRepaint( DrawInfo drawInfo )
		{
			base.OnNodeRepaint( drawInfo );

			if ( !m_isVisible )
			{
				return;
			}

			if ( m_referenceType == TexReferenceType.Instance )
			{
				GUI.Label( m_iconPos, string.Empty, UIUtils.GetCustomStyle( CustomStyle.SamplerTextureIcon ) );
			}
		}

		void DrawInstancedProperties()
		{
			// @diogo: undo can restore a register snapshot still listing this Instance node; realign so it can't offer itself
			if ( UIUtils.CurrentWindow.OutsideGraph.GlobalArrayNodes.GetNodeRegisterIdx( UniqueId ) > -1 )
			{
				UIUtils.CurrentWindow.OutsideGraph.GlobalArrayNodes.RemoveNode( this );
			}
			string[] arr = UIUtils.GlobalArrayNodeArr();
			bool guiEnabledBuffer = GUI.enabled;
			if( arr != null && arr.Length > 0 )
			{
				GUI.enabled = true;
			}
			else
			{
				m_referenceArrayId = -1;
				m_referenceNodeId = -1;
				m_referenceNode = null;
				GUI.enabled = false;
			}
			arr = UIUtils.DisambiguateReferenceLabels( arr, UIUtils.CurrentWindow.CurrentGraph.GlobalArrayNodes.NodesList, 0 );
			EditorGUILayout.BeginHorizontal();
			SearchableDropdown.Draw( Constants.AvailableReferenceStr, m_referenceArrayId, arr, ( idx ) =>
			{
				if( idx != m_referenceArrayId )
				{
					UndoRecordObject( "Selected global array reference" );
					m_referenceArrayId = idx;
					m_referenceNode = UIUtils.GetGlobalArrayNode( m_referenceArrayId );
					if( m_referenceNode != null )
					{
						m_referenceNodeId = m_referenceNode.UniqueId;
					}
					UpdatePorts();
					SetDirtyFromAsyncEdit();
				}
			} );
			UIUtils.DrawReferenceFocusButton( m_referenceNode );
			EditorGUILayout.EndHorizontal();

			GUI.enabled = guiEnabledBuffer;

			InputPort indexXPort = GetInputPortByUniqueId( 0 );
			if( !indexXPort.IsConnected )
			{
				EditorGUI.BeginChangeCheck();
				m_indexX = EditorGUILayoutIntField( indexXPort.Name , m_indexX );
				if( EditorGUI.EndChangeCheck() )
				{
					m_indexX = Mathf.Clamp( m_indexX , 0 , ( ArrayLengthX - 1 ) );
				}
			}

			if( m_isJagged )
			{
				InputPort indexYPort = GetInputPortByUniqueId( 2 );
				if( !indexYPort.IsConnected )
				{
					EditorGUI.BeginChangeCheck();
					m_indexY = EditorGUILayoutIntField( indexYPort.Name , m_indexY );
					if( EditorGUI.EndChangeCheck() )
					{
						m_indexY = Mathf.Clamp( m_indexY , 0 , ( ArrayLengthY - 1 ) );
					}
				}
			}
		}

		public override void DrawProperties()
		{
			EditorGUI.BeginChangeCheck();
			m_referenceType = (TexReferenceType)EditorGUILayoutPopup( Constants.ReferenceTypeStr , (int)m_referenceType , Constants.ReferenceArrayLabels );
			if( EditorGUI.EndChangeCheck() )
			{
				if ( m_referenceType == TexReferenceType.Object )
				{
					UIUtils.CurrentWindow.OutsideGraph.GlobalArrayNodes.AddNode( this );
				}
				else
				{
					UIUtils.CurrentWindow.OutsideGraph.GlobalArrayNodes.RemoveNode( this );
				}
				UpdatePorts();
			}

			UIUtils.DrawSeparator();

			if( m_referenceType == TexReferenceType.Object )
				DrawObjectProperties();
			else
				DrawInstancedProperties();
		}

		public string GetArrayValue( string indexX , string indexY = null )
		{
			if( m_isJagged )
				return string.Format( JaggedArrayFormatStr , m_name , indexX , indexY );

			return string.Format( ArrayFormatStr , m_name , indexX );
		}

		public string GenerateInstancedShaderForOutput( int outputId , ref MasterNodeDataCollector dataCollector , bool ignoreLocalvar )
		{
			string result = string.Empty;
			if( m_referenceNode != null )
			{
				// @diogo: an unconnected source never generates its uniform; declare it here through the source's own
				// length ports so the string matches the source's declaration and the collector dedups it
				string srcDataType = UIUtils.PrecisionWirePortToCgType( m_referenceNode.CurrentPrecisionType, AvailableTypesValues[ m_referenceNode.m_type ] );
				InputPort srcLengthXPort = m_referenceNode.GetInputPortByUniqueId( 1 );
				string srcLengthX = srcLengthXPort.IsConnected ? srcLengthXPort.GeneratePortInstructions( ref dataCollector ) : m_referenceNode.ArrayLengthX.ToString();
				if ( m_referenceNode.IsJagged )
				{
					InputPort srcLengthYPort = m_referenceNode.GetInputPortByUniqueId( 3 );
					string srcLengthY = srcLengthYPort.IsConnected ? srcLengthYPort.GeneratePortInstructions( ref dataCollector ) : m_referenceNode.ArrayLengthY.ToString();
					dataCollector.AddToUniforms( m_referenceNode.UniqueId, srcDataType, string.Format( JaggedArrayFormatStr, m_referenceNode.m_name, srcLengthX, srcLengthY ) );
				}
				else
				{
					dataCollector.AddToUniforms( m_referenceNode.UniqueId, srcDataType, string.Format( ArrayFormatStr, m_referenceNode.m_name, srcLengthX ) );
				}

				InputPort indexXPort = GetInputPortByUniqueId( 0 );
				if( m_referenceNode.IsJagged )
				{
					InputPort indexYPort = GetInputPortByUniqueId( 2 );
					string arrayIndexX = indexXPort.IsConnected ? indexXPort.GeneratePortInstructions( ref dataCollector ) : m_indexX.ToString();
					string arrayIndexY = indexYPort.IsConnected ? indexYPort.GeneratePortInstructions( ref dataCollector ) : m_indexY.ToString();
					result = m_referenceNode.GetArrayValue( arrayIndexX , arrayIndexY );
				}
				else
				{
					string arrayIndexX = indexXPort.IsConnected ? indexXPort.GeneratePortInstructions( ref dataCollector ) : m_indexX.ToString();
					result = m_referenceNode.GetArrayValue( arrayIndexX );
				}
			}
			m_outputPorts[ 0 ].SetLocalValue( result , dataCollector.PortCategory );
			return result;
		}

		public override string GenerateShaderForOutput( int outputId , ref MasterNodeDataCollector dataCollector , bool ignoreLocalvar )
		{
			if( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
				return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );

			if( m_referenceType == TexReferenceType.Instance )
				return GenerateInstancedShaderForOutput( outputId , ref dataCollector , ignoreLocalvar );

			string dataType = UIUtils.PrecisionWirePortToCgType( CurrentPrecisionType , AvailableTypesValues[ m_type ] );

			InputPort indexXPort = GetInputPortByUniqueId( 0 );
			InputPort arrayLengthXPort = GetInputPortByUniqueId( 1 );
			string result = string.Empty;

			if( m_isJagged )
			{
				InputPort indexYPort = GetInputPortByUniqueId( 2 );
				InputPort arrayLengthYPort = GetInputPortByUniqueId( 3 );

				string arrayIndexX = indexXPort.IsConnected ? indexXPort.GeneratePortInstructions( ref dataCollector ) : m_indexX.ToString();
				string arrayLengthX = arrayLengthXPort.IsConnected ? arrayLengthXPort.GeneratePortInstructions( ref dataCollector ) : ArrayLengthX.ToString();

				string arrayIndexY = indexYPort.IsConnected ? indexYPort.GeneratePortInstructions( ref dataCollector ) : m_indexY.ToString();
				string arrayLengthY = arrayLengthYPort.IsConnected ? arrayLengthYPort.GeneratePortInstructions( ref dataCollector ) : ArrayLengthY.ToString();

				dataCollector.AddToUniforms( UniqueId , dataType , string.Format( JaggedArrayFormatStr , m_name , arrayLengthX , arrayLengthY ) );
				if( m_autoRangeCheck )
				{
					arrayIndexX = string.Format( "clamp({0},0,({1} - 1))" , arrayIndexX , arrayLengthX );
					arrayIndexY = string.Format( "clamp({0},0,({1} - 1))" , arrayIndexY , arrayLengthY );
				}
				result = string.Format( JaggedArrayFormatStr , m_name , arrayIndexX , arrayIndexY );
			}
			else
			{

				string arrayIndex = indexXPort.IsConnected ? indexXPort.GeneratePortInstructions( ref dataCollector ) : m_indexX.ToString();
				string arrayLength = arrayLengthXPort.IsConnected ? arrayLengthXPort.GeneratePortInstructions( ref dataCollector ) : ArrayLengthX.ToString();


				dataCollector.AddToUniforms( UniqueId , dataType , string.Format( ArrayFormatStr , m_name , arrayLength ) );

				if( m_autoRangeCheck )
					arrayIndex = string.Format( "clamp({0},0,({1} - 1))" , arrayIndex , arrayLength );

				result = string.Format( ArrayFormatStr , m_name , arrayIndex );
			}

			m_outputPorts[ 0 ].SetLocalValue( result , dataCollector.PortCategory );
			return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );
		}

		public void CheckIfAutoRegister( ref MasterNodeDataCollector dataCollector )
		{
			if( m_referenceType == TexReferenceType.Object && m_autoRegister && m_connStatus != NodeConnectionStatus.Connected )
			{
				string dataType = UIUtils.PrecisionWirePortToCgType( CurrentPrecisionType , AvailableTypesValues[ m_type ] );
				if( m_isJagged )
				{
					dataCollector.AddToUniforms( UniqueId , dataType , string.Format( JaggedArrayFormatStr , m_name , ArrayLengthX , ArrayLengthY ) );
				}
				else
				{
					dataCollector.AddToUniforms( UniqueId , dataType , string.Format( ArrayFormatStr , m_name , ArrayLengthX ) );
				}
			}
		}

		public override void WriteToString( ref string nodeInfo , ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo , ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo , m_name );
			IOUtils.AddFieldValueToString( ref nodeInfo , m_indexX );
			IOUtils.AddFieldValueToString( ref nodeInfo , m_arrayLengthX );
			IOUtils.AddFieldValueToString( ref nodeInfo , m_type );
			IOUtils.AddFieldValueToString( ref nodeInfo , m_autoRangeCheck );
			IOUtils.AddFieldValueToString( ref nodeInfo , m_isJagged );
			IOUtils.AddFieldValueToString( ref nodeInfo , m_indexY );
			IOUtils.AddFieldValueToString( ref nodeInfo , m_arrayLengthY );
			IOUtils.AddFieldValueToString( ref nodeInfo , m_autoRegister );
			IOUtils.AddFieldValueToString( ref nodeInfo , m_referenceType );
			IOUtils.AddFieldValueToString( ref nodeInfo , m_referenceNodeId );
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			m_name = GetCurrentParam( ref nodeParams );
			m_indexX = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			m_arrayLengthX = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			m_type = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			m_autoRangeCheck = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
			if( UIUtils.CurrentShaderVersion() > 15801 )
			{
				m_isJagged = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
				m_indexY = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
				m_arrayLengthY = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
				m_autoRegister = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
				m_referenceType = (TexReferenceType)Enum.Parse( typeof( TexReferenceType ) , GetCurrentParam( ref nodeParams ) );
				m_referenceNodeId = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
				if ( m_referenceType == TexReferenceType.Instance )
				{
					UIUtils.CurrentWindow.OutsideGraph.GlobalArrayNodes.RemoveNode( this );
				}
			}
			SetAdditonalTitleText( string.Format( Constants.SubTitleValueFormatStr , m_name ) );
			UpdatePorts();
			// @diogo: the register add ran before the name was read; dedup pasted copies, then re-push the real name
			if ( m_referenceType == TexReferenceType.Object )
			{
				if ( m_isNodeBeingCopied )
				{
					EnsureUniqueArrayName();
				}
				UIUtils.CurrentWindow.OutsideGraph.GlobalArrayNodes.UpdateDataOnNode( UniqueId , m_name );
			}
		}

		public override void RefreshExternalReferences()
		{
			base.RefreshExternalReferences();
			if( m_referenceType == TexReferenceType.Instance && m_referenceNodeId > -1 )
			{
				m_referenceNode = UIUtils.GetNode( m_referenceNodeId ) as GlobalArrayNode;
				// @diogo: graphs saved before the register fix may carry a self-reference; drop it
				if ( m_referenceNode == this )
				{
					m_referenceNode = null;
				}
				if( m_referenceNode != null )
				{
					m_referenceArrayId = UIUtils.GetGlobalArrayNodeRegisterId( m_referenceNodeId );
					UpdatePorts();
				}
				else
				{
					m_referenceNodeId = -1;
				}
			}
		}

		public override void ReconnectClipboardReferences( Clipboard clipboard )
		{
			// validate node first
			int newId = clipboard.GeNewNodeId( m_referenceNodeId );
			if ( ContainerGraph.GetNode( newId ) != null )
			{
				m_referenceNodeId = newId;
			}
			RefreshExternalReferences();
		}

		public int ArrayLengthX { get { return ( m_referenceNode != null ) ? m_referenceNode.ArrayLengthX : m_arrayLengthX; } }
		public int ArrayLengthY { get { return ( m_referenceNode != null ) ? m_referenceNode.ArrayLengthY : m_arrayLengthY; } }
		public override int ReferencedNodeId { get { return ( m_referenceType == TexReferenceType.Instance && m_referenceNodeId > -1 ) ? m_referenceNodeId : base.ReferencedNodeId; } }
		public override bool HasMissingReference { get { return m_referenceType == TexReferenceType.Instance && m_referenceNodeId <= -1; } }

		public bool AutoRegister { get { return m_autoRegister; } }
		public bool IsJagged { get { return m_isJagged; } }
		public bool Updated { get { return m_updated; } }
		public override string DataToArray { get { return m_name; } }
	}
}

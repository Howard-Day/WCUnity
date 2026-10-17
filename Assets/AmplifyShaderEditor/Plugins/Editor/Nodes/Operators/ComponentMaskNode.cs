// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using UnityEditor;
using System;

namespace AmplifyShaderEditor
{
	public enum ComponentMaskMode
	{
		Mask = 0,
		Index
	}

	[Serializable]
	[NodeAttributes( "Component Mask", "Vector Operators", "Mask certain channels from vectors/color components", null, KeyCode.K )]
	public sealed class ComponentMaskNode : ParentNode
	{
		private const string OutputLocalVarName = "componentMask";
		private const int IndexPortIdx = 1;
		private const string IndexPortName = "Index";
		private const string ModeStr = "Mode";
		private const string ClampIndexStr = "Clamp Index";
		private const string IndexModeInfoMsg = "Index mode outputs a single float: the one component picked by the Index input. Unlike Mask mode, the picked component can change at runtime.\n\nClamp Index keeps the index inside the amount of components the input has; turn it off to save a couple of instructions when the index is already known to be valid.";
		private const string UnclampedIndexInfoMsg = "Clamping is off: an Index outside the input component range produces undefined values.";

		[SerializeField]
		private bool[] m_selection = { true, true, true, true };

		[SerializeField]
		private bool m_clampIndex = true;

		[SerializeField]
		private int m_outputPortCount = 4;

		[SerializeField]
		private string[] m_labels;

		[SerializeField]
		private ComponentMaskMode m_mode = ComponentMaskMode.Mask;

		private int m_cachedOrderId = -1;
		private int m_cachedSingularId = -1;
		private int m_cachedModeId = -1;
		private int m_cachedCountId = -1;
		private int m_cachedClampId = -1;

		// @diogo: OBJECT has no CG type of its own, so it stands in for the 4 component vector it already counts as
		private WirePortDataType InputDataType
		{
			get { return ( m_inputPorts[ 0 ].DataType == WirePortDataType.OBJECT ) ? WirePortDataType.FLOAT4 : m_inputPorts[ 0 ].DataType; }
		}

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( WirePortDataType.FLOAT4, false, Constants.EmptyPortValue );

			AddInputPort( WirePortDataType.INT, false, IndexPortName );
			m_inputPorts[ IndexPortIdx ].Visible = false;
			m_inputPorts[ IndexPortIdx ].AddPortRestrictions( WirePortDataType.FLOAT, WirePortDataType.INT );

			AddOutputPort( WirePortDataType.FLOAT4, Constants.EmptyPortValue );
			m_useInternalPortData = true;
			m_selectedLocation = PreviewLocation.TopCenter;
			m_labels = new string[] { "X", "Y", "Z", "W" };
			m_previewShaderGUID = "b78e2b295c265cd439c80d218fb3e88e";
			SetAdditonalTitleText( "Value( XYZW )" );
		}

		// @diogo: components the input can address; scalars and matrices have none to pick from
		private int ComponentCount()
		{
			switch ( m_inputPorts[ 0 ].DataType )
			{
				case WirePortDataType.FLOAT2: return 2;
				case WirePortDataType.FLOAT3: return 3;
				case WirePortDataType.FLOAT4:
				case WirePortDataType.OBJECT:
				case WirePortDataType.COLOR: return 4;
			}
			return 0;
		}

		public override void SetPreviewInputs()
		{
			base.SetPreviewInputs();

			Vector4 order = new Vector4(-1,-1,-1,-1);
			int lastIndex = 0;
			int singularId = -1;
			var datatype = m_inputPorts[ 0 ].DataType;

			if( m_selection[ 0 ] )
			{
				order.Set( lastIndex, order.y, order.z, order.w );
				lastIndex++;
				singularId = 0;
			}
			if( m_selection[ 1 ] && datatype >= WirePortDataType.FLOAT2 )
			{
				order.Set( order.x, lastIndex, order.z, order.w );
				lastIndex++;
				singularId = 1;
			}
			if( m_selection[ 2 ] && datatype >= WirePortDataType.FLOAT3 )
			{
				order.Set( order.x, order.y, lastIndex, order.w );
				lastIndex++;
				singularId = 2;
			}
			if( m_selection[ 3 ] && ( datatype == WirePortDataType.FLOAT4 || datatype == WirePortDataType.COLOR ) )
			{
				order.Set( order.x, order.y, order.z, lastIndex );
				lastIndex++;
				singularId = 3;
			}

			if ( lastIndex != 1 )
				singularId = -1;

			if ( m_cachedOrderId == -1 )
				m_cachedOrderId = Shader.PropertyToID( "_Order" );

			if ( m_cachedSingularId == -1 )
				m_cachedSingularId = Shader.PropertyToID( "_Singular" );

			if ( m_cachedModeId == -1 )
			{
				m_cachedModeId = Shader.PropertyToID( "_Mode" );
			}

			if ( m_cachedCountId == -1 )
			{
				m_cachedCountId = Shader.PropertyToID( "_Count" );
			}

			if ( m_cachedClampId == -1 )
			{
				m_cachedClampId = Shader.PropertyToID( "_Clamp" );
			}

			PreviewMaterial.SetVector( m_cachedOrderId, order );
			PreviewMaterial.SetFloat( m_cachedSingularId, singularId );
			PreviewMaterial.SetFloat( m_cachedModeId, ( int )m_mode );
			PreviewMaterial.SetFloat( m_cachedCountId, ComponentCount() );
			PreviewMaterial.SetFloat( m_cachedClampId, m_clampIndex ? 1 : 0 );
		}

		public override void OnInputPortConnected( int portId, int otherNodeId, int otherPortId, bool activateNode = true )
		{
			base.OnInputPortConnected( portId, otherNodeId, otherPortId, activateNode );
			UpdatePorts();
			UpdateTitle();
		}

		public override void OnConnectedOutputNodeChanges( int outputPortId, int otherNodeId, int otherPortId, string name, WirePortDataType type )
		{
			base.OnConnectedOutputNodeChanges( outputPortId, otherNodeId, otherPortId, name, type );
			UpdatePorts();
			UpdateTitle();
		}

		public override void OnInputPortDisconnected( int portId )
		{
			base.OnInputPortDisconnected( portId );
			UpdateTitle();
		}

		void UpdatePorts()
		{
			m_inputPorts[ 0 ].MatchPortToConnection();

			m_inputPorts[ IndexPortIdx ].Visible = ( m_mode == ComponentMaskMode.Index );
			m_sizeIsDirty = true;

			int count = ComponentCount();

			switch ( m_mode )
			{
				// @diogo: collapses the input to one component, except on scalars and matrices where there is nothing to pick
				case ComponentMaskMode.Index:
				{
					// @diogo: port types are restored from the saved graph, so pin them back after every load
					m_inputPorts[ IndexPortIdx ].ChangeType( WirePortDataType.INT, false );
					ChangeOutputType( ( count > 0 ) ? WirePortDataType.FLOAT : m_inputPorts[ 0 ].DataType, false );
				}
				break;
				case ComponentMaskMode.Mask:
				{
					int activeCount = 0;
					if ( count > 0 )
					{
						for ( int i = 0; i < count; i++ )
						{
							if ( m_selection[ i ] )
								activeCount += 1;
						}
					}

					m_outputPortCount = activeCount;
					switch ( activeCount )
					{
						case 0: ChangeOutputType( m_inputPorts[ 0 ].DataType, false ); break;
						case 1: ChangeOutputType( WirePortDataType.FLOAT, false ); break;
						case 2: ChangeOutputType( WirePortDataType.FLOAT2, false ); break;
						case 3: ChangeOutputType( WirePortDataType.FLOAT3, false ); break;
						case 4: ChangeOutputType( m_inputPorts[ 0 ].DataType, false ); break;
					}
				}
				break;
			}
		}

		private void UpdateTitle()
		{
			switch ( m_mode )
			{
				case ComponentMaskMode.Index:
				{
					SetAdditonalTitleText( "Mode( Index )" );
				}
				break;
				case ComponentMaskMode.Mask:
				{
					int count = ComponentCount();
					string additionalText = string.Empty;

					if ( count > 0 )
					{
						for ( int i = 0; i < count; i++ )
						{
							if ( m_selection[ i ] )
							{
								additionalText += UIUtils.GetComponentForPosition( i, m_inputPorts[ 0 ].DataType ).ToUpper();
							}
						}
					}

					if ( additionalText.Length > 0 )
						SetAdditonalTitleText( "Value( " + additionalText + " )" );
					else
						SetAdditonalTitleText( string.Empty );
				}
				break;
			}
		}

		public override void DrawProperties()
		{
			NodeUtils.DrawPropertyGroup( ref m_propertiesFoldout, Constants.ParameterLabelStr, PropertyGroup );

			if ( m_mode == ComponentMaskMode.Index )
			{
				NodeUtils.DrawHelpBox( IndexModeInfoMsg, MessageType.Info );

				if ( !m_clampIndex )
				{
					NodeUtils.DrawHelpBox( UnclampedIndexInfoMsg, MessageType.Warning );
				}
			}
		}

		private void PropertyGroup()
		{
			EditorGUILayout.BeginVertical();

			EditorGUI.BeginChangeCheck();
			m_mode = ( ComponentMaskMode )EditorGUILayoutEnumPopup( ModeStr, m_mode );
			if ( EditorGUI.EndChangeCheck() )
			{
				UpdatePorts();
				UpdateTitle();
				SetSaveIsDirty();
			}

			if ( m_mode == ComponentMaskMode.Index )
			{
				EditorGUI.BeginChangeCheck();
				m_clampIndex = EditorGUILayoutToggle( ClampIndexStr, m_clampIndex );
				if ( EditorGUI.EndChangeCheck() )
				{
					PreviewIsDirty = true;
					SetSaveIsDirty();
				}
			}

			if ( m_mode == ComponentMaskMode.Mask )
			{
				int count = ComponentCount();

				int activeCount = 0;
				if ( count > 0 )
				{
					for ( int i = 0; i < count; i++ )
					{
						m_selection[ i ] = EditorGUILayoutToggleLeft( m_labels[i], m_selection[ i ] );
						m_labels[ i ] = UIUtils.GetComponentForPosition( i, m_inputPorts[ 0 ].DataType ).ToUpper();
						if ( m_selection[ i ] )
						{
							activeCount += 1;
						}
					}
				}

				if ( activeCount != m_outputPortCount )
				{
					m_outputPortCount = activeCount;
					switch ( activeCount )
					{
						case 0: ChangeOutputType( m_inputPorts[ 0 ].DataType, false ); break;
						case 1: ChangeOutputType( WirePortDataType.FLOAT, false ); break;
						case 2: ChangeOutputType( WirePortDataType.FLOAT2, false ); break;
						case 3: ChangeOutputType( WirePortDataType.FLOAT3, false ); break;
						case 4: ChangeOutputType( m_inputPorts[ 0 ].DataType, false ); break;
					}
					UpdateTitle();
					SetSaveIsDirty();
				}
			}

			EditorGUILayout.EndVertical();
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalVar )
		{

			if( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
				return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );

			string value = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );

			int count = ComponentCount();

			switch ( m_mode )
			{
				case ComponentMaskMode.Index:
				{
					if ( count > 0 )
					{
						string index = m_inputPorts[ IndexPortIdx ].GeneratePortInstructions( ref dataCollector );
						string localVarName = OutputLocalVarName + OutputId;
						// @diogo: dynamic component indexing only compiles on an lvalue, hence the extra local
						dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, InputDataType, localVarName, value );
						if ( m_clampIndex )
						{
							index = "clamp( " + index + ", 0, " + ( count - 1 ) + " )";
						}
						value = localVarName + "[ " + index + " ]";
					}
				}
				break;
				case ComponentMaskMode.Mask:
				{
					if ( count > 0 )
					{
						bool firstElement = true;
						value = string.Format("({0})",value);
						for ( int i = 0; i < count; i++ )
						{
							if ( m_selection[ i ] )
							{
								if( firstElement )
								{
									firstElement = false;
									value += ".";
								}
								value += UIUtils.GetComponentForPosition( i, m_inputPorts[ 0 ].DataType );
							}
						}
					}
				}
				break;
			}

			return CreateOutputLocalVariable( 0, value, ref dataCollector );
		}

		public string GetComponentForPosition( int i )
		{
			switch ( i )
			{
				case 0:
				{
					return ( ( m_outputPorts[ 0 ].DataType == WirePortDataType.COLOR ) ? "r" : "x" );
				}
				case 1:
				{
					return ( ( m_outputPorts[ 0 ].DataType == WirePortDataType.COLOR ) ? "g" : "y" );
				}
				case 2:
				{
					return ( ( m_outputPorts[ 0 ].DataType == WirePortDataType.COLOR ) ? "b" : "z" );
				}
				case 3:
				{
					return ( ( m_outputPorts[ 0 ].DataType == WirePortDataType.COLOR ) ? "a" : "w" );
				}
			}
			return string.Empty;
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			for ( int i = 0; i < 4; i++ )
			{
				m_selection[ i ] = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
			}

			if ( UIUtils.CurrentShaderVersion() > 19912 )
			{
				// @diogo: a mode no longer in the enum falls back to Mask instead of throwing
				Enum.TryParse<ComponentMaskMode>( GetCurrentParam( ref nodeParams ), out m_mode );
				m_clampIndex = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
			}

			// @diogo: every legacy graph is Mask mode and must keep loading with the ports CommonInit left it
			if ( m_mode != ComponentMaskMode.Mask )
			{
				UpdatePorts();
			}
			UpdateTitle();
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			for ( int i = 0; i < 4; i++ )
			{
				IOUtils.AddFieldValueToString( ref nodeInfo, m_selection[ i ] );
			}
			IOUtils.AddFieldValueToString( ref nodeInfo, m_mode );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_clampIndex );
		}
	}
}

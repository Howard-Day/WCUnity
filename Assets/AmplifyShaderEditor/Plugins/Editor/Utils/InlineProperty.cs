using UnityEngine;
using UnityEditor;
using System;
using System.Globalization;

namespace AmplifyShaderEditor
{
	[System.Serializable]
	public class InlineProperty
	{
		[SerializeField]
		private float m_value = 0;

		[SerializeField]
		private bool m_active = false;

		[SerializeField]
		private int m_nodeId = -1;

		[SerializeField]
		private string m_nodePropertyName = string.Empty;

		[SerializeField]
		private bool m_inlineButtonVisible = true;

		public InlineProperty()
		{
			InlinePropertyTable.Register( this );
		}

		public InlineProperty( float val ) : base()
		{
			m_value = val;
		}

		public InlineProperty( int val ) : base()
		{
			m_value = val;
		}

		public void ResetProperty()
		{
			m_nodeId = -1;
			m_active = false;
			m_nodePropertyName = string.Empty;
		}

		public void CopyFrom( InlineProperty other, bool copyPropertyName = false )
		{
			m_value = other.m_value;
			m_active = other.m_active;
			m_nodeId = other.m_nodeId;
			if ( copyPropertyName )
			{
				m_nodePropertyName = other.m_nodePropertyName;
			}
		}

		public void SetInlineByName( string propertyName )
		{
			m_nodeId = UIUtils.GetFloatIntNodeIdByName( propertyName );
			m_nodePropertyName = propertyName;
			m_active = !string.IsNullOrEmpty( propertyName );
		}

		public void CheckInlineButton()
		{
			if( m_inlineButtonVisible )
			{
				if( GUILayout.Button( UIUtils.FloatIntIconON , UIUtils.FloatIntPickerONOFF , GUILayout.Width( 15 ) , GUILayout.Height( 15 ) ) )
					m_active = !m_active;
			}
		}

		public void IntField( ref UndoParentNode owner , string content )
		{
			if( !m_active )
			{
				EditorGUILayout.BeginHorizontal();
				m_value = owner.EditorGUILayoutIntField( content , (int)m_value );
				CheckInlineButton();
				EditorGUILayout.EndHorizontal();
			}
			else
			{
				DrawPicker( ref owner , content );
			}
		}

		public void IntSlider( ref UndoParentNode owner , GUIContent content , int min , int max )
		{
			if( !m_active )
			{
				EditorGUILayout.BeginHorizontal();
				m_value = owner.EditorGUILayoutIntSlider( content , (int)m_value , min , max );
				CheckInlineButton();
				EditorGUILayout.EndHorizontal();
			}
			else
			{
				DrawPicker( ref owner , content );
			}
		}

		public void IntSlider( ref UndoParentNode owner , string content , int min , int max )
		{
			if( !m_active )
			{
				EditorGUILayout.BeginHorizontal();
				m_value = owner.EditorGUILayoutIntSlider( content , (int)m_value , min , max );
				CheckInlineButton();
				EditorGUILayout.EndHorizontal();
			}
			else
			{
				DrawPicker( ref owner , content );
			}
		}

		public void EnumTypePopup( ref UndoParentNode owner , string content , string[] displayOptions )
		{
			if( !m_active )
			{
				EditorGUILayout.BeginHorizontal();
				m_value = owner.EditorGUILayoutPopup( content , (int)m_value , displayOptions );
				CheckInlineButton();
				EditorGUILayout.EndHorizontal();

			}
			else
			{
				DrawPicker( ref owner , content );
			}
		}

		public void FloatField( ref UndoParentNode owner , string content )
		{
			if( !m_active )
			{
				EditorGUILayout.BeginHorizontal();
				m_value = owner.EditorGUILayoutFloatField( content , m_value );
				CheckInlineButton();
				EditorGUILayout.EndHorizontal();
			}
			else
			{
				DrawPicker( ref owner , content );
			}
		}

		public void SliderField( ref UndoParentNode owner , string content , float min , float max )
		{
			if( !m_active )
			{
				EditorGUILayout.BeginHorizontal();
				m_value = owner.EditorGUILayoutSlider( content , m_value , min , max );
				CheckInlineButton();
				EditorGUILayout.EndHorizontal();
			}
			else
			{
				DrawPicker( ref owner , content );
			}
		}

		public void RangedFloatField( ref UndoParentNode owner , string content , float min , float max )
		{
			if( !m_active )
			{
				EditorGUILayout.BeginHorizontal();
				m_value = owner.EditorGUILayoutRangedFloatField( content , m_value , min , max );
				CheckInlineButton();
				EditorGUILayout.EndHorizontal();
			}
			else
			{
				DrawPicker( ref owner , content );
			}
		}


		public void CustomDrawer( ref UndoParentNode owner , DrawPropertySection Drawer , string content )
		{
			if( !m_active )
			{
				EditorGUILayout.BeginHorizontal();
				Drawer( owner );
				CheckInlineButton();
				EditorGUILayout.EndHorizontal();
			}
			else
			{
				DrawPicker( ref owner , content );
			}
		}

		public delegate void DrawPropertySection( UndoParentNode owner );

		private void DrawPicker( ref UndoParentNode owner , GUIContent content )
		{
			DrawPicker( ref owner , content.text );
		}

		private void DrawPicker( ref UndoParentNode owner , string content )
		{
			EditorGUILayout.BeginHorizontal();
			string[] intArraysNames = owner.ContainerGraph.ParentWindow.CurrentGraph.FloatIntNodes.NodesArr;
			int[] intIds = owner.ContainerGraph.ParentWindow.CurrentGraph.FloatIntNodes.NodeIds;
			int prevNodeId = m_nodeId;
			m_nodeId = owner.EditorGUILayoutIntPopup( content , m_nodeId , intArraysNames , intIds );
			if ( m_nodeId != prevNodeId )
			{
				m_nodePropertyName = UIUtils.GetFloatIntNameByNodeId( m_nodeId, m_nodePropertyName );
			}
			if ( GUILayout.Button( UIUtils.FloatIntIconOFF , UIUtils.FloatIntPickerONOFF , GUILayout.Width( 15 ) , GUILayout.Height( 15 ) ) )
				m_active = !m_active;
			EditorGUILayout.EndHorizontal();
		}

		public string GetValueOrProperty( bool parentesis = true )
		{
			if( m_active )
			{
				PropertyNode node = GetPropertyNode();
				if( node != null )
				{
					if ( node.CurrentParameterType == PropertyType.Constant )
					{
						return node.GetDefaultValue();
					}
					else
					{
						return parentesis ? "[" + node.PropertyName + "]" : node.PropertyName;
					}
				}
				else if ( !string.IsNullOrEmpty( m_nodePropertyName ) )
				{
					return parentesis ? "[" + m_nodePropertyName + "]" : m_nodePropertyName;
				}
				else
				{
					m_active = false;
					m_nodeId = -1;
					return m_value.ToString();
				}
			}
			else
			{
				return m_value.ToString();
			}
		}

		public string GetValueOrProperty( string defaultValue , bool parentesis = true )
		{
			if( m_active )
			{
				PropertyNode node = GetPropertyNode();
				if( node != null )
				{
					if ( node.CurrentParameterType == PropertyType.Constant )
					{
						return node.GetDefaultValue();
					}
					else
					{
						return parentesis ? "[" + node.PropertyName + "]" : node.PropertyName;
					}
				}
				else if ( !string.IsNullOrEmpty( m_nodePropertyName ) )
				{
					return parentesis ? "[" + m_nodePropertyName + "]" : m_nodePropertyName;
				}
				else if( !string.IsNullOrEmpty( defaultValue ) )
				{
					m_active = false;
					m_nodeId = -1;
					return defaultValue;
				}
				else
				{
					m_active = false;
					m_nodeId = -1;
					return m_value.ToString();
				}
			}
			else
			{
				return defaultValue;
			}
		}

		public void TryResolveDependency()
		{
			if ( m_active && !string.IsNullOrEmpty( m_nodePropertyName ) )
			{
				m_nodeId = UIUtils.GetFloatIntNodeIdByName( m_nodePropertyName );
				RevertIfUnresolved();
			}
		}

		// @diogo: when an inline reference points at a property that no longer exists (e.g. it came from a node
		//         or shader function that was since removed), revert to the template-defined value instead of
		//         keeping a dangling reference. Relies on m_nodeId having just been resolved from the name, so a
		//         -1 id means the property is genuinely absent from the fully-loaded graph (internal template
		//         properties are also registered as float/int nodes, so they resolve to a valid id too).
		public void RevertIfUnresolved()
		{
			if ( m_active && m_nodeId == -1 && !string.IsNullOrEmpty( m_nodePropertyName ) )
			{
				ResetProperty();
			}
		}

		// Reverts to the template value when the referenced node is gone and no property with its name remains
		public void RevertIfDangling()
		{
			if ( m_active && GetPropertyNode() == null )
			{
				m_nodeId = UIUtils.GetFloatIntNodeIdByName( m_nodePropertyName );
				RevertIfUnresolved();
			}
		}

		private void TryReadUniqueId( string param )
		{
			if ( Preferences.User.ForceTemplateInlineProperties && !string.IsNullOrEmpty( m_nodePropertyName ) )
			{
				// @diogo: exception path => ignore param and revert to template default
				m_nodeId = UIUtils.GetFloatIntNodeIdByName( m_nodePropertyName );

				// @diogo: by defaulting to template we are signaling the inline property is active
				m_active = true;
			}
			else
			{
				// @diogo: normal path
				if ( int.TryParse( param, out int nodeId ) )
				{
					m_nodeId = Convert.ToInt32( param );
					m_nodePropertyName = UIUtils.GetFloatIntNameByNodeId( m_nodeId, m_nodePropertyName );
				}
				else
				{
					m_nodePropertyName = param;
					m_nodeId = UIUtils.GetFloatIntNodeIdByName( m_nodePropertyName );
				}
			}
		}

		public void ReadFromString( ref uint index , ref string[] nodeParams , bool isInt = true )
		{
			m_value = isInt ? Convert.ToInt32( nodeParams[ index++ ] ) : Convert.ToSingle( nodeParams[ index++ ] );
			m_active = Convert.ToBoolean( nodeParams[ index++ ] );
			TryReadUniqueId( nodeParams[ index++ ] );
		}

		public void ReadFromSingle( string singleLine )
		{
			string[] data = singleLine.Split( IOUtils.VECTOR_SEPARATOR );
			m_value = Convert.ToSingle( data[ 0 ] , System.Globalization.CultureInfo.InvariantCulture );
			m_active = Convert.ToBoolean( data[ 1 ] );
			TryReadUniqueId( data[ 2 ] );
		}

		public void WriteToString( ref string nodeInfo )
		{
			// @diogo: only persist the property name while the inline property is active; otherwise a stale
			// name lingers in the data and gets re-read (and possibly re-activated) on the next load
			string propertyName = m_active ? m_nodePropertyName : string.Empty;

			IOUtils.AddFieldValueToString( ref nodeInfo , m_value );
			IOUtils.AddFieldValueToString( ref nodeInfo , m_active );
			IOUtils.AddFieldValueToString( ref nodeInfo, propertyName );
		}

		public string WriteToSingle()
		{
			string propertyName = m_active ? m_nodePropertyName : string.Empty;
			return m_value.ToString( CultureInfo.InvariantCulture ) + IOUtils.VECTOR_SEPARATOR + m_active + IOUtils.VECTOR_SEPARATOR + propertyName;
		}

		public void SetInlineNodeValue()
		{
			if ( IsValid )
			{
				ParentNode node = UIUtils.GetNode( m_nodeId );
				if ( node is RangedFloatNode fnode )
				{
					fnode.Value = m_value;
					fnode.SetMaterialValueFromInline( m_value );
				}
				else if ( node is IntNode inode )
				{
					inode.Value = ( int )m_value;
					inode.SetMaterialValueFromInline( ( int )m_value );
				}
				else if ( node is IntegerNode integerNode )
				{
					integerNode.Value = ( int )m_value;
					integerNode.SetMaterialValueFromInline( ( int )m_value );
				}
			}
		}

		public bool IsValid { get { return m_active; } }

		public PropertyNode GetPropertyNode()
		{
			if( m_nodeId >= 0 )
				return UIUtils.GetNode( m_nodeId ) as PropertyNode;

			if( m_nodeId < -1 )
			{
				if( !string.IsNullOrEmpty( m_nodePropertyName ) )
					return UIUtils.GetInternalTemplateNode( m_nodePropertyName );


				return UIUtils.GetInternalTemplateNode( m_nodeId );
			}

			return null;
		}

		public void HideInlineButton()
		{
			m_inlineButtonVisible = false;
		}

		public int IntValue { get { return (int)m_value; } set { m_value = value; } }
		public float FloatValue { get { return m_value; } set { m_value = value; } }
		public bool Active { get { return m_active; } set { m_active = value; } }
		public int NodeId { get { return m_nodeId; } set { m_nodeId = value; } }
	}
}

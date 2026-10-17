// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using UnityEditor;
using System;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Integer", "Constants And Properties", "Integer property", null, KeyCode.Alpha0 )]
	public sealed class IntegerNode : PropertyNode
	{
		private const string MinValueStr = "Min";
		private const string MaxValueStr = "Max";

		[SerializeField]
		private int m_defaultValue;

		[SerializeField]
		private int m_materialValue;

		[SerializeField]
		private int m_min = 0;

		[SerializeField]
		private int m_max = 0;

		[SerializeField]
		private bool m_intMode = true;

		[SerializeField]
		private bool m_setAsUINT = false;

		private const float LabelWidth = 8;

		private int m_cachedPropertyId = -1;

		private bool m_isEditingFields;
		private int[] m_previousValue = { 0, 0, 0 };
		private string[] m_fieldText = new string[] { "0", "0", "0" };

		public IntegerNode() : base() { }
		public IntegerNode( int uniqueId, float x, float y, float width, float height ) : base( uniqueId, x, y, width, height ) { }

		// @diogo: preview frag returns a single constant value, independent of UV (QA #5).
		public override bool ConstantPreview { get { return true; } }

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			GlobalTypeWarningText = string.Format( GlobalTypeWarningText, "Integer" );
			AddOutputPort( WirePortDataType.INT, Constants.EmptyPortValue );
			m_customPrefix = "Integer ";
			m_insideSize.Set( 50, 10 );
			m_selectedLocation = PreviewLocation.BottomCenter;
			m_drawPrecisionUI = false;
			m_showHybridInstancedUI = true;
			m_availableAttribs.Add( new PropertyAttributes( "Enum", "[Enum]" ) );
			m_previewShaderGUID = "0f64d695b6ffacc469f2dd31432a232a";
			m_srpBatcherCompatible = true;
			m_canBeReferenced = true;
		}
		protected override void OnUniqueIDAssigned()
		{
			base.OnUniqueIDAssigned();
			UIUtils.RegisterFloatIntNode( this );
		}

		public override void Destroy()
		{
			base.Destroy();
			UIUtils.UnregisterFloatIntNode( this );
		}

		public override void OnDirtyProperty()
		{
			UIUtils.UpdateFloatIntDataNode( UniqueId, PropertyInspectorName );
		}

		public override void RefreshExternalReferences()
		{
			base.RefreshExternalReferences();
			OnPropertyNameChanged();
			OnDirtyProperty();
		}

		public override void SetPreviewInputs()
		{
			base.SetPreviewInputs();

			if ( m_cachedPropertyId == -1 )
			{
				m_cachedPropertyId = Shader.PropertyToID( "_InputInt" );
			}

			if ( m_materialMode && m_currentParameterType != PropertyType.Constant )
			{
				PreviewMaterial.SetInt( m_cachedPropertyId, m_materialValue );
			}
			else
			{
				PreviewMaterial.SetInt( m_cachedPropertyId, m_defaultValue );
			}
		}

		public void SetIntMode( bool value )
		{
			if ( m_intMode == value )
			{
				return;
			}

			m_intMode = value;
			if ( value )
			{
				m_insideSize.x = 50;// + ( m_showPreview ? 50 : 0 );
				//m_firstPreviewDraw = true;
			}
			else
			{
				m_insideSize.x = 200;// + ( m_showPreview ? 0 : 0 );
				//m_firstPreviewDraw = true;
			}
			m_sizeIsDirty = true;
		}

		public override void CopyDefaultsToMaterial()
		{
			m_materialValue = m_defaultValue;
			DrawSetAsUINT();
		}

		void DrawMinMaxUI()
		{
			EditorGUI.BeginChangeCheck();
			m_min = EditorGUILayoutIntField( MinValueStr, m_min );
			m_max = EditorGUILayoutIntField( MaxValueStr, m_max );
			if ( m_min > m_max )
			{
				m_min = m_max;
			}

			if ( m_max < m_min )
			{
				m_max = m_min;
			}

			if ( EditorGUI.EndChangeCheck() )
			{
				SetIntMode( m_min == m_max );
			}
		}

		public override void DrawSubProperties()
		{
			DrawMinMaxUI();

			if ( m_intMode )
			{
				m_defaultValue = EditorGUILayoutIntField( Constants.DefaultValueLabel, m_defaultValue );
			}
			else
			{
				m_defaultValue = EditorGUILayoutIntSlider( Constants.DefaultValueLabel, m_defaultValue, m_min, m_max );
			}

			DrawSetAsUINT();
		}

		private void DrawSetAsUINT()
		{
			EditorGUI.BeginChangeCheck();
			m_setAsUINT = EditorGUILayoutToggle( "Set as UINT", m_setAsUINT );
			if ( EditorGUI.EndChangeCheck() )
			{
				WirePortDataType portType = m_setAsUINT ? WirePortDataType.UINT : WirePortDataType.INT;
				m_outputPorts[ 0 ].ChangeType( portType, false );
			}
		}

		public override void DrawMaterialProperties()
		{
			if ( m_materialMode )
			{
				EditorGUI.BeginChangeCheck();
			}

			if ( m_intMode )
			{
				m_materialValue = EditorGUILayoutIntField( Constants.MaterialValueLabel, m_materialValue );
			}
			else
			{
				m_materialValue = EditorGUILayoutIntSlider( Constants.MaterialValueLabel, m_materialValue, m_min, m_max );
			}

			if ( m_materialMode && EditorGUI.EndChangeCheck() )
			{
				m_requireMaterialUpdate = true;
			}
		}

		public override void OnNodeLayout( DrawInfo drawInfo, NodeUpdateCache cache )
		{
			base.OnNodeLayout( drawInfo, cache );

			if ( m_intMode )
			{
				m_propertyDrawPos = m_remainingBox;
				m_propertyDrawPos.x = m_remainingBox.x - LabelWidth * drawInfo.InvertedZoom;
				m_propertyDrawPos.width = drawInfo.InvertedZoom * Constants.FLOAT_DRAW_WIDTH_FIELD_SIZE;
				m_propertyDrawPos.height = drawInfo.InvertedZoom * Constants.FLOAT_DRAW_HEIGHT_FIELD_SIZE;
			}
			else
			{
				m_propertyDrawPos = m_remainingBox;
				m_propertyDrawPos.width = m_outputPorts[ 0 ].Position.x - m_propertyDrawPos.x - ( m_outputPorts[ 0 ].LabelSize.x + ( Constants.PORT_TO_LABEL_SPACE_X + 3 ) * drawInfo.InvertedZoom + 2 );
				m_propertyDrawPos.height = drawInfo.InvertedZoom * Constants.FLOAT_DRAW_HEIGHT_FIELD_SIZE;
			}
		}

		public override void DrawGUIControls( DrawInfo drawInfo )
		{
			base.DrawGUIControls( drawInfo );

			if ( IsPropertyReference )
			{
				m_isEditingFields = false;
				return;
			}

			if ( drawInfo.CurrentEventType != EventType.MouseDown )
			{
				return;
			}

			Rect hitBox = m_remainingBox;
			hitBox.xMin -= LabelWidth * drawInfo.InvertedZoom;
			bool insideBox = hitBox.Contains( drawInfo.MousePosition );

			if ( insideBox )
			{
				GUI.FocusControl( null );
				m_isEditingFields = true;
			}
			else if ( m_isEditingFields && !insideBox )
			{
				GUI.FocusControl( null );
				m_isEditingFields = false;
			}
		}

		void DrawFakeIntMaterial( DrawInfo drawInfo )
		{
			if ( m_intMode )
			{
				Rect fakeField = m_propertyDrawPos;
				fakeField.xMin += LabelWidth * drawInfo.InvertedZoom;
				if ( GUI.enabled )
				{
					Rect fakeLabel = m_propertyDrawPos;
					fakeLabel.xMax = fakeField.xMin;
					EditorGUIUtility.AddCursorRect( fakeLabel, MouseCursor.SlideArrow );
					EditorGUIUtility.AddCursorRect( fakeField, MouseCursor.Text );
				}
				if ( m_previousValue[ 0 ] != m_materialValue )
				{
					m_previousValue[ 0 ] = m_materialValue;
					m_fieldText[ 0 ] = m_materialValue.ToString();
				}

				GUI.Label( fakeField, m_fieldText[ 0 ], UIUtils.MainSkin.textField );
			}
			else
			{
				DrawFakeSlider( ref m_materialValue, drawInfo );
			}
		}

		public override void Draw( DrawInfo drawInfo )
		{
			base.Draw( drawInfo );

			if ( !m_isVisible )
			{
				return;
			}

			if ( m_isEditingFields && m_currentParameterType != PropertyType.Global )
			{
				if ( m_materialMode && m_currentParameterType != PropertyType.Constant )
				{
					EditorGUI.BeginChangeCheck();
					if ( m_intMode )
					{
						UIUtils.DrawInt( this, ref m_propertyDrawPos, ref m_materialValue, LabelWidth * drawInfo.InvertedZoom );
					}
					else
					{
						DrawSlider( ref m_materialValue, drawInfo );
					}
					if ( EditorGUI.EndChangeCheck() )
					{
						PreviewIsDirty = true;
						m_requireMaterialUpdate = true;
						if ( m_currentParameterType != PropertyType.Constant )
						{
							BeginDelayedDirtyProperty();
						}
					}
				}
				else
				{
					EditorGUI.BeginChangeCheck();

					if ( m_intMode )
					{
						UIUtils.DrawInt( this, ref m_propertyDrawPos, ref m_defaultValue, LabelWidth * drawInfo.InvertedZoom );
					}
					else
					{
						DrawSlider( ref m_defaultValue, drawInfo );
					}
					if ( EditorGUI.EndChangeCheck() )
					{
						PreviewIsDirty = true;
						BeginDelayedDirtyProperty();
					}

				}
			}
			else if ( drawInfo.CurrentEventType == EventType.Repaint && ContainerGraph.LodLevel <= ParentGraph.NodeLOD.LOD4 )
			{
				if ( m_currentParameterType == PropertyType.Global )
				{
					bool guiEnabled = GUI.enabled;
					GUI.enabled = false;
					DrawFakeIntMaterial( drawInfo );
					GUI.enabled = guiEnabled;
				}
				else if ( m_materialMode && m_currentParameterType != PropertyType.Constant )
				{
					DrawFakeIntMaterial( drawInfo );
				}
				else
				{
					if ( m_intMode )
					{
						//UIUtils.DrawFloat( this, ref m_propertyDrawPos, ref m_defaultValue, LabelWidth * drawInfo.InvertedZoom );
						Rect fakeField = m_propertyDrawPos;
						fakeField.xMin += LabelWidth * drawInfo.InvertedZoom;
						Rect fakeLabel = m_propertyDrawPos;
						fakeLabel.xMax = fakeField.xMin;
						EditorGUIUtility.AddCursorRect( fakeLabel, MouseCursor.SlideArrow );
						EditorGUIUtility.AddCursorRect( fakeField, MouseCursor.Text );

						if ( m_previousValue[ 0 ] != m_defaultValue )
						{
							m_previousValue[ 0 ] = m_defaultValue;
							m_fieldText[ 0 ] = m_defaultValue.ToString();
						}

						GUI.Label( fakeField, m_fieldText[ 0 ], UIUtils.MainSkin.textField );
					}
					else
					{
						DrawFakeSlider( ref m_defaultValue, drawInfo );
					}
				}
			}
		}

		void DrawFakeSlider( ref int value, DrawInfo drawInfo )
		{
			float rangeWidth = 30 * drawInfo.InvertedZoom;
			float rangeSpacing = 5 * drawInfo.InvertedZoom;

			//Min
			Rect minRect = m_propertyDrawPos;
			minRect.width = rangeWidth;
			EditorGUIUtility.AddCursorRect( minRect, MouseCursor.Text );
			if ( m_previousValue[ 1 ] != m_min )
			{
				m_previousValue[ 1 ] = m_min;
				m_fieldText[ 1 ] = m_min.ToString();
			}
			GUI.Label( minRect, m_fieldText[ 1 ], UIUtils.MainSkin.textField );

			//Value Area
			Rect valRect = m_propertyDrawPos;
			valRect.width = rangeWidth;
			valRect.x = m_propertyDrawPos.xMax - rangeWidth - rangeWidth - rangeSpacing;
			EditorGUIUtility.AddCursorRect( valRect, MouseCursor.Text );
			if ( m_previousValue[ 0 ] != value )
			{
				m_previousValue[ 0 ] = value;
				m_fieldText[ 0 ] = value.ToString();
			}
			GUI.Label( valRect, m_fieldText[ 0 ], UIUtils.MainSkin.textField );

			//Max
			Rect maxRect = m_propertyDrawPos;
			maxRect.width = rangeWidth;
			maxRect.x = m_propertyDrawPos.xMax - rangeWidth;
			EditorGUIUtility.AddCursorRect( maxRect, MouseCursor.Text );
			if ( m_previousValue[ 2 ] != m_max )
			{
				m_previousValue[ 2 ] = m_max;
				m_fieldText[ 2 ] = m_max.ToString();
			}
			GUI.Label( maxRect, m_fieldText[ 2 ], UIUtils.MainSkin.textField );

			Rect sliderValRect = m_propertyDrawPos;
			sliderValRect.x = minRect.xMax + rangeSpacing;
			sliderValRect.xMax = valRect.xMin - rangeSpacing;
			Rect sliderBackRect = sliderValRect;
			sliderBackRect.height = 5 * drawInfo.InvertedZoom;
			sliderBackRect.center = new Vector2( sliderValRect.center.x, Mathf.Round( sliderValRect.center.y ) );


			GUI.Label( sliderBackRect, string.Empty, UIUtils.GetCustomStyle( CustomStyle.SliderStyle ) );

			sliderValRect.width = 10;
			float percent = ( value - m_min ) / ( m_max - m_min );
			percent = Mathf.Clamp01( percent );
			sliderValRect.x += percent * ( sliderBackRect.width - 10 * drawInfo.InvertedZoom );
			GUI.Label( sliderValRect, string.Empty, UIUtils.RangedFloatSliderThumbStyle );
		}

		void DrawSlider( ref int value, DrawInfo drawInfo )
		{
			float rangeWidth = 30 * drawInfo.InvertedZoom;
			float rangeSpacing = 5 * drawInfo.InvertedZoom;

			//Min
			Rect minRect = m_propertyDrawPos;
			minRect.width = rangeWidth;
			m_min = EditorGUIIntField( minRect, m_min, UIUtils.MainSkin.textField );

			//Value Area
			Rect valRect = m_propertyDrawPos;
			valRect.width = rangeWidth;
			valRect.x = m_propertyDrawPos.xMax - rangeWidth - rangeWidth - rangeSpacing;
			value = EditorGUIIntField( valRect, value, UIUtils.MainSkin.textField );

			//Max
			Rect maxRect = m_propertyDrawPos;
			maxRect.width = rangeWidth;
			maxRect.x = m_propertyDrawPos.xMax - rangeWidth;
			m_max = EditorGUIIntField( maxRect, m_max, UIUtils.MainSkin.textField );

			//Value Slider
			Rect sliderValRect = m_propertyDrawPos;
			sliderValRect.x = minRect.xMax + rangeSpacing;
			sliderValRect.xMax = valRect.xMin - rangeSpacing;
			Rect sliderBackRect = sliderValRect;
			sliderBackRect.height = 5 * drawInfo.InvertedZoom;
			sliderBackRect.center = new Vector2( sliderValRect.center.x, Mathf.Round( sliderValRect.center.y ) );
			GUI.Label( sliderBackRect, string.Empty, UIUtils.GetCustomStyle( CustomStyle.SliderStyle ) );
			value = ( int )GUIHorizontalSlider( sliderValRect, ( float )value, ( float )m_min, ( float )m_max, GUIStyle.none, UIUtils.RangedFloatSliderThumbStyle );
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			PropertyNode reference = PropertyReference;
			if ( reference != null )
			{
				OrderIndex = reference.RawOrderIndex;
				return reference.GenerateShaderForOutput( outputId, ref dataCollector, ignoreLocalvar );
			}

			base.GenerateShaderForOutput( outputId, ref dataCollector, ignoreLocalvar );

			if ( m_currentParameterType != PropertyType.Constant )
			{
				return PropertyData( dataCollector.PortCategory );
			}

			return m_defaultValue.ToString();
		}

		public override string GetPropertyValue()
		{
			string value = m_defaultValue.ToString();
			if ( m_intMode )
			{
				return PropertyAttributes + PropertyAttributesSeparator + m_propertyName + "( \"" + m_propertyInspectorName + "\", Integer ) = " + m_defaultValue;
			}
			else
			{
				return "[IntRange]" + PropertyAttributes + PropertyAttributesSeparator + m_propertyName + "( \"" + m_propertyInspectorName +
					"\", Range( " + m_min + ", " + m_max + " ) ) = " + value;
			}
		}

		public override string GetDefaultValue()
		{
			return m_defaultValue.ToString();
		}

		public override void UpdateMaterial( Material mat )
		{
			base.UpdateMaterial( mat );
			if ( UIUtils.IsProperty( m_currentParameterType ) && !InsideShaderFunction && !IsPropertyReference )
			{
				// @diogo: int mode declares an Integer property, slider mode a float Range one
				if ( m_intMode )
				{
					mat.SetInteger( m_propertyName, m_materialValue );
				}
				else
				{
					mat.SetInt( m_propertyName, m_materialValue );
				}
			}
		}

		public override void SetMaterialMode( Material mat, bool fetchMaterialValues )
		{
			base.SetMaterialMode( mat, fetchMaterialValues );
			if ( fetchMaterialValues && m_materialMode && UIUtils.IsProperty( m_currentParameterType ) && mat.HasProperty( m_propertyName ) )
			{
				m_materialValue = m_intMode ? mat.GetInteger( m_propertyName ) : mat.GetInt( m_propertyName );
			}
		}

		public override void ForceUpdateFromMaterial( Material material )
		{
			if ( UIUtils.IsProperty( m_currentParameterType ) && material.HasProperty( m_propertyName ) )
			{
				m_materialValue = m_intMode ? material.GetInteger( m_propertyName ) : material.GetInt( m_propertyName );
				PreviewIsDirty = true;
			}
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			m_defaultValue = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );

			if ( UIUtils.CurrentShaderVersion() > 14101 )
			{
				m_materialValue = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			}

			if ( UIUtils.CurrentShaderVersion() > 18500 )
			{
				m_setAsUINT = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
			}

			if ( UIUtils.CurrentShaderVersion() >= 19906 )
			{
				m_min = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
				m_max = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
				SetIntMode( m_min == m_max );
			}
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_defaultValue );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_materialValue );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_setAsUINT );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_min );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_max );
		}

		public override string GetPropertyValStr()
		{
			return ( m_materialMode && m_currentParameterType != PropertyType.Constant ) ?
				m_materialValue.ToString( Mathf.Abs( m_materialValue ) > 1000 ? Constants.PropertyBigIntFormatLabel : Constants.PropertyIntFormatLabel ) :
				m_defaultValue.ToString( Mathf.Abs( m_defaultValue ) > 1000 ? Constants.PropertyBigIntFormatLabel : Constants.PropertyIntFormatLabel );
		}

		protected override void CopyPropertyReferenceValues( PropertyNode reference )
		{
			IntegerNode node = reference as IntegerNode;
			if ( node == null )
			{
				return;
			}

			if ( m_defaultValue != node.m_defaultValue || m_materialValue != node.m_materialValue || m_min != node.m_min || m_max != node.m_max )
			{
				m_defaultValue = node.m_defaultValue;
				m_materialValue = node.m_materialValue;
				m_min = node.m_min;
				m_max = node.m_max;
				SetIntMode( m_min == m_max );
				PreviewIsDirty = true;
			}
		}

		public override Vector4 TypeChangeDefaultValue { get { return new Vector4( m_defaultValue, 0, 0, 0 ); } }
		public override Vector4 TypeChangeMaterialValue { get { return new Vector4( m_materialValue, 0, 0, 0 ); } }
		public override void SetTypeChangeValues( Vector4 defaultValue, Vector4 materialValue )
		{
			m_defaultValue = ( int )defaultValue.x;
			m_materialValue = ( int )materialValue.x;
		}

		public override Vector2 TypeChangeRangeValue { get { return new Vector2( m_min, m_max ); } }
		public override void SetTypeChangeRange( Vector2 range )
		{
			m_min = ( int )range.x;
			m_max = ( int )range.y;
			SetIntMode( m_min == m_max );
		}

		public override void SetGlobalValue() { Shader.SetGlobalInt( m_propertyName, m_defaultValue ); }
		public override void FetchGlobalValue() { m_materialValue = Shader.GetGlobalInt( m_propertyName ); }
		public int Value
		{
			get { return m_defaultValue; }
			set { m_defaultValue = value; }
		}

		public void SetMaterialValueFromInline( int val )
		{
			m_materialValue = val;
			m_requireMaterialUpdate = true;
		}
	}
}

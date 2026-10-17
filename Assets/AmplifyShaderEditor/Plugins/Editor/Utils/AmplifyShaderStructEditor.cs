// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AmplifyShaderEditor
{
	[CustomEditor( typeof( AmplifyShaderStruct ) )]
	public class AmplifyShaderStructEditor : Editor
	{
		private const string TypeStr = "Type";
		// @diogo: an asset cannot borrow, so this only ever decides whether ASE declares the type - the
		// node's Definition dropdown is the wider choice, and reports this same value back as Declaration
		private const string DeclarationStr = "Declaration";
		private const string FieldsStr = "Fields";
		private const string DefaultFieldName = "Field";
		private const string NoFieldsStr = "No fields yet. Add one with the + button.";
		private const string HelpStr =
			"The type name comes from this asset's file name, and Type shows the form it takes in the " +
			"generated shader.\n" +
			"\n" +
			"Declaration says who owns the type: Create for one ASE declares, Fetch for one that " +
			"already comes from an include, where the fields only drive the ports.\n" +
			"\n" +
			"Any Struct node can reference this asset, and every one of them builds the same type.";
		private const string UndoStr = "Edited Struct";

		private static readonly GUIContent NameTooltip = new GUIContent( string.Empty, "Field name, exactly as it appears in the generated struct." );
		private static readonly GUIContent TypeTooltip = new GUIContent( string.Empty, "Field type." );
		private static readonly GUIContent AddTooltip = new GUIContent( string.Empty, "Duplicate this field below it." );
		private static readonly GUIContent RemoveTooltip = new GUIContent( string.Empty, "Remove this field." );
		private static readonly GUIContent TintContent = new GUIContent( "Tint", "What this struct is drawn with: the header of every Struct node carrying it, and every wire it travels on." );

		// @diogo: ASE's own +/- styles are built by the editor window's skin init and nulled when it tears
		// down, and an asset can be inspected without that window ever opening - so the built-in list
		// styles are used here instead, with a plain button as the last resort
		private static GUIStyle PlusStyle { get { return FindStyle( "OL Plus" ); } }

		private static GUIStyle MinusStyle { get { return FindStyle( "OL Minus" ); } }

		private static GUIStyle FindStyle( string name )
		{
			GUIStyle style = ( GUI.skin != null ) ? GUI.skin.FindStyle( name ) : null;
			return ( style != null ) ? style : EditorStyles.miniButton;
		}

		private AmplifyShaderStruct m_target = null;
		private ReorderableList m_fieldsList = null;
		private List<StructFieldData> m_fieldsSource = null;
		private int m_actionIndex = -1;
		private ReordableAction m_actionType = ReordableAction.None;
		private bool m_actionInherit = false;
		private AssetDependencyList m_dependencies = new AssetDependencyList();

		private void OnEnable()
		{
			m_target = target as AmplifyShaderStruct;
			m_fieldsList = null;

			// @diogo: an asset saved before fields had ids gets them on first inspection, ahead of any
			// edit that would need them
			if ( m_target.Definition.EnsureFieldIds() )
			{
				EditorUtility.SetDirty( m_target );
			}
		}

		public override void OnInspectorGUI()
		{
			StructDefinitionData definition = m_target.Definition;

			// @diogo: the file name is already the inspector's own header, so only the form it generates
			// as is worth a row here - and that is not always obvious ( "2 Type" reads back as "_2Type" )
			NodeUtils.DrawDisabledValue( TypeStr, definition.CodeName );

			EditorGUI.BeginChangeCheck();
			VariableMode declarationMode = ( VariableMode )EditorGUILayout.EnumPopup( DeclarationStr, definition.DeclarationMode );
			if ( EditorGUI.EndChangeCheck() )
			{
				Undo.RecordObject( m_target, UndoStr );
				definition.DeclarationMode = declarationMode;
				EditorUtility.SetDirty( m_target );
			}

			EditorGUI.BeginChangeCheck();
			Color tint = EditorGUILayout.ColorField( TintContent, definition.Tint, true, false, false );
			if ( EditorGUI.EndChangeCheck() )
			{
				Undo.RecordObject( m_target, UndoStr );
				definition.Tint = tint;
				EditorUtility.SetDirty( m_target );
			}

			EditorGUILayout.Space();
			DrawFieldsHeader( definition );
			DrawFieldsList( definition );

			if ( m_actionType != ReordableAction.None )
			{
				Undo.RecordObject( m_target, UndoStr );
				if ( m_actionType == ReordableAction.Add )
				{
					AddFieldAt( definition, m_actionIndex + 1, m_actionInherit );
				}
				else
				{
					RemoveFieldAt( definition, m_actionIndex );
				}
				m_actionType = ReordableAction.None;
				m_actionIndex = -1;
				EditorUtility.SetDirty( m_target );
			}

			EditorGUILayout.Space();
			EditorGUILayout.HelpBox( HelpStr, MessageType.Info );

			EditorGUILayout.Space();
			m_dependencies.Draw( m_target, "struct" );
		}

		private void DrawFieldsHeader( StructDefinitionData definition )
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField( FieldsStr );
			GUILayout.FlexibleSpace();

			if ( GUILayout.Button( string.Empty, PlusStyle, GUILayout.Width( Constants.PlusMinusButtonLayoutWidth ) ) )
			{
				m_actionType = ReordableAction.Add;
				m_actionIndex = definition.Fields.Count - 1;
				m_actionInherit = false;
				EditorGUI.FocusTextInControl( null );
			}

			if ( GUILayout.Button( string.Empty, MinusStyle, GUILayout.Width( Constants.PlusMinusButtonLayoutWidth ) ) )
			{
				m_actionType = ReordableAction.Remove;
				m_actionIndex = definition.Fields.Count - 1;
				EditorGUI.FocusTextInControl( null );
			}
			EditorGUILayout.EndHorizontal();
		}

		private void DrawFieldsList( StructDefinitionData definition )
		{
			// @diogo: the list binds to one List instance, so it is rebuilt whenever that instance moves
			if ( m_fieldsSource != definition.Fields )
			{
				m_fieldsSource = definition.Fields;
				m_fieldsList = null;
			}

			if ( m_fieldsList == null )
			{
				m_fieldsList = new ReorderableList( m_fieldsSource, typeof( StructFieldData ), true, false, false, false )
				{
					headerHeight = 0,
					footerHeight = 0,
					showDefaultBackground = false,
					elementHeight = EditorGUIUtility.singleLineHeight + 2,

					onReorderCallbackWithDetails = ( ReorderableList list, int oldIndex, int newIndex ) =>
					{
						// @diogo: the list has already moved the field; move it back so the undo record holds the pre-drag order
						definition.MoveField( newIndex, oldIndex );
						Undo.RecordObject( m_target, UndoStr );
						definition.MoveField( oldIndex, newIndex );
						EditorUtility.SetDirty( m_target );
					},

					drawElementCallback = ( Rect rect, int index, bool isActive, bool isFocused ) =>
					{
						DrawField( definition, rect, index );
					}
				};
			}

			if ( m_fieldsSource.Count == 0 )
			{
				EditorGUILayout.HelpBox( NoFieldsStr, MessageType.Info );
			}
			else
			{
				m_fieldsList.DoLayoutList();
			}
		}

		private void DrawField( StructDefinitionData definition, Rect rect, int index )
		{
			if ( index >= m_fieldsSource.Count )
			{
				return;
			}

			StructFieldData field = m_fieldsSource[ index ];
			float typeWidth = 70;
			float buttonWidth = Constants.PlusMinusButtonLayoutWidth;
			float spacing = 2;
			float nameWidth = rect.width - typeWidth - 2 * buttonWidth - 4 * spacing;

			Rect nameRect = new Rect( rect.x, rect.y, nameWidth, EditorGUIUtility.singleLineHeight );
			Rect typeRect = new Rect( nameRect.xMax + spacing, rect.y, typeWidth, EditorGUIUtility.singleLineHeight );
			Rect addRect = new Rect( typeRect.xMax + spacing, rect.y, buttonWidth, EditorGUIUtility.singleLineHeight );
			Rect removeRect = new Rect( addRect.xMax + spacing, rect.y, buttonWidth, EditorGUIUtility.singleLineHeight );

			EditorGUI.BeginChangeCheck();
			string fieldName = EditorGUI.TextField( nameRect, field.Name );
			if ( EditorGUI.EndChangeCheck() )
			{
				Undo.RecordObject( m_target, UndoStr );
				field.Name = UniqueFieldName( definition, fieldName, index );
				EditorUtility.SetDirty( m_target );
			}

			EditorGUI.BeginChangeCheck();
			int typeIdx = Mathf.Max( 0, Array.IndexOf( StructDefinitionData.AvailableFieldTypes, field.DataType ) );
			typeIdx = EditorGUI.Popup( typeRect, typeIdx, StructDefinitionData.FieldTypeLabels );
			if ( EditorGUI.EndChangeCheck() )
			{
				Undo.RecordObject( m_target, UndoStr );
				field.DataType = StructDefinitionData.AvailableFieldTypes[ typeIdx ];
				EditorUtility.SetDirty( m_target );
			}

			if ( GUI.Button( addRect, AddTooltip, PlusStyle ) )
			{
				m_actionType = ReordableAction.Add;
				m_actionIndex = index;
				m_actionInherit = true;
			}

			if ( GUI.Button( removeRect, RemoveTooltip, MinusStyle ) )
			{
				m_actionType = ReordableAction.Remove;
				m_actionIndex = index;
			}

			// @diogo: drawn last so the tooltip wins the rect - none of the Rect overloads above carry a
			// GUIContent of their own, and an empty label paints nothing
			GUI.Label( nameRect, NameTooltip );
			GUI.Label( typeRect, TypeTooltip );
		}

		private void AddFieldAt( StructDefinitionData definition, int index, bool inheritSettings = false )
		{
			int at = Mathf.Clamp( index, 0, definition.Fields.Count );

			// @diogo: a field added from a row duplicates it - same type, and the name it was cloned from
			// carried on to the next free index
			StructFieldData source = ( inheritSettings && at > 0 ) ? definition.Fields[ at - 1 ] : null;

			StructFieldData field = new StructFieldData();
			field.Id = definition.NewFieldId();
			if ( source != null )
			{
				field.DataType = source.DataType;
				field.TypeName = source.TypeName;
			}

			definition.Fields.Insert( at, field );
			field.Name = UniqueFieldName( definition, ( source != null ) ? StructDefinitionData.FieldNameBase( source.Name ) : DefaultFieldName, at );
		}

		private void RemoveFieldAt( StructDefinitionData definition, int index )
		{
			if ( index >= 0 && index < definition.Fields.Count )
			{
				definition.Fields.RemoveAt( index );
			}
		}

		// Two fields sharing a name would declare the same member twice, so a clash takes a suffix.
		private string UniqueFieldName( StructDefinitionData definition, string name, int ownIndex )
		{
			string candidate = UIUtils.RemoveInvalidCharacters( name );
			if ( string.IsNullOrEmpty( candidate ) )
			{
				candidate = DefaultFieldName;
			}

			string unique = candidate;
			int suffix = 0;
			bool taken = true;
			while ( taken )
			{
				taken = false;
				for ( int i = 0; i < definition.Fields.Count; i++ )
				{
					if ( i != ownIndex && definition.Fields[ i ].Name.Equals( unique ) )
					{
						taken = true;
						suffix++;
						unique = candidate + suffix;
					}
				}
			}

			return unique;
		}
	}
}

// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AmplifyShaderEditor
{
	[CustomEditor( typeof( AmplifyShaderBatchSet ) )]
	[CanEditMultipleObjects]
	public class AmplifyShaderBatchSetEditor : Editor
	{
		private const string DialogTitle = "Shader Batch Set";
		private const int OpenConfirmThreshold = 10;

		private static readonly GUIContent ActiveContent = new GUIContent( "Active", "When checked, this set is processed by \"Resave Active Sets\" ( AmplifyShaderBatchAPI.ResaveActiveBatchSets, scriptable / command-line )." );
		private static readonly GUIContent ResaveSetContent = new GUIContent( "Resave", "Regenerates every enabled shader in this set from its ASE graph and writes it back to disk." );
		private static readonly GUIContent OpenSetContent = new GUIContent( "Open", "Opens every enabled shader in this set in the shader editor, each in its own tab." );
		private static readonly GUIContent ResaveSelectedContent = new GUIContent( "Resave", "Regenerates every enabled shader across all selected sets." );
		private static readonly GUIContent OpenSelectedContent = new GUIContent( "Open", "Opens every enabled shader across all selected sets, each in its own tab." );
		private static readonly GUIContent AddFolderContent = new GUIContent( "Add Folder...", "Adds every ASE shader found in a folder." );
		private static readonly GUIContent AddSelectedContent = new GUIContent( "Add Selected", "Adds the ASE shaders currently selected in the Project window." );
		private static readonly GUIContent AddAllContent = new GUIContent( "Add All", "Adds every ASE shader in the project." );
		private static readonly GUIContent ClearContent = new GUIContent( "Clear", "Removes all shaders from this set." );

		private SerializedProperty m_entriesProp;
		private SerializedProperty m_activeProp;
		private ReorderableList m_list;
		private GUIStyle m_dropAreaStyle = null;

		private void OnEnable()
		{
			m_entriesProp = serializedObject.FindProperty( "m_entries" );
			m_activeProp = serializedObject.FindProperty( "m_active" );

			m_list = new ReorderableList( serializedObject, m_entriesProp, true, true, true, true );
			m_list.elementHeight = 20;

			m_list.drawHeaderCallback = ( Rect rect ) =>
			{
				EditorGUI.LabelField( rect, "Shaders" );
			};

			m_list.drawElementCallback = ( Rect rect, int index, bool isActive, bool isFocused ) =>
			{
				SerializedProperty element = m_entriesProp.GetArrayElementAtIndex( index );
				SerializedProperty shaderProp = element.FindPropertyRelative( "Shader" );
				SerializedProperty enabledProp = element.FindPropertyRelative( "Enabled" );

				rect.y += 2;
				rect.height = EditorGUIUtility.singleLineHeight;

				Rect toggleRect = new Rect( rect.x, rect.y, 16, rect.height );
				enabledProp.boolValue = EditorGUI.Toggle( toggleRect, enabledProp.boolValue );

				Rect fieldRect = new Rect( rect.x + 20, rect.y, rect.width - 20, rect.height );
				EditorGUI.PropertyField( fieldRect, shaderProp, GUIContent.none );
			};

			m_list.onAddCallback = ( ReorderableList list ) =>
			{
				int i = m_entriesProp.arraySize;
				m_entriesProp.InsertArrayElementAtIndex( i );
				SerializedProperty element = m_entriesProp.GetArrayElementAtIndex( i );
				element.FindPropertyRelative( "Shader" ).objectReferenceValue = null;
				element.FindPropertyRelative( "Enabled" ).boolValue = true;
			};
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			if ( targets.Length > 1 )
			{
				serializedObject.ApplyModifiedProperties();
				DrawMultiSelection();
				return;
			}

			m_activeProp.boolValue = EditorGUILayout.ToggleLeft( ActiveContent, m_activeProp.boolValue );

			EditorGUILayout.Space();
			DrawToolbar();
			DrawDropArea();
			m_list.DoLayoutList();

			serializedObject.ApplyModifiedProperties();

			DrawFooter();
		}

		private void DrawToolbar()
		{
			EditorGUILayout.BeginHorizontal( EditorStyles.toolbar );
			{
				if ( GUILayout.Button( AddFolderContent, EditorStyles.toolbarButton ) )
				{
					AddFromFolderPanel();
				}
				if ( GUILayout.Button( AddSelectedContent, EditorStyles.toolbarButton ) )
				{
					AddSelected();
				}
				if ( GUILayout.Button( AddAllContent, EditorStyles.toolbarButton ) )
				{
					AddFromFolder( "Assets" );
				}

				GUILayout.FlexibleSpace();

				using ( new EditorGUI.DisabledScope( m_entriesProp.arraySize == 0 ) )
				{
					if ( GUILayout.Button( ClearContent, EditorStyles.toolbarButton ) )
					{
						m_entriesProp.ClearArray();
						CommitStructuralChange();
					}
				}
			}
			EditorGUILayout.EndHorizontal();
		}

		private void DrawDropArea()
		{
			EnsureStyles();

			Rect dropArea = GUILayoutUtility.GetRect( 0, 34, GUILayout.ExpandWidth( true ) );
			dropArea.xMin += 4;
			dropArea.xMax -= 4;
			dropArea.yMin += 2;
			GUI.Box( dropArea, "Drag ASE shaders or folders here", m_dropAreaStyle );

			Event evt = Event.current;
			if ( ( evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform ) && dropArea.Contains( evt.mousePosition ) )
			{
				DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

				if ( evt.type == EventType.DragPerform )
				{
					DragAndDrop.AcceptDrag();
					bool added = false;
					foreach ( Object dragged in DragAndDrop.objectReferences )
					{
						string path = AssetDatabase.GetAssetPath( dragged );
						if ( !string.IsNullOrEmpty( path ) && AssetDatabase.IsValidFolder( path ) )
						{
							added |= AddFromFolderInternal( path );
						}
						else if ( dragged is Shader shader )
						{
							added |= AddShader( shader );
						}
					}
					if ( added )
					{
						CommitStructuralChange();
					}
				}
				evt.Use();
			}
		}

		private void DrawFooter()
		{
			if ( AmplifyShaderEditorWindow.IsBatchProcessing )
			{
				DrawBatchProgress();
				return;
			}

			AmplifyShaderBatchSet set = ( AmplifyShaderBatchSet )target;
			int enabled = CountEnabled( set );

			EditorGUILayout.Space();
			EditorGUILayout.BeginHorizontal();
			{
				GUILayout.Label( set.Entries.Count + " shaders  /  " + enabled + " enabled", EditorStyles.miniLabel );
				GUILayout.FlexibleSpace();

				using ( new EditorGUI.DisabledScope( enabled == 0 ) )
				{
					if ( GUILayout.Button( OpenSetContent, GUILayout.Height( 24 ), GUILayout.Width( 100 ) ) )
					{
						EditorApplication.delayCall += () =>
						{
							if ( ConfirmOpen( enabled ) )
							{
								AmplifyShaderBatchAPI.OpenBatchSet( set );
							}
						};
					}
					if ( GUILayout.Button( ResaveSetContent, GUILayout.Height( 24 ), GUILayout.Width( 100 ) ) )
					{
						// @diogo: defer so the batch starts outside OnInspectorGUI ( opening the ASE window mid-IMGUI is unsafe ).
						EditorApplication.delayCall += () => AmplifyShaderBatchAPI.ResaveBatchSet( set );
					}
				}
			}
			EditorGUILayout.EndHorizontal();
		}

		private void DrawMultiSelection()
		{
			if ( AmplifyShaderEditorWindow.IsBatchProcessing )
			{
				DrawBatchProgress();
				return;
			}

			List<AmplifyShaderBatchSet> sets = new List<AmplifyShaderBatchSet>();
			int enabled = 0;
			for ( int i = 0; i < targets.Length; i++ )
			{
				if ( targets[ i ] is AmplifyShaderBatchSet set )
				{
					sets.Add( set );
					enabled += CountEnabled( set );
				}
			}

			EditorGUILayout.LabelField( sets.Count + " Batch Sets selected", EditorStyles.boldLabel );
			EditorGUILayout.LabelField( enabled + " enabled shader(s) across them.", EditorStyles.miniLabel );
			EditorGUILayout.Space();

			using ( new EditorGUI.DisabledScope( enabled == 0 ) )
			{
				EditorGUILayout.BeginHorizontal();
				if ( GUILayout.Button( OpenSelectedContent, GUILayout.Height( 26 ) ) )
				{
					EditorApplication.delayCall += () =>
					{
						if ( ConfirmOpen( enabled ) )
						{
							AmplifyShaderBatchAPI.OpenBatchSets( sets );
						}
					};
				}
				if ( GUILayout.Button( ResaveSelectedContent, GUILayout.Height( 26 ) ) )
				{
					EditorApplication.delayCall += () => AmplifyShaderBatchAPI.ResaveBatchSets( sets );
				}
				EditorGUILayout.EndHorizontal();
			}
		}

		private void DrawBatchProgress()
		{
			EditorGUILayout.Space();
			int total = AmplifyShaderEditorWindow.BatchTotal;
			int done = AmplifyShaderEditorWindow.BatchDone;
			if ( total > 0 )
			{
				Rect r = EditorGUILayout.GetControlRect( false, 20f );
				EditorGUI.ProgressBar( r, ( float )done / total, done + " / " + total );
				if ( GUILayout.Button( "Cancel" ) )
				{
					AmplifyShaderEditorWindow.CancelBatch();
				}
			}
			else
			{
				EditorGUILayout.HelpBox( "Batch resave in progress...", MessageType.Info );
			}
			Repaint();
		}

		private void EnsureStyles()
		{
			if ( m_dropAreaStyle == null )
			{
				m_dropAreaStyle = new GUIStyle( "box" );
				m_dropAreaStyle.alignment = TextAnchor.MiddleCenter;
				m_dropAreaStyle.normal.textColor = EditorStyles.label.normal.textColor;
			}
		}

		// applies the pending array edit and bails the current IMGUI pass so the next one lays out with the
		// new element count ( avoids a one-frame "Mismatched LayoutGroup" from a structural change mid-draw )
		private void CommitStructuralChange()
		{
			serializedObject.ApplyModifiedProperties();
			GUIUtility.ExitGUI();
		}

		private bool SetContains( Shader shader )
		{
			for ( int i = 0; i < m_entriesProp.arraySize; i++ )
			{
				if ( m_entriesProp.GetArrayElementAtIndex( i ).FindPropertyRelative( "Shader" ).objectReferenceValue == shader )
				{
					return true;
				}
			}
			return false;
		}

		private bool AddShader( Shader shader )
		{
			if ( shader == null || SetContains( shader ) || !IOUtils.IsASEShader( shader ) )
			{
				return false;
			}
			int i = m_entriesProp.arraySize;
			m_entriesProp.InsertArrayElementAtIndex( i );
			SerializedProperty element = m_entriesProp.GetArrayElementAtIndex( i );
			element.FindPropertyRelative( "Shader" ).objectReferenceValue = shader;
			element.FindPropertyRelative( "Enabled" ).boolValue = true;
			return true;
		}

		private void AddSelected()
		{
			Object[] selection = Selection.objects;
			bool added = false;
			for ( int i = 0; i < selection.Length; i++ )
			{
				if ( selection[ i ] is Shader shader )
				{
					added |= AddShader( shader );
				}
			}
			if ( added )
			{
				CommitStructuralChange();
			}
		}

		private void AddFromFolderPanel()
		{
			string absolute = EditorUtility.OpenFolderPanel( "Select Folder to Search", Application.dataPath, "" );
			if ( string.IsNullOrEmpty( absolute ) )
			{
				return;
			}

			string relative = FileUtil.GetProjectRelativePath( absolute.Replace( "\\", "/" ) );
			if ( string.IsNullOrEmpty( relative ) || !AssetDatabase.IsValidFolder( relative ) )
			{
				EditorUtility.DisplayDialog( DialogTitle, "The selected folder must be inside the project's Assets folder.", "OK" );
				return;
			}

			AddFromFolder( relative );
		}

		private void AddFromFolder( string projectRelativeFolder )
		{
			if ( AddFromFolderInternal( projectRelativeFolder ) )
			{
				CommitStructuralChange();
			}
		}

		// scans a folder for ASE shaders and appends the new ones; returns true if anything was added.
		// does NOT commit ( callers batch a single CommitStructuralChange after possibly several folders ).
		private bool AddFromFolderInternal( string projectRelativeFolder )
		{
			string[] guids = AssetDatabase.FindAssets( "t:Shader", new string[] { projectRelativeFolder } );
			int added = 0;
			for ( int i = 0; i < guids.Length; i++ )
			{
				string path = AssetDatabase.GUIDToAssetPath( guids[ i ] );
				Shader shader = AssetDatabase.LoadAssetAtPath<Shader>( path );
				if ( shader != null && AddShader( shader ) )
				{
					added++;
				}
			}
			if ( added == 0 )
			{
				Debug.Log( "[AmplifyShaderEditor] No new ASE shaders found in \"" + projectRelativeFolder + "\"." );
			}
			return added > 0;
		}

		// @diogo: opening keeps every graph live in its own tab, so past a handful ask first, matching the
		// "Open in Editor" context menu. Counts enabled entries: a couple may still be dropped later as
		// duplicates or non-ASE shaders.
		private static bool ConfirmOpen( int count )
		{
			if ( count <= OpenConfirmThreshold )
			{
				return true;
			}
			return EditorUtility.DisplayDialog( DialogTitle,
				"This will open " + count + " shaders in the editor, each in its own tab. Do you wish to proceed?",
				"Open " + count,
				"Cancel" );
		}

		private static int CountEnabled( AmplifyShaderBatchSet set )
		{
			int count = 0;
			for ( int i = 0; i < set.Entries.Count; i++ )
			{
				AmplifyShaderBatchSet.Entry entry = set.Entries[ i ];
				if ( entry != null && entry.Enabled && entry.Shader != null )
				{
					count++;
				}
			}
			return count;
		}
	}
}

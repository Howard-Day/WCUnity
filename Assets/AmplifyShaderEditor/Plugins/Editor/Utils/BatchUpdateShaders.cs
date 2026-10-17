using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AmplifyShaderEditor
{
	public class BatchUpdateShaders : EditorWindow
	{
		private const string UpdateAllStr = "Update All";
		private const string UpdateAllStyle = "prebutton";


		[SerializeField]
		private ASESaveBundleAsset m_asset;

		[SerializeField]
		private ASESaveBundleAsset m_dummyAsset;

		private GUIStyle m_contentStyle = null;

		private Vector2 m_scrollPos;
		private GUIContent m_ViewToolIcon;

		ASESaveBundleAssetEditor m_editor;

		private const string Title = "Batch Update Shaders";

		[NonSerialized]
		private GUIStyle m_titleStyle;

		[MenuItem( "Window/Amplify Shader Editor/" + Title, false, priority: 1102 )]
		static void ShowWindow()
		{
			var window = EditorWindow.GetWindow<BatchUpdateShaders>();
			window.titleContent.text = "Batch Update...";
			window.titleContent.tooltip = Title;
			window.minSize = new Vector2( 302 , 350 );
			window.Show();
		}

		private void OnEnable()
		{
			if( m_contentStyle == null )
			{
				m_contentStyle = new GUIStyle( GUIStyle.none );
				m_contentStyle.margin = new RectOffset( 6 , 4 , 5 , 5 );
			}

			if( m_ViewToolIcon == null )
			{
				m_ViewToolIcon = EditorGUIUtility.IconContent( "icons/d_ViewToolZoom.png" );
			}
		}

		private void OnDestroy()
		{
			DestroyImmediate( m_editor );
			if( m_dummyAsset != null && m_dummyAsset != m_asset )
				DestroyImmediate( m_dummyAsset );
		}


		public static string FetchPath( string title, string folderpath )
		{
			folderpath = EditorUtility.OpenFolderPanel( title , folderpath , "" );
			folderpath = FileUtil.GetProjectRelativePath( folderpath );
			if( string.IsNullOrEmpty( folderpath ) )
				folderpath = "Assets";

			return folderpath;
		}

		private bool m_updatingShaders = false;

		private void OnGUI()
		{
			if( m_updatingShaders )
			{
				m_updatingShaders = AmplifyShaderEditorWindow.IsBatchProcessing;
			}


			if( m_titleStyle == null )
			{
				m_titleStyle = new GUIStyle( "BoldLabel" );
				m_titleStyle.fontSize = 13;
				m_titleStyle.alignment = TextAnchor.MiddleCenter;
			}


			EditorGUILayout.LabelField( Title , m_titleStyle );
			EditorGUI.BeginDisabledGroup( m_updatingShaders );
			{
				ASESaveBundleAsset currentAsset = null;
				if( m_dummyAsset == null )
				{
					m_dummyAsset = ScriptableObject.CreateInstance<ASESaveBundleAsset>();
					m_dummyAsset.name = "Dummy";
				}
				currentAsset = m_dummyAsset;

				m_scrollPos = EditorGUILayout.BeginScrollView( m_scrollPos , GUILayout.Height( position.height ) );
				{
					float cachedWidth = EditorGUIUtility.labelWidth;
					EditorGUIUtility.labelWidth = 100;
					EditorGUILayout.BeginVertical( m_contentStyle );
					{
						EditorGUI.BeginDisabledGroup( currentAsset.AllShaders.Count <= 0 );
						{
							// Update all shaders
							if( GUILayout.Button( UpdateAllStr ) )
							{
								m_updatingShaders = true;
								string[] assetPaths = new string[ currentAsset.AllShaders.Count ];
								for( int i = 0 ; i < assetPaths.Length ; i++ )
								{
									assetPaths[ i ] = AssetDatabase.GetAssetPath( currentAsset.AllShaders[ i ] );
								}
								AmplifyShaderEditorWindow.LoadAndSaveList( assetPaths );
							}
						}
						EditorGUI.EndDisabledGroup();

						EditorGUILayout.Separator();

						if( Event.current.type == EventType.Layout )
						{
							if( m_editor == null )
							{
								m_editor = Editor.CreateEditor( currentAsset , typeof( ASESaveBundleAssetEditor ) ) as ASESaveBundleAssetEditor;
							}
							else
							{
								if( m_editor.Instance != currentAsset )
								{
									DestroyImmediate( m_editor );
									m_editor = Editor.CreateEditor( currentAsset , typeof( ASESaveBundleAssetEditor ) ) as ASESaveBundleAssetEditor;
								}
							}
						}
						if( m_editor != null )
							m_editor.PackageFreeGUI();

					}
					EditorGUILayout.EndVertical();
				}
				EditorGUILayout.EndScrollView();
			}
			EditorGUI.EndDisabledGroup();
		}
	}
}

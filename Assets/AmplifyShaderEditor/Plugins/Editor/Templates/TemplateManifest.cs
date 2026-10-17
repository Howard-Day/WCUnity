// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEditorInternal;
using UnityEngine;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace AmplifyShaderEditor
{
	[Serializable]
	public class TemplateManifest : ScriptableObject
	{
		[Serializable]
		public struct TemplateVersionRange
		{
			public string Min;
			public string Max;
		}

		[Serializable]
		public struct TemplateVariant
		{
			public TemplateVersionRange VersionRange;
			public UnityEngine.Object Override;
		}

		[Serializable]
		public struct TemplateLink
		{
			public UnityEngine.Object Template;
			public TemplateVariant[] Versions;

			[SerializeField] private string TemplateName;
			[SerializeField] private long TemplateTimestamp;

			static readonly Regex ShaderNameRegex = new Regex( @"Shader(?:\s|/\*.*?\*/)+""([^""]+)""", RegexOptions.Compiled | RegexOptions.Multiline );
			static readonly string DefaultShaderBodyFormat =
				"Shader \"{0}\"\n" +
				"{{\n" +
				"\tSubShader {{ Pass {{ }} }}\n" +
				"}}";

			public bool HasTemplateChanged()
			{
				long fileTimestamp = 0;
				if ( Template != null )
				{
					fileTimestamp = File.GetLastWriteTime( AssetDatabase.GetAssetPath( Template ) ).Ticks;
				}
				return TemplateTimestamp != fileTimestamp;
			}

			public void ParseTemplateName()
			{
				Debug.Assert( Template != null );

				var path = AssetDatabase.GetAssetPath( Template );

				Debug.Assert( path != null );

				var text = File.ReadAllText( path );

				var match = ShaderNameRegex.Match( text );
				if ( match.Success )
				{
					TemplateName = match.Groups[ 1 ].Value;
					TemplateTimestamp = File.GetLastWriteTime( path ).Ticks;
				}
				else
				{
					TemplateName = string.Empty;
					TemplateTimestamp = 0;
				}
			}

			public string DefaultShaderBody()
			{
				return string.Format( DefaultShaderBodyFormat, TemplateName );
			}
		}

		public string PackageName;
		public TemplateLink[] Templates;

		public class DoCreateManifest : AssetUtils.EndAction
		{
			public override void Action( AssetUtils.EntityId entityId, string pathName, string resourceFile )
			{
				UnityEngine.Object obj = AssetUtils.EntityIdToObject( entityId );
				AssetDatabase.CreateAsset( obj, AssetDatabase.GenerateUniqueAssetPath( pathName ) );
				AssetDatabase.SaveAssets();
			}
		}

		[MenuItem( "Assets/Create/Amplify Shader Tools/Template Manifest", false, 87 )]
		static void CreateNewTemplateManifest()
		{
			TemplateManifest asset = ScriptableObject.CreateInstance<TemplateManifest>();
			var endNameEditAction = ScriptableObject.CreateInstance<DoCreateManifest>();
			ProjectWindowUtil.StartNameEditingIfProjectWindowExists( AssetUtils.GetEntityId( asset ), endNameEditAction, "New Template Manifest.asset", AssetPreview.GetMiniThumbnail( asset ), null );
		}
	}

	[CustomPropertyDrawer( typeof( TemplateManifest.TemplateVariant ) )]
	public class TemplateVariantDrawer : PropertyDrawer
	{
		const float Pad = 4;

		public override float GetPropertyHeight( SerializedProperty property, GUIContent label ) => EditorGUIUtility.singleLineHeight;

		static readonly Regex VersionRegex = new Regex( @"^\d+(\.\d+){0,2}$", RegexOptions.Compiled );

		static bool IsValidVersion( string v )
		{
			if ( string.IsNullOrEmpty( v ) || !VersionRegex.IsMatch( v ) )
			{
				return false;
			}
			var parts = v.Split( '.' );
			foreach ( var p in parts )
			{
				if ( !uint.TryParse( p, out _ ) )
				{
					return false;
				}
			}
			return true;
		}

		public override void OnGUI( Rect rect, SerializedProperty property, GUIContent label )
		{
			var versionRange = property.FindPropertyRelative( "VersionRange" );
			var minProp = versionRange.FindPropertyRelative( "Min" );
			var maxProp = versionRange.FindPropertyRelative( "Max" );
			var overrideProp = property.FindPropertyRelative( "Override" );

			EditorGUI.BeginProperty( rect, label, property );

			int oldIndent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;

			rect.height = EditorGUIUtility.singleLineHeight;

			float dashWidth = 10;
			float availWidth = rect.width;
			float flexibleWidth = availWidth - ( dashWidth + Pad * 4 );
			float minWidth = Mathf.Clamp( 60, 30, flexibleWidth * 0.20f );
			float maxWidth = Mathf.Clamp( 60, 30, flexibleWidth * 0.20f );
			float overrideWidth = Mathf.Max( 0, flexibleWidth - minWidth - maxWidth );
			const float minOverrideWidth = 80;

			if ( overrideWidth < minOverrideWidth )
			{
				float need = minOverrideWidth - overrideWidth;
				float takeEach = need * 0.5f;

				minWidth = Mathf.Max( 30, minWidth - takeEach );
				maxWidth = Mathf.Max( 30, maxWidth - takeEach );
				overrideWidth = Mathf.Max( 0, flexibleWidth - minWidth - maxWidth );
			}

			var overrideRect = new Rect( rect.x, rect.y, overrideWidth, rect.height );
			var minRect = new Rect( overrideRect.xMax + Pad + Pad, rect.y, minWidth, rect.height );
			var dashRect = new Rect( minRect.xMax + Pad, rect.y, dashWidth, rect.height );
			var maxRect = new Rect( dashRect.xMax + Pad, rect.y, maxWidth, rect.height );

			EditorGUI.PropertyField( overrideRect, overrideProp, GUIContent.none );

			// MIN
			string minValue = minProp.stringValue;
			bool minValid = IsValidVersion( minValue );
			Color oldColor = GUI.backgroundColor;
			GUI.backgroundColor = minValid ? GUI.backgroundColor : new Color( 1, 0.6f, 0.6f );
			EditorGUI.BeginChangeCheck();
			string newMin = EditorGUI.TextField( minRect, minValue );
			minProp.stringValue = ( EditorGUI.EndChangeCheck() && IsValidVersion( newMin ) ) ? newMin : minProp.stringValue;
			GUI.backgroundColor = oldColor;

			// DASH
			EditorGUI.LabelField( dashRect, "�" );

			// MAX
			string maxValue = maxProp.stringValue;
			bool maxValid = IsValidVersion( maxValue );
			GUI.backgroundColor = maxValid ? GUI.backgroundColor : new Color( 1, 0.6f, 0.6f );
			EditorGUI.BeginChangeCheck();
			string newMax = EditorGUI.TextField( maxRect, maxValue );
			maxProp.stringValue = ( EditorGUI.EndChangeCheck() && IsValidVersion( newMax ) ) ? newMax : maxProp.stringValue;
			GUI.backgroundColor = oldColor;
		}
	}

	[CustomEditor( typeof( TemplateManifest ) )]
	public class TemplateManifestEditor : Editor
	{
		SerializedProperty m_package;
		SerializedProperty m_templates;

		ReorderableList m_templatesList;

		void OnEnable()
		{
			m_package = serializedObject.FindProperty( "PackageName" );
			m_templates = serializedObject.FindProperty( "Templates" );
			m_templatesList = new ReorderableList( serializedObject, m_templates, true, true, true, true );

			m_templatesList.drawHeaderCallback = rect =>
			{
				EditorGUI.LabelField( rect, "Templates" );
			};

			m_templatesList.elementHeightCallback = index =>
			{
				var element = m_templates.GetArrayElementAtIndex( index );
				var variantsProp = element.FindPropertyRelative( "Versions" );
				return EditorGUIUtility.singleLineHeight + EditorGUI.GetPropertyHeight( variantsProp, true ) + 10;
			};

			m_templatesList.drawElementCallback = ( rect, index, isActive, isFocused ) =>
			{
				var element = m_templates.GetArrayElementAtIndex( index );
				var templateProp = element.FindPropertyRelative( "Template" );
				var variantsProp = element.FindPropertyRelative( "Versions" );
				var templateLink = ( TemplateManifest.TemplateLink )element.boxedValue;

				rect.y += 3;

				var templateRect = new Rect( rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight );

				EditorGUI.BeginChangeCheck();
				EditorGUI.PropertyField( templateRect, templateProp, GUIContent.none );
				if ( EditorGUI.EndChangeCheck() || templateLink.HasTemplateChanged() )
				{
					templateLink.ParseTemplateName();
					element.boxedValue = templateLink;
				}

				rect.y += EditorGUIUtility.singleLineHeight + 4;

				var variantsHeight = EditorGUI.GetPropertyHeight( variantsProp, true );
				var variantsRect = new Rect( rect.x + 18, rect.y, rect.width - 18, variantsHeight );

				EditorGUI.PropertyField( variantsRect, variantsProp, new GUIContent( "Versions" ), true );


			};
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label( "Package Name", GUILayout.Width( 95 ) );
			EditorGUILayout.PropertyField( m_package, GUIContent.none );
			m_package.stringValue = m_package.stringValue.ToLowerInvariant();
			EditorGUILayout.EndHorizontal();

			GUILayout.Space( 6 );

			m_templatesList.DoLayoutList();

			serializedObject.ApplyModifiedProperties();
		}
	}

}

// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;

namespace AmplifyShaderEditor
{
	public class DoCreateFunction : AssetUtils.EndAction
	{
		public override void Action( AssetUtils.EntityId entityId, string pathName, string resourceFile )
		{
			base.Action( entityId, pathName, resourceFile );

			UnityEngine.Object obj = AssetUtils.EntityIdToObject( entityId );

			AssetDatabase.CreateAsset( obj, AssetDatabase.GenerateUniqueAssetPath( pathName ) );
			AmplifyShaderEditorWindow.LoadShaderFunctionToASE( (AmplifyShaderFunction)obj, false );
		}
	}
}

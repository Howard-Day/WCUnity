// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using System.IO;

namespace AmplifyShaderEditor
{
	public class DoCreateStandardShader : AssetUtils.EndAction
	{
		public override void Action( AssetUtils.EntityId entityId, string pathName, string resourceFile )
		{
			base.Action( entityId, pathName, resourceFile );

			string uniquePath = AssetDatabase.GenerateUniqueAssetPath( pathName );
			string shaderName = Path.GetFileName( uniquePath );

			if( IOUtils.AllOpenedWindows.Count > 0 )
			{
				EditorWindow openedWindow = AmplifyShaderEditorWindow.GetWindow<AmplifyShaderEditorWindow>();
				UIUtils.CurrentWindow = AmplifyShaderEditorWindow.CreateTab();
				WindowHelper.AddTab( openedWindow, UIUtils.CurrentWindow );
			}
			else
			{
				UIUtils.CurrentWindow = AmplifyShaderEditorWindow.OpenWindow( shaderName, UIUtils.ShaderIcon );
			}

			Shader shader = UIUtils.CreateNewEmpty( uniquePath, shaderName );
			ProjectWindowUtil.ShowCreatedAsset( shader );

			UIUtils.CurrentWindow.RequestSave();
		}
	}

	public class DoCreateTemplateShader : AssetUtils.EndAction
	{
		public override void Action( AssetUtils.EntityId entityId, string pathName, string resourceFile )
		{
			base.Action( entityId, pathName, resourceFile );

			string uniquePath = AssetDatabase.GenerateUniqueAssetPath( pathName );
			string shaderName = Path.GetFileName( uniquePath );

			if( !string.IsNullOrEmpty( UIUtils.NewTemplateGUID ) )
			{
				Shader shader = AmplifyShaderEditorWindow.CreateNewTemplateShader( UIUtils.NewTemplateGUID, uniquePath, shaderName );
				ProjectWindowUtil.ShowCreatedAsset( shader );

				UIUtils.CurrentWindow.RequestSave();
			}
		}
	}
}

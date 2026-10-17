// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEditor;

namespace AmplifyShaderEditor
{
	// Catch when scene is saved (Ctr+S) and also save ase shader
	public class SceneSaveCallback : UnityEditor.AssetModificationProcessor
	{
		static string[] OnWillSaveAssets( string[] paths )
		{
			if ( Preferences.User.UpdateOnSceneSave && UIUtils.CurrentWindow != null && EditorWindow.focusedWindow == UIUtils.CurrentWindow )
			{
				UIUtils.CurrentWindow.SetCtrlSCallback( false );
			}
			return paths;
		}
	}
}

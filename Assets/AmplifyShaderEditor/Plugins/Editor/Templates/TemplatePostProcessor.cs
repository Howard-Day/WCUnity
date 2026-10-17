// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEditor;

namespace AmplifyShaderEditor
{
	public sealed class TemplatePostProcessor : AssetPostprocessor
	{
		static void OnPostprocessAllAssets( string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths )
		{
			TemplateTracker.QueueUpdate();
		}
	}
}

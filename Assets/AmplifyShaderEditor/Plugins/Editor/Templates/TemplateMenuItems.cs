// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>
using UnityEditor;

namespace AmplifyShaderEditor
{
	public class TemplateMenuItems
	{
		[MenuItem( "Assets/Create/Amplify Shader/Built-In/Default Sprites", false, 85 )]
		public static void ApplyTemplateBuiltInDefaultSprites()
		{
			AmplifyShaderEditorWindow.CreateConfirmationTemplateShader( "0f8ba0101102bb14ebf021ddadce9b49" );
		}
		[MenuItem( "Assets/Create/Amplify Shader/Built-In/Image Effect", false, 85 )]
		public static void ApplyTemplateBuiltInImageEffect()
		{
			AmplifyShaderEditorWindow.CreateConfirmationTemplateShader( "c71b220b631b6344493ea3cf87110c93" );
		}
		[MenuItem( "Assets/Create/Amplify Shader/Built-In/Lit", false, 85 )]
		public static void ApplyTemplateBuiltInLit()
		{
			AmplifyShaderEditorWindow.CreateConfirmationTemplateShader( "ed95fe726fd7b4644bb42f4d1ddd2bcd" );
		}
		[MenuItem( "Assets/Create/Amplify Shader/Built-In/Multi Pass Unlit", false, 85 )]
		public static void ApplyTemplateBuiltInMultiPassUnlit()
		{
			AmplifyShaderEditorWindow.CreateConfirmationTemplateShader( "e1de45c0d41f68c41b2cc20c8b9c05ef" );
		}
		[MenuItem( "Assets/Create/Amplify Shader/Built-In/Particles Alpha Blended", false, 85 )]
		public static void ApplyTemplateBuiltInParticlesAlphaBlended()
		{
			AmplifyShaderEditorWindow.CreateConfirmationTemplateShader( "0b6a9f8b4f707c74ca64c0be8e590de0" );
		}
		[MenuItem( "Assets/Create/Amplify Shader/Built-In/Post-Processing Stack", false, 85 )]
		public static void ApplyTemplateBuiltInPostProcessingStack()
		{
			AmplifyShaderEditorWindow.CreateConfirmationTemplateShader( "32139be9c1eb75640a847f011acf3bcf" );
		}
		[MenuItem( "Assets/Create/Amplify Shader/Built-In/Unlit", false, 85 )]
		public static void ApplyTemplateBuiltInUnlit()
		{
			AmplifyShaderEditorWindow.CreateConfirmationTemplateShader( "0770190933193b94aaa3065e307002fa" );
		}
		[MenuItem( "Assets/Create/Amplify Shader/Built-In/Unlit Lightmap", false, 85 )]
		public static void ApplyTemplateBuiltInUnlitLightmap()
		{
			AmplifyShaderEditorWindow.CreateConfirmationTemplateShader( "899e609c083c74c4ca567477c39edef0" );
		}
		[MenuItem( "Assets/Create/Amplify Shader/Built-In/Wireframe", false, 85 )]
		public static void ApplyTemplateBuiltInWireframe()
		{
			AmplifyShaderEditorWindow.CreateConfirmationTemplateShader( "45bf09b07249a46489d5200e9ba4d9f1" );
		}
		[MenuItem( "Assets/Create/Amplify Shader/Custom Render Texture/Initialize", false, 85 )]
		public static void ApplyTemplateCustomRenderTextureInitialize()
		{
			AmplifyShaderEditorWindow.CreateConfirmationTemplateShader( "6ce779933eb99f049b78d6163735e06f" );
		}
		[MenuItem( "Assets/Create/Amplify Shader/Custom Render Texture/Update", false, 85 )]
		public static void ApplyTemplateCustomRenderTextureUpdate()
		{
			AmplifyShaderEditorWindow.CreateConfirmationTemplateShader( "32120270d1b3a8746af2aca8bc749736" );
		}
		[MenuItem( "Assets/Create/Amplify Shader/UI/Default", false, 85 )]
		public static void ApplyTemplateUIDefault()
		{
			AmplifyShaderEditorWindow.CreateConfirmationTemplateShader( "5056123faa0c79b47ab6ad7e8bf059a4" );
		}
	}
}

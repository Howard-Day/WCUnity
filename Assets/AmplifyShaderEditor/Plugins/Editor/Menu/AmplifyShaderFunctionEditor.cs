using UnityEngine;
using UnityEditor;
using System;
using System.IO;

namespace AmplifyShaderEditor
{
	[CustomEditor( typeof( AmplifyShaderFunction ) )]
	public class AmplifyShaderFunctionEditor : Editor
	{
		AmplifyShaderFunction m_target;
		private AssetDependencyList m_dependencies = new AssetDependencyList();

		void OnEnable()
		{
			m_target = ( target as AmplifyShaderFunction );
		}

		public override void OnInspectorGUI()
		{
			//base.OnInspectorGUI();
			//base.serializedObject.Update();
			if( GUILayout.Button( "Open in Shader Editor" ) )
			{
				ASEPackageManagerHelper.SetupLateShaderFunction( m_target );
			}
			//EditorGUILayout.Separator();
			//m_target.FunctionInfo = EditorGUILayout.TextArea( m_target.FunctionInfo );

			if ( m_target.Description.Length > 0 )
			{
				EditorGUILayout.HelpBox( m_target.Description, MessageType.Info );
			}

			EditorGUILayout.Space();

			m_dependencies.Draw( m_target, "shader function" );
		}

		public static bool SearchForGUID( string guid, string pathName )
		{
			bool result = false;
			int count = 0;
			if( !string.IsNullOrEmpty( pathName ) && File.Exists( pathName ) )
			{
				StreamReader fileReader = null;
				try
				{
					fileReader = new StreamReader( pathName );

					string line;
					int index = -1;
					while( ( line = fileReader.ReadLine() ) != null )
					{
						index = line.IndexOf( guid );
						count++;

						if( index > -1 )
						{
							result = true;
							break;
						}
					}
				}
				catch( Exception e )
				{
					Debug.LogException( e );
				}
				finally
				{
					if( fileReader != null )
						fileReader.Close();
				}
			}
			return result;
		}
	}
}

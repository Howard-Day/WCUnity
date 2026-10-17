using System;
using UnityEngine;
using UnityEditor;

namespace AmplifyShaderEditor
{
	[Serializable]
	public class FallbackPickerHelper : ScriptableObject
	{
		private const string FallbackFormat = "Fallback \"{0}\"";
		private const string FallbackOff = "Fallback Off";
		private const string FallbackShaderStr = "Fallback";
		private const string ShaderPoputContext = "CONTEXT/ShaderPopup";

		[SerializeField]
		private string m_fallbackShader = string.Empty;

		public void Init()
		{
			hideFlags = HideFlags.HideAndDontSave;
		}

		private Rect m_pickerButtonRect;

		public void Draw( ParentNode owner )
		{
			EditorGUILayout.BeginHorizontal();
			m_fallbackShader = owner.EditorGUILayoutTextField( FallbackShaderStr, m_fallbackShader );

			bool clicked = GUILayout.Button( string.Empty, UIUtils.InspectorPopdropdownFallback, GUILayout.Width( 17 ), GUILayout.Height( 19 ) );

			m_pickerButtonRect = ( Event.current.type == EventType.Repaint ) ? GUILayoutUtility.GetLastRect() : m_pickerButtonRect;

			if ( clicked )
			{
				EditorGUI.FocusTextInControl( null );
				GUI.FocusControl( null );

				DisplayShaderContext( owner, m_pickerButtonRect );
			}
			EditorGUILayout.EndHorizontal();
		}

		private void DisplayShaderContext( ParentNode node, Rect position )
		{
			UIUtils.BuildShaderSelectionMenu( OnShaderSelected ).DropDown( position );
		}

		private void OnShaderSelected( object userData )
		{
			string shaderName = userData as string;
			if( !string.IsNullOrEmpty( shaderName ) )
			{
				m_fallbackShader = shaderName;
			}
		}

		public void ReadFromString( ref uint index, ref string[] nodeParams )
		{
			m_fallbackShader = nodeParams[ index++ ];
		}

		public void WriteToString( ref string nodeInfo )
		{
			IOUtils.AddFieldValueToString( ref nodeInfo, m_fallbackShader );
		}

		public void Destroy()
		{
		}

		public readonly string TabbedFallbackShaderOff = "\t" + FallbackOff + "\n";

		public string TabbedFallbackShader
		{
			get
			{
				if ( string.IsNullOrEmpty( m_fallbackShader ) )
					return "\t" + FallbackOff + "\n";

				return "\t" + string.Format( FallbackFormat, m_fallbackShader ) + "\n";
			}
		}

		public string FallbackShader
		{
			get
			{
				if( string.IsNullOrEmpty( m_fallbackShader ) )
					return FallbackOff;

				return string.Format( FallbackFormat, m_fallbackShader );
			}
		}

		public string RawFallbackShader
		{
			get
			{
				return m_fallbackShader;
			}
			set
			{
				m_fallbackShader = value;
			}
		}


		public bool Active { get { return !string.IsNullOrEmpty( m_fallbackShader ); } }

	}
}

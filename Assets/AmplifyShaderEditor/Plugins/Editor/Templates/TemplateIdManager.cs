// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AmplifyShaderEditor
{
	[Serializable]
	public class TemplatePassId
	{
		public string PassId;
		public bool RemoveFromShader;
	}


	[Serializable]
	public class TemplateTag
	{
		public string Tag = string.Empty;
		public string Replacement = string.Empty;
		public string Output = string.Empty;
		public TemplateTag( string tag, string replacement = null )
		{
			Tag = tag;
			if( replacement != null )
			{
				Replacement = replacement;
				Output = replacement;
			}
		}
	}

	[Serializable]
	public class TemplateId
	{
		public int StartIdx = -1;
		public string UniqueID;
		public string Tag;
		public string ReplacementText;
		public bool IsReplaced = false;
		public bool EmptyReplacer = false;
		public TemplateId( int bodyIdx, string uniqueID, string tag, bool emptyReplacer = false )
		{
			StartIdx = bodyIdx;
			UniqueID = uniqueID;
			Tag = tag;
			EmptyReplacer = emptyReplacer;
			ReplacementText = emptyReplacer ? string.Empty : tag;
		}

		public void SetReplacementText( string replacementText )
		{
			ReplacementText = replacementText;
			IsReplaced = true;
		}

		public void Reset()
		{
			ReplacementText = EmptyReplacer?string.Empty:Tag;
			IsReplaced = false;
		}
	}

	[Serializable]
	public class TemplateIdManager
	{
		[SerializeField]
		private bool m_isSorted = false;
		[SerializeField]
		private string m_shaderBody;
		[SerializeField]
		private List<TemplateId> m_registeredIds = new List<TemplateId>();

		[SerializeField]
		private List<TemplateTag> m_registeredTags = new List<TemplateTag>();

		[SerializeField]
		private List<TemplatePassId> m_registeredPassIds = new List<TemplatePassId>();

		private Dictionary<string, TemplateId> m_registeredIdsDict = new Dictionary<string, TemplateId>();

		public TemplateIdManager( string shaderBody )
		{
			m_shaderBody = shaderBody;
		}

		public void Destroy()
		{
			m_registeredPassIds.Clear();
			m_registeredPassIds = null;

			m_registeredTags.Clear();
			m_registeredTags = null;

			m_registeredIds.Clear();
			m_registeredIds = null;
			if( m_registeredIdsDict != null )
			{
				m_registeredIdsDict.Clear();
				m_registeredIdsDict = null;
			}
		}

		void RefreshIds()
		{
			if( m_registeredIdsDict == null )
			{
				m_registeredIdsDict = new Dictionary<string, TemplateId>();
			}

			if( m_registeredIdsDict.Count != m_registeredIds.Count )
			{
				m_registeredIdsDict.Clear();
				int count = m_registeredIds.Count;
				for( int i = 0; i < count; i++ )
				{
					m_registeredIdsDict.Add( m_registeredIds[ i ].UniqueID, m_registeredIds[ i ] );
				}
			}
		}

		public void RegisterId( int bodyIdx, string uniqueID, string tag, bool emptyReplacer = false )
		{
			if( bodyIdx < 0 )
				return;

			RefreshIds();

			TemplateId templateId = new TemplateId( bodyIdx, uniqueID, tag, emptyReplacer );
			m_registeredIds.Add( templateId );
			m_registeredIdsDict.Add( uniqueID, templateId );
		}

		public void RegisterTag( string tag, string replacement = null )
		{
			m_registeredTags.Add( new TemplateTag( tag, replacement ) );
		}

		public void RegisterPassId( string passId )
		{
			m_registeredPassIds.Add( new TemplatePassId() { PassId = passId, RemoveFromShader = false } );
		}

		public void SetPassIdUsage( int idx , bool removeFromShader )
		{
			m_registeredPassIds[ idx ].RemoveFromShader = removeFromShader;
		}

		public void SetReplacementText( string uniqueId, string replacementText )
		{
			RefreshIds();

			if( m_registeredIdsDict.ContainsKey( uniqueId ) && m_registeredIdsDict[ uniqueId ].StartIdx >= 0 )
				m_registeredIdsDict[ uniqueId ].SetReplacementText( replacementText );
		}


		public string BuildShader()
		{
			if( !m_isSorted )
			{
				m_registeredIds.Sort( ( x, y ) => { return x.StartIdx.CompareTo( y.StartIdx ); } );
			}

			int idCount = m_registeredIds.Count;
			// @diogo: one pass over the body; a Remove+Insert per tag rebuilt the whole string every time
			StringBuilder builder = new StringBuilder( m_shaderBody.Length );
			int cursor = 0;
			for( int i = 0; i < idCount; i++ )
			{
				if( m_registeredIds[ i ].StartIdx >= 0 && m_registeredIds[ i ].IsReplaced )
				{
					builder.Append( m_shaderBody, cursor, m_registeredIds[ i ].StartIdx - cursor );
					builder.Append( m_registeredIds[ i ].ReplacementText );
					cursor = m_registeredIds[ i ].StartIdx + m_registeredIds[ i ].Tag.Length;
				}
			}
			builder.Append( m_shaderBody, cursor, m_shaderBody.Length - cursor );
			string finalShaderBody = builder.ToString();

			int count = m_registeredPassIds.Count;
			for( int i = 0; i < count; i++ )
			{
				if( m_registeredPassIds[ i ].RemoveFromShader )
					finalShaderBody = ReplaceOrdinal( finalShaderBody, m_registeredPassIds[ i ].PassId, string.Empty );
			}

			for( int i = 0; i < idCount; i++ )
			{
				if( !m_registeredIds[ i ].IsReplaced && !m_registeredIds[ i ].Tag.Equals( m_registeredIds[ i ].ReplacementText ) )
				{
					finalShaderBody = ReplaceOrdinal( finalShaderBody, m_registeredIds[ i ].Tag, m_registeredIds[ i ].ReplacementText );
				}
			}			

			count = m_registeredTags.Count;
			for( int i = 0; i < count; i++ )
			{
				TemplateTag tag = m_registeredTags[ i ];
				finalShaderBody = ReplaceOrdinal( finalShaderBody, tag.Tag, tag.Replacement );
				tag.Replacement = tag.Output;
			}

			//finalShaderBody = finalShaderBody.Replace( TemplatesManager.TemplateExcludeFromGraphTag, string.Empty );
			//finalShaderBody = finalShaderBody.Replace( TemplatesManager.TemplateMainPassTag, string.Empty );

			return finalShaderBody;
		}

		// @diogo: string.Replace costs ~30 ms per call on a 4 MB body under Mono, hit or miss, and
		// BuildShader issues a hundred of them; an ordinal scan only rebuilds the body on a hit
		private static string ReplaceOrdinal( string body, string oldValue, string newValue )
		{
			string result = body;
			int hit = string.IsNullOrEmpty( oldValue ) ? -1 : body.IndexOf( oldValue, StringComparison.Ordinal );
			if ( hit >= 0 )
			{
				StringBuilder builder = new StringBuilder( body.Length );
				int cursor = 0;
				while ( hit >= 0 )
				{
					builder.Append( body, cursor, hit - cursor );
					builder.Append( newValue );
					cursor = hit + oldValue.Length;
					hit = body.IndexOf( oldValue, cursor, StringComparison.Ordinal );
				}
				builder.Append( body, cursor, body.Length - cursor );
				result = builder.ToString();
			}
			return result;
		}

		public void ResetRegistersState()
		{
			int count = m_registeredIds.Count;
			for( int i = 0; i < count; i++ )
			{
				m_registeredIds[ i ].Reset();
			}
		}

		public void Reset()
		{
			m_registeredIds.Clear();
			if( m_registeredIdsDict == null )
			{
				m_registeredIdsDict = new Dictionary<string, TemplateId>();
			}
			else
			{
				m_registeredIdsDict.Clear();
			}
		}

		public string ShaderBody
		{
			get { return m_shaderBody; }
			set { m_shaderBody = value; }
		}

		public List<TemplateTag> RegisteredTags { get { return m_registeredTags; } set { m_registeredTags = value; } }
	}
}

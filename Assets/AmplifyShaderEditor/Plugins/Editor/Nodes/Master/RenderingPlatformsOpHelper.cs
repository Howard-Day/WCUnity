// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;

namespace AmplifyShaderEditor
{
	[Serializable]
	public class RenderPlatformInfo
	{
		public string Label;
		public RenderPlatforms Value;
	}

	public enum RenderPlatforms
	{
		d3d9,
		d3d11,
		glcore,
		gles,
		gles3,
		metal,
		d3d11_9x,
		xbox360,
		xboxone,
		xboxseries,
		ps4,
		playstation,
		psp2,
		n3ds,
		wiiu,
		@switch,
		vulkan,
		nomrt,
		ps5,        // @diogo: added in 19100
	#if UNITY_6000_0_OR_NEWER
		switch2,        // @diogo: added in 19905
		webgpu,         // @diogo: added in 19905
	#endif
		all
	}

	[Serializable]
	[NoAutoStaticsCleanup]
	public class RenderingPlatformOpHelper
	{
		private const string RenderingPlatformsStr = " Rendering Platforms";
		public static readonly RenderPlatformInfo[] RenderingPlatformsInfo =
		{
			new RenderPlatformInfo(){Label = " Direct3D 11/12", Value = RenderPlatforms.d3d11},
			new RenderPlatformInfo(){Label = " OpenGL 3.x/4.x", Value = RenderPlatforms.glcore},
			new RenderPlatformInfo(){Label = " OpenGL ES 2.0", Value = RenderPlatforms.gles},
			new RenderPlatformInfo(){Label = " OpenGL ES 3.x", Value = RenderPlatforms.gles3},
			new RenderPlatformInfo(){Label = " Metal iOS/macOS", Value = RenderPlatforms.metal},
			new RenderPlatformInfo(){Label = " Vulkan", Value = RenderPlatforms.vulkan},
			new RenderPlatformInfo(){Label = " Xbox One", Value = RenderPlatforms.xboxone},
			new RenderPlatformInfo(){Label = " Xbox Series X", Value = RenderPlatforms.xboxseries},
			new RenderPlatformInfo(){Label = " PlayStation", Value = RenderPlatforms.playstation},
			new RenderPlatformInfo(){Label = " PlayStation 4", Value = RenderPlatforms.ps4},
			new RenderPlatformInfo(){Label = " PlayStation 5", Value = RenderPlatforms.ps5},
			new RenderPlatformInfo(){Label = " Nintendo Switch", Value = RenderPlatforms.@switch},
		#if UNITY_6000_0_OR_NEWER
			new RenderPlatformInfo(){Label = " Nintendo Switch 2", Value = RenderPlatforms.switch2},
			new RenderPlatformInfo(){Label = " WebGPU", Value = RenderPlatforms.webgpu},
		#endif
		};

		// Values from this dictionary must be the indices corresponding from the list above
		public static readonly Dictionary<RenderPlatforms, int> PlatformToIndex = new Dictionary<RenderPlatforms, int>()
		{
			{RenderPlatforms.d3d11,			0},
			{RenderPlatforms.glcore,		1},
			{RenderPlatforms.gles,			2},
			{RenderPlatforms.gles3,			3},
			{RenderPlatforms.metal,			4},
			{RenderPlatforms.vulkan,		5},
			{RenderPlatforms.xboxone,		6},
			{RenderPlatforms.xboxseries,    7},
			{RenderPlatforms.playstation,   8},
			{RenderPlatforms.ps4,			9},
			{RenderPlatforms.ps5,			10},
			{RenderPlatforms.@switch,		11},
		#if UNITY_6000_0_OR_NEWER
			{RenderPlatforms.switch2,		12},
			{RenderPlatforms.webgpu,		13},
		#endif
		};


		public static readonly List<RenderPlatforms> LegacyIndexToPlatform = new List<RenderPlatforms>()
		{
			RenderPlatforms.d3d9,
			RenderPlatforms.d3d11,
			RenderPlatforms.glcore,
			RenderPlatforms.gles,
			RenderPlatforms.gles3,
			RenderPlatforms.metal,
			RenderPlatforms.d3d11_9x,
			RenderPlatforms.xbox360,
			RenderPlatforms.xboxone,
			RenderPlatforms.ps4,
			RenderPlatforms.psp2,
			RenderPlatforms.n3ds,
			RenderPlatforms.wiiu
		};

		[SerializeField]
		private bool[] m_renderingPlatformValues;

		public RenderingPlatformOpHelper()
		{
			m_renderingPlatformValues = new bool[ RenderingPlatformsInfo.Length ];
			for( int i = 0; i < m_renderingPlatformValues.Length; i++ )
			{
				m_renderingPlatformValues[ i ] = true;
			}
		}


		public void Draw( ParentNode owner )
		{
			bool value = owner.ContainerGraph.ParentWindow.InnerWindowVariables.ExpandedRenderingPlatforms;
			NodeUtils.DrawPropertyGroup( ref value, RenderingPlatformsStr, () =>
			{
				DrawPlatformToggles( owner );
			} );
			owner.ContainerGraph.ParentWindow.InnerWindowVariables.ExpandedRenderingPlatforms = value;
		}

		public void DrawNested( ParentNode owner )
		{
			bool value = owner.ContainerGraph.ParentWindow.InnerWindowVariables.ExpandedRenderingPlatforms;
			NodeUtils.DrawNestedPropertyGroup( ref value , RenderingPlatformsStr , () =>
			{
				DrawPlatformToggles( owner );
			} );
			owner.ContainerGraph.ParentWindow.InnerWindowVariables.ExpandedRenderingPlatforms = value;
		}

		// @diogo: "PlayStation" is an umbrella over ps4 + ps5; keep the three toggles in sync
		private void DrawPlatformToggles( ParentNode owner )
		{
			int psIndex = PlatformToIndex[ RenderPlatforms.playstation ];
			int ps4Index = PlatformToIndex[ RenderPlatforms.ps4 ];
			int ps5Index = PlatformToIndex[ RenderPlatforms.ps5 ];

			for( int i = 0; i < m_renderingPlatformValues.Length; i++ )
			{
				if( i == psIndex )
				{
					EditorGUI.BeginChangeCheck();
					bool playstation = owner.EditorGUILayoutToggleLeft( RenderingPlatformsInfo[ i ].Label, m_renderingPlatformValues[ i ] );
					if( EditorGUI.EndChangeCheck() )
					{
						// toggling the umbrella drives both child platforms
						m_renderingPlatformValues[ ps4Index ] = playstation;
						m_renderingPlatformValues[ ps5Index ] = playstation;
					}
				}
				else
				{
					m_renderingPlatformValues[ i ] = owner.EditorGUILayoutToggleLeft( RenderingPlatformsInfo[ i ].Label, m_renderingPlatformValues[ i ] );
				}
			}

			// umbrella reflects its children: on only when both are on
			m_renderingPlatformValues[ psIndex ] = m_renderingPlatformValues[ ps4Index ] && m_renderingPlatformValues[ ps5Index ];
		}

		// @diogo: programmatic single-platform setter for batch tooling ( Shader Utility ); mirrors the
		// playstation-umbrella rules in DrawPlatformToggles so the serialized state stays consistent.
		public void SetPlatform( RenderPlatforms platform, bool enabled )
		{
			if( m_renderingPlatformValues == null )
				return;

			int psIndex = PlatformToIndex[ RenderPlatforms.playstation ];
			int ps4Index = PlatformToIndex[ RenderPlatforms.ps4 ];
			int ps5Index = PlatformToIndex[ RenderPlatforms.ps5 ];

			if( platform == RenderPlatforms.playstation )
			{
				// toggling the umbrella drives both child platforms
				m_renderingPlatformValues[ psIndex ] = enabled;
				m_renderingPlatformValues[ ps4Index ] = enabled;
				m_renderingPlatformValues[ ps5Index ] = enabled;
				return;
			}

			if( PlatformToIndex.TryGetValue( platform, out int index ) )
			{
				m_renderingPlatformValues[ index ] = enabled;
				// umbrella reflects its children: on only when both are on
				m_renderingPlatformValues[ psIndex ] = m_renderingPlatformValues[ ps4Index ] && m_renderingPlatformValues[ ps5Index ];
			}
		}


		public string CreateResult( bool addPragmaPrefix = false )
		{
			int checkedPlatforms = 0;
			int uncheckedPlatforms = 0;

			for ( int i = 0; i < m_renderingPlatformValues.Length; i++ )
			{
				if ( m_renderingPlatformValues[ i ] )
				{
					checkedPlatforms += 1;
				}
				else
				{
					uncheckedPlatforms += 1;
				}
			}

			if ( checkedPlatforms > 0 && checkedPlatforms < m_renderingPlatformValues.Length )
			{
			#if UNITY_6000_0_OR_NEWER
				// @diogo: thanks, Unity...
				int unityVersion = TemplateHelperFunctions.GetUnityVersion();
				bool supportsSwitch2 = ( unityVersion < 60010000 && unityVersion >= 60000059 ) || ( unityVersion >= 60030000 );
			#endif

				string result = string.Empty;

				// @diogo: "playstation" token stands in for both ps4 and ps5
				bool playstationActive = m_renderingPlatformValues[ PlatformToIndex[ RenderPlatforms.ps4 ] ] && m_renderingPlatformValues[ PlatformToIndex[ RenderPlatforms.ps5 ] ];

				if ( checkedPlatforms < uncheckedPlatforms )
				{
					for ( int i = 0; i < m_renderingPlatformValues.Length; i++ )
					{
						if ( ShouldEmitToken( i, true, playstationActive ) )
						{
						#if UNITY_6000_0_OR_NEWER
							if ( RenderingPlatformsInfo[ i ].Value == RenderPlatforms.switch2 && !supportsSwitch2 )
							{
								continue;
							}
						#endif

							if ( string.IsNullOrEmpty( result ) )
							{
								result = "only_renderers ";
							}

							result += ( RenderPlatforms )RenderingPlatformsInfo[ i ].Value + " ";
						}
					}
				}
				else
				{
					for ( int i = 0; i < m_renderingPlatformValues.Length; i++ )
					{
						if ( ShouldEmitToken( i, false, playstationActive ) )
						{
						#if UNITY_6000_0_OR_NEWER
							if ( RenderingPlatformsInfo[ i ].Value == RenderPlatforms.switch2 && !supportsSwitch2 )
							{
								continue;
							}
						#endif

							if ( string.IsNullOrEmpty( result ) )
							{
								result = "exclude_renderers ";
							}

							result += ( RenderPlatforms )RenderingPlatformsInfo[ i ].Value + " ";
						}
					}
				}

				if ( !string.IsNullOrEmpty( result ) && addPragmaPrefix )
				{
					result = "#pragma " + result;
				}
				return result;
			}
			return string.Empty;
		}

		// @diogo: "playstation" is the umbrella token for ps4 + ps5
		private bool ShouldEmitToken( int i, bool onlyRenderers, bool playstationActive )
		{
			int psIndex = PlatformToIndex[ RenderPlatforms.playstation ];
			int ps4Index = PlatformToIndex[ RenderPlatforms.ps4 ];
			int ps5Index = PlatformToIndex[ RenderPlatforms.ps5 ];

			if ( onlyRenderers )
			{
				if ( i == psIndex )
				{
					// fold ps4 + ps5 into the single "playstation" token
					return playstationActive;
				}
				if ( i == ps4Index || i == ps5Index )
				{
					return !playstationActive && m_renderingPlatformValues[ i ];
				}
				return m_renderingPlatformValues[ i ];
			}

			// exclude_renderers: fold ps4 + ps5 into the single "playstation" token when both are off
			bool playstationInactive = !m_renderingPlatformValues[ ps4Index ] && !m_renderingPlatformValues[ ps5Index ];
			if ( i == psIndex )
			{
				return playstationInactive;
			}
			if ( i == ps4Index || i == ps5Index )
			{
				return !playstationInactive && !m_renderingPlatformValues[ i ];
			}
			return !m_renderingPlatformValues[ i ];
		}

		public void SetRenderingPlatforms( ref string ShaderBody )
		{
			string result = CreateResult();
			if( !string.IsNullOrEmpty(result))
			{
				MasterNode.AddShaderPragma( ref ShaderBody, result );
			}
		}

		public void ReadFromString( ref uint index, ref string[] nodeParams )
		{
			if( UIUtils.CurrentShaderVersion() < 17006 )
			{
				for( int i = 0; i < m_renderingPlatformValues.Length; i++ )
				{
					m_renderingPlatformValues[ i ] = false;
				}

				int count = LegacyIndexToPlatform.Count;
				int activeCount = 0;
				for( int i = 0; i < count; i++ )
				{
					RenderPlatforms platform = LegacyIndexToPlatform[ i ];
					bool value = Convert.ToBoolean( nodeParams[ index++ ] );
					// @diogo: some legacy platforms (d3d9, d3d11_9x, xbox360, psp2, n3ds, wiiu) no longer
					// exist in PlatformToIndex; still consume their serialized value to keep the read index
					// aligned, but only map the ones that survive
					if ( PlatformToIndex.TryGetValue( platform, out int newIndex ) )
					{
						m_renderingPlatformValues[ newIndex ] = value;
					}
					if ( value )
					{
						activeCount += 1;
					}
				}

				if( activeCount == count )
				{
					m_renderingPlatformValues[ PlatformToIndex[ RenderPlatforms.vulkan ] ] = true;
				}
			}
			else
			{
				int count = Convert.ToInt32( nodeParams[ index++ ] );
				if( count > 0 )
				{
					string firstPlatformToken = nodeParams[ index++ ];
					if( firstPlatformToken == RenderPlatforms.all.ToString() )
					{
						for( int i = 0; i < m_renderingPlatformValues.Length; i++ )
						{
							m_renderingPlatformValues[ i ] = true;
						}
					}
					else
					{
						for( int i = 0; i < m_renderingPlatformValues.Length; i++ )
						{
							m_renderingPlatformValues[ i ] = false;
						}

						// @diogo: should have been designed as exclude, instead of include, to avoid stuff like this...
						if ( UIUtils.CurrentShaderVersion() < 19100 )
						{
							m_renderingPlatformValues[ PlatformToIndex[ RenderPlatforms.ps5 ] ] = true;
						}
						if ( UIUtils.CurrentShaderVersion() < 19905 )
						{
						#if UNITY_6000_0_OR_NEWER
							m_renderingPlatformValues[ PlatformToIndex[ RenderPlatforms.switch2 ] ] = true;
							m_renderingPlatformValues[ PlatformToIndex[ RenderPlatforms.webgpu ] ] = true;
						#endif
						}

						for( int i = 0; i < count; i++ )
						{
							string platformToken = ( i == 0 ) ? firstPlatformToken : nodeParams[ index++ ];
							// @diogo: skip platform tokens saved by a newer editor/Unity that don't exist in this enum build
							// (e.g. switch2/webgpu under Unity < 6000) while keeping the read index aligned
							if ( !Enum.TryParse( platformToken, out RenderPlatforms currPlatform ) )
								continue;
							if ( currPlatform == RenderPlatforms.playstation )
							{
								// @diogo: the "playstation" token stands in for both ps4 and ps5
								m_renderingPlatformValues[ PlatformToIndex[ RenderPlatforms.playstation ] ] = true;
								m_renderingPlatformValues[ PlatformToIndex[ RenderPlatforms.ps4 ] ] = true;
								m_renderingPlatformValues[ PlatformToIndex[ RenderPlatforms.ps5 ] ] = true;
							}
							else if ( PlatformToIndex.TryGetValue( currPlatform, out int platformIndex ) )
							{
								m_renderingPlatformValues[ platformIndex ] = true;
							}
						}
					}
				}
			}
		}

		public void WriteToString( ref string nodeInfo )
		{
			int active = 0;
			for( int i = 0; i < m_renderingPlatformValues.Length; i++ )
			{
				if( m_renderingPlatformValues[ i ] )
					active += 1;
			}
			IOUtils.AddFieldValueToString( ref nodeInfo, active );
			if( active == m_renderingPlatformValues.Length )
			{
				IOUtils.AddFieldValueToString( ref nodeInfo, RenderPlatforms.all );
			}
			else
			{
				for( int i = 0; i < m_renderingPlatformValues.Length; i++ )
				{
					if( m_renderingPlatformValues[ i ] )
					{
						IOUtils.AddFieldValueToString( ref nodeInfo, RenderingPlatformsInfo[i].Value );
					}
				}
			}

		}

		public void Destroy()
		{
			m_renderingPlatformValues = null;
		}

		//TEMPLATE SPECIFIC
		[SerializeField]
		private bool m_loadedFromTemplate = false;

		[SerializeField]
		private bool m_validData = false;
		public void SetupFromTemplate( TemplateRenderPlatformHelper template )
		{
			if( m_loadedFromTemplate )
				return;

			if( m_renderingPlatformValues.Length != template.RenderingPlatforms.Length )
			{
				Debug.LogError( "Rendering platform length mismatch" );
				return;
			}


			m_loadedFromTemplate = true;
			m_validData = true;

			for( int i = 0 ; i < m_renderingPlatformValues.Length ; i++ )
			{
				m_renderingPlatformValues[ i ] = template.RenderingPlatforms[ i ];
			}

			// @diogo: the "playstation" token stands in for both ps4 and ps5
			if( m_renderingPlatformValues[ PlatformToIndex[ RenderPlatforms.playstation ] ] )
			{
				m_renderingPlatformValues[ PlatformToIndex[ RenderPlatforms.ps4 ] ] = true;
				m_renderingPlatformValues[ PlatformToIndex[ RenderPlatforms.ps5 ] ] = true;
			}
		}

		public void ReadFromStringTemplate( ref uint index , ref string[] nodeParams )
		{
			if( UIUtils.CurrentShaderVersion() > 18911 )
			{
				bool isValid = Convert.ToBoolean( nodeParams[ index++ ] );
				if( isValid )
				{
					ReadFromString( ref index , ref nodeParams );
				}
			}
			else
			{
				ReadFromString( ref index , ref nodeParams );
			}
		}

		public void WriteToStringTemplate( ref string nodeInfo )
		{
			IOUtils.AddFieldValueToString( ref nodeInfo , m_validData );
			if( m_validData )
			{
				WriteToString( ref nodeInfo );
			}
		}

		public bool LoadedFromTemplate { get { return m_loadedFromTemplate; } }
		public bool ValidData { get { return m_validData; } }
	}
}

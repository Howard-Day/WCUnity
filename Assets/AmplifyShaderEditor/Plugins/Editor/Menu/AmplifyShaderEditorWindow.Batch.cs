// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Unity.Scripting.LifecycleManagement;

namespace AmplifyShaderEditor
{
	[NoAutoStaticsCleanup]
	public partial class AmplifyShaderEditorWindow
	{
		public const string ASEFileList = "ASEfileList";

		// True while a batch resave is running ( BatchUpdateShaders -> LoadAndSaveList cycles graphs through
		// this window one after another ). Derived from the same key the batch driver sets/clears, so it
		// auto-resets when the run finishes or aborts with no state to restore. Stored in SessionState, not
		// EditorPrefs: EditorPrefs is machine-global per Unity version, so a batch in one editor instance
		// would wrongly read as "running" in every other instance ( and a mid-batch crash would leave the
		// key set forever ). SessionState is per editor process, survives domain reloads, and is cleared on
		// quit. Undo registration and the scheduled auto-backup are suppressed while this is set: their
		// snapshots would be of transient graphs loaded only to resave, not of anything the user is editing.
		public static bool IsBatchProcessing { get { return !string.IsNullOrEmpty( SessionState.GetString( ASEFileList, string.Empty ) ); } }

		private static System.Diagnostics.Stopwatch m_batchTimer = new System.Diagnostics.Stopwatch();
		private static Action m_batchOnFinish = null;

		private List<string> m_assetPaths = new List<string>();
		public OpenSaveState State = OpenSaveState.NONE;
		public enum OpenSaveState
		{
			NONE,
			OPEN,
			WAIT,
			SAVE,
			CLOSE
		}

		// MP Modification Begin
		private static bool modifyShaderDuringLoadSave = false;
		// Queued platform op consumed by ModifyCurrentShader during a LoadModifyAndSaveList batch.
		private static RenderPlatforms s_batchPlatform = RenderPlatforms.all;
		private static bool s_batchPlatformEnabled = false;
		// MP Modification End

		public static int BatchTotal = 0;
		public static int BatchDone = 0;
		private static bool s_batchCancelled = false;
		private static bool s_batchAssetEditing = false;

		public static void InitBatch( int total )
		{
			BatchTotal = total;
			BatchDone = 0;
			s_batchCancelled = false;
			// Defer all shader imports to a single pass at the end of the batch. DisallowAutoRefresh stops
			// Unity's file-change scan from importing the resaved shaders mid-batch. We intentionally do NOT
			// use StartAssetEditing here: that scope is meant for a tight synchronous block, and holding it
			// open across the batch's many editor frames makes Unity display a "Hold on... Importing assets"
			// progress dialog for the whole run ( the bar frozen, since the imports are all still deferred ).
			// The matching EndBatchAssetEditing() must run on every batch exit ( finish or cancel ) or
			// auto-refresh is left disabled until the editor restarts.
			if ( !s_batchAssetEditing )
			{
				AssetDatabase.DisallowAutoRefresh();
				s_batchAssetEditing = true;
			}
		}

		private static void EndBatchAssetEditing()
		{
			if ( s_batchAssetEditing )
			{
				s_batchAssetEditing = false;
				AssetDatabase.AllowAutoRefresh();
				// Import everything the batch wrote to disk, in a single pass now that the run is done.
				AssetDatabase.Refresh();
			}
		}

		// Resets batch state and closes the deferred-import scope. Called on every batch exit ( normal
		// finish, cancel, or an abort while loading the next asset ) so auto-refresh is never left disabled.
		private static void FinishBatch()
		{
			s_batchCancelled = false;
			BatchTotal = 0;
			BatchDone = 0;
			SessionState.EraseString( ASEFileList );

			// Flush the deferred imports queued during the batch before the finish callback runs,
			// so it observes the freshly imported assets.
			EndBatchAssetEditing();

			if ( m_batchOnFinish != null )
			{
				m_batchOnFinish();
				m_batchOnFinish = null;
			}
			modifyShaderDuringLoadSave = false;  // MP Modification

			m_batchTimer.Stop();
			if ( Preferences.User.LogBatchCompile )
			{
				Debug.Log( "[AmplifyShaderEditor] Finished batch compilation in " + m_batchTimer.Elapsed.TotalSeconds.ToString( "0.00" ) + " seconds." );
			}
		}

		public static void CancelBatch()
		{
			s_batchCancelled = true;
		}

		// @diogo: closing the batch-driving window must act like Cancel: with the window gone nothing pumps
		// the state machine again, freezing progress UIs and leaving auto-refresh disabled. State is
		// only non-NONE on the window currently cycling a shader; the self-Close() between shaders
		// runs with State already NONE, so it doesn't trip this.
		private void AbortBatchOnWindowClose()
		{
			if ( IsBatchProcessing && State != OpenSaveState.NONE )
			{
				State = OpenSaveState.NONE;
				FinishBatch();
			}
		}

		public static void LoadAndSaveList( string[] assetList, Action onFinish = null )
		{
			m_batchOnFinish += onFinish;

			// @diogo: LoadAndSaveList is re-entered once per shader ( see ProcessSaveStateMachine's CLOSE state ),
			// so only reset/start the timer at the true start of a batch, otherwise it would only ever measure the
			// last shader. IsBatchProcessing reads the ASEFileList set below, still set on every recursive call.
			if ( !IsBatchProcessing )
			{
				m_batchTimer.Reset();
				m_batchTimer.Start();
			}

			SessionState.SetString( ASEFileList , string.Join( ",", assetList ) );

			if ( Preferences.User.LogShaderCompile )
			{
				// Logged before the asset is loaded so that, if processing hard-crashes or hangs, the last line names the culprit.
				// The asset is passed as the log context so selecting the entry pings it in the Project window.
				UnityEngine.Object pingTarget = AssetDatabase.LoadMainAssetAtPath( assetList[ 0 ] );
				Debug.Log( "[AmplifyShaderEditor] Batch processing next: " + assetList[ 0 ] + " (" + assetList.Length + " remaining)", pingTarget );
			}

			if( assetList[ 0 ].EndsWith( ".asset" ) )
			{
				var obj = AssetDatabase.LoadAssetAtPath<AmplifyShaderFunction>( assetList[ 0 ] );
				AmplifyShaderEditorWindow.LoadShaderFunctionToASE( obj, false , true );
			}
			else
			{
				var obj = AssetDatabase.LoadAssetAtPath<Shader>( assetList[ 0 ] );
				// MP Modification: changed the following bool from `true` to `false` to prevent
				// a ton of windows opening up and stealing focus during the batch save process.
				AmplifyShaderEditorWindow.ConvertShaderToASE( obj, false );
			}

			UIUtils.CurrentWindow.State = AmplifyShaderEditorWindow.OpenSaveState.OPEN;
			UIUtils.CurrentWindow.Repaint();
		}

		// @diogo: LoadAndSaveList's counterpart without the save/close cycle: every asset is loaded into its own
		// tab and left open, reusing the tab of any already open. Nothing is written to disk, so there is no
		// batch state machine and no deferred-import scope, and the whole list is loaded in one synchronous
		// pass ( callers must not run it from inside an OnGUI pass ).
		public static void LoadList( string[] assetList )
		{
			for ( int i = 0; i < assetList.Length; i++ )
			{
				string assetPath = assetList[ i ];
				if ( assetPath.EndsWith( ".asset" ) )
				{
					AmplifyShaderFunction shaderFunction = AssetDatabase.LoadAssetAtPath<AmplifyShaderFunction>( assetPath );
					if ( shaderFunction != null )
					{
						LoadShaderFunctionToASE( shaderFunction, false );
					}
				}
				else
				{
					Shader shader = AssetDatabase.LoadAssetAtPath<Shader>( assetPath );
					if ( shader != null )
					{
						ConvertShaderToASE( shader );
					}
				}
			}
		}

		// MP Modification Begin
		// Batch enable/disable of a single render platform across the loaded shaders, applied per-shader during
		// the SAVE state ( see ModifyCurrentShader ). Uses the public RenderingPlatformOpHelper.SetPlatform API
		// instead of reaching into private fields.
		public static void LoadModifyAndSaveList( string[] assetList, RenderPlatforms platform, bool enabled )
		{
			s_batchPlatform = platform;
			s_batchPlatformEnabled = enabled;
			modifyShaderDuringLoadSave = true;
			InitBatch( assetList.Length );
			LoadAndSaveList( assetList );
		}

		private void ModifyCurrentShader ()
		{
			foreach( ParentNode node in m_mainGraphInstance.AllNodes )
			{
				if( node is TemplateMultiPassMasterNode templateMaster )
				{
					templateMaster.SubShaderModule.RenderingPlatforms.SetPlatform( s_batchPlatform, s_batchPlatformEnabled );
				}
				else if( node is StandardSurfaceOutputNode standardMaster )
				{
					standardMaster.RenderingPlatforms.SetPlatform( s_batchPlatform, s_batchPlatformEnabled );
				}
			}
		}
		// MP Modification End

		private void ProcessSaveStateMachine()
		{
			if( Event.current.type == EventType.Layout )
			{
				switch( State )
				{
					default:
					case OpenSaveState.NONE:
					break;
					case OpenSaveState.OPEN:
					{
						State = OpenSaveState.WAIT;
						string list = SessionState.GetString( ASEFileList , "" );
						m_assetPaths = new List<string>( list.Split( ',' ) );
						Repaint();
					}
					break;
					case OpenSaveState.WAIT:
					{
						// we wait one frame to give time for the editor to properly initialize everything
						State = OpenSaveState.SAVE;
						Repaint();
					}
					break;
					case OpenSaveState.SAVE:
					{
						// MP Modification Begin
						if (modifyShaderDuringLoadSave)
						{
							ModifyCurrentShader();
						}
						// MP Modification End

						State = OpenSaveState.CLOSE;
						try
						{
							SaveToDisk( false );
						}
						catch( Exception e )
						{
							// Log which shader failed, preserving the real stack trace, and leave State as CLOSE so the
							// batch skips this shader and continues with the rest instead of aborting the whole run.
							string failedPath = ( m_assetPaths != null && m_assetPaths.Count > 0 ) ? m_assetPaths[ 0 ] : "<unknown>";
							// @diogo: read-only files are an expected skip; log one clean warning, keep full error+stack for other exceptions.
							if ( e is UnauthorizedAccessException )
							{
								Debug.LogWarning( "[AmplifyShaderEditor] Skipped read-only shader during batch resave: " + failedPath );
							}
							else
							{
								Debug.LogError( "[AmplifyShaderEditor] Batch resave failed while saving: " + failedPath );
								Debug.LogException( e );
							}
							modifyShaderDuringLoadSave = false;  // MP Modification
						}

						Repaint();
					}
					break;
					case OpenSaveState.CLOSE:
					{
						State = OpenSaveState.NONE;
						m_assetPaths.RemoveAt( 0 );
						BatchDone++;
						if( m_assetPaths.Count > 0 && !s_batchCancelled )
						{
							try
							{
								AmplifyShaderEditorWindow.LoadAndSaveList( m_assetPaths.ToArray() );
							}
							catch( Exception e )
							{
								// Loading the next asset failed; finish the batch so the deferred-import scope is
								// always closed instead of leaving the AssetDatabase paused.
								Debug.LogError( "[AmplifyShaderEditor] Batch aborted while loading the next asset." );
								Debug.LogException( e );
								FinishBatch();
							}
						}
						else
						{
							FinishBatch();
						}
						this.Close();
					}
					break;
				}
			}
		}
	}
}

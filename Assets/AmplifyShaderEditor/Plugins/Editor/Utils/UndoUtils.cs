using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Internal;
using System;

namespace AmplifyShaderEditor
{
	public class UndoUtils
	{
		public static void RegisterUndoRedoCallback( Undo.UndoRedoCallback onUndoRedo )
		{
			if ( Preferences.User.EnableUndo )
			{
				Undo.undoRedoPerformed -= onUndoRedo;
				Undo.undoRedoPerformed += onUndoRedo;
			}			
		}

		public static void UnregisterUndoRedoCallback( Undo.UndoRedoCallback onUndoRedo )
		{
			// @diogo: unconditional on purpose: gating on EnableUndo would leak the subscription if the pref flips off mid-session
			Undo.undoRedoPerformed -= onUndoRedo;
		}

		public static void ClearUndo( UnityEngine.Object obj )
		{
			if ( Preferences.User.EnableUndo )
			{
				Profiler.BeginSample( "Undo_ClearUndo" );
				Undo.ClearUndo( obj );
				Profiler.EndSample();
			}
		}

		// Destroys the object without an undo record; the snapshot proxy restores it on undo.
		public static void DestroyObjectImmediate( UnityEngine.Object objectToUndo )
		{
			UnityEngine.Object.DestroyImmediate( objectToUndo );
		}

		public static void PerformUndo()
		{
			if ( Preferences.User.EnableUndo )
			{
				Profiler.BeginSample( "Undo_PerformUndo" );
				Undo.PerformUndo();
				Profiler.EndSample();
			}
		}

		public static void PerformRedo()
		{
			if ( Preferences.User.EnableUndo )
			{
				Profiler.BeginSample( "Undo_PerformRedo" );
				Undo.PerformRedo();
				Profiler.EndSample();
			}
		}

		public static void IncrementCurrentGroup()
		{
			if ( Preferences.User.EnableUndo )
			{
				Profiler.BeginSample( "Undo_IncrementCurrentGroup" );
				Undo.IncrementCurrentGroup();
				Profiler.EndSample();
			}
		}
	}
}

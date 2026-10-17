// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Unity.Scripting.LifecycleManagement;

namespace AmplifyShaderEditor
{
	// One stored backup of a graph snapshot on disk.
	public struct AutoBackupEntry
	{
		public string FilePath;
		public string DisplayName;
		public DateTime Timestamp;
	}

	// File-side of the auto-backup system: writes graph snapshots to disk, lists/prunes the history and
	// reads a snapshot back. Each graph gets its own subfolder under the configured root, keyed by its
	// asset GUID ( or a per-session id while unsaved ), so a graph's history is self-contained and the
	// history-size cap applies per graph. The snapshot stored is whatever AmplifyShaderEditorWindow.
	// GenerateGraphSnapshot produces - the same self-contained meta a .shader file embeds - so a restore
	// is just a LoadFromMeta on the file's contents. Timing/scheduling lives on the window; this class is
	// stateless file I/O.
	[NoAutoStaticsCleanup]
	public static class AutoBackup
	{
		public const string DefaultRelativeFolder = "Library/AmplifyShaderEditor/AutoBackup";
		private const string Extension = ".asebackup";
		private const string TimestampFormat = "yyyyMMdd-HHmmss-fff";

		// Bumped on every successful backup write so a UI showing the history can invalidate its cache on
		// an actual change instead of polling the disk on a timer.
		public static int Revision { get; private set; }

		// Absolute path of the backup root. An empty/relative preference resolves against the project
		// root ( the folder containing Assets ), so the default lands in Library/ - outside Assets, so
		// Unity never imports the backups as assets.
		public static string ResolveRoot()
		{
			string folder = Preferences.User.AutoBackupFolder;
			if ( string.IsNullOrEmpty( folder ) )
			{
				folder = DefaultRelativeFolder;
			}

			if ( !Path.IsPathRooted( folder ) )
			{
				string projectRoot = Directory.GetParent( Application.dataPath ).FullName;
				folder = Path.Combine( projectRoot, folder );
			}

			return folder;
		}

		// Collapses an absolute path that lives inside the project to a project-relative one ( forward
		// slashes ); paths outside the project, or already-relative ones, are returned unchanged. Used so
		// the preferences field shows a short relative folder unless the user picked one elsewhere.
		public static string ToProjectRelative( string path )
		{
			if ( string.IsNullOrEmpty( path ) || !Path.IsPathRooted( path ) )
			{
				return path;
			}

			string rootFull = Path.GetFullPath( Directory.GetParent( Application.dataPath ).FullName );
			string full = Path.GetFullPath( path );
			if ( full.StartsWith( rootFull + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase ) )
			{
				return full.Substring( rootFull.Length + 1 ).Replace( '\\', '/' );
			}

			return path;
		}

		// Writes a snapshot for the given graph, then trims the graph's history to the configured size.
		public static void SaveBackup( string key, string displayName, string snapshot )
		{
			if ( string.IsNullOrEmpty( snapshot ) )
			{
				return;
			}

			try
			{
				string dir = GraphFolder( key );
				Directory.CreateDirectory( dir );

				string name = Sanitize( displayName, "shader" );
				string fileName = name + "_" + DateTime.Now.ToString( TimestampFormat ) + Extension;
				File.WriteAllText( Path.Combine( dir, fileName ), snapshot );

				Prune( dir, Preferences.User.AutoBackupHistorySize );
				Revision++;
			}
			catch ( Exception e )
			{
				Debug.LogError( "[AmplifyShaderEditor] Auto Backup failed to write: " + e.Message );
			}
		}

		// @diogo: moves a graph's backup history to a new key, e.g. when an unsaved graph is saved and gains a GUID.
		public static void MigrateKey( string oldKey, string newKey )
		{
			if ( string.IsNullOrEmpty( oldKey ) || string.IsNullOrEmpty( newKey ) || oldKey == newKey )
			{
				return;
			}

			string oldDir = GraphFolder( oldKey );
			if ( !Directory.Exists( oldDir ) )
			{
				return;
			}

			string newDir = GraphFolder( newKey );
			try
			{
				if ( !Directory.Exists( newDir ) )
				{
					Directory.Move( oldDir, newDir );
				}
				else
				{
					string[] files = Directory.GetFiles( oldDir, "*" + Extension );
					for ( int i = 0; i < files.Length; i++ )
					{
						string dest = Path.Combine( newDir, Path.GetFileName( files[ i ] ) );
						if ( !File.Exists( dest ) )
						{
							File.Move( files[ i ], dest );
						}
					}

					Directory.Delete( oldDir, true );
					Prune( newDir, Preferences.User.AutoBackupHistorySize );
				}

				Revision++;
			}
			catch ( Exception e )
			{
				Debug.LogWarning( "[AmplifyShaderEditor] Auto Backup could not migrate history from '" + oldKey + "' to '" + newKey + "': " + e.Message );
			}
		}

		// @diogo: renames a backup by rewriting the display-name portion of its file name, preserving the
		// trailing timestamp token ( and thus history order ). Returns the new path, or the old on failure.
		public static string Rename( string filePath, string newLabel )
		{
			if ( string.IsNullOrEmpty( filePath ) || !File.Exists( filePath ) )
			{
				return filePath;
			}

			string fileName = Path.GetFileNameWithoutExtension( filePath );
			int split = fileName.LastIndexOf( '_' );
			if ( split < 0 || split >= fileName.Length - 1 )
			{
				return filePath;
			}

			string stampToken = fileName.Substring( split + 1 );
			string newName = Sanitize( newLabel, "shader" ) + "_" + stampToken + Extension;
			string newPath = Path.Combine( Path.GetDirectoryName( filePath ), newName );
			if ( string.Equals( newPath, filePath, StringComparison.OrdinalIgnoreCase ) )
			{
				return filePath;
			}

			try
			{
				if ( File.Exists( newPath ) )
				{
					Debug.LogWarning( "[AmplifyShaderEditor] Auto Backup could not rename backup: a file named '" + newName + "' already exists." );
					return filePath;
				}
				File.Move( filePath, newPath );
				Revision++;
				return newPath;
			}
			catch ( Exception e )
			{
				Debug.LogWarning( "[AmplifyShaderEditor] Auto Backup could not rename backup: " + e.Message );
				return filePath;
			}
		}

		// @diogo: a backup keeps its auto-generated name ( the sanitized shader name at save time ) until the
		// user renames it; the restore list shows date/time for those and the custom label otherwise.
		public static bool IsDefaultName( string displayName, string rawShaderName )
		{
			return displayName == Sanitize( rawShaderName, "shader" );
		}

		// History for a single graph, newest first.
		public static List<AutoBackupEntry> GetHistory( string key )
		{
			List<AutoBackupEntry> result = new List<AutoBackupEntry>();
			string dir = GraphFolder( key );
			if ( !Directory.Exists( dir ) )
			{
				return result;
			}

			string[] files = Directory.GetFiles( dir, "*" + Extension );
			for ( int i = 0; i < files.Length; i++ )
			{
				AutoBackupEntry entry;
				entry.FilePath = files[ i ];
				entry.Timestamp = ParseTimestamp( files[ i ] );
				entry.DisplayName = ParseDisplayName( files[ i ] );
				result.Add( entry );
			}

			result.Sort( ( a, b ) => b.Timestamp.CompareTo( a.Timestamp ) );
			return result;
		}

		public static string LoadSnapshot( string filePath )
		{
			if ( string.IsNullOrEmpty( filePath ) || !File.Exists( filePath ) )
			{
				return string.Empty;
			}

			return File.ReadAllText( filePath );
		}

		// Reveals the graph's backup folder in the OS file browser, creating it if needed.
		public static void RevealFolder( string key )
		{
			string dir = GraphFolder( key );
			Directory.CreateDirectory( dir );
			EditorUtility.RevealInFinder( dir );
		}

		private static string GraphFolder( string key )
		{
			return Path.Combine( ResolveRoot(), Sanitize( key, "graph" ) );
		}

		// Deletes the oldest backups until at most maxCount remain.
		private static void Prune( string dir, int maxCount )
		{
			if ( maxCount < 1 )
			{
				maxCount = 1;
			}

			string[] files = Directory.GetFiles( dir, "*" + Extension );
			if ( files.Length <= maxCount )
			{
				return;
			}

			Array.Sort( files, ( a, b ) => ParseTimestamp( a ).CompareTo( ParseTimestamp( b ) ) );
			int toRemove = files.Length - maxCount;
			for ( int i = 0; i < toRemove; i++ )
			{
				try
				{
					File.Delete( files[ i ] );
				}
				catch ( Exception e )
				{
					Debug.LogWarning( "[AmplifyShaderEditor] Auto Backup could not delete '" + files[ i ] + "': " + e.Message );
				}
			}
		}

		// The timestamp lives in the trailing token of the file name ( name_yyyyMMdd-HHmmss-fff ). The
		// display-name portion can itself contain underscores, so the timestamp is split off from the
		// right. Falls back to the file's last-write time when the token is missing/malformed.
		private static DateTime ParseTimestamp( string filePath )
		{
			string fileName = Path.GetFileNameWithoutExtension( filePath );
			int split = fileName.LastIndexOf( '_' );
			if ( split >= 0 && split < fileName.Length - 1 )
			{
				string stamp = fileName.Substring( split + 1 );
				if ( DateTime.TryParseExact( stamp, TimestampFormat, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime parsed ) )
				{
					return parsed;
				}
			}

			return File.GetLastWriteTime( filePath );
		}

		private static string ParseDisplayName( string filePath )
		{
			string fileName = Path.GetFileNameWithoutExtension( filePath );
			int split = fileName.LastIndexOf( '_' );
			if ( split > 0 )
			{
				return fileName.Substring( 0, split );
			}

			return fileName;
		}

		private static string Sanitize( string value, string fallback )
		{
			if ( string.IsNullOrEmpty( value ) )
			{
				return fallback;
			}

			char[] invalid = Path.GetInvalidFileNameChars();
			for ( int i = 0; i < invalid.Length; i++ )
			{
				value = value.Replace( invalid[ i ], '_' );
			}

			return value;
		}
	}
}

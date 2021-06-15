using UnityEditor;
using System.IO;
using UnityEngine;

[InitializeOnLoad]
public class CleanEmptyFolders
{
	// Check for empty folders when you quit.
	static CleanEmptyFolders()
	{
		EditorApplication.wantsToQuit += OnQuit;
	}

	static bool OnQuit()
	{
		CheckForEmptyFolder(new DirectoryInfo(Application.dataPath));
		return true;
	}

	// This is actually recursive.
    private static void CheckForEmptyFolder(DirectoryInfo subDirectory)
    {
		// Go depth-first on directories and nuke empty ones first
		foreach (DirectoryInfo di in subDirectory.EnumerateDirectories())
		{
			CheckForEmptyFolder(di);
		}

		// These two checks are really fast--better than returning the whole list
		bool hasFiles = false;
		foreach (FileInfo fi in subDirectory.EnumerateFiles())
		{
			hasFiles = true;
			break;
		}

		bool hasSubdirectories = false;
		foreach (DirectoryInfo di in subDirectory.EnumerateDirectories())
		{
			hasSubdirectories = true;
			break;
		}

		if (!hasFiles && !hasSubdirectories)
		{
			Debug.Log("Deleted empty folder: "+subDirectory.FullName);
			subDirectory.Delete(true);
			
			// Also delete the corresponding .meta file
			File.Delete(subDirectory.FullName + ".meta");
		}
    }
}
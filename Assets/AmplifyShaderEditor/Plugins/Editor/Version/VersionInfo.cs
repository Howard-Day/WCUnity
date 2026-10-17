// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using Unity.Scripting.LifecycleManagement;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NoAutoStaticsCleanup]
	public class VersionInfo
	{
		public const byte Major = 1;
		public const byte Minor = 9;
		public const byte Release = 9;
		public static byte Revision = 14;
		public static string Stage = ""; // empty for official releases

		public static string StaticToString()
		{
			return string.Format( "{0}.{1}.{2}", Major, Minor, Release, Stage ) + ( Revision > 0 ? "." + Revision.ToString() : "" ) + Stage;
		}

		public static int FullNumber { get { return Major * 10000 + Minor * 1000 + Release * 100 + Revision; } }
		public static string FullLabel { get { return "Version=" + FullNumber; } }
	}
}

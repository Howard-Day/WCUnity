// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

#if !UNITY_6000_5_OR_NEWER
using System;

namespace Unity.Scripting.LifecycleManagement
{
	// @diogo: keeps the namespace alive below 6000.5 so ASE's "using" of it compiles unguarded
}

namespace AmplifyShaderEditor
{
	// Stand-in for the attribute Unity made public in 6000.5, so editor types can be tagged
	// unconditionally; editor statics must survive entering Play Mode, so ASE opts out everywhere.
	[AttributeUsage( AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Event, AllowMultiple = false )]
	internal sealed class NoAutoStaticsCleanupAttribute : Attribute { }
}
#endif

// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEngine;

namespace AmplifyShaderEditor
{
	sealed class ScopedTimer : IDisposable
	{
		readonly string label;
		readonly bool enabled;
		readonly long start;

		public ScopedTimer( string label, bool enabled = true )
		{
			this.label = label;
			this.start = DateTime.Now.Ticks;
			this.enabled = enabled;
		}

		public void Dispose()
		{
			if ( this.enabled )
			{
				Debug.LogFormat( "[AmplifyShaderEditor] {0} took {1:0.##} ms", label, ( DateTime.Now.Ticks - start ) / 10000.0f );
			}
		}
	}
}

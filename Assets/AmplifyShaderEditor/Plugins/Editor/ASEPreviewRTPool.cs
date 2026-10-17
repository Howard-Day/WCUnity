// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

// @diogo: transient preview pool for QA #5 (pooled previews). See docs/preview-rt-pooling.md.
// Hands out preview slices at the global preview size/format for the duration of a shader function's
// compute window, so only the peak concurrent live set is ever held. The storage itself lives in
// ASEPreviewSlices ( whose free list gives the reuse ); this class only keeps the per-window counts the
// stats line and the leak check read. Callers must not Return a slice that is still aliased for display
// (e.g. a FunctionOutput slice referenced by a visible FunctionNode).

using UnityEngine;

namespace AmplifyShaderEditor
{
	public class ASEPreviewRTPool
	{
		private int m_inUse = 0;
		private int m_peak = 0;

		public int Allocated { get { return m_peak; } }
		public int InUse { get { return m_inUse; } }

		public ASEPreviewSlice Checkout( ParentNode consumer )
		{
			m_inUse++;
			m_peak = Mathf.Max( m_peak, m_inUse );
			return ASEPreviewSlices.Allocate( UIUtils.CurrentPreviewSize, Preferences.User.PreviewFormat, consumer );
		}

		public void Return( ASEPreviewSlice slice )
		{
			if ( slice.IsValid )
			{
				m_inUse--;
				ASEPreviewSlices.Free( slice );
			}
		}

		public void Flush()
		{
			m_peak = 0;
		}
	}
}

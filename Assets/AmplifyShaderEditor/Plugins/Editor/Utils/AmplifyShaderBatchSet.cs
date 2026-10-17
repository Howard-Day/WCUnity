// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.Collections.Generic;
using UnityEngine;

namespace AmplifyShaderEditor
{
	// @diogo: a saved, reusable list of ASE shaders regenerated together ( "batch resave" ). The resave runs
	// through the same InitBatch/LoadAndSaveList path the Shader Utility uses, from the asset's inspector.
	[CreateAssetMenu( menuName = "Amplify Shader Tools/Batch Set", fileName = "New Shader Batch Set", order = 87 )]
	public class AmplifyShaderBatchSet : ScriptableObject
	{
		[Serializable]
		public class Entry
		{
			public Shader Shader = null;
			public bool Enabled = true;
		}

		[SerializeField]
		private List<Entry> m_entries = new List<Entry>();

		[SerializeField]
		private bool m_active = true;

		public List<Entry> Entries { get { return m_entries; } }

		// @diogo: consumed only by AmplifyShaderBatchAPI.ResaveActiveBatchSets ( there is no manager window ).
		public bool Active { get { return m_active; } set { m_active = value; } }
	}
}

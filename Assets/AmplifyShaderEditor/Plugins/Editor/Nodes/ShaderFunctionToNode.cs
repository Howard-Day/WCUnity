// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using Unity.Scripting.LifecycleManagement;

namespace AmplifyShaderEditor
{
	// @diogo: This class is used as a helper to replace ourl simpler SFs with their scripted node counterparts; for performance reasons.
	[NoAutoStaticsCleanup]
	public static class ShaderFunctionToNode
	{
		private static Dictionary<string, Type> m_table = new Dictionary<string, Type>
		{
			{ "06a5811c77de878498d3c93c4fd438d5", typeof( Hash22Node ) },
			{ "50f923f3b90822e47953386ea346e02f", typeof( AndNode ) },
			{ "dcfde22f80031984b87bcc46a052ad1f", typeof( OrNode ) },
			{ "fea980a1f68019543b2cd91d506986e8", typeof( SquareNode ) }
		};

		public static bool IsEligible( string guid )
		{
			return m_table.ContainsKey( guid );
		}

		// Swaps an eligible function node for its scripted replacement while a graph loads
		public static ParentNode ConvertIfEligible( ParentNode node )
		{
			ParentNode result = node;
			if ( node is FunctionNode functionNode && functionNode.IsEligibleForConversion )
			{
				string previousName = functionNode.Filename;
				result = Convert( functionNode );
				UIUtils.ShowMessage( $"Automatically converted \"{previousName}\" SF to internal \"{result.GetType().Name}\"", MessageSeverity.Normal );
			}
			return result;
		}

		public static ParentNode Convert( FunctionNode functionNode )
		{
			if ( m_table.TryGetValue( functionNode.FunctionGUID, out Type replacementNodeType ) )
			{
				// Create a new node to replace the function node
				ParentNode replacementNode = ( ParentNode )ScriptableObject.CreateInstance( replacementNodeType );

				// Copy basic parameters
				replacementNode.CopyBasePropertiesFrom( functionNode );

				// Delete the now unused function node
				ScriptableObject.DestroyImmediate( functionNode );

				return replacementNode;
			}
			return null;
		}
	}
}

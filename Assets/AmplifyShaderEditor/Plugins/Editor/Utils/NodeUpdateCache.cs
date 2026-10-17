using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmplifyShaderEditor
{
	public class NodeUpdateCache : HashSet<ParentNode>
	{
		public bool Touch( ParentNode node )
		{
			if ( Contains( node ) )
			{
				// @diogo: already touched; cache hit
				return true;
			}
			else
			{
				// @diogo: not touched yet; cache miss
				Add( node );
				return false;
			}
		}

	}
}

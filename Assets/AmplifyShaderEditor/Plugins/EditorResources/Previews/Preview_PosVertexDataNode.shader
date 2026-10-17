Shader "Hidden/PosVertexDataNode"
{
	SubShader
	{
		Pass
		{
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "Preview.cginc"

			float4 frag(v2f_img i) : SV_Target
			{
				float3 vertexPos = PreviewFragmentPositionOS( i.uv );
				return float4(vertexPos, 1);
			}
			ENDCG
		}
	}
}

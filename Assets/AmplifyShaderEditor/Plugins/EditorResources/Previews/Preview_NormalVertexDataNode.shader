Shader "Hidden/NormalVertexDataNode"
{
	SubShader
	{
		Pass
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#pragma vertex vert_img
			#pragma fragment frag

			float4 frag(v2f_img i) : SV_Target
			{
				float3 normal = PreviewFragmentNormalOS( i.uv );
				return float4(normal, 1);
			}
			ENDCG
		}
	}
}

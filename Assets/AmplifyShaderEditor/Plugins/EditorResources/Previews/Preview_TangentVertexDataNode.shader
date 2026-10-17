Shader "Hidden/TangentVertexDataNode"
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

			float4 frag( v2f_img i ) : SV_Target
			{
				float3 normal = PreviewFragmentNormalOS( i.uv );
				float3 tangent = normalize(float3( -normal.z, normal.y*0.01, normal.x ));
				return float4((tangent), 1);
			}
			ENDCG
		}
	}
}

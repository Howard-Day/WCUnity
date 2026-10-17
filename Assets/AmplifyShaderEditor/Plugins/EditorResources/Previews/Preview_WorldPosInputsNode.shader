Shader "Hidden/WorldPosInputsNode"
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
				float3 vertexPos = PreviewFragmentPositionOS( i.uv );
				float4 worldPos = mul(unity_ObjectToWorld, vertexPos);
				return float4 (worldPos.xyz, 1);
			}
			ENDCG
		}
	}
}

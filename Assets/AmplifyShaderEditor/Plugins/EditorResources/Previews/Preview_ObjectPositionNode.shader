Shader "Hidden/ObjectPositionNode"
{
	SubShader
	{
		
		Pass
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma vertex vert_img
			#pragma fragment frag

			float4 frag(v2f_img i) : SV_Target
			{
				float3 objectScale = UNITY_MATRIX_M._m03_m13_m23;
				return float4(objectScale, 1);
			}
			ENDCG
		}
	}
}

Shader "Hidden/WorldSpaceCameraPos"
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
				//_WorldSpaceCameraPos
				return float4(preview_WorldSpaceCameraPos,0);
			}
			ENDCG
		}
	}
}

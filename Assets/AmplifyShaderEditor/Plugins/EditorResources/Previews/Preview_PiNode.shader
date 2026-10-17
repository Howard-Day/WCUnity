Shader "Hidden/PiNode"
{
	SubShader
	{
		Pass
		{
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5
			#include "UnityCG.cginc"
			#include "Preview.cginc"

			ASE_PORT( _A );

			float4 frag(v2f_img i) : SV_Target
			{
				return ASE_SAMPLE_PORT( _A, i.uv ).r * UNITY_PI;
			}
			ENDCG
		}
	}
}

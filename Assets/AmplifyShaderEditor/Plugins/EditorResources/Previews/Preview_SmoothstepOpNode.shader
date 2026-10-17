Shader "Hidden/SmoothstepOpNode"
{
	Properties
	{
		_A ("_Alpha", 2DArray) = "white" {}
		_B ("_Min", 2DArray) = "white" {}
		_C ("_Max", 2DArray) = "white" {}
	}
	SubShader
	{
		Pass
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5

			ASE_PORT( _A );
			ASE_PORT( _B );
			ASE_PORT( _C );

			float4 frag(v2f_img i) : SV_Target
			{
				float4 alpha = ASE_SAMPLE_PORT( _A, i.uv );
				float4 min = ASE_SAMPLE_PORT( _B, i.uv );
				float4 max = ASE_SAMPLE_PORT( _C, i.uv );
				return smoothstep(min, max, alpha);
			}
			ENDCG
		}
	}
}

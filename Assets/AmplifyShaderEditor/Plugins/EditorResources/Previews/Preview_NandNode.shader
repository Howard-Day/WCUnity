Shader "Hidden/AllNode"
{
	Properties
	{
		_A ("_A", 2DArray) = "white" {}
		_B ("_B", 2DArray) = "white" {}
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

			float4 frag(v2f_img i) : SV_Target
			{
				return ( !ASE_SAMPLE_PORT( _A, i.uv) && !ASE_SAMPLE_PORT( _B, i.uv) ) ? ( 1 ).xxxx : ( 0 ).xxxx;
			}
			ENDCG
		}
	}
}

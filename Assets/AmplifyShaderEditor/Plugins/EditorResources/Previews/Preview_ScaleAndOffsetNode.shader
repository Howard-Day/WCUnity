Shader "Hidden/ScaleAndOffsetNode"
{
	Properties
	{
		_A ("_Value", 2DArray) = "white" {}
		_B ("_Scale", 2DArray) = "white" {}
		_C ("_Offset", 2DArray) = "white" {}
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
				float4 v = ASE_SAMPLE_PORT( _A, i.uv );
				float4 s = ASE_SAMPLE_PORT( _B, i.uv );
				float4 o = ASE_SAMPLE_PORT( _C, i.uv );

				return v * s + o;
			}
			ENDCG
		}
	}
}

Shader "Hidden/RefractOpVec"
{
	Properties
	{
		_A ("_Incident", 2DArray) = "white" {}
		_B ("_Normal", 2DArray) = "white" {}
		_C ("_Eta", 2DArray) = "white" {}
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
				float4 inc = ASE_SAMPLE_PORT( _A, i.uv );
				float4 nor = ASE_SAMPLE_PORT( _B, i.uv );
				float eta = ASE_SAMPLE_PORT( _C, i.uv ).x;
				return refract( inc, nor, eta );
			}
			ENDCG
		}
	}
}

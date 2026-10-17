Shader "Hidden/TFHCCompareGreater"
{
	Properties
	{
		_A ("_A", 2DArray) = "white" {}
		_B ("_B", 2DArray) = "white" {}
		_C ("_True", 2DArray) = "white" {}
		_D ( "_False", 2DArray ) = "white" {}
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
			ASE_PORT( _D );

			float4 frag(v2f_img i) : SV_Target
			{
				float A = ASE_SAMPLE_PORT( _A, i.uv ).x;
				float B = ASE_SAMPLE_PORT( _B, i.uv ).x;
				float4 True = ASE_SAMPLE_PORT( _C, i.uv );
				float4 False = ASE_SAMPLE_PORT( _D, i.uv );
				return ( ( A > B ) ? True : False );
			}
			ENDCG
		}
	}
}

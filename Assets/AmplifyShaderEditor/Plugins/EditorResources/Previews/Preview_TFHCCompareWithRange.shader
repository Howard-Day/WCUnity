Shader "Hidden/TFHCCompareWithRange"
{
	Properties
	{
		_A ("_Value", 2DArray) = "white" {}
		_B ("_RangeMin", 2DArray) = "white" {}
		_C ("_RangeMax", 2DArray) = "white" {}
		_D ( "_True", 2DArray ) = "white" {}
		_E ( "_False", 2DArray ) = "white" {}
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
			ASE_PORT( _E );

			float4 frag(v2f_img i) : SV_Target
			{
				float4 Value = ASE_SAMPLE_PORT( _A, i.uv ).x;
				float4 RangeMin = ASE_SAMPLE_PORT( _B, i.uv ).x;
				float4 RangeMax = ASE_SAMPLE_PORT( _C, i.uv );
				float4 True = ASE_SAMPLE_PORT( _D, i.uv );
				float4 False = ASE_SAMPLE_PORT( _E, i.uv );
				return ( ( Value >= RangeMin && Value <= RangeMax ) ? True : False );
			}
			ENDCG
		}
	}
}

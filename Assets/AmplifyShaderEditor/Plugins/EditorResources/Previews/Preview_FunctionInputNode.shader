Shader "Hidden/FunctionInputNode"
{
	Properties
	{
		_A ("_A", 2DArray) = "white" {}
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
			int _Type;

			float4 frag(v2f_img i) : SV_Target
			{
				if( _Type == 1 )
				{
					return ASE_SAMPLE_PORT( _A, i.uv ).r;
				} else if( _Type == 2 )
				{
					return float4(ASE_SAMPLE_PORT( _A, i.uv ).rg,0,0);
				} else if( _Type == 3 )
				{
					return float4(ASE_SAMPLE_PORT( _A, i.uv ).rgb,0);
				}
				else
				{
					return ASE_SAMPLE_PORT( _A, i.uv );
				}
			}
			ENDCG
		}
	}
}

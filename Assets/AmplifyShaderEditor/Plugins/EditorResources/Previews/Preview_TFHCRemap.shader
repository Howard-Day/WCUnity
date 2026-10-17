Shader "Hidden/TFHCRemap"
{
	Properties
	{
		_A ("_Value", 2DArray) = "white" {}
		_B ("_MinOld", 2DArray) = "white" {}
		_C ("_MaxOld", 2DArray) = "white" {}
		_D ("_MinNew", 2DArray) = "white" {}
		_E ("_MaxNew", 2DArray) = "white" {}
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
				float4 value = ASE_SAMPLE_PORT( _A, i.uv );
				float4 minold = ASE_SAMPLE_PORT( _B, i.uv );
				float4 maxold = ASE_SAMPLE_PORT( _C, i.uv );
				float4 minnew = ASE_SAMPLE_PORT( _D, i.uv );
				float4 maxnew = ASE_SAMPLE_PORT( _E, i.uv );

				float4 denom = maxold - minold;
				if(denom.x == 0)
					denom.x = 0.000001;
				if(denom.y == 0)
					denom.y = 0.000001;
				if(denom.z == 0)
					denom.z = 0.000001;
				if(denom.w == 0)
					denom.w = 0.000001;

				return (minnew + (value - minold) * (maxnew - minnew) / denom);
			}
			ENDCG
		}
	}
}

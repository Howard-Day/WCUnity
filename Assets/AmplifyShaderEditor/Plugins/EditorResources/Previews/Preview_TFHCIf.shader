Shader "Hidden/TFHCIf"
{
	Properties
	{
		_A ("_A", 2DArray) = "white" {}
		_B ("_B", 2DArray) = "white" {}
		_C ("_AGreaterB", 2DArray) = "white" {}
		_D ( "_AEqualsB", 2DArray ) = "white" {}
		_E ( "_ALessB", 2DArray ) = "white" {}
		_F ( "_EqualThreshold", 2DArray ) = "white" {}
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
			ASE_PORT( _F );

			float4 frag(v2f_img i) : SV_Target
			{
				float4 A = ASE_SAMPLE_PORT( _A, i.uv ).x;
				float4 B = ASE_SAMPLE_PORT( _B, i.uv ).x;
				float4 AGreaterB = ASE_SAMPLE_PORT( _C, i.uv );
				float4 AEqualsB = ASE_SAMPLE_PORT( _D, i.uv );
				float4 ALessB = ASE_SAMPLE_PORT( _E, i.uv );
				float4 EqualThreshold = ASE_SAMPLE_PORT( _F, i.uv );
				return ( A - EqualThreshold > B ? AGreaterB : A - EqualThreshold <= B && A + EqualThreshold >= B ? AEqualsB : ALessB );
			}
			ENDCG
		}
	}
}

Shader "Hidden/SwitchByFaceNode"
{
	Properties
	{
		_A ("_Front", 2DArray) = "white" {}
		_B ("_Back", 2DArray) = "white" {}
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

			float4 frag( v2f_img i, half ase_vface : VFACE ) : SV_Target
			{
				float4 front = ASE_SAMPLE_PORT( _A, i.uv );
				float4 back = ASE_SAMPLE_PORT( _B, i.uv );
				return ( ( ase_vface > 0 ) ? ( front ) : ( back ) );
			}
			ENDCG
		}
	}
}

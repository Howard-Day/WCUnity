Shader "Hidden/HeightMapTextureBlend"
{
	Properties
	{
		_A ("_A", 2DArray) = "white" {}
		_B ("_B", 2DArray) = "white" {}
		_C ("_C", 2DArray) = "white" {}
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

			float4 frag( v2f_img i ) : SV_Target
			{
				float heightmap = ASE_SAMPLE_PORT( _A, i.uv ).x;
				float splatMask = ASE_SAMPLE_PORT( _B, i.uv ).x;
				float blendStrength = ASE_SAMPLE_PORT( _C, i.uv ).x;
				return saturate( pow((( heightmap*splatMask ) * 4 ) + ( splatMask * 2 ), blendStrength ));
			}
			ENDCG
		}
	}
}

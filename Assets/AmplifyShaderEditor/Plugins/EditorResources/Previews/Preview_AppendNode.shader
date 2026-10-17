Shader "Hidden/AppendNode"
{
	Properties
	{
		_A ("_A", 2DArray) = "white" {}
		_B ("_B", 2DArray) = "white" {}
		_C ("_C", 2DArray) = "white" {}
		_D ("_D", 2DArray) = "white" {}
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
				float x = ASE_SAMPLE_PORT( _A, i.uv).x;
				float y = ASE_SAMPLE_PORT( _B, i.uv).y;
				float z = ASE_SAMPLE_PORT( _C, i.uv).z;
				float w = ASE_SAMPLE_PORT( _D, i.uv).w;

				return float4(x,y,z,w);
			}
			ENDCG
		}
	}
}

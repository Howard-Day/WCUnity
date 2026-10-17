Shader "Hidden/HSVToRGBNode"
{
	Properties
	{
		_A ( "_Hue",  2DArray ) = "white" {}
		_B ( "_Saturation",  2DArray ) = "white" {}
		_C ( "_Value",  2DArray ) = "white" {}
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

			float4 frag ( v2f_img i ) : SV_Target
			{
				float h = ASE_SAMPLE_PORT( _A, i.uv ).r;
				float s = ASE_SAMPLE_PORT( _B, i.uv ).r;
				float v = ASE_SAMPLE_PORT( _C, i.uv ).r;

				return float4( HSVToRGB(float3(h,s,v)), 1 );
			}
			ENDCG
		}
	}
}

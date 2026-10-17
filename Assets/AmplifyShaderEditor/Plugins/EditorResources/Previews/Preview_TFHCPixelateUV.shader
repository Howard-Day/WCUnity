Shader "Hidden/TFHCPixelateUV"
{
	Properties
	{
		_A ("_UV", 2DArray) = "white" {}
		_B ("_PixelX", 2DArray) = "white" {}
		_C ("_PixelY", 2DArray) = "white" {}
		_D ("_PixelOffset", 2DArray) = "black" {}
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
				float2 uv = ASE_SAMPLE_PORT( _A, i.uv ).rg;
				float pix = ASE_SAMPLE_PORT( _B, i.uv ).r;
				float piy = ASE_SAMPLE_PORT( _C, i.uv ).r;
				float2 poffset = ASE_SAMPLE_PORT( _D, i.uv ).rg;

				float2 steppedPixel = float2( pix, piy );
				float2 pixelatedUV = floor( uv * steppedPixel + poffset ) / steppedPixel;
				return float4(pixelatedUV, 0 , 0);
			}
			ENDCG
		}
	}
}

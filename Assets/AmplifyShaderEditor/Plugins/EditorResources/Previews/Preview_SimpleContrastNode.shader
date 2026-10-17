Shader "Hidden/SimpleContrastNode"
{
	Properties
	{
		_A ( "_RBG",  2DArray ) = "white" {}
		_B ( "_Fraction",  2DArray ) = "white" {}
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
			
			float4 CalculateContrast ( float contrastValue, float4 colorTarget )
			{
				float t = 0.5 * ( 1.0 - contrastValue );
				return mul ( float4x4( contrastValue, 0, 0, t, 0, contrastValue, 0, t, 0, 0, contrastValue, t, 0, 0, 0, 1 ), colorTarget );
			}

			float4 frag ( v2f_img i ) : SV_Target
			{
				float4 rgba = ASE_SAMPLE_PORT( _B, i.uv );
				float value = ASE_SAMPLE_PORT( _A, i.uv ).r;

				float4 finalColor = CalculateContrast( value , rgba );
				
				return finalColor;
			}
			ENDCG
		}
	}
}

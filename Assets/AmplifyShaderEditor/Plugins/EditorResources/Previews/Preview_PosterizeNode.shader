Shader "Hidden/PosterizeNode"
{
	Properties
	{
		_A ( "_RGBA",  2DArray ) = "white" {}
		_B ( "_Power",  2DArray ) = "white" {}
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
			
			float4 frag ( v2f_img i ) : SV_Target
			{
				float4 rgba = ASE_SAMPLE_PORT( _B, i.uv );
				float power = ASE_SAMPLE_PORT( _A, i.uv ).r;
				if ( power < 1 )
					return float4(0,0,0,0);
				float divideOp = 256.0 / float ( (int)power );
				float4 finalColor = ( floor ( rgba * divideOp ) / divideOp );

				return finalColor;
			}
			ENDCG
		}
	}
}

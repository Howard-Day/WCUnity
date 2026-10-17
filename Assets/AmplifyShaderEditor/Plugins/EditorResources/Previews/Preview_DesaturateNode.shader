Shader "Hidden/DesaturateNode"
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
			
			float4 frag ( v2f_img i ) : SV_Target
			{
				float3 rgb = ASE_SAMPLE_PORT( _A, i.uv ).rgb;
				float fraction = ASE_SAMPLE_PORT( _B, i.uv ).r;

				float dotResult = dot ( rgb, float3( 0.299, 0.587, 0.114 ) );
				float3 finalColor = lerp ( rgb, dotResult.xxx, fraction );
				
				return float4( finalColor, 1 );
			}
			ENDCG
		}
	}
}

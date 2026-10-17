Shader "Hidden/Hash11Node"
{
	Properties
	{
		_A ("_A", 2DArray) = "black" {}
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

			float Hash11( float p )
			{
				p = frac( p * 0.1031 );
				p *= p + 33.33;
				p *= p + p;
				return frac( p );
			}

			float4 frag( v2f_img i ) : SV_Target
			{
				float p = ASE_SAMPLE_PORT( _A, i.uv ).r;
				return float4( Hash11( p ), 0, 0, 0 );
			}
			ENDCG
		}
	}
}

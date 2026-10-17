Shader "Hidden/Hash24Node"
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

			float4 Hash24( float2 p )
			{
				float4 p4 = frac( p.xyxy * float4( 0.1031, 0.1030, 0.0973, 0.1099 ) );
				p4 += dot( p4, p4.wzxy + 33.33 );
				return frac( ( p4.xxyz + p4.yzzw ) * p4.zywx );
			}

			float4 frag( v2f_img i ) : SV_Target
			{
				float2 p = ASE_SAMPLE_PORT( _A, i.uv ).rg;
				return Hash24( p );
			}
			ENDCG
		}
	}
}

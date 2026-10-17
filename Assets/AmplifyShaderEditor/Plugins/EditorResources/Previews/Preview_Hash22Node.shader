Shader "Hidden/Hash22Node"
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

			float2 Hash22( float2 p )
			{
				float3 p3 = frac( p.xyx * float3( 0.1031, 0.1030, 0.0973 ) );
				p3 += dot( p3, p3.yzx + 33.33 );
				return frac( ( p3.xx + p3.yz ) * p3.zy );
			}

			float4 frag( v2f_img i ) : SV_Target
			{
				float2 p = ASE_SAMPLE_PORT( _A, i.uv ).rg;
				return float4( Hash22( p ), 0, 0 );
			}
			ENDCG
		}
	}
}

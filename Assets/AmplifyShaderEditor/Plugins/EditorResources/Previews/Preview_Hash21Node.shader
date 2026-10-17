Shader "Hidden/Hash21Node"
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

			float Hash21( float2 p )
			{
				float3 p3 = frac( p.xyx * 0.1031 );
				p3 += dot( p3, p3.yzx + 33.33 );
				return frac( ( p3.x + p3.y ) * p3.z );
			}

			float4 frag( v2f_img i ) : SV_Target
			{
				float2 p = ASE_SAMPLE_PORT( _A, i.uv ).rg;
				return float4( Hash21( p ), 0, 0, 0 );
			}
			ENDCG
		}
	}
}

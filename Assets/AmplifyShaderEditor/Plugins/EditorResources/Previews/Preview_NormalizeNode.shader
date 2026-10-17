Shader "Hidden/NormalizeNode"
{
	Properties
	{
		_A ("_A", 2DArray) = "white" {}
		_Safe("_Safe", Int) = 0
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
			int _Safe;

			float4 safe_normalize( float4 v )
			{
				float d = max( 1.175494351e-38, dot( v, v ) );
				return v * rsqrt( d );
			}

			float4 frag(v2f_img i) : SV_Target
			{
				float4 input = ASE_SAMPLE_PORT( _A, i.uv );
				return ( _Safe == 0 ) ? normalize( input ) : safe_normalize( input );
			}
			ENDCG
		}
	}
}

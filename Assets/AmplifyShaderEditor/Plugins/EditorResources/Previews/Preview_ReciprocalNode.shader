Shader "Hidden/ReciprocalNode"
{
	Properties
	{
		_A ("_A", 2DArray) = "white" {}
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

			float4 frag(v2f_img i) : SV_Target
			{
			#if ( SHADER_TARGET >= 50 )
				return rcp( ASE_SAMPLE_PORT( _A, i.uv ) );
			#else
				return 1.0 / ASE_SAMPLE_PORT( _A, i.uv );
			#endif
			}
			ENDCG
		}
	}
}

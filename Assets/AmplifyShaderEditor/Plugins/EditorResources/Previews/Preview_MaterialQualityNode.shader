Shader "Hidden/MaterialQualityNode"
{
	Properties
	{
		_A ("_A", 2DArray) = "white" {}
		_B ("_B", 2DArray) = "white" {}
		_C ("_C", 2DArray) = "white" {}
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
			#pragma multi_compile MATERIAL_QUALITY_HIGH MATERIAL_QUALITY_MEDIUM MATERIAL_QUALITY_LOW

			ASE_PORT( _A );
			ASE_PORT( _B );
			ASE_PORT( _C );

			float4 frag( v2f_img i ) : SV_Target
			{
				#if defined( MATERIAL_QUALITY_HIGH )
					return ASE_SAMPLE_PORT( _A, i.uv );
				#elif defined( MATERIAL_QUALITY_MEDIUM )
					return ASE_SAMPLE_PORT( _B, i.uv );
				#else
					return ASE_SAMPLE_PORT( _C, i.uv );
				#endif
			}
			ENDCG
		}
	}
}

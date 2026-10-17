Shader "Hidden/TextureCoordinatesNode"
{
	Properties
	{
		_A ("_Tilling", 2DArray) = "white" {}
		_B ("_Offset", 2DArray) = "white" {}
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

			float4 frag( v2f_img i ) : SV_Target
			{
				float4 t = ASE_SAMPLE_PORT( _A, i.uv );
				float4 o = ASE_SAMPLE_PORT( _B, i.uv );
				return float4( i.uv * t.xy + o.xy, 0, 0 );
			}
			ENDCG
		}
	}
}

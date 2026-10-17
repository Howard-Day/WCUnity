Shader "Hidden/EncodeFloatRGBAHlpNode"
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
		

			float4 frag( v2f_img i ) : SV_Target
			{
				float a = ASE_SAMPLE_PORT( _A, i.uv ).r;
				return EncodeFloatRGBA( a );
			}
			ENDCG
		}
	}
}

Shader "Hidden/UnpackScaleNormalNode"
{
	Properties
	{
		_A ("_Value", 2DArray) = "white" {}
		_B ("_NormalScale", 2DArray) = "white" {}
	}
	SubShader
	{
		Pass
		{
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#include "UnityStandardUtils.cginc"

			ASE_PORT( _A );
			ASE_PORT( _B );

			float4 frag( v2f_img i ) : SV_Target
			{
				float4 c = ASE_SAMPLE_PORT( _A, i.uv );
				float n = ASE_SAMPLE_PORT( _B, i.uv ).r;
				c.rgb = UnpackScaleNormal( c, n );

				return float4(c.rgb, 0);
			}
			ENDCG
		}
	}
}
